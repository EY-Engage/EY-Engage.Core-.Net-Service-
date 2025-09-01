using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EYEngage.Core.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class addingFonctionAttributeInUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Fonction",
                table: "AspNetUsers",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Fonction",
                table: "AspNetUsers");
        }
    }
}
