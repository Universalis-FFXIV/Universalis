using FluentMigrator;

namespace Universalis.DbAccess.Migrations;

[Migration(5)]
public class MakeColumnsNullable : Migration
{
    public override void Up()
    {
        Alter.Column("quantity")
            .OnTable("sale").AsInt32().Nullable();
        Alter.Column("buyer_name")
            .OnTable("sale").AsString().Nullable();
        Alter.Column("uploader_id")
            .OnTable("sale").AsString().Nullable();
        
        Alter.Column("name")
            .OnTable("api_key").AsString().Nullable();
    }

    public override void Down()
    {
        Alter.Column("quantity")
            .OnTable("sale").AsInt32().NotNullable();
        Alter.Column("buyer_name")
            .OnTable("sale").AsString().NotNullable();
        Alter.Column("uploader_id")
            .OnTable("sale").AsString().NotNullable();
        
        Alter.Column("name")
            .OnTable("api_key").AsString().NotNullable();
    }
}