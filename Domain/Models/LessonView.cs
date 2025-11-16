namespace TutorLinkBe.Domain.Models;

public class LessonView
{
    public Guid LessonViewId { get; set; }
    public Guid LessonId { get; set; }
    public string StudentId { get; set; }

    public DateTime ViewedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Lesson Lesson { get; set; }
    public ApplicationUser Student { get; set; }
}