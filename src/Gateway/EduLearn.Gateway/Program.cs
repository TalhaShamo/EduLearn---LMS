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

// ── CORS: allow local frontend dev servers ────────────────────
// `ng serve` may run on 4200/4201/etc, and devs often use 127.0.0.1 instead of localhost.
builder.WebHost.ConfigureKestrel(o =>
    o.Limits.MaxRequestBodySize = 4L * 1024 * 1024 * 1024); // 4 GB for video uploads

builder.Services.AddCors(options =>
{
    options.AddPolicy("Angular", policy =>
        policy.SetIsOriginAllowed(origin =>
        {
            if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri)) return false;
            return uri.Scheme is "http" or "https"
                && (uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase)
                    || uri.Host.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase));
        })
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials()); // Required for HttpOnly refresh-token cookie
});

// ── OCELOT: register routing engine ──────────────────────────
builder.Services.AddOcelot(builder.Configuration);

// Allow large video uploads through gateway
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(o =>
    o.MultipartBodyLengthLimit = 4L * 1024 * 1024 * 1024); // 4 GB

var app = builder.Build();

// ── MIDDLEWARE PIPELINE ───────────────────────────────────────
app.UseMiddleware<GlobalExceptionMiddleware>(); // Catch gateway-level errors
app.UseSerilogRequestLogging();                 // Log every request

// CORS must be before Ocelot to handle preflight requests
app.UseCors("Angular");

// Add CORS headers to all responses (including Ocelot proxied responses)
app.Use(async (context, next) =>
{
    var origin = context.Request.Headers["Origin"].ToString();
    if (!string.IsNullOrEmpty(origin))
    {
        if (Uri.TryCreate(origin, UriKind.Absolute, out var uri) &&
            uri.Scheme is "http" or "https" &&
            (uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
             uri.Host.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase)))
        {
            context.Response.Headers["Access-Control-Allow-Origin"] = origin;
            context.Response.Headers["Access-Control-Allow-Credentials"] = "true";
            context.Response.Headers["Access-Control-Allow-Headers"] = context.Request.Headers["Access-Control-Request-Headers"].ToString();
            context.Response.Headers["Access-Control-Allow-Methods"] = "GET, POST, PUT, PATCH, DELETE, OPTIONS";
        }
    }
    
    if (context.Request.Method == "OPTIONS")
    {
        context.Response.StatusCode = 204;
        return;
    }
    
    await next();
});

app.UseAuthentication();
app.UseAuthorization();

// Let Ocelot handle all routing — must be last
await app.UseOcelot();
app.Run();
