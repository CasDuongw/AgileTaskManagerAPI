using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgileTaskManagerAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddDueDateAndStatusToKanbanColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DueDate",
                table: "KanbanColumns",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "KanbanColumns",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DueDate",
                table: "KanbanColumns");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "KanbanColumns");
        }
    }
}
