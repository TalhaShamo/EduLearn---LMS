using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using StackExchange.Redis;
using System.Text;
using EduLearn.Shared.Middleware;

var builder = WebApplication.CreateBuilder(args);

// ── SERILOG: structured logging to console and file ──────────
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/gateway-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// ── OCELOT: load route configuration ─────────────────────────
// ocelot.json defines how to route /api/v1/auth/* → identity-api, etc.
builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);

// ── JWT: validate tokens at the gateway level ─────────────────
// All services also validate independently for defence-in-depth
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
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

// ── REDIS: used for rate limiting counters ────────────────────
builder.Services.AddSingleton<IConnectionMultiplexer>(
    ConnectionMultiplexer.Connect(builder.Configuration["Redis:ConnectionString"]!));

// ── CORS: allow Angular frontend origin ──────────────────────
builder.Services.AddCors(options =>
{
    options.AddPolicy("Angular", policy =>
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials()); // Required for HttpOnly refresh-token cookie
});

// ── OCELOT: register routing engine ──────────────────────────
builder.Services.AddOcelot(builder.Configuration);

var app = builder.Build();

// ── MIDDLEWARE PIPELINE ───────────────────────────────────────
app.UseMiddleware<GlobalExceptionMiddleware>(); // Catch gateway-level errors
app.UseSerilogRequestLogging();                 // Log every request
app.UseCors("Angular");                         // Allow Angular requests
app.UseAuthentication();
app.UseAuthorization();

// Let Ocelot handle all routing — must be last
await app.UseOcelot();
app.Run();
