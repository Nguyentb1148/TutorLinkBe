using System.Security.Claims;
using System.Text;
using TutorLinkBe.Config;
using TutorLinkBe.Services;
using TutorLinkBe.Context;
using TutorLinkBe.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TutorLinkBe.Repository;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using TutorLinkBe.Helper;
using TutorLinkBe.MiddleWare;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddControllers();
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Events.OnRedirectToAccessDenied = context =>
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        return Task.CompletedTask;
    };
    options.Events.OnRedirectToLogin = context =>
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return Task.CompletedTask;
    };
});

builder.Services.Configure<AppSettings>(builder.Configuration.GetSection("AppSettings"));
builder.Services.AddSingleton<MongoDbService>();
// builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(builder.Configuration.GetConnectionString("PostgresConnection")));
builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(builder.Configuration.GetConnectionString("SupabaseConnection")));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.User.AllowedUserNameCharacters = null;
    }) 
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders(); 
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
    });
});
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));
builder.Services.AddScoped<IEmailService, EmailServices>();
builder.Services.AddScoped<UserRepository>(); 
builder.Services.AddScoped<TokenService>();
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
#if DEBUG
    .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true)
#endif
    .AddEnvironmentVariables();

var jwtSettings = builder.Configuration.GetSection("Jwt");
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options => {
    options.TokenValidationParameters = new TokenValidationParameters {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["SecretKey"])),

        RoleClaimType = ClaimTypes.Role,
        // NameClaimType = ClaimTypes.NameIdentifier
        NameClaimType = JwtRegisteredClaimNames.Sub
    };
});
//AutoMapper
var autoMapperLicense = builder.Configuration["AutoMapper:LicenseKey"];
builder.Services.AddAutoMapper(cfg =>
{
    cfg.LicenseKey = autoMapperLicense;
},typeof(Program).Assembly);

builder.Services.AddAuthorization(options => {
    options.DefaultPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});
// Add authorization handler for debugging
builder.Services.AddScoped<IAuthorizationHandler, DebugAuthorizationHandler>();

builder.Services.AddHttpContextAccessor(); 

builder.Logging.ClearProviders();

builder.Logging.AddConsole();

var app = builder.Build();
using (var scope = app.Services.CreateScope())
{
    try
    {
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        await RoleSeeder.SeedRolesAsync(roleManager);
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Error seeding roles");
    }
}

// Configure the HTTP request pipeline.
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "TutorLink API v1");
    c.RoutePrefix = "swagger";
});

app.UseExceptionHandler(a => a.Run(async context => {
    var exception = context.Features.Get<IExceptionHandlerPathFeature>()?.Error;
    var logger = context.RequestServices.GetService<ILogger<Program>>();
    if (logger != null) logger.LogError(exception, "Unhandled exception.");
    context.Response.StatusCode = 500;
    await context.Response.WriteAsJsonAsync(new { error = "An unexpected error occurred." });
}));
app.UseCors("AllowAll");
// app.UseMiddleware<JwtMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
// app.UseHttpsRedirection();
app.MapControllers();
app.Run();

