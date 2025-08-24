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


    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite("Data Source=csp_database.db");
    }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>().HasData(
            new User { Id = 1, Name = "Enes Orak (Author)", Role = "Author" },
            new User { Id = 2, Name = "John Smith (Approver)", Role = "Approver" }
        );
    }
}