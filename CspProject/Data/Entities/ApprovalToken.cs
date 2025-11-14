// Data/Entities/ApprovalToken.cs

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CspProject.Data.Entities
{
    public class ApprovalToken
    {
        [Key]
        public Guid Token { get; set; } // Benzersiz, tahmin edilemez token (GUID)

        public int DocumentId { get; set; }
        [ForeignKey("DocumentId")]
        public Document Document { get; set; }

        public string Action { get; set; } // "Approve" veya "Reject"
        public bool IsUsed { get; set; } = false;
        public DateTime ExpiryDate { get; set; } = DateTime.UtcNow.AddDays(7); // Token 7 gün geçerli
    }
}