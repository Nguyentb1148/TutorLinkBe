namespace TutorLinkBe.Models;

public class QuizAnswer
{
    public Guid QuizAnswerId { get; set; }
    public Guid QuizSubmissionId { get; set; }
    public Guid QuizQuestionId { get; set; }
    public Guid? SelectedOptionId { get; set; }
    public bool IsCorrect { get; set; }

    // Navigation
    public QuizSubmission QuizSubmission { get; set; }
    public QuizQuestion QuizQuestion { get; set; }
    public QuizOption SelectedOption { get; set; }
}