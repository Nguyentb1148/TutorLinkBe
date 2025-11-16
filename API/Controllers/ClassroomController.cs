using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TutorLinkBe.Domain.Context;
using TutorLinkBe.Domain.Models;
using TutorLinkBe.Application.Dto;
using AutoMapper;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using TutorLinkBe.Application.Services;

namespace TutorLinkBe.API.Controllers;
//Some upgrade for v2
//Checking the MaxCapacity before allowing new students to classroom
//Export classroom information
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Teacher, Admin")]
public class ClassroomController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IMapper _mapper;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;

    public ClassroomController(AppDbContext context, UserManager<ApplicationUser> userManager, IMapper mapper, IEmailService emailService, IConfiguration configuration)
    {
        _context = context;
        _userManager = userManager;
        _mapper = mapper;
        _emailService = emailService;
        _configuration = configuration;
    }
    
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ClassroomCreateDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        bool isExistName= await _context.Classrooms.AnyAsync(x => x.Name == dto.Name);
        if (isExistName)    
            return BadRequest("Name is already taken");
        var classroom = _mapper.Map<Classroom>(dto);
        classroom.ClassroomId = Guid.NewGuid();
        classroom.CreatedAt = DateTime.UtcNow;
        classroom.TutorId = userId;
        classroom.Code = await GenerateUniqueCodeAsync();
        classroom.MaxCapacity = 100;
        await _context.Classrooms.AddAsync(classroom);
        await _context.SaveChangesAsync();

        var result = _mapper.Map<ClassroomDto>(classroom);

        return Ok(new
        {
            success = true,
            message = "Classroom created successfully!",
        });
    }
    private async Task<string> GenerateUniqueCodeAsync()
    {
        var random = new Random();
        string code;

        do
        {
            code = random.Next(100000, 999999).ToString(); 
        }
        while (await _context.Classrooms.AnyAsync(c => c.Code == code)); 

        return code;
    }
    
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Teacher, Admin")]
    public async Task<IActionResult> Update(Guid id, [FromBody] ClassroomUpdateDto dto)
    {
        if (!ModelState.IsValid) 
            return BadRequest(new { success = false, message = "Invalid data", errors = ModelState });

        if (id != dto.ClassroomId) 
            return BadRequest(new { success = false, message = "Classroom ID mismatch" });

        var classroom = await _context.Classrooms.FindAsync(id);
        if (classroom == null) 
            return NotFound(new { success = false, message = "Classroom not found" });

        var userId = _userManager.GetUserId(User);
        var isAdmin = User.IsInRole("Admin");

        // Teachers cannot update rejected classrooms
        if (!isAdmin && classroom.Status == ClassroomStatus.Rejected)
            return StatusCode(403, new {
                    success = false,
                    message = "Only admin can update rejected classrooms!"
                }
            );

        // Teachers can only update their own classrooms
        if (!isAdmin && classroom.TutorId != userId)
            return StatusCode(403, new {
                    success = false,
                    message = "Only admin or teacher can update classrooms!"
                }
            );

        // Check for unique name
        if (!string.Equals(classroom.Name, dto.Name, StringComparison.OrdinalIgnoreCase))
        {
            var isExistName = await _context.Classrooms
                .AnyAsync(c => c.Name == dto.Name && c.ClassroomId != id);
            if (isExistName)    
                return BadRequest(new {
                    success = false, 
                    message = "Classroom name is already taken"
                });
        }

        // Map updates
        _mapper.Map(dto, classroom);
        classroom.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        var response = _mapper.Map<ClassroomDto>(classroom);

        return Ok(new
        {
            success = true,
            message = "Classroom updated successfully",
            data = response
        });
    }
    
    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetByClassroomId(Guid id)
    {
        var classroom = await _context.Classrooms
            .AsNoTracking()
            .Include(c => c.Tutor) 
            .FirstOrDefaultAsync(c => c.ClassroomId == id);

        if (classroom == null)
            return NotFound(new { success = false, message = "Classroom not found" });

        if (classroom.Status != ClassroomStatus.Active)
        {
            var userId = _userManager.GetUserId(User);
            var isAdmin = User?.IsInRole("Admin") ?? false;
            var isOwner = userId != null && classroom.TutorId == userId;

            if (!isAdmin && !isOwner)
                return Forbid();
        }
        var response = new
        {
            classroom.ClassroomId,
            classroom.Name,
            classroom.Description,
            classroom.ThumbnailUrl,
            classroom.Code,
            classroom.Status,
            classroom.CreatedAt,
            classroom.UpdatedAt,
            Tutor = new
            {
                classroom.Tutor.Id,
                classroom.Tutor.UserName
            }
        };

        return Ok(new
        {
            success = true,
            message = "Classroom data retrieved successfully",
            data = response
        });
    }
    
    [HttpGet("by-user/{userId}")]
    [Authorize(Roles = "Teacher, Admin")]
    public async Task<IActionResult> GetClassroomsByUser(string userId, int page = 1, int pageSize = 20)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var currentUserId = _userManager.GetUserId(User);
        var isAdmin = User.IsInRole("Admin");
        var isTeacher = User.IsInRole("Teacher");

        var query = _context.Classrooms
            .AsNoTracking()
            .Include(c => c.Tutor)
            .AsQueryable();
        //query to get tutor classrooms only
        if (!isAdmin && isTeacher) {
            if (userId != currentUserId)
                return Forbid();

            query = query.Where(c => c.TutorId == userId);
        }
        //403 for user or anonymous
        else if (!isAdmin && !isTeacher) {
            return Forbid();
        }

        query = query.OrderByDescending(c => c.CreatedAt);

        var total = await query.CountAsync();
        var results = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var items = results.Select(c => new {
            c.ClassroomId,
            c.Name,
            c.Description,
            c.ThumbnailUrl,
            c.Status,
            c.CreatedAt,
            c.UpdatedAt,
            c.Code,
            c.StartAt,
            c.EndAt,
            c.MaxCapacity,
            c.Note,
            Tutor = new {
                c.Tutor.Id,
                c.Tutor.UserName
            }
        }).ToList();

        return Ok(new {
            success = true,
            message = "Classrooms retrieved successfully",
            data = new {
                Total = total,
                Page = page,
                PageSize = pageSize,
                Items = items
            }
        });
    }

    [HttpPut("reject/{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> RejectClassroom(Guid id)
    {
        var classroom = await _context.Classrooms.FindAsync(id);
        if (classroom == null)
            return NotFound(new { success = false, message = "Classroom not found" });

        classroom.Status = ClassroomStatus.Rejected;
        classroom.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(new {
            success = true,
            message = "Classroom has been rejected.",
            data = new {
                classroom.ClassroomId,
                classroom.Name,
                classroom.Status
            }
        });
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Teacher, Admin")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var classroom = await _context.Classrooms.FindAsync(id);
        if (classroom == null)
            return NotFound(new { success = false, message = "Classroom not found" });

        var userId = _userManager.GetUserId(User);
        if (classroom.TutorId != userId && !User.IsInRole("Admin"))
            return Forbid();

        _context.Classrooms.Remove(classroom);
        await _context.SaveChangesAsync();

        return Ok(new {
            success = true,
            message = "Classroom permanently deleted."
        });
    }

    [HttpGet("search")]
    // [Authorize] 
    public async Task<IActionResult> Search([FromQuery] string keyword, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        if (string.IsNullOrWhiteSpace(keyword))
            return BadRequest(new { success = false, message = "Keyword is required" });

        var userId = _userManager.GetUserId(User);
        var isAdmin = User.IsInRole("Admin");
        var isTeacher = User.IsInRole("Teacher");

        var result = await QueryClassroomsAsync(keyword, userId, isAdmin, isTeacher, page, pageSize);
        return Ok(result);
    }

    private async Task<object> QueryClassroomsAsync(string keyword, string? userId, bool isAdmin, bool isTeacher, int page, int pageSize)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _context.Classrooms
            .AsNoTracking()
            .Include(c => c.Tutor)
            .AsQueryable();

        query = query.Where(c => EF.Functions.Like(c.Name, $"%{keyword}%"));

        if (!isAdmin)//admin
        {
            if (isTeacher)//teacher
            {
                query = query.Where(c => c.TutorId == userId);
            }
            else// user or anonymous
            {
                query = query.Where(c => c.Status == ClassroomStatus.Active);
            }
        }

        query = query.OrderByDescending(c => c.CreatedAt);

        var total = await query.CountAsync();
        var list = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        var items = list.Select(c => new {
            ClassroomId = c.ClassroomId,
            Name = c.Name,
            Description = c.Description,
            ThumbnailUrl = c.ThumbnailUrl,
            Status = c.Status,
            CreatedAt = c.CreatedAt,
            UpdatedAt = c.UpdatedAt,
            Tutor = new {
                c.Tutor.Id,
                c.Tutor.UserName
            }
        }).ToList();

        return new {
            success = true,
            message = "Classrooms retrieved successfully",
            data = new {
                Total = total,
                Page = page,
                PageSize = pageSize,
                Items = items
            }
        };
    }
    
   [HttpPost("invite")]
    [Authorize(Roles = "Teacher, Admin")]
    public async Task<IActionResult> InviteByEmail([FromBody] ClassroomInviteRequestDto request)
    {
        if (request == null || request.ClassroomId == Guid.Empty || request.Emails == null || !request.Emails.Any())
            return BadRequest(new { success = false, message = "ClassroomId and at least one email are required." });

        var classroom = await _context.Classrooms.FirstOrDefaultAsync(c => c.ClassroomId == request.ClassroomId);
        if (classroom == null)
            return NotFound(new { success = false, message = "Classroom not found." });

        if (classroom.Status != ClassroomStatus.Active)
            return BadRequest(new { success = false, message = "Cannot invite students to a non-active classroom." });

        var userId = _userManager.GetUserId(User);
        if (classroom.TutorId != userId && !User.IsInRole("Admin"))
            return StatusCode(403,new { success = false, message = "You do not have permission to invite to this classroom." });
        
        var baseUrl = _configuration["FrontEnd:BaseUrl"] ?? "http://localhost:3000";
        var joinUrl = $"{baseUrl}/join?classroomId={classroom.ClassroomId}&code={classroom.Code}";

        var subject = $"You're invited to join {classroom.Name}";
        var success = new List<string>();
        var failed = new List<string>();

        // Deduplicate emails
        var emails = request.Emails.Select(e => e.Trim().ToLower()).Distinct();

        foreach (var email in emails) {
            try {
                var htmlContent = $@"
                    <h3>Hi there 👋</h3>
                    <p>You’ve been invited to join the classroom <strong>{classroom.Name}</strong>.</p>
                    <p>Click the button below to join:</p>
                    <p><a href='{joinUrl}' 
                          style='display:inline-block;padding:10px 20px;background-color:#007bff;color:#fff;text-decoration:none;border-radius:5px;'>
                          Join Classroom
                       </a></p>
                    <p>Or use this code: <strong>{classroom.Code}</strong></p>
                    <hr/>
                    <p>If you didn’t expect this invite, please ignore this email.</p>";

                await _emailService.SendEmailAsync(email, subject, htmlContent);
                success.Add(email);
            }
            catch (Exception ex) {
                failed.Add(email);
                Console.WriteLine($"Error sending email to {email}: {ex.Message}");
            }
        }

        return Ok(new {
            success = true,
            message = "Invitation process completed.",
            data = new {
                classroomId = classroom.ClassroomId,
                total = emails.Count(),
                successCount = success.Count,
                failedCount = failed.Count,
                joinUrl,
                success,
                failed
            }
        });
    }

    [HttpGet("list")]
    public async Task<IActionResult> GetClassrooms([FromQuery] string? keyword = null,
                                                    [FromQuery] ClassroomStatus? status = null, 
                                                    [FromQuery] string? sortBy = "CreatedAt", 
                                                    [FromQuery] string? sortOrder = "desc", 
                                                    [FromQuery] int page = 1, 
                                                    [FromQuery] int pageSize = 20)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var userId = _userManager.GetUserId(User);
        var isAdmin = User.IsInRole("Admin");
        var isTeacher = User.IsInRole("Teacher");

        var query = _context.Classrooms
            .AsNoTracking()
            .Include(c => c.Tutor)
            .AsQueryable();

        // Keyword filter
        if (!string.IsNullOrWhiteSpace(keyword)) {
            query = query.Where(c => EF.Functions.Like(c.Name, $"%{keyword}%"));
        }

        // Status filter
        if (status.HasValue) {
            if (!isAdmin && !isTeacher)
                return StatusCode (403,new { success = false, message = "You cannot filter by status." });
            query = query.Where(c => c.Status == status.Value);
        }
        else {
            // Default visibility
            if (!isAdmin) {
                if (isTeacher)
                    query = query.Where(c => c.TutorId == userId); // own classrooms
                else
                    query = query.Where(c => c.Status == ClassroomStatus.Active); // students / anonymous
            }
        }

        // Sorting
        query = sortBy?.ToLower() switch {
            "name" => sortOrder?.ToLower() == "asc" ? query.OrderBy(c => c.Name) : query.OrderByDescending(c => c.Name),
            "updatedat" => sortOrder?.ToLower() == "asc" ? query.OrderBy(c => c.UpdatedAt) : query.OrderByDescending(c => c.UpdatedAt),
            _ => sortOrder?.ToLower() == "asc" ? query.OrderBy(c => c.CreatedAt) : query.OrderByDescending(c => c.CreatedAt),
        };

        var total = await query.CountAsync();
        var list = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        var items = list.Select(c => new {
            c.ClassroomId,
            c.Name,
            c.Description,
            c.ThumbnailUrl,
            c.Status,
            c.CreatedAt,
            c.UpdatedAt,
            Tutor = new {
                c.Tutor.Id,
                c.Tutor.UserName
            }
        }).ToList();

        return Ok(new {
            success = true,
            message = "Classrooms retrieved successfully",
            data = new {
                Total = total,
                Page = page,
                PageSize = pageSize,
                Items = items
            }
        });
    }
}