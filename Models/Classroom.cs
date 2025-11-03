namespace TutorLinkBe.Models;
public class Classroom
{
    public Guid ClassroomId { get; set; }
    public string TutorId { get; set; }
    public string Name { get; set; }                     
    public string Code { get; set; }                     
    public string Description { get; set; }
    public string ThumbnailUrl { get; set; }
    public DateTime? StartAt { get; set; }               
    public DateTime? EndAt { get; set; }                 
    public ClassroomStatus Status { get; set; }
    public int MaxCapacity { get; set; }
    public string? Note { get; set; }                  
    public DateTime CreatedAt { get; set; } 
    public DateTime? UpdatedAt { get; set; }
    public ApplicationUser Tutor { get; set; }
    public ICollection<Course> Courses { get; set; }     // can contain 1 or more courses (curriculum templates)
    public ICollection<ClassroomStudent> ClassroomStudents { get; set; }
}

public enum ClassroomStatus
{
    Draft ,
    Active ,
    Completed ,
    Cancelled ,
    Rejected  // Only used by Admin moderation
}