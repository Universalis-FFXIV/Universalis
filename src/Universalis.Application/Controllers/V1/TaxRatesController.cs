using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Threading.Tasks;
using Universalis.Application.Views;
using Universalis.DbAccess.MarketBoard;
using Universalis.DbAccess.Queries.MarketBoard;
using Universalis.GameData;

namespace Universalis.Application.Controllers.V1
{
    [ApiController]
    [Route("api/tax-rates")]
    public class TaxRatesController : WorldDcControllerBase
    {
        private readonly ITaxRatesDbAccess _taxRatesDb;

        public TaxRatesController(IGameDataProvider gameData, ITaxRatesDbAccess taxRatesDb) : base(gameData)
        {
            _taxRatesDb = taxRatesDb;
        }

        [HttpGet]
        public async Task<IActionResult> Get([FromQuery, BindRequired] string world)
        {
            if (!TryGetWorldDc(world, out var worldDc))
            {
                return NotFound();
            }

            if (!worldDc.IsWorld)
            {
                return NotFound();
            }

            var taxRates = await _taxRatesDb.Retrieve(new TaxRatesQuery { WorldId = worldDc.WorldId });

            return Ok(new TaxRatesView
            {
                LimsaLominsa = taxRates.LimsaLominsa,
                Gridania = taxRates.Gridania,
                Uldah = taxRates.Uldah,
                Ishgard = taxRates.Ishgard,
                Kugane = taxRates.Kugane,
                Crystarium = taxRates.Crystarium,
            });
        }
    }
}
