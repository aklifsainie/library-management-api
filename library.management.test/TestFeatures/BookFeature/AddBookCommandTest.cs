using library.management.application.Features.BookFeatures.Commands;
using library.management.application.Interfaces.Repositories;
using library.management.application.Shared.Errors;
using library.management.domain.Models.Entities;
using library.management.domain.SharedKernel;
using Moq;

namespace library.management.test.TestFeatures.BookFeature;

public sealed class AddBookCommandTest
{
    [Fact]
    public async Task Handle_ValidBook_SavesAndReturnsCreatedBook()
    {
        var books = new Mock<IBookRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        using var cancellation = new CancellationTokenSource();
        Book? added = null;

        books.Setup(repository => repository.AddAsync(It.IsAny<Book>(), cancellation.Token))
            .ReturnsAsync((Book book, CancellationToken _) =>
            {
                added = book;
                return book;
            });
        unitOfWork.Setup(work => work.SaveChangesAsync(cancellation.Token))
            .ReturnsAsync(() =>
            {
                added!.Id = 42;
                return 1;
            });

        var handler = new AddBookCommandHandler(books.Object, unitOfWork.Object);
        var result = await handler.Handle(new AddBookCommand
        {
            Title = "  The Hobbit  ",
            Author = "  J. R. R. Tolkien  ",
            Isbn = " 9780261103344 "
        }, cancellation.Token);

        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value.BookId);
        Assert.Equal("The Hobbit", result.Value.Title);
        Assert.Equal("J. R. R. Tolkien", result.Value.Author);
        Assert.Equal("9780261103344", result.Value.Isbn);
        books.Verify(repository => repository.AddAsync(It.IsAny<Book>(), cancellation.Token), Times.Once);
        unitOfWork.Verify(work => work.SaveChangesAsync(cancellation.Token), Times.Once);
    }

    [Fact]
    public async Task Handle_BlankTitle_ReturnsValidationErrorWithoutSaving()
    {
        var books = new Mock<IBookRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var handler = new AddBookCommandHandler(books.Object, unitOfWork.Object);

        var result = await handler.Handle(new AddBookCommand
        {
            Title = "   ", Author = "Author"
        }, CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.Contains(result.Errors, error => error is ValidationError);
        books.Verify(repository => repository.AddAsync(It.IsAny<Book>(), It.IsAny<CancellationToken>()), Times.Never);
        unitOfWork.Verify(work => work.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Validator_RejectsTooLongIsbn()
    {
        var validator = new AddBookCommandValidator();
        var result = await validator.ValidateAsync(new AddBookCommand
        {
            Title = "Book", Author = "Author", Isbn = new string('1', 18)
        });

        Assert.Contains(result.Errors, error => error.PropertyName == nameof(AddBookCommand.Isbn));
    }
}
