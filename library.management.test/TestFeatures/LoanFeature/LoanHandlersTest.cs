using System.Linq.Expressions;
using library.management.application.Features.LoanFeatures.Commands;
using library.management.application.Features.LoanFeatures.Queries;
using library.management.application.Interfaces.Commons;
using library.management.application.Interfaces.Repositories;
using library.management.application.Interfaces.Services;
using library.management.application.Shared.Errors;
using library.management.domain.Models.Entities;
using library.management.domain.Models.ViewModels.CommonVM;
using library.management.domain.SharedKernel;
using library.management.identity;
using library.management.identity.Interfaces;
using Moq;

namespace library.management.test.TestFeatures.LoanFeature;

public sealed class LoanHandlersTest
{
    [Fact]
    public async Task Borrow_UsesSignedInMemberAndCreatesActiveLoan()
    {
        var books = new Mock<IBookRepository>();
        var loans = new Mock<IBookLoanRepository>();
        var current = CurrentUser("member-1", UserRoles.Member);
        var users = new Mock<ILoanUserService>();
        users.Setup(service => service.IsActiveAsync("member-1", It.IsAny<CancellationToken>())).ReturnsAsync(true);
        books.Setup(repository => repository.FirstOrDefaultAsync(It.IsAny<Expression<Func<Book, bool>>>(),
            true, It.IsAny<CancellationToken>())).ReturnsAsync(new Book { Id = 3 });
        loans.Setup(repository => repository.TryBorrowAsync(It.IsAny<BookLoan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await new BorrowBookCommandHandler(books.Object, loans.Object, current.Object, users.Object)
            .Handle(new BorrowBookCommand(3), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("member-1", result.Value.UserId);
        Assert.Null(result.Value.ReturnedAtUtc);
        loans.Verify(repository => repository.TryBorrowAsync(It.Is<BookLoan>(loan =>
            loan.BookId == 3 && loan.UserId == "member-1" && loan.ReturnedAtUtc == null),
            CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task Borrow_AlreadyLoaned_ReturnsConflictWithoutCreatingLoan()
    {
        var books = new Mock<IBookRepository>();
        var loans = new Mock<IBookLoanRepository>();
        var users = new Mock<ILoanUserService>();
        users.Setup(service => service.IsActiveAsync("member-1", It.IsAny<CancellationToken>())).ReturnsAsync(true);
        books.Setup(repository => repository.FirstOrDefaultAsync(It.IsAny<Expression<Func<Book, bool>>>(),
            true, It.IsAny<CancellationToken>())).ReturnsAsync(new Book { Id = 3 });
        loans.Setup(repository => repository.HasOpenLoanAsync(3, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var result = await new BorrowBookCommandHandler(books.Object, loans.Object,
            CurrentUser("member-1", UserRoles.Member).Object, users.Object)
            .Handle(new BorrowBookCommand(3), CancellationToken.None);

        Assert.IsType<ConflictError>(Assert.Single(result.Errors));
        loans.Verify(repository => repository.TryBorrowAsync(It.IsAny<BookLoan>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Borrow_RacingInsertConflict_ReturnsConflict()
    {
        var books = new Mock<IBookRepository>();
        var loans = new Mock<IBookLoanRepository>();
        var users = new Mock<ILoanUserService>();
        users.Setup(service => service.IsActiveAsync("member-1", It.IsAny<CancellationToken>())).ReturnsAsync(true);
        books.Setup(repository => repository.FirstOrDefaultAsync(It.IsAny<Expression<Func<Book, bool>>>(),
            true, It.IsAny<CancellationToken>())).ReturnsAsync(new Book { Id = 3 });
        loans.Setup(repository => repository.TryBorrowAsync(It.IsAny<BookLoan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await new BorrowBookCommandHandler(books.Object, loans.Object,
            CurrentUser("member-1", UserRoles.Member).Object, users.Object)
            .Handle(new BorrowBookCommand(3), CancellationToken.None);

        Assert.IsType<ConflictError>(Assert.Single(result.Errors));
    }

    [Fact]
    public async Task Borrow_UnavailableBook_ReturnsNotFound()
    {
        var books = new Mock<IBookRepository>();
        var loans = new Mock<IBookLoanRepository>();
        var users = new Mock<ILoanUserService>();
        users.Setup(service => service.IsActiveAsync("member-1", It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var result = await new BorrowBookCommandHandler(books.Object, loans.Object,
            CurrentUser("member-1", UserRoles.Member).Object, users.Object)
            .Handle(new BorrowBookCommand(3), CancellationToken.None);

        Assert.IsType<EntityNotFoundError>(Assert.Single(result.Errors));
        loans.Verify(repository => repository.TryBorrowAsync(It.IsAny<BookLoan>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Member_CannotReadOrReturnAnotherMembersLoan()
    {
        var loans = new Mock<IBookLoanRepository>();
        var audit = new Mock<IAuditLogger>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var current = CurrentUser("member-1", UserRoles.Member);
        loans.Setup(repository => repository.GetByIdAsync(5, It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BookLoan { Id = 5, UserId = "member-2", BookId = 3 });

        var read = await new GetLoanByIdQueryHandler(loans.Object, current.Object, audit.Object)
            .Handle(new GetLoanByIdQuery(5), CancellationToken.None);
        var returned = await new ReturnBookCommandHandler(loans.Object, unitOfWork.Object, current.Object)
            .Handle(new ReturnBookCommand(5), CancellationToken.None);

        Assert.IsType<EntityNotFoundError>(Assert.Single(read.Errors));
        Assert.IsType<EntityNotFoundError>(Assert.Single(returned.Errors));
        unitOfWork.Verify(work => work.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Admin_CanReturnLoanEvenWhenBookIsUnavailable()
    {
        var loans = new Mock<IBookLoanRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var loan = new BookLoan { Id = 5, UserId = "member-2", BookId = 3 };
        loans.Setup(repository => repository.GetByIdAsync(5, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(loan);

        var result = await new ReturnBookCommandHandler(loans.Object, unitOfWork.Object,
            CurrentUser("admin-1", UserRoles.Admin).Object)
            .Handle(new ReturnBookCommand(5), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(loan.ReturnedAtUtc);
        unitOfWork.Verify(work => work.SaveChangesAsync(CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task Return_AlreadyReturned_ReturnsConflictWithoutSaving()
    {
        var loans = new Mock<IBookLoanRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        loans.Setup(repository => repository.GetByIdAsync(5, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BookLoan
            {
                Id = 5, UserId = "member-1", BookId = 3, ReturnedAtUtc = DateTime.UtcNow
            });

        var result = await new ReturnBookCommandHandler(loans.Object, unitOfWork.Object,
            CurrentUser("member-1", UserRoles.Member).Object)
            .Handle(new ReturnBookCommand(5), CancellationToken.None);

        Assert.IsType<ConflictError>(Assert.Single(result.Errors));
        unitOfWork.Verify(work => work.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task List_FiltersMembersAndRejectsOversizedPages()
    {
        var loans = new Mock<IBookLoanRepository>();
        var audit = new Mock<IAuditLogger>();
        loans.Setup(repository => repository.GetPageAsync("member-1", 2, 20,
            It.IsAny<CancellationToken>())).ReturnsAsync(new PaginatedResult<BookLoan>
        {
            Items = new List<BookLoan> { new() { Id = 8, UserId = "member-1", BookId = 3 } },
            PageNumber = 2, PageSize = 20, TotalCount = 21
        });
        var handler = new GetLoansQueryHandler(loans.Object,
            CurrentUser("member-1", UserRoles.Member).Object, audit.Object);

        var page = await handler.Handle(new GetLoansQuery(2, 20), CancellationToken.None);
        var invalid = await handler.Handle(new GetLoansQuery(1, 101), CancellationToken.None);

        Assert.True(page.IsSuccess);
        Assert.Equal(21, page.Value.TotalCount);
        Assert.Single(page.Value.Items);
        Assert.IsType<ValidationError>(Assert.Single(invalid.Errors));
        loans.Verify(repository => repository.GetPageAsync(It.IsAny<string>(), It.IsAny<int>(),
            It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    private static Mock<ICurrentUserService> CurrentUser(string userId, string role)
    {
        var current = new Mock<ICurrentUserService>();
        current.SetupGet(service => service.UserId).Returns(userId);
        current.SetupGet(service => service.UserRole).Returns(role);
        return current;
    }
}
