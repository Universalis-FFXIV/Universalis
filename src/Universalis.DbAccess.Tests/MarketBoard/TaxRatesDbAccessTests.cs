using MongoDB.Driver;
using System.Threading.Tasks;
using Universalis.DbAccess.MarketBoard;
using Universalis.DbAccess.Queries.MarketBoard;
using Xunit;

namespace Universalis.DbAccess.Tests.MarketBoard
{
    public class TaxRatesDbAccessTests
    {
        public TaxRatesDbAccessTests()
        {
            var client = new MongoClient("mongodb://localhost:27017");
            client.DropDatabase(Constants.DatabaseName);
        }

        [Fact]
        public async Task Create_DoesNotThrow()
        {
            var db = new TaxRatesDbAccess(Constants.DatabaseName);
            var document = SeedDataGenerator.GetTaxRates(74);
            await db.Create(document);
        }

        [Fact]
        public async Task Retrieve_DoesNotThrow()
        {
            var db = new TaxRatesDbAccess(Constants.DatabaseName);
            var output = await db.Retrieve(new TaxRatesQuery { WorldId = 74 });
            Assert.Null(output);
        }

        [Fact]
        public async Task Update_DoesNotThrow()
        {
            var db = new TaxRatesDbAccess(Constants.DatabaseName);

            var document = SeedDataGenerator.GetTaxRates(74);
            await db.Update(document, new TaxRatesQuery { WorldId = 74 });
        }

        [Fact]
        public async Task Delete_DoesNotThrow()
        {
            var db = new TaxRatesDbAccess(Constants.DatabaseName);
            await db.Delete(new TaxRatesQuery { WorldId = 74 });
        }

        [Fact]
        public async Task Create_DoesInsert()
        {
            var db = new TaxRatesDbAccess(Constants.DatabaseName);

            var document = SeedDataGenerator.GetTaxRates(74);
            await db.Create(document);

            var output = await db.Retrieve(new TaxRatesQuery { WorldId = 74 });
            Assert.NotNull(output);
            Assert.Equal(document.LimsaLominsa, output.LimsaLominsa);
            Assert.Equal(document.Gridania, output.Gridania);
            Assert.Equal(document.Uldah, output.Uldah);
            Assert.Equal(document.Ishgard, output.Ishgard);
            Assert.Equal(document.Kugane, output.Kugane);
            Assert.Equal(document.Crystarium, output.Crystarium);
            Assert.Equal(document.UploaderIdHash, output.UploaderIdHash);
            Assert.Equal(document.WorldId, output.WorldId);
            Assert.Equal(document.UploadApplicationName, output.UploadApplicationName);
        }
    }
}