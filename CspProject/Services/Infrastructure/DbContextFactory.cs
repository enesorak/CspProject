using CspProject.Data;
using Microsoft.Extensions.DependencyInjection;

namespace CspProject.Services.Infrastructure;

public class DbContextFactory(IServiceProvider serviceProvider) : IDbContextFactory
{
    public ApplicationDbContext CreateDbContext()
    {
        var scope = serviceProvider.CreateScope();
        return scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    }
}