// ***********************************************************************************
// File: CspProject/Data/Entities/Document.cs
// Description: Represents the 'Documents' table in the database.
// Author: Enes Orak
// ***********************************************************************************


using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CspProject.Data.Entities;
public class Document
{
    [Key]
    public int Id { get; set; }
    public string DocumentName { get; set; } = "New FMEA Document";
    
    public int AuthorId { get; set; }
    [ForeignKey("AuthorId")]
    public User Author { get; set; }
    
    public int? ApproverId { get; set; }
    [ForeignKey("ApproverId")]
    public User? Approver { get; set; }
    
    
    public string? FmeaId { get; set; }
    public string? ProductPart { get; set; }
    public string? ProjectName { get; set; }
    public string? Team { get; set; }
    public string? ResponsibleParty { get; set; }
    public string? ApprovedBy { get; set; }
    public string Version { get; set; } = "0.0.1";
    
    public string Status { get; set; } = "Draft";
    public byte[]? Content { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.Now;
    public DateTime ModifiedDate { get; set; } = DateTime.Now;
}