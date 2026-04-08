using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using InternshipManagementSystem.Models;

namespace InternshipManagementSystem.Data
{
    public class ApplicationDbContext : DbContext
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, IHttpContextAccessor httpContextAccessor) : base(options)
        {
            _httpContextAccessor = httpContextAccessor;
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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            // To prevent cascading delete loops, configure them here.
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

        public override int SaveChanges()
        {
            AddAuditInfo();
            return base.SaveChanges();
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            AddAuditInfo();
            return base.SaveChangesAsync(cancellationToken);
        }

        private void AddAuditInfo()
        {
            var currentUser = _httpContextAccessor?.HttpContext?.User?.Identity?.Name ?? "System";
            var entries = ChangeTracker.Entries<AuditableEntity>();

            foreach (var entry in entries)
            {
                if (entry.State == EntityState.Added)
                {
                    entry.Entity.CreatedAt = DateTime.Now;
                    entry.Entity.CreatedBy = currentUser;
                }

                if (entry.State == EntityState.Added || entry.State == EntityState.Modified)
                {
                    entry.Entity.UpdatedAt = DateTime.Now;
                    entry.Entity.UpdatedBy = currentUser;
                }
            }
        }
    }
}
