using library.management.identity.Contexts;
using library.management.identity.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace library.management.identity.Services;

public sealed class LoanUserService : ILoanUserService
{
    private readonly IdentityContext _context;

    public LoanUserService(IdentityContext context) => _context = context;

    public Task<bool> IsActiveAsync(string userId, CancellationToken cancellationToken) =>
        _context.Users.AsNoTracking().AnyAsync(user => user.Id == userId && user.IsActive && !user.IsDeleted,
            cancellationToken);
}
