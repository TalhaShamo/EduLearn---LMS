namespace EduLearn.Identity.API.Domain.Enums;

// User roles in EduLearn — stored as string in DB for readability
public enum UserRole
{
    Student    = 1,   // Can browse, enroll, watch, quiz, certify
    Instructor = 2,   // Can create courses, grade assignments, view analytics
    Admin      = 3    // Platform administrator — full access
}
