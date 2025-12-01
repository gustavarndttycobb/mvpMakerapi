using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MvpMakerApi.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddGitHubBusinessType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "GitHubBusinessType",
                table: "MVPs",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GitHubBusinessType",
                table: "MVPs");
        }
    }
}
