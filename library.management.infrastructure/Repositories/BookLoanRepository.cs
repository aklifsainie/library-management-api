using library.management.application.Interfaces.Repositories;
using library.management.domain.Models.Entities;
using library.management.domain.Models.ViewModels.CommonVM;
using library.management.infrastructure.Contexts;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace library.management.infrastructure.Repositories;

public sealed class BookLoanRepository : IBookLoanRepository
{
    private readonly ApplicationDbContext _context;

    public BookLoanRepository(ApplicationDbContext context) => _context = context;

    public Task<bool> HasOpenLoanAsync(int bookId, CancellationToken cancellationToken) =>
        _context.BookLoans.AsNoTracking().AnyAsync(loan => loan.BookId == bookId && loan.ReturnedAtUtc == null,
            cancellationToken);

    public Task<bool> HasOpenLoanForMemberAsync(int memberId, CancellationToken cancellationToken) =>
        _context.BookLoans.AsNoTracking().AnyAsync(loan => loan.MemberId == memberId && loan.ReturnedAtUtc == null,
            cancellationToken);

    public async Task<bool> TryBorrowAsync(BookLoan loan, CancellationToken cancellationToken)
    {
        await _context.BookLoans.AddAsync(loan, cancellationToken);
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException exception) when (exception.InnerException is SqlException sqlException
            && sqlException.Number is 2601 or 2627)
        {
            _context.Entry(loan).State = EntityState.Detached;
            return false;
        }
    }

    public Task<BookLoan?> GetByIdAsync(int id, bool asNoTracking, CancellationToken cancellationToken)
    {
        IQueryable<BookLoan> query = _context.BookLoans.Where(loan => loan.Id == id);
        if (asNoTracking) query = query.AsNoTracking();
        return query.FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<PaginatedResult<BookLoan>> GetPageAsync(int pageNumber, int pageSize,
        CancellationToken cancellationToken)
    {
        IQueryable<BookLoan> query = _context.BookLoans.AsNoTracking();
        var count = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(loan => loan.BorrowedAtUtc)
            .ThenByDescending(loan => loan.Id)
            .Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return new PaginatedResult<BookLoan>
        {
            Items = items, PageNumber = pageNumber, PageSize = pageSize, TotalCount = count
        };
    }
}
