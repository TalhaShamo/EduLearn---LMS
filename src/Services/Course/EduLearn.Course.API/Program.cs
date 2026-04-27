using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MassTransit;
using FluentValidation;
using FluentValidation.AspNetCore;
using Serilog;
using System.Text;
using EduLearn.Course.API.Application.Interfaces;
using EduLearn.Course.API.Infrastructure.Data;
using EduLearn.Course.API.Infrastructure.Repositories;
using EduLearn.Course.API.Infrastructure.Services;
using EduLearn.Shared.Middleware;

var builder = WebApplication.CreateBuilder(args);

// ── SERILOG ───────────────────────────────────────────────────
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/course-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();
builder.Host.UseSerilog();

// ── EF CORE ───────────────────────────────────────────────────
builder.Services.AddDbContext<CourseDbContext>(opts =>
    opts.UseSqlServer(builder.Configuration.GetConnectionString("CourseDb")));

// ── REPOSITORIES via DI ───────────────────────────────────────
builder.Services.AddScoped<ICourseRepository,   CourseRepository>();
builder.Services.AddScoped<ISectionRepository,  SectionRepository>();
builder.Services.AddScoped<ILessonRepository,   LessonRepository>();
builder.Services.AddScoped<IVideoStorageService, LocalVideoStorageService>();

// ── MEDIATR ───────────────────────────────────────────────────
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));

// ── FLUENT VALIDATION ─────────────────────────────────────────
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);

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

// ── MASSTRANSIT (RabbitMQ) ────────────────────────────────────
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

// ── CORS ──────────────────────────────────────────────────────
builder.WebHost.ConfigureKestrel(o =>
    o.Limits.MaxRequestBodySize = 4L * 1024 * 1024 * 1024); // 4 GB

builder.Services.AddCors(opts =>
    opts.AddPolicy("Angular", p =>
        p.WithOrigins("http://localhost:4200", "https://localhost:7094")
         .AllowAnyHeader().AllowAnyMethod().AllowCredentials()));

// ── SWAGGER ───────────────────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "EduLearn Course API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization", Type = SecuritySchemeType.Http,
        Scheme = "bearer", BearerFormat = "JWT", In = ParameterLocation.Header
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        { new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } }, [] }
    });
});

// Allow large video uploads
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(o =>
    o.MultipartBodyLengthLimit = 4L * 1024 * 1024 * 1024); // 4 GB

builder.Services.AddControllers();

var app = builder.Build();

// ── AUTO-MIGRATE ──────────────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<CourseDbContext>();
    db.Database.Migrate();
}

// ── PIPELINE ──────────────────────────────────────────────────
app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseSerilogRequestLogging();
app.UseCors("Angular");
app.UseStaticFiles(); // Serve wwwroot (video files)

if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Docker"))
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Course API v1"));
}

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
