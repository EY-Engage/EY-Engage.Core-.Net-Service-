using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EYEngage.Core.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class clearingRecommanderName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Justification",
                table: "JobApplications",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Score",
                table: "JobApplications",
                type: "float",
                nullable: false,
                defaultValue: 0.0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Justification",
                table: "JobApplications");

            migrationBuilder.DropColumn(
                name: "Score",
                table: "JobApplications");
        }
    }
}
