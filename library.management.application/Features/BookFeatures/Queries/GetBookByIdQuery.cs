using System.Text.Json;
using FluentResults;
using library.management.application.Interfaces.Commons;
using library.management.application.Interfaces.Repositories;
using library.management.application.Shared.Errors;
using library.management.application.Shared.Mediator;
using library.management.domain.Enums;
using library.management.domain.Models.Entities;
using library.management.domain.Models.ViewModels.BookVM;

namespace library.management.application.Features.BookFeatures.Queries;

public sealed record GetBookByIdQuery(int Id) : IRequest<Result<BookVM>>;

public sealed class GetBookByIdQueryHandler : IRequestHandler<GetBookByIdQuery, Result<BookVM>>
{
    private readonly IBookRepository _books;
    private readonly IAuditLogger _auditLogger;

    public GetBookByIdQueryHandler(IBookRepository books, IAuditLogger auditLogger)
    {
        _books = books;
        _auditLogger = auditLogger;
    }

    public async Task<Result<BookVM>> Handle(GetBookByIdQuery query, CancellationToken cancellationToken)
    {
        if (query.Id <= 0)
        {
            var error = new ValidationError("The supplied book ID is invalid.");
            error.WithMetadata(nameof(query.Id), "Book ID must be greater than zero.");
            return Result.Fail<BookVM>(error);
        }

        var book = await _books.FirstOrDefaultAsync(item => item.Id == query.Id && item.IsActive,
            asNoTracking: true, cancellationToken: cancellationToken);
        if (book is null)
            return Result.Fail<BookVM>(new EntityNotFoundError(nameof(Book), query.Id));

        var result = new BookVM
        {
            BookId = book.Id,
            Title = book.Title,
            Author = book.Author,
            Isbn = book.Isbn
        };

        await _auditLogger.LogAsync(AuditActionEnum.View, nameof(Book), book.Id.ToString(),
            JsonSerializer.Serialize(new { Operation = "GetById" }),
            cancellationToken: cancellationToken);

        return Result.Ok(result);
    }
}
