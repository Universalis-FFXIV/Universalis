using FluentMigrator;

namespace Universalis.DbAccess.Migrations;

[Migration(2)]
public class CreateSaleTable : Migration
{
    public override void Up()
    {
        Create.Table("sale")
            .WithColumn("id").AsGuid().NotNullable().PrimaryKey()
            .WithColumn("item_id").AsInt32().NotNullable()
            .WithColumn("world_id").AsInt32().NotNullable()
            .WithColumn("hq").AsBoolean().NotNullable()
            .WithColumn("unit_price").AsInt64().NotNullable()
            .WithColumn("quantity").AsInt32()
            .WithColumn("buyer_name").AsString()
            .WithColumn("sale_time").AsDateTimeOffset().NotNullable()
            .WithColumn("uploader_id").AsString();

        Create.ForeignKey()
            .FromTable("sale").ForeignColumns("item_id", "world_id")
            .ToTable("market_item").PrimaryColumns("item_id", "world_id");
    }

    public override void Down()
    {
        Delete.Table("sale");
    }
}