using library.management.domain.Models.Entities;

namespace library.management.application.Interfaces.Repositories;

public interface IMemberRepository : IGenericRepository<Member>
{
    Task<bool> IsMemberNumberTakenAsync(string memberNumber, CancellationToken cancellationToken);
}
