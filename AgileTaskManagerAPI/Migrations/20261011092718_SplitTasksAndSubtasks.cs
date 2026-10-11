using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgileTaskManagerAPI.Migrations
{
    /// <inheritdoc />
    public partial class SplitTasksAndSubtasks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Subtasks_Tasks_TaskId",
                table: "Subtasks");

            migrationBuilder.AlterColumn<DateTime>(
                name: "DueDate",
                table: "Tasks",
                type: "timestamp without time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "TaskId",
                table: "Subtasks",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<int>(
                name: "AssigneeId",
                table: "Subtasks",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ColumnId",
                table: "Subtasks",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Subtasks",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "DueDate",
                table: "Subtasks",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Subtasks",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "OrderIndex",
                table: "Subtasks",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ProjectId",
                table: "Subtasks",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Tags",
                table: "Subtasks",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Projects",
                type: "timestamp without time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "DueDate",
                table: "KanbanColumns",
                type: "timestamp without time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Subtasks_AssigneeId",
                table: "Subtasks",
                column: "AssigneeId");

            migrationBuilder.CreateIndex(
                name: "IX_Subtasks_ColumnId",
                table: "Subtasks",
                column: "ColumnId");

            migrationBuilder.CreateIndex(
                name: "IX_Subtasks_ProjectId",
                table: "Subtasks",
                column: "ProjectId");

            migrationBuilder.AddForeignKey(
                name: "FK_Subtasks_KanbanColumns_ColumnId",
                table: "Subtasks",
                column: "ColumnId",
                principalTable: "KanbanColumns",
                principalColumn: "ColumnId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Subtasks_Projects_ProjectId",
                table: "Subtasks",
                column: "ProjectId",
                principalTable: "Projects",
                principalColumn: "ProjectId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Subtasks_Tasks_TaskId",
                table: "Subtasks",
                column: "TaskId",
                principalTable: "Tasks",
                principalColumn: "TaskId");

            migrationBuilder.AddForeignKey(
                name: "FK_Subtasks_Users_AssigneeId",
                table: "Subtasks",
                column: "AssigneeId",
                principalTable: "Users",
                principalColumn: "UserId");

            // Tự động sao chép toàn bộ dữ liệu từ Tasks cũ sang Subtasks để chia rành mạch Task lớn và Subtask
            migrationBuilder.Sql(@"
                INSERT INTO ""Subtasks"" (""Title"", ""Description"", ""IsCompleted"", ""OrderIndex"", ""IsActive"", ""DueDate"", ""Tags"", ""ColumnId"", ""ProjectId"", ""AssigneeId"")
                SELECT ""TaskName"", ""Description"", false, ""OrderIndex"", ""IsActive"", ""DueDate"", ""Tags"", ""ColumnId"", ""ProjectId"", ""AssigneeId""
                FROM ""Tasks""
                WHERE ""ColumnId"" IN (SELECT ""ColumnId"" FROM ""KanbanColumns"");
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Subtasks_KanbanColumns_ColumnId",
                table: "Subtasks");

            migrationBuilder.DropForeignKey(
                name: "FK_Subtasks_Projects_ProjectId",
                table: "Subtasks");

            migrationBuilder.DropForeignKey(
                name: "FK_Subtasks_Tasks_TaskId",
                table: "Subtasks");

            migrationBuilder.DropForeignKey(
                name: "FK_Subtasks_Users_AssigneeId",
                table: "Subtasks");

            migrationBuilder.DropIndex(
                name: "IX_Subtasks_AssigneeId",
                table: "Subtasks");

            migrationBuilder.DropIndex(
                name: "IX_Subtasks_ColumnId",
                table: "Subtasks");

            migrationBuilder.DropIndex(
                name: "IX_Subtasks_ProjectId",
                table: "Subtasks");

            migrationBuilder.DropColumn(
                name: "AssigneeId",
                table: "Subtasks");

            migrationBuilder.DropColumn(
                name: "ColumnId",
                table: "Subtasks");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Subtasks");

            migrationBuilder.DropColumn(
                name: "DueDate",
                table: "Subtasks");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Subtasks");

            migrationBuilder.DropColumn(
                name: "OrderIndex",
                table: "Subtasks");

            migrationBuilder.DropColumn(
                name: "ProjectId",
                table: "Subtasks");

            migrationBuilder.DropColumn(
                name: "Tags",
                table: "Subtasks");

            migrationBuilder.AlterColumn<DateTime>(
                name: "DueDate",
                table: "Tasks",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "TaskId",
                table: "Subtasks",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Projects",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "DueDate",
                table: "KanbanColumns",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Subtasks_Tasks_TaskId",
                table: "Subtasks",
                column: "TaskId",
                principalTable: "Tasks",
                principalColumn: "TaskId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
