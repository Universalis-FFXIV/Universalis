using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using Microsoft.Extensions.Logging;
using Universalis.Entities.MarketBoard;

namespace Universalis.DbAccess.MarketBoard;

public class MarketItemStore : IMarketItemStore
{
    private readonly ILogger<MarketItemStore> _logger;
    private readonly IAmazonDynamoDB _dynamoDb;
    private readonly DynamoDBContext _ddbContext;

    public MarketItemStore(IAmazonDynamoDB dynamoDb, ILogger<MarketItemStore> logger)
    {
        _dynamoDb = dynamoDb;
        _ddbContext = new DynamoDBContext(_dynamoDb);

        _logger = logger;
    }

    public async Task Insert(MarketItem marketItem, CancellationToken cancellationToken = default)
    {
        if (marketItem == null)
        {
            throw new ArgumentNullException(nameof(marketItem));
        }

        try
        {
            await _ddbContext.SaveAsync(marketItem, cancellationToken);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to insert market item (world={WorldId}, item={ItemId})", marketItem.WorldId, marketItem.ItemId);
        }
    }

    public async Task Update(MarketItem marketItem, CancellationToken cancellationToken = default)
    {
        if (marketItem == null)
        {
            throw new ArgumentNullException(nameof(marketItem));
        }

        try
        {
            await _ddbContext.SaveAsync(marketItem, cancellationToken);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to update market item (world={WorldId}, item={ItemId})", marketItem.WorldId, marketItem.ItemId);
        }
    }

    public async ValueTask<MarketItem> Retrieve(uint worldId, uint itemId, CancellationToken cancellationToken = default)
    {
        try
        {
            var results = await _ddbContext
            .QueryAsync<MarketItem>(itemId, new DynamoDBOperationConfig
            {
                QueryFilter = new List<ScanCondition>
                {
                    new ScanCondition("WorldId", ScanOperator.Equal, worldId),
                },
            })
            .GetRemainingAsync(cancellationToken);
            var match = results.FirstOrDefault();
            if (match == null)
            {
                return null;
            }

            return match;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to retrieve market item (world={WorldId}, item={ItemId})", worldId, itemId);
            return null;
        }
    }

    private static string GetCacheKey(uint worldId, uint itemId)
    {
        return $"market-item:{worldId}:{itemId}";
    }
}