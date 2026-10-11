namespace library.management.identity.Interfaces;

public interface ILoanUserService
{
    Task<bool> IsActiveAsync(string userId, CancellationToken cancellationToken);
}
