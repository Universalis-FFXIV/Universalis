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

        private static void CompareContext<T>(T context) where T : DbContext
        {
            context.Database.EnsureDeleted();
            context.Database.EnsureCreated();

            var comparer = new CompareEfSql();

            var hasErrors = comparer.CompareEfWithDb(context);

            hasErrors.Should().BeFalse(comparer.GetAllErrors);
        }

        [Fact]
        public void Compare_AuthenticationInfoContext()
        {
            var dbOptions = new DbContextOptionsBuilder<AuthenticationInfoContext>()
                .UseMySql(TestConnectionString, ServerVersion.AutoDetect(TestConnectionString))
                .Options;

            using var context = new AuthenticationInfoContext(dbOptions);

            CompareContext(context);
        }

        [Fact]
        public void Compare_CurrentlyShownContext()
        {
            var dbOptions = new DbContextOptionsBuilder<CurrentlyShownContext>()
                .UseMySql(TestConnectionString, ServerVersion.AutoDetect(TestConnectionString))
                .Options;

            using var context = new CurrentlyShownContext(dbOptions);

            CompareContext(context);
        }

        [Fact]
        public void Compare_UploadApplicationContext()
        {
            var dbOptions = new DbContextOptionsBuilder<UploadApplicationContext>()
                .UseMySql(TestConnectionString, ServerVersion.AutoDetect(TestConnectionString))
                .Options;

            using var context = new UploadApplicationContext(dbOptions);

            CompareContext(context);
        }
    }
}