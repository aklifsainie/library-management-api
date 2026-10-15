using library.management.domain.Common;

namespace library.management.domain.Models.Entities;

public sealed class Member : BaseEntity, IAuditableEntity
{
    public string MemberNumber { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    [SensitiveData] public string? Email { get; set; }
    [SensitiveData] public string? PhoneNumber { get; set; }
    public string? LegacyIdentityUserId { get; set; }
}
