using System.Threading.Tasks;
using Universalis.Entities.MarketBoard;

namespace Universalis.DbAccess.MarketBoard;

public interface ITaxRatesStore
{
    Task SetTaxRates(uint worldId, TaxRates taxRates);

    Task<TaxRates> GetTaxRates(uint worldId);
}