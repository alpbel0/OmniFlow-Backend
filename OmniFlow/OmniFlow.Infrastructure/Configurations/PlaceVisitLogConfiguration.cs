using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OmniFlow.Domain.Entities;

namespace OmniFlow.Infrastructure.Configurations;

public sealed class PlaceVisitLogConfiguration : IEntityTypeConfiguration<PlaceVisitLog>
{
    public void Configure(EntityTypeBuilder<PlaceVisitLog> builder)
    {
        builder.ToTable("place_visit_logs", table =>
        {
            table.HasCheckConstraint("visit_log_target_xor", "(timeline_entry_id IS NOT NULL) <> (place_id IS NOT NULL)");
            table.HasCheckConstraint("visit_log_rating_range", "rating IS NULL OR rating BETWEEN 1 AND 5");
            table.HasCheckConstraint("visit_log_cost_non_negative", "actual_cost IS NULL OR actual_cost >= 0");
            table.HasCheckConstraint("visit_log_currency_codes", "length(currency_code) = 3 AND length(base_currency_code) = 3");
        });
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.TripId).HasColumnName("trip_id").IsRequired();
        builder.Property(x => x.TripDestinationId).HasColumnName("trip_destination_id").IsRequired();
        builder.Property(x => x.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(x => x.TimelineEntryId).HasColumnName("timeline_entry_id");
        builder.Property(x => x.PlaceId).HasColumnName("place_id");
        builder.Property(x => x.ActualCost).HasColumnName("actual_cost").HasColumnType("numeric(18,2)");
        builder.Property(x => x.CurrencyCode).HasColumnName("currency_code").HasMaxLength(3).IsRequired();
        builder.Property(x => x.Rating).HasColumnName("rating");
        builder.Property(x => x.Note).HasColumnName("note").HasMaxLength(1000);
        builder.Property(x => x.VisitedAt).HasColumnName("visited_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.ConvertedActualCost).HasColumnName("converted_actual_cost").HasColumnType("numeric(18,2)");
        builder.Property(x => x.ExchangeRate).HasColumnName("exchange_rate").HasColumnType("numeric(18,8)");
        builder.Property(x => x.RateRequestedDate).HasColumnName("rate_requested_date");
        builder.Property(x => x.ExchangeRateDate).HasColumnName("exchange_rate_date");
        builder.Property(x => x.BaseCurrencyCode).HasColumnName("base_currency_code").HasMaxLength(3).IsRequired();
        builder.Property(x => x.ConversionStatus).HasColumnName("conversion_status").HasConversion<string>().IsRequired();
        builder.Property(x => x.ConversionAttemptCount).HasColumnName("conversion_attempt_count").IsRequired();
        builder.Property(x => x.LastConversionAttemptAtUtc).HasColumnName("last_conversion_attempt_at_utc").HasColumnType("timestamp with time zone");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        builder.Property(x => x.DeletedAt).HasColumnName("deleted_at");

        builder.HasOne(x => x.Trip).WithMany(x => x.VisitLogs).HasForeignKey(x => x.TripId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.TripDestination).WithMany(x => x.VisitLogs).HasForeignKey(x => x.TripDestinationId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.User).WithMany(x => x.VisitLogs).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.TimelineEntry).WithMany().HasForeignKey(x => x.TimelineEntryId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Place).WithMany().HasForeignKey(x => x.PlaceId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => x.TimelineEntryId).IsUnique().HasFilter("timeline_entry_id IS NOT NULL AND deleted_at IS NULL").HasDatabaseName("ux_visit_log_timeline_active");
        builder.HasIndex(x => new { x.TripId, x.UserId }).HasDatabaseName("ix_visit_logs_trip_user");
        builder.HasIndex(x => new { x.TripDestinationId, x.VisitedAt }).HasDatabaseName("ix_visit_logs_destination_visited_at");
        builder.HasIndex(x => new { x.ConversionStatus, x.LastConversionAttemptAtUtc }).HasDatabaseName("ix_visit_logs_pending_conversion");
    }
}
