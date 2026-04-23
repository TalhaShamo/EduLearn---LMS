using MediatR;
using EduLearn.Identity.API.Application.DTOs;
using EduLearn.Identity.API.Application.Interfaces;
using EduLearn.Shared.Exceptions;

namespace EduLearn.Identity.API.Application.Queries;

// ── GET ALL USERS (Admin only) ────────────────────────────────

public record GetAllUsersQuery(int Page = 1, int PageSize = 20) : IRequest<IEnumerable<UserDto>>;

public class GetAllUsersQueryHandler : IRequestHandler<GetAllUsersQuery, IEnumerable<UserDto>>
{
    private readonly IUserRepository _userRepo;

    public GetAllUsersQueryHandler(IUserRepository userRepo) => _userRepo = userRepo;

    public async Task<IEnumerable<UserDto>> Handle(GetAllUsersQuery query, CancellationToken ct)
    {
        var users = await _userRepo.GetAllUsersPagedAsync(query.Page, query.PageSize);

        // Project entity list to DTO list using LINQ (Collections concept)
        return users.Select(u =>
            new UserDto(u.UserId, u.FullName, u.Email, u.Role.ToString(), u.IsActive, u.IsBanned, u.CreatedAt));
    }
}

// ── GET USER BY ID ────────────────────────────────────────────

public record GetUserByIdQuery(Guid UserId) : IRequest<UserDto>;

public class GetUserByIdQueryHandler : IRequestHandler<GetUserByIdQuery, UserDto>
{
    private readonly IUserRepository _userRepo;

    public GetUserByIdQueryHandler(IUserRepository userRepo) => _userRepo = userRepo;

    public async Task<UserDto> Handle(GetUserByIdQuery query, CancellationToken ct)
    {
        var user = await _userRepo.GetByIdAsync(query.UserId)
                   ?? throw new NotFoundException("User", query.UserId);

        return new UserDto(user.UserId, user.FullName, user.Email, user.Role.ToString(), user.IsActive, user.IsBanned, user.CreatedAt);
    }
}
