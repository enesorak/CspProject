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

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite("Data Source=csp_database.db");
    }
}