using MvpMakerApi.Infrastructure;
using MvpMakerApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// Add Infrastructure (DB, Repos, Services)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
// Fallback for Docker if env var is set
var dbHost = Environment.GetEnvironmentVariable("DB_HOST");
var dbName = Environment.GetEnvironmentVariable("DB_NAME");
var dbPassword = Environment.GetEnvironmentVariable("DB_PASSWORD");
if (!string.IsNullOrEmpty(dbHost))
{
    connectionString = $"Server={dbHost};Database={dbName};User=root;Password={dbPassword};";
}

builder.Services.AddInfrastructure(connectionString!);

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        builder =>
        {
            builder.AllowAnyOrigin()
                   .AllowAnyMethod()
                   .AllowAnyHeader();
        });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
app.MapOpenApi();
app.MapScalarApiReference();

app.UseCors("AllowAll");

app.UseAuthorization();

app.MapControllers();

// Apply migrations automatically at startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    try
    {
        db.Database.Migrate();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Could not migrate database: {ex.Message}");
    }
}

app.Run();
