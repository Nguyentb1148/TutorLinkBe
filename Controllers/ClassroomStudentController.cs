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
	[Authorize]
	public async Task<IActionResult> JoinClassroom([FromBody] ClassroomStudentRequestDto request)
	{
		if (request.ClassroomId == Guid.Empty || string.IsNullOrWhiteSpace(request.Code))
			return BadRequest(new { message = "ClassroomId and Code are required." });

		var classroom = await _context.Classrooms
			.FirstOrDefaultAsync(c => c.ClassroomId == request.ClassroomId && c.Code == request.Code);

		if (classroom == null)
			return NotFound(new { message = "Invalid classroom or code." });

		var userId = _userManager.GetUserId(User);

		// Already member check
		var existing = await _context.ClassroomStudents
			.FirstOrDefaultAsync(cs => cs.ClassroomId == classroom.ClassroomId && cs.StudentId == userId);

		if (existing is { EnrollmentStatus: EnrollmentStatus.Approved })
			return Ok(new { success= true, message = "You are already a member of this classroom." });

		var newJoin = new ClassroomStudent {
			ClassroomId = classroom.ClassroomId,
			StudentId = userId,
			EnrollmentStatus = EnrollmentStatus.Pending,
			CreatedAt = DateTime.UtcNow,
			UpdatedAt = DateTime.UtcNow
		};

		_context.ClassroomStudents.Add(newJoin);
		await _context.SaveChangesAsync();

		return Ok(new {
			success = true,
			message = "Join request submitted. Please wait for teacher approval.",
		});
	}

	[HttpGet("myClassrooms")]
	[Authorize(Roles = "User")]
	public async Task<IActionResult> GetMyClassrooms(
		[FromQuery] int page = 1,
		[FromQuery] int pageSize = 20,
		[FromQuery] string sortBy = "CreatedAt",
		[FromQuery] string order = "desc")
	{
		var userId = _userManager.GetUserId(User);

		var queryBase = _context.ClassroomStudents
			.AsNoTracking()
			.Where(cs => cs.StudentId == userId && cs.IsActive && cs.EnrollmentStatus == EnrollmentStatus.Approved)
			.Include(cs => cs.Classroom)
			.ThenInclude(c => c.Tutor);


		// Dynamic sorting
		IQueryable<ClassroomStudent> query = sortBy.ToLower() switch
		{
			"name" => order == "asc"
				? queryBase.OrderBy(cs => cs.Classroom.Name)
				: queryBase.OrderByDescending(cs => cs.Classroom.Name),
			_ => order == "asc"
				? queryBase.OrderBy(cs => cs.CreatedAt)
				: queryBase.OrderByDescending(cs => cs.CreatedAt)
		};

		var total = await query.CountAsync();
		var entities = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
		var items = _mapper.Map<List<ClassroomStudentDto>>(entities);

		return Ok(new {
				success = true,
				message = "GetMyClassrooms.",
				data= new {
					Total = total,
					Page = page,
					PageSize = pageSize,
					Items = items
				}
			}
		);
	}
	
	[HttpDelete("leave/{classroomId:guid}")]
	[Authorize(Roles = "User")]
	public async Task<IActionResult> Leave(Guid classroomId)
	{
		var userId = _userManager.GetUserId(User);

		var cs = await _context.ClassroomStudents
			.FirstOrDefaultAsync(x => x.ClassroomId == classroomId && x.StudentId == userId && x.IsActive);

		if (cs == null)
			return NotFound(new { message = "Membership not found." });

		if (cs.EnrollmentStatus == EnrollmentStatus.Left)
			return Conflict(new { message = "You already left this classroom." });

		cs.IsActive = false;
		cs.EnrollmentStatus = EnrollmentStatus.Left;
		cs.UpdatedAt = DateTime.UtcNow;

		await _context.SaveChangesAsync();

		return Ok(new
		{
			success = true,
			message = "You have left the classroom successfully."
		});
	}
	
	[HttpGet("classroom-detail/{classroomId:guid}")]
	[Authorize]
	public async Task<IActionResult> GetClassroomDetail(Guid classroomId)
	{
		var userId = _userManager.GetUserId(User);
		var isAdmin = User.IsInRole("Admin");
		var isTeacher = User.IsInRole("Teacher");

		var classroom = await _context.Classrooms
			.AsNoTracking()
			.Include(c => c.Tutor)
			.FirstOrDefaultAsync(c => c.ClassroomId == classroomId);

		if (classroom == null)
			return NotFound(new { success = false, message = "Classroom not found." });

		if (!isAdmin && !isTeacher)
		{
			var membership = await _context.ClassroomStudents
				.AsNoTracking()
				.FirstOrDefaultAsync(cs =>
					cs.ClassroomId == classroomId &&
					cs.StudentId == userId &&
					cs.EnrollmentStatus == EnrollmentStatus.Approved);

			if (membership == null)
				return Forbid();
		}

		if (classroom.Status != ClassroomStatus.Active && !isAdmin && classroom.TutorId != userId)
			return Forbid();

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
			Courses = new List<object>()// courses data for future
		};

		return Ok(new
		{
			success = true,
			message = "Classroom details retrieved successfully.",
			data = response
		});
	}

	[HttpPost("approve-invite")]
	[Authorize]
	public async Task<IActionResult> ApproveInvite([FromBody] ClassroomStudentRequestDto request)
	{
		var userId = _userManager.GetUserId(User);

		var classroom = await _context.Classrooms
			.FirstOrDefaultAsync(c => c.ClassroomId == request.ClassroomId && c.Code == request.Code);

		if (classroom == null)
			return NotFound(new { message = "Invalid classroom or invite code." });

		if (classroom.Status != ClassroomStatus.Active)
			return BadRequest(new { message = "This classroom is not open for new members." });

		var existing = await _context.ClassroomStudents
			.FirstOrDefaultAsync(cs => cs.ClassroomId == request.ClassroomId && cs.StudentId == userId);

		if (existing != null)
		{
			existing.IsApproved = true;
			existing.IsActive = true;
			existing.EnrollmentStatus = EnrollmentStatus.Approved;
			existing.UpdatedAt = DateTime.UtcNow;

			await _context.SaveChangesAsync();
			return Ok(new { message = "Rejoined or reapproved successfully.", classroomId = classroom.ClassroomId });
		}

		var classroomStudent = new ClassroomStudent
		{
			ClassroomId = request.ClassroomId,
			StudentId = userId,
			IsApproved = true,
			IsActive = true,
			EnrollmentStatus = EnrollmentStatus.Approved,
			CreatedAt = DateTime.UtcNow
		};

		_context.ClassroomStudents.Add(classroomStudent);

		try
		{
			await _context.SaveChangesAsync();
		}
		catch (DbUpdateException)
		{
			return Conflict(new { message = "You already joined this classroom." });
		}

		return Ok(new {
			sucess= true,
			message = "Joined classroom successfully.",
		});
	}
	
	[HttpGet("{classroomId:guid}/students")]
	[Authorize]
	public async Task<IActionResult> GetStudentsByClassroom(
	    Guid classroomId,
	    [FromQuery] int page = 1,
	    [FromQuery] int pageSize = 20,
	    [FromQuery] string? sortBy = "JoinedAt",
	    [FromQuery] string? order = "desc")
	{
	    var userId = _userManager.GetUserId(User);
	    var user = await _userManager.GetUserAsync(User);
	    var userRoles = await _userManager.GetRolesAsync(user);
	    var isAdmin = userRoles.Contains("Admin");

	    var classroom = await _context.Classrooms
	        .AsNoTracking()
	        .FirstOrDefaultAsync(c => c.ClassroomId == classroomId);

	    if (classroom == null)
	        return NotFound(new { message = "Classroom not found" });

	    if (!isAdmin && classroom.TutorId != userId) {
	        var membership = await _context.ClassroomStudents
	            .FirstOrDefaultAsync(cs => cs.ClassroomId == classroomId && cs.StudentId == userId && cs.IsApproved && cs.IsActive);

	        if (membership == null)
	            return Forbid();
	    }

	    var queryBase = _context.ClassroomStudents
	        .AsNoTracking()
	        .Where(cs => cs.ClassroomId == classroomId && cs.IsActive)
	        .Include(cs => cs.Student);

	    IQueryable<ClassroomStudent> query = sortBy switch {
	        "name" => order == "asc"
	            ? queryBase.OrderBy(cs => cs.Student.UserName)
	            : queryBase.OrderByDescending(cs => cs.Student.UserName),
	        _ => order == "asc"
	            ? queryBase.OrderBy(cs => cs.CreatedAt)
	            : queryBase.OrderByDescending(cs => cs.CreatedAt)
	    };

	    var totalCount = await query.CountAsync();
	    var students = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

	    var list = students.Select(cs => new ClassroomStudentDto {
	        ClassroomStudentId = cs.ClassroomStudentId,
	        ClassroomId = cs.ClassroomId,
	        StudentId = cs.StudentId,
	        StudentName = cs.Student?.UserName,
	        StudentEmail = cs.Student?.Email,
	        AvatarUrl = cs.Student?.AvatarUrl,
	        IsApproved = cs.IsApproved,
	        IsActive = cs.IsActive,
	        EnrollmentStatus = cs.EnrollmentStatus,
	        JoinedAt = cs.CreatedAt
	    }).ToList();

	    return Ok(new {
		    success = true,
		    message = "Classroom students successfully.",
		    data= new {
			    totalCount,
			    page, 
			    pageSize,
			    data = list
		    }
	    });
	}
	
	[HttpGet("{classroomId:guid}/students/pending")]
	[Authorize(Roles = "Teacher, Admin")]
	public async Task<IActionResult> GetPending(
		Guid classroomId,
		[FromQuery] int page = 1,
		[FromQuery] int pageSize = 20,
		[FromQuery] string? sortBy = "JoinedAt",
		[FromQuery] string? order = "asc")
	{
		var userId = _userManager.GetUserId(User);
		var classroom = await _context.Classrooms.FirstOrDefaultAsync(c => c.ClassroomId == classroomId);
		if (classroom == null) return NotFound(new { message = "Classroom not found." });

		if (classroom.TutorId != userId && !User.IsInRole("Admin"))
			return Forbid();

		var queryBase = _context.ClassroomStudents
			.AsNoTracking()
			.Where(cs => cs.ClassroomId == classroomId && cs.EnrollmentStatus == EnrollmentStatus.Pending)
			.Include(cs => cs.Student);

		IQueryable<ClassroomStudent> query = sortBy switch
		{
			"name" => order == "asc"
				? queryBase.OrderBy(cs => cs.Student.UserName)
				: queryBase.OrderByDescending(cs => cs.Student.UserName),
			_ => order == "asc"
				? queryBase.OrderBy(cs => cs.CreatedAt)
				: queryBase.OrderByDescending(cs => cs.CreatedAt)
		};

		var totalCount = await query.CountAsync();
		var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

		var list = items.Select(cs => new ClassroomStudentDto
		{
			ClassroomStudentId = cs.ClassroomStudentId,
			ClassroomId = cs.ClassroomId,
			StudentId = cs.StudentId,
			StudentName = cs.Student?.UserName,
			StudentEmail = cs.Student?.Email,
			AvatarUrl = cs.Student?.AvatarUrl,
			IsApproved = cs.IsApproved,
			IsActive = cs.IsActive,
			EnrollmentStatus = cs.EnrollmentStatus,
			JoinedAt = cs.CreatedAt
		}).ToList();

		return Ok(new {
			success = true,
			message = "Get pending Classroom students successfully.",
			data= new {
				totalCount,
				page, 
				pageSize,
				data = list
			}
		});	
	}
	
	[HttpPut("approve/{id}")]
	[Authorize(Roles = "Teacher,Admin")]
	public async Task<IActionResult> ApproveOrReject([FromBody] ClassroomStudentManageDto req)
	{
		if (req.ClassroomStudentId == Guid.Empty)
			return BadRequest(new { success = false, message = "Invalid request." });

		var cs = await _context.ClassroomStudents
			.Include(x => x.Classroom)
			.FirstOrDefaultAsync(x => x.ClassroomStudentId == req.ClassroomStudentId);

		if (cs == null)
			return NotFound(new { success = false, message = "Student not found." });

		var userId = _userManager.GetUserId(User);
		var isAdmin = User.IsInRole("Admin");

		if (!isAdmin && cs.Classroom.TutorId != userId)
			return Forbid();

		var allowedStatuses = new[] { EnrollmentStatus.Approved, EnrollmentStatus.Rejected };
		if (!allowedStatuses.Contains(req.EnrollmentStatus))
			return BadRequest(new { success = false, message = "Invalid enrollment status." });

		cs.EnrollmentStatus = req.EnrollmentStatus;
		if (req.IsActive.HasValue) cs.IsActive = req.IsActive.Value;

		await _context.SaveChangesAsync();

		return Ok(new {
			success = true,
			message = "Student enrollment updated successfully"
		});
	}
	
	[HttpPut("kick/{id}")]
	[Authorize(Roles = "Teacher, Admin")]
	public async Task<IActionResult> Kick(Guid id)
	{
		var cs = await _context.ClassroomStudents
			.Include(x => x.Classroom)
			.FirstOrDefaultAsync(x => x.ClassroomStudentId == id);

		if (cs == null)
			return NotFound(new { success = false, message = "Student not found." });

		var userId = _userManager.GetUserId(User);
		var isAdmin = User.IsInRole("Admin");

		if (!isAdmin && cs.Classroom.TutorId != userId)
			return Forbid();

		cs.IsActive = false;
		cs.EnrollmentStatus = EnrollmentStatus.Removed;

		await _context.SaveChangesAsync();

		return Ok(new
		{
			success = true,
			message = "Student kicked from classroom."
		});
	}
	
	[HttpGet("search")]
	[Authorize(Roles = "Teacher, Admin")]
	public async Task<IActionResult> SearchStudents([FromQuery] Guid classroomId, [FromQuery] string keyword, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
	{
		var userId = _userManager.GetUserId(User);
		var classroom = await _context.Classrooms.FirstOrDefaultAsync(c => c.ClassroomId == classroomId);

		if (classroom == null) return NotFound();

		if (classroom.TutorId != userId && !User.IsInRole("Admin"))
			return Forbid();

		var queryBase = _context.ClassroomStudents
			.AsNoTracking()
			.Where(cs => cs.ClassroomId == classroomId)
			.Include(cs => cs.Student);
		
		IQueryable<ClassroomStudent>? query = queryBase;
		if (!string.IsNullOrWhiteSpace(keyword)) {
			var lower = keyword.ToLower();
			query = queryBase.Where(cs => cs.Student.UserName.ToLower().Contains(lower) || cs.Student.Email.ToLower().Contains(lower));
		}
		
		var total = await query.CountAsync();
		var list = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

		var results = list.Select(cs => new ClassroomStudentDto {
			ClassroomStudentId = cs.ClassroomStudentId,
			ClassroomId = cs.ClassroomId,
			StudentId = cs.StudentId,
			StudentName = cs.Student.UserName,
			StudentEmail = cs.Student.Email,
			AvatarUrl = cs.Student.AvatarUrl,
			IsApproved = cs.IsApproved,
			IsActive = cs.IsActive,
			EnrollmentStatus = cs.EnrollmentStatus,
			JoinedAt = cs.CreatedAt
		}).ToList();

		return Ok(new {
			success = true,
			message = "Search students successfully.",
			data= new {
				total, page, pageSize, items = results
			}
		});
	}

	
	//list endpoint 
	//GET /api/ClassroomStudent/all for admin to moderation or debugging
	//PUT /api/ClassroomStudent/reactivate/{id}
}