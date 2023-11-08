using Azure;
using Azure.Data.Tables;
using Azure.Identity;
using Azure.Messaging.ServiceBus;
using TinyLink.Core.Commands.CommandMessages;
using TinyLink.Core.ExtensionMethods;
using TinyLink.Jobs.HitsTotalizer.Entities;

Console.WriteLine("Starting the hits total accumulation job");
var storageAccountName = Environment.GetEnvironmentVariable("StorageAccountName");
var sourceTableName = Environment.GetEnvironmentVariable("StorageSourceTableName");
var tenMinutesTableName = Environment.GetEnvironmentVariable("StorageTenMinutesTableName");
var totalTableName = Environment.GetEnvironmentVariable("StorageTotalTableName");
var serviceBusName = Environment.GetEnvironmentVariable("ServiceBusName");
var serviceBusQueue = Environment.GetEnvironmentVariable("ServiceBusQueue");

if (string.IsNullOrWhiteSpace(storageAccountName))
{
    Console.WriteLine("Storage account name not configured properly");
    return -1;
}
if (string.IsNullOrWhiteSpace(sourceTableName))
{
    Console.WriteLine("Source table name not configured properly");
    return -2;
}
if (string.IsNullOrWhiteSpace(tenMinutesTableName))
{
    Console.WriteLine("Ten minutes cumulative table name not configured properly");
    return -3;
}
if (string.IsNullOrWhiteSpace(totalTableName))
{
    Console.WriteLine("Total table name not configured properly");
    return -4;
}
if (string.IsNullOrWhiteSpace(serviceBusName))
{
    Console.WriteLine("The name of the Azure Service Bus should have been configured");
    return -4;
}



Console.WriteLine("Configuration fine, retrieving hits from source table");

var identity = new ManagedIdentityCredential();
var storageAccountUrl = new Uri($"https://{storageAccountName}.table.core.windows.net");
var tableClient = new TableClient(storageAccountUrl, sourceTableName, identity);
var tenMinutesTableClient = new TableClient(storageAccountUrl, tenMinutesTableName, identity);
var totalTableClient = new TableClient(storageAccountUrl, totalTableName, identity);
var serviceBusFqdn = $"{serviceBusName}.servicebus.windows.net";
var serviceBusClient = new ServiceBusClient(serviceBusFqdn, identity);
var serviceBusSender = serviceBusClient.CreateSender(serviceBusQueue);

var hitsQuery = tableClient.QueryAsync<HitTableEntity>();
var entities = new List<HitTableEntity>();
Console.WriteLine("Downloading unprocessed hits from calculation table");

await foreach (var queryPage in hitsQuery.AsPages().WithCancellation(CancellationToken.None))
{
    entities.AddRange(queryPage.Values);
}
Console.WriteLine($"Downloaded {entities.Count} entities for accumulation");
if (entities.Count == 0)
{
    // Nothing to do, safely exit
    Console.WriteLine($"{entities.Count} hits waiting, nothing to do, exiting");
    return 0;
}

var withTimeStamp = entities.Where(ent => ent.Timestamp.HasValue).ToList();

// Getting the totals of all hits awaiting calculation
var totalEntities = withTimeStamp.GroupBy(ent => ent.PartitionKey).Select(ent =>
    new HitTableEntity
    {
        PartitionKey = ent.First().PartitionKey,
        RowKey = ent.First().ShortCode,
        ShortCode = ent.First().ShortCode,
        OwnerId = ent.First().OwnerId,
        Hits = ent.Count(),
    });


