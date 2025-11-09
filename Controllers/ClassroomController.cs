using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TutorLinkBe.Context;
using TutorLinkBe.Models;
using TutorLinkBe.Dto;
using AutoMapper;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using TutorLinkBe.Services;

namespace TutorLinkBe.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Teacher, Admin")]
public class ClassroomController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IMapper _mapper;
    private readonly IEmailService _emailService;

    public ClassroomController(AppDbContext context, UserManager<ApplicationUser> userManager, IMapper mapper, IEmailService emailService)
    {
        _context = context;
        _userManager = userManager;
        _mapper = mapper;
        _emailService = emailService;
    }
    
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ClassroomCreateDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var classroom = _mapper.Map<Classroom>(dto);
        classroom.ClassroomId = Guid.NewGuid();
        classroom.CreatedAt = DateTime.UtcNow;
        classroom.TutorId = userId;
        classroom.Code = await GenerateUniqueCodeAsync();
        classroom.MaxCapacity = 100;
        await _context.Classrooms.AddAsync(classroom);
        await _context.SaveChangesAsync();

        var result = _mapper.Map<ClassroomDto>(classroom);

        return CreatedAtAction(nameof(GetById), new { id = classroom.ClassroomId }, result);
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
    public async Task<IActionResult> Update(Guid id, [FromBody] ClassroomUpdateDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        if (id != dto.ClassroomId) return BadRequest(new { message = "Id mismatch" });

        var classroom = await _context.Classrooms.FindAsync(id);
        if (classroom == null) return NotFound();

        var userId = _userManager.GetUserId(User);
        if (classroom.TutorId != userId && !User.IsInRole("Admin"))
            return Forbid();

        _mapper.Map(dto, classroom);
        classroom.UpdatedAt = DateTime.UtcNow;

        _context.Classrooms.Update(classroom);
        await _context.SaveChangesAsync();

        var response = _mapper.Map<ClassroomDto>(classroom);
        return Ok(response);
    }

    [HttpGet("{id:guid}")] 
    [AllowAnonymous]
    public async Task<IActionResult> GetById(Guid id)
    {
        var classroom = await _context.Classrooms
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.ClassroomId == id);

        if (classroom == null)
            return NotFound();

        if (classroom.Status != ClassroomStatus.Active)
        {
            var userId = _userManager.GetUserId(User);
            var isAdmin = User.IsInRole("Admin");
            var isOwner = userId != null && classroom.TutorId == userId;

            if (!isAdmin && !isOwner)
                return Forbid();
        }

        return Ok(_mapper.Map<ClassroomDto>(classroom));
    }

    [HttpGet]
    public async Task<IActionResult> GetClassrooms(string? keyword = null, int page = 1, int pageSize = 20)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var userId = _userManager.GetUserId(User);
        var isAdmin = User.IsInRole("Admin");
        var isTeacher = User.IsInRole("Teacher");

        var query = _context.Classrooms.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var lower = keyword.ToLower();
            query = query.Where(c => c.Name.ToLower().Contains(lower));
        }

        if (isAdmin)
            query = query;
        else if (isTeacher)
            query = query.Where(c => c.TutorId == userId);
        else
            query = query.Where(c => c.Status == ClassroomStatus.Active);


        query = query.OrderByDescending(c => c.CreatedAt);

        var total = await query.CountAsync();
        var results = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var items = _mapper.Map<List<ClassroomDto>>(results);

        return Ok(new PagedResult<ClassroomDto> { Total = total, Page = page, PageSize = pageSize, Items = items  });
    }
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var classroom = await _context.Classrooms.FindAsync(id);
        if (classroom == null) return NotFound();

        var userId = _userManager.GetUserId(User);
        if (classroom.TutorId != userId && !User.IsInRole("Admin"))
            return Forbid();

        _context.Classrooms.Remove(classroom);
        await _context.SaveChangesAsync();
        return NoContent();
    }
    [HttpGet("search")]
    [AllowAnonymous]
    public async Task<IActionResult> Search([FromQuery] string keyword, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        if (string.IsNullOrWhiteSpace(keyword))
            return BadRequest(new { message = "Keyword is required" });
        var isAdmin = User.IsInRole("Admin");
        return Ok(await QueryClassroomsAsync(keyword, isAdmin, page, pageSize));
    }
    private async Task<object> QueryClassroomsAsync(string? keyword, bool isAdmin, int page, int pageSize)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _context.Classrooms.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var lower = keyword.ToLower();
            query = query.Where(c => EF.Functions.Like(c.Name, $"%{keyword}%"));
        }

        if (!isAdmin)
        {
            query = query.Where(c => c.Status == ClassroomStatus.Active);
        }

        query = query.OrderByDescending(c => c.CreatedAt);

        var total = await query.CountAsync();
        var list = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return Ok(new PagedResult<ClassroomDto> { Total = total, Page = page, PageSize = pageSize, Items = _mapper.Map<List<ClassroomDto>>(list) });
    }
    public class PagedResult<T>
    {
        public int Total { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public List<T> Items { get; set; }
    }

    [HttpPost("invite")]
    public async Task<IActionResult> InviteByEmail([FromBody] ClassroomInviteRequestDto request)
    {
        if (request == null || request.ClassroomId == Guid.Empty || request.Emails == null || !request.Emails.Any())
            return BadRequest(new { message = "ClassroomId and at least one email are required." });

        var classroom = await _context.Classrooms.FirstOrDefaultAsync(c => c.ClassroomId == request.ClassroomId);
        if (classroom == null) return NotFound(new { message = "Classroom not found." });

        var userId = _userManager.GetUserId(User);
        if (classroom.TutorId != userId && !User.IsInRole("Admin"))
            return Forbid();

        var baseUrl = "http://localhost:3000";
        var joinUrl = $"{baseUrl}/join?classroomId={classroom.ClassroomId}&code={classroom.Code}";

        var subject = $"You're invited to join {classroom.Name}";
        var success = new List<string>();
        var failed = new List<string>();

        foreach (var email in request.Emails)
        {
            try
            {
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
            catch (Exception ex)
            {
                failed.Add(email);
                Console.WriteLine($"Error sending email to {email}: {ex.Message}");
            }
        }

        return Ok(new
        {
            classroomId = classroom.ClassroomId,
            total = request.Emails.Count,
            successCount = success.Count,
            failedCount = failed.Count,
            joinUrl,
            success,
            failed
        });
    }

}