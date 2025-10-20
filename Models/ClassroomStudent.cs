namespace TutorLinkBe.Models;

public class ClassroomStudent
{
    public Guid ClassroomStudentId { get; set; }
    public Guid ClassroomId { get; set; }
    public Guid StudentId { get; set; }

    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    public bool IsApproved { get; set; }
    public string EnrollmentStatus { get; set; }

    // Navigation
    public Classroom Classroom { get; set; }
    public ApplicationUser Student { get; set; }
}