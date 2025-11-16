using Supabase.Gotrue.Exceptions;

namespace TutorLinkBe.Domain.Models
{
    public class ClassroomStudent
    {
        public Guid ClassroomStudentId { get; set; }
        public Guid ClassroomId { get; set; }
        public string StudentId { get; set; }
        public bool IsApproved { get; set; }
        public DateTime CreatedAt { get; set; }// CreatedAt
        public DateTime UpdatedAt { get; set; }
        // Track leave or kick events (simple for v1)
        public bool IsActive { get; set; } = true;
        public EnrollmentStatus EnrollmentStatus { get; set; } 
        public Classroom Classroom { get; set; }
        public ApplicationUser Student { get; set; }
    }

    public enum EnrollmentStatus
    {
        Pending, 
        Approved, 
        Rejected,
        Left, 
        Removed
    }
}