using Microsoft.EntityFrameworkCore;
using InternshipManagementSystem.Models;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace InternshipManagementSystem.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Student> Students { get; set; }
        public DbSet<Guide> Guides { get; set; }
        public DbSet<InternshipDetails> InternshipDetails { get; set; }
        public DbSet<DailyDiary> DailyDiaries { get; set; }
        public DbSet<WeeklyReport> WeeklyReports { get; set; }
        public DbSet<InternshipTask> Tasks { get; set; }
        public DbSet<Document> Documents { get; set; }
        public DbSet<Feedback> Feedbacks { get; set; }
        public DbSet<GuideAssignment> GuideAssignments { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<Notification> Notifications { get; set; }

        public override int SaveChanges()
        {
            AddTimestamps();
            return base.SaveChanges();
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            AddTimestamps();
            return await base.SaveChangesAsync(cancellationToken);
        }

        private void AddTimestamps()
        {
            var entities = ChangeTracker.Entries()
                .Where(x => x.Entity is BaseEntity && (x.State == EntityState.Added));

            foreach (var entity in entities)
            {
                if (entity.State == EntityState.Added)
                {
                    ((BaseEntity)entity.Entity).CreatedAt = DateTime.UtcNow;
                }
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure Message relationship
            modelBuilder.Entity<Message>()
                .HasOne(m => m.Sender)
                .WithMany()
                .HasForeignKey(m => m.SenderId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Message>()
                .HasOne(m => m.Receiver)
                .WithMany()
                .HasForeignKey(m => m.ReceiverId)
                .OnDelete(DeleteBehavior.Restrict);

            // Prevent cascading delete loops for other entities
            modelBuilder.Entity<InternshipTask>()
                .HasOne(t => t.Guide)
                .WithMany()
                .HasForeignKey(t => t.GuideId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<InternshipTask>()
                .HasOne(t => t.Student)
                .WithMany()
                .HasForeignKey(t => t.StudentId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Feedback>()
                .HasOne(f => f.Guide)
                .WithMany()
                .HasForeignKey(f => f.GuideId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<GuideAssignment>()
                .HasOne(ga => ga.Guide)
                .WithMany()
                .HasForeignKey(ga => ga.GuideId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
