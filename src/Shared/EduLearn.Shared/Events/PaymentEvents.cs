namespace EduLearn.Shared.Events;

// Published when a student initiates a paid course purchase
// Consumed by the Payment Saga to begin orchestration
public record PaymentInitiatedEvent(
    Guid OrderId,       // Saga correlation ID
    Guid StudentId,
    Guid CourseId,
    decimal Amount,
    string Currency,
    string RazorpayOrderId,
    DateTime InitiatedAt
);

// Published by Payment Saga when Razorpay confirms payment
// Consumed by Enrollment.API → creates enrollment
public record PaymentSucceededEvent(
    Guid OrderId,
    Guid StudentId,
    Guid CourseId,
    decimal Amount,
    string RazorpayPaymentId,
    DateTime PaidAt
);

// Published by Payment Saga when Razorpay payment fails
// Consumed by Notification.API → sends failure email
public record PaymentFailedEvent(
    Guid OrderId,
    Guid StudentId,
    Guid CourseId,
    string Reason,
    DateTime FailedAt
);
