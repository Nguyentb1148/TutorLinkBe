namespace TutorLinkBe.Models
{
    public class Lesson
    {
        public Guid LessonId { get; set; }
        public Guid CourseId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string DocxFilePath { get; set; }
        public string CoverImageUrl { get; set; }
        public ICollection<LessonVideoUrl> LessonVideoUrls { get; set; }
        public int OrderIndex { get; set; }
        public int DurationMinutes { get; set; }
        public LessonStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public Course Course { get; set; }
        public ICollection<Quiz> Quizzes { get; set; }
        public ICollection<LessonView> LessonViews { get; set; }
    }
   
    public enum LessonStatus
    {
        Draft,
        Published,
        Archived,
        Deleted
    }
}