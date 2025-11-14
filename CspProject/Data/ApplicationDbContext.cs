// ***********************************************************************************
// File: CspProject/Data/ApplicationDbContext.cs
// Description: The Entity Framework Core database context class.
// Author: Enes Orak
// ***********************************************************************************

using CspProject.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace CspProject.Data;

public class ApplicationDbContext : DbContext
{
    public DbSet<Document> Documents { get; set; }
    
    public DbSet<User> Users { get; set; } // YENİ

    public DbSet<EmailSetting> EmailSettings { get; set; }

    public DbSet<ApprovalToken> ApprovalTokens { get; set; } 
    
    public DbSet<AuditLog> AuditLogs { get; set; }


    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite("Data Source=csp_database.db");
    }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        
        
        modelBuilder.Entity<EmailSetting>().HasData(
            new EmailSetting
            {
                Id = 1,
                // SMTP Ayarları
                SmtpServer = "smtp.gmail.com",
                SmtpPort = 587,
                // YENİ EKLENEN SATIRLAR
                ImapServer = "imap.gmail.com",
                ImapPort = 993,
                // ---
                SenderEmail = "approval4testing@gmail.com",
                SenderName = "CSP Application",
                Password = "dqlb yeca kxwr drmb", // Kullanıcı tarafından girilecek
                EnableSsl = true
            }
        );
    }
}