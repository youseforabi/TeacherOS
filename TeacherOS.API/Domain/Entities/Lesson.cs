namespace TeacherOS.Domain.Entities;

public class Lesson
{
    public Guid Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public int Order { get; set; }

    public string VideoUrl { get; set; } = string.Empty;

    public Guid CourseId { get; set; }

    // 🔥 السطر 18 المقيد للتعديل: الاسم لازم يكون Course مش Lesson
    public Course? Course { get; set; } 

    public string? ContentType { get; set; }

    public string? Content { get; set; }
}