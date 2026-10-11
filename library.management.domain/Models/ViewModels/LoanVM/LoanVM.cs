namespace library.management.domain.Models.ViewModels.LoanVM;

public sealed class LoanVM
{
    public int Id { get; set; }
    public int BookId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public DateTime BorrowedAtUtc { get; set; }
    public DateTime? ReturnedAtUtc { get; set; }
}
