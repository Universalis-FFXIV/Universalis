using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Universalis.Entities.MarketBoard;

namespace Universalis.DbAccess.MarketBoard;

public interface ISaleStore
{
    Task Insert(Sale sale, CancellationToken cancellationToken = default);

    Task InsertMany(IEnumerable<Sale> sales, CancellationToken cancellationToken = default);

    Task<IEnumerable<Sale>> RetrieveBySaleTime(uint worldId, uint itemId, int count, DateTime? from = null,
        CancellationToken cancellationToken = default);

    Task<long> RetrieveUnitTradeVolume(uint worldId, uint itemId, DateTime from, DateTime to,
        CancellationToken cancellationToken = default);
}