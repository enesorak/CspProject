using System.ComponentModel.DataAnnotations;

namespace CspProject.Data.Entities;

public class EmailSetting
{
    [Key]
    public int Id { get; set; } // Genellikle tek bir ayar seti olacağı için bu 1 olacaktır.
    public string SmtpServer { get; set; }
    public int SmtpPort { get; set; }
    public string SenderEmail { get; set; }
    public string SenderName { get; set; }
    public string Password { get; set; } // DİKKAT: Bu, şifrelenerek saklanmalıdır.
    public bool EnableSsl { get; set; }
    
    
    public string ImapServer { get; set; }
    public int ImapPort { get; set; }
}