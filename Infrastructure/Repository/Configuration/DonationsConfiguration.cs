using Core.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.ValueGeneration;

namespace Infrastructure.Repository.Configuration;

public class DonationsConfiguration : IEntityTypeConfiguration<Donation>
{
    public void Configure(EntityTypeBuilder<Donation> builder)
    {
        builder.ToTable("Donations");
        builder.HasKey(x => x.Id);
        builder.Property(x=>x.Id).HasColumnType("uuid").ValueGeneratedOnAdd().HasValueGenerator<GuidValueGenerator>();
        builder.Property(x=>x.UserId).HasColumnType("uuid").IsRequired();
        builder.Property(x=>x.CampaignId).HasColumnType("uuid").IsRequired();
        builder.Property(x => x.Amount).HasColumnType("DECIMAL(18,2)").IsRequired();
        builder.Property(x=> x.CreatedAt).HasColumnType("TIMESTAMP").IsRequired();
    }
}