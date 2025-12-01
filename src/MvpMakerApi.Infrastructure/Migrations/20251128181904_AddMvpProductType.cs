using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MvpMakerApi.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMvpProductType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GitHubRepoUrl",
                table: "MVPs");

            migrationBuilder.AddColumn<string>(
                name: "Link",
                table: "MVPs",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "PreviewLink",
                table: "MVPs",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "ProductType",
                table: "MVPs",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Link",
                table: "MVPs");

            migrationBuilder.DropColumn(
                name: "PreviewLink",
                table: "MVPs");

            migrationBuilder.DropColumn(
                name: "ProductType",
                table: "MVPs");

            migrationBuilder.AddColumn<string>(
                name: "GitHubRepoUrl",
                table: "MVPs",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }
    }
}
