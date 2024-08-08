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

        var uploadedTaxRates = parameters.TaxRates!;
        activity?.AddTag("taxRates.limsaLominsa", uploadedTaxRates.LimsaLominsa);
        activity?.AddTag("taxRates.gridania", uploadedTaxRates.Gridania);
        activity?.AddTag("taxRates.uldah", uploadedTaxRates.Uldah);
        activity?.AddTag("taxRates.ishgard", uploadedTaxRates.Ishgard);
        activity?.AddTag("taxRates.kugane", uploadedTaxRates.Kugane);
        activity?.AddTag("taxRates.crystarium", uploadedTaxRates.Crystarium);
        activity?.AddTag("taxRates.sharlayan", uploadedTaxRates.OldSharlayan);
        activity?.AddTag("taxRates.tuliyollal", uploadedTaxRates.Tuliyollal);
        activity?.AddTag("source", source.Name);

        var existingTaxRates = await _taxRatesDb.Retrieve(new TaxRatesQuery { WorldId = parameters.WorldId!.Value },
            cancellationToken);

        await _taxRatesDb.Update(new TaxRates
        {
            LimsaLominsa = uploadedTaxRates.LimsaLominsa ?? existingTaxRates?.LimsaLominsa ?? 0,
            Gridania = uploadedTaxRates.Gridania ?? existingTaxRates?.Gridania ?? 0,
            Uldah = uploadedTaxRates.Uldah ?? existingTaxRates?.Uldah ?? 0,
            Ishgard = uploadedTaxRates.Ishgard ?? existingTaxRates?.Ishgard ?? 0,
            Kugane = uploadedTaxRates.Kugane ?? existingTaxRates?.Kugane ?? 0,
            Crystarium = uploadedTaxRates.Crystarium ?? existingTaxRates?.Crystarium ?? 0,
            OldSharlayan = uploadedTaxRates.OldSharlayan ?? existingTaxRates?.OldSharlayan ?? 0,
            Tuliyollal = uploadedTaxRates.Tuliyollal ?? existingTaxRates?.Tuliyollal ?? 0,
            UploadApplicationName = source.Name,
        }, new TaxRatesQuery
        {
            WorldId = parameters.WorldId.Value,
        }, cancellationToken);

        return null;
    }
}