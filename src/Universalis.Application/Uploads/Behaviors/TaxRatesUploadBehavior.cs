using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Universalis.Application.Uploads.Schema;
using Universalis.DbAccess.MarketBoard;
using Universalis.DbAccess.Queries.MarketBoard;
using Universalis.Entities.MarketBoard;
using Universalis.Entities.Uploads;

namespace Universalis.Application.Uploads.Behaviors
{
    public class TaxRatesUploadBehavior : IUploadBehavior
    {
        private readonly ITaxRatesDbAccess _taxRatesDb;

        public TaxRatesUploadBehavior(ITaxRatesDbAccess taxRatesDb)
        {
            _taxRatesDb = taxRatesDb;
        }

        public bool ShouldExecute(UploadParameters parameters)
        {
            return parameters.WorldId != null && parameters.TaxRates != null && !string.IsNullOrEmpty(parameters.UploaderId);
        }

        public async Task<IActionResult> Execute(TrustedSource source, UploadParameters parameters, CancellationToken cancellationToken = default)
        {
            // ReSharper disable once PossibleInvalidOperationException
            var existingTaxRates = await _taxRatesDb.Retrieve(new TaxRatesQuery { WorldId = parameters.WorldId.Value }, cancellationToken);
            
            await _taxRatesDb.Update(new TaxRates
            {
                LimsaLominsa = parameters.TaxRates.LimsaLominsa,
                Gridania = parameters.TaxRates.Gridania,
                Uldah = parameters.TaxRates.Uldah,
                Ishgard = parameters.TaxRates.Ishgard,
                Kugane = parameters.TaxRates.Kugane,
                Crystarium = parameters.TaxRates.Crystarium,
                OldSharlayan = parameters.TaxRates.OldSharlayan ?? existingTaxRates?.OldSharlayan ?? 0,
                UploaderIdHash = parameters.UploaderId,
                WorldId = parameters.WorldId.Value,
                UploadApplicationName = source.Name,
            }, new TaxRatesQuery
            {
                WorldId = parameters.WorldId.Value,
            }, cancellationToken);

            return null;
        }
    }
}