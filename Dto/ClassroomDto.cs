using System.ComponentModel.DataAnnotations;
using TutorLinkBe.Models;

namespace TutorLinkBe.Dto;

public class ClassroomCreateDto
{
    [Required(ErrorMessage = "Classroom name is required.")]
    [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters.")]
    public string Name { get; set; }                     

    [StringLength(10, ErrorMessage = "Code cannot exceed 10 characters.")]
    public string? Code { get; set; }                     

    [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters.")]
    public string? Description { get; set; }

    [Url(ErrorMessage = "Invalid thumbnail URL format.")]
    public string? ThumbnailUrl { get; set; }

    [DataType(DataType.Date)]
    public DateTime? StartAt { get; set; }               

    [DataType(DataType.Date)]
    public DateTime? EndAt { get; set; }                 

    [StringLength(300, ErrorMessage = "Note cannot exceed 300 characters.")]
    public string? Note { get; set; }                  
}
public class ClassroomUpdateDto : ClassroomCreateDto
{
    [Required(ErrorMessage = "ClassroomId is required.")]
    public Guid ClassroomId { get; set; }

    public ClassroomStatus? Status { get; set; }
}
public class ClassroomDto : ClassroomUpdateDto
{
    public DateTime CreatedAt { get; set; } 
    public DateTime? UpdatedAt { get; set; }        
}

