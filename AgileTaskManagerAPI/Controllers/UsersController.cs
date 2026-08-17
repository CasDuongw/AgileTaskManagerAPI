using AgileTaskManagerAPI.Data;
using AgileTaskManagerAPI.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AgileTaskManagerAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly AppDbContext _context;

        public UsersController(AppDbContext context)
        {
            _context = context;
        }

        // Tạo tài khoản (Đăng ký)
        [HttpPost("register")]
        public async Task<ActionResult<User>> Register(User user)
        {
            // Trong thực tế sẽ cần mã hóa mật khẩu, nhưng MVP ta lưu tạm để test luồng
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Đăng ký thành công!", userId = user.UserId });
        }

        // Lấy danh sách User (Để xem ai đang có trong hệ thống)
        [HttpGet]
        public async Task<ActionResult<IEnumerable<User>>> GetUsers()
        {
            return await _context.Users.ToListAsync();
        }
    }
}
