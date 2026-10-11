using library.management.application.Features.BookFeatures.Commands;
using library.management.application.Features.BookFeatures.Queries;
using library.management.application.Shared;
using library.management.application.Shared.Mediator;
using library.management.domain.Models.ViewModels.BookVM;
using library.management.identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.ComponentModel.DataAnnotations;

namespace library.management.api.Controllers.v1;

[ApiController]
[Route("api/v1/books")]
[Authorize(AuthenticationSchemes = "Bearer")]
[EnableRateLimiting("ApiPolicy")]
public sealed class BooksController : BaseApiController
{
    private readonly IMediator _mediator;

    public BooksController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>List active books.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ResponseDto<List<BookVM>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        return ToActionResult(await _mediator.Send(new GetAllBooksQuery(), cancellationToken));
    }

    /// <summary>Get an active book by ID.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ResponseDto<BookVM>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> GetById([FromRoute, Range(1, int.MaxValue)] int id, CancellationToken cancellationToken)
    {
        return ToActionResult(await _mediator.Send(new GetBookByIdQuery(id), cancellationToken));
    }

    /// <summary>Add a book to the library.</summary>
    [HttpPost]
    [Authorize(Roles = UserRoles.Admin)]
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

    /// <summary>Update a book's title, author, and ISBN.</summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = UserRoles.Admin)]
    [ProducesResponseType(typeof(ResponseDto<BookVM>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Update([FromRoute, Range(1, int.MaxValue)] int id,
        [FromBody] UpdateBookCommand command, CancellationToken cancellationToken)
    {
        command.Id = id;
        return ToActionResult(await _mediator.Send(command, cancellationToken));
    }

    /// <summary>Soft-delete a book.</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = UserRoles.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Delete([FromRoute, Range(1, int.MaxValue)] int id,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new DeleteBookCommand(id), cancellationToken);
        return result.IsFailed ? ToActionResult(result) : NoContent();
    }
}
