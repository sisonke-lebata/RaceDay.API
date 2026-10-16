using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using RaceDay.API.Data;
using RaceDay.API.Middleware;
using RaceDay.API.Services;

var builder = WebApplication.CreateBuilder(args);

// ─── Database ───────────────────────────────────────
builder.Services.AddDbContext<AppDbContext>(o =>
    o.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ─── JWT Service ────────────────────────────────────
builder.Services.AddScoped<IJwtService, JwtService>();
var jwtKey = builder.Configuration["Jwt:Key"]!;

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o => o.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
    });

builder.Services.AddAuthorization();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// ─── Swagger with JWT ───────────────────────────────
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "RaceDay API",
        Version = "v1",
        Description = "RaceDay Event Management System "
    });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Enter 'Bearer {token}'",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// ─── Swagger (always on for demos) ──────────────────
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "RaceDay API v1");
});

// ─── Middleware pipeline ────────────────────────────
app.UseHttpsRedirection();
app.UseMiddleware<SessionValidationMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// ─── Migrate + Seed (with logging) ──────────────────
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var db = services.GetRequiredService<AppDbContext>();
        Console.WriteLine(">>> Applying migrations...");
        db.Database.Migrate();
        Console.WriteLine(">>> Migrations applied.");

        Console.WriteLine(">>> Seeding database...");
        AppDbContext.Seed(db);
        Console.WriteLine(">>> Seed complete.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($">>> STARTUP ERROR: {ex.Message}");
        Console.WriteLine(ex.StackTrace);
    }
}

Console.WriteLine(">>> RaceDay API is starting...");
app.Run();