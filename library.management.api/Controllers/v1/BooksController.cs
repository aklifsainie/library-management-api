using library.management.application.Features.BookFeatures.Commands;
using library.management.application.Shared;
using library.management.application.Shared.Mediator;
using library.management.domain.Models.ViewModels.BookVM;
using library.management.identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace library.management.api.Controllers.v1;

[ApiController]
[Route("api/v1/books")]
[Authorize(AuthenticationSchemes = "Bearer", Roles = UserRoles.Admin)]
[EnableRateLimiting("ApiPolicy")]
public sealed class BooksController : BaseApiController
{
    private readonly IMediator _mediator;

    public BooksController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>Add a book to the library.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ResponseDto<BookVM>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Add([FromBody] AddBookCommand command, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        if (result.IsFailed)
            return ToActionResult(result);

        var message = result.Successes.FirstOrDefault()?.Message;
        return StatusCode(StatusCodes.Status201Created,
            new ResponseDto<BookVM>(result.Value, message ?? "Book has been added successfully."));
    }
}
