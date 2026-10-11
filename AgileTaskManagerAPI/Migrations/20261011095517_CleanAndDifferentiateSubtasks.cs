using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgileTaskManagerAPI.Migrations
{
    /// <inheritdoc />
    public partial class CleanAndDifferentiateSubtasks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
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

            // Dọn sạch toàn bộ các bản ghi bị sao chép nhầm vào Subtasks (do có TaskId IS NULL)
            migrationBuilder.Sql(@"DELETE FROM ""Subtasks"" WHERE ""TaskId"" IS NULL;");

            migrationBuilder.AlterColumn<int>(
                name: "TaskId",
                table: "Subtasks",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "ProjectId",
                table: "Subtasks",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "ColumnId",
                table: "Subtasks",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddForeignKey(
                name: "FK_Subtasks_KanbanColumns_ColumnId",
                table: "Subtasks",
                column: "ColumnId",
                principalTable: "KanbanColumns",
                principalColumn: "ColumnId");

            migrationBuilder.AddForeignKey(
                name: "FK_Subtasks_Projects_ProjectId",
                table: "Subtasks",
                column: "ProjectId",
                principalTable: "Projects",
                principalColumn: "ProjectId");

            migrationBuilder.AddForeignKey(
                name: "FK_Subtasks_Tasks_TaskId",
                table: "Subtasks",
                column: "TaskId",
                principalTable: "Tasks",
                principalColumn: "TaskId",
                onDelete: ReferentialAction.Cascade);
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

            migrationBuilder.AlterColumn<int>(
                name: "TaskId",
                table: "Subtasks",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "ProjectId",
                table: "Subtasks",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "ColumnId",
                table: "Subtasks",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

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
        }
    }
}
