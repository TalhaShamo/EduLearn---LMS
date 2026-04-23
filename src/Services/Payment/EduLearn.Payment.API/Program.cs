using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MassTransit;
using Serilog;
using System.Text;
using EduLearn.Payment.API.Infrastructure.Data;
using EduLearn.Payment.API.Saga;
using EduLearn.Shared.Middleware;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration).Enrich.FromLogContext()
    .WriteTo.Console().WriteTo.File("logs/payment-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();
builder.Host.UseSerilog();

builder.Services.AddDbContext<PaymentDbContext>(opts =>
    opts.UseSqlServer(builder.Configuration.GetConnectionString("PaymentDb")));

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

// ── MASSTRANSIT + SAGA ────────────────────────────────────────
builder.Services.AddMassTransit(x =>
{
    // Register the saga state machine with EF Core persistence
    x.AddSagaStateMachine<PaymentSagaStateMachine, PaymentSagaState>()
     .EntityFrameworkRepository(r =>
     {
         r.ExistingDbContext<PaymentDbContext>();
         r.UseSqlServer(); // Uses optimistic concurrency via ETag
     });

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
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "EduLearn Payment API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    { Name = "Authorization", Type = SecuritySchemeType.Http, Scheme = "bearer", In = ParameterLocation.Header });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {{ new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } }, [] }});
});

builder.Services.AddControllers();

var app = builder.Build();

// Auto-migrate: creates saga state table + payment orders table
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();
    db.Database.Migrate();
}

app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseSerilogRequestLogging();
app.UseCors("Angular");

if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Docker"))
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Payment API v1"));
}

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
