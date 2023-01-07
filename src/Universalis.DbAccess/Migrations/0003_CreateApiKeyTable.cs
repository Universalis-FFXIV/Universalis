using FluentMigrator;

namespace Universalis.DbAccess.Migrations;

[Migration(3)]
public class CreateApiKeyTable : Migration
{
    public override void Up()
    {
        Create.Table("api_key")
            .WithColumn("token_sha512").AsString().NotNullable().PrimaryKey()
            .WithColumn("can_upload").AsBoolean().NotNullable();
    }

    public override void Down()
    {
        Delete.Table("api_key");
    }
}