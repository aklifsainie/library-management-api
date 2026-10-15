using System.Security.Claims;
using FluentResults;
using library.management.application.Features.AccountFeatures.Commands;
using library.management.application.Interfaces.Services;
using library.management.identity;
using library.management.identity.Interfaces;
using library.management.identity.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Moq;

namespace library.management.test.TestFeatures.AccountFeature;

public sealed class UserRolesTest
{
    [Fact]
    public async Task RoleClaimsValidator_RejectsTokenAfterRoleChanges()
    {
        var user = new ApplicationUser { Id = "user-1" };
        var manager = CreateUserManager();
        manager.Setup(value => value.GetRolesAsync(user)).ReturnsAsync([UserRoles.Admin]);
        var tokenPrincipal = PrincipalWithRole(UserRoles.Officer);

        Assert.False(await RoleClaimsValidator.MatchesCurrentRolesAsync(tokenPrincipal, user, manager.Object));
    }

    [Fact]
    public async Task RoleClaimsValidator_AcceptsCurrentRole()
    {
        var user = new ApplicationUser { Id = "user-1" };
        var manager = CreateUserManager();
        manager.Setup(value => value.GetRolesAsync(user)).ReturnsAsync([UserRoles.Admin]);
        var tokenPrincipal = PrincipalWithRole(UserRoles.Admin);

        Assert.True(await RoleClaimsValidator.MatchesCurrentRolesAsync(tokenPrincipal, user, manager.Object));
    }

    [Fact]
    public async Task RoleClaimsValidator_RejectsRetiredMemberRole()
    {
        var user = new ApplicationUser { Id = "legacy-1" };
        var manager = CreateUserManager();
        manager.Setup(value => value.GetRolesAsync(user)).ReturnsAsync(["Member"]);

        Assert.False(await RoleClaimsValidator.MatchesCurrentRolesAsync(PrincipalWithRole("Member"), user, manager.Object));
    }

    [Fact]
    public async Task SetRoleHandler_UsesAuthenticatedActorAndCancellation()
    {
        var accounts = new Mock<IAccountService>();
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.SetupGet(value => value.UserId).Returns("superadmin-1");
        using var cancellation = new CancellationTokenSource();
        accounts.Setup(value => value.SetRoleAsync("superadmin-1", "user-2", UserRoles.Admin, cancellation.Token))
            .ReturnsAsync(Result.Ok());

        var handler = new SetUserRoleCommandHandler(accounts.Object, currentUser.Object);
        var result = await handler.Handle(new SetUserRoleCommand { UserId = "user-2", Role = UserRoles.Admin }, cancellation.Token);

        Assert.True(result.IsSuccess);
        accounts.Verify(value => value.SetRoleAsync("superadmin-1", "user-2", UserRoles.Admin, cancellation.Token), Times.Once);
    }

    [Fact]
    public async Task SetRoleHandler_DoesNotCallIdentityWithoutActor()
    {
        var accounts = new Mock<IAccountService>();
        var currentUser = new Mock<ICurrentUserService>();
        var handler = new SetUserRoleCommandHandler(accounts.Object, currentUser.Object);

        var result = await handler.Handle(new SetUserRoleCommand { UserId = "user-2", Role = UserRoles.Admin }, CancellationToken.None);

        Assert.True(result.IsFailed);
        accounts.Verify(value => value.SetRoleAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private static ClaimsPrincipal PrincipalWithRole(string role) =>
        new(new ClaimsIdentity([new Claim(ClaimTypes.Role, role)], "Bearer"));

    private static Mock<UserManager<ApplicationUser>> CreateUserManager() =>
        new(new Mock<IUserStore<ApplicationUser>>().Object, null!, null!, null!, null!, null!, null!, null!, null!);
}
