namespace TutorLinkBe.Application.DTOs;

public class AuthResponseDto
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public UserInfoDto User { get; set; } = null!;
    public string Role { get; set; } = string.Empty;
}

public class UserInfoDto
{
    public string Id { get; set; } = string.Empty;
    public string? UserName { get; set; }
    public string? Email { get; set; }
    public bool EmailConfirmed { get; set; }
    public string? PhoneNumber { get; set; }
    public string? AvatarUrl { get; set; }
}

public class RefreshTokenResponseDto
{
    public string AccessToken { get; set; } = string.Empty;
}

