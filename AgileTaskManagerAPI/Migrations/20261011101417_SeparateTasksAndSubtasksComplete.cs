using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgileTaskManagerAPI.Migrations
{
    /// <inheritdoc />
    public partial class SeparateTasksAndSubtasksComplete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Chuyển toàn bộ các thẻ con (bao gồm 'taskss 22', 'setting',...) từ bảng Tasks sang bảng Subtasks
            migrationBuilder.Sql(@"
                INSERT INTO ""Subtasks"" (""Title"", ""Description"", ""IsCompleted"", ""OrderIndex"", ""IsActive"", ""DueDate"", ""Tags"", ""ColumnId"", ""ProjectId"", ""AssigneeId"", ""TaskId"")
                SELECT ""TaskName"", ""Description"", false, ""OrderIndex"", ""IsActive"", ""DueDate"", ""Tags"", ""ColumnId"", ""ProjectId"", ""AssigneeId"", ""ColumnId""
                FROM ""Tasks""
                WHERE ""ColumnId"" > 0;
            ");

            // 2. Dọn sạch các thẻ con này khỏi bảng Tasks
            migrationBuilder.Sql(@"
                DELETE FROM ""Tasks"" WHERE ""ColumnId"" > 0;
            ");

            // 3. Đưa toàn bộ các Task lớn (các cột từ KanbanColumns như Calendar, Viec so 2, Fix, Sua chua) vào bảng Tasks
            migrationBuilder.Sql(@"
                INSERT INTO ""Tasks"" (""TaskId"", ""TaskName"", ""Description"", ""OrderIndex"", ""IsActive"", ""DueDate"", ""Tags"", ""ColumnId"", ""ProjectId"")
                SELECT ""ColumnId"", ""ColumnName"", '', ""OrderIndex"", ""IsActive"", ""DueDate"", '', ""ColumnId"", ""ProjectId""
                FROM ""KanbanColumns""
                ON CONFLICT (""TaskId"") DO UPDATE 
                SET ""TaskName"" = EXCLUDED.""TaskName"", ""OrderIndex"" = EXCLUDED.""OrderIndex"", ""DueDate"" = EXCLUDED.""DueDate"";
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
