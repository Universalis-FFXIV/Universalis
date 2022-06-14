using FluentMigrator;

namespace Universalis.DbAccess.Migrations;

[Migration(6)]
public class CreateCharacterTable : Migration
{
    public override void Up()
    {
        Create.Table("character")
            .WithColumn("content_id_sha256").AsString().NotNullable().PrimaryKey()
            .WithColumn("name").AsString().NotNullable()
            .WithColumn("world_id").AsInt32().Nullable();
    }

    public override void Down()
    {
        Delete.Table("character");
    }
}