using Microsoft.EntityFrameworkCore;
using Task_Manager.Models;

namespace Task_Manager.Data
{
    public class Task_Manager_DbContext : DbContext
    {
        public Task_Manager_DbContext(DbContextOptions<Task_Manager_DbContext> options)
            : base(options)
        {
        }
        public DbSet<User> Users { get; set; } // => Set<User>();
        public DbSet<TaskItem> TaskItems { get; set; }  // => Set<TaskItem>();
        public DbSet<Notification> Notifications { get; set; } // => Set<Notification>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<TaskItem>()
                .HasOne(t => t.User)
                .WithMany(u => u.TaskItems)
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Notification>()
                .HasOne(n => n.User)
                .WithMany(u => u.Notifications)
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Notification>()
                .HasOne(n => n.TaskItem)
                .WithMany(t => t.Notifications)
                .HasForeignKey(n => n.TaskItemId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();
        }
    }
}