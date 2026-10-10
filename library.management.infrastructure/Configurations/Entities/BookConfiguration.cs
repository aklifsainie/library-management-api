using library.management.domain.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace library.management.infrastructure.Configurations.Entities;

public sealed class BookConfiguration : IEntityTypeConfiguration<Book>
{
    public void Configure(EntityTypeBuilder<Book> builder)
    {
        builder.ToTable("Book");
        builder.HasKey(book => book.Id);
        builder.Property(book => book.Title).HasMaxLength(200).IsRequired();
        builder.Property(book => book.Author).HasMaxLength(200).IsRequired();
        builder.Property(book => book.Isbn).HasMaxLength(17);
    }
}
