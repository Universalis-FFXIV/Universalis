using Microsoft.EntityFrameworkCore.Migrations;

namespace Universalis.Application.Migrations.AuthenticationInfo
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "RegisteredClients",
                columns: table => new
                {
                    UploadApplication = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    ApiKeySha256 = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegisteredClients", x => x.UploadApplication);
                })
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RegisteredClients");
        }
    }
}