foreach (var entity in totalEntities)
{
    var totalCounterTransaction = new List<TableTransactionAction>();
    Console.WriteLine($"Adding up totals, for {entity.ShortCode} processing {entity.Hits} hits");

    var totalHitsEntity = new HitTableEntity
    {
        PartitionKey = entity.PartitionKey,
        RowKey = entity.PartitionKey,
        ShortCode = entity.ShortCode,
        OwnerId = entity.OwnerId,
        Hits = entity.Hits,
        Timestamp = entity.Timestamp,
    };
    try
    {
        var existingEntity = await totalTableClient.GetEntityAsync<HitTableEntity>(entity.PartitionKey, entity.PartitionKey, cancellationToken: CancellationToken.None);
        if (existingEntity.HasValue)
        {
            Console.WriteLine($"Shortcode {entity.ShortCode} already had total cumulative of {existingEntity.Value.Hits}");
            totalHitsEntity.Hits += existingEntity.Value.Hits;
            Console.WriteLine($"Added up to a new total of {totalHitsEntity.Hits}");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine(ex.Message);
    }

    try
    {
        Console.WriteLine($"Sending out service bus messate for short code {totalHitsEntity.PartitionKey} ({totalHitsEntity.ShortCode}) with a {totalHitsEntity.Hits} hitcount");
        var command = new TotalHitsChangedCommand(
            Guid.Parse(totalHitsEntity.PartitionKey),
            totalHitsEntity.ShortCode,
            totalHitsEntity.OwnerId,
            totalHitsEntity.Hits);
        await serviceBusSender.SendMessageAsync(command.ToServiceBusMessage(), CancellationToken.None);
    }
    catch (Exception ex)
    {
        Console.WriteLine(ex.Message);
    }

    totalCounterTransaction.Add(new TableTransactionAction(TableTransactionActionType.UpsertReplace, totalHitsEntity));
    if (totalCounterTransaction.Any())
    {
        foreach (var chunk in totalCounterTransaction.Chunk(100))
        {
            Console.WriteLine(
                $"Submitting a transaction of {chunk.Count()} operations for UpsertReplace of total counts");
            await totalTableClient.SubmitTransactionAsync(chunk, CancellationToken.None);
        }
    }
}



Console.WriteLine($"Total hits calculation complete, now working on ten minute accumulatives");

var calculatedMinDate = withTimeStamp.Min(ent => ent.Timestamp).Value;
Console.WriteLine($"Working with a found min date of {calculatedMinDate}");

// Need to try and group accumulates in logical chunks of 10 minutes. So when min date
// is 15:35:24, normalize to 15:30:00 and then take steps of 10 minutes
var roundedMinute = calculatedMinDate.Minute - calculatedMinDate.Minute % 10;
var minDate = new DateTimeOffset(
    calculatedMinDate.Year,
    calculatedMinDate.Month,
    calculatedMinDate.Day,
    calculatedMinDate.Hour,
    roundedMinute,
    0,
    TimeSpan.Zero);
Console.WriteLine($"Calculated to logical chunk of min date {minDate}");

do
{
    var tenMinutesAccumulatedEntities = new List<HitTableEntity>();
    var maxDate = minDate.AddMinutes(10);
    var currentBatch = withTimeStamp.Where(ent => ent.Timestamp >= minDate && ent.Timestamp <= maxDate);

    tenMinutesAccumulatedEntities.AddRange(currentBatch.GroupBy(ent => ent.ShortCode).Select(ent =>
        new HitTableEntity
        {
            PartitionKey = ent.First().PartitionKey,
            RowKey = minDate.ToString("yyyyMMddHHmm"),
            ShortCode = ent.First().ShortCode,
            OwnerId = ent.First().OwnerId,
            Hits = ent.Count(),
        }));

    minDate = minDate.AddMinutes(10);
    if (tenMinutesAccumulatedEntities.Count > 0)
    {
        Console.WriteLine($"Submitting batch of {tenMinutesAccumulatedEntities.Count} ten-minute accumulation operations.");
        foreach (var entity in tenMinutesAccumulatedEntities)
        {
            await tenMinutesTableClient.UpsertEntityAsync(entity, TableUpdateMode.Replace, CancellationToken.None);
        }
    }

} while (minDate < DateTimeOffset.UtcNow);




foreach (var entity in entities)
{
    await tableClient.DeleteEntityAsync(entity.PartitionKey, entity.RowKey, ETag.All, CancellationToken.None);
}

return 0;
