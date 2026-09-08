using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgileTaskManagerAPI.Migrations
{
    public partial class AddIsActiveAndOrder : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Tasks",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<int>(
                name: "OrderIndex",
                table: "Tasks",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "KanbanColumns",
                type: "bit",
                nullable: false,
                defaultValue: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "OrderIndex",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "KanbanColumns");
        }
    }
}
