using System.ComponentModel.DataAnnotations;

namespace TutorLinkBe.Application.DTOs;

using System.ComponentModel.DataAnnotations;

public class LoginViaGoogleDto
{
    [Required(ErrorMessage = "Google credential is required.")]
    public string Credential { get; set; }
}
