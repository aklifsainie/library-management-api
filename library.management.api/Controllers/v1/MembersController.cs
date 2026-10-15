using library.management.application.Features.MemberFeatures;
using library.management.application.Shared;
using library.management.application.Shared.Mediator;
using library.management.domain.Models.ViewModels.MemberVM;
using library.management.identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace library.management.api.Controllers.v1;

[ApiController]
[Route("api/v1/members")]
[Authorize(AuthenticationSchemes = "Bearer", Roles = UserRoles.Officer)]
[EnableRateLimiting("ApiPolicy")]
public sealed class MembersController(IMediator mediator) : BaseApiController
{
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateMemberCommand command, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(command, cancellationToken);
        return result.IsFailed ? ToActionResult(result) : StatusCode(201, new ResponseDto<MemberVM>(result.Value, "Member created."));
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken) =>
        ToActionResult(await mediator.Send(new GetMembersQuery(), cancellationToken));

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken) =>
        ToActionResult(await mediator.Send(new GetMemberByIdQuery(id), cancellationToken));

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateMemberRequest request, CancellationToken cancellationToken)
    {
        if (request.IsActive is null) return BadRequest(new { message = "IsActive is required." });
        return ToActionResult(await mediator.Send(new UpdateMemberCommand(id, request.FullName, request.Email,
            request.PhoneNumber, request.IsActive.Value), cancellationToken));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Deactivate(int id, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new DeactivateMemberCommand(id), cancellationToken);
        return result.IsFailed ? ToActionResult(result) : NoContent();
    }
}

public sealed record UpdateMemberRequest(string FullName, string? Email, string? PhoneNumber, bool? IsActive);
