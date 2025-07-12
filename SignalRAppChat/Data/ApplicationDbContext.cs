using Microsoft.EntityFrameworkCore;
using SignalRAppChat.Shared.Models;

namespace SignalRAppChat.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }
        public DbSet<User> Users { get; set; }
        public DbSet<Message> Messages { get; set; }
    }
}
