namespace TutorLinkBe.Models;

public class Lesson
{
    public Guid LessonId { get; set; }
    public Guid CourseId { get; set; }

    public string Title { get; set; }
    public string Description { get; set; }
    public string FileUrl { get; set; }
    public string VideoUrl { get; set; }
    public int OrderIndex { get; set; }
    public int DurationMinutes { get; set; }
    public bool IsPublished { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }

    // Navigation
    public Course Course { get; set; }
    public ICollection<LessonView> LessonViews { get; set; }
    public ICollection<Quiz> Quizzes { get; set; }
}