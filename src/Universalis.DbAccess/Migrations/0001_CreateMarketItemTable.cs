using FluentMigrator;

namespace Universalis.DbAccess.Migrations;

[Migration(1)]
public class CreateMarketItemTable : Migration
{
    public override void Up()
    {
        Create.Table("market_item")
            .WithColumn("item_id").AsInt32().NotNullable()
            .WithColumn("world_id").AsInt32().NotNullable()
            .WithColumn("updated").AsDateTimeOffset().NotNullable();
        
        Create.PrimaryKey().OnTable("market_item").Columns("item_id", "world_id");
    }

    public override void Down()
    {
        Delete.Table("market_item");
    }
}
