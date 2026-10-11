namespace library.management.domain.Models.ViewModels.BookVM;

public sealed class BookVM
{
    public int BookId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Author { get; init; } = string.Empty;
    public string? Isbn { get; init; }
}
