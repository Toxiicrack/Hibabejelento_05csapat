using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hibabejelento.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddTicketStatusWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ClosureTypeId",
                table: "TicketNotes",
                type: "INTEGER",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ClosureTypeId",
                table: "TicketNotes");
        }
    }
}
