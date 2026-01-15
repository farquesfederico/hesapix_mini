using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Hesapix.Models.Entities
{
    public class Subscription
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        [MaxLength(50)]
        public string PlanType { get; set; } = "Free"; // Free, Basic, Premium, Enterprise

        public DateTime StartDate { get; set; } = DateTime.UtcNow;

        public DateTime? EndDate { get; set; }

        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = "Active"; // Active, Expired, Cancelled

        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        // Navigation Property
        [ForeignKey(nameof(UserId))]
        public virtual User User { get; set; } = null!;

        // Helper method
        public bool IsActive()
        {
            return Status == "Active" &&
                   (EndDate == null || EndDate > DateTime.UtcNow);
        }
    }
}