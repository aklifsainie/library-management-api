using FluentResults;
using library.management.application.Interfaces.Repositories;
using library.management.application.Shared.Errors;
using library.management.application.Shared.Mediator;
using library.management.domain.Models.Entities;
using library.management.domain.Models.ViewModels.BookVM;
using library.management.domain.SharedKernel;

namespace library.management.application.Features.BookFeatures.Commands;

public sealed class AddBookCommand : IRequest<Result<BookVM>>
{
    public string Title { get; init; } = string.Empty;
    public string Author { get; init; } = string.Empty;
    public string? Isbn { get; init; }
}

public sealed class AddBookCommandHandler : IRequestHandler<AddBookCommand, Result<BookVM>>
{
    private readonly IBookRepository _books;
    private readonly IUnitOfWork _unitOfWork;

    public AddBookCommandHandler(IBookRepository books, IUnitOfWork unitOfWork)
    {
        _books = books;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<BookVM>> Handle(AddBookCommand command, CancellationToken cancellationToken)
    {
        var title = command.Title?.Trim() ?? string.Empty;
        var author = command.Author?.Trim() ?? string.Empty;
        var isbn = string.IsNullOrWhiteSpace(command.Isbn) ? null : command.Isbn.Trim();

        var error = new ValidationError("One or more book fields are invalid.");
        if (title.Length is < 1 or > 200)
            error.WithMetadata(nameof(command.Title), "Title must be between 1 and 200 characters.");
        if (author.Length is < 1 or > 200)
            error.WithMetadata(nameof(command.Author), "Author must be between 1 and 200 characters.");
        if (isbn?.Length > 17)
            error.WithMetadata(nameof(command.Isbn), "Isbn cannot exceed 17 characters.");
        if (error.Metadata.Count > 0)
            return Result.Fail<BookVM>(error);

        var book = new Book { Title = title, Author = author, Isbn = isbn };
        await _books.AddAsync(book, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Ok(new BookVM
        {
            BookId = book.Id,
            Title = book.Title,
            Author = book.Author,
            Isbn = book.Isbn
        }).WithSuccess("Book has been added successfully.");
    }
}
