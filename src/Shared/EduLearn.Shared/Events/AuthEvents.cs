namespace EduLearn.Shared.Events;

// Published by Identity.API when a new user registers
// Consumed by Notification.API → sends welcome email
public record UserRegisteredEvent(
    Guid UserId,
    string FullName,
    string Email,
    string Role,
    DateTime RegisteredAt
);

// Published by Identity.API when user requests email verification
// Consumed by Notification.API → sends verification email
public record EmailVerificationRequestedEvent(
    Guid UserId,
    string FullName,
    string Email,
    string VerificationLink
);

// Published by Identity.API when user requests password reset
// Consumed by Notification.API → sends password reset email
public record PasswordResetRequestedEvent(
    Guid UserId,
    string FullName,
    string Email,
    string ResetLink
);
