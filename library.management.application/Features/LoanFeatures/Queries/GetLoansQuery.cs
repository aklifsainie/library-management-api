using System.Text.Json;
using FluentResults;
using library.management.application.Interfaces.Commons;
using library.management.application.Interfaces.Repositories;
using library.management.application.Shared.Errors;
using library.management.application.Shared.Mediator;
using library.management.domain.Enums;
using library.management.domain.Models.Entities;
using library.management.domain.Models.ViewModels.CommonVM;
using library.management.domain.Models.ViewModels.LoanVM;

namespace library.management.application.Features.LoanFeatures.Queries;

public sealed record GetLoansQuery(int PageNumber = 1, int PageSize = 20)
    : IRequest<Result<PaginatedResult<LoanVM>>>;

public sealed class GetLoansQueryHandler : IRequestHandler<GetLoansQuery, Result<PaginatedResult<LoanVM>>>
{
    private readonly IBookLoanRepository _loans;
    private readonly IAuditLogger _auditLogger;

    public GetLoansQueryHandler(IBookLoanRepository loans, IAuditLogger auditLogger)
    {
        _loans = loans;
        _auditLogger = auditLogger;
    }

    public async Task<Result<PaginatedResult<LoanVM>>> Handle(GetLoansQuery query, CancellationToken cancellationToken)
    {
        if (query.PageNumber < 1 || query.PageSize is < 1 or > 100 ||
            (query.PageSize > 0 && query.PageNumber > int.MaxValue / query.PageSize))
        {
            var error = new ValidationError("The supplied paging parameters are invalid.");
            if (query.PageNumber < 1)
                error.WithMetadata(nameof(query.PageNumber), "Page number must be greater than zero.");
            else if (query.PageSize > 0 && query.PageNumber > int.MaxValue / query.PageSize)
                error.WithMetadata(nameof(query.PageNumber), "Page number is too large for the page size.");
            if (query.PageSize is < 1 or > 100)
                error.WithMetadata(nameof(query.PageSize), "Page size must be between 1 and 100.");
            return Result.Fail<PaginatedResult<LoanVM>>(error);
        }

        var page = await _loans.GetPageAsync(query.PageNumber, query.PageSize, cancellationToken);
        await _auditLogger.LogAsync(AuditActionEnum.View, nameof(BookLoan), "LIST",
            JsonSerializer.Serialize(new { Operation = "GetPage", page.PageNumber, page.PageSize, page.TotalCount }),
            cancellationToken: cancellationToken);

        return Result.Ok(new PaginatedResult<LoanVM>
        {
            Items = page.Items.Select(loan => loan.ToViewModel()).ToList(),
            PageNumber = page.PageNumber,
            PageSize = page.PageSize,
            TotalCount = page.TotalCount
        });
    }
}
