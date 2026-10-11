namespace library.management.identity;

public static class UserRoles
{
    public const string Superadmin = "Superadmin";
    public const string Member = "Member";
    public const string Admin = "Admin";

    public static readonly string[] All = [Superadmin, Member, Admin];
}
