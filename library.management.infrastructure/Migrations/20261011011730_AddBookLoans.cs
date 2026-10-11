using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace library.management.infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBookLoans : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BookLoan",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BookId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    BorrowedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReturnedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
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
                    table.PrimaryKey("PK_BookLoan", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BookLoan_Book_BookId",
                        column: x => x.BookId,
                        principalTable: "Book",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BookLoan_UserId_BorrowedAtUtc",
                table: "BookLoan",
                columns: new[] { "UserId", "BorrowedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "UX_BookLoan_OpenBook",
                table: "BookLoan",
                column: "BookId",
                unique: true,
                filter: "[ReturnedAtUtc] IS NULL");

            // IdentityUsers belongs to IdentityContext, so this cross-context FK is migration-only.
            migrationBuilder.AddForeignKey(
                name: "FK_BookLoan_IdentityUsers_UserId",
                table: "BookLoan",
                column: "UserId",
                principalTable: "IdentityUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BookLoan");
        }
    }
}
