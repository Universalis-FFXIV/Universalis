using EfSchemaCompare;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Pomelo.EntityFrameworkCore.MySql.Design.Internal;
using Universalis.Application.DbAccess;
using Xunit;
using Xunit.Abstractions;

namespace Universalis.Application.Tests
{
    public class DbTests
    {
        private const string TestConnectionString = "Server=localhost;Database=universalis_test;User=dalamud;Password=dalamud";

        private readonly ITestOutputHelper _output;

        public DbTests(ITestOutputHelper output)
        {
            _output = output;
        }

        private DbContextOptions<T> BuildDbContextOptions<T>() where T : DbContext
        {
            return new DbContextOptionsBuilder<T>()
                .UseMySql(TestConnectionString, ServerVersion.AutoDetect(TestConnectionString))
                .LogTo(_output.WriteLine)
                .Options;
        }

        private static void CompareContext<T>(T context) where T : DbContext
        {
            context.Database.EnsureDeleted();
            context.Database.EnsureCreated();

            var comparer = new CompareEfSql();

#pragma warning disable EF1001 // Internal EF Core API usage.
            var hasErrors = comparer.CompareEfWithDb<MySqlDesignTimeServices>(context);
#pragma warning restore EF1001 // Internal EF Core API usage.

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