namespace Hesapix.Models.DTOs.Auth
{
    public class UserDto
    {
        public int Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string? CompanyName { get; set; }
        public string? TaxNumber { get; set; }
        public bool EmailVerified { get; set; }
    }
}