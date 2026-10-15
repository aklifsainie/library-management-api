using System.Text.Json;
using FluentResults;
using library.management.application.Interfaces.Commons;
using library.management.application.Interfaces.Repositories;
using library.management.application.Shared.Errors;
using library.management.application.Shared.Mediator;
using library.management.domain.Enums;
using library.management.domain.Models.Entities;
using library.management.domain.Models.ViewModels.LoanVM;

namespace library.management.application.Features.LoanFeatures.Queries;

public sealed record GetLoanByIdQuery(int Id) : IRequest<Result<LoanVM>>;

public sealed class GetLoanByIdQueryHandler : IRequestHandler<GetLoanByIdQuery, Result<LoanVM>>
{
    private readonly IBookLoanRepository _loans;
    private readonly IAuditLogger _auditLogger;

    public GetLoanByIdQueryHandler(IBookLoanRepository loans, IAuditLogger auditLogger)
    {
        _loans = loans;
        _auditLogger = auditLogger;
    }

    public async Task<Result<LoanVM>> Handle(GetLoanByIdQuery query, CancellationToken cancellationToken)
    {
        if (query.Id <= 0)
        {
            var error = new ValidationError("The supplied loan ID is invalid.");
            error.WithMetadata(nameof(query.Id), "Loan ID must be greater than zero.");
            return Result.Fail<LoanVM>(error);
        }

        var loan = await _loans.GetByIdAsync(query.Id, asNoTracking: true, cancellationToken);
        if (loan is null)
            return Result.Fail<LoanVM>(new EntityNotFoundError(nameof(BookLoan), query.Id));

        await _auditLogger.LogAsync(AuditActionEnum.View, nameof(BookLoan), loan.Id.ToString(),
            JsonSerializer.Serialize(new { Operation = "GetById" }), cancellationToken: cancellationToken);
        return Result.Ok(loan.ToViewModel());
    }
}
