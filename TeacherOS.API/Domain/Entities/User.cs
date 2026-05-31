namespace TeacherOS.Domain.Entities;

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string Role { get; set; } = "Student";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<Course> Courses { get; set; } = new List<Course>();
    public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
}