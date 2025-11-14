using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CspProject.Data.Entities
{
    public class AuditLog
    {
        [Key]
        public int Id { get; set; }

        public int DocumentId { get; set; }
        [ForeignKey("DocumentId")]
        public Document Document { get; set; }

        public int UserId { get; set; }
        [ForeignKey("UserId")]
        public User User { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.Now;
        
        public string FieldChanged { get; set; } // Örn: "Cell: A5" veya "Status"
        public string OldValue { get; set; }
        public string NewValue { get; set; }
        public string Revision { get; set; } // Değişikliğin yapıldığı andaki doküman versiyonu
        public string Rationale { get; set; } // Değişiklik için girilen gerekçe (gelecek adımda eklenecek)
    }
}