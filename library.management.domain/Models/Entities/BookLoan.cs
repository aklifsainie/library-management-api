using library.management.domain.Common;

namespace library.management.domain.Models.Entities;

public sealed class BookLoan : BaseEntity, IAuditableEntity
{
    public int BookId { get; set; }
    public Book Book { get; set; } = null!;
    public string UserId { get; set; } = string.Empty;
    public DateTime BorrowedAtUtc { get; set; }
    public DateTime? ReturnedAtUtc { get; set; }
}
