using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using EduLearn.Course.API.Application.Interfaces;
using EduLearn.Shared.Models;
using System.Security.Claims;

namespace EduLearn.Course.API.Controllers;

[ApiController]
[Route("api/v1/instructor")]
[Authorize(Roles = "Instructor")]
public class InstructorController : ControllerBase
{
    private readonly ICourseRepository _courseRepo;
    private readonly IHttpClientFactory _httpFactory;

    public InstructorController(ICourseRepository courseRepo, IHttpClientFactory httpFactory)
    {
        _courseRepo = courseRepo;
        _httpFactory = httpFactory;
    }

    // GET /api/v1/instructor/stats
    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        var instructorId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        
        // Get instructor's courses
        var courses = await _courseRepo.GetByInstructorAsync(instructorId);
        var publishedCourses = courses.Where(c => c.Status == Domain.Enums.CourseStatus.Published).ToList();
        var draftCourses = courses.Where(c => c.Status == Domain.Enums.CourseStatus.Draft).ToList();
        
        // Get total students enrolled in instructor's courses
        int totalStudents = 0;
        
        try
        {
            var httpClient = _httpFactory.CreateClient();
            var token = Request.Headers["Authorization"].ToString();
            
            foreach (var course in publishedCourses)
            {
                var enrollmentRequest = new HttpRequestMessage(HttpMethod.Get, 
                    $"http://enrollment-api:8080/api/v1/enrollments/course/{course.CourseId}/count");
                
                if (!string.IsNullOrEmpty(token))
                {
                    enrollmentRequest.Headers.Add("Authorization", token);
                }
                
                var enrollmentResponse = await httpClient.SendAsync(enrollmentRequest);
                if (enrollmentResponse.IsSuccessStatusCode)
                {
                    var enrollmentJson = await enrollmentResponse.Content.ReadAsStringAsync();
                    var enrollmentData = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(enrollmentJson);
                    
                    if (enrollmentData.TryGetProperty("data", out var data) && 
                        data.TryGetProperty("count", out var count))
                    {
                        totalStudents += count.GetInt32();
                    }
                }
            }
        }
        catch
        {
            // If Enrollment API is unavailable, use 0
        }

        var stats = new
        {
            totalStudents,
            totalCourses = publishedCourses.Count,
            draftCourses = draftCourses.Count
        };

        return Ok(ApiResponse<object>.Ok(stats));
    }
}
