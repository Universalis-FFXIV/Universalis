using FluentMigrator;

namespace Universalis.DbAccess.Migrations;

[Migration(8)]
public class AddMannequinToSales : Migration
{
    public override void Up()
    {
        Alter.Table("sale")
            .AddColumn("mannequin").AsBoolean().Nullable();
    }

    public override void Down()
    {
        Delete.Column("mannequin")
            .FromTable("sale");
    }
}