using System.ComponentModel.DataAnnotations;

namespace Hesapix.Models.Entities
{
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(255)]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string FullName { get; set; } = string.Empty;

        [MaxLength(20)]
        public string PhoneNumber { get; set; } = string.Empty;

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public bool IsActive { get; set; } = true;

        [MaxLength(200)]
        public string? CompanyName { get; set; }

        [MaxLength(50)]
        public string? TaxNumber { get; set; }

        // Email Verification
        public bool EmailVerified { get; set; } = false;

        [MaxLength(10)]
        public string? EmailVerificationCode { get; set; }

        public DateTime? EmailVerificationExpiry { get; set; }

        // Password Reset
        [MaxLength(200)]
        public string? PasswordResetToken { get; set; }

        public DateTime? PasswordResetExpiry { get; set; }

        // Security
        public int FailedLoginAttempts { get; set; } = 0;

        public DateTime? LastLoginDate { get; set; }

        public DateTime? LastFailedLoginDate { get; set; }

        public DateTime? LockoutEnd { get; set; }

        // Audit
        public DateTime? UpdatedDate { get; set; }

        public string? UpdatedBy { get; set; }

        // Navigation Properties
        public virtual Subscription? Subscription { get; set; }
        public virtual ICollection<Stock> Stocks { get; set; } = new List<Stock>();
        public virtual ICollection<Sale> Sales { get; set; } = new List<Sale>();
        public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
    }
}