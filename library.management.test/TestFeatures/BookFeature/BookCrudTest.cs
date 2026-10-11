using System.Linq.Expressions;
using library.management.application.Features.BookFeatures.Commands;
using library.management.application.Features.BookFeatures.Queries;
using library.management.application.Interfaces.Commons;
using library.management.application.Interfaces.Repositories;
using library.management.application.Shared.Errors;
using library.management.domain.Enums;
using library.management.domain.Models.Entities;
using library.management.domain.SharedKernel;
using Moq;

namespace library.management.test.TestFeatures.BookFeature;

public sealed class BookCrudTest
{
    [Fact]
    public async Task GetAll_ReturnsOnlyActiveBooksInStableOrder()
    {
        var repository = new Mock<IBookRepository>();
        var audit = new Mock<IAuditLogger>();
        var source = new[]
        {
            new Book { Id = 3, Title = "Zebra", Author = "Author" },
            new Book { Id = 2, Title = "Alpha", Author = "Author", IsActive = false },
            new Book { Id = 1, Title = "alpha", Author = "Author" },
            new Book { Id = 4, Title = "Deleted", Author = "Author", IsDelete = true }
        };
        repository.Setup(value => value.GetAsync(It.IsAny<Expression<Func<Book, bool>>>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Expression<Func<Book, bool>> predicate, bool ignoredTracking, CancellationToken ignoredCancellation) =>
                source.Where(book => !book.IsDelete).Where(predicate.Compile()).ToList());

        var result = await new GetAllBooksQueryHandler(repository.Object, audit.Object)
            .Handle(new GetAllBooksQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(new[] { 1, 3 }, result.Value.Select(book => book.BookId));
        audit.Verify(value => value.LogAsync(AuditActionEnum.View, nameof(Book), "LIST",
            It.IsAny<string>(), null, null, null, CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task GetById_InactiveBook_ReturnsNotFoundWithoutViewAudit()
    {
        var repository = new Mock<IBookRepository>();
        var audit = new Mock<IAuditLogger>();
        repository.Setup(value => value.FirstOrDefaultAsync(It.IsAny<Expression<Func<Book, bool>>>(), true,
            It.IsAny<CancellationToken>())).ReturnsAsync((Book?)null);

        var result = await new GetBookByIdQueryHandler(repository.Object, audit.Object)
            .Handle(new GetBookByIdQuery(7), CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.IsType<EntityNotFoundError>(result.Errors.Single());
        audit.Verify(value => value.LogAsync(It.IsAny<AuditActionEnum>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetById_ActiveBook_ReturnsDetailsAndAuditsView()
    {
        var repository = new Mock<IBookRepository>();
        var audit = new Mock<IAuditLogger>();
        var book = new Book { Id = 7, Title = "Title", Author = "Author", Isbn = "123" };
        repository.Setup(value => value.FirstOrDefaultAsync(It.IsAny<Expression<Func<Book, bool>>>(), true,
            It.IsAny<CancellationToken>())).ReturnsAsync(book);

        var result = await new GetBookByIdQueryHandler(repository.Object, audit.Object)
            .Handle(new GetBookByIdQuery(7), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(7, result.Value.BookId);
        Assert.Equal("Title", result.Value.Title);
        Assert.Equal("123", result.Value.Isbn);
        audit.Verify(value => value.LogAsync(AuditActionEnum.View, nameof(Book), "7",
            It.IsAny<string>(), null, null, null, CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task Update_TrackedBook_TrimsFieldsAndSaves()
    {
        var repository = new Mock<IBookRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var book = new Book { Id = 8, Title = "Old", Author = "Old Author" };
        repository.Setup(value => value.FirstOrDefaultAsync(It.IsAny<Expression<Func<Book, bool>>>(), false,
            It.IsAny<CancellationToken>())).ReturnsAsync(book);

        var result = await new UpdateBookCommandHandler(repository.Object, unitOfWork.Object)
            .Handle(new UpdateBookCommand
            {
                Id = 8, Title = " New Title ", Author = " New Author ", Isbn = " 1234567890 "
            }, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("New Title", book.Title);
        Assert.Equal("New Author", book.Author);
        Assert.Equal("1234567890", result.Value.Isbn);
        repository.Verify(value => value.Update(It.IsAny<Book>()), Times.Never);
        unitOfWork.Verify(value => value.SaveChangesAsync(CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task Update_InvalidTitle_DoesNotLoadOrSave()
    {
        var repository = new Mock<IBookRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();

        var result = await new UpdateBookCommandHandler(repository.Object, unitOfWork.Object)
            .Handle(new UpdateBookCommand { Id = 8, Title = " ", Author = "Author" }, CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.IsType<ValidationError>(result.Errors.Single());
        repository.Verify(value => value.FirstOrDefaultAsync(It.IsAny<Expression<Func<Book, bool>>>(),
            It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
        unitOfWork.Verify(value => value.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Update_MissingBook_ReturnsNotFoundWithoutSaving()
    {
        var repository = new Mock<IBookRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();

        var result = await new UpdateBookCommandHandler(repository.Object, unitOfWork.Object)
            .Handle(new UpdateBookCommand { Id = 8, Title = "Title", Author = "Author" }, CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.IsType<EntityNotFoundError>(result.Errors.Single());
        unitOfWork.Verify(value => value.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Delete_TrackedBook_SoftDeletesAndSaves()
    {
        var repository = new Mock<IBookRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var book = new Book { Id = 9, Title = "Title", Author = "Author" };
        repository.Setup(value => value.FirstOrDefaultAsync(It.IsAny<Expression<Func<Book, bool>>>(), false,
            It.IsAny<CancellationToken>())).ReturnsAsync(book);

        var result = await new DeleteBookCommandHandler(repository.Object, unitOfWork.Object)
            .Handle(new DeleteBookCommand(9), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(book.IsDelete);
        Assert.False(book.IsActive);
        repository.Verify(value => value.Remove(It.IsAny<Book>()), Times.Never);
        unitOfWork.Verify(value => value.SaveChangesAsync(CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task Delete_MissingBook_DoesNotSave()
    {
        var repository = new Mock<IBookRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();

        var result = await new DeleteBookCommandHandler(repository.Object, unitOfWork.Object)
            .Handle(new DeleteBookCommand(9), CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.IsType<EntityNotFoundError>(result.Errors.Single());
        unitOfWork.Verify(value => value.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
