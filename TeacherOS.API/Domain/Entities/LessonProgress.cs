namespace TeacherOS.Domain.Entities;

public class LessonProgress
{
    public Guid Id { get; set; }

    public Guid StudentId { get; set; }

    public Guid LessonId { get; set; }

    public DateTime CompletedAt { get; set; }

    public DateTime? LastViewedAt { get; set; }

    public bool IsCompleted { get; set; }

    public User Student { get; set; }

    public Lesson Lesson { get; set; }
}