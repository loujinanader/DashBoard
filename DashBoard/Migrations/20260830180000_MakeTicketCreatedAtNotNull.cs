using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DashBoard.Migrations
{
    /// <inheritdoc />
    public partial class MakeTicketCreatedAtNotNull : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Tickets",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Tickets",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");
        }
    }
}
