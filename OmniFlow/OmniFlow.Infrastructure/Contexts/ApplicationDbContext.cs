using System.Linq.Expressions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using OmniFlow.Application.Interfaces;
using OmniFlow.Domain.Common;
using OmniFlow.Domain.Entities;
using OmniFlow.Infrastructure.Models;

namespace OmniFlow.Infrastructure.Contexts;

public class ApplicationDbContext
	: IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>, IApplicationDbContext
{
	public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
		: base(options)
	{
	}

	// IdentityDbContext already has Users (ApplicationUser), so domain User DbSet is exposed explicitly.
	DbSet<User> IApplicationDbContext.Users => Set<User>();

	public DbSet<Trip> Trips => Set<Trip>();
	public DbSet<TripDestination> TripDestinations => Set<TripDestination>();
	public DbSet<GeocodingCacheEntry> GeocodingCacheEntries => Set<GeocodingCacheEntry>();
	public DbSet<TripRouteCache> TripRouteCaches => Set<TripRouteCache>();
	public DbSet<TripChecklistConfirmation> TripChecklistConfirmations => Set<TripChecklistConfirmation>();
	public DbSet<TimelineEntry> TimelineEntries => Set<TimelineEntry>();
	public DbSet<PlaceVisitLog> PlaceVisitLogs => Set<PlaceVisitLog>();
	public DbSet<ExchangeRateSnapshot> ExchangeRateSnapshots => Set<ExchangeRateSnapshot>();
	public DbSet<Place> Places => Set<Place>();
	public DbSet<Flight> Flights => Set<Flight>();
	public DbSet<Hotel> Hotels => Set<Hotel>();
	public DbSet<ProviderFlight> ProviderFlights => Set<ProviderFlight>();
	public DbSet<ProviderHotel> ProviderHotels => Set<ProviderHotel>();
	public DbSet<Post> Posts => Set<Post>();
	public DbSet<Comment> Comments => Set<Comment>();
	public DbSet<CommunityTip> CommunityTips => Set<CommunityTip>();
	public DbSet<Follow> Follows => Set<Follow>();
	public DbSet<Block> Blocks => Set<Block>();
	public DbSet<PostUpvote> PostUpvotes => Set<PostUpvote>();
	public DbSet<CommentUpvote> CommentUpvotes => Set<CommentUpvote>();
	public DbSet<TipUpvote> TipUpvotes => Set<TipUpvote>();
	public DbSet<TripUpvote> TripUpvotes => Set<TripUpvote>();
	public DbSet<SavedTrip> SavedTrips => Set<SavedTrip>();
	public DbSet<Notification> Notifications => Set<Notification>();
	public DbSet<KarmaEvent> KarmaEvents => Set<KarmaEvent>();
	public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
	public DbSet<EmailVerificationDispatch> EmailVerificationDispatches => Set<EmailVerificationDispatch>();
	public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();

	public new DbSet<T> Set<T>() where T : class => base.Set<T>();

	public Task<int> IncrementTripViewCountAsync(Guid tripId, CancellationToken cancellationToken = default)
	{
		return Trips
			.Where(t => t.Id == tripId && t.DeletedAt == null)
			.ExecuteUpdateAsync(
				setters => setters.SetProperty(t => t.ViewCount, t => t.ViewCount + 1),
				cancellationToken);
	}

	protected override void OnModelCreating(ModelBuilder builder)
	{
		base.OnModelCreating(builder);

		builder.HasPostgresExtension("citext");
		builder.HasPostgresExtension("postgis");

		builder.Entity<ApplicationUser>(entity =>
		{
			entity.ToTable("users");
			entity.Property(u => u.Id).HasColumnName("id");
			entity.Property(u => u.UserName).HasColumnName("username").HasColumnType("citext");
			entity.Property(u => u.Email).HasColumnName("email").HasColumnType("citext");
			entity.Property(u => u.NormalizedUserName).HasColumnName("normalized_username").HasColumnType("citext");
			entity.Property(u => u.NormalizedEmail).HasColumnName("normalized_email").HasColumnType("citext");
			entity.Property(u => u.PasswordHash).HasColumnName("password_hash");
			entity.Property(u => u.SecurityStamp).HasColumnName("security_stamp");
			entity.Property(u => u.ConcurrencyStamp).HasColumnName("concurrency_stamp");
			entity.Property(u => u.PhoneNumber).HasColumnName("phone_number");
			entity.Property(u => u.PhoneNumberConfirmed).HasColumnName("phone_number_confirmed");
			entity.Property(u => u.TwoFactorEnabled).HasColumnName("two_factor_enabled");
			entity.Property(u => u.LockoutEnd).HasColumnName("lockout_end");
			entity.Property(u => u.LockoutEnabled).HasColumnName("lockout_enabled");
			entity.Property(u => u.AccessFailedCount).HasColumnName("access_failed_count");
			entity.HasOne<User>()
				.WithOne()
				.HasForeignKey<User>(u => u.Id)
				.OnDelete(DeleteBehavior.Cascade);
		});

		builder.Entity<IdentityRole<Guid>>().ToTable("roles");
		builder.Entity<IdentityUserRole<Guid>>().ToTable("user_roles");
		builder.Entity<IdentityUserClaim<Guid>>().ToTable("user_claims");
		builder.Entity<IdentityUserLogin<Guid>>().ToTable("user_logins");
		builder.Entity<IdentityRoleClaim<Guid>>().ToTable("role_claims");
		builder.Entity<IdentityUserToken<Guid>>().ToTable("user_tokens");
		builder.Entity<EmailVerificationDispatch>().ToTable("email_verification_dispatches");
		builder.Entity<PasswordResetToken>().ToTable("password_reset_tokens");

		builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

		// Global query filter for soft-delete using Expression Tree
		foreach (var entityType in builder.Model.GetEntityTypes())
		{
			if (typeof(AuditableBaseEntity).IsAssignableFrom(entityType.ClrType))
			{
				// Create parameter expression: e => e.DeletedAt == null
				var parameter = Expression.Parameter(entityType.ClrType, "e");
				var deletedAtProperty = Expression.Property(parameter, "DeletedAt");
				var nullConstant = Expression.Constant(null, typeof(DateTime?));
				var filter = Expression.Equal(deletedAtProperty, nullConstant);
				var lambda = Expression.Lambda(filter, parameter);

				// Apply filter
				builder.Entity(entityType.ClrType).HasQueryFilter(lambda);
			}
		}
	}

	public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
	{
		var utcNow = DateTime.UtcNow;

		foreach (var entry in ChangeTracker.Entries<AuditableBaseEntity>())
		{
			if (entry.State == EntityState.Added)
			{
				entry.Entity.CreatedAt = utcNow;
				entry.Entity.UpdatedAt = utcNow;
			}
			else if (entry.State == EntityState.Modified)
			{
				entry.Entity.UpdatedAt = utcNow;
			}
		}

		return base.SaveChangesAsync(cancellationToken);
	}
}
