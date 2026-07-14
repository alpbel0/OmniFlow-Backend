using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OmniFlow.Domain.Entities;

namespace OmniFlow.Infrastructure.Configurations;

public sealed class ExchangeRateSnapshotConfiguration : IEntityTypeConfiguration<ExchangeRateSnapshot>
{
    public void Configure(EntityTypeBuilder<ExchangeRateSnapshot> builder)
    {
        builder.ToTable("exchange_rate_snapshots", table =>
        {
            table.HasCheckConstraint("exchange_rate_positive", "rate > 0");
            table.HasCheckConstraint("exchange_rate_currency_codes", "length(base_currency) = 3 AND length(quote_currency) = 3");
        });
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.BaseCurrency).HasColumnName("base_currency").HasMaxLength(3).IsRequired();
        builder.Property(x => x.QuoteCurrency).HasColumnName("quote_currency").HasMaxLength(3).IsRequired();
        builder.Property(x => x.RateDate).HasColumnName("rate_date").IsRequired();
        builder.Property(x => x.Rate).HasColumnName("rate").HasColumnType("numeric(18,8)").IsRequired();
        builder.Property(x => x.Provider).HasColumnName("provider").HasMaxLength(32).IsRequired();
        builder.Property(x => x.FetchedAtUtc).HasColumnName("fetched_at_utc").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        builder.Property(x => x.DeletedAt).HasColumnName("deleted_at");
        builder.HasIndex(x => new { x.BaseCurrency, x.QuoteCurrency, x.RateDate, x.Provider })
            .IsUnique()
            .HasFilter("deleted_at IS NULL")
            .HasDatabaseName("ux_exchange_rate_pair_date_provider");
    }
}
