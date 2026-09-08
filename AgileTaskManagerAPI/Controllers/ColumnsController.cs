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
            int currentUserId = GetCurrentUserId();
            var project = await _context.Projects.FindAsync(projectId);
            if (project == null || project.OwnerId != currentUserId) return Forbid();

            return await _context.KanbanColumns
                .Where(c => c.ProjectId == projectId && c.IsActive)
                .OrderBy(c => c.OrderIndex)
                .ToListAsync();
        }

        [HttpPost]
        public async Task<ActionResult<KanbanColumn>> CreateColumn(KanbanColumn column)
        {
            int currentUserId = GetCurrentUserId();
            var project = await _context.Projects.FindAsync(column.ProjectId);
            if (project == null || project.OwnerId != currentUserId) return Forbid();

            var maxOrder = await _context.KanbanColumns
                .Where(c => c.ProjectId == column.ProjectId)
                .MaxAsync(c => (int?)c.OrderIndex) ?? -1;
            
            column.OrderIndex = maxOrder + 1;
            column.IsActive = true;

            _context.KanbanColumns.Add(column);
            await _context.SaveChangesAsync();

            return Ok(column);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteColumn(int id)
        {
            var col = await _context.KanbanColumns.Include(c => c.Project).FirstOrDefaultAsync(c => c.ColumnId == id);
            if (col == null) return NotFound();
            if (col.Project == null || col.Project.OwnerId != GetCurrentUserId()) return Forbid();

            col.IsActive = false;
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
