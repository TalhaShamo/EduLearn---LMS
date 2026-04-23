using MassTransit;
using MediatR;
using EduLearn.Enrollment.API.Application.Commands;
using EduLearn.Shared.Events;
using Microsoft.Extensions.Logging;

namespace EduLearn.Enrollment.API.Consumers;

// Listens for PaymentSucceededEvent published by Payment.API saga
// When payment is confirmed, this creates the paid enrollment
public class PaymentSucceededConsumer : IConsumer<PaymentSucceededEvent>
{
    private readonly IMediator _mediator;
    private readonly ILogger<PaymentSucceededConsumer> _logger;

    public PaymentSucceededConsumer(IMediator mediator, ILogger<PaymentSucceededConsumer> logger)
    { _mediator = mediator; _logger = logger; }

    public async Task Consume(ConsumeContext<PaymentSucceededEvent> context)
    {
        var evt = context.Message;
        _logger.LogInformation(
            "Payment succeeded for Student {StudentId}, Course {CourseId}. Creating enrollment.",
            evt.StudentId, evt.CourseId);

        // Dispatch command to create the paid enrollment
        // TotalLessons defaults to 0 here — real impl would call Course.API or store in event
        await _mediator.Send(new CreatePaidEnrollmentCommand(
            evt.StudentId, evt.CourseId,
            TotalLessons: 0,       // Course.API should be queried for this in production
            PaymentId: evt.OrderId,
            StudentEmail: "student@email.com",
            StudentName:  "Student",
            CourseTitle:  "Course"));
    }
}
