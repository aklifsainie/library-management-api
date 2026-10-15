using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using library.management.identity.Contexts;
using library.management.identity.Models.Entities;
using library.management.identity.Interfaces;

namespace library.management.identity;

public static class IdentityRoleSeeder
{
    public static async Task SeedRolesAsync(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        foreach (var role in UserRoles.All)
        {
            if (await roleManager.RoleExistsAsync(role))
                continue;

            var result = await roleManager.CreateAsync(new IdentityRole(role));
            if (!result.Succeeded && !await roleManager.RoleExistsAsync(role))
                throw new InvalidOperationException($"Could not create the {role} role: " +
                    string.Join("; ", result.Errors.Select(error => error.Description)));
        }
    }

    public static async Task BootstrapFirstSuperadminAsync(this IServiceProvider services, string email, string callbackBaseUrl)
    {
        using var scope = services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var context = scope.ServiceProvider.GetRequiredService<IdentityContext>();
        var accountService = scope.ServiceProvider.GetRequiredService<IAccountService>();

        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("A user email is required.", nameof(email));

        await using var transaction = await context.Database.BeginTransactionAsync();
        if (await userManager.GetUsersInRoleAsync(UserRoles.Superadmin) is { Count: > 0 })
            throw new InvalidOperationException("A Superadmin already exists.");

        var user = await userManager.FindByEmailAsync(email);
        var newlyCreated = user is null;
        if (user is null)
        {
            user = new ApplicationUser { Email = email, UserName = email, IsActive = true, CreatedAt = DateTime.UtcNow };
            var created = await userManager.CreateAsync(user);
            if (!created.Succeeded)
                throw new InvalidOperationException("Could not create the initial Superadmin account.");
        }
        else if (!user.IsActive || user.IsDeleted || !user.EmailConfirmed ||
            !await userManager.HasPasswordAsync(user))
            throw new InvalidOperationException("An existing account must be active, email confirmed, and password configured.");

        var currentRoles = await userManager.GetRolesAsync(user);
        if (currentRoles.Count != 0)
        {
            var removed = await userManager.RemoveFromRolesAsync(user, currentRoles);
            if (!removed.Succeeded)
                throw new InvalidOperationException("Could not clear the account's existing roles.");
        }

        var added = await userManager.AddToRoleAsync(user, UserRoles.Superadmin);
        if (!added.Succeeded)
            throw new InvalidOperationException("Could not assign the Superadmin role.");

        if (newlyCreated)
        {
            var sent = await accountService.SendEmailConfirmationAsync(user, callbackBaseUrl);
            if (sent.IsFailed)
                throw new InvalidOperationException("Superadmin invitation email delivery failed.");
        }
        await transaction.CommitAsync();
    }
}
