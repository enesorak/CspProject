// ***********************************************************************************
// File: CspProject/Data/Entities/Document.cs
// Description: Represents the 'Documents' table in the database.
// Author: Enes Orak
// ***********************************************************************************


using System.ComponentModel.DataAnnotations;
 
namespace CspProject.Data.Entities;
public class Document
{
    [Key]
    public int Id { get; set; }
    public string DocumentName { get; set; } = "New FMEA Document";
    public string? FmeaId { get; set; }
    public string? ProductPart { get; set; }
    public string? ProjectName { get; set; }
    public string? Team { get; set; }
    public string? ResponsibleParty { get; set; }
    public string? ApprovedBy { get; set; }
    public string Version { get; set; } = "1.0.0";
    
    public string Status { get; set; } = "Draft";
    public byte[]? Content { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.Now;
    public DateTime ModifiedDate { get; set; } = DateTime.Now;
}