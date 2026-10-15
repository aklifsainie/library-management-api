using library.management.domain.Common;

namespace library.management.domain.Models.Entities;

public sealed class BookLoan : BaseEntity, IAuditableEntity
{
    public int BookId { get; set; }
    public Book Book { get; set; } = null!;
    public int MemberId { get; set; }
    public Member Member { get; set; } = null!;
    public string? CheckoutOfficerUserId { get; set; }
    public string? ReturnOfficerUserId { get; set; }
    public DateTime BorrowedAtUtc { get; set; }
    public DateTime? ReturnedAtUtc { get; set; }
}
