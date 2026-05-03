using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using EduLearn.Identity.API.Application.Commands;
using EduLearn.Identity.API.Application.DTOs;
using EduLearn.Shared.Models;

namespace EduLearn.Identity.API.Controllers;

// Auth controller — handles registration, login, refresh, logout
[ApiController]
[Route("api/v1/auth")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuthController(IMediator mediator) => _mediator = mediator;

    // POST /api/v1/auth/register
    // Anyone can register; FluentValidation runs automatically via middleware
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var command = new RegisterCommand(request.FullName, request.Email, request.Password, request.Role);
        var user    = await _mediator.Send(command);

        return StatusCode(201, ApiResponse<UserDto>.Created(user, "Account created successfully. You can now log in."));
    }

    // POST /api/v1/auth/login
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var command          = new LoginCommand(request.Email, request.Password);
        var (auth, rawToken) = await _mediator.Send(command);

        // Store refresh token in HttpOnly cookie — Angular cannot read this via JS (XSS protection)
        SetRefreshTokenCookie(rawToken, auth.User.UserId);

        return Ok(ApiResponse<AuthResponse>.Ok(auth, "Login successful."));
    }

    // POST /api/v1/auth/refresh-token
    // Angular calls this silently before the access token expires
    [HttpPost("refresh-token")]
    [AllowAnonymous]
    public async Task<IActionResult> RefreshToken()
    {
        // Read refresh token from the HttpOnly cookie
        var cookieToken = Request.Cookies["edulearn_refresh_token"];
        if (string.IsNullOrEmpty(cookieToken))
            return Unauthorized(ApiResponse<string>.Fail("Refresh token missing.", 401));

        var command          = new RefreshTokenCommand(cookieToken);
        var (auth, newToken) = await _mediator.Send(command);

        // Rotate: overwrite the cookie with the new refresh token
        SetRefreshTokenCookie(newToken, auth.User.UserId);

        return Ok(ApiResponse<AuthResponse>.Ok(auth, "Token refreshed."));
    }

    // POST /api/v1/auth/logout — requires valid JWT
    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        // Extract userId from the JWT sub claim (or nameid as fallback)
        var subClaim = User.FindFirst("sub")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        if (string.IsNullOrEmpty(subClaim) || !Guid.TryParse(subClaim, out var userId))
        {
            return BadRequest(ApiResponse<string>.Fail("Invalid token."));
        }

        await _mediator.Send(new LogoutCommand(userId));

        // Clear the refresh token cookie
        Response.Cookies.Delete("edulearn_refresh_token");

        return Ok(ApiResponse<string>.Ok("", "Logged out successfully."));
    }

    // POST /api/v1/auth/verify-email
    [HttpPost("verify-email")]
    [AllowAnonymous]
    public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailRequest request)
    {
        await _mediator.Send(new VerifyEmailCommand(request.Token));
        return Ok(ApiResponse<string>.Ok("", "Email verified successfully! You can now log in."));
    }

    // POST /api/v1/auth/resend-verification
    [HttpPost("resend-verification")]
    [Authorize]
    public async Task<IActionResult> ResendVerification()
    {
        var subClaim = User.FindFirst("sub")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        if (string.IsNullOrEmpty(subClaim) || !Guid.TryParse(subClaim, out var userId))
        {
            return BadRequest(ApiResponse<string>.Fail("Invalid token."));
        }

        await _mediator.Send(new SendEmailVerificationCommand(userId));
        return Ok(ApiResponse<string>.Ok("", "Verification email sent. Please check your inbox."));
    }

    // POST /api/v1/auth/forgot-password
    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        await _mediator.Send(new RequestPasswordResetCommand(request.Email));
        return Ok(ApiResponse<string>.Ok("", "If an account with that email exists, a password reset link has been sent."));
    }

    // POST /api/v1/auth/reset-password
    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        await _mediator.Send(new ResetPasswordCommand(request.Token, request.NewPassword));
        return Ok(ApiResponse<string>.Ok("", "Password reset successfully! You can now log in with your new password."));
    }

    // Helper: set the refresh token as a secure HttpOnly cookie
    private void SetRefreshTokenCookie(string rawToken, Guid userId)
    {
        Response.Cookies.Append("edulearn_refresh_token", rawToken, new CookieOptions
        {
            HttpOnly  = true,           // JS cannot read this — prevents XSS token theft
            // In local dev (http://localhost) we must allow non-HTTPS, otherwise browsers drop the cookie.
            // In production behind HTTPS, this becomes Secure automatically.
            Secure    = Request.IsHttps,
            // Lax allows same-site top-level navigation + typical SPA flows, while still protecting against CSRF better than None.
            SameSite  = SameSiteMode.Lax,
            Expires   = DateTime.UtcNow.AddDays(7),
            Path      = "/api/v1/auth" // Cookie only sent to auth endpoints
        });
    }
}

// Request DTOs
public record VerifyEmailRequest(string Token);
public record ForgotPasswordRequest(string Email);
public record ResetPasswordRequest(string Token, string NewPassword);
