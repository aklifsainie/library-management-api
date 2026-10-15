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
using Moq;

namespace library.management.test.TestFeatures.LoanFeature;

public sealed class LoanHandlersTest
{
    [Fact]
    public async Task Checkout_RecordsMembershipAndApprovingOfficer()
    {
        var books = new Mock<IBookRepository>();
        var loans = new Mock<IBookLoanRepository>();
        books.Setup(repository => repository.FirstOrDefaultAsync(It.IsAny<Expression<Func<Book, bool>>>(),
            true, It.IsAny<CancellationToken>())).ReturnsAsync(new Book { Id = 3, IsActive = true });
        loans.Setup(repository => repository.TryBorrowAsync(It.IsAny<BookLoan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await new BorrowBookCommandHandler(books.Object, loans.Object, Officer(), ActiveMember().Object)
            .Handle(new BorrowBookCommand(3, 7), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(7, result.Value.MemberId);
        Assert.Equal("officer-1", result.Value.CheckoutOfficerUserId);
        loans.Verify(repository => repository.TryBorrowAsync(It.Is<BookLoan>(loan =>
            loan.BookId == 3 && loan.MemberId == 7 && loan.CheckoutOfficerUserId == "officer-1" &&
            loan.ReturnedAtUtc == null), CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task Checkout_RejectsInactiveMembership()
    {
        var books = new Mock<IBookRepository>();
        var loans = new Mock<IBookLoanRepository>();
        var members = new Mock<IMemberRepository>();
        members.Setup(repository => repository.GetByIdAsync(7, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Member { Id = 7, IsActive = false });

        var result = await new BorrowBookCommandHandler(books.Object, loans.Object, Officer(), members.Object)
            .Handle(new BorrowBookCommand(3, 7), CancellationToken.None);

        Assert.IsType<EntityNotFoundError>(Assert.Single(result.Errors));
        loans.Verify(repository => repository.TryBorrowAsync(It.IsAny<BookLoan>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Checkout_AlreadyLoaned_ReturnsConflict()
    {
        var books = new Mock<IBookRepository>();
        var loans = new Mock<IBookLoanRepository>();
        books.Setup(repository => repository.FirstOrDefaultAsync(It.IsAny<Expression<Func<Book, bool>>>(),
            true, It.IsAny<CancellationToken>())).ReturnsAsync(new Book { Id = 3, IsActive = true });
        loans.Setup(repository => repository.HasOpenLoanAsync(3, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var result = await new BorrowBookCommandHandler(books.Object, loans.Object, Officer(), ActiveMember().Object)
            .Handle(new BorrowBookCommand(3, 7), CancellationToken.None);

        Assert.IsType<ConflictError>(Assert.Single(result.Errors));
    }

    [Fact]
    public async Task Return_RequiresMatchingMemberAndBook()
    {
        var loans = new Mock<IBookLoanRepository>();
        var work = new Mock<IUnitOfWork>();
        loans.Setup(repository => repository.GetByIdAsync(5, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BookLoan { Id = 5, MemberId = 7, BookId = 3 });

        var result = await new ReturnBookCommandHandler(loans.Object, work.Object, Officer())
            .Handle(new ReturnBookCommand(5, 8, 3), CancellationToken.None);

        Assert.IsType<ConflictError>(Assert.Single(result.Errors));
        work.Verify(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Return_RecordsReceivingOfficer()
    {
        var loans = new Mock<IBookLoanRepository>();
        var work = new Mock<IUnitOfWork>();
        var loan = new BookLoan { Id = 5, MemberId = 7, BookId = 3 };
        loans.Setup(repository => repository.GetByIdAsync(5, false, It.IsAny<CancellationToken>())).ReturnsAsync(loan);

        var result = await new ReturnBookCommandHandler(loans.Object, work.Object, Officer())
            .Handle(new ReturnBookCommand(5, 7, 3), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(loan.ReturnedAtUtc);
        Assert.Equal("officer-1", loan.ReturnOfficerUserId);
        work.Verify(unit => unit.SaveChangesAsync(CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task LoanHistory_ValidatesPaging()
    {
        var loans = new Mock<IBookLoanRepository>();
        var audit = new Mock<IAuditLogger>();
        loans.Setup(repository => repository.GetPageAsync(2, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaginatedResult<BookLoan>
            {
                Items = new List<BookLoan> { new() { Id = 8, MemberId = 7, BookId = 3 } },
                PageNumber = 2, PageSize = 20, TotalCount = 21
            });
        var handler = new GetLoansQueryHandler(loans.Object, audit.Object);

        var page = await handler.Handle(new GetLoansQuery(2, 20), CancellationToken.None);
        var invalid = await handler.Handle(new GetLoansQuery(1, 101), CancellationToken.None);

        Assert.True(page.IsSuccess);
        Assert.Equal(7, Assert.Single(page.Value.Items).MemberId);
        Assert.IsType<ValidationError>(Assert.Single(invalid.Errors));
    }

    private static Mock<IMemberRepository> ActiveMember()
    {
        var members = new Mock<IMemberRepository>();
        members.Setup(repository => repository.GetByIdAsync(7, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Member { Id = 7, IsActive = true });
        return members;
    }

    private static ICurrentUserService Officer()
    {
        var current = new Mock<ICurrentUserService>();
        current.SetupGet(service => service.UserId).Returns("officer-1");
        return current.Object;
    }
}
