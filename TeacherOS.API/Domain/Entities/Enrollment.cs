namespace TeacherOS.Domain.Entities;

public class Enrollment
{
    public Guid Id  { get; set; }

    public Guid StudentId { get; set; }

    public User Student { get; set; } = null!;

    public Guid CourseId { get; set; }

    public Course Course { get; set; } = null!;

    public DateTime PurchasedAt { get; set; } = DateTime.UtcNow;


}