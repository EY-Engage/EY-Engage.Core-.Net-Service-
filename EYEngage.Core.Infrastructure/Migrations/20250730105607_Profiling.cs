using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EYEngage.Core.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Profiling : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ApprovedById",
                table: "Events",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ApprovedById",
                table: "EventParticipations",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Events_ApprovedById",
                table: "Events",
                column: "ApprovedById");

            migrationBuilder.CreateIndex(
                name: "IX_EventParticipations_ApprovedById",
                table: "EventParticipations",
                column: "ApprovedById");

            migrationBuilder.AddForeignKey(
                name: "FK_EventParticipations_AspNetUsers_ApprovedById",
                table: "EventParticipations",
                column: "ApprovedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Events_AspNetUsers_ApprovedById",
                table: "Events",
                column: "ApprovedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EventParticipations_AspNetUsers_ApprovedById",
                table: "EventParticipations");

            migrationBuilder.DropForeignKey(
                name: "FK_Events_AspNetUsers_ApprovedById",
                table: "Events");

            migrationBuilder.DropIndex(
                name: "IX_Events_ApprovedById",
                table: "Events");

            migrationBuilder.DropIndex(
                name: "IX_EventParticipations_ApprovedById",
                table: "EventParticipations");

            migrationBuilder.DropColumn(
                name: "ApprovedById",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "ApprovedById",
                table: "EventParticipations");
        }
    }
}
