namespace TutorLinkBe.Domain.Entities;

public class LessonVideoUrl
{
    public Guid Id { get; set; }
    public Guid LessonId { get; set; }
    public string Url { get; set; }
    public int Position { get; set; }
    public string Title { get; set; }
    public Lesson Lesson { get; set; }
}