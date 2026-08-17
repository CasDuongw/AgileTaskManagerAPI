using AgileTaskManagerAPI.Model;
using Microsoft.EntityFrameworkCore;

namespace AgileTaskManagerAPI.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        // Khai báo 3 bảng sẽ được tạo trong Database
        public DbSet<User> Users { get; set; }
        public DbSet<Project> Projects { get; set; }
        public DbSet<AppTask> Tasks { get; set; }
    }
}
