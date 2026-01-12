namespace Hesapix.Models.Entities
{
    public class User
    {
        public int Id { get; set; }
        public string Email { get; set; }
        public string PasswordHash { get; set; }
        public string FullName { get; set; }
        public string PhoneNumber { get; set; }
        public DateTime CreatedDate { get; set; }
        public bool IsActive { get; set; }
        public string? CompanyName { get; set; }
        public string? TaxNumber { get; set; }

        // Navigation Properties
        public virtual Subscription Subscription { get; set; }
        public virtual ICollection<Stock> Stocks { get; set; }
        public virtual ICollection<Sale> Sales { get; set; }
        public virtual ICollection<Payment> Payments { get; set; }
    }
}