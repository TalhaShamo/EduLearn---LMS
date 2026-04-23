namespace EduLearn.Shared.Models;

// Standard API envelope wrapping every response
// Angular services will always receive this shape
public class ApiResponse<T>
{
    public bool   Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public T?     Data    { get; set; }
    public int    StatusCode { get; set; }

    // Factory — success with data
    public static ApiResponse<T> Ok(T data, string message = "Success") =>
        new() { Success = true, Message = message, Data = data, StatusCode = 200 };

    // Factory — success without data (created, deleted, etc.)
    public static ApiResponse<T> Created(T data, string message = "Created successfully") =>
        new() { Success = true, Message = message, Data = data, StatusCode = 201 };

    // Factory — failure  
    public static ApiResponse<T> Fail(string message, int statusCode = 400) =>
        new() { Success = false, Message = message, StatusCode = statusCode };
}

// Paginated list wrapper for catalog-style endpoints
public class PagedResponse<T>
{
    public IEnumerable<T> Items      { get; set; } = [];
    public int            Page       { get; set; }
    public int            PageSize   { get; set; }
    public int            TotalCount { get; set; }
    public int            TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool           HasNext    => Page < TotalPages;
    public bool           HasPrev    => Page > 1;
}
