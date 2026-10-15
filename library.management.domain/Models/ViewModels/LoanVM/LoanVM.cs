namespace library.management.domain.Models.ViewModels.LoanVM;

public sealed class LoanVM
{
    public int Id { get; set; }
    public int BookId { get; set; }
    public int MemberId { get; set; }
    public string? CheckoutOfficerUserId { get; set; }
    public string? ReturnOfficerUserId { get; set; }
    public DateTime BorrowedAtUtc { get; set; }
    public DateTime? ReturnedAtUtc { get; set; }
}
