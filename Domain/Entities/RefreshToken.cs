using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;
using TutorLinkBe.Domain.Entities;

namespace TutorLinkBe.Domain.Entities
{
    public class RefreshToken
    {
        public Guid Id { get; set; } 
        public string? Token { get; set; }
        public string? UserId { get; set; }
        public virtual ApplicationUser? User { get; set; }
        public DateTime IssuedUtc { get; set; }
        public DateTime ExpiresUtc { get; set; }
        public bool IsRevoked { get; set; }
        public string? ReplacedByToken { get; set; }
        public string? JwtId { get; set; }
        
        // for multiple devices
        /*
            public string DeviceName { get; set; } // e.g., "Web", "Mobile"
            public string IPAddress { get; set; }
            public DateTime LastUsed { get; set; } 
         */
    }
}