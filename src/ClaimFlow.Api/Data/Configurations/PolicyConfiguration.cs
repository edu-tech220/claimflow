using ClaimFlow.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClaimFlow.Api.Data.Configurations;

public class PolicyConfiguration : IEntityTypeConfiguration<Policy>
{
    public void Configure(EntityTypeBuilder<Policy> builder)
    {
        builder.Property(p => p.PolicyNumber).HasMaxLength(32).IsRequired();
        builder.HasIndex(p => p.PolicyNumber).IsUnique();

        builder.Property(p => p.HolderName).HasMaxLength(128).IsRequired();
        builder.Property(p => p.HolderEmail).HasMaxLength(256).IsRequired();

        builder.Property(p => p.CoverageType).HasConversion<string>().HasMaxLength(16);
        builder.Property(p => p.Status).HasConversion<string>().HasMaxLength(16);

        builder.Property(p => p.CoverageLimit).HasPrecision(18, 2);
        builder.Property(p => p.DeductibleAmount).HasPrecision(18, 2);

        builder.Property(p => p.Version).IsConcurrencyToken();

        builder.HasMany(p => p.Claims)
            .WithOne(c => c.Policy)
            .HasForeignKey(c => c.PolicyId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
