using library.management.domain.Common;

namespace library.management.domain.Models.Entities;

public sealed class Book : BaseEntity, IAuditableEntity
{
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string? Isbn { get; set; }
}
