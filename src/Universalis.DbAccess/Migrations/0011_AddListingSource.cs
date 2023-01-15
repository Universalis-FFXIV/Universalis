using FluentMigrator;

namespace Universalis.DbAccess.Migrations;

[Migration(11)]
public class AddListingSource : Migration
{
    public override void Up()
    {
        Alter.Table("listing")
            .AddColumn("source").AsString().NotNullable().WithDefaultValue("");
    }

    public override void Down()
    {
        Delete.Column("source").FromTable("listing");
    }
}