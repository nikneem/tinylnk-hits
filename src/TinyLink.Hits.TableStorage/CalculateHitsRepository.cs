﻿using Azure;
using Azure.Data.Tables;
using HexMaster.DomainDrivenDesign.ChangeTracking;
using Microsoft.Extensions.Options;
using TinyLink.Core.Configuration;
using TinyLink.Core.Helpers;
using TinyLink.Hits.Abstractions.DomainModels;
using TinyLink.Hits.Abstractions.Repositories;
using TinyLink.Hits.TableStorage.Entities;

namespace TinyLink.Hits.TableStorage;

public class CalculateHitsRepository : ICalculateHitsRepository
{

    private const string TableName = "hitsforcalculation";
    private readonly TableClient _tableClient;

    public async Task<bool> Create(IRawHit domainModel, CancellationToken cancellationToken = default)
    {
        if (domainModel.TrackingState != TrackingState.New)
        {
            return false;
        }
        var voteEntity = new HitTableEntity
        {
            PartitionKey = domainModel.ShortCodeId.ToString(),
            RowKey = Guid.NewGuid().ToString(),
            ShortCode = domainModel.ShortCode,
            OwnerId = domainModel.OwnerId,
            Hits = 1,
            Timestamp = domainModel.CreatedOn,
            ETag = ETag.All
        };
        var response = await _tableClient.AddEntityAsync(voteEntity, cancellationToken);
        return !response.IsError;
    }

    public CalculateHitsRepository(IOptions<AzureCloudConfiguration> config)
    {
        var storageAccountName =
            Environment.GetEnvironmentVariable("StorageAccountName") ?? config.Value.StorageAccountName;
        var identity = CloudIdentity.GetChainedTokenCredential();
        var storageAccountUrl = new Uri($"https://{storageAccountName}.table.core.windows.net");
        _tableClient = new TableClient(storageAccountUrl, TableName, identity);
    }
}