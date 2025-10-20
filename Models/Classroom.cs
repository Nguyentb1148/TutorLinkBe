namespace TutorLinkBe.Models;

public class Classroom
{
    public Guid ClassroomId { get; set; }
    public Guid TutorId { get; set; }
    public string Name { get; set; }
    public string Code { get; set; }
    public string Description { get; set; }
    public string ThumbnailUrl { get; set; }
    public string Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Navigation
    public ApplicationUser Tutor { get; set; }
    public ICollection<Course> Courses { get; set; }
    public ICollection<ClassroomStudent> ClassroomStudents { get; set; }
}