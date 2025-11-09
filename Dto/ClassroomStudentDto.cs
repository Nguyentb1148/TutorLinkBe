using System;
using System.ComponentModel.DataAnnotations;
using TutorLinkBe.Models;

namespace TutorLinkBe.Dto
{
    public class ClassroomStudentDto
    {
        public Guid ClassroomStudentId { get; set; }
        public Guid ClassroomId { get; set; }
        public string StudentId { get; set; }
        public string? StudentName { get; set; }
        public string? StudentEmail { get; set; }
        public string? AvatarUrl { get; set; }
        public bool IsApproved { get; set; }
        public bool IsActive { get; set; }
        public EnrollmentStatus EnrollmentStatus { get; set; }
        public DateTime JoinedAt { get; set; }
    }

    public class ClassroomStudentRequestDto
    {
        [Required(ErrorMessage = "ClassroomId is required.")]
        public Guid ClassroomId { get; set; }
        [Required(ErrorMessage = "code is required.")]
        public string Code { get; set; }
    }

    public class ClassroomStudentManageDto
    {
        [Required(ErrorMessage = "ClassroomStudentId is required.")]
        public Guid ClassroomStudentId { get; set; }

        [Required(ErrorMessage = "EnrollmentStatus is required.")]
        public EnrollmentStatus EnrollmentStatus { get; set; }

        public bool? IsApproved { get; set; } 
        public bool? IsActive { get; set; }
    }

    public class ClassroomStudentUpdateDto
    {
        [Required(ErrorMessage = "ClassroomStudentId is required.")]
        public Guid ClassroomStudentId { get; set; }

        public bool? IsApproved { get; set; }
        public EnrollmentStatus? EnrollmentStatus { get; set; }
        public bool? IsActive { get; set; }
    }
}