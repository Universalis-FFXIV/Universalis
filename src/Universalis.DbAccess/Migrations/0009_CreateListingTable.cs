using FluentMigrator;
using FluentMigrator.Infrastructure;
using FluentMigrator.Postgres;

namespace Universalis.DbAccess.Migrations;

[Migration(9)]
public class CreateListingTable : Migration
{
    public override void Up()
    {
        Create.Table("listing")
            .WithColumn("listing_id").AsString().NotNullable().PrimaryKey()
            .WithColumn("item_id").AsInt32().NotNullable()
            .WithColumn("world_id").AsInt32().NotNullable()
            .WithColumn("live").AsBoolean().NotNullable()
            .WithColumn("hq").AsBoolean().NotNullable()
            .WithColumn("on_mannequin").AsBoolean().NotNullable()
            .WithColumn("materia").AsCustom("jsonb").NotNullable()
            .WithColumn("unit_price").AsInt32().NotNullable()
            .WithColumn("quantity").AsInt32().NotNullable()
            .WithColumn("dye_id").AsInt32().NotNullable()
            .WithColumn("creator_id").AsString().Nullable()
            .WithColumn("creator_name").AsString().Nullable()
            .WithColumn("last_review_time").AsDateTimeOffset().NotNullable()
            .WithColumn("retainer_id").AsString().NotNullable()
            .WithColumn("retainer_name").AsString().NotNullable()
            .WithColumn("retainer_city_id").AsInt32().Nullable()
            .WithColumn("seller_id").AsString().NotNullable();

        Create.Index()
            .OnTable("listing")
            .OnColumn("item_id")
            .Ascending()
            .OnColumn("world_id")
            .Ascending()
            .OnColumn("live")
            .Ascending();
    }

    public override void Down()
    {
        Delete.Table("listing");
    }
}