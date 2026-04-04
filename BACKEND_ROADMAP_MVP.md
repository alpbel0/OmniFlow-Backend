# OmniFlow Backend — Project Roadmap (MVP / CSE332 Aşaması)

**Proje:** OmniFlow Backend — ASP.NET Core 8.0 Clean Architecture API  
**Phase 1:** Infrastructure & Auth (3 hafta)  
**Phase 2:** Core Domain — Trips, Places, Stops (4 hafta)  
**Phase 3:** Booking — Flights & Hotels (2 hafta)  
**Phase 4:** Social — Posts, Comments, Feed, Follow (4 hafta)  
**Phase 5:** Community — Tips, Karma, Notifications (3 hafta)  
**Phase 6:** AI Integration — Timeline Generation & Fallback (2 hafta)  
**Phase 7:** Testing, Polish & Deployment (2 hafta)  

**Toplam:** ~20 hafta

---

## 📋 İçindekiler

- [Phase 1: Infrastructure & Auth](#-phase-1-overview)
- [Phase 2: Core Domain — Trips, Places, Stops](#-phase-2-overview)
- [Phase 3: Booking — Flights & Hotels](#-phase-3-overview)
- [Phase 4: Social — Posts, Comments, Feed, Follow](#-phase-4-overview)
- [Phase 5: Community — Tips, Karma, Notifications](#-phase-5-overview)
- [Phase 6: AI Integration](#-phase-6-overview)
- [Phase 7: Testing, Polish & Deployment](#-phase-7-overview)

---

## 🎯 Phase 1 Overview

### Scope #24.03.2026

**Dahil:**
- Solution yapısı (4 proje: Domain, Application, Infrastructure, WebApi)
- Domain katmanı: 18 entity, 20 enum, 3 domain exception
- Application katmanı: Interface'ler, application exception'lar, IApplicationDbContext
- Azure PostgreSQL bağlantısı, EF Core DbContext, Initial Migration (18 tablo)
- ASP.NET Identity + JWT authentication
- Register, Login, Refresh Token, Forgot Password endpoint'leri
- Dual refresh token stratejisi (web: cookie, mobile: body)
- Swagger/OpenAPI yapılandırması
- Global error handling middleware
- Seed data (default roller, admin kullanıcı)

**Hariç:**
- Domain feature'ları (Trips, Posts, vs.)
- AI entegrasyonu
- Frontend bağlantısı
- UserBlock, PushToken (bitirme)

### Definition of Done

Phase 1 tamamlanmış sayılır eğer:
- [x] 4 proje oluşturuldu ve referans zinciri doğru (Domain → Application → Infrastructure → WebApi)
- [x] 18 entity, 20 enum, tüm EF Core configuration'lar yazıldı
- [x] Azure PostgreSQL'e migration başarıyla uygulandı, 18 tablo oluştu
- [x] Register → Login → Access Token → Refresh Token akışı çalışıyor
- [x] Web (cookie) ve Mobile (body) refresh akışı ayrı çalışıyor
- [x] Swagger UI'da tüm auth endpoint'leri test edilebiliyor
- [x] Seed data ile default admin + traveler roller oluşuyor
- [x] Global error handler tüm exception tiplerini yakalıyor
- [x] Phase 1 testleri geçiyor

---

## 📅 Week 1: Solution Setup & Domain Layer

**Hedef:** Solution yapısı, Domain katmanı (Entities, Enums, Exceptions), proje referansları

---

### Task 1.1: Solution & Project Creation

**Tahmini Süre:** 2 saat  
**Durum:** ✅ Tamamlandı

**Yapılacaklar:**
- [x] OmniFlow solution'ı oluştur
- [x] 4 proje oluştur: OmniFlow.Domain, OmniFlow.Application, OmniFlow.Infrastructure, OmniFlow.WebApi
- [x] Solution'a tüm projeleri ekle
- [x] Referans zincirini kur: Application → Domain, Infrastructure → Application, WebApi → Application + Infrastructure
- [x] Build al ve 0 error olduğunu doğrula
- [x] .gitignore dosyası ekle (dotnet template)
- [x] Initial commit yap

---

### Task 1.2: Domain — Base Entity'ler

**Tahmini Süre:** 1 saat  
**Durum:** ✅ Tamamlandı

**Yapılacaklar:**
- [x] `Domain/Common/BaseEntity.cs` oluştur — Guid Id property, auto-generate
- [x] `Domain/Common/AuditableBaseEntity.cs` oluştur — BaseEntity'den türer, CreatedAt, UpdatedAt, DeletedAt (nullable) property'leri

---

### Task 1.3: Domain — Enums (20 adet)

**Tahmini Süre:** 2 saat  
**Durum:** ✅ Tamamlandı

**Yapılacaklar:**
- [x] Roles — Traveler, Admin
- [x] BudgetTier — Economy, Standard, Premium
- [x] TravelStyle — Solo, Family, Adventure, Luxury, Relax
- [x] TripStatus — Draft, Published, Archived
- [x] PlaceCategory — Museum, Restaurant, Cafe, Historical, Nature, Entertainment, Hotel, Transport
- [x] TransportMode — Walking, Metro, Taxi, Bus, Car, Train, Ferry
- [x] StopAddedBy — Ai, User
- [x] CabinClass — Economy, Business, First
- [x] FlightStatus — Scheduled, Cancelled, Completed
- [x] FlightDirection — Outbound, Return
- [x] FlightDataSource — Mock, Amadeus, RapidApi
- [x] RoomType — Single, Double, Suite, Deluxe
- [x] CancellationPolicy — Free, NonRefundable, Partial
- [x] HotelStatus — Confirmed, Cancelled, Completed
- [x] HotelDataSource — Mock, Amadeus, RapidApi
- [x] PostType — Photo, Tip, Route
- [x] NotificationType — Follow, PostUpvote, CommentUpvote, TipUpvote, TripUpvote, Comment, Mention, Fork
- [x] NotificationTargetType — Post, Trip, Comment, Tip
- [x] KarmaEventType — TripPublished, TripForked, PostUpvoted, TipUpvoted, TripUpvoted
- [x] KarmaSourceType — Trip, Post, Tip

**Notlar:**
- Tüm enum değerler PostgreSQL ENUM'larıyla birebir eşleşecek
- EF Core tarafında string conversion kullanılacak

---

### Task 1.4: Domain — Core Entities (6 adet)

**Tahmini Süre:** 3 saat  
**Durum:** ✅ Tamamlandı

**Yapılacaklar:**
- [x] `User.cs` — AuditableBaseEntity'den türer. Username (string), Email (string), Bio (nullable), ProfilePhotoUrl (nullable), KarmaScore (int, default 0), FollowersCount (int, default 0), FollowingCount (int, default 0), Role (Roles enum, default Traveler), IsVerified (bool), IsSuspended (bool). Navigation property'ler: Trips, Posts, Followers, Following koleksiyonları. Not: password_hash tutmaz, bu ApplicationUser (Identity) tarafında
- [x] `Trip.cs` — AuditableBaseEntity'den türer. OwnerId (Guid), ForkedFromId (nullable Guid), Title (string, max 100), Description (nullable), CoverPhotoUrl (nullable), Status (TripStatus, default Draft), City (string), Country (string), StartDate (DateOnly), EndDate (DateOnly), PersonCount (int, default 1), BudgetTier (enum), TravelStyle (enum), UserBudget (nullable decimal), EstimatedCost (nullable decimal), ForkCount/UpvoteCount/ViewCount (int, default 0), PopularityScore (decimal, default 0), Tags (List of string). Navigation: Owner, Stops, Flights, Hotels koleksiyonları
- [x] `Place.cs` — BaseEntity'den türer (soft delete yok, is_active var). Name (string), Description (nullable), Category (PlaceCategory), PhotoUrl (nullable), Phone (nullable), WebsiteUrl (nullable), Latitude (double), Longitude (double), Address (nullable), City (string), Country (string), Timezone (nullable), GooglePlaceId (nullable, unique), EstimatedPrice (decimal, default 0), CurrencyCode (string, default "USD"), IsFree (bool), BudgetTiers (List of BudgetTier), TravelStyles (List of TravelStyle), DurationMinutes (nullable int), Rating (nullable decimal), OpeningHours (nullable JSON), BestMonths (List of int), IsActive (bool, default true)
- [x] `Stop.cs` — AuditableBaseEntity'den türer. TripId (Guid), PlaceId (nullable Guid), FallbackPlaceId (nullable Guid), DayNumber (int), OrderIndex (double, LexoRank), ArrivalTime (TimeOnly nullable), DurationMinutes (nullable int), IsTimeLocked (bool), CustomName (nullable), CustomCategory (nullable PlaceCategory), CustomPhotoUrl (nullable), CustomLatitude/CustomLongitude (nullable double), Notes (nullable), BookingReference (nullable), ReservationNote (nullable), ActivityPrice (decimal, default 0), TransportPrice (decimal, default 0), CurrencyCode (string), TransportFromPrevious (nullable TransportMode), TravelTimeFromPrevious (nullable int), IsVisited (bool), VisitedAt (nullable DateTime), AddedBy (StopAddedBy, default User), AiReasoning (nullable). Navigation: Trip, Place, FallbackPlace
- [x] `Flight.cs` — TripId (Guid), ItineraryGroupId (nullable Guid), FlightDirection (enum), FromCity/FromAirport/ToCity/ToAirport (string), DepartureAt/ArrivalAt (DateTime, timezone'suz), DurationMinutes (int), Airline (string), FlightNumber (string), CabinClass (enum), IsDirect (bool), PricePerPerson/TotalPrice (decimal), CurrencyCode (string), IsBooked (bool), BookedAt (nullable), BookingReference (nullable), Status (FlightStatus), DataSource (FlightDataSource), DataFetchedAt (DateTime). Navigation: Trip
- [x] `Hotel.cs` — TripId (Guid), PlaceId (nullable Guid), HotelName (nullable), HotelLatitude/HotelLongitude (nullable double), HotelAddress/HotelPhone (nullable), ProviderUrl (nullable), Stars (nullable int), RoomType (enum), BreakfastIncluded (bool), CancellationPolicy (enum), CheckIn/CheckOut (DateTime, timezone'suz), PricePerNight/TotalPrice (decimal), CurrencyCode (string), IsBooked (bool), BookedAt (nullable), BookingReference (nullable), Status (HotelStatus), DataSource (HotelDataSource), DataFetchedAt (DateTime). Navigation: Trip, Place

---

### Task 1.5: Domain — Social Entities (9 adet)

**Tahmini Süre:** 2 saat  
**Durum:** ✅ Tamamlandı

**Yapılacaklar:**
- [x] `Post.cs` — AuditableBaseEntity'den türer. UserId (Guid), TripId (nullable Guid), PlaceId (nullable Guid), PostType (enum), Content (nullable), Photos (List of string, JSONB), Tags (List of string), AiTags (List of string), LocationLatitude/LocationLongitude (nullable double), City/Country (nullable), UpvoteCount/CommentCount (int, default 0), IsVisible (bool, default true). Navigation: User, Trip, Place, Comments
- [x] `Comment.cs` — AuditableBaseEntity'den türer. PostId (Guid), UserId (Guid), ParentCommentId (nullable Guid), Content (string), Mentions (List of string, JSONB), UpvoteCount (int, default 0), IsVisible (bool). Navigation: Post, User, ParentComment, Replies. Not: Composite FK ile cross-post koruması EF Configuration'da yapılacak
- [x] `CommunityTip.cs` — AuditableBaseEntity'den türer. TripId (Guid), UserId (Guid), PlaceId (nullable Guid), Content (string), UpvoteCount (int, default 0), IsVisible (bool). Navigation: Trip, User, Place
- [x] `Follow.cs` — BaseEntity'den türemez, composite PK. FollowerId (Guid), FollowingId (Guid), CreatedAt (DateTime). Navigation: Follower, Following
- [x] `PostUpvote.cs` — Composite PK. PostId (Guid), UserId (Guid), CreatedAt
- [x] `CommentUpvote.cs` — Composite PK. CommentId (Guid), UserId (Guid), CreatedAt
- [x] `TipUpvote.cs` — Composite PK. TipId (Guid), UserId (Guid), CreatedAt
- [x] `TripUpvote.cs` — Composite PK. TripId (Guid), UserId (Guid), CreatedAt
- [x] `SavedTrip.cs` — Composite PK. UserId (Guid), TripId (Guid), CreatedAt

---

### Task 1.6: Domain — System Entities (3 adet)

**Tahmini Süre:** 1 saat  
**Durum:** ✅ Tamamlandı

**Yapılacaklar:**
- [x] `Notification.cs` — BaseEntity'den türer. UserId (Guid), ActorId (nullable Guid), NotificationType (enum), TargetId (nullable Guid), TargetType (nullable enum), IsRead (bool, default false), ReadAt (nullable DateTime), CreatedAt. Navigation: User, Actor
- [x] `KarmaEvent.cs` — BaseEntity'den türer. UserId (Guid), ActorId (nullable Guid), EventType (KarmaEventType), Points (int), SourceId (nullable Guid), SourceType (nullable KarmaSourceType), CreatedAt. Navigation: User, Actor
- [x] `RefreshToken.cs` — BaseEntity'den türer. UserId (Guid), TokenHash (string), ExpiresAt (DateTime), RevokedAt (nullable DateTime), DeviceFingerprint (nullable string), CreatedAt

---

### Task 1.7: Domain — Exceptions

**Tahmini Süre:** 30 dakika  
**Durum:** ✅ Tamamlandı

**Yapılacaklar:**
- [x] `DuplicateUpvoteException.cs` — Mesajda content type ve content ID bilgisi olacak
- [x] `SelfFollowException.cs` — Mesajda user ID bilgisi olacak
- [x] `SelfForkException.cs` — Mesajda user ID ve trip ID bilgisi olacak
- [x] Domain projesini build et, 0 error doğrula

---

## 📅 Week 2: Infrastructure — DbContext, Migrations, Auth

**Hedef:** EF Core DbContext, 18 tablo configuration, Azure PostgreSQL migration, ASP.NET Identity, seed data

---

### Task 2.1: NuGet Paketleri

**Tahmini Süre:** 1 saat  
**Durum:** ✅ Tamamlandı

**Yapılacaklar:**
- [x] Infrastructure'a ekle: Npgsql.EntityFrameworkCore.PostgreSQL, Microsoft.AspNetCore.Identity.EntityFrameworkCore, Microsoft.EntityFrameworkCore.Tools
- [x] Application'a ekle: MediatR, FluentValidation, FluentValidation.DependencyInjectionExtensions, AutoMapper.Extensions.Microsoft.DependencyInjection
- [x] WebApi'ye ekle: Microsoft.AspNetCore.Authentication.JwtBearer, Swashbuckle.AspNetCore
- [x] Build al, 0 error doğrula

---

### Task 2.2: Application — Exceptions & Interfaces

**Tahmini Süre:** 2 saat  
**Durum:** ✅ Tamamlandı

**Yapılacaklar:**
- [x] `Application/Exceptions/ApiException.cs` — HTTP/API seviyesi genel hata, mesaj ve status code tutar
- [x] `Application/Exceptions/ValidationException.cs` — FluentValidation hataları listesi tutar
- [x] `Application/Exceptions/EntityNotFoundException.cs` — Entity tipi ve ID bilgisi tutar
- [x] `Application/Exceptions/ForbiddenException.cs` — Yetkilendirme hatası mesajı tutar
- [x] `Application/Interfaces/IApplicationDbContext.cs` — 18 DbSet property tanımı (Users, Trips, Places, Stops, Flights, Hotels, Posts, Comments, CommunityTips, Follows, PostUpvotes, CommentUpvotes, TipUpvotes, TripUpvotes, SavedTrips, Notifications, KarmaEvents, RefreshTokens) + SaveChangesAsync metod imzası
- [x] `Application/Interfaces/IGenericRepositoryAsync.cs` — GetByIdAsync, GetAllAsync, GetPagedAsync, AddAsync, UpdateAsync, DeleteAsync metod imzaları
- [x] `Application/Interfaces/IAccountService.cs` — RegisterAsync, LoginAsync, RefreshTokenAsync, ForgotPasswordAsync metod imzaları
- [x] `Application/Interfaces/IAuthenticatedUserService.cs` — UserId property
- [x] `Application/Interfaces/IDateTimeService.cs` — NowUtc property
- [x] `Application/Parameters/RequestParameter.cs` — PageNumber, PageSize property'leri
- [x] `Application/Wrappers/PagedResponse.cs` — Data, PageNumber, PageSize, TotalCount
- [x] `Application/Wrappers/ErrorResponse.cs` — Message, Errors listesi

---

### Task 2.3: Infrastructure — ApplicationUser & Identity

**Tahmini Süre:** 30 dakika  
**Durum:** ✅ Tamamlandı

**Yapılacaklar:**
- [x] `Infrastructure/Models/ApplicationUser.cs` oluştur — IdentityUser<Guid>'den türer, sadece Identity/auth modeli olarak kalır
- [x] Domain User entity ile aynı Guid Id'yi paylaşacak, aralarında 1:1 mapping ApplicationDbContext'te tanımlanacak

**Notlar:**
- ApplicationUser: login, password hash, token — Identity concern'ü
- Domain User: bio, karma, followers — business concern'ü
- Aynı `users` tablosu, iki farklı projection, paralel veri taşıma yok

---

### Task 2.4: Infrastructure — EF Core Configurations (18 adet)

**Tahmini Süre:** 5 saat  
**Durum:** ✅ Tamamlandı

**Yapılacaklar:**
- [x] `UserConfiguration.cs` — users tablosu, citext kolonlar, partial unique index'ler (deleted_at IS NULL), non_negative_follow_counts CHECK, username_format regex CHECK
- [x] `TripConfiguration.cs` — trips tablosu, enum string conversion'lar, valid_dates CHECK (end >= start), valid_person_count CHECK, non_negative_counts CHECK, tags_is_array CHECK, idx_trips_explore partial index (published + deleted_at IS NULL), GIN index (tags)
- [x] `PlaceConfiguration.cs` — places tablosu, PostGIS Point (latitude/longitude), valid_rating CHECK (1-5), free_has_zero_price CHECK, valid_best_months CHECK, GIN index'ler (budget_tier, travel_style, best_months, opening_hours), GIST index (location), city partial index
- [x] `StopConfiguration.cs` — stops tablosu, place_or_custom_name CHECK, custom_place_requires_category CHECK, time_lock_requires_arrival CHECK, visited_consistency CHECK, fallback_differs_from_place CHECK, ai_reasoning_required CHECK, LexoRank order_index (double precision), composite index (trip_id, day_number, order_index)
- [x] `FlightConfiguration.cs` — flights tablosu, IATA code regex CHECK (from_airport, to_airport), valid_duration CHECK, booked_consistency CHECK, booking_ref_requires_is_booked CHECK, departure/arrival TIMESTAMP (timezone'suz), itinerary_group_id index
- [x] `HotelConfiguration.cs` — hotels tablosu, place_or_hotel_name CHECK, valid_dates CHECK (check_out > check_in), valid_stars CHECK (1-5), booked_consistency CHECK, GIST index (hotel_location)
- [x] `PostConfiguration.cs` — posts tablosu, route_requires_trip CHECK, content_or_photo CHECK, tags/photos/ai_tags is_array CHECK, non_negative_counts CHECK, partial index'ler (deleted_at IS NULL AND is_visible = TRUE), GIN index (tags, ai_tags), GIST index (location)
- [x] `CommentConfiguration.cs` — comments tablosu, Composite FK kurulumu: (parent_comment_id, post_id) → comments(id, post_id) cross-post koruması, UNIQUE (id, post_id) eklenmesi, valid_content CHECK, GIN index (mentions)
- [x] `CommunityTipConfiguration.cs` — community_tips tablosu, valid_content CHECK, partial index'ler
- [x] `FollowConfiguration.cs` — follows tablosu, composite PK (follower_id, following_id), no_self_follow CHECK, following_id index
- [x] `PostUpvoteConfiguration.cs` — composite PK (post_id, user_id), user_id index
- [x] `CommentUpvoteConfiguration.cs` — composite PK (comment_id, user_id), user_id index
- [x] `TipUpvoteConfiguration.cs` — composite PK (tip_id, user_id), user_id index
- [x] `TripUpvoteConfiguration.cs` — composite PK (trip_id, user_id), user_id index
- [x] `SavedTripConfiguration.cs` — composite PK (user_id, trip_id), trip_id index
- [x] `NotificationConfiguration.cs` — valid_notification_target_type CHECK (her notification_type hangi target_type ile gelebilir), follow_has_no_target CHECK, read_consistency CHECK, partial index (is_read = FALSE)
- [x] `KarmaEventConfiguration.cs` — valid_event_source_type CHECK, source_consistency CHECK, valid_points CHECK (!=0), farming koruması unique index'ler: idx_karma_publish_unique (user_id, source_id, event_type WHERE trip_published), idx_karma_interaction_unique (user_id, source_id, event_type, actor_id WHERE diğerleri)
- [x] `RefreshTokenConfiguration.cs` — valid_expiry CHECK (expires_at > created_at), partial unique index (token_hash WHERE revoked_at IS NULL), user partial index (WHERE revoked_at IS NULL)

**Notlar:**
- Her Configuration omniflow_sql_schema_V3.md ile birebir eşleşmeli
- CHECK constraint'ler HasCheckConstraint() ile, partial index'ler HasFilter() ile, GIN index HasMethod("gin") ile

---

### Task 2.5: Infrastructure — ApplicationDbContext

**Tahmini Süre:** 2 saat  
**Durum:** ✅ Tamamlandı

**Yapılacaklar:**
- [x] `ApplicationDbContext.cs` oluştur — IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>'den türer, IApplicationDbContext implement eder
- [x] 18 DbSet property tanımla
- [x] OnModelCreating'de PostgreSQL extension'ları aktifleştir: citext, postgis
- [x] OnModelCreating'de ApplyConfigurationsFromAssembly ile tüm configuration'ları otomatik yükle
- [x] SaveChangesAsync override — modified entity'lerde UpdatedAt otomatik güncelleme
- [x] Build al, 0 error doğrula

---

### Task 2.6: Azure PostgreSQL Connection & Migration

**Tahmini Süre:** 2 saat
**Durum:** ✅ Tamamlandı

**Yapılacaklar:**
- [x] appsettings.Development.json'a Azure PostgreSQL connection string ekle (Host, Database, Username, Password, SSL Mode)
- [x] Infrastructure/ServiceRegistration.cs'de DbContext DI registration yap
- [x] Initial migration oluştur (InitialCreate)
- [x] Migration'ı Azure PostgreSQL'e uygula
- [x] pgAdmin veya psql ile 18 tablonun oluştuğunu doğrula
- [x] CHECK constraint'lerin uygulandığını kontrol et (information_schema.check_constraints sorgusu)
- [x] Index'lerin oluştuğunu kontrol et (pg_indexes sorgusu)

---

### Task 2.7: Seed Data

**Tahmini Süre:** 1 saat  
**Durum:** ✅ Tamamlandı

**Yapılacaklar:**
- [x] `DefaultRoles.cs` — "Traveler" ve "Admin" rollerini oluştur (yoksa ekle, varsa atla)
- [x] `DefaultSuperAdmin.cs` — admin@omniflow.com kullanıcısını oluştur, Admin rolü ata, aynı ID ile Domain User entity de oluştur
- [x] `DefaultBasicUser.cs` — traveler@omniflow.com kullanıcısını oluştur, Traveler rolü ata
- [x] Program.cs'de uygulama başlarken seed metotlarını çağır
- [x] Uygulamayı çalıştır, seed data'nın oluştuğunu DB'den doğrula

---

## 📅 Week 3: Auth Endpoints & Token Refresh

**Hedef:** Register, Login, Refresh Token (dual platform), JWT yapılandırması, error handling middleware, Swagger

---

### Task 3.1: Application — Auth DTOs

**Tahmini Süre:** 1 saat  
**Durum:** ✅ Tamamlandı

**Yapılacaklar:**
- [x] `RegisterRequest.cs` — Username, Email, Password, ConfirmPassword property'leri
- [x] `AuthenticationRequest.cs` — Email, Password
- [x] `AuthenticationResponse.cs` — AccessToken (string), RefreshToken (string, sadece mobile'da dönecek), User bilgisi (Id, Username, Email, Role)
- [x] `RefreshTokenRequest.cs` — RefreshToken (string, mobile body'den gelecek)
- [x] `ForgotPasswordRequest.cs` — Email

---

### Task 3.2: Application — Auth Interface & Settings

**Tahmini Süre:** 1 saat  
**Durum:** ✅ Tamamlandı

**Yapılacaklar:**
- [x] `IAccountService.cs` interface'inde 4 metod: RegisterAsync, LoginAsync, RefreshTokenAsync, ForgotPasswordAsync
- [x] `JWTSettings.cs` — Key, Issuer, Audience, AccessTokenExpirationMinutes (default 15), RefreshTokenExpirationDays (default 7)
- [x] `MailSettings.cs` — SmtpHost, SmtpPort, SenderEmail, SenderName (placeholder, MVP'de kullanılmayacak)
- [x] `OpenAISettings.cs` — ApiKey, Model, MaxTokens (Phase 6'da kullanılacak ama yapı hazır)
- [x] `GoogleMapsSettings.cs` — ApiKey (frontend entegrasyonunda kullanılacak)

---

### Task 3.3: Infrastructure — AccountService Implementation

**Tahmini Süre:** 4 saat  
**Durum:** ✅ Tamamlandı

**Yapılacaklar:**
- [x] `AccountService.cs` oluştur, IAccountService'i implement et
- [x] RegisterAsync implementasyonu:
  - [x] UserManager ile ApplicationUser oluştur
  - [x] Traveler rolü ata
  - [x] Aynı Guid Id ile Domain User entity oluştur (bio, karma = 0)
  - [x] Access token üret (JWT, 15 dk ömür)
  - [x] Refresh token üret (random string → SHA256 hash → refresh_tokens tablosuna kaydet)
  - [x] AuthenticationResponse dön
- [x] LoginAsync implementasyonu:
  - [x] Email ile ApplicationUser bul
  - [x] Password doğrula
  - [x] Suspended kontrolü yap
  - [x] Token pair üret (access + refresh)
  - [x] AuthenticationResponse dön
- [x] RefreshTokenAsync implementasyonu:
  - [x] Gelen token'ı hash'le, refresh_tokens'ta ara
  - [x] Geçerliliğini kontrol et (expires_at > now, revoked_at IS NULL)
  - [x] Eski token'ı revoke et (revoked_at = now)
  - [x] Yeni token pair üret (rotation)
  - [x] AuthenticationResponse dön
- [x] JWT token generation helper metodu — claims: NameIdentifier (user ID), Email, Name (username), Role
- [x] Refresh token generation helper — 64 byte random → base64 → SHA256 hash DB'ye

**Notlar:**
- Access token: 15 dakika ömür, ClockSkew = 0
- Refresh token: 7 gün ömür, her kullanımda rotation (eski revoke, yeni üret)
- Token hash'lenerek saklanır, düz metin DB'de olmaz

---

### Task 3.4: WebApi — Auth Controller & Dual Platform Refresh

**Tahmini Süre:** 3 saat  
**Durum:** ✅ Tamamlandı

**Yapılacaklar:**
- [x] `AccountController.cs` oluştur (route: api/account)
- [x] POST /api/account/register endpoint'i:
  - [x] RegisterRequest body'den al
  - [x] AccountService.RegisterAsync çağır
  - [x] Refresh token'ı HttpOnly cookie olarak set et
  - [x] Access token + user bilgisi response body'de dön
- [x] POST /api/account/login endpoint'i:
  - [x] AuthenticationRequest body'den al
  - [x] AccountService.LoginAsync çağır
  - [x] Refresh token'ı HttpOnly cookie olarak set et
  - [x] Access token + user bilgisi dön
- [x] POST /api/account/refresh-token endpoint'i (dual platform):
  - [x] Önce cookie'den refresh token okumayı dene (web)
  - [x] Cookie yoksa body'den RefreshTokenRequest oku (mobile)
  - [x] İkisi de yoksa 400 Bad Request dön
  - [x] AccountService.RefreshTokenAsync çağır
  - [x] X-Platform header'ı "mobile" ise: access token + refresh token body'de dön
  - [x] Değilse (web): refresh token'ı cookie'ye yaz, sadece access token body'de dön
- [x] Cookie ayarları: HttpOnly = true, Secure = Request.IsHttps, SameSite = Strict, Expires = 7 gün
- [x] POST /api/account/forgot-password endpoint'i (placeholder, mail servisi MVP'de yok)

---

### Task 3.5: WebApi — JWT Configuration & Middleware

**Tahmini Süre:** 2 saat  
**Durum:** ✅ Tamamlandı

**Yapılacaklar:**
- [x] Program.cs'de JWT authentication yapılandırması: ValidateIssuer, ValidateAudience, ValidateLifetime, ValidateIssuerSigningKey, ClockSkew = TimeSpan.Zero
- [x] Global error handling middleware oluştur — exception tipine göre HTTP status code:
  - [x] ApiException → 400
  - [x] ValidationException → 422 (hata listesi ile)
  - [x] EntityNotFoundException → 404
  - [x] ForbiddenException → 403
  - [x] DuplicateUpvoteException → 409
  - [x] SelfFollowException → 409
  - [x] SelfForkException → 409
  - [x] Diğer Exception → 500 (log + generic mesaj)
- [x] Swagger'da JWT Authorization header desteği ekle (Bearer token input)
- [x] CORS policy ekle: frontend URL'leri (localhost:3000, production URL)
- [x] Program.cs'de middleware pipeline sıralaması: ErrorHandler → CORS → Authentication → Authorization → Controllers

**Test Kapsamı (Task 3.5):**
- [x] `ErrorHandlerMiddlewareTests` — 11 test: ApiException (400/401/custom), ValidationException (422+errors), EntityNotFoundException (404), ForbiddenException (403), DuplicateUpvoteException (409), SelfFollowException (409), SelfForkException (409), UnhandledException (500 generic), ContentType=JSON
- [x] `JwtConfigurationTests` — 7 test: WrongIssuer→401, WrongAudience→401, WrongKey→401, ExpiredToken→401 (ClockSkew=Zero), ValidToken→200, NoToken→401, CorrectIssuer→200
- [x] `CorsMiddlewareTests` — 6 test: Preflight localhost origin, Preflight production origin, AllowCredentials header, AllowCustomHeader, ActualRequest allowed origin, ActualRequest disallowed origin

---

### Task 3.6: WebApi — BaseApiController & MetaController

**Tahmini Süre:** 30 dakika  
**Durum:** ✅ Tamamlandı

**Yapılacaklar:**
- [x] `BaseApiController.cs` — abstract, route "api/v1/[controller]", IMediator property (lazy resolve from HttpContext)
- [x] `MetaController.cs` — GET /api/meta/health (basit health check), GET /api/meta/info (API versiyon bilgisi)

---

### Task 3.7: Application — ServiceExtensions & Behaviours

**Tahmini Süre:** 1 saat  
**Durum:** ✅ Tamamlandı

**Yapılacaklar:**
- [x] `Application/ServiceExtensions.cs` — MediatR, AutoMapper, FluentValidation DI registration
- [x] `Application/Behaviours/ValidationBehaviour.cs` — MediatR pipeline behaviour, request'i handler'a göndermeden önce FluentValidation çalıştır, hata varsa ValidationException fırlat
- [x] `Application/Mappings/GeneralProfile.cs` — boş AutoMapper profil (Phase 2'de mapping'ler eklenecek)

**Test Kapsamı (Task 3.7):**
- [x] `ValidationBehaviourTests` — 8 test: no validators→passes, passing validator→passes, failing validator→throws, error messages aggregated, multiple validators→all failures collected, next never called on failure, null input→correct error, ValidationException message correct
Kontrol edildi bu kısma kadar .

---

## 🧪 Phase 1 Test Gereksinimleri

### Unit Tests (OmniFlow.UnitTests)

- [x] **UserEntityTests** — Yeni User default değerleri doğru mu (Role = Traveler, KarmaScore = 0, IsVerified = false, Id != Empty, CreatedAt set edilmiş mi)
- [x] **TripEntityTests** — Yeni Trip default Status = Draft mi, counter'lar 0 mı
- [x] **StopEntityTests** — Yeni Stop default AddedBy = User mı, IsVisited = false mı
- [x] **ExceptionTests** — DuplicateUpvoteException mesajında content type ve ID var mı, SelfFollowException mesajında user ID var mı, SelfForkException mesajında trip ID var mı

### Integration Tests (OmniFlow.Api.IntegrationTests)

- [x] **Setup** — CustomWebApplicationFactory oluştur (in-memory veya test PostgreSQL), TestDatabaseSeeder ile seed data
- [x] **Register_WithValidData_ReturnsAccessToken** — Geçerli register request → 200, response'da accessToken var
- [x] **Register_WithDuplicateEmail_Returns400** — Aynı email ile ikinci register → 400
- [x] **Register_WithWeakPassword_Returns422** — Zayıf şifre → validation hatası
- [x] **Login_WithValidCredentials_ReturnsToken** — Doğru email/şifre → 200, accessToken var
- [x] **Login_WithWrongPassword_Returns401** — Yanlış şifre → 401
- [x] **Login_WithNonExistentEmail_Returns401** — Olmayan email → 401
- [x] **RefreshToken_WithCookie_ReturnsNewAccessToken** — Cookie'den refresh → yeni access token
- [x] **RefreshToken_WithBody_MobilePlatform_ReturnsTokenInBody** — X-Platform: mobile + body refresh → response'da hem accessToken hem refreshToken
- [x] **RefreshToken_WithExpiredToken_Returns401** — Süresi dolmuş token → 401
- [x] **RefreshToken_WithRevokedToken_Returns401** — Revoke edilmiş token → 401
- [x] **ProtectedEndpoint_WithoutToken_Returns401** — Token olmadan korumalı endpoint → 401
- [x] **ProtectedEndpoint_WithValidToken_Returns200** — Geçerli token ile → 200

---

## ✅ Phase 1 Success Metrics

### Technical Metrics

- [x] Build: 0 error, 0 warning
- [x] Migration: 18 tablo Azure PostgreSQL'de oluşturuldu
- [x] Auth Flow: Register → Login → Access Token → Refresh → New Token çalışıyor
- [x] Dual Platform: Web (cookie) ve Mobile (body) refresh ayrı çalışıyor
- [x] Swagger: Tüm auth endpoint'leri test edilebilir durumda

### Quality Metrics

- [x] Clean Architecture: Domain hiçbir projeye referans vermiyor, referans zinciri doğru
- [x] EF Core: Tüm CHECK constraint'ler ve index'ler SQL şemasıyla eşleşiyor
- [x] Security: Token hash'leniyor, HttpOnly cookie, Secure flag, ClockSkew = 0, rotation aktif
- [x] Error Handling: Global middleware 7 exception tipini yakalıyor, 500'lerde stack trace dışarı sızmıyor
- [x] Test: Phase 1 unit + integration testlerinin tamamı geçiyor

---

## 🎯 Phase 2 Overview

### Scope

**Dahil:**
- Places CRUD + şehir/kategori filtreleme.
- Trips CRUD + Publish/Archive state machine
- Stops CRUD + LexoRank reorder + time lock + custom stops
- Fork sistemi (deep copy + karma tetikleme placeholder)
- Explore endpoint (popularity score, filtreleme, pagination)
- Trip upvote + save/unsave
- Generic repository pattern

**Hariç:**
- Flights & Hotels (Phase 3)
- Social features (Phase 4)
- AI timeline generation (Phase 6)
- Karma puan hesaplama (Phase 5, fork'ta sadece placeholder)

### Definition of Done

Phase 2 tamamlanmış sayılır eğer:
- [ ] Places CRUD çalışıyor, şehir ve kategori bazlı filtreleme var
- [ ] Trip oluşturma → düzenleme → yayınlama → arşivleme state machine çalışıyor
- [ ] Sadece trip sahibi düzenleyebilir / silebilir / yayınlayabilir (owner authorization)
- [ ] Stop ekleme, düzenleme, silme, drag-and-drop sıralama çalışıyor
- [ ] Time-locked stop'lar reorder'da korunuyor, kilit varsa arrival_time zorunlu
- [ ] Custom stop oluşturulabiliyor (place_id NULL, custom_name + custom_category zorunlu)
- [ ] Fork yapılabiliyor, orijinal trip'in fork_count artıyor, self-fork engelleniyor
- [ ] Explore endpoint'i city/budget/style filter + popularity sort + cursor pagination ile çalışıyor
- [ ] Trip upvote ve save/unsave çalışıyor, çift upvote engelliyor
- [ ] Tüm endpoint'ler Swagger'da test edilebiliyor
- [ ] Phase 2 testleri geçiyor

---

## 📅 Week 4: Places — Entity, Repository, Endpoints

**Hedef:** Generic repository altyapısı, Places CRUD, şehir/kategori filtreleme, admin-only create

---

### Task 4.1: Application — Generic Repository & Wrappers

**Tahmini Süre:** 1.5 saat
**Durum:** ✅ Tamamlandı

**Yapılacaklar:**
- [x] `IGenericRepositoryAsync.cs` interface'ini finalize et — GetByIdAsync, GetAllAsync, GetPagedAsync (RequestParameter alır, PagedResponse döner), AddAsync, UpdateAsync, DeleteAsync
- [x] `Infrastructure/Repositories/GenericRepositoryAsync.cs` implementasyonu — ApplicationDbContext inject, LINQ ile pagination, total count hesaplama
- [x] DI registration (ServiceRegistration.cs)
- [x] `IApplicationDbContext`'e `Set<T>()` metodu eklendi
- [x] EF Core Global Query Filter (soft-delete için Expression Tree)
- [x] DeleteAsync soft-delete/hard-delete ayrımı
- [x] Infrastructure.Tests ile entegrasyon testleri (9 test, omniflow_dev PostgreSQL)

---

### Task 4.2: Application — Place Feature (CQRS)

**Tahmini Süre:** 3 saat
**Durum:** ✅ Tamamlandı

**Yapılacaklar:**
- [x] `IPlaceRepositoryAsync.cs` — IGenericRepositoryAsync<Place>'den türer, ek metotlar: GetByCityAsync (city, pagination), GetByCategoryAsync (category, pagination)
- [x] `PlaceResponse.cs` DTO — Id, Name, Category, Latitude, Longitude, City, Country, Rating, EstimatedPrice, IsFree, BudgetTiers, TravelStyles, DurationMinutes bilgilerini içerir
- [x] `CreatePlaceRequest.cs` DTO — Tüm place field'larını içerir
- [x] `CreatePlaceCommand.cs` — MediatR IRequest, tüm place field'larını alır, handler'da repository'ye ekler
- [x] `CreatePlaceCommandValidator.cs` — Name boş olamaz (max 255), Category enum'da olmalı, City ve Country zorunlu, Latitude -90/90 arası, Longitude -180/180 arası, Rating 1-5 arası (nullable), EstimatedPrice >= 0, CurrencyCode 3 harf büyük harf regex
- [x] `GetAllPlacesQuery.cs` — pagination parametreleri alır, PagedResponse<PlaceResponse> döner
- [x] `GetPlaceByIdQuery.cs` — Guid alır, PlaceResponse döner, bulunamazsa EntityNotFoundException
- [x] `GetPlacesByCityQuery.cs` — city string + pagination alır, PagedResponse<PlaceResponse> döner
- [x] GeneralProfile.cs'ye Place → PlaceResponse AutoMapper mapping ekle
- [x] Unit tests (18 test - Command, Query, Validator)

---

### Task 4.3: Infrastructure — Place Repository

**Tahmini Süre:** 1.5 saat
**Durum:** ✅ Tamamlandı

**Yapılacaklar:**
- [x] `PlaceRepositoryAsync.cs` — GenericRepositoryAsync<Place>'den türer, IPlaceRepositoryAsync implement eder
- [x] GetByCityAsync — is_active = true filtreli, case-insensitive city eşleşmesi, name'e göre sıralı, pagination uygulanmış
- [x] GetByCategoryAsync — is_active = true filtreli, category eşleşmesi, pagination uygulanmış
- [x] DI registration

---

### Task 4.4: WebApi — PlacesController

**Tahmini Süre:** 1 saat
**Durum:** ✅ Tamamlandı

**Yapılacaklar:**
- [x] `v1/PlacesController.cs` — BaseApiController'dan türer, [Authorize] attribute
- [x] GET /api/v1/places — query parametrelerinden filtre alır, GetAllPlacesQuery gönderir
- [x] GET /api/v1/places/{id} — Guid alır, GetPlaceByIdQuery gönderir
- [x] GET /api/v1/places/city/{city} — şehir adı + pagination, GetPlacesByCityQuery gönderir
- [x] POST /api/v1/places — [Authorize(Roles = "Admin")], CreatePlaceCommand body'den alır
- [x] Integration tests (13 test) — OmniFlow.Api.IntegrationTests/Controllers/PlacesControllerTests.cs

---

## 📅 Week 5: Trips — CRUD, Publish, Archive

**Hedef:** Trip CRUD, status state machine (Draft → Published → Archived), owner authorization, trip save/unsave

---

### Task 5.1: Application — Trip Repository Interface

**Tahmini Süre:** 1 saat  
**Durum:** ✅ Tamamlandı

**Yapılacaklar:**
- [x] `ITripRepositoryAsync.cs` — IGenericRepositoryAsync<Trip>'den türer, ek metotlar: GetByOwnerAsync (userId, pagination), GetPublishedByOwnerAsync (userId), GetWithStopsAsync (tripId, includes: Stops → Place), GetByIdWithOwnerAsync (tripId, Owner include)
- [x] `TripResponse.cs` DTO — tüm trip field'ları + owner bilgisi (Id, Username, ProfilePhotoUrl)
- [x] `CreateTripRequest.cs` DTO — Title, City, Country, StartDate, EndDate, PersonCount, BudgetTier, TravelStyle, UserBudget (nullable)

---

### Task 5.2: Application — Trip Commands

**Tahmini Süre:** 3 saat  
**Durum:** ✅ Tamamlandı

**Yapılacaklar:**
- [x] `CreateTripCommand.cs` — authenticated user'ın Id'sini owner olarak atar, status = Draft, handler repository'ye ekler
- [x] `CreateTripCommandValidator.cs` — Title boş olamaz (max 100), City/Country zorunlu, EndDate >= StartDate, PersonCount > 0, UserBudget >= 0 (nullable)
- [x] `UpdateTripCommand.cs` — TripId + güncellenebilir alanlar, handler'da owner kontrolü (authenticated user = trip owner, değilse ForbiddenException), sadece Draft trip güncellenebilir
- [x] `UpdateTripCommandValidator.cs` — CreateTrip ile aynı validasyon kuralları
- [x] `DeleteTripCommand.cs` — soft delete (deleted_at = now), owner kontrolü
- [x] `PublishTripCommand.cs` — status Draft → Published geçişi, owner kontrolü, en az 1 stop olmalı (yoksa ApiException)
- [x] `ArchiveTripCommand.cs` — status Published → Archived geçişi, owner kontrolü

---

### Task 5.3: Application — Trip Queries

**Tahmini Süre:** 1.5 saat  
**Durum:** ✅ Tamamlandı

**Yapılacaklar:**
- [x] `GetTripByIdQuery.cs` — Trip + Owner bilgisi döner, soft deleted trip için EntityNotFoundException
- [x] `GetMyTripsQuery.cs` — authenticated user'ın trip'leri, pagination, status filtresi (opsiyonel)
- [x] GeneralProfile.cs'ye Trip → TripResponse mapping ekle

---

### Task 5.4: Infrastructure — Trip Repository & Controller

**Tahmini Süre:** 2 saat  
**Durum:** ✅ Tamamlandı

**Yapılacaklar:**
- [x] `TripRepositoryAsync.cs` implementasyonu — soft delete filtresi (deleted_at IS NULL), owner bazlı sorgular, include ile Stop/Flight/Hotel yükleme, GetByIdWithOwnerAsync (Owner include)
- [x] `v1/TripsController.cs` oluştur:
  - [x] GET /api/v1/trips — GetMyTripsQuery (kendi trip'leri)
  - [x] GET /api/v1/trips/{id} — GetTripByIdQuery
  - [x] POST /api/v1/trips — CreateTripCommand
  - [x] PUT /api/v1/trips/{id} — UpdateTripCommand
  - [x] DELETE /api/v1/trips/{id} — DeleteTripCommand
  - [x] POST /api/v1/trips/{id}/publish — PublishTripCommand
  - [x] POST /api/v1/trips/{id}/archive — ArchiveTripCommand

---

### Task 5.5: Trip Save/Unsave & Upvote

**Tahmini Süre:** 1.5 saat  
**Durum:** ✅ Tamamlandı

**Yapılacaklar:**
- [x] `SaveTripCommand.cs` — IApplicationDbContext üzerinden SavedTrip ekler, çift kayıt varsa ignore
- [x] `UnsaveTripCommand.cs` — SavedTrip siler
- [x] `UpvoteTripCommand.cs` — IApplicationDbContext üzerinden TripUpvote ekler, çift upvote varsa DuplicateUpvoteException, trip'in upvote_count'ını 1 artırır
- [x] `RemoveUpvoteTripCommand.cs` — TripUpvote siler, upvote_count azaltır
- [x] `GetSavedTripsQuery.cs` — authenticated user'ın kaydettiği trip'ler, pagination
- [x] `SelfUpvoteException.cs` — Domain exception for self-upvote prevention
- [x] Controller endpoint'leri:
  - [x] POST /api/v1/trips/{id}/save
  - [x] DELETE /api/v1/trips/{id}/save
  - [x] POST /api/v1/trips/{id}/upvote
  - [x] DELETE /api/v1/trips/{id}/upvote
  - [x] GET /api/v1/saved-trips
- [x] Unit tests: UpvoteTrip, RemoveUpvoteTrip, SaveTrip, UnsaveTrip handlers
- [x] Integration tests: TripsControllerSaveUpvoteTests

---

## 📅 Week 6: Stops — CRUD, Reorder, Time Lock

**Hedef:** Stop CRUD, LexoRank drag-and-drop sıralama, time lock koruması, custom stop'lar, visited tracking

---

### Task 6.1: Application — Stop Repository & DTOs

**Tahmini Süre:** 1.5 saat
**Durum:** ✅ Tamamlandı

**Yapılacaklar:**
- [x] `IStopRepositoryAsync.cs` — GetByTripAsync, GetByTripAndDayAsync, GetByIdWithPlaceAsync, GetLastStopInDayAsync
- [x] `StopResponse.cs` DTO — tüm stop field'ları + place bilgisi + fallback place bilgisi
- [x] `CreateStopRequest.cs` DTO — tüm gerekli alanlar
- [x] `UpdateStopRequest.cs` DTO — güncellenebilir alanlar
- [x] `ReorderStopRequest.cs` DTO — StopId, NewDayNumber, AfterStopId, BeforeStopId (LexoRank)

---

### Task 6.2: Application — Stop Commands

**Tahmini Süre:** 3 saat
**Durum:** ✅ Tamamlandı

**Yapılacaklar:**
- [x] `CreateStopCommand.cs` + Handler + Validator — owner kontrolü, CHECK constraint'ler, OrderIndex otomatik hesaplama, AddedBy = User
- [x] `UpdateStopCommand.cs` + Handler + Validator — owner kontrolü, time lock koruması
- [x] `DeleteStopCommand.cs` + Handler — owner kontrolü, soft delete
- [x] `ReorderStopsCommand.cs` + Handler + Validator — LexoRank middle value hesaplama, time lock koruması
- [x] `MarkStopVisitedCommand.cs` + Handler — IsVisited + VisitedAt set

---

### Task 6.3: Application — Stop Queries & Controller

**Tahmini Süre:** 1.5 saat
**Durum:** ✅ Tamamlandı

**Yapılacaklar:**
- [x] `GetStopsByTripQuery.cs` + Handler — day_number + order_index sıralı, Place/FallbackPlace include, authorization
- [x] `StopsController.cs` endpoint'leri:
     - [x] GET /api/v1/trips/{tripId}/stops — GetStopsByTripQuery
     - [x] POST /api/v1/trips/{tripId}/stops — CreateStopCommand
     - [x] PUT /api/v1/trips/{tripId}/stops/{stopId} — UpdateStopCommand
     - [x] DELETE /api/v1/trips/{tripId}/stops/{stopId} — DeleteStopCommand
     - [x] PUT /api/v1/trips/{tripId}/stops/reorder — ReorderStopsCommand
     - [x] POST /api/v1/trips/{tripId}/stops/{stopId}/visited — MarkStopVisitedCommand

---

## 📅 Week 7: Fork System & Explore

**Hedef:** Fork (deep copy), self-fork engeli, Explore endpoint (popularity, filtreleme, pagination)

---

### Task 7.1: Application — Fork Command

**Tahmini Süre:** 2.5 saat
**Durum:** ✅ Tamamlandı

**Yapılacaklar:**
- [x] `ForkTripCommand.cs`:
  - [x] Orijinal trip'in published olduğunu doğrula
  - [x] Self-fork kontrolü: authenticated user = trip owner ise SelfForkException
  - [x] Yeni Trip entity oluştur: tüm field'ları kopyala, OwnerId = authenticated user, ForkedFromId = orijinal trip Id, Status = Draft, counter'lar sıfırla
  - [x] Orijinal trip'in tüm Stop'larını deep copy et (yeni Id'ler ile, aynı sıra ve ayarlar)
  - [x] Orijinal trip'in Flight ve Hotel seçimlerini kopyala (yeni Id'ler ile)
  - [x] Orijinal trip'in fork_count'ını 1 artır
  - [x] Karma tetikleme: placeholder olarak bırak (Phase 5'te KarmaService entegrasyonu)
  - [x] Yeni trip'in Id'sini dön

---

### Task 7.2: Application — Explore Query

**Tahmini Süre:** 2.5 saat
**Durum:** ✅ Tamamlandı

**Yapılacaklar:**
- [x] `ExploreTripsQuery.cs`:
  - [x] Sadece published ve soft deleted olmayan trip'ler
  - [x] Opsiyonel filtreler: city, country, budget_tier, travel_style, tags (herhangi biri eşleşirse)
  - [x] Sıralama: popularity_score DESC (default), created_at DESC (opsiyonel)
  - [x] Cursor-based pagination (offset değil, son trip'in popularity_score + id ile devam)
  - [x] Her trip'te owner bilgisi (Username, ProfilePhotoUrl) ve istatistikler (upvote, fork, view)
  - [x] Authenticated user'ın bu trip'i upvote/save edip etmediği bilgisi (isUpvoted, isSaved boolean)
- [x] `ExploreTripsParameter.cs` — City, BudgetTier, TravelStyle, Tags, SortBy, Cursor, PageSize
- [x] `ExploreTripsViewModel.cs` — TripResponse listesi + NextCursor

---

### Task 7.3: Explore & Fork Controller Endpoints

**Tahmini Süre:** 1 saat
**Durum:** ✅ Tamamlandı

**Yapılacaklar:**
- [x] GET /api/v1/explore — ExploreTripsQuery, query parametrelerinden filtreler
- [x] POST /api/v1/trips/{id}/fork — ForkTripCommand
- [x] Swagger'da her iki endpoint'i test et

---

## 🧪 Phase 2 Test Gereksinimleri

### Unit Tests

- [x] **CreateTripCommandValidator** — boş title reddedilmeli, EndDate < StartDate reddedilmeli, PersonCount = 0 reddedilmeli, geçerli komut kabul edilmeli
- [x] **CreateStopCommandValidator** — DayNumber = 0 reddedilmeli, IsTimeLocked = true + ArrivalTime = null reddedilmeli, negatif fiyat reddedilmeli
- [x] **CreatePlaceCommandValidator** — boş name reddedilmeli, rating = 6 reddedilmeli, geçersiz koordinat reddedilmeli
- [x] **ForkTripCommand** — self-fork SelfForkException fırlatmalı
- [x] **PublishTripCommand** — stop'u olmayan trip publish edilememeli
- [x] **ReorderStopsCommand** — time-locked stop'un sırası değiştirilemez

### Integration Tests

- [x] **Places_CRUD** — Create (admin) → GetById → GetByCity → her biri doğru sonuç
- [x] **Places_Create_NonAdmin_Returns403** — Traveler rolü ile create deneme → 403
- [x] **Trips_FullLifecycle** — Create → Update → AddStop → Publish → Archive, her adımda doğru status
- [x] **Trips_OwnerAuthorization** — Başka kullanıcının trip'ini update deneme → 403
- [x] **Stops_Reorder** — 3 stop oluştur → reorder → sıranın değiştiğini doğrula
- [x] **Stops_TimeLock** — time-locked stop reorder deneme → hata
- [x] **Fork_Success** — Published trip fork → yeni trip oluştu, stop'lar kopyalandı, fork_count arttı
- [x] **Fork_SelfFork_Returns409** — Kendi trip'ini fork deneme → 409
- [x] **Fork_DraftTrip_Returns400** — Draft trip fork deneme → hata
- [x] **Explore_Filters** — city + budget_tier filtresi ile doğru sonuçlar
- [x] **Explore_Pagination** — cursor-based pagination ile ardışık sayfalar farklı trip'ler dönmeli
- [x] **TripUpvote_Success** — upvote → upvote_count 1 arttı
- [x] **TripUpvote_Duplicate_Returns409** — aynı trip'e ikinci upvote → 409
- [x] **SaveTrip_Success** — save → saved-trips'te görünüyor, unsave → görünmüyor

---

## ✅ Phase 2 Success Metrics

- [x] Places CRUD + city/category filtreleme çalışıyor
- [x] Trip lifecycle: Draft → Published → Archived, her geçişte doğru validasyon
- [x] Owner authorization: başka kullanıcının trip/stop'una erişim engelleniyor
- [x] Stop reorder LexoRank ile çalışıyor, time-lock korunuyor
- [x] Custom stop (place_id NULL) oluşturulabiliyor
- [x] Fork: deep copy + self-fork engeli + fork_count artışı
- [x] Explore: filter + sort + cursor pagination çalışıyor
- [x] Upvote + Save: çalışıyor, duplicate engeli var
- [x] Phase 2 testlerinin tamamı geçiyor

---

## 🎯 Phase 3 Overview

### Scope

**Dahil:**
- Flights entity, trip bazlı listeleme, uçuş seçimi, gidiş-dönüş gruplama (itinerary_group_id)
- Hotels entity, trip bazlı listeleme, otel seçimi
- Mock data source desteği (data_source = 'mock')
- Booking consistency kuralları (is_booked + booked_at senkron)

**Hariç:**
- Amadeus API entegrasyonu (bitirme projesi)
- Gerçek ödeme/rezervasyon sistemi

---

## 📅 Week 8: Flights — Entity, Selection, Queries

**Hedef:** Flight repository, trip bazlı listeleme, uçuş seçimi, gidiş-dönüş gruplama

---

### Task 8.1: Application — Flight Feature

**Tahmini Süre:** 2.5 saat  
**Durum:** ✅ Tamamlandı

**Yapılacaklar:**
- [x] `IFlightRepositoryAsync.cs` — GetByTripAsync (tripId, direction filtresi opsiyonel), GetByGroupAsync (itineraryGroupId)
- [x] `FlightResponse.cs` DTO — tüm flight field'ları, formatlı departure/arrival bilgisi
- [x] `SelectFlightRequest.cs` DTO — FlightId
- [x] `SelectFlightCommand.cs` — trip owner kontrolü, seçilen flight'ın bu trip'e ait olduğunu doğrula, is_booked = true + booked_at = now set et, aynı direction'daki önceki seçimi iptal et (is_booked = false)
- [x] `GetFlightsByTripQuery.cs` — trip'in tüm flight seçenekleri, direction bazlı gruplama

---

### Task 8.2: WebApi — FlightsController

**Tahmini Süre:** 1 saat  
**Durum:** ✅ Tamamlandı

**Yapılacaklar:**
- [x] `v1/FlightsController.cs`:
  - [x] GET /api/v1/trips/{tripId}/flights — GetFlightsByTripQuery
  - [x] POST /api/v1/trips/{tripId}/flights/select — SelectFlightCommand
- [x] Swagger'da test et

---

## 📅 Week 9: Hotels — Entity, Selection, Queries

**Hedef:** Hotel repository, trip bazlı listeleme, otel seçimi

---

### Task 9.1: Application — Hotel Feature

**Tahmini Süre:** 2.5 saat  
**Durum:** ✅ Tamamlandı

**Yapılacaklar:**
- [x] `IHotelRepositoryAsync.cs` — GetByTripAsync (tripId, check-in sıralı)
- [x] `HotelResponse.cs` DTO — tüm hotel field'ları, formatlı check-in/out, gece sayısı hesaplı
- [x] `SelectHotelRequest.cs` DTO — HotelId
- [x] `SelectHotelCommand.cs` — trip owner kontrolü, seçilen hotel'in bu trip'e ait olduğunu doğrula, is_booked = true + booked_at = now set et, önceki seçimi iptal et
- [x] `GetHotelsByTripQuery.cs` — trip'in tüm otel seçenekleri, check-in sıralı

---

### Task 9.2: WebApi — HotelsController

**Tahmini Süre:** 1 saat  
**Durum:** ✅ Tamamlandı

**Yapılacaklar:**
- [x] `v1/HotelsController.cs`:
  - [x] GET /api/v1/trips/{tripId}/hotels — GetHotelsByTripQuery
  - [x] POST /api/v1/trips/{tripId}/hotels/select — SelectHotelCommand
- [x] Swagger'da test et

---

## 🧪 Phase 3 Test Gereksinimleri

### Unit Tests

- [x] **SelectFlightCommand** — başka kullanıcının trip'indeki flight seçme → ForbiddenException
- [x] **SelectFlightCommand** — trip'e ait olmayan flight seçme → EntityNotFoundException
- [x] **SelectHotelCommand** — aynı validasyonlar

### Integration Tests

- [x] **Flights_GetByTrip** — trip'e ait flight'lar dönmeli, başka trip'in flight'ları dönmemeli
- [x] **Flights_Select** — select → is_booked = true, önceki seçim iptal oldu
- [x] **Hotels_GetByTrip** — check-in sıralı listeleme
- [x] **Hotels_Select** — select → is_booked = true, booking_reference opsiyonel
- [x] **BookingConsistency** — is_booked = true olan kayıtta booked_at dolu olmalı

---

## ✅ Phase 3 Success Metrics

- [x] Flight seçimi çalışıyor, gidiş-dönüş itinerary gruplama var
- [x] Hotel seçimi çalışıyor, check-in/out validation doğru
- [x] Booking consistency: is_booked ve booked_at her zaman senkron
- [x] data_source = 'mock' ile seeded veriler sorgulanabiliyor
- [x] Phase 3 testlerinin tamamı geçiyor

---

## 🎯 Phase 4 Overview

### Scope

**Dahil:**
- Posts CRUD (photo/tip/route tipleri), upvote, soft delete, is_visible (moderasyon)
- Comments CRUD, 1-level reply, composite FK ile cross-post koruması, upvote, mention'lar
- Feed endpoint: For You / Following / Latest tab'ları, infinite scroll cursor pagination
- Follow/Unfollow sistemi, followers/following listeleme, self-follow engeli, counter cache
- User profile görüntüleme, profil güncelleme

**Hariç:**
- AI tagging (bitirme projesi)
- Reverse geocoding (bitirme projesi)
- Push notification (Phase 5)
- UserBlock / kullanıcı engelleme (bitirme projesi)

### Definition of Done

Phase 4 tamamlanmış sayılır eğer:
- [x] 3 tip post oluşturulabiliyor, route tipi trip gerektiriyor
- [x] Comment + reply (1 seviye) çalışıyor, cross-post koruması aktif
- [x] Feed 3 tab ile cursor pagination çalışıyor
- [x] Follow/unfollow counter cache doğru güncelleniyor
- [x] User profile + update çalışıyor
- [x] Phase 4 testleri geçiyor

---

## 📅 Week 10: Posts — CRUD, Types, Upvote

**Hedef:** Post oluşturma (3 tip), güncelleme, silme, upvote, post detay

---

### Task 10.1: Application — Post Feature

**Tahmini Süre:** 3 saat  
**Durum:** ✅ Tamamlandı

**Yapılacaklar:**
- [x] `IPostRepositoryAsync.cs` — GetByIdWithUserAsync, GetByUserAsync (userId, pagination), GetVisibleAsync (soft delete + is_visible filtreli)
- [x] `PostResponse.cs` DTO — tüm post field'ları + user bilgisi (Username, ProfilePhotoUrl, KarmaScore) + isUpvoted (authenticated user)
- [x] `CreatePostCommand.cs` — UserId authenticated user'dan alınır, PostType = Route ise TripId zorunlu (route_requires_trip), Content veya Photos'dan en az biri dolu olmalı (content_or_photo), Tags opsiyonel
- [x] `CreatePostCommandValidator.cs` — PostType enum'da olmalı, content ve photos ikisi de boşsa hata
- [x] `UpdatePostCommand.cs` — post owner kontrolü, Content ve Tags güncellenebilir
- [x] `DeletePostCommand.cs` — soft delete (deleted_at = now), post owner kontrolü
- [x] `UpvotePostCommand.cs` — IApplicationDbContext üzerinden PostUpvote ekle, duplicate → DuplicateUpvoteException, post upvote_count 1 artır
- [x] `GetPostByIdQuery.cs` — post + user bilgisi, soft deleted → EntityNotFoundException

**Ekstra Test Kapsamı (Task 10.1):**
- [x] `CreatePostCommandValidatorTests` — valid post geçer, content+photos boşsa hata, route post + TripId null ise hata
- [x] `CreatePostCommandHandlerTests` — authenticated user ID post'a set edilir, oluşturulan post ID döner
- [x] `UpdatePostCommandHandlerTests` — post bulunamazsa 404 exception, owner değilse 403 exception, owner ise update çalışır
- [x] `DeletePostCommandHandlerTests` — post bulunamazsa 404 exception, owner değilse 403 exception, owner ise soft-delete çağrılır
- [x] `UpvotePostCommandHandlerTests` — post bulunamazsa 404 exception, duplicate upvote 409 exception, başarılı upvote count artırır
- [x] `GetPostByIdQueryHandlerTests` — post bulunamazsa 404 exception, başarılı durumda `isUpvoted` doğru hesaplanır

---

### Task 10.2: WebApi — PostsController

**Tahmini Süre:** 1 saat  
**Durum:** ✅ Tamamlandı

**Yapılacaklar:**
- [x] `v1/PostsController.cs`:
  - [x] GET /api/v1/posts/{id} — GetPostByIdQuery
  - [x] POST /api/v1/posts — CreatePostCommand
  - [x] PUT /api/v1/posts/{id} — UpdatePostCommand
  - [x] DELETE /api/v1/posts/{id} — DeletePostCommand
  - [x] POST /api/v1/posts/{id}/upvote — UpvotePostCommand

**Ekstra Test Kapsamı:**
- [x] `PostsControllerTests.cs` — 11 integration test: auth guard, create, get by id, update, delete, upvote, not found senaryoları

---

## 📅 Week 11: Comments — CRUD, Reply, Upvote

**Hedef:** Comment oluşturma, reply (1 seviye), cross-post koruması, upvote, mention

---

### Task 11.1: Application — Comment Feature

**Tahmini Süre:** 3 saat  
**Durum:** ✅ Tamamlandı

**Yapılacaklar:**
- [x] `ICommentRepositoryAsync.cs` — GetByPostAsync (postId, created_at sıralı, replies include), GetByIdWithRepliesAsync
- [x] `CommentResponse.cs` DTO — comment field'ları + user bilgisi + replies listesi (1 seviye) + isUpvoted
- [x] `CreateCommentCommand.cs` — PostId zorunlu, ParentCommentId opsiyonel (reply), Content zorunlu, Mentions opsiyonel. Handler'da: post var mı kontrolü, ParentCommentId varsa parent'ın aynı post'a ait olduğunu doğrula (cross-post koruması), post comment_count 1 artır
- [x] `CreateCommentCommandValidator.cs` — Content boş olamaz
- [x] `DeleteCommentCommand.cs` — soft delete, comment owner kontrolü, post comment_count 1 azalt
- [x] `UpvoteCommentCommand.cs` — duplicate engeli, comment upvote_count artır
- [x] `GetCommentsByPostQuery.cs` — post'un tüm comment'leri (soft deleted ve invisible hariç), reply'ler include, pagination

**Ekstra Test Kapsamı:**
- [x] `CreateCommentCommandTests.cs` — validator + create handler testleri
- [x] `DeleteCommentCommandTests.cs` — not found, forbidden, successful delete
- [x] `UpvoteCommentCommandTests.cs` — not found, duplicate, successful upvote
- [x] `GetCommentsByPostQueryTests.cs` — post not found, paged response + reply tree + upvote flag

---

### Task 11.2: WebApi — CommentsController

**Tahmini Süre:** 1 saat  
**Durum:** ✅ Tamamlandı

**Yapılacaklar:**
- [x] `v1/CommentsController.cs`:
  - [x] GET /api/v1/posts/{postId}/comments — GetCommentsByPostQuery
  - [x] POST /api/v1/posts/{postId}/comments — CreateCommentCommand
  - [x] DELETE /api/v1/comments/{id} — DeleteCommentCommand
  - [x] POST /api/v1/comments/{id}/upvote — UpvoteCommentCommand

**Ekstra Test Kapsamı:**
- [x] `CommentsControllerTests.cs` — 8 integration test: auth guard, create, get by post, delete, upvote, not found olmayan akışlar

---

## 📅 Week 12: Feed — Pagination, Tabs, Infinite Scroll

**Hedef:** Feed endpoint (3 tab), cursor-based infinite scroll

---

### Task 12.1: Application — Feed Query

**Tahmini Süre:** 3 saat  
**Durum:** ✅ Tamamlandı

**Yapılacaklar:**
- [x] `GetFeedQuery.cs` — tab parametresi (ForYou / Following / Latest):
  - [x] **Latest**: tüm visible + non-deleted post'lar, created_at DESC, cursor pagination
  - [x] **Following**: authenticated user'ın takip ettiği kullanıcıların post'ları, created_at DESC
  - [x] **ForYou**: başlangıçta Latest ile aynı (ileride recommendation algoritması eklenebilir)
  - [x] Her post'ta: user bilgisi, isUpvoted, comment_count
- [x] `GetFeedParameter.cs` — Tab (enum: ForYou, Following, Latest), Cursor (nullable string), PageSize (default 20)
- [x] `GetFeedViewModel.cs` — PostResponse listesi + NextCursor + HasMore boolean

**Ekstra Test Kapsamı:**
- [x] `GetFeedQueryTests.cs` — latest order/filter, following tab, cursor pagination, invalid cursor, isUpvoted flag

---

### Task 12.2: WebApi — Feed Endpoint

**Tahmini Süre:** 30 dakika  
**Durum:** ✅ Tamamlandı

**Yapılacaklar:**
- [x] GET /api/v1/feed — query parametrelerinden tab, cursor, pageSize alır, GetFeedQuery gönderir
- [x] Swagger'da 3 tab ile test et

**Ekstra Test Kapsamı:**
- [x] `FeedControllerTests.cs` — auth guard, Latest tab, Following tab, query binding ve feed response doğrulama

---

## 📅 Week 13: Follow System & User Profiles

**Hedef:** Follow/unfollow, followers/following listeleme, user profile, update profile

---

### Task 13.1: Application — Follow Feature

**Tahmini Süre:** 2 saat  
**Durum:** ✅ Tamamlandı

**Yapılacaklar:**
- [x] `IFollowRepositoryAsync.cs` — GetFollowersAsync (userId, pagination), GetFollowingAsync (userId, pagination), IsFollowingAsync (followerId, followingId)
- [x] `FollowUserCommand.cs` — self-follow kontrolü (SelfFollowException), zaten takip ediyorsa ignore, following user'ın followers_count ve follower user'ın following_count'ını 1 artır
- [x] `UnfollowUserCommand.cs` — takip ilişkisi yoksa ignore, counter'ları 1 azalt
- [x] `GetFollowersQuery.cs` — userId'nin follower'ları, pagination, her kullanıcıda isFollowing bilgisi
- [x] `GetFollowingQuery.cs` — userId'nin takip ettikleri, pagination

**Ekstra Test Kapsamı:**
- [x] `FollowUserCommandTests.cs` — self-follow, duplicate follow ignore, valid follow counter update
- [x] `UnfollowUserCommandTests.cs` — missing follow ignore, valid unfollow counter update
- [x] `GetFollowersQueryTests.cs` — missing user error, followers list + isFollowing flag
- [x] `GetFollowingQueryTests.cs` — missing user error, following list + isFollowing flag

---

### Task 13.2: Application — User Profile Feature

**Tahmini Süre:** 2 saat  
**Durum:** ✅ Tamamlandı

**Yapılacaklar:**
- [x] `IUserRepositoryAsync.cs` — GetByIdAsync, GetByUsernameAsync, UpdateAsync
- [x] `UserProfileResponse.cs` DTO — Id, Username, Email, Bio, ProfilePhotoUrl, KarmaScore, FollowersCount, FollowingCount, IsVerified, isFollowing (authenticated user), tripCount, postCount
- [x] `GetUserProfileQuery.cs` — username veya id ile kullanıcı profili
- [x] `UpdateProfileCommand.cs` — sadece kendi profilini güncelleyebilir, Bio (max 300), ProfilePhotoUrl güncellenebilir
- [x] `UpdateProfileCommandValidator.cs` — Bio max 300 karakter

**Ekstra Test Kapsamı:**
- [x] `GetUserProfileQueryHandlerTests.cs` — id/username lookup, profile counts, isFollowing flag, not found
- [x] `UpdateProfileCommandHandlerTests.cs` — not found, successful profile update
- [x] `UpdateProfileCommandValidatorTests.cs` — valid update, bio length validation

---

### Task 13.3: WebApi — Follows & Users Controllers

**Tahmini Süre:** 1.5 saat  
**Durum:** ✅ Tamamlandı

**Yapılacaklar:**
- [x] `v1/FollowsController.cs`:
  - [x] POST /api/v1/users/{userId}/follow — FollowUserCommand
  - [x] DELETE /api/v1/users/{userId}/follow — UnfollowUserCommand
  - [x] GET /api/v1/users/{userId}/followers — GetFollowersQuery
  - [x] GET /api/v1/users/{userId}/following — GetFollowingQuery
- [x] `v1/UsersController.cs`:
  - [x] GET /api/v1/users/{username} — GetUserProfileQuery
  - [x] GET /api/v1/users/me — GetUserProfileQuery (authenticated user)
  - [x] PUT /api/v1/users/me — UpdateProfileCommand

**Ekstra Test Kapsamı:**
- [x] `FollowsControllerTests.cs` — auth guard, follow/unfollow, followers/following response ve counter updates
- [x] `UsersControllerTests.cs` — auth guard, get by username, get me, update me ve persisted profile changes

---

## 🧪 Phase 4 Test Gereksinimleri

### Unit Tests

- [x] **CreatePostCommandValidator** — content ve photos ikisi de boş → hata, PostType = Route + TripId = null → hata
- [x] **CreateCommentCommandValidator** — boş content → hata
- [x] **FollowUserCommand** — self-follow → SelfFollowException
- [x] **UpdateProfileCommandValidator** — Bio 301 karakter → hata

### Integration Tests

- [x] **Posts_CRUD** — Create (photo tip) → GetById → Update → Delete (soft), her adım doğru
- [x] **Posts_RouteRequiresTrip** — PostType = Route, TripId = null → 422
- [x] **Posts_Upvote** — upvote → count arttı, duplicate → 409
- [x] **Comments_Reply** — post'a comment → comment'e reply → doğru parent ilişkisi
- [x] **Comments_CrossPostProtection** — A post'unun comment'ine B post'undan reply → hata
- [x] **Feed_Latest** — 5 post oluştur → feed latest tab → 5 post created_at DESC sıralı
- [x] **Feed_Following** — A, B'yi takip ediyor, C'yi takip etmiyor → following tab'da sadece B'nin post'ları
- [x] **Follow_Success** — follow → followers_count arttı, following_count arttı
- [x] **Follow_SelfFollow_Returns409** — kendini takip → 409
- [x] **Follow_Unfollow** — unfollow → counter'lar azaldı
- [x] **UserProfile_GetByUsername** — profil bilgileri doğru, isFollowing bilgisi doğru
- [x] **UserProfile_Update** — bio güncelle → yeni bio doğru

---

## ✅ Phase 4 Success Metrics

- [x] 3 tip post oluşturulabiliyor, route tipi trip gerektiriyor
- [x] Comment + reply (1 seviye) çalışıyor, cross-post koruması aktif
- [x] Feed 3 tab ile cursor pagination çalışıyor
- [x] Follow/unfollow counter cache doğru güncelleniyor
- [x] User profile + update çalışıyor
- [x] Phase 4 testlerinin tamamı geçiyor

---

## 🎯 Phase 5 Overview

### Scope

**Dahil:**
- Community Tips CRUD — rotanın geneline veya spesifik bir mekana tip, upvote
- Karma System — event oluşturma, puan hesaplama, farming koruması (unique index), cache güncelleme
- KarmaService (Application katmanı) — publish +10, fork +5, tip upvote +2, post upvote +1
- Notifications — polymorphic target yapısı, bildirim oluşturma, mark read, unread count
- NotificationService (Application katmanı) — follow, upvote, comment, mention, fork event'lerinde bildirim tetikleme
- Önceki Phase'lerdeki command handler'lara KarmaService ve NotificationService entegrasyonu

---

## 📅 Week 14: Community Tips — CRUD, Upvote

**Hedef:** Rota geneline veya mekana özel tip oluşturma, listeleme, upvote

---

### Task 14.1: Application — CommunityTip Feature

**Tahmini Süre:** 2.5 saat  
**Durum:** ✅ Tamamlandı

**Yapılacaklar:**
- [x] `ICommunityTipRepositoryAsync.cs` — GetByTripAsync (tripId, upvote_count DESC sıralı, pagination), GetByPlaceInTripAsync (tripId, placeId)
- [x] `TipResponse.cs` DTO — tip field'ları + user bilgisi + isUpvoted
- [x] `CreateTipCommand.cs` — TripId zorunlu, PlaceId opsiyonel (null = rotanın geneli, dolu = mekana özel), Content zorunlu (trim boş olamaz)
- [x] `CreateTipCommandValidator.cs` — Content boş olamaz
- [x] `DeleteTipCommand.cs` — soft delete, tip owner kontrolü
- [x] `UpvoteTipCommand.cs` — duplicate engeli, tip upvote_count artır
- [x] `GetTipsByTripQuery.cs` — trip'in tüm tip'leri, place bilgisi include (varsa), upvote_count DESC sıralı

**Ekstra Testler:**
- `Tests/OmniFlow.UnitTests/CommunityTips/CreateTipCommandValidatorTests.cs`
- `Tests/OmniFlow.UnitTests/CommunityTips/CreateTipCommandHandlerTests.cs`
- `Tests/OmniFlow.UnitTests/CommunityTips/DeleteTipCommandHandlerTests.cs`
- `Tests/OmniFlow.UnitTests/CommunityTips/UpvoteTipCommandHandlerTests.cs`
- `Tests/OmniFlow.UnitTests/CommunityTips/GetTipsByTripQueryHandlerTests.cs`

---

### Task 14.2: WebApi — CommunityTipsController

**Tahmini Süre:** 1 saat  
**Durum:** ✅ Tamamlandı

**Yapılacaklar:**
- [x] `v1/CommunityTipsController.cs`:
  - [x] GET /api/v1/trips/{tripId}/tips — GetTipsByTripQuery
  - [x] POST /api/v1/trips/{tripId}/tips — CreateTipCommand
  - [x] DELETE /api/v1/tips/{id} — DeleteTipCommand
  - [x] POST /api/v1/tips/{id}/upvote — UpvoteTipCommand

**Ekstra Test Kapsamı:**
- [x] `CommunityTipsControllerTests.cs` — auth guard, create, place-specific get, upvote, delete, tip list response

---

## 📅 Week 15: Karma System — Events, Scoring, Farming Protection

**Hedef:** KarmaService implementasyonu, karma event'leri, puan hesaplama, farming koruması, cache güncelleme

---

### Task 15.1: Application — KarmaService

**Tahmini Süre:** 3 saat  
**Durum:** ✅ Tamamlandı

**Yapılacaklar:**
- [x] `Application/Interfaces/IKarmaService.cs` — AwardKarmaAsync(userId, actorId, eventType, points, sourceId, sourceType)
- [x] `Application/Services/KarmaService.cs` implementasyonu:
  - [x] IApplicationDbContext inject edildi
  - [x] AwardKarmaAsync: KarmaEvent entity oluşturup DB'ye ekliyor
  - [x] Farming koruması: aynı (userId, sourceId, eventType, actorId) kombinasyonu varsa sessizce skip ediyor
  - [x] users tablosundaki karma_score cache'ini güncelliyor (+ points)
  - [x] Puan tablosu: TripPublished = +10, TripForked = +5, TipUpvoted = +2, PostUpvoted = +1, TripUpvoted = +1

**Ekstra Test Kapsamı:**
- [x] `Tests/OmniFlow.UnitTests/Karma/KarmaServiceTests.cs` — valid award, duplicate skip, user not found

---

### Task 15.2: Karma Entegrasyonu — Önceki Handler'lar

**Tahmini Süre:** 2 saat  
**Durum:** ✅ Tamamlandı

**Yapılacaklar:**
- [x] `PublishTripCommand` handler'ına eklendi: KarmaService.AwardKarmaAsync (trip owner'a +10, source = trip)
- [x] `ForkTripCommand` handler'ına eklendi: KarmaService.AwardKarmaAsync (orijinal trip owner'a +5, actor = fork yapan, source = trip)
- [x] `UpvotePostCommand` handler'ına eklendi: KarmaService.AwardKarmaAsync (post owner'a +1, actor = upvote yapan, source = post)
- [x] `UpvoteTipCommand` handler'ına eklendi: KarmaService.AwardKarmaAsync (tip owner'a +2, actor = upvote yapan, source = tip)
- [x] `UpvoteTripCommand` handler'ına eklendi: KarmaService.AwardKarmaAsync (trip owner'a +1, actor = upvote yapan, source = trip)

**Ekstra Test Kapsamı:**
- [x] `PublishTripCommandTests.cs` — publish sonrası `TripPublished (+10)` karma çağrısı doğrulandı
- [x] `ForkTripCommandTests.cs` — fork sonrası orijinal owner için `TripForked (+5)` karma çağrısı doğrulandı
- [x] `UpvotePostCommandTests.cs` — upvote sonrası post owner için `PostUpvoted (+1)` karma çağrısı doğrulandı
- [x] `UpvoteTipCommandHandlerTests.cs` — upvote sonrası tip owner için `TipUpvoted (+2)` karma çağrısı doğrulandı
- [x] `UpvoteTripCommandHandlerTests.cs` — upvote sonrası trip owner için `TripUpvoted (+1)` karma çağrısı doğrulandı

---

### Task 15.3: Application — Karma Query & Controller

**Tahmini Süre:** 1 saat  
**Durum:** ✅ Tamamlandı

**Yapılacaklar:**
- [x] `GetKarmaHistoryQuery.cs` — authenticated user'ın karma event geçmişi, created_at DESC, pagination
- [x] `KarmaEventResponse.cs` DTO — EventType, Points, SourceType, CreatedAt, ActorUsername (nullable)
- [x] `v1/KarmaController.cs`:
  - [x] GET /api/v1/karma/history — GetKarmaHistoryQuery

**Ekstra Test Kapsamı:**
- [x] `Tests/OmniFlow.UnitTests/Karma/GetKarmaHistoryQueryHandlerTests.cs` — authenticated user filtreleme, created_at DESC + pagination, ActorUsername (nullable) mapping

---

## 📅 Week 16: Notifications — Polymorphic, Mark Read, Unread Count

**Hedef:** NotificationService, bildirim oluşturma, listeleme, okundu işaretleme, okunmamış sayısı

---

### Task 16.1: Application — NotificationService

**Tahmini Süre:** 3 saat  
**Durum:** ✅ Tamamlandı

**Yapılacaklar:**
- [x] `Application/Interfaces/INotificationService.cs` — CreateNotificationAsync(userId, actorId, type, targetId, targetType)
- [x] `Application/Services/NotificationService.cs` implementasyonu:
  - [x] IApplicationDbContext inject et
  - [x] CreateNotificationAsync: Notification entity oluştur, DB'ye ekle
  - [x] Follow bildirimi: target null (follow_has_no_target kuralı)
  - [x] Upvote bildirimleri: target = ilgili içerik (post, comment, tip, trip)
  - [x] Comment bildirimi: target = post
  - [x] Mention bildirimi: target = post veya comment
  - [x] Fork bildirimi: target = trip
  - [x] Self-notification engeli: actor = user ise bildirim oluşturma

**Ekstra Test Kapsamı:**
- [x] `Tests/OmniFlow.UnitTests/Notifications/NotificationServiceTests.cs` — self-notification skip, follow type target null normalization

---

### Task 16.2: Notification Entegrasyonu — Önceki Handler'lar

**Tahmini Süre:** 2 saat  
**Durum:** ✅ Tamamlandı

**Yapılacaklar:**
- [x] `FollowUserCommand` handler'ına ekle: NotificationService → follow bildirimi
- [x] `UpvotePostCommand` handler'ına ekle: NotificationService → post_upvote bildirimi (self değilse)
- [x] `UpvoteCommentCommand` handler'ına ekle: NotificationService → comment_upvote bildirimi
- [x] `UpvoteTipCommand` handler'ına ekle: NotificationService → tip_upvote bildirimi
- [x] `UpvoteTripCommand` handler'ına ekle: NotificationService → trip_upvote bildirimi
- [x] `CreateCommentCommand` handler'ına ekle: NotificationService → comment bildirimi (post owner'a) + mention bildirim'leri (mentions listesindeki user'lara)
- [x] `ForkTripCommand` handler'ına ekle: NotificationService → fork bildirimi

---

### Task 16.3: Application — Notification Queries & Commands

**Tahmini Süre:** 2 saat  
**Durum:** ✅ Tamamlandı

**Yapılacaklar:**
- [x] `INotificationRepositoryAsync.cs` — GetByUserAsync (userId, is_read filtresi opsiyonel, created_at DESC, pagination), GetUnreadCountAsync (userId)
- [x] `NotificationResponse.cs` DTO — Id, Type, TargetId, TargetType, IsRead, ReadAt, CreatedAt, Actor bilgisi (Username, ProfilePhotoUrl)
- [x] `GetNotificationsQuery.cs` — authenticated user'ın bildirimleri, pagination
- [x] `GetUnreadCountQuery.cs` — authenticated user'ın okunmamış bildirim sayısı
- [x] `MarkAsReadCommand.cs` — notification owner kontrolü, is_read = true + read_at = now
- [x] `MarkAllAsReadCommand.cs` — authenticated user'ın tüm okunmamış bildirimlerini okundu yap
- [x] `v1/NotificationsController.cs`:
  - [x] GET /api/v1/notifications — GetNotificationsQuery
  - [x] GET /api/v1/notifications/unread-count — GetUnreadCountQuery
  - [x] POST /api/v1/notifications/{id}/read — MarkAsReadCommand
  - [x] POST /api/v1/notifications/read-all — MarkAllAsReadCommand

**Ekstra Test Kapsamı (Task 16.3):**
- [x] `Tests/OmniFlow.UnitTests/Notifications/NotificationFeatureTests.cs` — GetNotifications, GetUnreadCount, MarkAsRead, MarkAllAsRead handler testleri
- [x] `Tests/OmniFlow.Api.IntegrationTests/Controllers/NotificationsControllerTests.cs` — follow/upvote/self-upvote/comment/mention + mark-read + unread-count uçtan uca senaryolar
- [x] `Tests/OmniFlow.Api.IntegrationTests/Controllers/KarmaIntegrationTests.cs` — publish/fork/farming/duplicate-upvote karma event ve score doğrulamaları

---

## 🧪 Phase 5 Test Gereksinimleri

### Unit Tests

- [x] **CreateTipCommandValidator** — boş content → hata
- [x] **KarmaService** — AwardKarmaAsync doğru event oluşturuyor, puan tablosu doğru, farming koruması çift puan engelliyor
- [x] **NotificationService** — self-notification oluşturmuyor, follow bildirimi target null
- [x] **Notification Handler Integrations** — Follow/Upvote/Comment/Mention/Fork handler'ları doğru notification type/target ile NotificationService çağırıyor

### Integration Tests

- [x] **Tips_CRUD** — tip oluştur → trip'in tip listesinde görünüyor → sil → görünmüyor
- [x] **Tips_PlaceSpecific** — place_id ile tip → GetTipsByTrip'te place bilgisi dolu
- [x] **Karma_PublishTrip** — trip publish → karma_events'te +10, user karma_score güncellendi
- [x] **Karma_ForkTrip** — fork → orijinal owner'a +5
- [x] **Karma_Farming** — aynı trip'i publish-unpublish-publish → sadece 1 kez +10
- [x] **Karma_DuplicateUpvote** — aynı post'a 2 kez upvote → sadece 1 kez +1 karma
- [x] **Notifications_Follow** — A, B'yi takip etti → B'nin bildirimlerinde "A seni takip etti"
- [x] **Notifications_Upvote** — A, B'nin post'unu upvote etti → B'ye bildirim
- [x] **Notifications_SelfUpvote** — A, kendi post'unu upvote etti → bildirim oluşmadı
- [x] **Notifications_Comment** — A, B'nin post'una yorum yaptı → B'ye bildirim
- [x] **Notifications_Mention** — A, C'yi mention etti → C'ye bildirim
- [x] **Notifications_MarkRead** — bildirim okundu → is_read = true, read_at dolu
- [x] **Notifications_UnreadCount** — 3 okunmamış → count = 3, 1 okundu → count = 2

---

## ✅ Phase 5 Success Metrics

- [x] Community tip: rota geneli + mekana özel oluşturma, upvote, listeleme çalışıyor
- [x] Karma: publish +10, fork +5, upvote +1/+2 doğru hesaplanıyor
- [x] Farming koruması: unique index + uygulama kontrolü çift puan engelliyor
- [x] Bildirim: 8 tip (follow, post_upvote, comment_upvote, tip_upvote, trip_upvote, comment, mention, fork) oluşuyor
- [x] Self-notification engelleniyor
- [x] Unread count doğru, mark read çalışıyor
- [x] Phase 5 testlerinin tamamı geçiyor

---

## 🎯 Phase 6 Overview

### Scope

**Dahil:**
- GenerateTimeline command — Trip bilgisi + filtrelenmiş place'ler → OpenAI GPT-4o → günlük stop listesi
- AI Fallback — OpenAI timeout/hata durumunda kural tabanlı motor devreye girer
- IAiTimelineService + IAiFallbackService (Application interface'leri, Infrastructure implementasyonları)
- Place filtreleme: trip'in city, budget_tier, travel_style ile eşleşen place'ler DB'den çekilir, LLM'e context olarak gönderilir
- LLM çıktısı Stop entity'lerine dönüştürülür (added_by = 'ai', ai_reasoning zorunlu)
- Fallback: popularity score sıralı place'ler → sabit şablon timeline

---

## 📅 Week 17: AI Timeline Generation

**Hedef:** OpenAI GPT-4o ile günlük timeline oluşturma, prompt tasarımı, response parsing

---

### Task 17.1: Application — AI Interfaces & GenerateTimeline Command

**Tahmini Süre:** 2 saat  
**Durum:** ⏳ Bekliyor

**Yapılacaklar:**
- [ ] `IAiTimelineService.cs` finalize — GenerateTimelineAsync(trip bilgisi, place listesi) → Stop listesi döner
- [ ] `IAiFallbackService.cs` finalize — GenerateFallbackTimelineAsync(trip bilgisi, place listesi) → Stop listesi döner
- [ ] `GenerateTimelineCommand.cs`:
  - [ ] Trip owner kontrolü
  - [ ] Trip'in city, budget_tier, travel_style bilgilerini al
  - [ ] Places tablosundan bu city'deki, bu budget_tier ve travel_style ile eşleşen place'leri çek (20-30 adet)
  - [ ] IAiTimelineService.GenerateTimelineAsync çağır
  - [ ] Başarısızsa (timeout, exception) → IAiFallbackService.GenerateFallbackTimelineAsync çağır
  - [ ] Dönen stop listesini DB'ye kaydet (added_by = 'ai', ai_reasoning dolu)
  - [ ] Trip'in mevcut AI-generated stop'larını sil (önceki timeline varsa temizle, user eklediği stop'lara dokunma)
- [ ] Controller endpoint: POST /api/v1/trips/{tripId}/generate-timeline

---

### Task 17.2: Infrastructure — AiTimelineService

**Tahmini Süre:** 3 saat  
**Durum:** ⏳ Bekliyor

**Yapılacaklar:**
- [ ] `AiTimelineService.cs` implementasyonu:
  - [ ] OpenAI API client (HttpClient veya Betalgo.OpenAI NuGet)
  - [ ] Prompt tasarımı: trip bilgisi (şehir, tarih aralığı, gün sayısı, kişi sayısı, bütçe, stil) + place listesi (ad, kategori, rating, süre, fiyat) → günlük timeline talebi
  - [ ] Response format: JSON (day_number, place_id, arrival_time, duration_minutes, ai_reasoning)
  - [ ] Response parsing: JSON → Stop entity listesine dönüşüm
  - [ ] Timeout: 30 saniye, retry: 1 kez
  - [ ] Hata durumunda exception fırlat (handler fallback'e yönlendirecek)
- [ ] `OpenAISettings.cs` kullanılacak (ApiKey, Model, MaxTokens)

---

## 📅 Week 18: AI Fallback & Retry Logic

**Hedef:** Kural tabanlı fallback motor, hata yönetimi, kullanıcı bildirimi

---

### Task 18.1: Infrastructure — AiFallbackService

**Tahmini Süre:** 2.5 saat  
**Durum:** ⏳ Bekliyor

**Yapılacaklar:**
- [ ] `AiFallbackService.cs` implementasyonu:
  - [ ] Place'leri rating ve popularity'ye göre sırala
  - [ ] Trip gün sayısına göre günlere dağıt (her gün 3-4 stop)
  - [ ] Sabit şablon: sabah 09:00 başla, her stop arasında 1.5 saat, öğlen yemek stop'u ekle, akşam restoran stop'u ekle
  - [ ] Her stop'a ai_reasoning = "Kural tabanlı yedek plan — AI geçici olarak kullanılamıyor" yaz
  - [ ] Stop entity listesi dön

---

### Task 18.2: Fallback Bildirim & Error Handling

**Tahmini Süre:** 1 saat  
**Durum:** ⏳ Bekliyor

**Yapılacaklar:**
- [ ] GenerateTimelineCommand response'una fallback bilgisi ekle: isAiFallback (boolean) + mesaj
- [ ] Frontend bu bilgiyi kullanarak kullanıcıya "AI geçici olarak kullanılamıyor, kural tabanlı plan oluşturuldu" gösterecek

---

## 🧪 Phase 6 Test Gereksinimleri

### Unit Tests

- [ ] **GenerateTimelineCommand** — trip owner olmayan kullanıcı → ForbiddenException
- [ ] **GenerateTimelineCommand** — place'ler boşsa → uygun hata mesajı
- [ ] **AiFallbackService** — 5 günlük trip + 15 place → her günde 3 stop, toplam 15 stop, sabah-akşam sıralı

### Integration Tests

- [ ] **GenerateTimeline_Success** — trip oluştur, place'ler seed et, generate-timeline çağır → stop'lar oluştu, added_by = 'ai', ai_reasoning dolu
- [ ] **GenerateTimeline_ClearsOldAiStops** — 2 kez generate → ilk sefer'in AI stop'ları silindi, ikinci sefer'inkiler var
- [ ] **GenerateTimeline_KeepsUserStops** — kullanıcı 1 stop ekledi, generate çağır → kullanıcı stop'u hala var, AI stop'ları eklendi
- [ ] **GenerateTimeline_Fallback** — OpenAI mock'u hata döndür → fallback çalıştı, isAiFallback = true

---

## ✅ Phase 6 Success Metrics

- [ ] GenerateTimeline: trip + filtrelenmiş place'ler → LLM → günlük stop listesi oluşuyor
- [ ] AI fallback: OpenAI timeout/hata → kural tabanlı motor devreye giriyor, kullanıcı bilgilendiriliyor
- [ ] Üretilen stop'larda added_by = 'ai' ve ai_reasoning dolu
- [ ] Tekrar generate mevcut AI stop'larını temizliyor, user stop'larına dokunmuyor
- [ ] Phase 6 testlerinin tamamı geçiyor

---

## 🎯 Phase 7 Overview

### Scope

**Dahil:**
- Comprehensive integration test suite (tüm controller'lar için)
- Load testing (k6, NBomber veya Bombardier)
- Swagger/OpenAPI documentation finalize
- Postman collection oluşturma
- CI/CD pipeline (Azure DevOps veya GitHub Actions)
- Docker containerization (backend + dependencies)
- Final documentation (README, API docs)

---

## 📅 Week 19: Integration Tests & Load Testing

**Hedef:** Tüm endpoint'lerin kapsamlı integration testi, load testing ile performans doğrulama

---

### Task 19.1: Integration Test Suite Tamamlama

**Tahmini Süre:** 6 saat  
**Durum:** ⏳ Bekliyor

**Yapılacaklar:**
- [ ] Önceki Phase'lerde yazılmamış kalan integration testlerini tamamla
- [ ] CustomWebApplicationFactory'yi finalize et — test DB setup, seed data, auth helper (test user token üretme)
- [ ] Her controller için pozitif + negatif senaryo testleri olduğunu doğrula
- [ ] Auth-gerektiren endpoint'lerin token olmadan 401 döndüğünü doğrula (tüm controller'lar için)
- [ ] Tüm testleri çalıştır, hepsinin geçtiğini doğrula
- [ ] Test coverage raporu al

---

### Task 19.2: Load Testing

**Tahmini Süre:** 3 saat  
**Durum:** ⏳ Bekliyor

**Yapılacaklar:**
- [ ] Load test aracı seç (k6, NBomber veya Bombardier)
- [ ] Test senaryoları tanımla:
  - [ ] Explore endpoint: 100 concurrent user, city filtresi ile
  - [ ] Feed endpoint: 100 concurrent user, infinite scroll simulasyonu
  - [ ] Trip detail: 50 concurrent user, aynı trip'e erişim
  - [ ] Login: 50 concurrent user, ardışık login
- [ ] Hedef: P95 response time < 500ms, error rate < 1%
- [ ] Darboğaz varsa tespit et ve optimize et (index eksikleri, N+1 query, vs.)

---

## 📅 Week 20: Documentation, CI/CD & Deployment

**Hedef:** API documentation, CI/CD pipeline, Docker, final docs

---

### Task 20.1: API Documentation

**Tahmini Süre:** 3 saat  
**Durum:** ⏳ Bekliyor

**Yapılacaklar:**
- [ ] Swagger/OpenAPI tüm endpoint'lerde açıklama ve örnek request/response
- [ ] Swagger UI'da authorization (JWT Bearer) çalışıyor
- [ ] Postman collection oluştur: tüm endpoint'ler, environment variables (base URL, token), pre-request scripts (otomatik login)
- [ ] Postman collection'ı docs/ klasörüne ekle

---

### Task 20.2: CI/CD Pipeline

**Tahmini Süre:** 3 saat  
**Durum:** ⏳ Bekliyor

**Yapılacaklar:**
- [ ] Azure DevOps veya GitHub Actions pipeline oluştur
- [ ] Pipeline adımları:
  - [ ] Checkout code
  - [ ] Setup .NET 8
  - [ ] Restore dependencies
  - [ ] Build solution
  - [ ] Run unit tests
  - [ ] Run integration tests (test DB ile)
  - [ ] Publish artifacts
- [ ] PR merge'de otomatik çalışacak şekilde konfigüre et
- [ ] Build badge'i README'ye ekle

---

### Task 20.3: Docker Containerization

**Tahmini Süre:** 2 saat  
**Durum:** ⏳ Bekliyor

**Yapılacaklar:**
- [ ] Backend Dockerfile oluştur (multi-stage build: SDK restore/build → runtime image)
- [ ] docker-compose.yml oluştur: backend service + Redis service (Azure PostgreSQL harici, managed)
- [ ] Environment variables docker-compose'dan okunacak şekilde konfigüre et
- [ ] docker-compose up ile full stack ayağa kaldır ve test et
- [ ] Health check endpoint'inin Docker'dan çalıştığını doğrula

---

### Task 20.4: Final Documentation

**Tahmini Süre:** 2 saat  
**Durum:** ⏳ Bekliyor

**Yapılacaklar:**
- [ ] README.md güncelle: kurulum adımları, environment variables, Docker kullanımı, API endpoint listesi
- [ ] BACKEND_SCHEMA_MVP.md'nin güncel olduğunu doğrula
- [ ] ROADMAP_MVP.md'deki tamamlanan task'ları ✅ işaretle
- [ ] Bilinen limitasyonlar ve teknik borç listesi oluştur
- [ ] Bitirme projesi için hazırlık notları (Amadeus API, RAG, Flutter mobil, UserBlock, PushToken)

---

## ✅ Phase 7 Success Metrics

- [ ] Integration test: tüm endpoint'ler pozitif + negatif test edildi, hepsi geçiyor
- [ ] Load test: 100 concurrent user, P95 < 500ms, error rate < 1%
- [ ] Swagger: tüm endpoint'ler documented, örnek request/response var
- [ ] Postman collection: tüm endpoint'ler, auth helper, environment variables
- [ ] CI/CD: push → build → test → artifacts pipeline çalışıyor
- [ ] Docker: docker-compose up ile backend + Redis ayağa kalkıyor
- [ ] Documentation: README, SCHEMA, ROADMAP güncel

---

## 🎯 Phase 7 Completion

**Phase 7 tamamlandığında elimizde şunlar olacak:**

✅ **18 tablo** Azure PostgreSQL'de  
✅ **Auth** — Register, Login, Dual Refresh (web cookie + mobile body)  
✅ **Trips** — CRUD, Publish, Archive, Fork, Explore, Upvote, Save  
✅ **Stops** — CRUD, LexoRank reorder, time lock, custom stops, visited  
✅ **Flights & Hotels** — Selection, listing, booking consistency  
✅ **Posts & Comments** — CRUD, upvote, reply, cross-post protection  
✅ **Feed** — 3 tab (ForYou/Following/Latest), cursor pagination  
✅ **Community Tips** — Route + place-specific, upvote  
✅ **Karma** — Event-based scoring, farming protection, cache sync  
✅ **Notifications** — 8 tip bildirim, polymorphic target, mark read, unread count  
✅ **AI Timeline** — GPT-4o generation + rule-based fallback  
✅ **Tests** — Unit + Integration + Load, tamamı geçiyor  
✅ **CI/CD** — Otomatik build + test pipeline  
✅ **Docker** — Containerized deployment  

**Bitirme projesine ertelenenler:** UserBlock, PushToken, Amadeus API, RAG/pgvector, Flutter mobil, AI tagging, admin panel 🔜

---

**Başarılar!** 💪
