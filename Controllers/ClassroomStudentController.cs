using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TutorLinkBe.Context;
using TutorLinkBe.Models;
using AutoMapper;
using TutorLinkBe.Dto;

namespace TutorLinkBe.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ClassroomStudentController : ControllerBase
{
	private readonly AppDbContext _context;
	private readonly UserManager<ApplicationUser> _userManager;
	private readonly IMapper _mapper;

	public ClassroomStudentController(AppDbContext context, UserManager<ApplicationUser> userManager, IMapper mapper)
	{
		_context = context;
		_userManager = userManager;
		_mapper = mapper;
	}

	[HttpPost("join")]
	[Authorize(Roles = "User")]
	public async Task<IActionResult> Join([FromBody] ClassroomStudentCreateDto req)
	{
		var userId = _userManager.GetUserId(User);

		if (req == null || req.ClassroomId == Guid.Empty || string.IsNullOrWhiteSpace(req.Code))
			return BadRequest(new { message = "ClassroomId and Code are required." });

		var classroom = await _context.Classrooms
			.FirstOrDefaultAsync(c => c.ClassroomId == req.ClassroomId);

		if (classroom == null)
			return NotFound(new { message = "Classroom not found." });

		if (!string.Equals(classroom.Code?.Trim(), req.Code?.Trim(), StringComparison.OrdinalIgnoreCase))
			return BadRequest(new { message = "Invalid classroom code." });

		var exists = await _context.ClassroomStudents
			.AnyAsync(cs => cs.ClassroomId == classroom.ClassroomId && cs.StudentId == userId && cs.IsActive);

		if (exists)
			return Conflict(new { message = "You already joined or have a pending request." });

		var csEntity = new ClassroomStudent
		{
			ClassroomStudentId = Guid.NewGuid(),
			ClassroomId = classroom.ClassroomId,
			StudentId = userId,
			JoinedAt = DateTime.UtcNow,
			IsActive = true,
			EnrollmentStatus = EnrollmentStatus.Pending
		};

		await _context.ClassroomStudents.AddAsync(csEntity);
		await _context.SaveChangesAsync();

		var dto = _mapper.Map<ClassroomStudentDto>(csEntity);
		return CreatedAtAction(nameof(GetMyClassrooms), new { }, dto);
	}

	[HttpGet("myClassrooms")]
	[Authorize(Roles = "User")]
	public async Task<IActionResult> GetMyClassrooms()
	{
		var userId = _userManager.GetUserId(User);
		var entities = await _context.ClassroomStudents
			.AsNoTracking()
			.Where(cs => cs.StudentId == userId && cs.IsActive && cs.EnrollmentStatus == EnrollmentStatus.Approved)
			.Include(cs => cs.Classroom)
			.ToListAsync();

		var items = _mapper.Map<List<ClassroomStudentDto>>(entities);
		return Ok(items);
	}

	[HttpDelete("leave/{classroomId:guid}")]
	[Authorize(Roles = "User")]
	public async Task<IActionResult> Leave(Guid classroomId)
	{
		var userId = _userManager.GetUserId(User);
		var cs = await _context.ClassroomStudents
			.FirstOrDefaultAsync(x => x.ClassroomId == classroomId && x.StudentId == userId && x.IsActive);
		if (cs == null) return NotFound(new { message = "Membership not found." });

		cs.IsActive = false;
		cs.EnrollmentStatus = EnrollmentStatus.Left;
		await _context.SaveChangesAsync();
		return Ok(new { message = "Left classroom." });
	}
	
	[HttpGet("classroom-detail/{classroomId:guid}")]
	[Authorize] 
	public async Task<IActionResult> GetClassroomDetail(Guid classroomId)
	{
		var userId = _userManager.GetUserId(User);
		var userRoles = await _userManager.GetRolesAsync(await _userManager.GetUserAsync(User));
		var isAdmin = userRoles.Contains("Admin");
		var isTeacher = userRoles.Contains("Teacher");

		var classroom = await _context.Classrooms
			.AsNoTracking()
			.Include(c => c.Tutor)
			.FirstOrDefaultAsync(c => c.ClassroomId == classroomId);

		if (classroom == null)
			return NotFound(new { message = "Classroom not found." });

		if (!isAdmin && !isTeacher)
		{
			var membership = await _context.ClassroomStudents
				.FirstOrDefaultAsync(cs =>
					cs.ClassroomId == classroomId &&
					cs.StudentId == userId &&
					cs.IsActive);

			if (membership == null)
				return Forbid();
		}

		var response = new
		{
			ClassroomId = classroom.ClassroomId,
			Name = classroom.Name,
			Description = classroom.Description,
			ThumbnailUrl = classroom.ThumbnailUrl,
			TutorName = classroom.Tutor?.UserName,
			TutorEmail = classroom.Tutor?.Email,
			CreatedAt = classroom.CreatedAt,
			Status = classroom.Status,
			Courses = new List<object>()
		};

		return Ok(response);
	}
	[HttpPost("approve-invite")]
	[Authorize]
	public async Task<IActionResult> ApproveInvite([FromBody] ClassroomStudentCreateDto request)
	{
		var userId = _userManager.GetUserId(User);

		// Validate classroom
		var classroom = await _context.Classrooms
			.FirstOrDefaultAsync(c => c.ClassroomId == request.ClassroomId && c.Code == request.Code);

		if (classroom == null)
			return NotFound("Invalid classroom or code.");

		// Check if already joined
		var existing = await _context.ClassroomStudents
			.FirstOrDefaultAsync(cs => cs.ClassroomId == request.ClassroomId && cs.StudentId == userId);

		if (existing != null)
		{
			if (!existing.IsApproved)
			{
				existing.IsApproved = true;
				existing.EnrollmentStatus = EnrollmentStatus.Approved;
				existing.JoinedAt = DateTime.UtcNow;
				await _context.SaveChangesAsync();
			}
			return Ok("Already joined or approved successfully.");
		}

		// Add new student entry
		var classroomStudent = new ClassroomStudent
		{
			ClassroomId = request.ClassroomId,
			StudentId = userId,
			IsApproved = true,
			IsActive = true,
			EnrollmentStatus = EnrollmentStatus.Approved,
			JoinedAt = DateTime.UtcNow
		};

		_context.ClassroomStudents.Add(classroomStudent);
		await _context.SaveChangesAsync();

		return Ok("Joined classroom successfully.");
	}


