using EfSchemaCompare;
using FluentAssertions;
using Universalis.Application.DbAccess;
using Xunit;

namespace Universalis.Application.Tests
{
    public class DbTests
    {
        [Fact]
        public void Compare_CurrentlyShownContext()
        {
            using var context = new CurrentlyShownContext();
            
            context.Database.EnsureDeleted();
            context.Database.EnsureCreated();

            var comparer = new CompareEfSql();

            var hasErrors = comparer.CompareEfWithDb(context);

            hasErrors.Should().BeFalse(comparer.GetAllErrors);
        }
    }
}