using ClaimFlow.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClaimFlow.Api.Data.Configurations;

public class ClaimConfiguration : IEntityTypeConfiguration<Claim>
{
    public void Configure(EntityTypeBuilder<Claim> builder)
    {
        builder.Property(c => c.ClaimNumber).HasMaxLength(32).IsRequired();
        builder.HasIndex(c => c.ClaimNumber).IsUnique();

        builder.Property(c => c.Description).HasMaxLength(2000).IsRequired();
        builder.Property(c => c.DecisionNotes).HasMaxLength(2000);

        builder.Property(c => c.ClaimedAmount).HasPrecision(18, 2);
        builder.Property(c => c.PayableAmount).HasPrecision(18, 2);

        builder.Property(c => c.Status).HasConversion<string>().HasMaxLength(32);

        builder.Property(c => c.Version).IsConcurrencyToken();

        builder.HasOne(c => c.AssignedAdjuster)
            .WithMany()
            .HasForeignKey(c => c.AssignedAdjusterUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(c => c.Status);
        builder.HasIndex(c => c.PolicyId);
    }
}
