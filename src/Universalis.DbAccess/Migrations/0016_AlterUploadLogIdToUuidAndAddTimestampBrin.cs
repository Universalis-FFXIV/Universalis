using FluentMigrator;

namespace Universalis.DbAccess.Migrations;

// The upload_log id was originally declared as varchar in migration 0012. With
// the audit-log feature re-enabled and inserting random Guids per row, a varchar
// PK produces poor B-tree cache locality - inserts scatter writes across the
// index and per-row comparisons fall back to string collation. Convert id to
// the native uuid type so comparisons are 16-byte binary, and add a BRIN index
// on timestamp because BRIN is well-suited for append-only time-ordered data
// and matches the natural audit-log query shape (time-range scans).
//
// The table has been quiescent since the feature was disabled in PR #1302
// (July 2024), so a simple ACCESS EXCLUSIVE rewrite is acceptable here without
// the online-rebuild dance used in migration 0015.
[Migration(16)]
public class AlterUploadLogIdToUuidAndAddTimestampBrin : Migration
{
    public override void Up()
    {
        Execute.Sql("ALTER TABLE upload_log ALTER COLUMN id TYPE uuid USING id::uuid");
        Execute.Sql("CREATE INDEX IF NOT EXISTS idx_upload_log_timestamp_brin ON upload_log USING BRIN (timestamp)");
    }

    public override void Down()
    {
        Execute.Sql("DROP INDEX IF EXISTS idx_upload_log_timestamp_brin");
        Execute.Sql("ALTER TABLE upload_log ALTER COLUMN id TYPE varchar USING id::text");
    }
}
