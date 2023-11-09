﻿using Azure.Data.Tables;
using Microsoft.Extensions.Options;
using TinyLink.Core.Configuration;
using TinyLink.Core.Helpers;
using TinyLink.Hits.Abstractions.DataTransferObjects;
using TinyLink.Hits.Abstractions.Repositories;
using TinyLink.Hits.TableStorage.Entities;

namespace TinyLink.Hits.TableStorage;

public class HitsTotalRepository : IHitsTotalRepository
{

    private const string TableName = "hitstotal";
    private readonly TableClient _tableClient;

    public async Task<HitsTotalDto> GetAsync(string ownerId, string shortCode, CancellationToken cancellationToken)
    {
        var pollsQuery = _tableClient.QueryAsync<HitTableEntity>($"{nameof(HitTableEntity.PartitionKey)} eq 'total' and {nameof(HitTableEntity.RowKey)} eq '{shortCode}' and {nameof(HitTableEntity.OwnerId)} eq '{ownerId}'");
        await foreach (var queryPage in pollsQuery.AsPages().WithCancellation(cancellationToken))
        {
            foreach (var value in queryPage.Values)
            {
                return new HitsTotalDto(value.RowKey, value.Hits, value.Timestamp);
            }
        }

        return new HitsTotalDto(shortCode, 0, DateTimeOffset.UtcNow);
    }


    public HitsTotalRepository(IOptions<AzureCloudConfiguration> config)
    {
        var storageAccountName =
            Environment.GetEnvironmentVariable("StorageAccountName") ?? config.Value.StorageAccountName;
        var identity = CloudIdentity.GetChainedTokenCredential();
        var storageAccountUrl = new Uri($"https://{storageAccountName}.table.core.windows.net");
        _tableClient = new TableClient(storageAccountUrl, TableName, identity);
    }

    public HitsTotalRepository(TableClient tableClient)
    {
        _tableClient = tableClient;
    }
}