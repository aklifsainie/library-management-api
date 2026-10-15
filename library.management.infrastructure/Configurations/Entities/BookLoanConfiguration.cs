using library.management.domain.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace library.management.infrastructure.Configurations.Entities;

public sealed class BookLoanConfiguration : IEntityTypeConfiguration<BookLoan>
{
    public void Configure(EntityTypeBuilder<BookLoan> builder)
    {
        builder.ToTable("BookLoan");
        builder.HasKey(loan => loan.Id);
        builder.Property(loan => loan.CheckoutOfficerUserId).HasMaxLength(450);
        builder.Property(loan => loan.ReturnOfficerUserId).HasMaxLength(450);
        builder.Property(loan => loan.BorrowedAtUtc).HasColumnType("datetime2").IsRequired();
        builder.Property(loan => loan.ReturnedAtUtc).HasColumnType("datetime2");
        builder.HasOne(loan => loan.Book).WithMany().HasForeignKey(loan => loan.BookId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(loan => loan.Member).WithMany().HasForeignKey(loan => loan.MemberId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(loan => loan.BookId).IsUnique().HasFilter("[ReturnedAtUtc] IS NULL")
            .HasDatabaseName("UX_BookLoan_OpenBook");
        builder.HasIndex(loan => new { loan.MemberId, loan.BorrowedAtUtc });
    }
}
