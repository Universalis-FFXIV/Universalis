using System.Threading.Tasks;
using Universalis.Entities.MarketBoard;

namespace Universalis.DbAccess.MarketBoard;

public interface ITaxRatesStore
{
    Task SetTaxRates(int worldId, TaxRates taxRates);

    Task<TaxRates> GetTaxRates(int worldId);
}