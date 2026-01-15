using System.ComponentModel.DataAnnotations;

namespace Hesapix.Models.DTOs.Auth
{
    public class LoginRequest
    {
        [Required(ErrorMessage = "Email zorunludur")]
        [EmailAddress(ErrorMessage = "Geçerli bir email adresi giriniz")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Şifre zorunludur")]
        public string Password { get; set; } = string.Empty;
    }
}