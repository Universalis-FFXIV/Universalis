﻿using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Universalis.Application.Tests.Mocks.DbAccess.MarketBoard;
using Universalis.Application.Uploads.Behaviors;
using Universalis.Application.Uploads.Schema;
using Universalis.DbAccess.Queries.MarketBoard;
using Universalis.Entities.AccessControl;
using Universalis.Entities.Uploads;
using Xunit;

namespace Universalis.Application.Tests.Uploads.Behaviors;

public class TaxRatesUploadBehaviorTests
{
    [Fact]
    public void Behavior_DoesNotRun_WithoutTaxRates()
    {
        var dbAccess = new MockTaxRatesDbAccess();
        var behavior = new TaxRatesUploadBehavior(dbAccess);

        var upload = new UploadParameters
        {
            WorldId = 74,
            UploaderId = "5627384655756342554",
        };
        Assert.False(behavior.ShouldExecute(upload));
    }

    [Fact]
    public void Behavior_DoesNotRun_WithoutUploaderId()
    {
        var dbAccess = new MockTaxRatesDbAccess();
        var behavior = new TaxRatesUploadBehavior(dbAccess);

        var upload = new UploadParameters
        {
            WorldId = 74,
            TaxRates = new MarketTaxRates
            {
                LimsaLominsa = 5,
                Gridania = 5,
                Uldah = 5,
                Ishgard = 3,
                Kugane = 0,
                Crystarium = 0,
            },
        };
        Assert.False(behavior.ShouldExecute(upload));
    }

    [Fact]
    public void Behavior_DoesNotRun_WithoutWorldId()
    {
        var dbAccess = new MockTaxRatesDbAccess();
        var behavior = new TaxRatesUploadBehavior(dbAccess);

        var upload = new UploadParameters
        {
            TaxRates = new MarketTaxRates
            {
                LimsaLominsa = 5,
                Gridania = 5,
                Uldah = 5,
                Ishgard = 3,
                Kugane = 0,
                Crystarium = 0,
            },
            UploaderId = "5627384655756342554",
        };
        Assert.False(behavior.ShouldExecute(upload));
    }

    [Fact]
    public async Task Behavior_Succeeds()
    {
        var dbAccess = new MockTaxRatesDbAccess();
        var behavior = new TaxRatesUploadBehavior(dbAccess);

        const string key = "blah";
        using var sha512 = SHA512.Create();
        var hash = Util.BytesToString(sha512.ComputeHash(Encoding.UTF8.GetBytes(key)));
        var source = new ApiKey(hash, "something", true);

        var upload = new UploadParameters
        {
            WorldId = 74,
            TaxRates = new MarketTaxRates
            {
                LimsaLominsa = 5,
                Gridania = 5,
                Uldah = 5,
                Ishgard = 3,
                Kugane = 0,
                Crystarium = 0,
            },
            UploaderId = "5627384655756342554",
        };
        Assert.True(behavior.ShouldExecute(upload));

        var result = await behavior.Execute(source, upload);
        Assert.Null(result);

        var data = await dbAccess.Retrieve(new TaxRatesQuery
        {
            WorldId = 74,
        });

        Assert.NotNull(data);
        Assert.Equal(upload.TaxRates.LimsaLominsa, data.LimsaLominsa);
        Assert.Equal(upload.TaxRates.Gridania, data.Gridania);
        Assert.Equal(upload.TaxRates.Uldah, data.Uldah);
        Assert.Equal(upload.TaxRates.Ishgard, data.Ishgard);
        Assert.Equal(upload.TaxRates.Kugane, data.Kugane);
        Assert.Equal(upload.TaxRates.Crystarium, data.Crystarium);
        Assert.Equal(source.Name, data.UploadApplicationName);
    }
}