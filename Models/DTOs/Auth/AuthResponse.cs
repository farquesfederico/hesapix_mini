namespace Hesapix.Models.DTOs.Auth
{
    public class AuthResponse
    {
        public string Token { get; set; }
        public UserDto User { get; set; }
        public bool HasActiveSubscription { get; set; }
        public DateTime? SubscriptionEndDate { get; set; }
    }
}