using System;

namespace TeacherOS.Domain.Entities;

public class SystemActivityLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    public required string ActionType { get; set; } 

    public required string ActorName { get; set; }
    public required string ActorRole { get; set; }

    public required string TargetName { get; set; }

    public required string Message { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}