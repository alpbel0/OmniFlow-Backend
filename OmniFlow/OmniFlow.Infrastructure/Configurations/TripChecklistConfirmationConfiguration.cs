using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OmniFlow.Domain.Entities;

namespace OmniFlow.Infrastructure.Configurations;

public class TripChecklistConfirmationConfiguration : IEntityTypeConfiguration<TripChecklistConfirmation>
{
	public void Configure(EntityTypeBuilder<TripChecklistConfirmation> builder)
	{
		builder.ToTable("trip_checklist_confirmations");

		builder.Property(c => c.Id).HasColumnName("id");
		builder.Property(c => c.TripId).HasColumnName("trip_id").IsRequired();
		builder.Property(c => c.ItemKey).HasColumnName("item_key").HasMaxLength(160).IsRequired();
		builder.Property(c => c.IsConfirmed).HasColumnName("is_confirmed").IsRequired();
		builder.Property(c => c.ConfirmedAt).HasColumnName("confirmed_at");

		builder.HasOne(c => c.Trip)
			.WithMany()
			.HasForeignKey(c => c.TripId)
			.OnDelete(DeleteBehavior.Cascade);

		builder.HasIndex(c => new { c.TripId, c.ItemKey })
			.IsUnique()
			.HasDatabaseName("idx_trip_checklist_confirmations_trip_item_key");
	}
}
