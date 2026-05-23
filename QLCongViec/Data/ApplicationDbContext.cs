using Microsoft.EntityFrameworkCore;
using QLCongViec.Models;

namespace QLCongViec.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<TaskItem> TaskItems { get; set; }

        public DbSet<UserAccount> UserAccounts { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<TaskItem>()
                .HasOne(t => t.UserAccount)
                .WithMany(u => u.TaskItems)
                .HasForeignKey(t => t.UserId);
        }
    }
}