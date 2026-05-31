namespace TeacherOS.Domain.Entities;

public class Course
{


    public Guid Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;


    public string Description { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public Guid TeacherId { get; set; }
    public string ThumbnailUrl { get; set; } = string.Empty;

public bool IsApproved { get; set; } = false;

public Guid? CategoryId { get; set; }
public Category? Category { get; set; }
    public User Teacher { get; set; } = null!;
    public ICollection<Lesson> Lessons { get; set; } = new List<Lesson>();

public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();

public ICollection<Review> Reviews {get;set;}=new List<Review>();

public bool IsDeleted {get;set;}

}