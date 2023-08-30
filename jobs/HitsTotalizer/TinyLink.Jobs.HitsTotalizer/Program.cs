using Azure.Data.Tables;
using Azure.Identity;
using TinyLink.Jobs.HitsTotalizer.Entities;

Console.WriteLine("Starting the hits total accumulation job");
var storageAccountName = Environment.GetEnvironmentVariable("StorageAccountName");
var sourceTableName = Environment.GetEnvironmentVariable("StorageSourceTableName");
var tenMinutesTableName = Environment.GetEnvironmentVariable("StorageTenMinutesTableName");
var totalTableName = Environment.GetEnvironmentVariable("StorageTotalTableName");

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

Console.WriteLine("Configuration fine, retrieving hits from source table");

var identity = new ManagedIdentityCredential();
var storageAccountUrl = new Uri($"https://{storageAccountName}.table.core.windows.net");
var tableClient = new TableClient(storageAccountUrl, sourceTableName, identity);

var hitsQuery = tableClient.QueryAsync<HitTableEntity>($"{nameof(HitTableEntity.PartitionKey)} eq 'hit'");
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

//var withTimeStamp = entities.Where(ent => ent.Timestamp.HasValue).ToList();

//// Getting the totals of all hits awaiting calculation
//var totalEntities = withTimeStamp.GroupBy(ent => ent.ShortCode).Select(ent =>
//    new HitTableEntity
//    {
//        PartitionKey = "hits",
//        RowKey = ent.First().ShortCode,
//        ShortCode = ent.First().ShortCode,
//        OwnerId = ent.First().OwnerId,
//        Hits = ent.Count(),
//    });

//var totalCounterTransaction = new List<TableTransactionAction>();
//foreach (var entity in totalEntities)
//{
//    Console.WriteLine($"Adding up totals, for {entity.ShortCode} processing {entity.Hits} hits");

//    var totalHitsEntity = new HitTableEntity
//    {
//        PartitionKey = "total",
//        RowKey = entity.ShortCode,
//        ShortCode = entity.ShortCode,
//        OwnerId = entity.OwnerId,
//        Hits = entity.Hits,
//        Timestamp = entity.Timestamp,
//    };
//    var existingEntity = await tableClient.GetEntityAsync<HitTableEntity>("hits", entity.RowKey, cancellationToken: CancellationToken.None);
//    if (existingEntity.HasValue)
//    {
//        Console.WriteLine($"Shortcode {entity.ShortCode} already had total cumulative of {existingEntity.Value.Hits}");
//        totalHitsEntity.Hits += existingEntity.Value.Hits;
//        Console.WriteLine($"Added up to a new total of {totalHitsEntity.Hits}");
//    }

//    totalCounterTransaction.Add(new TableTransactionAction(TableTransactionActionType.UpsertReplace, totalHitsEntity));
//}

//if (totalCounterTransaction.Count > 0)
//{
//    Console.WriteLine($"Submitting a transaction of {totalCounterTransaction} operations for UpsertReplace of total counts");
//    await tableClient.SubmitTransactionAsync(totalCounterTransaction, CancellationToken.None);
//}

//Console.WriteLine($"Total hits calculation complete, now working on ten minute accumulatives");

//var calculatedMinDate = withTimeStamp.Min(ent => ent.Timestamp).Value;
//Console.WriteLine($"Working with a found min date of {calculatedMinDate}");

//// Need to try and group accumulates in logical chunks of 10 minutes. So when min date
//// is 15:35:24, normalize to 15:30:00 and then take steps of 10 minutes
//var roundedMinute = calculatedMinDate.Minute - calculatedMinDate.Minute % 10;
//var minDate = new DateTimeOffset(
//    calculatedMinDate.Year,
//    calculatedMinDate.Month,
//    calculatedMinDate.Day,
//    calculatedMinDate.Hour,
//    roundedMinute,
//    0,
//    TimeSpan.Zero);
//Console.WriteLine($"Calculated to logical chunk of min date {minDate}");

//do
//{
//    var maxDate = minDate.AddMinutes(10);
//    var currentBatch = withTimeStamp.Where(ent => ent.Timestamp >= minDate && ent.Timestamp <= maxDate);

//    var accumulatedEntities = currentBatch.GroupBy(ent => ent.ShortCode).Select(ent =>
//        new HitTableEntity
//        {
//            PartitionKey = "hits",
//            RowKey = minDate.ToString("yyyyMMddHHmm"),
//            ShortCode = ent.First().ShortCode,
//            OwnerId = ent.First().OwnerId,
//            Hits = ent.Count(),
//        });

//    var insertAccumulatedEntities = accumulatedEntities.Select(
//        ent =>
//        {
//            Console.WriteLine($"Adding accumulative for {ent.ShortCode} with hit count {ent.Hits}");
//            return new TableTransactionAction(TableTransactionActionType.Add, ent);
//        });

//    await tableClient.SubmitTransactionAsync(insertAccumulatedEntities);
//    minDate = minDate.AddMinutes(10);
//} while (minDate < DateTimeOffset.UtcNow);



//var deleteTransactions = new List<TableTransactionAction>();
//foreach (var entity in entities)
//{
//    deleteTransactions.Add(new TableTransactionAction(TableTransactionActionType.Delete, entity));
//}

//await tableClient.SubmitTransactionAsync(deleteTransactions);

return 0;

