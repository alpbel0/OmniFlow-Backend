# OmniFlow Backend - As-Built Schema Snapshot

Last updated for `BACKEND_ROADMAP_V2` B0.5.

This document reflects the current backend codebase after the trip-planning cleanup. The legacy
`Stop` model, `StopsController`, `IStopRepositoryAsync`, and flight/hotel `select` endpoints are no
longer part of the runtime design.

## Current Trip Planning Model

The backend now uses a two-level trip planning model:

- `TripDestination` represents ordered destination legs for a trip.
- `TimelineEntry` represents itinerary items inside a trip/destination/day. Places, custom flights,
  custom transport, accommodation, and custom events all live here.
- Flight and hotel provider data remains available through provider tables and provider query
  endpoints, but selecting a flight or hotel no longer creates booking records through
  `/flights/select` or `/hotels/select`. The chosen details are modeled as `TimelineEntry` records.

Legacy mapping:

| Removed concept | Current concept |
|-----------------|-----------------|
| `Stop` entity/table | `TimelineEntry` |
| `StopsController` | `TimelineController` |
| `IStopRepositoryAsync` | `ITimelineEntryRepositoryAsync` and `IApplicationDbContext` |
| Stop reorder | `ReorderTimelineEntriesCommand` |
| Stop visited flag | `MarkEntryVisitedCommand` |
| Flight/Hotel `select` endpoints | `TimelineEntry` custom flight/accommodation fields |

## Projects

| Project | Responsibility |
|---------|----------------|
| `OmniFlow.Domain` | Entities, enums, domain exceptions, base classes |
| `OmniFlow.Application` | DTOs, CQRS features, validators, interfaces, mappings, wrappers |
| `OmniFlow.Infrastructure` | EF Core context/configurations/migrations, repositories, external services |
| `OmniFlow.WebApi` | Controllers, middleware, DI, request pipeline |
| `Tests` | Unit, API integration, and infrastructure tests |

## Domain Entities

Current domain entity count: 24.

| Area | Entities |
|------|----------|
| Identity/Profile | `User`, `RefreshToken`, `EmailVerificationDispatch`, `PasswordResetToken` |
| Trip planning | `Trip`, `TripDestination`, `TimelineEntry`, `Place`, `Flight`, `Hotel`, `ProviderFlight`, `ProviderHotel` |
| Social | `Post`, `Comment`, `CommunityTip`, `Follow`, `Block` |
| Reactions/Saves | `PostUpvote`, `CommentUpvote`, `TipUpvote`, `TripUpvote`, `SavedTrip` |
| System | `Notification`, `KarmaEvent` |

## Database Tables

Current domain/application table count: 24. `ApplicationUser` and domain `User` share the `users`
table through ASP.NET Identity mapping.

| Entity | Table | Notes |
|--------|-------|-------|
| `User` / `ApplicationUser` | `users` | Identity + domain profile in one table; includes profile location and travel styles |
| `Trip` | `trips` | Includes origin, wizard preferences, counters, tags |
| `TripDestination` | `trip_destinations` | Ordered destination legs |
| `TimelineEntry` | `timeline_entries` | Current itinerary item model |
| `Place` | `places` | Catalog/searchable place data |
| `Flight` | `flights` | Legacy/manual flight records still present |
| `Hotel` | `hotels` | Legacy/manual hotel records still present |
| `ProviderFlight` | `provider_flights` | Provider flight options with freshness metadata |
| `ProviderHotel` | `provider_hotels` | Provider hotel options with freshness metadata |
| `Post` | `posts` | Feed/explore social content |
| `Comment` | `comments` | Post comments |
| `CommunityTip` | `community_tips` | Trip/place tips |
| `Follow` | `follows` | User follow graph |
| `Block` | `blocks` | User block graph |
| `PostUpvote` | `post_upvotes` | Post reactions |
| `CommentUpvote` | `comment_upvotes` | Comment reactions |
| `TipUpvote` | `tip_upvotes` | Tip reactions |
| `TripUpvote` | `trip_upvotes` | Trip reactions |
| `SavedTrip` | `saved_trips` | Saved trips |
| `Notification` | `notifications` | In-app notifications |
| `KarmaEvent` | `karma_events` | Karma audit events |
| `RefreshToken` | `refresh_tokens` | Hashed refresh tokens |
| `EmailVerificationDispatch` | `email_verification_dispatches` | Email verification send tracking |
| `PasswordResetToken` | `password_reset_tokens` | Password reset tokens |

Identity also maps the standard support tables: `roles`, `user_roles`, `user_claims`,
`user_logins`, `user_tokens`, and `role_claims`.

## Enum List

Current enum count: 25.

`BudgetTier`, `CabinClass`, `CancellationPolicy`, `FlightDataSource`, `FlightDirection`,
`FlightStatus`, `HotelDataSource`, `HotelStatus`, `KarmaEventType`, `KarmaSourceType`,
`NotificationTargetType`, `NotificationType`, `PlaceCategory`, `PostType`, `Roles`, `RoomType`,
`Season`, `StopAddedBy`, `Tempo`, `TimelineEntryType`, `TransportMode`, `TransportPreference`,
`TravelCompanion`, `TravelStyle`, `TripStatus`.

