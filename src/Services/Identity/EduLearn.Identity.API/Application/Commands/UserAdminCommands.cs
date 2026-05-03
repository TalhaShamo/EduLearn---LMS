using MediatR;
using EduLearn.Identity.API.Application.Interfaces;
using EduLearn.Shared.Exceptions;

namespace EduLearn.Identity.API.Application.Commands;

// ── DEACTIVATE USER ───────────────────────────────────────────
public record DeactivateUserCommand(Guid UserId) : IRequest;

public class DeactivateUserCommandHandler : IRequestHandler<DeactivateUserCommand>
{
    private readonly IUserRepository _userRepo;
    public DeactivateUserCommandHandler(IUserRepository r) => _userRepo = r;

    public async Task Handle(DeactivateUserCommand cmd, CancellationToken ct)
    {
        var user = await _userRepo.GetByIdAsync(cmd.UserId)
                   ?? throw new NotFoundException("User", cmd.UserId);
        user.Deactivate();    // OOP: business logic inside entity
        _userRepo.Update(user);
        await _userRepo.SaveChangesAsync();
    }
}

// ── BAN USER ──────────────────────────────────────────────────
public record BanUserCommand(Guid UserId) : IRequest;

public class BanUserCommandHandler : IRequestHandler<BanUserCommand>
{
    private readonly IUserRepository _userRepo;
    public BanUserCommandHandler(IUserRepository r) => _userRepo = r;

    public async Task Handle(BanUserCommand cmd, CancellationToken ct)
    {
        var user = await _userRepo.GetByIdAsync(cmd.UserId)
                   ?? throw new NotFoundException("User", cmd.UserId);
        user.Ban();
        _userRepo.Update(user);
        await _userRepo.SaveChangesAsync();
    }
}

// ── ACTIVATE USER ─────────────────────────────────────────────
public record ActivateUserCommand(Guid UserId) : IRequest;

public class ActivateUserCommandHandler : IRequestHandler<ActivateUserCommand>
{
    private readonly IUserRepository _userRepo;
    public ActivateUserCommandHandler(IUserRepository r) => _userRepo = r;

    public async Task Handle(ActivateUserCommand cmd, CancellationToken ct)
    {
        var user = await _userRepo.GetByIdAsync(cmd.UserId)
                   ?? throw new NotFoundException("User", cmd.UserId);
        user.Activate();
        _userRepo.Update(user);
        await _userRepo.SaveChangesAsync();
    }
}

// ── DELETE USER ───────────────────────────────────────────────
public record DeleteUserCommand(Guid UserId) : IRequest;

public class DeleteUserCommandHandler : IRequestHandler<DeleteUserCommand>
{
    private readonly IUserRepository _userRepo;
    public DeleteUserCommandHandler(IUserRepository r) => _userRepo = r;

    public async Task Handle(DeleteUserCommand cmd, CancellationToken ct)
    {
        var user = await _userRepo.GetByIdAsync(cmd.UserId)
                   ?? throw new NotFoundException("User", cmd.UserId);
        
        // Permanently delete user from database
        _userRepo.Delete(user);
        await _userRepo.SaveChangesAsync();
    }
}
