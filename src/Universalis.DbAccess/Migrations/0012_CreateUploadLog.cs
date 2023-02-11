using FluentMigrator;

namespace Universalis.DbAccess.Migrations;

[Migration(12)]
public class CreateUploadLog : Migration
{
    public override void Up()
    {
        Create.Table("upload_log")
            .WithColumn("id").AsString().NotNullable().PrimaryKey()
            .WithColumn("timestamp").AsDateTimeOffset().NotNullable()
            .WithColumn("event").AsString().NotNullable()
            .WithColumn("application").AsString().NotNullable()
            .WithColumn("world_id").AsInt32().NotNullable()
            .WithColumn("item_id").AsInt32().NotNullable()
            .WithColumn("listings").AsInt32().NotNullable()
            .WithColumn("sales").AsInt32().NotNullable();
    }

    public override void Down()
    {
        Delete.Table("upload_log");
    }
}