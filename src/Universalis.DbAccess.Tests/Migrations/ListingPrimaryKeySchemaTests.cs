using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Xunit;

namespace Universalis.DbAccess.Tests.Migrations;

// Empirical assertions on the post-migration listing schema. The DbFixture runs
// every migration on container startup, so by the time these tests run the
// listing table reflects whatever state migration 15 left behind. Locks in the
// claim that the CONCURRENTLY-built index swap produces the same end state as
// a vanilla `Create.PrimaryKey` would: same constraint name, same backing
// index name, no leftover scratch index.
[Collection("Database collection")]
public class ListingPrimaryKeySchemaTests
{
    private readonly DbFixture _fixture;

    public ListingPrimaryKeySchemaTests(DbFixture fixture)
    {
        _fixture = fixture;
    }

#if DEBUG
    [Fact]
#endif
    public async Task ListingPrimaryKey_IsCompositeOnListingIdAndWorldId()
    {
        var dataSource = _fixture.Services.GetRequiredService<NpgsqlDataSource>();

        await using var cmd = dataSource.CreateCommand(@"
            SELECT a.attname FROM pg_constraint c
            JOIN pg_attribute a ON a.attrelid = c.conrelid AND a.attnum = ANY(c.conkey)
            WHERE c.conname = 'PK_listing' AND c.contype = 'p'
            ORDER BY array_position(c.conkey, a.attnum)
        ");
        await using var reader = await cmd.ExecuteReaderAsync();
        var columns = new List<string>();
        while (await reader.ReadAsync())
        {
            columns.Add(reader.GetString(0));
        }

        Assert.Equal(new[] { "listing_id", "world_id" }, columns);
    }

#if DEBUG
    [Fact]
#endif
    public async Task ListingPrimaryKey_BackingIndexIsNamedToMatchConstraint()
    {
        // `ADD CONSTRAINT ... PRIMARY KEY USING INDEX <existing>` renames the
        // adopted index to match the constraint name. After migration 15 the
        // backing index for PK_listing should therefore be named PK_listing —
        // identical to what a synchronous `Create.PrimaryKey` would produce.
        var dataSource = _fixture.Services.GetRequiredService<NpgsqlDataSource>();

        await using var cmd = dataSource.CreateCommand(@"
            SELECT i.relname FROM pg_constraint c
            JOIN pg_class i ON i.oid = c.conindid
            WHERE c.conname = 'PK_listing' AND c.contype = 'p'
        ");
        var indexName = (string)(await cmd.ExecuteScalarAsync())!;

        Assert.Equal("PK_listing", indexName);
    }

#if DEBUG
    [Fact]
#endif
    public async Task ListingMigration_LeavesNoScratchIndexBehind()
    {
        // The CONCURRENTLY build uses `listing_pkey_new` as a build-time scratch
        // name. The swap renames it to PK_listing, so on the happy path no index
        // by that name should remain. This guards against a regression where a
        // future change forgets the rename or leaves an orphan after a partial
        // run.
        var dataSource = _fixture.Services.GetRequiredService<NpgsqlDataSource>();

        await using var cmd = dataSource.CreateCommand(
            "SELECT count(*) FROM pg_class WHERE relname = 'listing_pkey_new' AND relkind = 'i'");
        var count = (long)(await cmd.ExecuteScalarAsync())!;

        Assert.Equal(0L, count);
    }
}
