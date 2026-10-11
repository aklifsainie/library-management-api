using System.ComponentModel.DataAnnotations;
using FluentResults;
using library.management.application.Interfaces.Services;
using library.management.application.Shared.Mediator;
using library.management.identity.Interfaces;

namespace library.management.application.Features.AccountFeatures.Commands;

public sealed class SetUserRoleCommand : IRequest<Result>
{
    [Required]
    public string Role { get; set; } = string.Empty;

    public string UserId { get; set; } = string.Empty;
}

public sealed class SetUserRoleCommandHandler : IRequestHandler<SetUserRoleCommand, Result>
{
    private readonly IAccountService _accountService;
    private readonly ICurrentUserService _currentUser;

    public SetUserRoleCommandHandler(IAccountService accountService, ICurrentUserService currentUser)
    {
        _accountService = accountService;
        _currentUser = currentUser;
    }

    public Task<Result> Handle(SetUserRoleCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_currentUser.UserId))
            return Task.FromResult(Result.Fail("User is not authenticated."));

        return _accountService.SetRoleAsync(_currentUser.UserId, request.UserId, request.Role, cancellationToken);
    }
}
