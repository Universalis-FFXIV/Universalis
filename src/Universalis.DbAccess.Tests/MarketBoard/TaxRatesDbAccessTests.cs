using MongoDB.Driver;
using System;
using System.Threading.Tasks;
using Universalis.DbAccess.MarketBoard;
using Universalis.DbAccess.Queries.MarketBoard;
using Xunit;

namespace Universalis.DbAccess.Tests.MarketBoard
{
    public class TaxRatesDbAccessTests : IDisposable
    {
        private static readonly string Database = CollectionUtils.GetDatabaseName(nameof(TaxRatesDbAccessTests));

        private readonly IMongoClient _client;
        
        public TaxRatesDbAccessTests()
        {
            _client = new MongoClient("mongodb://localhost:27017");
            _client.DropDatabase(Database);
        }

        public void Dispose()
        {
            _client.DropDatabase(Database);
            GC.SuppressFinalize(this);
        }

        [Fact]
        public async Task Create_DoesNotThrow()
        {
            var db = new TaxRatesDbAccess(_client, Database);
            var document = SeedDataGenerator.MakeTaxRates(74);
            await db.Create(document);
        }

        [Fact]
        public async Task Retrieve_DoesNotThrow()
        {
            var db = new TaxRatesDbAccess(_client, Database);
            var output = await db.Retrieve(new TaxRatesQuery { WorldId = 74 });
            Assert.Null(output);
        }

        [Fact]
        public async Task Update_DoesNotThrow()
        {
            var db = new TaxRatesDbAccess(_client, Database);
            var document = SeedDataGenerator.MakeTaxRates(74);
            await db.Update(document, new TaxRatesQuery { WorldId = document.WorldId });
            await db.Update(document, new TaxRatesQuery { WorldId = document.WorldId });

            document = SeedDataGenerator.MakeTaxRates(74);
            await db.Update(document, new TaxRatesQuery { WorldId = document.WorldId });
        }

        [Fact]
        public async Task Update_DoesUpdate()
        {
            const uint worldId = 74;

            var db = new TaxRatesDbAccess(_client, Database);
            var query = new TaxRatesQuery { WorldId = worldId };

            var document1 = SeedDataGenerator.MakeTaxRates(worldId);
            await db.Update(document1, query);

            var document2 = SeedDataGenerator.MakeTaxRates(worldId);
            await db.Update(document2, query);

            var retrieved = await db.Retrieve(query);
            Assert.Equal(document2, retrieved);
        }

        [Fact]
        public async Task Delete_DoesNotThrow()
        {
            var db = new TaxRatesDbAccess(_client, Database);
            await db.Delete(new TaxRatesQuery { WorldId = 74 });
        }

        [Fact]
        public async Task Create_DoesInsert()
        {
            var db = new TaxRatesDbAccess(_client, Database);
            var document = SeedDataGenerator.MakeTaxRates(74);
            await db.Create(document);

            var output = await db.Retrieve(new TaxRatesQuery { WorldId = document.WorldId });
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