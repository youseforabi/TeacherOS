namespace TeacherOS.Domain.Entities;

public class Wishlist
{
    public Guid Id {get;set;}

    public Guid StudentId {get;set;}

public User? Student { get; set; }
public Course? Course { get; set; }
    public Guid CourseId {get;set;}

}