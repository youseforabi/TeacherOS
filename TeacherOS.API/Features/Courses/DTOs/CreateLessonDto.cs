namespace TeacherOS.Features.Courses.DTOs;

public class CreateLessonDto
{
    public required string Title { get; set; }
    public string? Description { get; set; } // إضافة ? لمنع الـ Null Exception
    public int Order { get; set; }
    public string? ContentType { get; set; } // إضافة ? 
    public required string Content { get; set; }
}