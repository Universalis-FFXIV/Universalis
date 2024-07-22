using FluentMigrator;

namespace Universalis.DbAccess.Migrations;

[Migration(14)]
public class DeleteIdentifyingColumns : Migration
{
    public override void Up()
    {
        Delete.Column("creator_id").FromTable("listing");
        Delete.Column("seller_id").FromTable("listing");
    }

    public override void Down()
    {
        Alter.Table("listing")
            .AddColumn("creator_id").AsString().Nullable()
            .AddColumn("seller_id").AsString().NotNullable().WithDefaultValue("");
    }
}