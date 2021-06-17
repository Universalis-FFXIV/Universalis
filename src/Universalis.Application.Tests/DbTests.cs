using EfSchemaCompare;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Universalis.Application.DbAccess;
using Xunit;

namespace Universalis.Application.Tests
{
    public class DbTests
    {
        private const string TestConnectionString = "server=localhost;database=universalis_test;user=dalamud;password=dalamud";

        private static DbContextOptions<T> BuildDbContextOptions<T>() where T : DbContext
        {
            return new DbContextOptionsBuilder<T>()
                .UseMySql(TestConnectionString, ServerVersion.AutoDetect(TestConnectionString))
                .Options;
        }

        private static void CompareContext<T>(T context) where T : DbContext
        {
            context.Database.EnsureDeleted();
            context.Database.EnsureCreated();

            var comparer = new CompareEfSql();

            var hasErrors = comparer.CompareEfWithDb(context);

            hasErrors.Should().BeFalse(comparer.GetAllErrors, "because the SQL migrations should match what EF Core expects the database model to be");
        }

        [Fact]
        public void Compare_AuthenticationInfoContext()
        {
            var dbOptions = BuildDbContextOptions<AuthenticationInfoContext>();
            using var context = new AuthenticationInfoContext(dbOptions);
            CompareContext(context);
        }

        [Fact]
        public void Compare_CurrentlyShownContext()
        {
            var dbOptions = BuildDbContextOptions<CurrentlyShownContext>();
            using var context = new CurrentlyShownContext(dbOptions);
            CompareContext(context);
        }

        [Fact]
        public void Compare_UploadApplicationContext()
        {
            var dbOptions = BuildDbContextOptions<UploadApplicationContext>();
            using var context = new UploadApplicationContext(dbOptions);
            CompareContext(context);
        }
    }
}