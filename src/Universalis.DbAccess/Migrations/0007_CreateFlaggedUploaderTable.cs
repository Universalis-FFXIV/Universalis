using FluentMigrator;

namespace Universalis.DbAccess.Migrations;

[Migration(7)]
public class CreateFlaggedUploaderTable : Migration
{
    public override void Up()
    {
        Create.Table("flagged_uploader")
            .WithColumn("id_sha256").AsString().NotNullable().PrimaryKey();
    }

    public override void Down()
    {
        Delete.Table("flagged_uploader");
    }
}
