using library.management.domain.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace library.management.infrastructure.Configurations.Entities;

public sealed class MemberConfiguration : IEntityTypeConfiguration<Member>
{
    public void Configure(EntityTypeBuilder<Member> builder)
    {
        builder.ToTable("Member");
        builder.HasKey(member => member.Id);
        builder.Property(member => member.MemberNumber).HasMaxLength(64).IsRequired();
        builder.Property(member => member.FullName).HasMaxLength(200).IsRequired();
        builder.Property(member => member.Email).HasMaxLength(256);
        builder.Property(member => member.PhoneNumber).HasMaxLength(32);
        builder.Property(member => member.LegacyIdentityUserId).HasMaxLength(450);
        builder.HasIndex(member => member.MemberNumber).IsUnique();
        builder.HasIndex(member => member.LegacyIdentityUserId).IsUnique()
            .HasFilter("[LegacyIdentityUserId] IS NOT NULL");
    }
}
