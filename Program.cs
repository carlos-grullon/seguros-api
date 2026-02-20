using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SegurosApi.Data;
using SegurosApi.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var defaultConnection = builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=seguros.db";

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    if (defaultConnection.Contains("Host=", StringComparison.OrdinalIgnoreCase) ||
        defaultConnection.Contains("Username=", StringComparison.OrdinalIgnoreCase) ||
        defaultConnection.Contains("User Id=", StringComparison.OrdinalIgnoreCase))
    {
        options.UseNpgsql(defaultConnection);
    }
    else
    {
        options.UseSqlite(defaultConnection);
    }
});

builder.Services.AddScoped<IAuthService, AuthService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        var originsRaw = builder.Configuration["Cors:Origins"];
        var origins = (originsRaw ?? "http://localhost:5173")
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        policy
            .WithOrigins(origins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ?? "YourSuperSecretKeyThatIsAtLeast32CharactersLong!"))
        };
    });

builder.Services.AddControllers();

var app = builder.Build();

// Ensure database is created and migrated
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();
}

app.UseHttpsRedirection();

app.UseCors("Frontend");

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
