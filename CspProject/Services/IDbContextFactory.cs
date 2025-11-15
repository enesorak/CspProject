// Services/IDbContextFactory.cs

using CspProject.Data;
using Microsoft.Extensions.DependencyInjection;

namespace CspProject.Services;

public interface IDbContextFactory
{
    ApplicationDbContext CreateDbContext();
}

// Services/DbContextFactory.cs
public class DbContextFactory(IServiceProvider serviceProvider) : IDbContextFactory
{
    public ApplicationDbContext CreateDbContext()
    {
        var scope = serviceProvider.CreateScope();
        return scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    }
}