	[HttpGet("student/{classroomId:guid}")]
	[Authorize]
	public async Task<IActionResult> GetStudentsByClassroom(Guid classroomId)
	{
		var userId = _userManager.GetUserId(User);

		var classroom = await _context.Classrooms
			.AsNoTracking()
			.FirstOrDefaultAsync(c => c.ClassroomId == classroomId);

		if (classroom == null)
			return NotFound(new { message = "Classroom not found" });

		// if (classroom.TutorId != userId && !User.IsInRole("Admin"))
		// 	return Forbid();

		var entities = await _context.ClassroomStudents
			.AsNoTracking()
			.Where(cs => cs.ClassroomId == classroomId)
			.Include(cs => cs.Student) // Make sure this navigation exists
			.ToListAsync();

		var list = entities.Select(cs => new ClassroomStudentDto
		{
			ClassroomStudentId = cs.ClassroomStudentId,
			ClassroomId = cs.ClassroomId,
			StudentId = cs.StudentId,
			StudentName = cs.Student.UserName,
			StudentEmail =  cs.Student.Email,
			AvatarUrl =  cs.Student.AvatarUrl,
			IsApproved = cs.IsApproved,
			IsActive = cs.IsActive,
			EnrollmentStatus = cs.EnrollmentStatus,
			JoinedAt = cs.JoinedAt
		}).ToList();

		return Ok(list);
	}
	
	[HttpGet("pending/{classroomId:guid}")]
	[Authorize(Roles = "Teacher")]
	public async Task<IActionResult> GetPending(Guid classroomId)
	{
		var userId = _userManager.GetUserId(User);
		var classroom = await _context.Classrooms.FirstOrDefaultAsync(c => c.ClassroomId == classroomId);
		if (classroom == null) return NotFound();

		if (classroom.TutorId != userId && !User.IsInRole("Admin"))
			return Forbid();

		var entities = await _context.ClassroomStudents
			.AsNoTracking()
			.Where(cs => cs.ClassroomId == classroomId)
			.Where(cs => cs.EnrollmentStatus == EnrollmentStatus.Pending )
			.Include(cs => cs.Student) 
			.ToListAsync();

		var list = entities.Select(cs => new ClassroomStudentDto
		{
			ClassroomStudentId = cs.ClassroomStudentId,
			ClassroomId = cs.ClassroomId,
			StudentId = cs.StudentId,
			StudentName = cs.Student.UserName,
			StudentEmail =  cs.Student.Email,
			AvatarUrl =  cs.Student.AvatarUrl,
			IsApproved = cs.IsApproved,
			IsActive = cs.IsActive,
			EnrollmentStatus = cs.EnrollmentStatus,
			JoinedAt = cs.JoinedAt
		}).ToList();

		return Ok(list);
	}
	
	[HttpPut("approve/{id}")]
	[Authorize(Roles = "Teacher")]
	public async Task<IActionResult> ApproveOrReject( [FromBody] ClassroomStudentManageDto req)
	{
		if (req == null || req.ClassroomStudentId == Guid.Empty) return BadRequest();
		var cs = await _context.ClassroomStudents.Include(x => x.Classroom).FirstOrDefaultAsync(x => x.ClassroomStudentId == req.ClassroomStudentId);
		if (cs == null) return NotFound();

		var userId = _userManager.GetUserId(User);
		if (cs.Classroom.TutorId != userId && !User.IsInRole("Admin"))
			return Forbid();

		cs.EnrollmentStatus = req.EnrollmentStatus;
		if (req.IsActive.HasValue) cs.IsActive = req.IsActive.Value;
		if (req.IsApproved.HasValue) { /* legacy boolean - keep for compatibility */ }

		await _context.SaveChangesAsync();

		return Ok(new { message = "Updated" });
	}

	[HttpPut("kick/{id}")]
	[Authorize(Roles = "Teacher")]
	public async Task<IActionResult> Kick(Guid id)
	{
		var cs = await _context.ClassroomStudents.Include(x => x.Classroom).FirstOrDefaultAsync(x => x.ClassroomStudentId == id);
		if (cs == null) return NotFound();

		var userId = _userManager.GetUserId(User);
		if (cs.Classroom.TutorId != userId && !User.IsInRole("Admin"))
			return Forbid();

		cs.IsActive = false;
		cs.EnrollmentStatus = EnrollmentStatus.Removed;
		await _context.SaveChangesAsync();

		return Ok(new { message = "Student kicked from classroom." });
	}
	
	//list endpoint 
	//GET /api/ClassroomStudent/all for admin to moderation or debugging
	//GET /api/ClassroomStudent/{id} to get student details
	//PUT /api/ClassroomStudent/reactivate/{id}
	//GET /api/ClassroomStudent/myPending for student to check status
}