Note: `StopAddedBy` still exists in the enum folder for compatibility/history, but there is no
current `Stop` entity/controller/repository.

## EF Configurations

Current configuration files:

`BlockConfiguration`, `CommentConfiguration`, `CommentUpvoteConfiguration`,
`CommunityTipConfiguration`, `FlightConfiguration`, `FollowConfiguration`, `HotelConfiguration`,
`KarmaEventConfiguration`, `NotificationConfiguration`, `PlaceConfiguration`, `PostConfiguration`,
`PostUpvoteConfiguration`, `ProviderFlightConfiguration`, `ProviderHotelConfiguration`,
`RefreshTokenConfiguration`, `SavedTripConfiguration`, `TimelineEntryConfiguration`,
`TipUpvoteConfiguration`, `TripConfiguration`, `TripDestinationConfiguration`,
`TripUpvoteConfiguration`, `UserConfiguration`.

`EmailVerificationDispatch` and `PasswordResetToken` are mapped directly in `ApplicationDbContext`.

## Repository Interfaces

Current specific repository interfaces:

`IUserRepositoryAsync`, `ITripRepositoryAsync`, `ITripDestinationRepositoryAsync`,
`ITimelineEntryRepositoryAsync`, `IProviderHotelRepositoryAsync`, `IProviderFlightRepositoryAsync`,
`IPostRepositoryAsync`, `IPlaceRepositoryAsync`, `INotificationRepositoryAsync`,
`IKarmaEventRepositoryAsync`, `IHotelRepositoryAsync`, `IFollowRepositoryAsync`,
`IFlightRepositoryAsync`, `ICommunityTipRepositoryAsync`, `ICommentRepositoryAsync`.

Many junction tables are intentionally accessed through `IApplicationDbContext` rather than a
dedicated repository.

## Controllers

Current v1 controller list:

`AdminController`, `BlocksController`, `CommentsController`, `CommunityTipsController`,
`ExploreController`, `FeedController`, `FlightsController`, `FollowsController`, `HotelsController`,
`KarmaController`, `MediaController`, `NotificationsController`, `PlacesController`,
`PostsController`, `ProvidersController`, `SavedTripsController`, `TimelineController`,
`TripDestinationsController`, `TripsController`, `UsersController`.

Root/non-v1 controllers:

`AccountController`, `BaseApiController`, `MetaController`.

## Current Trip-Planning Endpoints

| Endpoint | Purpose |
|----------|---------|
| `GET /api/v1/trips/{tripId}/destinations` | List trip destinations |
| `POST /api/v1/trips/{tripId}/destinations` | Add destination |
| `PUT /api/v1/trips/{tripId}/destinations/{destinationId}` | Update destination |
| `DELETE /api/v1/trips/{tripId}/destinations/{destinationId}` | Delete destination |
| `GET /api/v1/trips/{tripId}/timeline` | List timeline entries |
| `POST /api/v1/trips/{tripId}/timeline/entry` | Add timeline entry |
| `PUT /api/v1/trips/{tripId}/timeline/entry/{entryId}` | Update timeline entry |
| `DELETE /api/v1/trips/{tripId}/timeline/entry/{entryId}` | Delete timeline entry |
| `PUT /api/v1/trips/{tripId}/timeline/reorder` | Reorder timeline entries |
| `PUT /api/v1/trips/{tripId}/timeline/entry/{entryId}/visited` | Mark entry visited/unvisited |

Provider lookups remain under `ProvidersController`; trip-owned flight/hotel reads remain under
`FlightsController` and `HotelsController`, but booking selection is no longer exposed as
`select` endpoints.

## Migration List

Current migration count: 13.

1. `20260316193107_InitialCreate`
2. `20260414223328_AddProviderTables`
3. `20260414225022_AddUserBlockFeature`
4. `20260419214902_AddPasswordResetToken`
5. `20260423221909_AddOsmFieldsToPlaces`
6. `20260423231441_AddGooglePlaceFieldsToPlaces`
7. `20260426220607_TripPlanningV1`
8. `20260428152254_UpdateTripDestinationOrderIndexLimit`
9. `20260501080302_MakeTripDestinationOrderIndexDeferrableWithSoftDeleteFilter`
10. `20260501113636_TripPlanningCleanupV1`
11. `20260501112921_AllowOrderIndexZeroForShift`
12. `20260705104746_AddProviderFreshnessFields`
13. `20260705114209_AddUserProfileLocationTravelStyles`

Provider lookup responses include freshness metadata from provider tables:
`LastUpdatedAt`, `IsLiveData`, and `DataSnapshotDate`.

User profile responses include `Location` and `TravelStyles`; user `travel_styles` is stored as
PostgreSQL `jsonb`.

## Design Notes

- `AuditableBaseEntity` entities are globally filtered with `DeletedAt == null`.
- `BaseEntity` entities are not soft-deleted unless handled explicitly.
- PostgreSQL extensions currently configured: `citext`, `postgis`.
- `Trip.TravelStyles` and several place fields use PostgreSQL arrays with explicit EF value
  converters/comparers.
- Timeline/day planning must use `TimelineEntry + TripDestination`; do not reintroduce `Stop`.
