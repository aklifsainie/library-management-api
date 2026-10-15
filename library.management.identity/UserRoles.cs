namespace library.management.identity;

public static class UserRoles
{
    public const string Superadmin = "Superadmin";
    public const string Officer = "Officer";
    public const string Admin = "Admin";

    public static readonly string[] All = [Superadmin, Officer, Admin];
}
