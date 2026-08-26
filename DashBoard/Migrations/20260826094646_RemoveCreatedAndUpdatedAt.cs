using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DashBoard.Migrations
{
    /// <inheritdoc />
    public partial class RemoveCreatedAndUpdatedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Tickets");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Tickets",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Tickets",
                type: "datetime2",
                nullable: true);
        }
    }
}
