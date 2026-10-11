using library.management.domain.Models.Entities;
using library.management.domain.Models.ViewModels.CommonVM;

namespace library.management.application.Interfaces.Repositories;

public interface IBookLoanRepository
{
    Task<bool> HasOpenLoanAsync(int bookId, CancellationToken cancellationToken);
    Task<bool> TryBorrowAsync(BookLoan loan, CancellationToken cancellationToken);
    Task<BookLoan?> GetByIdAsync(int id, bool asNoTracking, CancellationToken cancellationToken);
    Task<PaginatedResult<BookLoan>> GetPageAsync(string? userId, int pageNumber, int pageSize, CancellationToken cancellationToken);
}
