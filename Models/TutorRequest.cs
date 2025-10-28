namespace TutorLinkBe.Models;

public class TutorRequest
{
    public Guid Id { get; set; }
    public string UserId { get; set; }
    public TutorRequestStatus Status { get; set; } 
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    //Other information for approve will update later
    //Navigation
    public ApplicationUser User { get; set; }
}

public enum TutorRequestStatus
{
    Pending, 
    Approved, 
    Rejected
}