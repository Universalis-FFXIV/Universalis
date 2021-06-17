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

        [Fact]
        public void Compare_CurrentlyShownContext()
        {
            var dbOptions = new DbContextOptionsBuilder<CurrentlyShownContext>()
                .UseMySql(TestConnectionString, ServerVersion.AutoDetect(TestConnectionString))
                .Options;

            using var context = new CurrentlyShownContext(dbOptions);
            
            context.Database.EnsureDeleted();
            context.Database.EnsureCreated();

            var comparer = new CompareEfSql();

            var hasErrors = comparer.CompareEfWithDb(context);

            hasErrors.Should().BeFalse(comparer.GetAllErrors);
        }
    }
}