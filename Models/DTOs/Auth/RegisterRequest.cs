namespace Hesapix.Models.DTOs.Auth;

public class RegisterRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string? TaxNumber { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Address { get; set; }
}