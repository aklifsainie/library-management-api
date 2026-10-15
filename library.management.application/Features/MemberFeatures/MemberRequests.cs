using FluentResults;
using library.management.application.Interfaces.Commons;
using library.management.application.Interfaces.Repositories;
using library.management.application.Shared.Errors;
using library.management.application.Shared.Mediator;
using library.management.domain.Models.Entities;
using library.management.domain.Models.ViewModels.MemberVM;
using library.management.domain.SharedKernel;
using library.management.domain.Enums;

namespace library.management.application.Features.MemberFeatures;

public sealed record CreateMemberCommand(string MemberNumber, string FullName, string? Email, string? PhoneNumber)
    : IRequest<Result<MemberVM>>;
public sealed record UpdateMemberCommand(int Id, string FullName, string? Email, string? PhoneNumber, bool IsActive)
    : IRequest<Result<MemberVM>>;
public sealed record GetMemberByIdQuery(int Id) : IRequest<Result<MemberVM>>;
public sealed record GetMembersQuery : IRequest<Result<List<MemberVM>>>;
public sealed record DeactivateMemberCommand(int Id) : IRequest<Result<bool>>;

internal static class MemberRules
{
    internal static MemberVM Map(Member member) => new()
    {
        Id = member.Id, MemberNumber = member.MemberNumber, FullName = member.FullName,
        Email = member.Email, PhoneNumber = member.PhoneNumber, IsActive = member.IsActive
    };

    internal static ValidationError? Validate(string? number, string? name, string? email, string? phone)
    {
        var error = new ValidationError("One or more membership fields are invalid.");
        if (number is not null && (number.Length is < 1 or > 64))
            error.WithMetadata("MemberNumber", "Member number must be between 1 and 64 characters.");
        if (name is null || name.Length is < 1 or > 200)
            error.WithMetadata("FullName", "Full name must be between 1 and 200 characters.");
        if (email?.Length > 256 || (email is not null && !new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(email)))
            error.WithMetadata("Email", "Email must be a valid address of at most 256 characters.");
        if (phone?.Length > 32)
            error.WithMetadata("PhoneNumber", "Phone number cannot exceed 32 characters.");
        if (email is null && phone is null)
            error.WithMetadata("Contact", "An email address or phone number is required.");
        return error.Metadata.Count == 0 ? null : error;
    }

    internal static string? Optional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

public sealed class CreateMemberCommandHandler(IMemberRepository members, IUnitOfWork work)
    : IRequestHandler<CreateMemberCommand, Result<MemberVM>>
{
    public async Task<Result<MemberVM>> Handle(CreateMemberCommand command, CancellationToken cancellationToken)
    {
        var number = command.MemberNumber?.Trim().ToUpperInvariant() ?? string.Empty;
        var name = command.FullName?.Trim() ?? string.Empty;
        var email = MemberRules.Optional(command.Email);
        var phone = MemberRules.Optional(command.PhoneNumber);
        var error = MemberRules.Validate(number, name, email, phone);
        if (error is not null) return Result.Fail<MemberVM>(error);
        if (await members.IsMemberNumberTakenAsync(number, cancellationToken))
            return Result.Fail<MemberVM>(new ConflictError("Member number already exists."));

        var member = new Member { MemberNumber = number, FullName = name, Email = email, PhoneNumber = phone };
        await members.AddAsync(member, cancellationToken);
        await work.SaveChangesAsync(cancellationToken);
        return Result.Ok(MemberRules.Map(member));
    }
}

public sealed class UpdateMemberCommandHandler(IMemberRepository members, IBookLoanRepository loans, IUnitOfWork work)
    : IRequestHandler<UpdateMemberCommand, Result<MemberVM>>
{
    public async Task<Result<MemberVM>> Handle(UpdateMemberCommand command, CancellationToken cancellationToken)
    {
        var name = command.FullName?.Trim() ?? string.Empty;
        var email = MemberRules.Optional(command.Email);
        var phone = MemberRules.Optional(command.PhoneNumber);
        var error = MemberRules.Validate(null, name, email, phone);
        if (error is not null) return Result.Fail<MemberVM>(error);
        var member = await members.GetByIdAsync(command.Id, false, cancellationToken);
        if (member is null) return Result.Fail<MemberVM>(new EntityNotFoundError(nameof(Member), command.Id));
        if (!command.IsActive && await loans.HasOpenLoanForMemberAsync(member.Id, cancellationToken))
            return Result.Fail<MemberVM>(new ConflictError("Member has an open loan."));
        member.FullName = name;
        member.Email = email;
        member.PhoneNumber = phone;
        member.IsActive = command.IsActive;
        await work.SaveChangesAsync(cancellationToken);
        return Result.Ok(MemberRules.Map(member));
    }
}

public sealed class GetMemberByIdQueryHandler(IMemberRepository members, IAuditLogger audit)
    : IRequestHandler<GetMemberByIdQuery, Result<MemberVM>>
{
    public async Task<Result<MemberVM>> Handle(GetMemberByIdQuery query, CancellationToken cancellationToken)
    {
        var member = await members.GetByIdAsync(query.Id, true, cancellationToken);
        if (member is null) return Result.Fail<MemberVM>(new EntityNotFoundError(nameof(Member), query.Id));
        await audit.LogAsync(AuditActionEnum.View, nameof(Member), member.Id.ToString(),
            "{\"Operation\":\"GetById\"}", cancellationToken: cancellationToken);
        return Result.Ok(MemberRules.Map(member));
    }
}

public sealed class GetMembersQueryHandler(IMemberRepository members, IAuditLogger audit)
    : IRequestHandler<GetMembersQuery, Result<List<MemberVM>>>
{
    public async Task<Result<List<MemberVM>>> Handle(GetMembersQuery query, CancellationToken cancellationToken)
    {
        var membersFound = await members.GetAllAsync(true, cancellationToken);
        await audit.LogAsync(AuditActionEnum.View, nameof(Member), "LIST",
            "{\"Operation\":\"GetAll\"}", cancellationToken: cancellationToken);
        return Result.Ok(membersFound.OrderBy(member => member.MemberNumber).Select(MemberRules.Map).ToList());
    }
}

public sealed class DeactivateMemberCommandHandler(IMemberRepository members, IBookLoanRepository loans, IUnitOfWork work)
    : IRequestHandler<DeactivateMemberCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(DeactivateMemberCommand command, CancellationToken cancellationToken)
    {
        var member = await members.GetByIdAsync(command.Id, false, cancellationToken);
        if (member is null) return Result.Fail<bool>(new EntityNotFoundError(nameof(Member), command.Id));
        if (await loans.HasOpenLoanForMemberAsync(member.Id, cancellationToken))
            return Result.Fail<bool>(new ConflictError("Member has an open loan."));
        member.IsActive = false;
        member.IsDelete = true;
        await work.SaveChangesAsync(cancellationToken);
        return Result.Ok(true);
    }
}
