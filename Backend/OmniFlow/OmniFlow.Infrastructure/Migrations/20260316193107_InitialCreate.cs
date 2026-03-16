using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace OmniFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:citext", ",,")
                .Annotation("Npgsql:PostgresExtension:postgis", ",,");

            migrationBuilder.CreateTable(
                name: "places",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    category = table.Column<string>(type: "text", nullable: false),
                    photo_url = table.Column<string>(type: "text", nullable: true),
                    phone = table.Column<string>(type: "text", nullable: true),
                    website_url = table.Column<string>(type: "text", nullable: true),
                    latitude = table.Column<double>(type: "double precision", nullable: false),
                    longitude = table.Column<double>(type: "double precision", nullable: false),
                    address = table.Column<string>(type: "text", nullable: true),
                    city = table.Column<string>(type: "text", nullable: false),
                    country = table.Column<string>(type: "text", nullable: false),
                    timezone = table.Column<string>(type: "text", nullable: true),
                    google_place_id = table.Column<string>(type: "text", nullable: true),
                    estimated_price = table.Column<decimal>(type: "numeric", nullable: false, defaultValue: 0m),
                    currency_code = table.Column<string>(type: "text", nullable: false, defaultValue: "USD"),
                    is_free = table.Column<bool>(type: "boolean", nullable: false),
                    budget_tiers = table.Column<string[]>(type: "text[]", nullable: false),
                    travel_styles = table.Column<string[]>(type: "text[]", nullable: false),
                    duration_minutes = table.Column<int>(type: "integer", nullable: true),
                    rating = table.Column<decimal>(type: "numeric", nullable: true),
                    opening_hours = table.Column<string>(type: "jsonb", nullable: true),
                    best_months = table.Column<List<int>>(type: "integer[]", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_places", x => x.id);
                    table.CheckConstraint("free_has_zero_price", "NOT is_free OR estimated_price = 0");
                    table.CheckConstraint("valid_best_months", "best_months IS NULL OR best_months <@ ARRAY[1,2,3,4,5,6,7,8,9,10,11,12]");
                    table.CheckConstraint("valid_rating", "rating IS NULL OR (rating >= 1 AND rating <= 5)");
                });

            migrationBuilder.CreateTable(
                name: "roles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_roles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    username = table.Column<string>(type: "citext", maxLength: 256, nullable: true),
                    email = table.Column<string>(type: "citext", maxLength: 256, nullable: true),
                    bio = table.Column<string>(type: "text", nullable: true),
                    profile_photo_url = table.Column<string>(type: "text", nullable: true),
                    karma_score = table.Column<int>(type: "integer", nullable: true, defaultValue: 0),
                    followers_count = table.Column<int>(type: "integer", nullable: true, defaultValue: 0),
                    following_count = table.Column<int>(type: "integer", nullable: true, defaultValue: 0),
                    role = table.Column<string>(type: "text", nullable: true),
                    is_verified = table.Column<bool>(type: "boolean", nullable: true, defaultValue: false),
                    is_suspended = table.Column<bool>(type: "boolean", nullable: true, defaultValue: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    normalized_username = table.Column<string>(type: "citext", maxLength: 256, nullable: true),
                    normalized_email = table.Column<string>(type: "citext", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    password_hash = table.Column<string>(type: "text", nullable: true),
                    security_stamp = table.Column<string>(type: "text", nullable: true),
                    concurrency_stamp = table.Column<string>(type: "text", nullable: true),
                    phone_number = table.Column<string>(type: "text", nullable: true),
                    phone_number_confirmed = table.Column<bool>(type: "boolean", nullable: false),
                    two_factor_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    lockout_end = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    lockout_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    access_failed_count = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.id);
                    table.CheckConstraint("non_negative_follow_counts", "followers_count >= 0 AND following_count >= 0");
                    table.CheckConstraint("username_format", "username ~ '^[a-zA-Z0-9_]{3,30}$'");
                });

            migrationBuilder.CreateTable(
                name: "role_claims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClaimType = table.Column<string>(type: "text", nullable: true),
                    ClaimValue = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_role_claims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_role_claims_roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "follows",
                columns: table => new
                {
                    follower_id = table.Column<Guid>(type: "uuid", nullable: false),
                    following_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_follows", x => new { x.follower_id, x.following_id });
                    table.CheckConstraint("no_self_follow", "follower_id != following_id");
                    table.ForeignKey(
                        name: "FK_follows_users_follower_id",
                        column: x => x.follower_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_follows_users_following_id",
                        column: x => x.following_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "karma_events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    actor_id = table.Column<Guid>(type: "uuid", nullable: true),
                    event_type = table.Column<string>(type: "text", nullable: false),
                    points = table.Column<int>(type: "integer", nullable: false),
                    source_id = table.Column<Guid>(type: "uuid", nullable: true),
                    source_type = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_karma_events", x => x.id);
                    table.CheckConstraint("source_consistency", "source_type IS NULL OR source_id IS NOT NULL");
                    table.CheckConstraint("valid_event_source_type", "(event_type IN ('TripPublished', 'TripForked', 'TripUpvoted') AND source_type = 'Trip') OR (event_type = 'PostUpvoted' AND source_type = 'Post') OR (event_type = 'TipUpvoted' AND source_type = 'Tip')");
                    table.CheckConstraint("valid_points", "points != 0");
                    table.ForeignKey(
                        name: "FK_karma_events_users_actor_id",
                        column: x => x.actor_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_karma_events_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "notifications",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    actor_id = table.Column<Guid>(type: "uuid", nullable: true),
                    notification_type = table.Column<string>(type: "text", nullable: false),
                    target_id = table.Column<Guid>(type: "uuid", nullable: true),
                    target_type = table.Column<string>(type: "text", nullable: true),
                    is_read = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    read_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notifications", x => x.id);
                    table.CheckConstraint("follow_has_no_target", "notification_type != 'Follow' OR (target_id IS NULL AND target_type IS NULL)");
                    table.CheckConstraint("read_consistency", "is_read = FALSE OR read_at IS NOT NULL");
                    table.CheckConstraint("valid_notification_target_type", "(notification_type = 'Follow') OR (notification_type IN ('PostUpvote', 'Comment', 'Mention') AND target_type = 'Post') OR (notification_type = 'CommentUpvote' AND target_type = 'Comment') OR (notification_type = 'TipUpvote' AND target_type = 'Tip') OR (notification_type IN ('TripUpvote', 'Fork') AND target_type = 'Trip')");
                    table.ForeignKey(
                        name: "FK_notifications_users_actor_id",
                        column: x => x.actor_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_notifications_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "refresh_tokens",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    token_hash = table.Column<string>(type: "text", nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    revoked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    device_fingerprint = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_refresh_tokens", x => x.id);
                    table.CheckConstraint("valid_expiry", "expires_at > created_at");
                    table.ForeignKey(
                        name: "FK_refresh_tokens_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "trips",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    owner_id = table.Column<Guid>(type: "uuid", nullable: false),
                    forked_from_id = table.Column<Guid>(type: "uuid", nullable: true),
                    title = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    cover_photo_url = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<string>(type: "text", nullable: false),
                    city = table.Column<string>(type: "text", nullable: false),
                    country = table.Column<string>(type: "text", nullable: false),
                    start_date = table.Column<DateOnly>(type: "date", nullable: false),
                    end_date = table.Column<DateOnly>(type: "date", nullable: false),
                    person_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    budget_tier = table.Column<string>(type: "text", nullable: false),
                    travel_style = table.Column<string>(type: "text", nullable: false),
                    user_budget = table.Column<decimal>(type: "numeric", nullable: true),
                    estimated_cost = table.Column<decimal>(type: "numeric", nullable: true),
                    fork_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    upvote_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    view_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    popularity_score = table.Column<decimal>(type: "numeric", nullable: false, defaultValue: 0m),
                    tags = table.Column<List<string>>(type: "text[]", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_trips", x => x.id);
                    table.CheckConstraint("non_negative_counts", "fork_count >= 0 AND upvote_count >= 0 AND view_count >= 0");
                    table.CheckConstraint("valid_dates", "end_date >= start_date");
                    table.CheckConstraint("valid_person_count", "person_count >= 1");
                    table.ForeignKey(
                        name: "FK_trips_trips_forked_from_id",
                        column: x => x.forked_from_id,
                        principalTable: "trips",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_trips_users_owner_id",
                        column: x => x.owner_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "user_claims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClaimType = table.Column<string>(type: "text", nullable: true),
                    ClaimValue = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_claims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_user_claims_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_logins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "text", nullable: false),
                    ProviderKey = table.Column<string>(type: "text", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "text", nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_logins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_user_logins_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_roles",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_roles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_user_roles_roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_user_roles_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_tokens",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    LoginProvider = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_tokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_user_tokens_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "community_tips",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    trip_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    place_id = table.Column<Guid>(type: "uuid", nullable: true),
                    content = table.Column<string>(type: "text", nullable: false),
                    upvote_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    is_visible = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_community_tips", x => x.id);
                    table.CheckConstraint("valid_content", "length(content) > 0");
                    table.ForeignKey(
                        name: "FK_community_tips_places_place_id",
                        column: x => x.place_id,
                        principalTable: "places",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_community_tips_trips_trip_id",
                        column: x => x.trip_id,
                        principalTable: "trips",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_community_tips_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "flights",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    trip_id = table.Column<Guid>(type: "uuid", nullable: false),
                    itinerary_group_id = table.Column<Guid>(type: "uuid", nullable: true),
                    flight_direction = table.Column<string>(type: "text", nullable: false),
                    from_city = table.Column<string>(type: "text", nullable: false),
                    from_airport = table.Column<string>(type: "text", nullable: false),
                    to_city = table.Column<string>(type: "text", nullable: false),
                    to_airport = table.Column<string>(type: "text", nullable: false),
                    departure_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    arrival_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    duration_minutes = table.Column<int>(type: "integer", nullable: false),
                    airline = table.Column<string>(type: "text", nullable: false),
                    flight_number = table.Column<string>(type: "text", nullable: false),
                    cabin_class = table.Column<string>(type: "text", nullable: false),
                    is_direct = table.Column<bool>(type: "boolean", nullable: false),
                    price_per_person = table.Column<decimal>(type: "numeric", nullable: false),
                    total_price = table.Column<decimal>(type: "numeric", nullable: false),
                    currency_code = table.Column<string>(type: "text", nullable: false),
                    is_booked = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    booked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    booking_reference = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<string>(type: "text", nullable: false),
                    data_source = table.Column<string>(type: "text", nullable: false),
                    data_fetched_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_flights", x => x.id);
                    table.CheckConstraint("booked_consistency", "is_booked = FALSE OR booked_at IS NOT NULL");
                    table.CheckConstraint("booking_ref_requires_is_booked", "booking_reference IS NULL OR is_booked = TRUE");
                    table.CheckConstraint("iata_from_airport", "from_airport ~ '^[A-Z]{3}$'");
                    table.CheckConstraint("iata_to_airport", "to_airport ~ '^[A-Z]{3}$'");
                    table.CheckConstraint("valid_duration", "duration_minutes > 0");
                    table.ForeignKey(
                        name: "FK_flights_trips_trip_id",
                        column: x => x.trip_id,
                        principalTable: "trips",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "hotels",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    trip_id = table.Column<Guid>(type: "uuid", nullable: false),
                    place_id = table.Column<Guid>(type: "uuid", nullable: true),
                    hotel_name = table.Column<string>(type: "text", nullable: true),
                    hotel_latitude = table.Column<double>(type: "double precision", nullable: true),
                    hotel_longitude = table.Column<double>(type: "double precision", nullable: true),
                    hotel_address = table.Column<string>(type: "text", nullable: true),
                    hotel_phone = table.Column<string>(type: "text", nullable: true),
                    provider_url = table.Column<string>(type: "text", nullable: true),
                    stars = table.Column<int>(type: "integer", nullable: true),
                    room_type = table.Column<string>(type: "text", nullable: false),
                    breakfast_included = table.Column<bool>(type: "boolean", nullable: false),
                    cancellation_policy = table.Column<string>(type: "text", nullable: false),
                    check_in = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    check_out = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    price_per_night = table.Column<decimal>(type: "numeric", nullable: false),
                    total_price = table.Column<decimal>(type: "numeric", nullable: false),
                    currency_code = table.Column<string>(type: "text", nullable: false),
                    is_booked = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    booked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    booking_reference = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<string>(type: "text", nullable: false),
                    data_source = table.Column<string>(type: "text", nullable: false),
                    data_fetched_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_hotels", x => x.id);
                    table.CheckConstraint("booked_consistency", "is_booked = FALSE OR booked_at IS NOT NULL");
                    table.CheckConstraint("place_or_hotel_name", "place_id IS NOT NULL OR hotel_name IS NOT NULL");
                    table.CheckConstraint("valid_dates", "check_out > check_in");
                    table.CheckConstraint("valid_stars", "stars IS NULL OR (stars >= 1 AND stars <= 5)");
                    table.ForeignKey(
                        name: "FK_hotels_places_place_id",
                        column: x => x.place_id,
                        principalTable: "places",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_hotels_trips_trip_id",
                        column: x => x.trip_id,
                        principalTable: "trips",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "posts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    trip_id = table.Column<Guid>(type: "uuid", nullable: true),
                    place_id = table.Column<Guid>(type: "uuid", nullable: true),
                    post_type = table.Column<string>(type: "text", nullable: false),
                    content = table.Column<string>(type: "text", nullable: true),
                    photos = table.Column<List<string>>(type: "text[]", nullable: false),
                    tags = table.Column<List<string>>(type: "text[]", nullable: false),
                    ai_tags = table.Column<List<string>>(type: "text[]", nullable: false),
                    location_latitude = table.Column<double>(type: "double precision", nullable: true),
                    location_longitude = table.Column<double>(type: "double precision", nullable: true),
                    city = table.Column<string>(type: "text", nullable: true),
                    country = table.Column<string>(type: "text", nullable: true),
                    upvote_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    comment_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    is_visible = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_posts", x => x.id);
                    table.CheckConstraint("content_or_photo", "content IS NOT NULL OR cardinality(photos) > 0");
                    table.CheckConstraint("non_negative_counts", "upvote_count >= 0 AND comment_count >= 0");
                    table.CheckConstraint("route_requires_trip", "post_type != 'Route' OR trip_id IS NOT NULL");
                    table.ForeignKey(
                        name: "FK_posts_places_place_id",
                        column: x => x.place_id,
                        principalTable: "places",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_posts_trips_trip_id",
                        column: x => x.trip_id,
                        principalTable: "trips",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_posts_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "saved_trips",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    trip_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_saved_trips", x => new { x.user_id, x.trip_id });
                    table.ForeignKey(
                        name: "FK_saved_trips_trips_trip_id",
                        column: x => x.trip_id,
                        principalTable: "trips",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_saved_trips_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "stops",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    trip_id = table.Column<Guid>(type: "uuid", nullable: false),
                    place_id = table.Column<Guid>(type: "uuid", nullable: true),
                    fallback_place_id = table.Column<Guid>(type: "uuid", nullable: true),
                    day_number = table.Column<int>(type: "integer", nullable: false),
                    order_index = table.Column<double>(type: "double precision", nullable: false),
                    arrival_time = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    duration_minutes = table.Column<int>(type: "integer", nullable: true),
                    is_time_locked = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    custom_name = table.Column<string>(type: "text", nullable: true),
                    custom_category = table.Column<string>(type: "text", nullable: true),
                    custom_photo_url = table.Column<string>(type: "text", nullable: true),
                    custom_latitude = table.Column<double>(type: "double precision", nullable: true),
                    custom_longitude = table.Column<double>(type: "double precision", nullable: true),
                    notes = table.Column<string>(type: "text", nullable: true),
                    booking_reference = table.Column<string>(type: "text", nullable: true),
                    reservation_note = table.Column<string>(type: "text", nullable: true),
                    activity_price = table.Column<decimal>(type: "numeric", nullable: false, defaultValue: 0m),
                    transport_price = table.Column<decimal>(type: "numeric", nullable: false, defaultValue: 0m),
                    currency_code = table.Column<string>(type: "text", nullable: false),
                    transport_from_previous = table.Column<string>(type: "text", nullable: true),
                    travel_time_from_previous = table.Column<int>(type: "integer", nullable: true),
                    is_visited = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    visited_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    added_by = table.Column<string>(type: "text", nullable: false),
                    ai_reasoning = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stops", x => x.id);
                    table.CheckConstraint("ai_reasoning_required", "added_by != 'Ai' OR ai_reasoning IS NOT NULL");
                    table.CheckConstraint("custom_place_requires_category", "custom_name IS NULL OR custom_category IS NOT NULL");
                    table.CheckConstraint("fallback_differs_from_place", "fallback_place_id IS NULL OR fallback_place_id != place_id");
                    table.CheckConstraint("place_or_custom_name", "place_id IS NOT NULL OR custom_name IS NOT NULL");
                    table.CheckConstraint("time_lock_requires_arrival", "is_time_locked = FALSE OR arrival_time IS NOT NULL");
                    table.CheckConstraint("visited_consistency", "is_visited = FALSE OR visited_at IS NOT NULL");
                    table.ForeignKey(
                        name: "FK_stops_places_fallback_place_id",
                        column: x => x.fallback_place_id,
                        principalTable: "places",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_stops_places_place_id",
                        column: x => x.place_id,
                        principalTable: "places",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_stops_trips_trip_id",
                        column: x => x.trip_id,
                        principalTable: "trips",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "trip_upvotes",
                columns: table => new
                {
                    trip_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_trip_upvotes", x => new { x.trip_id, x.user_id });
                    table.ForeignKey(
                        name: "FK_trip_upvotes_trips_trip_id",
                        column: x => x.trip_id,
                        principalTable: "trips",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_trip_upvotes_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tip_upvotes",
                columns: table => new
                {
                    tip_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tip_upvotes", x => new { x.tip_id, x.user_id });
                    table.ForeignKey(
                        name: "FK_tip_upvotes_community_tips_tip_id",
                        column: x => x.tip_id,
                        principalTable: "community_tips",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_tip_upvotes_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "comments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    post_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    parent_comment_id = table.Column<Guid>(type: "uuid", nullable: true),
                    content = table.Column<string>(type: "text", nullable: false),
                    mentions = table.Column<List<string>>(type: "text[]", nullable: false),
                    upvote_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    is_visible = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_comments", x => x.id);
                    table.UniqueConstraint("uq_comments_id_post_id", x => new { x.id, x.post_id });
                    table.CheckConstraint("valid_content", "length(content) > 0");
                    table.ForeignKey(
                        name: "FK_comments_comments_parent_comment_id_post_id",
                        columns: x => new { x.parent_comment_id, x.post_id },
                        principalTable: "comments",
                        principalColumns: new[] { "id", "post_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_comments_posts_post_id",
                        column: x => x.post_id,
                        principalTable: "posts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_comments_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "post_upvotes",
                columns: table => new
                {
                    post_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_post_upvotes", x => new { x.post_id, x.user_id });
                    table.ForeignKey(
                        name: "FK_post_upvotes_posts_post_id",
                        column: x => x.post_id,
                        principalTable: "posts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_post_upvotes_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "comment_upvotes",
                columns: table => new
                {
                    comment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_comment_upvotes", x => new { x.comment_id, x.user_id });
                    table.ForeignKey(
                        name: "FK_comment_upvotes_comments_comment_id",
                        column: x => x.comment_id,
                        principalTable: "comments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_comment_upvotes_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "idx_comment_upvotes_user_id",
                table: "comment_upvotes",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_comments_parent_comment_id_post_id",
                table: "comments",
                columns: new[] { "parent_comment_id", "post_id" });

            migrationBuilder.CreateIndex(
                name: "IX_comments_post_id",
                table: "comments",
                column: "post_id");

            migrationBuilder.CreateIndex(
                name: "IX_comments_user_id",
                table: "comments",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "idx_comments_mentions_gin",
                table: "comments",
                column: "mentions")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "IX_community_tips_place_id",
                table: "community_tips",
                column: "place_id");

            migrationBuilder.CreateIndex(
                name: "IX_community_tips_user_id",
                table: "community_tips",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "idx_community_tips_visible",
                table: "community_tips",
                columns: new[] { "trip_id", "user_id" },
                filter: "deleted_at IS NULL AND is_visible = TRUE");

            migrationBuilder.CreateIndex(
                name: "IX_flights_trip_id",
                table: "flights",
                column: "trip_id");

            migrationBuilder.CreateIndex(
                name: "idx_flights_itinerary_group",
                table: "flights",
                column: "itinerary_group_id");

            migrationBuilder.CreateIndex(
                name: "idx_follows_following_id",
                table: "follows",
                column: "following_id");

            migrationBuilder.CreateIndex(
                name: "IX_hotels_place_id",
                table: "hotels",
                column: "place_id");

            migrationBuilder.CreateIndex(
                name: "IX_hotels_trip_id",
                table: "hotels",
                column: "trip_id");

            migrationBuilder.CreateIndex(
                name: "IX_karma_events_actor_id",
                table: "karma_events",
                column: "actor_id");

            migrationBuilder.CreateIndex(
                name: "idx_karma_interaction_unique",
                table: "karma_events",
                columns: new[] { "user_id", "source_id", "event_type", "actor_id" },
                unique: true,
                filter: "event_type != 'TripPublished'");

            migrationBuilder.CreateIndex(
                name: "idx_karma_publish_unique",
                table: "karma_events",
                columns: new[] { "user_id", "source_id", "event_type" },
                unique: true,
                filter: "event_type = 'TripPublished'");

            migrationBuilder.CreateIndex(
                name: "IX_notifications_actor_id",
                table: "notifications",
                column: "actor_id");

            migrationBuilder.CreateIndex(
                name: "idx_notifications_unread",
                table: "notifications",
                column: "user_id",
                filter: "is_read = FALSE");

            migrationBuilder.CreateIndex(
                name: "idx_places_best_months_gin",
                table: "places",
                column: "best_months")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "idx_places_budget_tiers_gin",
                table: "places",
                column: "budget_tiers")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "idx_places_city",
                table: "places",
                column: "city",
                filter: "is_active = TRUE");

            migrationBuilder.CreateIndex(
                name: "idx_places_opening_hours_gin",
                table: "places",
                column: "opening_hours")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "idx_places_travel_styles_gin",
                table: "places",
                column: "travel_styles")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "idx_post_upvotes_user_id",
                table: "post_upvotes",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_posts_place_id",
                table: "posts",
                column: "place_id");

            migrationBuilder.CreateIndex(
                name: "IX_posts_trip_id",
                table: "posts",
                column: "trip_id");

            migrationBuilder.CreateIndex(
                name: "idx_posts_ai_tags_gin",
                table: "posts",
                column: "ai_tags")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "idx_posts_tags_gin",
                table: "posts",
                column: "tags")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "idx_posts_visible",
                table: "posts",
                column: "user_id",
                filter: "deleted_at IS NULL AND is_visible = TRUE");

            migrationBuilder.CreateIndex(
                name: "idx_refresh_tokens_hash_active",
                table: "refresh_tokens",
                column: "token_hash",
                unique: true,
                filter: "revoked_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "idx_refresh_tokens_user_active",
                table: "refresh_tokens",
                column: "user_id",
                filter: "revoked_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_role_claims_RoleId",
                table: "role_claims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "roles",
                column: "NormalizedName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_saved_trips_trip_id",
                table: "saved_trips",
                column: "trip_id");

            migrationBuilder.CreateIndex(
                name: "IX_stops_fallback_place_id",
                table: "stops",
                column: "fallback_place_id");

            migrationBuilder.CreateIndex(
                name: "IX_stops_place_id",
                table: "stops",
                column: "place_id");

            migrationBuilder.CreateIndex(
                name: "idx_stops_trip_day_order",
                table: "stops",
                columns: new[] { "trip_id", "day_number", "order_index" });

            migrationBuilder.CreateIndex(
                name: "idx_tip_upvotes_user_id",
                table: "tip_upvotes",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "idx_trip_upvotes_user_id",
                table: "trip_upvotes",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_trips_forked_from_id",
                table: "trips",
                column: "forked_from_id");

            migrationBuilder.CreateIndex(
                name: "IX_trips_owner_id",
                table: "trips",
                column: "owner_id");

            migrationBuilder.CreateIndex(
                name: "idx_trips_explore",
                table: "trips",
                column: "status",
                filter: "status = 'Published' AND deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "idx_trips_tags_gin",
                table: "trips",
                column: "tags")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "IX_user_claims_UserId",
                table: "user_claims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_user_logins_UserId",
                table: "user_logins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_user_roles_RoleId",
                table: "user_roles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "users",
                column: "normalized_email");

            migrationBuilder.CreateIndex(
                name: "idx_users_email_unique",
                table: "users",
                column: "email",
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "idx_users_username_unique",
                table: "users",
                column: "username",
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "users",
                column: "normalized_username",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "comment_upvotes");

            migrationBuilder.DropTable(
                name: "flights");

            migrationBuilder.DropTable(
                name: "follows");

            migrationBuilder.DropTable(
                name: "hotels");

            migrationBuilder.DropTable(
                name: "karma_events");

            migrationBuilder.DropTable(
                name: "notifications");

            migrationBuilder.DropTable(
                name: "post_upvotes");

            migrationBuilder.DropTable(
                name: "refresh_tokens");

            migrationBuilder.DropTable(
                name: "role_claims");

            migrationBuilder.DropTable(
                name: "saved_trips");

            migrationBuilder.DropTable(
                name: "stops");

            migrationBuilder.DropTable(
                name: "tip_upvotes");

            migrationBuilder.DropTable(
                name: "trip_upvotes");

            migrationBuilder.DropTable(
                name: "user_claims");

            migrationBuilder.DropTable(
                name: "user_logins");

            migrationBuilder.DropTable(
                name: "user_roles");

            migrationBuilder.DropTable(
                name: "user_tokens");

            migrationBuilder.DropTable(
                name: "comments");

            migrationBuilder.DropTable(
                name: "community_tips");

            migrationBuilder.DropTable(
                name: "roles");

            migrationBuilder.DropTable(
                name: "posts");

            migrationBuilder.DropTable(
                name: "places");

            migrationBuilder.DropTable(
                name: "trips");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
