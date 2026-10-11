using FluentResults;
using library.management.application.Interfaces.Repositories;
using library.management.application.Shared.Errors;
using library.management.application.Shared.Mediator;
using library.management.domain.Models.Entities;
using library.management.domain.Models.ViewModels.BookVM;
using library.management.domain.SharedKernel;

namespace library.management.application.Features.BookFeatures.Commands;

public sealed class UpdateBookCommand : IRequest<Result<BookVM>>
{
    public int Id { get; set; }
    public string Title { get; init; } = string.Empty;
    public string Author { get; init; } = string.Empty;
    public string? Isbn { get; init; }
}

public sealed class UpdateBookCommandHandler : IRequestHandler<UpdateBookCommand, Result<BookVM>>
{
    private readonly IBookRepository _books;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateBookCommandHandler(IBookRepository books, IUnitOfWork unitOfWork)
    {
        _books = books;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<BookVM>> Handle(UpdateBookCommand command, CancellationToken cancellationToken)
    {
        var title = command.Title?.Trim() ?? string.Empty;
        var author = command.Author?.Trim() ?? string.Empty;
        var isbn = string.IsNullOrWhiteSpace(command.Isbn) ? null : command.Isbn.Trim();

        var error = new ValidationError("One or more book fields are invalid.");
        if (command.Id <= 0)
            error.WithMetadata(nameof(command.Id), "Book ID must be greater than zero.");
        if (title.Length is < 1 or > 200)
            error.WithMetadata(nameof(command.Title), "Title must be between 1 and 200 characters.");
        if (author.Length is < 1 or > 200)
            error.WithMetadata(nameof(command.Author), "Author must be between 1 and 200 characters.");
        if (isbn?.Length > 17)
            error.WithMetadata(nameof(command.Isbn), "Isbn cannot exceed 17 characters.");
        if (error.Metadata.Count > 0)
            return Result.Fail<BookVM>(error);

        var book = await _books.FirstOrDefaultAsync(item => item.Id == command.Id && item.IsActive,
            asNoTracking: false, cancellationToken: cancellationToken);
        if (book is null)
            return Result.Fail<BookVM>(new EntityNotFoundError(nameof(Book), command.Id));

        book.Title = title;
        book.Author = author;
        book.Isbn = isbn;
        book.UpdatedDate = DateTime.UtcNow;
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Ok(new BookVM
        {
            BookId = book.Id,
            Title = book.Title,
            Author = book.Author,
            Isbn = book.Isbn
        }).WithSuccess("Book has been updated successfully.");
    }
}
