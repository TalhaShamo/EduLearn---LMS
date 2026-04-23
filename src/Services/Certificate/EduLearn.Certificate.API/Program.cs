using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MassTransit;
using Serilog;
using System.Text;
using EduLearn.Certificate.API.Consumers;
using EduLearn.Certificate.API.Infrastructure.Data;
using EduLearn.Certificate.API.Services;
using EduLearn.Shared.Middleware;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration).Enrich.FromLogContext()
    .WriteTo.Console().WriteTo.File("logs/certificate-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();
builder.Host.UseSerilog();

builder.Services.AddDbContext<CertificateDbContext>(opts =>
    opts.UseSqlServer(builder.Configuration.GetConnectionString("CertificateDb")));

// DI: PDF generation service
builder.Services.AddScoped<CertificatePdfService>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opts => opts.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true, ValidateAudience = true, ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer      = builder.Configuration["Jwt:Issuer"],
        ValidAudience    = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(builder.Configuration["Jwt:SecretKey"]!))
    });
builder.Services.AddAuthorization();

// MassTransit: consume CourseCompletedEvent to trigger certificate generation
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<CourseCompletedConsumer>();

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

builder.Services.AddCors(opts =>
    opts.AddPolicy("Angular", p =>
        p.WithOrigins("http://localhost:4200").AllowAnyHeader().AllowAnyMethod().AllowCredentials()));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "EduLearn Certificate API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    { Name = "Authorization", Type = SecuritySchemeType.Http, Scheme = "bearer", In = ParameterLocation.Header });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {{ new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } }, [] }});
});

builder.Services.AddControllers();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<CertificateDbContext>();
    db.Database.Migrate();
}

app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseSerilogRequestLogging();
app.UseCors("Angular");
app.UseStaticFiles(); // Serve certificate PDFs from wwwroot

if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Docker"))
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Certificate API v1"));
}

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
