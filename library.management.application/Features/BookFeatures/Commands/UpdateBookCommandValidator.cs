using FluentValidation;

namespace library.management.application.Features.BookFeatures.Commands;

public sealed class UpdateBookCommandValidator : AbstractValidator<UpdateBookCommand>
{
    public UpdateBookCommandValidator()
    {
        RuleFor(book => book.Id).GreaterThan(0);
        RuleFor(book => book.Title)
            .Must(value => !string.IsNullOrWhiteSpace(value) && value.Trim().Length <= 200)
            .WithMessage("Title must be between 1 and 200 characters.");
        RuleFor(book => book.Author)
            .Must(value => !string.IsNullOrWhiteSpace(value) && value.Trim().Length <= 200)
            .WithMessage("Author must be between 1 and 200 characters.");
        RuleFor(book => book.Isbn)
            .Must(value => string.IsNullOrWhiteSpace(value) || value.Trim().Length <= 17)
            .WithMessage("Isbn cannot exceed 17 characters.");
    }
}
