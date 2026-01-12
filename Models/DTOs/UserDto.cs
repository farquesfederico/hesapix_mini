namespace Hesapix.Models.DTOs
{
    public class UserDto
    {
        public int Id { get; set; }
        public string Email { get; set; }
        public string FullName { get; set; }
        public string PhoneNumber { get; set; }
        public string? CompanyName { get; set; }
        public string? TaxNumber { get; set; }
    }
}