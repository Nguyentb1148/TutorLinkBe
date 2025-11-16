namespace TutorLinkBe.Domain.Entities;

public class RoleHistory
{
    public Guid Id { get; set; }
    public string UserId { get; set; }
    public string OldRole { get; set; }
    public string NewRole { get; set; }
    public string ChangedBy { get; set; }//admin Id
    public DateTime ChangedAt { get; set; }
    
    //Navigation
    public ApplicationUser User { get; set; }
    public ApplicationUser ChangedByUser { get; set; }
}