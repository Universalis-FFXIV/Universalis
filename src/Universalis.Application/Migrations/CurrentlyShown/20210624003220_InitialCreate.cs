using Microsoft.EntityFrameworkCore.Migrations;

namespace Universalis.Application.Migrations.CurrentlyShown
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "CurrentlyShownData",
                columns: table => new
                {
                    ItemId = table.Column<uint>(type: "int unsigned", nullable: false),
                    WorldId = table.Column<uint>(type: "int unsigned", nullable: false),
                    UploadApplication = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    UploaderIdHash = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CurrentlyShownData", x => new { x.ItemId, x.WorldId });
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Listing",
                columns: table => new
                {
                    InternalId = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ListingId = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Hq = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    PricePerUnit = table.Column<uint>(type: "int unsigned", nullable: false),
                    Quantity = table.Column<uint>(type: "int unsigned", nullable: false),
                    Dye = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    IsCrafted = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatorId = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatorName = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    LastReviewTime = table.Column<uint>(type: "int unsigned", nullable: false),
                    RetainerId = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RetainerName = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RetainerCity = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    CurrentlyShownItemId = table.Column<uint>(type: "int unsigned", nullable: true),
                    CurrentlyShownWorldId = table.Column<uint>(type: "int unsigned", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Listing", x => x.InternalId);
                    table.ForeignKey(
                        name: "FK_Listing_CurrentlyShownData_CurrentlyShownItemId_CurrentlySho~",
                        columns: x => new { x.CurrentlyShownItemId, x.CurrentlyShownWorldId },
                        principalTable: "CurrentlyShownData",
                        principalColumns: new[] { "ItemId", "WorldId" },
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Sale",
                columns: table => new
                {
                    InternalId = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Hq = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    PricePerUnit = table.Column<uint>(type: "int unsigned", nullable: false),
                    Quantity = table.Column<uint>(type: "int unsigned", nullable: false),
                    BuyerName = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Timestamp = table.Column<uint>(type: "int unsigned", nullable: false),
                    OnMannequin = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CurrentlyShownItemId = table.Column<uint>(type: "int unsigned", nullable: true),
                    CurrentlyShownWorldId = table.Column<uint>(type: "int unsigned", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sale", x => x.InternalId);
                    table.ForeignKey(
                        name: "FK_Sale_CurrentlyShownData_CurrentlyShownItemId_CurrentlyShownW~",
                        columns: x => new { x.CurrentlyShownItemId, x.CurrentlyShownWorldId },
                        principalTable: "CurrentlyShownData",
                        principalColumns: new[] { "ItemId", "WorldId" },
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Materia",
                columns: table => new
                {
                    InternalId = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SlotId = table.Column<uint>(type: "int unsigned", nullable: false),
                    MateriaId = table.Column<uint>(type: "int unsigned", nullable: false),
                    ListingInternalId = table.Column<string>(type: "varchar(255)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Materia", x => x.InternalId);
                    table.ForeignKey(
                        name: "FK_Materia_Listing_ListingInternalId",
                        column: x => x.ListingInternalId,
                        principalTable: "Listing",
                        principalColumn: "InternalId",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Listing_CurrentlyShownItemId_CurrentlyShownWorldId",
                table: "Listing",
                columns: new[] { "CurrentlyShownItemId", "CurrentlyShownWorldId" });

            migrationBuilder.CreateIndex(
                name: "IX_Materia_ListingInternalId",
                table: "Materia",
                column: "ListingInternalId");

            migrationBuilder.CreateIndex(
                name: "IX_Sale_CurrentlyShownItemId_CurrentlyShownWorldId",
                table: "Sale",
                columns: new[] { "CurrentlyShownItemId", "CurrentlyShownWorldId" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Materia");

            migrationBuilder.DropTable(
                name: "Sale");

            migrationBuilder.DropTable(
                name: "Listing");

            migrationBuilder.DropTable(
                name: "CurrentlyShownData");
        }
    }
}
