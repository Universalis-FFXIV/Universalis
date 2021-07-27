using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System;
using System.Threading.Tasks;
using Universalis.Application.Common;
using Universalis.Application.Views;
using Universalis.DbAccess;
using Universalis.DbAccess.Queries;
using Universalis.GameData;

namespace Universalis.Application.Controllers.V1
{
    [Route("api/tax-rates")]
    [ApiController]
    public class TaxRatesController : ControllerBase
    {
        private readonly IGameDataProvider _gameData;
        private readonly ITaxRatesDbAccess _taxRatesDb;

        public TaxRatesController(IGameDataProvider gameData, ITaxRatesDbAccess taxRatesDb)
        {
            _gameData = gameData;
            _taxRatesDb = taxRatesDb;
        }

        [HttpGet]
        public async Task<IActionResult> Get([FromQuery, BindRequired] string world)
        {
            WorldDc worldDc;
            try
            {
                worldDc = WorldDc.From(world, _gameData);
                if (!worldDc.IsWorld)
                {
                    return NotFound();
                }
            }
            catch (Exception)
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
