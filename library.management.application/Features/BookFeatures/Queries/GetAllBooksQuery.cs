using System.Text.Json;
using FluentResults;
using library.management.application.Interfaces.Commons;
using library.management.application.Interfaces.Repositories;
using library.management.application.Shared.Mediator;
using library.management.domain.Enums;
using library.management.domain.Models.Entities;
using library.management.domain.Models.ViewModels.BookVM;

namespace library.management.application.Features.BookFeatures.Queries;

public sealed record GetAllBooksQuery : IRequest<Result<List<BookVM>>>;

public sealed class GetAllBooksQueryHandler : IRequestHandler<GetAllBooksQuery, Result<List<BookVM>>>
{
    private readonly IBookRepository _books;
    private readonly IAuditLogger _auditLogger;

    public GetAllBooksQueryHandler(IBookRepository books, IAuditLogger auditLogger)
    {
        _books = books;
        _auditLogger = auditLogger;
    }

    public async Task<Result<List<BookVM>>> Handle(GetAllBooksQuery query, CancellationToken cancellationToken)
    {
        var entities = await _books.GetAsync(book => book.IsActive, asNoTracking: true,
            cancellationToken: cancellationToken);
        var books = entities.OrderBy(book => book.Title, StringComparer.OrdinalIgnoreCase)
            .ThenBy(book => book.Id)
            .Select(book => new BookVM
            {
                BookId = book.Id,
                Title = book.Title,
                Author = book.Author,
                Isbn = book.Isbn
            }).ToList();

        await _auditLogger.LogAsync(AuditActionEnum.View, nameof(Book), "LIST",
            JsonSerializer.Serialize(new { Operation = "GetAll", ResultCount = books.Count }),
            cancellationToken: cancellationToken);

        return Result.Ok(books);
    }
}
