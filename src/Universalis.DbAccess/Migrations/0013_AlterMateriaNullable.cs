using FluentMigrator;
using Newtonsoft.Json.Linq;

namespace Universalis.DbAccess.Migrations;

[Migration(13)]
public class AlterMateriaNullable : Migration
{
    public override void Up()
    {
        Alter.Table("listing")
            .AlterColumn("materia").AsCustom("jsonb").Nullable();
    }

    public override void Down()
    {
        Alter.Table("listing")
            .AlterColumn("materia").AsCustom("jsonb").NotNullable().WithDefaultValue(new JArray());
    }
}