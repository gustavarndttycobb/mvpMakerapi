using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MvpMakerApi.Application.Interfaces;
using MvpMakerApi.Application.Services;
using MvpMakerApi.Domain.Interfaces;
using MvpMakerApi.Infrastructure.Data;
using MvpMakerApi.Infrastructure.Repositories;

namespace MvpMakerApi.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IMvpRepository, MvpRepository>();
        services.AddScoped<IMvpService, MvpService>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<ITechnologyRepository, TechnologyRepository>();
        services.AddScoped<ITransactionRepository, TransactionRepository>();

        // Git & Drive Services
        services.AddHttpClient<IGitHubService, Services.GitHubService>();
        services.AddHttpClient<IGoogleDriveService, Services.GoogleDriveService>();

        // Payment Services
        services.AddScoped<IPaymentService, Services.StripePaymentService>();

        return services;
    }
}
