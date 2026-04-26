using Microsoft.EntityFrameworkCore;
using OmniFlow.Domain.Entities;

namespace OmniFlow.Application.Interfaces;

public interface IApplicationDbContext
{
	DbSet<User> Users { get; }
	DbSet<Trip> Trips { get; }
	DbSet<TripDestination> TripDestinations { get; }
	DbSet<TimelineEntry> TimelineEntries { get; }
	DbSet<Place> Places { get; }
	DbSet<Flight> Flights { get; }
	DbSet<Hotel> Hotels { get; }
	DbSet<ProviderFlight> ProviderFlights { get; }
	DbSet<ProviderHotel> ProviderHotels { get; }
	DbSet<Post> Posts { get; }
	DbSet<Comment> Comments { get; }
	DbSet<CommunityTip> CommunityTips { get; }
	DbSet<Follow> Follows { get; }
	DbSet<Block> Blocks { get; }
	DbSet<PostUpvote> PostUpvotes { get; }
	DbSet<CommentUpvote> CommentUpvotes { get; }
	DbSet<TipUpvote> TipUpvotes { get; }
	DbSet<TripUpvote> TripUpvotes { get; }
	DbSet<SavedTrip> SavedTrips { get; }
	DbSet<Notification> Notifications { get; }
	DbSet<KarmaEvent> KarmaEvents { get; }
	DbSet<RefreshToken> RefreshTokens { get; }
	DbSet<EmailVerificationDispatch> EmailVerificationDispatches { get; }
	DbSet<PasswordResetToken> PasswordResetTokens { get; }

	DbSet<T> Set<T>() where T : class;

	Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
