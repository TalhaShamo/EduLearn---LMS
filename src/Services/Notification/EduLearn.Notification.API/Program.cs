using MassTransit;
using Serilog;
using EduLearn.Notification.API.Consumers;
using EduLearn.Notification.API.Services;
using EduLearn.Shared.Middleware;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration).Enrich.FromLogContext()
    .WriteTo.Console().WriteTo.File("logs/notification-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();
builder.Host.UseSerilog();

// DI: register the MailKit email service
builder.Services.AddSingleton<EmailService>();

// MassTransit: register ALL notification consumers
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<UserRegisteredConsumer>();
    x.AddConsumer<StudentEnrolledConsumer>();
    x.AddConsumer<PaymentSucceededConsumer>();
    x.AddConsumer<PaymentFailedConsumer>();
    x.AddConsumer<QuizGradedConsumer>();
    x.AddConsumer<AssignmentGradedConsumer>();
    x.AddConsumer<CertificateIssuedConsumer>();

    x.UsingRabbitMq((ctx, cfg) =>
    {
        cfg.Host(builder.Configuration["RabbitMQ:Host"], "/", h =>
        {
            h.Username(builder.Configuration["RabbitMQ:Username"]!);
            h.Password(builder.Configuration["RabbitMQ:Password"]!);
        });
        // Each consumer gets its own queue based on its type name
        cfg.ConfigureEndpoints(ctx);
    });
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Docker"))
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Notification API v1"));
}

app.MapControllers();
app.Run();
