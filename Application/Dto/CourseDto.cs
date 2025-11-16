using System.ComponentModel.DataAnnotations;
using TutorLinkBe.Domain.Models;

namespace TutorLinkBe.Application.Dto;

public class CourseCreateDto
{
    [Required(ErrorMessage = "Classroom is required.")]
    public Guid? ClassroomId { get; set; }

    [Required(ErrorMessage = "Course title is required.")]
    [StringLength(100, ErrorMessage = "Title cannot exceed 100 characters.")]
    public string Title { get; set; }

    [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters.")]
    public string? Description { get; set; }

    [Url(ErrorMessage = "Invalid thumbnail URL format.")]
    public string? ThumbnailUrl { get; set; }

    public bool IsTemplate { get; set; }
    public bool IsPublished { get; set; }

    [Required(ErrorMessage = "Course status is required.")]
    public CourseStatus Status { get; set; }

    [Required(ErrorMessage = "Order index is required.")]
    public int OrderIndex { get; set; }

    [DataType(DataType.Date)]
    public DateTime? StartDate { get; set; }

    [DataType(DataType.Date)]
    public DateTime? EndDate { get; set; }
}

public class CourseUpdateDto : CourseCreateDto
{
    [Required(ErrorMessage = "CourseId is required.")]
    public Guid CourseId { get; set; }
    
    public DateTime? UpdatedAt { get; set; }
}

public class CourseDto : CourseUpdateDto
{
    [Required(ErrorMessage = "Tutor ID is required.")]
    
    public string TutorId { get; set; }
    public string TutorName { get; set; }
    public string TutorEmail { get; set; }
    public string? ClassroomName { get; set; }

    public DateTime CreatedAt { get; set; }
    public List<LessonBriefDto>? Lessons { get; set; }
}
