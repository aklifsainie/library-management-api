using library.management.application.Interfaces.Services;
using library.management.application.Shared.Mediator;
using library.management.domain.Models.ViewModels.AccountVM;
using library.management.identity.Interfaces;
using FluentResults;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;

namespace library.management.application.Features.AccountFeatures.Commands
{
    public class GenerateAuthenticatorSetupCommand : IRequest<Result<AuthenticatorSetupResponseVM>>
    {

    }

    public class GenerateAuthenticatorSetupCommandHandler : IRequestHandler<GenerateAuthenticatorSetupCommand, Result<AuthenticatorSetupResponseVM>>
    {
        private readonly IAccountService _accountService;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<GenerateAuthenticatorSetupCommandHandler> _logger;

        public GenerateAuthenticatorSetupCommandHandler(IAccountService accountService, ICurrentUserService currentUserService, ILogger<GenerateAuthenticatorSetupCommandHandler> logger)
        {
            _accountService = accountService;
            _currentUserService = currentUserService;
            _logger = logger;
        }

        public async Task<Result<AuthenticatorSetupResponseVM>> Handle(GenerateAuthenticatorSetupCommand request, CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId;

            if (string.IsNullOrWhiteSpace(userId))
            {
                return Result.Fail("User is not authenticated");
            }
            _logger.LogInformation("Generating authenticator setup for {UserId}", userId);

            var result = await _accountService.GenerateAuthenticatorSetupAsync(userId);
            if (!result.IsSuccess)
            {
                return Result.Fail<AuthenticatorSetupResponseVM>(result.Errors.FirstOrDefault()?.Message ?? "Failed to generate authenticator setup.");
            }

            AuthenticatorSetupResponseVM vm = new AuthenticatorSetupResponseVM
            { 
                Key = result.Value.Key,
                ProvisionUri = result.Value.ProvisionUri
            };

            return Result.Ok<AuthenticatorSetupResponseVM>(vm);
        }
    }
}
