namespace TutorLinkBe.Models;

public class Quiz
{
    public Guid QuizId { get; set; }
    public Guid LessonId { get; set; }

    public string Title { get; set; }
    public string Instructions { get; set; }
    public bool IsPublished { get; set; }
    public int TimeLimitMinutes { get; set; }
    public int MaxScore { get; set; }
    public int PassingScore { get; set; }
    public int AttemptsAllowed { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }

    // Navigation
    public Lesson Lesson { get; set; }
    public ICollection<QuizQuestion> QuizQuestions { get; set; }
    public ICollection<QuizSubmission> QuizSubmissions { get; set; }
}