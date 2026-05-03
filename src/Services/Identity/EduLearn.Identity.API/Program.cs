using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MassTransit;
using FluentValidation;
using FluentValidation.AspNetCore;
using Serilog;
using System.Text;
using EduLearn.Identity.API.Application.Interfaces;
using EduLearn.Identity.API.Application.Validators;
using EduLearn.Identity.API.Infrastructure.Data;
using EduLearn.Identity.API.Infrastructure.Repositories;
using EduLearn.Identity.API.Infrastructure.Services;
using EduLearn.Shared.Middleware;

var builder = WebApplication.CreateBuilder(args);

// ── SERILOG ───────────────────────────────────────────────────
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/identity-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();
builder.Host.UseSerilog();

// ── EF CORE: connect to EduLearnIdentityDb ───────────────────
builder.Services.AddDbContext<IdentityDbContext>(opts =>
    opts.UseSqlServer(builder.Configuration.GetConnectionString("IdentityDb")));

// ── REPOSITORY PATTERN: register via DI ──────────────────────
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();

// ── JWT SERVICE ───────────────────────────────────────────────
builder.Services.AddScoped<IJwtService, JwtService>();

// ── MEDIATR: auto-discover all commands and queries ───────────
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));

// ── FLUENT VALIDATION ─────────────────────────────────────────
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<RegisterRequestValidator>();

// ── HTTP CLIENT FACTORY ───────────────────────────────────────
builder.Services.AddHttpClient();

// ── JWT AUTHENTICATION ────────────────────────────────────────
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opts =>
    {
        opts.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer           = true,
            ValidateAudience         = true,
            ValidateLifetime         = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer              = builder.Configuration["Jwt:Issuer"],
            ValidAudience            = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey         = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:SecretKey"]!))
        };
    });

builder.Services.AddAuthorization();

// ── MASSTRANSIT + RABBITMQ ────────────────────────────────────
builder.Services.AddMassTransit(x =>
{
    x.UsingRabbitMq((ctx, cfg) =>
    {
        cfg.Host(builder.Configuration["RabbitMQ:Host"], "/", h =>
        {
            h.Username(builder.Configuration["RabbitMQ:Username"]!);
            h.Password(builder.Configuration["RabbitMQ:Password"]!);
        });
        cfg.ConfigureEndpoints(ctx);
    });
});

// ── CORS: allow local frontend dev servers ────────────────────
// `ng serve` may run on 4200/4201/etc, and devs often use 127.0.0.1 instead of localhost.
builder.Services.AddCors(opts =>
    opts.AddPolicy("Angular", p =>
        p.SetIsOriginAllowed(origin =>
         {
             if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri)) return false;
             return uri.Scheme is "http" or "https"
                 && (uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase)
                     || uri.Host.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase));
         })
         .AllowAnyHeader()
         .AllowAnyMethod()
         .AllowCredentials()));

// ── SWAGGER with JWT bearer support ──────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "EduLearn Identity API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name         = "Authorization",
        Type         = SecuritySchemeType.Http,
        Scheme       = "bearer",
        BearerFormat = "JWT",
        In           = ParameterLocation.Header,
        Description  = "Enter your JWT access token here"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddControllers();

var app = builder.Build();

// ── AUTO-MIGRATE DATABASE ON STARTUP ─────────────────────────
// Applies any pending EF Core migrations automatically
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
    db.Database.Migrate();
}

// ── MIDDLEWARE PIPELINE ───────────────────────────────────────
app.UseMiddleware<GlobalExceptionMiddleware>(); // Global error handler
app.UseSerilogRequestLogging();
app.UseCors("Angular");

if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Docker"))
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Identity API v1"));
}

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();

// Make Program accessible for integration tests
public partial class Program { }
