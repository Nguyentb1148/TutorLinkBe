using System.ComponentModel.DataAnnotations;

namespace TutorLinkBe.Dto;

public class RefreshTokenRequestDto
{
    [Required(ErrorMessage = "Refresh Token is required.")]
    public string Token { get; set; }

}