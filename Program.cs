using System.Text;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SegurosApi.Data;
using SegurosApi.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
static string NormalizeConnectionString(string raw)
{
    if (raw.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) ||
        raw.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
    {
        var uri = new Uri(raw);
        var userInfo = uri.UserInfo.Split(':', 2);
        var username = userInfo.Length > 0 ? Uri.UnescapeDataString(userInfo[0]) : "";
        var password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : "";

        var database = uri.AbsolutePath.Trim('/');
        if (string.IsNullOrWhiteSpace(database))
            database = "postgres";

        var port = uri.IsDefaultPort ? 5432 : uri.Port;

        return $"Host={uri.Host};Port={port};Database={database};Username={username};Password={password};Ssl Mode=Require;Trust Server Certificate=true";
    }

    return raw;
}

var defaultConnectionRaw = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(defaultConnectionRaw))
    throw new InvalidOperationException("Missing connection string 'ConnectionStrings:DefaultConnection'. This project requires PostgreSQL.");
var defaultConnection = NormalizeConnectionString(defaultConnectionRaw);

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseNpgsql(defaultConnection);
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
        var issuer = builder.Configuration["Jwt:Issuer"] ?? "SegurosApi";
        var audience = builder.Configuration["Jwt:Audience"] ?? "SegurosApi";
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = issuer,
            ValidAudience = audience,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ?? "YourSuperSecretKeyThatIsAtLeast32CharactersLong!"))
        };
    });

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

var swaggerEnabled = app.Environment.IsDevelopment() ||
                     string.Equals(builder.Configuration["Swagger:Enabled"], "true", StringComparison.OrdinalIgnoreCase);

if (swaggerEnabled)
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseExceptionHandler(exceptionApp =>
{
    exceptionApp.Run(async context =>
    {
        var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;

        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/problem+json";

        var traceId = context.TraceIdentifier;
        var title = "An unexpected error occurred.";

        if (exception is not null)
        {
            app.Logger.LogError(exception, "Unhandled exception. TraceId: {TraceId}", traceId);
        }

        await context.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = title,
            Extensions = { ["traceId"] = traceId }
        });
    });
});

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

// Ensure database is created and migrated
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();

    if (db.Database.IsNpgsql())
    {
        db.Database.ExecuteSqlRaw(@"
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_class WHERE relname = 'Roles_RoleId_seq') THEN
        CREATE SEQUENCE ""Roles_RoleId_seq"";
    END IF;

    IF EXISTS (
        SELECT 1
        FROM information_schema.columns
        WHERE table_schema = 'public'
          AND table_name = 'Roles'
          AND column_name = 'RoleId'
          AND column_default IS NULL
    ) THEN
        ALTER TABLE ""Roles"" ALTER COLUMN ""RoleId"" SET DEFAULT nextval('""Roles_RoleId_seq""');
        ALTER SEQUENCE ""Roles_RoleId_seq"" OWNED BY ""Roles"".""RoleId"";
    END IF;

    PERFORM setval('""Roles_RoleId_seq""', COALESCE((SELECT MAX(""RoleId"") FROM ""Roles""), 0) + 1, false);

    IF NOT EXISTS (SELECT 1 FROM pg_class WHERE relname = 'Users_UserId_seq') THEN
        CREATE SEQUENCE ""Users_UserId_seq"";
    END IF;

    IF EXISTS (
        SELECT 1
        FROM information_schema.columns
        WHERE table_schema = 'public'
          AND table_name = 'Users'
          AND column_name = 'UserId'
          AND column_default IS NULL
    ) THEN
        ALTER TABLE ""Users"" ALTER COLUMN ""UserId"" SET DEFAULT nextval('""Users_UserId_seq""');
        ALTER SEQUENCE ""Users_UserId_seq"" OWNED BY ""Users"".""UserId"";
    END IF;

    PERFORM setval('""Users_UserId_seq""', COALESCE((SELECT MAX(""UserId"") FROM ""Users""), 0) + 1, false);

    IF EXISTS (
        SELECT 1
        FROM information_schema.columns
        WHERE table_schema = 'public'
          AND table_name = 'Users'
          AND column_name = 'CreatedAt'
          AND data_type = 'text'
    ) THEN
        ALTER TABLE ""Users"" ALTER COLUMN ""CreatedAt"" TYPE timestamp with time zone
            USING (""CreatedAt""::timestamptz);
    END IF;
END $$;
");
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseCors("Frontend");

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
