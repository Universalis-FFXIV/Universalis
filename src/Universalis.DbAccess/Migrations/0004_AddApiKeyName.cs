using FluentMigrator;

namespace Universalis.DbAccess.Migrations;

[Migration(4)]
public class AddApiKeyName : Migration
{
    public override void Up()
    {
        Alter.Table("api_key")
            .AddColumn("name").AsString();
    }

    public override void Down()
    {
        Delete.Column("name")
            .FromTable("api_key");
    }
}