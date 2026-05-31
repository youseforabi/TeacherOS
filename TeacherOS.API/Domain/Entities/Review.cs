namespace TeacherOS.Domain.Entities;

public class Review
{
    public Guid Id { get; set; }

    public int Rating { get; set; }

    public string Comment { get; set; } = string.Empty;

    public Guid StudentId { get; set; }


    public Guid CourseId { get; set; }

public User? Student { get; set; }
public Course? Course { get; set; }}