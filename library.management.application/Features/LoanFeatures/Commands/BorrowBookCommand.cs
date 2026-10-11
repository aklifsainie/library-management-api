using FluentResults;
using library.management.application.Interfaces.Repositories;
using library.management.application.Interfaces.Services;
using library.management.application.Shared.Errors;
using library.management.application.Shared.Mediator;
using library.management.domain.Models.Entities;
using library.management.domain.Models.ViewModels.LoanVM;
using library.management.identity.Interfaces;

namespace library.management.application.Features.LoanFeatures.Commands;

public sealed record BorrowBookCommand(int BookId) : IRequest<Result<LoanVM>>;

public sealed class BorrowBookCommandHandler : IRequestHandler<BorrowBookCommand, Result<LoanVM>>
{
    private readonly IBookRepository _books;
    private readonly IBookLoanRepository _loans;
    private readonly ICurrentUserService _currentUser;
    private readonly ILoanUserService _users;

    public BorrowBookCommandHandler(IBookRepository books, IBookLoanRepository loans,
        ICurrentUserService currentUser, ILoanUserService users)
    {
        _books = books;
        _loans = loans;
        _currentUser = currentUser;
        _users = users;
    }

    public async Task<Result<LoanVM>> Handle(BorrowBookCommand command, CancellationToken cancellationToken)
    {
        if (command.BookId <= 0)
        {
            var error = new ValidationError("The supplied book ID is invalid.");
            error.WithMetadata(nameof(command.BookId), "Book ID must be greater than zero.");
            return Result.Fail<LoanVM>(error);
        }

        var userId = _currentUser.UserId;
        if (string.IsNullOrWhiteSpace(userId) || !await _users.IsActiveAsync(userId, cancellationToken))
            return Result.Fail<LoanVM>(new EntityNotFoundError("User", userId ?? string.Empty));

        var book = await _books.FirstOrDefaultAsync(item => item.Id == command.BookId && item.IsActive,
            asNoTracking: true, cancellationToken: cancellationToken);
        if (book is null)
            return Result.Fail<LoanVM>(new EntityNotFoundError(nameof(Book), command.BookId));

        if (await _loans.HasOpenLoanAsync(command.BookId, cancellationToken))
            return Result.Fail<LoanVM>(new ConflictError("This book is already on loan."));

        var loan = new BookLoan
        {
            BookId = command.BookId,
            UserId = userId,
            BorrowedAtUtc = DateTime.UtcNow
        };

        if (!await _loans.TryBorrowAsync(loan, cancellationToken))
            return Result.Fail<LoanVM>(new ConflictError("This book is already on loan."));

        return Result.Ok(loan.ToViewModel()).WithSuccess("Book borrowed successfully.");
    }
}
