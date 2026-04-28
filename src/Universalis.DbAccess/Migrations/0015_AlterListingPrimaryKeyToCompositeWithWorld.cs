using System;
using System.Data;
using FluentMigrator;

namespace Universalis.DbAccess.Migrations;

// Switch the listing primary key from (listing_id) to (listing_id, world_id).
//
// We observed in production that the same `listing_id` value can appear in uploads
// from different worlds, attached to different retainers. Combined with a sole PK
// on `listing_id` and `ON CONFLICT (listing_id) DO NOTHING` in ReplaceLive, this
// meant that whichever world wrote the row first kept it, and subsequent uploads
// for the same id from any other world were silently discarded. Including
// `world_id` in the PK lets such rows coexist.
//
// On 11.5M rows under live write traffic, the synchronous form of this rebuild
// (`ALTER TABLE ADD CONSTRAINT ... PRIMARY KEY (...)`) takes ACCESS EXCLUSIVE for
// the duration of the new btree build, which exceeded Npgsql's command timeout
// during a prior deploy attempt. Build the index online via CREATE INDEX
// CONCURRENTLY and adopt it as the PK in a fast catalog swap, so the rebuild
// runs without blocking listing reads or writes.
//
// Multi-replica safety: Docker Swarm starts every replica in parallel, and each
// replica's FluentMigrator runner independently sees v15 as unapplied. A session
// advisory lock serializes them so only one replica does the work; later replicas
// acquire the lock after the first releases, re-check whether a peer beat them
// to the migration, and short-circuit. The post-migration state is identical to
// what `Create.PrimaryKey("PK_listing").Columns("listing_id", "world_id")` would
// produce: per Postgres docs, `ADD CONSTRAINT ... PRIMARY KEY USING INDEX <idx>`
// renames the underlying index to match the constraint name.
[Migration(15, TransactionBehavior.None)]
public class AlterListingPrimaryKeyToCompositeWithWorld : Migration
{
    private const string AdvisoryLockKey = "hashtext('listing_pkey_migration_15')";

    // FluentMigrator's WithGlobalCommandTimeout setting only propagates to
    // commands the runner creates (Execute.Sql, Schema.*, etc.). The commands
    // we issue inside Execute.WithConnection are raw ADO.NET commands and
    // default to Npgsql's 30s timeout, which is too short for both the
    // CONCURRENTLY index build and for waiting on the peer-held advisory lock.
    // Set this explicitly on every command we create in the migration body.
    private const int CommandTimeoutSeconds = 30 * 60;

    public override void Up()
    {
        Execute.WithConnection((conn, _) => RunMigration(conn, fromColumnCount: 1, () =>
        {
            // Clean any partial orphan from a previous failed attempt before
            // rebuilding. After a successful run `listing_pkey_new` no longer
            // exists in the catalog (it gets renamed to PK_listing as part of
            // the swap below), so this is a no-op on the happy path.
            Exec(conn, "DROP INDEX IF EXISTS listing_pkey_new");

            // Build the replacement index online. Cannot run inside a
            // transaction, which is why this migration uses
            // TransactionBehavior.None.
            Exec(conn, "CREATE UNIQUE INDEX CONCURRENTLY listing_pkey_new ON listing (listing_id, world_id)");

            // Adopt the new index as the primary key in a single catalog
            // operation. Postgres renames listing_pkey_new to PK_listing here.
            Exec(conn, @"
                ALTER TABLE listing
                    DROP CONSTRAINT ""PK_listing"",
                    ADD CONSTRAINT ""PK_listing"" PRIMARY KEY USING INDEX listing_pkey_new");
        }));
    }

    public override void Down()
    {
        // Best-effort rollback for dev/test only. If any cross-world duplicate
        // listing_ids have been written since the Up ran, the unique-index build
        // for the single-column form will fail.
        Execute.WithConnection((conn, _) => RunMigration(conn, fromColumnCount: 2, () =>
        {
            Exec(conn, "DROP INDEX IF EXISTS listing_pkey_old");
            Exec(conn, "CREATE UNIQUE INDEX CONCURRENTLY listing_pkey_old ON listing (listing_id)");
            Exec(conn, @"
                ALTER TABLE listing
                    DROP CONSTRAINT ""PK_listing"",
                    ADD CONSTRAINT ""PK_listing"" PRIMARY KEY USING INDEX listing_pkey_old");
        }));
    }

    // Acquire the session advisory lock, verify the migration still needs to
    // run (a peer may have completed it while we were waiting on the lock),
    // and execute the body. The lock is released automatically when the
    // FluentMigrator connection closes; Npgsql's DISCARD ALL on pool return
    // covers connection reuse.
    private static void RunMigration(IDbConnection conn, int fromColumnCount, Action body)
    {
        using (var lockCmd = conn.CreateCommand())
        {
            lockCmd.CommandText = $"SELECT pg_advisory_lock({AdvisoryLockKey})";
            lockCmd.CommandTimeout = CommandTimeoutSeconds;
            lockCmd.ExecuteNonQuery();
        }

        using (var checkCmd = conn.CreateCommand())
        {
            checkCmd.CommandText = @"
                SELECT count(*) FROM information_schema.key_column_usage
                WHERE table_name = 'listing' AND constraint_name = 'PK_listing'";
            checkCmd.CommandTimeout = CommandTimeoutSeconds;
            var currentColumns = Convert.ToInt32(checkCmd.ExecuteScalar());
            if (currentColumns != fromColumnCount)
            {
                // Either a peer already migrated us, or we're not in the
                // expected starting state. Either way, nothing safe to do.
                return;
            }
        }

        body();
    }

    private static void Exec(IDbConnection conn, string sql)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.CommandTimeout = CommandTimeoutSeconds;
        cmd.ExecuteNonQuery();
    }
}
