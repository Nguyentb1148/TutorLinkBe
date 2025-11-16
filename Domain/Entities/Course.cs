namespace TutorLinkBe.Domain.Entities
{
    public class Course
    {
        public Guid CourseId { get; set; }
        public string TutorId { get; set; }               
        public Guid? ClassroomId { get; set; } 
        public string Title { get; set; }
        public string Description { get; set; }
        public string ThumbnailUrl { get; set; }
        public bool IsTemplate { get; set; } 
        public bool IsPublished { get; set; } 
        public CourseStatus Status { get; set; }
        public int OrderIndex { get; set; } 
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public DateTime CreatedAt { get; set; } 
        public DateTime? UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }
        public ApplicationUser Tutor { get; set; }
        public Classroom Classroom { get; set; }
        public ICollection<Lesson> Lessons { get; set; }
        
        //Level(basic/ intermediate/advanced) or tags for v2
        //EstimateDuration for analytics
    }
    public enum CourseStatus
    {
        Draft,        
        Published,    
        Archived,    
        Deleted
    }
}