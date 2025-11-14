using System.ComponentModel.DataAnnotations;

namespace CspProject.Data.Entities;

public class User
{
    [Key]
    public int Id { get; set; }
    public string Name { get; set; }
   // public string? Role { get; set; } // "Author", "Approver", "Viewer" 
    
    public string Email { get; set; } 

}