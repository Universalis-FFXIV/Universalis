using System.Threading.Tasks;
using Universalis.Entities.MarketBoard;

namespace Universalis.DbAccess.MarketBoard;

public interface ITaxRatesStore
{
    Task SetTaxRates(uint worldId, TaxRatesSimple taxRates);

    Task<TaxRatesSimple> GetTaxRates(uint worldId);
}