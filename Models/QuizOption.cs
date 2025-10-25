namespace TutorLinkBe.Models;

public class QuizOption
{
    public Guid QuizOptionId { get; set; }
    public Guid QuizQuestionId { get; set; }

    public Guid OptionText { get; set; }
    public bool IsCorrect { get; set; }

    // Navigation
    public QuizQuestion QuizQuestion { get; set; }
}