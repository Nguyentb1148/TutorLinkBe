using System.ComponentModel.DataAnnotations;

namespace TutorLinkBe.Application.Dto;

public class RevokeTokenDto
{
    [Required(ErrorMessage = "Refresh Token is required.")]
    public string Token { get; set; }
}