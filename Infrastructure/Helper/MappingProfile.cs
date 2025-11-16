using AutoMapper;
using TutorLinkBe.Domain.Entities;
using TutorLinkBe.Application.DTOs;
namespace TutorLinkBe.Infrastructure.Helper;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        //Classroom
        CreateMap<ClassroomCreateDto, Classroom>();
        CreateMap<ClassroomUpdateDto, Classroom>()
            .ForAllMembers(opt =>
                opt.Condition((src, dest, srcMember) => srcMember != null)
            );
        CreateMap<Classroom, ClassroomDto>();
        //Classroom Student
        CreateMap<ClassroomStudentRequestDto, ClassroomStudent>()
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(_ => DateTime.UtcNow))
            .ForMember(dest => dest.IsApproved, opt => opt.MapFrom(_ => false))
            .ForMember(dest => dest.IsActive, opt => opt.MapFrom(_ => true))
            .ForMember(dest => dest.EnrollmentStatus, opt => opt.MapFrom(_ => EnrollmentStatus.Pending));
        CreateMap<ClassroomStudentUpdateDto, ClassroomStudent>()
            .ForAllMembers(opt =>
                opt.Condition((src, dest, srcMember) => srcMember != null)
            );
        CreateMap<ClassroomStudent, ClassroomStudentDto>()
            .ForMember(dest => dest.StudentName, opt => opt.MapFrom(src => src.Student.UserName))
            .ForMember(dest => dest.StudentEmail, opt => opt.MapFrom(src => src.Student.Email))
            .ForMember(dest => dest.AvatarUrl, opt => opt.MapFrom(src => src.Student.AvatarUrl));
        //Course
        CreateMap<CourseCreateDto, Course>();

        CreateMap<CourseUpdateDto, Course>()
            .ForAllMembers(opt =>
                opt.Condition((src, dest, srcMember) => srcMember != null));
        CreateMap<Course, CourseDto>()
            .ForMember(dest => dest.TutorName, opt => opt.MapFrom(src => src.Tutor.UserName))
            .ForMember(dest => dest.TutorEmail, opt => opt.MapFrom(src => src.Tutor.Email))
            .ForMember(dest => dest.ClassroomName, opt => opt.MapFrom(src => src.Classroom != null ? src.Classroom.Name : null))
            .ForMember(dest => dest.Lessons, opt => opt.MapFrom(src => src.Lessons));
    }
}