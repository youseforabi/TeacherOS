namespace TeacherOS.Features.Courses.DTOs;

public class CreateCourseDto
{
    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public Guid CategoryId { get; set; }

    public IFormFile Thumbnail { get; set; } = null!;
}