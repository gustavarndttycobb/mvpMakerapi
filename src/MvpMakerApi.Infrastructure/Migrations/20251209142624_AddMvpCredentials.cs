using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MvpMakerApi.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMvpCredentials : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "GitHubPatToken",
                table: "MVPs",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "GoogleOAuthToken",
                table: "MVPs",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GitHubPatToken",
                table: "MVPs");

            migrationBuilder.DropColumn(
                name: "GoogleOAuthToken",
                table: "MVPs");
        }
    }
}
