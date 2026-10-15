using library.management.application.Features.LoanFeatures.Commands;
using library.management.application.Features.LoanFeatures.Queries;
using library.management.application.Shared;
using library.management.application.Shared.Mediator;
using library.management.domain.Models.ViewModels.CommonVM;
using library.management.domain.Models.ViewModels.LoanVM;
using library.management.identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.ComponentModel.DataAnnotations;

namespace library.management.api.Controllers.v1;

[ApiController]
[Route("api/v1/loans")]
[Authorize(AuthenticationSchemes = "Bearer", Roles = UserRoles.Officer)]
[EnableRateLimiting("ApiPolicy")]
public sealed class LoansController : BaseApiController
{
    private readonly IMediator _mediator;

    public LoansController(IMediator mediator) => _mediator = mediator;

    /// <summary>Approve checkout of one available book to an active membership.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ResponseDto<LoanVM>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Borrow([FromBody] BorrowBookCommand command, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        if (result.IsFailed) return ToActionResult(result);
        return StatusCode(StatusCodes.Status201Created,
            new ResponseDto<LoanVM>(result.Value, "Book borrowed successfully."));
    }

    /// <summary>List loan history for Officers.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ResponseDto<PaginatedResult<LoanVM>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default) =>
        ToActionResult(await _mediator.Send(new GetLoansQuery(pageNumber, pageSize), cancellationToken));

    /// <summary>Get one loan for an Officer.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ResponseDto<LoanVM>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById([FromRoute, Range(1, int.MaxValue)] int id,
        CancellationToken cancellationToken) =>
        ToActionResult(await _mediator.Send(new GetLoanByIdQuery(id), cancellationToken));

    /// <summary>Verify member and book at the counter, then record the return.</summary>
    [HttpPost("{id:int}/return")]
    [ProducesResponseType(typeof(ResponseDto<LoanVM>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Return([FromRoute, Range(1, int.MaxValue)] int id,
        [FromBody] ReturnBookRequest request, CancellationToken cancellationToken) =>
        ToActionResult(await _mediator.Send(new ReturnBookCommand(id, request.MemberId, request.BookId), cancellationToken));
}

public sealed record ReturnBookRequest(int MemberId, int BookId);
