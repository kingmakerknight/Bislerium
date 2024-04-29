using Bislerium.Application.Interfaces.Repositories.Base;
using Bislerium.Application.Interfaces.Services;
using Bislerium.Infrastructure.Implementation.Repository.Base;
using Bislerium.Infrastructure.Implementation.Services;
using Bislerium.Infrastructure.Persistence;
using Bislerium.Infrastructure.Persistence.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Bislerium.Infrastructure.Dependency;

public static class InfrastructureService
{
    public static IServiceCollection AddInfrastructureService(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(connectionString,
                b => b.MigrationsAssembly("Bislerium.Infrastructure")));

        services.AddScoped<IDbInitializer, DbInitializer>();

        services.AddTransient<IGenericRepository, GenericRepository>();
        services.AddTransient<IFileUploadService, FileUploadService>();

        return services;
    }
}