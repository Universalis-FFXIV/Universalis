using Universalis.DbAccess.Tests;
using Xunit;

namespace Universalis.Application.Tests;

[CollectionDefinition("Database collection")]
public class DatabaseCollection : ICollectionFixture<DbFixture>
{
}
