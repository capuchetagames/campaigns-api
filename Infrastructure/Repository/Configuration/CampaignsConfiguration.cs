using Core.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.ValueGeneration;

namespace Infrastructure.Repository.Configuration;

public class CampaignsConfiguration : IEntityTypeConfiguration<Campaign>
{
    public void Configure(EntityTypeBuilder<Campaign> builder)
    {
        builder.ToTable("Campaigns");
        builder.HasKey(x => x.Id);
        builder.Property(x=>x.Id).HasColumnType("uuid").ValueGeneratedOnAdd().HasValueGenerator<GuidValueGenerator>();
        builder.Property(x => x.Title).HasColumnType("VARCHAR(100)").IsRequired();
        builder.Property(x => x.Description).HasColumnType("VARCHAR(100)").IsRequired();
        builder.Property(x => x.Status).HasColumnType("VARCHAR(10)").IsRequired();
        builder.Property(x=> x.StartDate).HasColumnType("TIMESTAMP").IsRequired();
        builder.Property(x=> x.EndDate).HasColumnType("TIMESTAMP").IsRequired();
        builder.Property(x => x.FinancialGoal).HasColumnType("DECIMAL(18,2)").IsRequired();
        builder.Property(x=> x.CreatedAt).HasColumnType("TIMESTAMP").IsRequired();
    }
}