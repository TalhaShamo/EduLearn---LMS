using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using EduLearn.Identity.API.Application.Commands;
using EduLearn.Identity.API.Application.Queries;
using EduLearn.Identity.API.Application.DTOs;
using EduLearn.Shared.Models;

namespace EduLearn.Identity.API.Controllers;

// User management controller — Admin can view, deactivate, ban users
[ApiController]
[Route("api/v1/users")]
[Authorize] // All endpoints here require authentication
public class UsersController : ControllerBase
{
    private readonly IMediator _mediator;

    public UsersController(IMediator mediator) => _mediator = mediator;

    // GET /api/v1/users?page=1&pageSize=20 — Admin only
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var users = await _mediator.Send(new GetAllUsersQuery(page, pageSize));
        return Ok(ApiResponse<IEnumerable<UserDto>>.Ok(users));
    }

    // GET /api/v1/users/{id} — Admin or the user themselves
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var user = await _mediator.Send(new GetUserByIdQuery(id));
        return Ok(ApiResponse<UserDto>.Ok(user));
    }

    // PATCH /api/v1/users/{id}/deactivate — Admin only
    [HttpPatch("{id:guid}/deactivate")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Deactivate(Guid id)
    {
        await _mediator.Send(new DeactivateUserCommand(id));
        return Ok(ApiResponse<string>.Ok("", "User deactivated."));
    }

    // PATCH /api/v1/users/{id}/ban — Admin only
    [HttpPatch("{id:guid}/ban")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Ban(Guid id)
    {
        await _mediator.Send(new BanUserCommand(id));
        return Ok(ApiResponse<string>.Ok("", "User banned."));
    }

    // PATCH /api/v1/users/{id}/activate — Admin only
    [HttpPatch("{id:guid}/activate")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Activate(Guid id)
    {
        await _mediator.Send(new ActivateUserCommand(id));
        return Ok(ApiResponse<string>.Ok("", "User activated."));
    }

    // GET /api/v1/users/recent — Admin only, get recent users
    [HttpGet("recent")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetRecentUsers()
    {
        var users = await _mediator.Send(new GetAllUsersQuery(1, 10));
        return Ok(ApiResponse<IEnumerable<UserDto>>.Ok(users));
    }

    // DELETE /api/v1/users/{id} — Admin only, permanently delete user
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _mediator.Send(new DeleteUserCommand(id));
        return Ok(ApiResponse<string>.Ok("", "User deleted permanently."));
    }
}
