using FluentResults;
using library.management.application.Interfaces.Repositories;
using library.management.application.Shared.Errors;
using library.management.application.Shared.Mediator;
using library.management.domain.Models.Entities;
using library.management.domain.SharedKernel;

namespace library.management.application.Features.BookFeatures.Commands;

public sealed record DeleteBookCommand(int Id) : IRequest<Result<bool>>;

public sealed class DeleteBookCommandHandler : IRequestHandler<DeleteBookCommand, Result<bool>>
{
    private readonly IBookRepository _books;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteBookCommandHandler(IBookRepository books, IUnitOfWork unitOfWork)
    {
        _books = books;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<bool>> Handle(DeleteBookCommand command, CancellationToken cancellationToken)
    {
        if (command.Id <= 0)
        {
            var error = new ValidationError("The supplied book ID is invalid.");
            error.WithMetadata(nameof(command.Id), "Book ID must be greater than zero.");
            return Result.Fail<bool>(error);
        }

        var book = await _books.FirstOrDefaultAsync(item => item.Id == command.Id && item.IsActive,
            asNoTracking: false, cancellationToken: cancellationToken);
        if (book is null)
            return Result.Fail<bool>(new EntityNotFoundError(nameof(Book), command.Id));

        book.IsDelete = true;
        book.IsActive = false;
        book.UpdatedDate = DateTime.UtcNow;
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Ok(true).WithSuccess("Book has been deleted successfully.");
    }
}
