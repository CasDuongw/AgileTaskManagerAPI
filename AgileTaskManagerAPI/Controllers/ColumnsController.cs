using AgileTaskManagerAPI.Data;
using AgileTaskManagerAPI.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AgileTaskManagerAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ColumnsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ColumnsController(AppDbContext context)
        {
            _context = context;
        }

        // 1. Lấy danh sách cột của dự án, sort theo OrderIndex
        [HttpGet("project/{projectId}")]
        public async Task<ActionResult<IEnumerable<KanbanColumn>>> GetColumnsByProject(int projectId)
        {
            return await _context.KanbanColumns
                .Where(c => c.ProjectId == projectId)
                .OrderBy(c => c.OrderIndex)
                .ToListAsync();
        }

        // 2. Tạo cột mới
        [HttpPost]
        public async Task<ActionResult<KanbanColumn>> CreateColumn(KanbanColumn column)
        {
            // Tự động gán OrderIndex vào cuối danh sách hiện tại của Project đó
            var maxOrder = await _context.KanbanColumns
                .Where(c => c.ProjectId == column.ProjectId)
                .MaxAsync(c => (int?)c.OrderIndex) ?? -1;
            
            column.OrderIndex = maxOrder + 1;

            _context.KanbanColumns.Add(column);
            await _context.SaveChangesAsync();

            return Ok(column);
        }

        public class ReorderRequest
        {
            public int ColumnId { get; set; }
            public int OrderIndex { get; set; }
        }

        // 3. Cập nhật thứ tự hàng loạt khi kéo thả
        [HttpPut("reorder")]
        public async Task<IActionResult> ReorderColumns([FromBody] List<ReorderRequest> reorders)
        {
            if (reorders == null || !reorders.Any())
                return BadRequest("Không có dữ liệu");

            foreach (var req in reorders)
            {
                var col = await _context.KanbanColumns.FindAsync(req.ColumnId);
                if (col != null)
                {
                    col.OrderIndex = req.OrderIndex;
                }
            }
            
            await _context.SaveChangesAsync();
            return Ok(new { message = "Cập nhật thứ tự thành công" });
        }
    }
}
