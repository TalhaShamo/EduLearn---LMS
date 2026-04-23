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
