using System.ComponentModel.DataAnnotations;

namespace TutorLinkBe.Dto;

public class RevokeTokenDto
{
    [Required(ErrorMessage = "Refresh Token is required.")]
    public string Token { get; set; }
}