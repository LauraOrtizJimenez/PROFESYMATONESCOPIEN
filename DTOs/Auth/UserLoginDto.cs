using System.ComponentModel.DataAnnotations;

namespace Proyecto1.DTOs.Auth
{
    public class UserLoginDto
    {
        [Required]
        public string Username { get; set; } = string.Empty;
        
        [Required]
        public string Password { get; set; } = string.Empty;
    }
}