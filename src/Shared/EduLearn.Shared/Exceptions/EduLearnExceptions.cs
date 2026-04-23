namespace EduLearn.Shared.Exceptions;

// Base class for all EduLearn domain exceptions
// Using OOP inheritance — every domain exception extends this
public abstract class EduLearnException : Exception
{
    public int StatusCode { get; }      // HTTP status to return
    public string ErrorCode { get; }    // Machine-readable code for frontend

    protected EduLearnException(string message, int statusCode, string errorCode)
        : base(message)
    {
        StatusCode = statusCode;
        ErrorCode = errorCode;
    }
}

// 404 — Resource does not exist
public class NotFoundException : EduLearnException
{
    public NotFoundException(string resourceName, object key)
        : base($"{resourceName} with key '{key}' was not found.", 404, "NOT_FOUND") { }
}

// 409 — Conflict (e.g., duplicate email, already enrolled)
public class ConflictException : EduLearnException
{
    public ConflictException(string message)
        : base(message, 409, "CONFLICT") { }
}

// 403 — Forbidden (role/ownership check failed)
public class ForbiddenException : EduLearnException
{
    public ForbiddenException(string message = "You do not have permission to perform this action.")
        : base(message, 403, "FORBIDDEN") { }
}

// 400 — Business rule violation (e.g., refund window expired)
public class BusinessRuleException : EduLearnException
{
    public BusinessRuleException(string message)
        : base(message, 400, "BUSINESS_RULE_VIOLATION") { }
}

// 422 — Pre-condition not met (e.g., prerequisite course not complete)
public class PreConditionException : EduLearnException
{
    public PreConditionException(string message)
        : base(message, 422, "PRECONDITION_FAILED") { }
}
