// Services/IDbContextFactory.cs

using CspProject.Data;

namespace CspProject.Services.Infrastructure;

public interface IDbContextFactory
{
    ApplicationDbContext CreateDbContext();
}

// Services/DbContextFactory.cs