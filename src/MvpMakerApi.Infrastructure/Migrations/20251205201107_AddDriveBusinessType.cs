using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MvpMakerApi.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDriveBusinessType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DriveBusinessType",
                table: "MVPs",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DriveBusinessType",
                table: "MVPs");
        }
    }
}
