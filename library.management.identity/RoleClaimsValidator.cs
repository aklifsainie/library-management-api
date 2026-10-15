using System.Security.Claims;
using library.management.identity.Models.Entities;
using Microsoft.AspNetCore.Identity;

namespace library.management.identity;

public static class RoleClaimsValidator
{
    public static async Task<bool> MatchesCurrentRolesAsync(
        ClaimsPrincipal principal,
        ApplicationUser user,
        UserManager<ApplicationUser> userManager)
    {
        var tokenRoles = principal.FindAll(ClaimTypes.Role)
            .Select(claim => claim.Value).ToHashSet(StringComparer.Ordinal);
        var currentRoles = (await userManager.GetRolesAsync(user))
            .ToHashSet(StringComparer.Ordinal);

        return user.IsActive && !user.IsDeleted &&
            currentRoles.Any(UserRoles.All.Contains) && tokenRoles.SetEquals(currentRoles);
    }
}
