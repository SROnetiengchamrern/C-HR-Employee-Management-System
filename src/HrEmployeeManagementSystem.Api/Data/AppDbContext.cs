using HrEmployeeManagementSystem.Api.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace HrEmployeeManagementSystem.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Position> Positions => Set<Position>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<AttendanceRecord> AttendanceRecords => Set<AttendanceRecord>();
    public DbSet<LeaveType> LeaveTypes => Set<LeaveType>();
    public DbSet<LeaveRequest> LeaveRequests => Set<LeaveRequest>();
    public DbSet<JobOpening> JobOpenings => Set<JobOpening>();
    public DbSet<TrainingSession> TrainingSessions => Set<TrainingSession>();
    public DbSet<AppNotification> Notifications => Set<AppNotification>();
    public DbSet<DocumentTemplate> DocumentTemplates => Set<DocumentTemplate>();
    public DbSet<BenefitPlan> BenefitPlans => Set<BenefitPlan>();
    public DbSet<PerformanceReview> PerformanceReviews => Set<PerformanceReview>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Department>().HasIndex(d => d.Code).IsUnique();
        builder.Entity<Position>().HasIndex(p => new { p.DepartmentId, p.Title }).IsUnique();

        builder.Entity<Employee>().HasIndex(e => e.EmployeeCode).IsUnique();
        builder.Entity<Employee>().HasIndex(e => e.Email).IsUnique();
        builder.Entity<Employee>().Property(e => e.Status).HasConversion<string>().HasMaxLength(32);
        builder.Entity<Employee>()
            .HasOne(e => e.Department)
            .WithMany(d => d.Employees)
            .HasForeignKey(e => e.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Entity<Employee>()
            .HasOne(e => e.Position)
            .WithMany(p => p.Employees)
            .HasForeignKey(e => e.PositionId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Entity<Employee>()
            .HasOne(e => e.Manager)
            .WithMany()
            .HasForeignKey(e => e.ManagerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<AttendanceRecord>().HasIndex(a => new { a.EmployeeId, a.WorkDate }).IsUnique();
        builder.Entity<AttendanceRecord>()
            .HasOne(a => a.Employee)
            .WithMany()
            .HasForeignKey(a => a.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<LeaveRequest>().Property(l => l.Status).HasConversion<string>().HasMaxLength(32);
        builder.Entity<LeaveRequest>()
            .HasOne(l => l.Employee)
            .WithMany()
            .HasForeignKey(l => l.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Entity<LeaveRequest>()
            .HasOne(l => l.LeaveType)
            .WithMany()
            .HasForeignKey(l => l.LeaveTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<JobOpening>()
            .HasOne(j => j.Department)
            .WithMany(d => d.JobOpenings)
            .HasForeignKey(j => j.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<PerformanceReview>()
            .HasOne(p => p.Employee)
            .WithMany()
            .HasForeignKey(p => p.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<AppNotification>().HasIndex(n => n.UserId);
    }
}
