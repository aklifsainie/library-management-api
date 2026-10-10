using library.management.application.Shared.Mediator;
using library.management.domain.Models.ViewModels.AuthenticationVM;
using library.management.identity.Interfaces;
using FluentResults;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace library.management.application.Features.AuthenticationFeatures.Commands
{
    public class ForgotPasswordCommand: IRequest<Result>
    {
        [Required, EmailAddress]
        public string Email { get; set; }

        public string CallbackBaseUrl { get; set; }
    }

    public class ForgotPasswordCommandHandler : IRequestHandler<ForgotPasswordCommand, Result>
    {
        private readonly IAuthenticationService _authenticationService;

        public ForgotPasswordCommandHandler(
            IAuthenticationService authenticationService)
        {
            _authenticationService = authenticationService;
        }

        public async Task<Result> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
        {
            var result = await _authenticationService.ForgotPasswordAsync(request.Email, request.CallbackBaseUrl);

            if (!result.IsSuccess)
                return Result.Fail(result.Errors.FirstOrDefault()?.Message ?? "Registration failed.");


            return Result.Ok();
        }
    }
}
