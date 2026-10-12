using AgileTaskManagerAPI.Data;
using AgileTaskManagerAPI.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AgileTaskManagerAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Microsoft.AspNetCore.Authorization.Authorize]
    public class ColumnsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ColumnsController(AppDbContext context)
        {
            _context = context;
        }

        private int GetCurrentUserId()
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
            return int.TryParse(userIdStr, out int userId) ? userId : 0;
        }

        [HttpGet("project/{projectId}")]
        public async Task<ActionResult<IEnumerable<KanbanColumn>>> GetColumnsByProject(int projectId)
        {
            var project = await _context.Projects.FindAsync(projectId);
            if (project == null) return NotFound();

            var columns = await _context.KanbanColumns
                .Where(c => c.ProjectId == projectId && c.IsActive)
                .OrderBy(c => c.OrderIndex)
                .ThenBy(c => c.ColumnId)
                .ToListAsync();

            // Tự động chuẩn hóa (Auto-heal) nếu phát hiện OrderIndex bị trùng hoặc cột tạo sau bị đè OrderIndex = 0
            bool hasDuplicates = columns.GroupBy(c => c.OrderIndex).Any(g => g.Count() > 1);
            bool hasInvertedZero = columns.Any(c => c.OrderIndex == 0 && columns.Any(o => o.ColumnId < c.ColumnId && o.OrderIndex > 0));

            if (hasDuplicates || hasInvertedZero)
            {
                // Xếp lại tuần tự theo đúng thời gian tạo (ColumnId tăng dần)
                var sorted = columns.OrderBy(c => c.ColumnId).ToList();
                for (int i = 0; i < sorted.Count; i++)
                {
                    sorted[i].OrderIndex = i;
                    var mainTask = await _context.Tasks.FirstOrDefaultAsync(t => t.ColumnId == sorted[i].ColumnId);
                    if (mainTask != null)
                    {
                        mainTask.OrderIndex = i;
                    }
                }
                await _context.SaveChangesAsync();
                columns = sorted;
            }

            foreach (var col in columns)
            {
                if (col.DueDate.HasValue)
                {
                    col.DueDate = DateTime.SpecifyKind(col.DueDate.Value, DateTimeKind.Utc);
                }
            }

            return columns.OrderBy(c => c.OrderIndex).ThenBy(c => c.ColumnId).ToList();
        }

        [HttpPost]
        public async Task<ActionResult<KanbanColumn>> CreateColumn(KanbanColumn column)
        {
            var project = await _context.Projects.FindAsync(column.ProjectId);
            if (project == null) return NotFound();

            var maxOrder = await _context.KanbanColumns
                .Where(c => c.ProjectId == column.ProjectId && c.IsActive)
                .MaxAsync(c => (int?)c.OrderIndex) ?? -1;
            
            column.OrderIndex = maxOrder + 1;
            column.IsActive = true;
            if (string.IsNullOrWhiteSpace(column.Status))
            {
                column.Status = "Vừa giao";
            }
            if (column.DueDate.HasValue)
            {
                column.DueDate = DateTime.SpecifyKind(column.DueDate.Value, DateTimeKind.Utc);
            }

            _context.KanbanColumns.Add(column);
            await _context.SaveChangesAsync();

            // Đồng bộ Task lớn vào bảng Tasks (không gán TaskId để database tự sinh Identity)
            var mainTask = new AppTask
            {
                TaskName = column.ColumnName,
                ProjectId = column.ProjectId,
                ColumnId = column.ColumnId,
                OrderIndex = column.OrderIndex,
                IsActive = true,
                DueDate = column.DueDate
            };
            _context.Tasks.Add(mainTask);
            await _context.SaveChangesAsync();

            return Ok(column);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateColumn(int id, KanbanColumn column)
        {
            if (id != column.ColumnId) return BadRequest();

            var existing = await _context.KanbanColumns.Include(c => c.Project).FirstOrDefaultAsync(c => c.ColumnId == id);
            if (existing == null) return NotFound();

            existing.ColumnName = column.ColumnName;
            if (!string.IsNullOrWhiteSpace(column.Status))
            {
                existing.Status = column.Status;
            }
            existing.DueDate = column.DueDate.HasValue ? DateTime.SpecifyKind(column.DueDate.Value, DateTimeKind.Utc) : null;
            
            // Bảo vệ OrderIndex: Tránh trường hợp client gửi OrderIndex = 0 đè lên OrderIndex hợp lệ đã có
            if (column.OrderIndex > 0 || existing.OrderIndex == 0)
            {
                existing.OrderIndex = column.OrderIndex;
            }

            var mainTask = await _context.Tasks.FirstOrDefaultAsync(t => t.ColumnId == id);
            if (mainTask != null)
            {
                mainTask.TaskName = column.ColumnName;
                mainTask.DueDate = existing.DueDate;
                mainTask.OrderIndex = existing.OrderIndex;
            }

            await _context.SaveChangesAsync();
            return Ok(existing);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteColumn(int id)
        {
            var col = await _context.KanbanColumns.FindAsync(id);
            if (col == null) return NotFound();

            col.IsActive = false;
            var mainTask = await _context.Tasks.FirstOrDefaultAsync(t => t.ColumnId == id);
            if (mainTask != null) mainTask.IsActive = false;
            await _context.SaveChangesAsync();
            return Ok();
        }

        public class ReorderRequest
        {
            public int ColumnId { get; set; }
            public int OrderIndex { get; set; }
        }

        [HttpPut("reorder")]
        public async Task<IActionResult> ReorderColumns([FromBody] List<ReorderRequest> reorders)
        {
            if (reorders == null || !reorders.Any())
                return BadRequest("Khong co du lieu");

            foreach (var req in reorders)
            {
                var col = await _context.KanbanColumns.FindAsync(req.ColumnId);
                if (col != null)
                {
                    col.OrderIndex = req.OrderIndex;
                    var mainTask = await _context.Tasks.FirstOrDefaultAsync(t => t.ColumnId == col.ColumnId);
                    if (mainTask != null)
                    {
                        mainTask.OrderIndex = req.OrderIndex;
                    }
                }
            }
            
            await _context.SaveChangesAsync();
            return Ok(new { message = "Cap nhat thanh cong" });
        }
    }
}

