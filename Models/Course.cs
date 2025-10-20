namespace TutorLinkBe.Models;

public class Course
{
    public Guid CourseId { get; set; }
    public Guid ClassroomId { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public string ThumbnailUrl { get; set; }
    public bool IsPublished { get; set; }
    public int OrderIndex { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }

    // Navigation
    public Classroom Classroom { get; set; }
    public ICollection<Lesson> Lessons { get; set; }
}