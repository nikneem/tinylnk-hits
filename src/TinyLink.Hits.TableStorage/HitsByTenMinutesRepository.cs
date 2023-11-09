using Azure.Data.Tables;
using Microsoft.Extensions.Options;
using System.Linq;
using System.Threading;
using TinyLink.Core.Configuration;
using TinyLink.Core.Helpers;
using TinyLink.Hits.Abstractions.DataTransferObjects;
using TinyLink.Hits.Abstractions.Repositories;
using TinyLink.Hits.TableStorage.Entities;

namespace TinyLink.Hits.TableStorage;

public class HitsByTenMinutesRepository: IHitsByTenMinutesRepository
{

    private const string TableName = "hitsbytenminutes";
    private readonly TableClient _tableClient;

    public async Task<List<CumulatedHitDto>> Get(Guid id, string ownerId, DateTimeOffset start, CancellationToken cancellationToken)
    {
        var pollsQuery = _tableClient.QueryAsync<HitTableEntity>($"{nameof(HitTableEntity.PartitionKey)} eq '{id}' and {nameof(HitTableEntity.Timestamp)} gt datetime'{start:s}' and {nameof(HitTableEntity.OwnerId)} eq '{ownerId}'");
        var list = new List<CumulatedHitDto>();
        await foreach (var queryPage in pollsQuery.AsPages().WithCancellation(cancellationToken))
        {
            list.AddRange(queryPage.Values.Select(ent => new CumulatedHitDto(
                ent.RowKey,
                ParseDateTimeKey(ent.RowKey),
                ent.Hits)));
        }

        return list;

    }

    public DateTimeOffset ParseDateTimeKey(string dateTimeKey)
    {
        var year = int.Parse(dateTimeKey[..4]);
        var month = int.Parse(dateTimeKey.Substring(4, 2));
        var day = int.Parse(dateTimeKey.Substring(6, 2));
        var hour = int.Parse(dateTimeKey.Substring(8, 2));
        var minute = int.Parse(dateTimeKey.Substring(10, 2));
        return new DateTimeOffset(year, month, day, hour, minute, 0, TimeSpan.Zero);
    }

    public HitsByTenMinutesRepository(IOptions<AzureCloudConfiguration> config)
    {
        var storageAccountName =
            Environment.GetEnvironmentVariable("StorageAccountName") ?? config.Value.StorageAccountName;
        var identity = CloudIdentity.GetChainedTokenCredential();
        var storageAccountUrl = new Uri($"https://{storageAccountName}.table.core.windows.net");
        _tableClient = new TableClient(storageAccountUrl, TableName, identity);
    }
}