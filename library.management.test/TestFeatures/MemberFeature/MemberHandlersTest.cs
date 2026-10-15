using library.management.application.Features.MemberFeatures;
using library.management.application.Interfaces.Repositories;
using library.management.application.Shared.Errors;
using library.management.domain.Models.Entities;
using library.management.domain.SharedKernel;
using Moq;

namespace library.management.test.TestFeatures.MemberFeature;

public sealed class MemberHandlersTest
{
    [Fact]
    public async Task Create_RequiresContactAndDoesNotSaveInvalidMembership()
    {
        var members = new Mock<IMemberRepository>();
        var work = new Mock<IUnitOfWork>();
        var handler = new CreateMemberCommandHandler(members.Object, work.Object);

        var result = await handler.Handle(new CreateMemberCommand("M-001", "Ada Reader", null, null), CancellationToken.None);

        Assert.IsType<ValidationError>(Assert.Single(result.Errors));
        work.Verify(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Create_NormalizesMemberNumberAndSaves()
    {
        var members = new Mock<IMemberRepository>();
        var work = new Mock<IUnitOfWork>();
        var handler = new CreateMemberCommandHandler(members.Object, work.Object);

        var result = await handler.Handle(new CreateMemberCommand(" m-001 ", " Ada Reader ", "ada@example.com", null), CancellationToken.None);

        Assert.True(result.IsSuccess);
        members.Verify(repository => repository.AddAsync(It.Is<Member>(member =>
            member.MemberNumber == "M-001" && member.FullName == "Ada Reader"), CancellationToken.None), Times.Once);
        work.Verify(unit => unit.SaveChangesAsync(CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task Create_RejectsReservedMemberNumber()
    {
        var members = new Mock<IMemberRepository>();
        var work = new Mock<IUnitOfWork>();
        members.Setup(repository => repository.IsMemberNumberTakenAsync("M-001", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await new CreateMemberCommandHandler(members.Object, work.Object)
            .Handle(new CreateMemberCommand("m-001", "Ada Reader", "ada@example.com", null), CancellationToken.None);

        Assert.IsType<ConflictError>(Assert.Single(result.Errors));
        work.Verify(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Deactivate_RejectsOpenLoan()
    {
        var members = new Mock<IMemberRepository>();
        var loans = new Mock<IBookLoanRepository>();
        var work = new Mock<IUnitOfWork>();
        members.Setup(repository => repository.GetByIdAsync(7, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Member { Id = 7, IsActive = true });
        loans.Setup(repository => repository.HasOpenLoanForMemberAsync(7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await new DeactivateMemberCommandHandler(members.Object, loans.Object, work.Object)
            .Handle(new DeactivateMemberCommand(7), CancellationToken.None);

        Assert.IsType<ConflictError>(Assert.Single(result.Errors));
        work.Verify(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
