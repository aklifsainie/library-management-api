using library.management.identity.Models.DTOs;
using library.management.identity.Models.Entities;
using library.management.identity.Services;
using FluentResults;
using System.Threading;

namespace library.management.identity.Interfaces
{
    public interface IAccountService
    {
        Task<Result<string>> RegisterAsync(string email, string userName, string callbackBaseUrl, CancellationToken cancellationToken);
        Task<Result> SetRoleAsync(string actorUserId, string targetUserId, string role, CancellationToken cancellationToken);
        Task<Result> SendEmailConfirmationAsync(ApplicationUser user, string callbackBaseUrl);
        Task<Result> ConfirmEmailAsync(string userId, string encodedToken);
        Task<Result> SetPasswordFirstTimeAsync(string userId, string password);
        Task<Result<ProfileUpdateDto>> UpdateProfileAsync(string userId, ProfileUpdateDto dto);
        Task<Result<AuthenticatorSetupDto>> GenerateAuthenticatorSetupAsync(string userId);
        Task<Result<IEnumerable<string>>> EnableAuthenticatorAsync(string userId, string verificationCode);
        Task<Result> DisableAuthenticatorAsync(string userId);
        Task<Result> ChangePasswordAsync(string userId, string currentPassword, string newPassword, CancellationToken cancellationToken);
        Task<Result> ResendConfirmationEmailAsync(string email, string callbackBaseUrl, CancellationToken cancellationToken);
        Task<Result<AccountMeDto>> GetMyAccountAsync(string userId, CancellationToken cancellationToken);
    }
}
