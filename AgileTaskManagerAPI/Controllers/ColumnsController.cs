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

            return await _context.KanbanColumns
                .AsNoTracking()
                .Where(c => c.ProjectId == projectId && c.IsActive)
                .OrderBy(c => c.OrderIndex)
                .ToListAsync();
        }

        [HttpPost]
        public async Task<ActionResult<KanbanColumn>> CreateColumn(KanbanColumn column)
        {
            var project = await _context.Projects.FindAsync(column.ProjectId);
            if (project == null) return NotFound();

            var maxOrder = await _context.KanbanColumns
                .Where(c => c.ProjectId == column.ProjectId)
                .MaxAsync(c => (int?)c.OrderIndex) ?? -1;
            
            column.OrderIndex = maxOrder + 1;
            column.IsActive = true;
            if (string.IsNullOrWhiteSpace(column.Status))
            {
                column.Status = "Vừa giao";
            }

            _context.KanbanColumns.Add(column);
            await _context.SaveChangesAsync();

            // Đồng bộ Task lớn vào bảng Tasks
            var mainTask = new AppTask
            {
                TaskId = column.ColumnId,
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
            existing.OrderIndex = column.OrderIndex;

            var mainTask = await _context.Tasks.FindAsync(id);
            if (mainTask != null)
            {
                mainTask.TaskName = column.ColumnName;
                mainTask.DueDate = existing.DueDate;
                mainTask.OrderIndex = column.OrderIndex;
            }
            else
            {
                mainTask = new AppTask
                {
                    TaskId = existing.ColumnId,
                    TaskName = existing.ColumnName,
                    ProjectId = existing.ProjectId,
                    ColumnId = existing.ColumnId,
                    OrderIndex = existing.OrderIndex,
                    IsActive = true,
                    DueDate = existing.DueDate
                };
                _context.Tasks.Add(mainTask);
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
            var mainTask = await _context.Tasks.FindAsync(id);
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
                }
            }
            
            await _context.SaveChangesAsync();
            return Ok(new { message = "Cap nhat thanh cong" });
        }
    }
}

