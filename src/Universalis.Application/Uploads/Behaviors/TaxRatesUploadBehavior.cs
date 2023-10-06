using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Universalis.Application.Uploads.Schema;
using Universalis.DbAccess.MarketBoard;
using Universalis.DbAccess.Queries.MarketBoard;
using Universalis.Entities.AccessControl;
using Universalis.Entities.MarketBoard;

namespace Universalis.Application.Uploads.Behaviors;

public class TaxRatesUploadBehavior : IUploadBehavior
{
    private readonly ITaxRatesDbAccess _taxRatesDb;

    public TaxRatesUploadBehavior(ITaxRatesDbAccess taxRatesDb)
    {
        _taxRatesDb = taxRatesDb;
    }

    public bool ShouldExecute(UploadParameters parameters)
    {
        return parameters.WorldId != null && parameters.TaxRates != null &&
               !string.IsNullOrEmpty(parameters.UploaderId);
    }

    public async Task<IActionResult> Execute(ApiKey source, UploadParameters parameters,
        CancellationToken cancellationToken = default)
    {
        using var activity = Util.ActivitySource.StartActivity("TaxRatesUploadBehavior.Execute");
        activity?.AddTag("worldId", parameters.WorldId);

        var existingTaxRates = await _taxRatesDb.Retrieve(new TaxRatesQuery { WorldId = parameters.WorldId!.Value },
            cancellationToken);

        await _taxRatesDb.Update(new TaxRates
        {
            LimsaLominsa = parameters.TaxRates!.LimsaLominsa ?? existingTaxRates?.LimsaLominsa ?? 0,
            Gridania = parameters.TaxRates.Gridania ?? existingTaxRates?.Gridania ?? 0,
            Uldah = parameters.TaxRates.Uldah ?? existingTaxRates?.Uldah ?? 0,
            Ishgard = parameters.TaxRates.Ishgard ?? existingTaxRates?.Ishgard ?? 0,
            Kugane = parameters.TaxRates.Kugane ?? existingTaxRates?.Kugane ?? 0,
            Crystarium = parameters.TaxRates.Crystarium ?? existingTaxRates?.Crystarium ?? 0,
            OldSharlayan = parameters.TaxRates.OldSharlayan ?? existingTaxRates?.OldSharlayan ?? 0,
            UploadApplicationName = source.Name,
        }, new TaxRatesQuery
        {
            WorldId = parameters.WorldId.Value,
        }, cancellationToken);

        return null;
    }
}