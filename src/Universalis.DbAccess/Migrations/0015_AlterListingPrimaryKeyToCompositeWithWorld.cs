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
[Migration(15)]
public class AlterListingPrimaryKeyToCompositeWithWorld : Migration
{
    public override void Up()
    {
        // The PK rebuild takes ACCESS EXCLUSIVE on `listing` for the duration of
        // the new btree build.
        Delete.PrimaryKey("PK_listing").FromTable("listing");
        Create.PrimaryKey("PK_listing")
            .OnTable("listing")
            .Columns("listing_id", "world_id");
    }

    public override void Down()
    {
        // Note: this rollback can fail once cross-world duplicate listing_ids have
        // been written, since they would violate the listing_id-only uniqueness
        // constraint.
        Delete.PrimaryKey("PK_listing").FromTable("listing");
        Create.PrimaryKey("PK_listing")
            .OnTable("listing")
            .Columns("listing_id");
    }
}
