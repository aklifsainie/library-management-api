using FluentResults;
using library.management.application.Interfaces.Repositories;
using library.management.application.Interfaces.Services;
using library.management.application.Shared.Errors;
using library.management.application.Shared.Mediator;
using library.management.domain.Models.Entities;
using library.management.domain.Models.ViewModels.LoanVM;
using library.management.domain.SharedKernel;

namespace library.management.application.Features.LoanFeatures.Commands;

public sealed record ReturnBookCommand(int Id, int MemberId, int BookId) : IRequest<Result<LoanVM>>;

public sealed class ReturnBookCommandHandler : IRequestHandler<ReturnBookCommand, Result<LoanVM>>
{
    private readonly IBookLoanRepository _loans;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public ReturnBookCommandHandler(IBookLoanRepository loans, IUnitOfWork unitOfWork,
        ICurrentUserService currentUser)
    {
        _loans = loans;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Result<LoanVM>> Handle(ReturnBookCommand command, CancellationToken cancellationToken)
    {
        if (command.Id <= 0)
        {
            var error = new ValidationError("The supplied loan ID is invalid.");
            error.WithMetadata(nameof(command.Id), "Loan ID must be greater than zero.");
            return Result.Fail<LoanVM>(error);
        }

        var loan = await _loans.GetByIdAsync(command.Id, asNoTracking: false, cancellationToken);
        if (loan is null)
            return Result.Fail<LoanVM>(new EntityNotFoundError(nameof(BookLoan), command.Id));
        if (command.MemberId <= 0 || command.BookId <= 0)
            return Result.Fail<LoanVM>(new ValidationError("Member and book IDs are required to verify the return."));
        if (loan.MemberId != command.MemberId || loan.BookId != command.BookId)
            return Result.Fail<LoanVM>(new ConflictError("The member and book do not match this loan."));
        if (loan.ReturnedAtUtc is not null)
            return Result.Fail<LoanVM>(new ConflictError("This loan has already been returned."));

        if (string.IsNullOrWhiteSpace(_currentUser.UserId))
            return Result.Fail<LoanVM>(new ValidationError("Officer identity is unavailable."));

        loan.ReturnedAtUtc = DateTime.UtcNow;
        loan.ReturnOfficerUserId = _currentUser.UserId;
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Ok(loan.ToViewModel()).WithSuccess("Book returned successfully.");
    }
}
