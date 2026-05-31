using Microsoft.EntityFrameworkCore;
using TeacherOS.Domain.Entities;

namespace TeacherOS.Database.Context;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users { get; set; }
    public DbSet<Course> Courses { get; set; }

public DbSet<Lesson> Lessons { get; set; }
public DbSet<Category> Categories { get; set; }
    public DbSet<Enrollment> Enrollments { get; set; }
    public DbSet<Review> Reviews { get; set; }
    public DbSet<Wishlist> Wishlists {get;set;}
    public DbSet<Notification> Notifications {get;set;}
    public DbSet<LessonProgress> LessonProgresses { get; set; }
    public DbSet<SystemActivityLog> SystemActivityLogs { get; set; }
}