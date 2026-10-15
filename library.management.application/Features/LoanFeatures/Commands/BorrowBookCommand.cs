using FluentResults;
using library.management.application.Interfaces.Repositories;
using library.management.application.Interfaces.Services;
using library.management.application.Shared.Errors;
using library.management.application.Shared.Mediator;
using library.management.domain.Models.Entities;
using library.management.domain.Models.ViewModels.LoanVM;

namespace library.management.application.Features.LoanFeatures.Commands;

public sealed record BorrowBookCommand(int BookId, int MemberId) : IRequest<Result<LoanVM>>;

public sealed class BorrowBookCommandHandler : IRequestHandler<BorrowBookCommand, Result<LoanVM>>
{
    private readonly IBookRepository _books;
    private readonly IBookLoanRepository _loans;
    private readonly ICurrentUserService _currentUser;
    private readonly IMemberRepository _members;

    public BorrowBookCommandHandler(IBookRepository books, IBookLoanRepository loans,
        ICurrentUserService currentUser, IMemberRepository members)
    {
        _books = books;
        _loans = loans;
        _currentUser = currentUser;
        _members = members;
    }

    public async Task<Result<LoanVM>> Handle(BorrowBookCommand command, CancellationToken cancellationToken)
    {
        if (command.BookId <= 0)
        {
            var error = new ValidationError("The supplied book ID is invalid.");
            error.WithMetadata(nameof(command.BookId), "Book ID must be greater than zero.");
            return Result.Fail<LoanVM>(error);
        }

        if (command.MemberId <= 0)
        {
            var error = new ValidationError("The supplied member ID is invalid.");
            error.WithMetadata(nameof(command.MemberId), "Member ID must be greater than zero.");
            return Result.Fail<LoanVM>(error);
        }
        var member = await _members.GetByIdAsync(command.MemberId, true, cancellationToken);
        if (member is null || !member.IsActive)
            return Result.Fail<LoanVM>(new EntityNotFoundError(nameof(Member), command.MemberId));
        var officerId = _currentUser.UserId;
        if (string.IsNullOrWhiteSpace(officerId))
            return Result.Fail<LoanVM>(new ValidationError("Officer identity is unavailable."));

        var book = await _books.FirstOrDefaultAsync(item => item.Id == command.BookId && item.IsActive,
            asNoTracking: true, cancellationToken: cancellationToken);
        if (book is null)
            return Result.Fail<LoanVM>(new EntityNotFoundError(nameof(Book), command.BookId));

        if (await _loans.HasOpenLoanAsync(command.BookId, cancellationToken))
            return Result.Fail<LoanVM>(new ConflictError("This book is already on loan."));

        var loan = new BookLoan
        {
            BookId = command.BookId,
            MemberId = member.Id,
            CheckoutOfficerUserId = officerId,
            BorrowedAtUtc = DateTime.UtcNow
        };

        if (!await _loans.TryBorrowAsync(loan, cancellationToken))
            return Result.Fail<LoanVM>(new ConflictError("This book is already on loan."));

        return Result.Ok(loan.ToViewModel()).WithSuccess("Book borrowed successfully.");
    }
}
