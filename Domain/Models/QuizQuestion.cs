namespace TutorLinkBe.Domain.Models;

public class QuizQuestion
{
    public Guid QuizQuestionId { get; set; }
    public Guid QuizId { get; set; }

    public string QuestionText { get; set; }
    public string QuestionType { get; set; } // e.g. "MultipleChoice", "TrueFalse"
    public int OrderIndex { get; set; }
    public int Points { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Quiz Quiz { get; set; }
    public ICollection<QuizOption> QuizOptions { get; set; }
    public ICollection<QuizAnswer> QuizAnswers { get; set; }
}