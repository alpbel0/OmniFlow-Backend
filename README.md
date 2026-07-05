# OmniFlow Backend

ASP.NET Core 8 backend for OmniFlow, a travel planning and social route-sharing platform.

This repository contains the backend API, application layer, domain model, EF Core infrastructure,
and test projects. The current backend uses the post-MVP trip planning model:

- `TripDestination` for ordered destination legs.
- `TimelineEntry` for places, custom flights, transport, accommodation, and events.
- Provider flight/hotel data is queried separately and can be represented in timeline entries.
- The legacy `Stop` model, `StopsController`, `IStopRepositoryAsync`, and flight/hotel `select`
  endpoints are no longer part of the active architecture.

## Stack

| Layer | Technology |
|-------|------------|
| Backend | ASP.NET Core 8.0 |
| Application | Clean Architecture, CQRS with MediatR, FluentValidation, AutoMapper |
| Database | PostgreSQL 15, EF Core, `citext`, `postgis` |
| Auth | ASP.NET Identity + JWT access/refresh tokens |
| Tests | xUnit, FluentAssertions, API integration tests |

## Project Layout

```text
OmniFlow/
  OmniFlow.Domain/         entities, enums, domain exceptions
  OmniFlow.Application/    DTOs, interfaces, CQRS features, validators, mappings
  OmniFlow.Infrastructure/ EF Core context/configurations/migrations, repositories, services
  OmniFlow.WebApi/         controllers, middleware, DI, request pipeline
Tests/
  OmniFlow.UnitTests/
  OmniFlow.Api.IntegrationTests/
  OmniFlow.Infrastructure.Tests/
docs/
  BACKEND_SCHEMA_MVP.md
  BACKEND_ROADMAP_V2.md
```

## Feature Areas

- Account/authentication with JWT and refresh tokens.
- Trip CRUD, publish/archive, fork, upvote, save.
- Multi-destination planning through `TripDestination`.
- Itinerary planning through `TimelineEntry`.
- Provider flight/hotel lookup data.
- Places, explore, feed, posts, comments, community tips.
- Follow/block graph, saved trips, karma, notifications.
- Admin user/content management.
- Media upload support through the existing blob service.

## Travel Planning Model

The route planning surface is no longer stop-based.

| Concept | Current implementation |
|---------|------------------------|
| Route legs | `TripDestination` |
| Daily itinerary rows | `TimelineEntry` |
| Place activity | `TimelineEntry.EntryType = Place` |
| Custom flight | `TimelineEntry.EntryType = CustomFlight` |
| Custom hotel/accommodation | `TimelineEntry.EntryType = CustomAccommodation` |
| Custom transport | `TimelineEntry.EntryType = CustomTransport` |
| Custom event | `TimelineEntry.EntryType = CustomEvent` |

Current trip-planning endpoints:

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

## Database Snapshot

Current domain/application table count: 24.

Core trip/profile tables:

`users`, `trips`, `trip_destinations`, `timeline_entries`, `places`, `flights`, `hotels`,
`provider_flights`, `provider_hotels`.

Social/system tables:

`posts`, `comments`, `community_tips`, `follows`, `blocks`, `post_upvotes`,
`comment_upvotes`, `tip_upvotes`, `trip_upvotes`, `saved_trips`, `notifications`,
`karma_events`, `refresh_tokens`, `email_verification_dispatches`, `password_reset_tokens`.

ASP.NET Identity also maps `roles`, `user_roles`, `user_claims`, `user_logins`,
`user_tokens`, and `role_claims`.

Current enum count: 25. Current EF migration count: 14. See
[`docs/BACKEND_SCHEMA_MVP.md`](docs/BACKEND_SCHEMA_MVP.md) for the detailed as-built schema.

## Current Travel Styles

`Romantic`, `Cultural`, `Adventure`, `Nature`, `Local`, `Relax`, `Shopping`, `Gastronomy`,
`Influencer`, `Nightlife`, `Budget`.

## Running Locally

Prerequisites:

- .NET 8 SDK
- PostgreSQL with `citext` and `postgis`

Commands:

```bash
dotnet restore OmniFlow/OmniFlow.WebApi/OmniFlow.WebApi.csproj
dotnet ef database update --project OmniFlow/OmniFlow.Infrastructure --startup-project OmniFlow/OmniFlow.WebApi
dotnet run --project OmniFlow/OmniFlow.WebApi/OmniFlow.WebApi.csproj
```

## Tests

Unit tests:

```bash
dotnet test Tests/OmniFlow.UnitTests/OmniFlow.UnitTests.csproj --no-restore
```

API integration tests use `TEST_DB_CONNECTION` when set, otherwise they default to:

```text
Host=localhost;Port=5432;Database=omniflow_dev;Username=postgres;Password=postgres
```

Solution-level `dotnet test OmniFlow/OmniFlow.slnx` currently requires SDK support for `.slnx`.
`BACKEND_ROADMAP_V2` task B0.2 tracks the CI fix.

## Deployment

The repository includes:

- `Dockerfile`
- `azure-pipelines.yml`
- App Service oriented publish/deploy pipeline

## Roadmaps

- `docs/BACKEND_ROADMAP_MVP.md` documents the original MVP plan.
- `docs/BACKEND_SCHEMA_MVP.md` is now the current as-built backend snapshot.
- `docs/BACKEND_ROADMAP_V2.md` tracks post-MVP backend work for the live product/mobile phase.
