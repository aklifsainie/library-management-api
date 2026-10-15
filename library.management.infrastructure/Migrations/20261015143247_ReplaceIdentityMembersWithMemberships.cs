using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace library.management.infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceIdentityMembersWithMemberships : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // The old loan FK points into IdentityContext and is absent from the EF snapshot.
            migrationBuilder.DropForeignKey(
                name: "FK_BookLoan_IdentityUsers_UserId",
                table: "BookLoan");

            migrationBuilder.DropIndex(
                name: "IX_BookLoan_UserId_BorrowedAtUtc",
                table: "BookLoan");

            migrationBuilder.AddColumn<int>(
                name: "MemberId",
                table: "BookLoan",
                type: "int",
                nullable: true);

            // Historical checkouts did not capture an Officer identity.
            migrationBuilder.AddColumn<string>(
                name: "CheckoutOfficerUserId",
                table: "BookLoan",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReturnOfficerUserId",
                table: "BookLoan",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Member",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MemberNumber = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    LegacyIdentityUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDelete = table.Column<bool>(type: "bit", nullable: false),
                    RegisterBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RegisterDate = table.Column<DateTime>(type: "datetime", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    Remarks = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Member", x => x.Id);
                });

            migrationBuilder.Sql("""
                IF OBJECT_ID(N'dbo.IdentityUsers', N'U') IS NULL OR
                   OBJECT_ID(N'dbo.AspNetRoles', N'U') IS NULL OR
                   OBJECT_ID(N'dbo.IdentityUserRoles', N'U') IS NULL OR
                   OBJECT_ID(N'dbo.RefreshTokens', N'U') IS NULL
                    THROW 51000, 'Identity tables must exist before migrating memberships.', 1;

                IF EXISTS (SELECT 1 FROM dbo.BookLoan loan
                           LEFT JOIN dbo.IdentityUsers identityUser ON identityUser.Id = loan.UserId
                           WHERE identityUser.Id IS NULL)
                    THROW 51001, 'A historical loan references a missing Identity user.', 1;

                IF EXISTS (SELECT 1 FROM dbo.IdentityUsers identityUser
                           WHERE LEN(identityUser.PhoneNumber) > 32 AND
                           (EXISTS (SELECT 1 FROM dbo.BookLoan loan WHERE loan.UserId = identityUser.Id) OR
                            EXISTS (SELECT 1 FROM dbo.IdentityUserRoles userRole
                                    JOIN dbo.AspNetRoles role ON role.Id = userRole.RoleId
                                    WHERE userRole.UserId = identityUser.Id AND role.NormalizedName = 'MEMBER')))
                    THROW 51002, 'A legacy member phone number exceeds the membership limit.', 1;

                ;WITH Legacy AS (
                    SELECT identityUser.Id, identityUser.FirstName, identityUser.LastName,
                           identityUser.UserName, identityUser.Email, identityUser.PhoneNumber,
                           identityUser.IsActive, identityUser.IsDeleted, identityUser.CreatedAt,
                           ROW_NUMBER() OVER (ORDER BY identityUser.Id) AS SequenceNumber
                    FROM dbo.IdentityUsers identityUser
                    WHERE EXISTS (SELECT 1 FROM dbo.BookLoan loan WHERE loan.UserId = identityUser.Id)
                       OR EXISTS (SELECT 1 FROM dbo.IdentityUserRoles userRole
                                  JOIN dbo.AspNetRoles role ON role.Id = userRole.RoleId
                                  WHERE userRole.UserId = identityUser.Id AND role.NormalizedName = 'MEMBER')
                )
                INSERT INTO dbo.Member
                    (MemberNumber, FullName, Email, PhoneNumber, LegacyIdentityUserId,
                     IsActive, IsDelete, RegisterDate)
                SELECT CONCAT('LEGACY-', CONVERT(varchar(20), SequenceNumber)),
                       LEFT(COALESCE(NULLIF(LTRIM(RTRIM(CONCAT(FirstName, ' ', LastName))), ''),
                                     NULLIF(UserName, ''), NULLIF(Email, ''), 'Legacy member'), 200),
                       Email, PhoneNumber, Id, IsActive, IsDeleted, CreatedAt
                FROM Legacy;

                UPDATE loan SET MemberId = member.Id
                FROM dbo.BookLoan loan
                JOIN dbo.Member member ON member.LegacyIdentityUserId = loan.UserId;

                IF EXISTS (SELECT 1 FROM dbo.BookLoan WHERE MemberId IS NULL)
                    THROW 51003, 'A historical loan could not be mapped to a membership.', 1;

                UPDATE token SET IsRevoked = 1, RevokedAt = COALESCE(token.RevokedAt, SYSUTCDATETIME())
                FROM dbo.RefreshTokens token
                WHERE token.IsRevoked = 0 AND EXISTS (
                    SELECT 1 FROM dbo.IdentityUserRoles userRole
                    JOIN dbo.AspNetRoles role ON role.Id = userRole.RoleId
                    WHERE userRole.UserId = token.UserId AND role.NormalizedName = 'MEMBER');

                UPDATE identityUser SET IsActive = 0, IsDeleted = 1,
                       DeletedAt = COALESCE(identityUser.DeletedAt, SYSUTCDATETIME())
                FROM dbo.IdentityUsers identityUser
                WHERE EXISTS (SELECT 1 FROM dbo.IdentityUserRoles userRole
                              JOIN dbo.AspNetRoles role ON role.Id = userRole.RoleId
                              WHERE userRole.UserId = identityUser.Id AND role.NormalizedName = 'MEMBER')
                  AND NOT EXISTS (SELECT 1 FROM dbo.IdentityUserRoles userRole
                                  JOIN dbo.AspNetRoles role ON role.Id = userRole.RoleId
                                  WHERE userRole.UserId = identityUser.Id AND
                                        role.NormalizedName IN ('OFFICER', 'ADMIN', 'SUPERADMIN'));

                DELETE userRole FROM dbo.IdentityUserRoles userRole
                JOIN dbo.AspNetRoles role ON role.Id = userRole.RoleId
                WHERE role.NormalizedName = 'MEMBER';
                DELETE roleClaim FROM dbo.IdentityRoleClaims roleClaim
                JOIN dbo.AspNetRoles role ON role.Id = roleClaim.RoleId
                WHERE role.NormalizedName = 'MEMBER';
                DELETE FROM dbo.AspNetRoles WHERE NormalizedName = 'MEMBER';
                DELETE FROM dbo.IdentityRoles WHERE NormalizedName = 'MEMBER';
                """);

            migrationBuilder.AlterColumn<int>(
                name: "MemberId", table: "BookLoan", type: "int", nullable: false,
                oldClrType: typeof(int), oldType: "int", oldNullable: true);

            migrationBuilder.DropColumn(name: "UserId", table: "BookLoan");

            migrationBuilder.CreateIndex(
                name: "IX_BookLoan_MemberId_BorrowedAtUtc",
                table: "BookLoan",
                columns: new[] { "MemberId", "BorrowedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Member_LegacyIdentityUserId",
                table: "Member",
                column: "LegacyIdentityUserId",
                unique: true,
                filter: "[LegacyIdentityUserId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Member_MemberNumber",
                table: "Member",
                column: "MemberNumber",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_BookLoan_Member_MemberId",
                table: "BookLoan",
                column: "MemberId",
                principalTable: "Member",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            throw new NotSupportedException("This data migration cannot safely restore Member sign-in accounts or map new loans back to Identity users.");
        }
    }
}
