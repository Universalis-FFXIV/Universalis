using FluentMigrator;

namespace Universalis.DbAccess.Migrations;

[Migration(10)]
public class AddListingUploadTime : Migration
{
    public override void Up()
    {
        Delete.Index()
            .OnTable("listing")
            .OnColumns("item_id", "world_id", "live");

        Delete.Column("live").FromTable("listing");

        Alter.Table("listing")
            .AddColumn("uploaded_at").AsDateTimeOffset().WithDefault(SystemMethods.CurrentUTCDateTime);

        Create.Index()
            .OnTable("listing")
            .OnColumn("item_id")
            .Ascending()
            .OnColumn("world_id")
            .Ascending()
            .OnColumn("uploaded_at")
            .Ascending();
    }

    public override void Down()
    {
        Delete.Index().OnTable("listing").OnColumns("item_id", "world_id", "uploaded_at");

        Delete.Column("uploaded_at").FromTable("listing");

        Alter.Table("listing")
            .AddColumn("live").AsBoolean().WithDefaultValue(true);

        Create.Index()
            .OnTable("listing")
            .OnColumn("item_id")
            .Ascending()
            .OnColumn("world_id")
            .Ascending()
            .OnColumn("live")
            .Ascending();
    }
}