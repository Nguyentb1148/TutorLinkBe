using Microsoft.AspNetCore.Identity;

namespace TutorLinkBe.Models;

public class ApplicationUser : IdentityUser
{
    public string? AvatarUrl { get; set; }
    public string? Bio { get; set; }
    public string? Role { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public ICollection<Classroom> Classrooms { get; set; }
    public ICollection<ClassroomStudent> ClassroomStudents { get; set; }
    public ICollection<QuizSubmission> QuizSubmissions { get; set; }
    public ICollection<RefreshToken> RefreshTokens { get; set; }
}