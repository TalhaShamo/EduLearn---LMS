using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using EduLearn.Identity.API.Infrastructure.Data;
using EduLearn.Shared.Models;

namespace EduLearn.Identity.API.Controllers;

[ApiController]
[Route("api/v1/admin")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly IdentityDbContext _db;
    private readonly IHttpClientFactory _httpFactory;

    public AdminController(IdentityDbContext db, IHttpClientFactory httpFactory)
    {
        _db = db;
        _httpFactory = httpFactory;
    }

    // GET /api/v1/admin/stats
    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        // Get user stats from Identity DB
        var totalUsers = await _db.Users.CountAsync();
        var activeUsers = await _db.Users.CountAsync(u => u.IsActive);
        
        // Get JWT token from request header to forward to other services
        var token = Request.Headers["Authorization"].ToString();
        
        // Get course stats from Course API
        var httpClient = _httpFactory.CreateClient();
        int totalCourses = 0;
        int publishedCourses = 0;
        int pendingCourses = 0;
        
        try
        {
            // Get all published courses
            var courseResponse = await httpClient.GetAsync("http://course-api:8080/api/v1/courses");
            if (courseResponse.IsSuccessStatusCode)
            {
                var courseJson = await courseResponse.Content.ReadAsStringAsync();
                var courseData = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(courseJson);
                
                if (courseData.TryGetProperty("data", out var data) && data.ValueKind == System.Text.Json.JsonValueKind.Array)
                {
                    publishedCourses = data.GetArrayLength();
                }
            }
            
            // Get pending courses
            var pendingRequest = new HttpRequestMessage(HttpMethod.Get, "http://course-api:8080/api/v1/courses/pending");
            if (!string.IsNullOrEmpty(token))
            {
                pendingRequest.Headers.Add("Authorization", token);
            }
            
            var pendingResponse = await httpClient.SendAsync(pendingRequest);
            if (pendingResponse.IsSuccessStatusCode)
            {
                var pendingJson = await pendingResponse.Content.ReadAsStringAsync();
                var pendingData = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(pendingJson);
                
                if (pendingData.TryGetProperty("data", out var data) && data.ValueKind == System.Text.Json.JsonValueKind.Array)
                {
                    pendingCourses = data.GetArrayLength();
                }
            }
            
            totalCourses = publishedCourses + pendingCourses;
        }
        catch
        {
            // If Course API is unavailable, use 0
        }

        // Get enrollment stats from Enrollment API
        int totalEnrollments = 0;
        
        try
        {
            var enrollmentRequest = new HttpRequestMessage(HttpMethod.Get, "http://enrollment-api:8080/api/v1/enrollments/all");
            if (!string.IsNullOrEmpty(token))
            {
                enrollmentRequest.Headers.Add("Authorization", token);
            }
            
            var enrollmentResponse = await httpClient.SendAsync(enrollmentRequest);
            if (enrollmentResponse.IsSuccessStatusCode)
            {
                var enrollmentJson = await enrollmentResponse.Content.ReadAsStringAsync();
                var enrollmentData = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(enrollmentJson);
                
                if (enrollmentData.TryGetProperty("data", out var data) && data.ValueKind == System.Text.Json.JsonValueKind.Array)
                {
                    totalEnrollments = data.GetArrayLength();
                }
            }
        }
        catch
        {
            // If Enrollment API is unavailable, use 0
        }

        var stats = new
        {
            totalUsers,
            totalCourses,
            totalEnrollments,
            activeCoursesCount = publishedCourses,
            pendingCoursesCount = pendingCourses,
            dailyActiveUsers = activeUsers
        };

        return Ok(ApiResponse<object>.Ok(stats));
    }
}
