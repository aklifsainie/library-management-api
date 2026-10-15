using library.management.application.Interfaces.Repositories;
using library.management.domain.Models.Entities;
using library.management.infrastructure.Contexts;
using Microsoft.EntityFrameworkCore;

namespace library.management.infrastructure.Repositories;

public sealed class MemberRepository(ApplicationDbContext context) : GenericRepository<Member>(context), IMemberRepository
{
    public Task<bool> IsMemberNumberTakenAsync(string memberNumber, CancellationToken cancellationToken) =>
        context.Members.AsNoTracking().AnyAsync(member => member.MemberNumber == memberNumber, cancellationToken);
}
