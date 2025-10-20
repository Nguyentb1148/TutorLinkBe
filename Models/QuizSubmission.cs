namespace TutorLinkBe.Models;

public class QuizSubmission
{
    public Guid QuizSubmissionId { get; set; }
    public Guid QuizId { get; set; }
    public Guid StudentId { get; set; }

    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    public double Score { get; set; }
    public bool IsGraded { get; set; }
    public int AttemptNumber { get; set; }

    // Navigation
    public Quiz Quiz { get; set; }
    public ApplicationUser Student { get; set; }
    public ICollection<QuizAnswer> QuizAnswers { get; set; }
}