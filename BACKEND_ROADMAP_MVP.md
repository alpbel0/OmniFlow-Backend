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

### Scope

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
- [ ] 18 entity, 20 enum, tüm EF Core configuration'lar yazıldı
- [ ] Azure PostgreSQL'e migration başarıyla uygulandı, 18 tablo oluştu
- [ ] Register → Login → Access Token → Refresh Token akışı çalışıyor
- [ ] Web (cookie) ve Mobile (body) refresh akışı ayrı çalışıyor
- [ ] Swagger UI'da tüm auth endpoint'leri test edilebiliyor
- [ ] Seed data ile default admin + traveler roller oluşuyor
- [ ] Global error handler tüm exception tiplerini yakalıyor
- [ ] Phase 1 testleri geçiyor

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

### Task 2.6: Azure PostgreSQL Connection & Migration (BU KISIM TAMAM ŞU AN LOCAL ORTAMDA ÇALIŞYIORUM BURAYI OKUMA KAFAN KARIŞMASIN)

**Tahmini Süre:** 2 saat  
**Durum:** ⏳ Bekliyor

**Yapılacaklar:**
- [ ] appsettings.Development.json'a Azure PostgreSQL connection string ekle (Host, Database, Username, Password, SSL Mode)
- [ ] Infrastructure/ServiceRegistration.cs'de DbContext DI registration yap
- [ ] Initial migration oluştur (InitialCreate)
- [ ] Migration'ı Azure PostgreSQL'e uygula
- [ ] pgAdmin veya psql ile 18 tablonun oluştuğunu doğrula
- [ ] CHECK constraint'lerin uygulandığını kontrol et (information_schema.check_constraints sorgusu)
- [ ] Index'lerin oluştuğunu kontrol et (pg_indexes sorgusu)

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

- [ ] Build: 0 error, 0 warning
- [ ] Migration: 18 tablo Azure PostgreSQL'de oluşturuldu
- [ ] Auth Flow: Register → Login → Access Token → Refresh → New Token çalışıyor
- [ ] Dual Platform: Web (cookie) ve Mobile (body) refresh ayrı çalışıyor
- [ ] Swagger: Tüm auth endpoint'leri test edilebilir durumda

### Quality Metrics

- [ ] Clean Architecture: Domain hiçbir projeye referans vermiyor, referans zinciri doğru
- [ ] EF Core: Tüm CHECK constraint'ler ve index'ler SQL şemasıyla eşleşiyor
- [ ] Security: Token hash'leniyor, HttpOnly cookie, Secure flag, ClockSkew = 0, rotation aktif
- [ ] Error Handling: Global middleware 7 exception tipini yakalıyor, 500'lerde stack trace dışarı sızmıyor
- [ ] Test: Phase 1 unit + integration testlerinin tamamı geçiyor

---

## 🎯 Phase 2 Overview

### Scope

**Dahil:**
- Places CRUD + şehir/kategori filtreleme
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
**Durum:** ⏳ Bekliyor

**Yapılacaklar:**
- [ ] `IGenericRepositoryAsync.cs` interface'ini finalize et — GetByIdAsync, GetAllAsync, GetPagedAsync (RequestParameter alır, PagedResponse döner), AddAsync, UpdateAsync, DeleteAsync
- [ ] `Infrastructure/Repositories/GenericRepositoryAsync.cs` implementasyonu — ApplicationDbContext inject, LINQ ile pagination, total count hesaplama
- [ ] DI registration (ServiceRegistration.cs)

---

### Task 4.2: Application — Place Feature (CQRS)

**Tahmini Süre:** 3 saat  
**Durum:** ⏳ Bekliyor

**Yapılacaklar:**
- [ ] `IPlaceRepositoryAsync.cs` — IGenericRepositoryAsync<Place>'den türer, ek metotlar: GetByCityAsync (city, pagination), GetByCategoryAsync (category, pagination)
- [ ] `PlaceResponse.cs` DTO — Id, Name, Category, Latitude, Longitude, City, Country, Rating, EstimatedPrice, IsFree, BudgetTiers, TravelStyles, DurationMinutes bilgilerini içerir
- [ ] `CreatePlaceCommand.cs` — MediatR IRequest, tüm place field'larını alır, handler'da repository'ye ekler
- [ ] `CreatePlaceCommandValidator.cs` — Name boş olamaz (max 255), Category enum'da olmalı, City ve Country zorunlu, Latitude -90/90 arası, Longitude -180/180 arası, Rating 1-5 arası (nullable), EstimatedPrice >= 0, CurrencyCode 3 harf büyük harf regex
- [ ] `GetAllPlacesQuery.cs` — pagination parametreleri alır, PagedResponse<PlaceResponse> döner
- [ ] `GetPlaceByIdQuery.cs` — Guid alır, PlaceResponse döner, bulunamazsa EntityNotFoundException
- [ ] `GetPlacesByCityQuery.cs` — city string + pagination alır, PagedResponse<PlaceResponse> döner
- [ ] GeneralProfile.cs'ye Place → PlaceResponse AutoMapper mapping ekle

---

### Task 4.3: Infrastructure — Place Repository

**Tahmini Süre:** 1.5 saat  
**Durum:** ⏳ Bekliyor

**Yapılacaklar:**
- [ ] `PlaceRepositoryAsync.cs` — GenericRepositoryAsync<Place>'den türer, IPlaceRepositoryAsync implement eder
- [ ] GetByCityAsync — is_active = true filtreli, case-insensitive city eşleşmesi, name'e göre sıralı, pagination uygulanmış
- [ ] GetByCategoryAsync — is_active = true filtreli, category eşleşmesi, pagination uygulanmış
- [ ] DI registration

---

### Task 4.4: WebApi — PlacesController

**Tahmini Süre:** 1 saat  
**Durum:** ⏳ Bekliyor

**Yapılacaklar:**
- [ ] `v1/PlacesController.cs` — BaseApiController'dan türer, [Authorize] attribute
- [ ] GET /api/v1/places — query parametrelerinden filtre alır, GetAllPlacesQuery gönderir
- [ ] GET /api/v1/places/{id} — Guid alır, GetPlaceByIdQuery gönderir
- [ ] GET /api/v1/places/city/{city} — şehir adı + pagination, GetPlacesByCityQuery gönderir
- [ ] POST /api/v1/places — [Authorize(Roles = "Admin")], CreatePlaceCommand body'den alır
- [ ] Swagger'da 4 endpoint'i test et

---

## 📅 Week 5: Trips — CRUD, Publish, Archive

**Hedef:** Trip CRUD, status state machine (Draft → Published → Archived), owner authorization, trip save/unsave

---

### Task 5.1: Application — Trip Repository Interface

**Tahmini Süre:** 1 saat  
**Durum:** ⏳ Bekliyor

**Yapılacaklar:**
- [ ] `ITripRepositoryAsync.cs` — IGenericRepositoryAsync<Trip>'den türer, ek metotlar: GetByOwnerAsync (userId, pagination), GetPublishedByOwnerAsync (userId), GetWithStopsAsync (tripId, includes: Stops → Place)
- [ ] `TripResponse.cs` DTO — tüm trip field'ları + owner bilgisi (Id, Username, ProfilePhotoUrl)
- [ ] `CreateTripRequest.cs` DTO — Title, City, Country, StartDate, EndDate, PersonCount, BudgetTier, TravelStyle, UserBudget (nullable)

---

### Task 5.2: Application — Trip Commands

**Tahmini Süre:** 3 saat  
**Durum:** ⏳ Bekliyor

**Yapılacaklar:**
- [ ] `CreateTripCommand.cs` — authenticated user'ın Id'sini owner olarak atar, status = Draft, handler repository'ye ekler
- [ ] `CreateTripCommandValidator.cs` — Title boş olamaz (max 100), City/Country zorunlu, EndDate >= StartDate, PersonCount > 0, UserBudget >= 0 (nullable)
- [ ] `UpdateTripCommand.cs` — TripId + güncellenebilir alanlar, handler'da owner kontrolü (authenticated user = trip owner, değilse ForbiddenException), sadece Draft trip güncellenebilir
- [ ] `UpdateTripCommandValidator.cs` — CreateTrip ile aynı validasyon kuralları
- [ ] `DeleteTripCommand.cs` — soft delete (deleted_at = now), owner kontrolü
- [ ] `PublishTripCommand.cs` — status Draft → Published geçişi, owner kontrolü, en az 1 stop olmalı (yoksa ApiException)
- [ ] `ArchiveTripCommand.cs` — status Published → Archived geçişi, owner kontrolü

---

### Task 5.3: Application — Trip Queries

**Tahmini Süre:** 1.5 saat  
**Durum:** ⏳ Bekliyor

**Yapılacaklar:**
- [ ] `GetTripByIdQuery.cs` — Trip + Owner bilgisi döner, soft deleted trip için EntityNotFoundException
- [ ] `GetMyTripsQuery.cs` — authenticated user'ın trip'leri, pagination, status filtresi (opsiyonel)
- [ ] GeneralProfile.cs'ye Trip → TripResponse mapping ekle

---

### Task 5.4: Infrastructure — Trip Repository & Controller

**Tahmini Süre:** 2 saat  
**Durum:** ⏳ Bekliyor

**Yapılacaklar:**
- [ ] `TripRepositoryAsync.cs` implementasyonu — soft delete filtresi (deleted_at IS NULL), owner bazlı sorgular, include ile Stop/Flight/Hotel yükleme
- [ ] `v1/TripsController.cs` oluştur:
  - [ ] GET /api/v1/trips — GetMyTripsQuery (kendi trip'leri)
  - [ ] GET /api/v1/trips/{id} — GetTripByIdQuery
  - [ ] POST /api/v1/trips — CreateTripCommand
  - [ ] PUT /api/v1/trips/{id} — UpdateTripCommand
  - [ ] DELETE /api/v1/trips/{id} — DeleteTripCommand
  - [ ] POST /api/v1/trips/{id}/publish — PublishTripCommand
  - [ ] POST /api/v1/trips/{id}/archive — ArchiveTripCommand

---

### Task 5.5: Trip Save/Unsave & Upvote

**Tahmini Süre:** 1.5 saat  
**Durum:** ⏳ Bekliyor

**Yapılacaklar:**
- [ ] `SaveTripCommand.cs` — IApplicationDbContext üzerinden SavedTrip ekler, çift kayıt varsa ignore
- [ ] `UnsaveTripCommand.cs` — SavedTrip siler
- [ ] `UpvoteTripCommand.cs` — IApplicationDbContext üzerinden TripUpvote ekler, çift upvote varsa DuplicateUpvoteException, trip'in upvote_count'ını 1 artırır
- [ ] `GetSavedTripsQuery.cs` — authenticated user'ın kaydettiği trip'ler, pagination
- [ ] Controller endpoint'leri:
  - [ ] POST /api/v1/trips/{id}/save
  - [ ] DELETE /api/v1/trips/{id}/save
  - [ ] POST /api/v1/trips/{id}/upvote
  - [ ] GET /api/v1/saved-trips

---

## 📅 Week 6: Stops — CRUD, Reorder, Time Lock

**Hedef:** Stop CRUD, LexoRank drag-and-drop sıralama, time lock koruması, custom stop'lar, visited tracking

---

### Task 6.1: Application — Stop Repository & DTOs

**Tahmini Süre:** 1.5 saat  
**Durum:** ⏳ Bekliyor

**Yapılacaklar:**
- [ ] `IStopRepositoryAsync.cs` — GetByTripAsync (tripId, day_number + order_index sıralı), GetByTripAndDayAsync (tripId, dayNumber)
- [ ] `StopResponse.cs` DTO — tüm stop field'ları + place bilgisi (varsa: Name, Category, PhotoUrl) + fallback place bilgisi (varsa)
- [ ] `CreateStopRequest.cs` DTO — TripId, PlaceId (nullable), DayNumber, ArrivalTime (nullable), DurationMinutes, CustomName/CustomCategory (nullable), Notes, ActivityPrice, TransportPrice, TransportFromPrevious
- [ ] `UpdateStopRequest.cs` DTO — güncellenebilir alanlar
- [ ] `ReorderStopRequest.cs` DTO — StopId, NewOrderIndex (double)

---

### Task 6.2: Application — Stop Commands

**Tahmini Süre:** 3 saat  
**Durum:** ⏳ Bekliyor

**Yapılacaklar:**
- [ ] `CreateStopCommand.cs` — trip owner kontrolü, PlaceId null ise CustomName + CustomCategory zorunlu (place_or_custom_name iş kuralı), OrderIndex otomatik hesapla (mevcut son stop'un index'i + 1000), AddedBy = User
- [ ] `CreateStopCommandValidator.cs` — DayNumber > 0, DurationMinutes > 0 (nullable), ActivityPrice >= 0, TransportPrice >= 0, CurrencyCode 3 harf regex, IsTimeLocked = true ise ArrivalTime zorunlu
- [ ] `UpdateStopCommand.cs` — trip owner kontrolü, stop'un trip'e ait olduğunu doğrula, time lock kurallarını kontrol et
- [ ] `DeleteStopCommand.cs` — trip owner kontrolü, fiziksel silme (stop'larda soft delete yok)
- [ ] `ReorderStopsCommand.cs` — trip owner kontrolü, yeni OrderIndex değeri ata (LexoRank: iki stop arasındaki orta değer), IsTimeLocked = true olan stop'ların sırası değiştirilemez
- [ ] `MarkStopVisitedCommand.cs` — trip owner kontrolü, IsVisited = true + VisitedAt = now, visited_consistency kuralını sağla

---

### Task 6.3: Application — Stop Queries & Controller

**Tahmini Süre:** 1.5 saat  
**Durum:** ⏳ Bekliyor

**Yapılacaklar:**
- [ ] `GetStopsByTripQuery.cs` — trip'in tüm stop'ları, day_number + order_index sıralı, Place ve FallbackPlace include, trip'in public (published) olması veya authenticated user'ın owner olması gerekir
- [ ] `StopsController.cs` endpoint'leri:
  - [ ] GET /api/v1/trips/{tripId}/stops — GetStopsByTripQuery
  - [ ] POST /api/v1/trips/{tripId}/stops — CreateStopCommand
  - [ ] PUT /api/v1/trips/{tripId}/stops/{stopId} — UpdateStopCommand
  - [ ] DELETE /api/v1/trips/{tripId}/stops/{stopId} — DeleteStopCommand
  - [ ] PUT /api/v1/trips/{tripId}/stops/reorder — ReorderStopsCommand
  - [ ] POST /api/v1/trips/{tripId}/stops/{stopId}/visited — MarkStopVisitedCommand

---

## 📅 Week 7: Fork System & Explore

**Hedef:** Fork (deep copy), self-fork engeli, Explore endpoint (popularity, filtreleme, pagination)

---

### Task 7.1: Application — Fork Command

**Tahmini Süre:** 2.5 saat  
**Durum:** ⏳ Bekliyor

**Yapılacaklar:**
- [ ] `ForkTripCommand.cs`:
  - [ ] Orijinal trip'in published olduğunu doğrula
  - [ ] Self-fork kontrolü: authenticated user = trip owner ise SelfForkException
  - [ ] Yeni Trip entity oluştur: tüm field'ları kopyala, OwnerId = authenticated user, ForkedFromId = orijinal trip Id, Status = Draft, counter'lar sıfırla
  - [ ] Orijinal trip'in tüm Stop'larını deep copy et (yeni Id'ler ile, aynı sıra ve ayarlar)
  - [ ] Orijinal trip'in Flight ve Hotel seçimlerini kopyala (yeni Id'ler ile)
  - [ ] Orijinal trip'in fork_count'ını 1 artır
  - [ ] Karma tetikleme: placeholder olarak bırak (Phase 5'te KarmaService entegrasyonu)
  - [ ] Yeni trip'in Id'sini dön

---

### Task 7.2: Application — Explore Query

**Tahmini Süre:** 2.5 saat  
**Durum:** ⏳ Bekliyor

**Yapılacaklar:**
- [ ] `ExploreTripsQuery.cs`:
  - [ ] Sadece published ve soft deleted olmayan trip'ler
  - [ ] Opsiyonel filtreler: city, country, budget_tier, travel_style, tags (herhangi biri eşleşirse)
  - [ ] Sıralama: popularity_score DESC (default), created_at DESC (opsiyonel)
  - [ ] Cursor-based pagination (offset değil, son trip'in popularity_score + id ile devam)
  - [ ] Her trip'te owner bilgisi (Username, ProfilePhotoUrl) ve istatistikler (upvote, fork, view)
  - [ ] Authenticated user'ın bu trip'i upvote/save edip etmediği bilgisi (isUpvoted, isSaved boolean)
- [ ] `ExploreTripsParameter.cs` — City, BudgetTier, TravelStyle, Tags, SortBy, Cursor, PageSize
- [ ] `ExploreTripsViewModel.cs` — TripResponse listesi + NextCursor

---

### Task 7.3: Explore & Fork Controller Endpoints

**Tahmini Süre:** 1 saat  
**Durum:** ⏳ Bekliyor

**Yapılacaklar:**
- [ ] GET /api/v1/explore — ExploreTripsQuery, query parametrelerinden filtreler
- [ ] POST /api/v1/trips/{id}/fork — ForkTripCommand
- [ ] Swagger'da her iki endpoint'i test et

---

## 🧪 Phase 2 Test Gereksinimleri

### Unit Tests

- [ ] **CreateTripCommandValidator** — boş title reddedilmeli, EndDate < StartDate reddedilmeli, PersonCount = 0 reddedilmeli, geçerli komut kabul edilmeli
- [ ] **CreateStopCommandValidator** — DayNumber = 0 reddedilmeli, IsTimeLocked = true + ArrivalTime = null reddedilmeli, negatif fiyat reddedilmeli
- [ ] **CreatePlaceCommandValidator** — boş name reddedilmeli, rating = 6 reddedilmeli, geçersiz koordinat reddedilmeli
- [ ] **ForkTripCommand** — self-fork SelfForkException fırlatmalı
- [ ] **PublishTripCommand** — stop'u olmayan trip publish edilememeli
- [ ] **ReorderStopsCommand** — time-locked stop'un sırası değiştirilemez

### Integration Tests

- [ ] **Places_CRUD** — Create (admin) → GetById → GetByCity → her biri doğru sonuç
- [ ] **Places_Create_NonAdmin_Returns403** — Traveler rolü ile create deneme → 403
- [ ] **Trips_FullLifecycle** — Create → Update → AddStop → Publish → Archive, her adımda doğru status
- [ ] **Trips_OwnerAuthorization** — Başka kullanıcının trip'ini update deneme → 403
- [ ] **Stops_Reorder** — 3 stop oluştur → reorder → sıranın değiştiğini doğrula
- [ ] **Stops_TimeLock** — time-locked stop reorder deneme → hata
- [ ] **Fork_Success** — Published trip fork → yeni trip oluştu, stop'lar kopyalandı, fork_count arttı
- [ ] **Fork_SelfFork_Returns409** — Kendi trip'ini fork deneme → 409
- [ ] **Fork_DraftTrip_Returns400** — Draft trip fork deneme → hata
- [ ] **Explore_Filters** — city + budget_tier filtresi ile doğru sonuçlar
- [ ] **Explore_Pagination** — cursor-based pagination ile ardışık sayfalar farklı trip'ler dönmeli
- [ ] **TripUpvote_Success** — upvote → upvote_count 1 arttı
- [ ] **TripUpvote_Duplicate_Returns409** — aynı trip'e ikinci upvote → 409
- [ ] **SaveTrip_Success** — save → saved-trips'te görünüyor, unsave → görünmüyor

---

## ✅ Phase 2 Success Metrics

- [ ] Places CRUD + city/category filtreleme çalışıyor
- [ ] Trip lifecycle: Draft → Published → Archived, her geçişte doğru validasyon
- [ ] Owner authorization: başka kullanıcının trip/stop'una erişim engelleniyor
- [ ] Stop reorder LexoRank ile çalışıyor, time-lock korunuyor
- [ ] Custom stop (place_id NULL) oluşturulabiliyor
- [ ] Fork: deep copy + self-fork engeli + fork_count artışı
- [ ] Explore: filter + sort + cursor pagination çalışıyor
- [ ] Upvote + Save: çalışıyor, duplicate engeli var
- [ ] Phase 2 testlerinin tamamı geçiyor

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
**Durum:** ⏳ Bekliyor

**Yapılacaklar:**
- [ ] `IFlightRepositoryAsync.cs` — GetByTripAsync (tripId, direction filtresi opsiyonel), GetByGroupAsync (itineraryGroupId)
- [ ] `FlightResponse.cs` DTO — tüm flight field'ları, formatlı departure/arrival bilgisi
- [ ] `SelectFlightRequest.cs` DTO — FlightId
- [ ] `SelectFlightCommand.cs` — trip owner kontrolü, seçilen flight'ın bu trip'e ait olduğunu doğrula, is_booked = true + booked_at = now set et, aynı direction'daki önceki seçimi iptal et (is_booked = false)
- [ ] `GetFlightsByTripQuery.cs` — trip'in tüm flight seçenekleri, direction bazlı gruplama

---

### Task 8.2: WebApi — FlightsController

**Tahmini Süre:** 1 saat  
**Durum:** ⏳ Bekliyor

**Yapılacaklar:**
- [ ] `v1/FlightsController.cs`:
  - [ ] GET /api/v1/trips/{tripId}/flights — GetFlightsByTripQuery
  - [ ] POST /api/v1/trips/{tripId}/flights/select — SelectFlightCommand
- [ ] Swagger'da test et

---

## 📅 Week 9: Hotels — Entity, Selection, Queries

**Hedef:** Hotel repository, trip bazlı listeleme, otel seçimi

---

### Task 9.1: Application — Hotel Feature

**Tahmini Süre:** 2.5 saat  
**Durum:** ⏳ Bekliyor

**Yapılacaklar:**
- [ ] `IHotelRepositoryAsync.cs` — GetByTripAsync (tripId, check-in sıralı)
- [ ] `HotelResponse.cs` DTO — tüm hotel field'ları, formatlı check-in/out, gece sayısı hesaplı
- [ ] `SelectHotelRequest.cs` DTO — HotelId
- [ ] `SelectHotelCommand.cs` — trip owner kontrolü, seçilen hotel'in bu trip'e ait olduğunu doğrula, is_booked = true + booked_at = now set et, önceki seçimi iptal et
- [ ] `GetHotelsByTripQuery.cs` — trip'in tüm otel seçenekleri, check-in sıralı

---

### Task 9.2: WebApi — HotelsController

**Tahmini Süre:** 1 saat  
**Durum:** ⏳ Bekliyor

**Yapılacaklar:**
- [ ] `v1/HotelsController.cs`:
  - [ ] GET /api/v1/trips/{tripId}/hotels — GetHotelsByTripQuery
  - [ ] POST /api/v1/trips/{tripId}/hotels/select — SelectHotelCommand
- [ ] Swagger'da test et

---

## 🧪 Phase 3 Test Gereksinimleri

### Unit Tests

- [ ] **SelectFlightCommand** — başka kullanıcının trip'indeki flight seçme → ForbiddenException
- [ ] **SelectFlightCommand** — trip'e ait olmayan flight seçme → EntityNotFoundException
- [ ] **SelectHotelCommand** — aynı validasyonlar

### Integration Tests

- [ ] **Flights_GetByTrip** — trip'e ait flight'lar dönmeli, başka trip'in flight'ları dönmemeli
- [ ] **Flights_Select** — select → is_booked = true, önceki seçim iptal oldu
- [ ] **Hotels_GetByTrip** — check-in sıralı listeleme
- [ ] **Hotels_Select** — select → is_booked = true, booking_reference opsiyonel
- [ ] **BookingConsistency** — is_booked = true olan kayıtta booked_at dolu olmalı

---

## ✅ Phase 3 Success Metrics

- [ ] Flight seçimi çalışıyor, gidiş-dönüş itinerary gruplama var
- [ ] Hotel seçimi çalışıyor, check-in/out validation doğru
- [ ] Booking consistency: is_booked ve booked_at her zaman senkron
- [ ] data_source = 'mock' ile seeded veriler sorgulanabiliyor
- [ ] Phase 3 testlerinin tamamı geçiyor

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
- [ ] 3 tip post oluşturulabiliyor, route tipi trip gerektiriyor
- [ ] Comment + reply (1 seviye) çalışıyor, cross-post koruması aktif
- [ ] Feed 3 tab ile cursor pagination çalışıyor
- [ ] Follow/unfollow counter cache doğru güncelleniyor
- [ ] User profile + update çalışıyor
- [ ] Phase 4 testleri geçiyor

---

## 📅 Week 10: Posts — CRUD, Types, Upvote

**Hedef:** Post oluşturma (3 tip), güncelleme, silme, upvote, post detay

---

### Task 10.1: Application — Post Feature

**Tahmini Süre:** 3 saat  
**Durum:** ⏳ Bekliyor

**Yapılacaklar:**
- [ ] `IPostRepositoryAsync.cs` — GetByIdWithUserAsync, GetByUserAsync (userId, pagination), GetVisibleAsync (soft delete + is_visible filtreli)
- [ ] `PostResponse.cs` DTO — tüm post field'ları + user bilgisi (Username, ProfilePhotoUrl, KarmaScore) + isUpvoted (authenticated user)
- [ ] `CreatePostCommand.cs` — UserId authenticated user'dan alınır, PostType = Route ise TripId zorunlu (route_requires_trip), Content veya Photos'dan en az biri dolu olmalı (content_or_photo), Tags opsiyonel
- [ ] `CreatePostCommandValidator.cs` — PostType enum'da olmalı, content ve photos ikisi de boşsa hata
- [ ] `UpdatePostCommand.cs` — post owner kontrolü, Content ve Tags güncellenebilir
- [ ] `DeletePostCommand.cs` — soft delete (deleted_at = now), post owner kontrolü
- [ ] `UpvotePostCommand.cs` — IApplicationDbContext üzerinden PostUpvote ekle, duplicate → DuplicateUpvoteException, post upvote_count 1 artır
- [ ] `GetPostByIdQuery.cs` — post + user bilgisi, soft deleted → EntityNotFoundException

---

### Task 10.2: WebApi — PostsController

**Tahmini Süre:** 1 saat  
**Durum:** ⏳ Bekliyor

**Yapılacaklar:**
- [ ] `v1/PostsController.cs`:
  - [ ] GET /api/v1/posts/{id} — GetPostByIdQuery
  - [ ] POST /api/v1/posts — CreatePostCommand
  - [ ] PUT /api/v1/posts/{id} — UpdatePostCommand
  - [ ] DELETE /api/v1/posts/{id} — DeletePostCommand
  - [ ] POST /api/v1/posts/{id}/upvote — UpvotePostCommand

---

## 📅 Week 11: Comments — CRUD, Reply, Upvote

**Hedef:** Comment oluşturma, reply (1 seviye), cross-post koruması, upvote, mention

---

### Task 11.1: Application — Comment Feature

**Tahmini Süre:** 3 saat  
**Durum:** ⏳ Bekliyor

**Yapılacaklar:**
- [ ] `ICommentRepositoryAsync.cs` — GetByPostAsync (postId, created_at sıralı, replies include), GetByIdWithRepliesAsync
- [ ] `CommentResponse.cs` DTO — comment field'ları + user bilgisi + replies listesi (1 seviye) + isUpvoted
- [ ] `CreateCommentCommand.cs` — PostId zorunlu, ParentCommentId opsiyonel (reply), Content zorunlu, Mentions opsiyonel. Handler'da: post var mı kontrolü, ParentCommentId varsa parent'ın aynı post'a ait olduğunu doğrula (cross-post koruması), post comment_count 1 artır
- [ ] `CreateCommentCommandValidator.cs` — Content boş olamaz
- [ ] `DeleteCommentCommand.cs` — soft delete, comment owner kontrolü, post comment_count 1 azalt
- [ ] `UpvoteCommentCommand.cs` — duplicate engeli, comment upvote_count artır
- [ ] `GetCommentsByPostQuery.cs` — post'un tüm comment'leri (soft deleted ve invisible hariç), reply'ler include, pagination

---

### Task 11.2: WebApi — CommentsController

**Tahmini Süre:** 1 saat  
**Durum:** ⏳ Bekliyor

**Yapılacaklar:**
- [ ] `v1/CommentsController.cs`:
  - [ ] GET /api/v1/posts/{postId}/comments — GetCommentsByPostQuery
  - [ ] POST /api/v1/posts/{postId}/comments — CreateCommentCommand
  - [ ] DELETE /api/v1/comments/{id} — DeleteCommentCommand
  - [ ] POST /api/v1/comments/{id}/upvote — UpvoteCommentCommand

---

## 📅 Week 12: Feed — Pagination, Tabs, Infinite Scroll

**Hedef:** Feed endpoint (3 tab), cursor-based infinite scroll

---

### Task 12.1: Application — Feed Query

**Tahmini Süre:** 3 saat  
**Durum:** ⏳ Bekliyor

**Yapılacaklar:**
- [ ] `GetFeedQuery.cs` — tab parametresi (ForYou / Following / Latest):
  - [ ] **Latest**: tüm visible + non-deleted post'lar, created_at DESC, cursor pagination
  - [ ] **Following**: authenticated user'ın takip ettiği kullanıcıların post'ları, created_at DESC
  - [ ] **ForYou**: başlangıçta Latest ile aynı (ileride recommendation algoritması eklenebilir)
  - [ ] Her post'ta: user bilgisi, isUpvoted, comment_count
- [ ] `GetFeedParameter.cs` — Tab (enum: ForYou, Following, Latest), Cursor (nullable string), PageSize (default 20)
- [ ] `GetFeedViewModel.cs` — PostResponse listesi + NextCursor + HasMore boolean

---

### Task 12.2: WebApi — Feed Endpoint

**Tahmini Süre:** 30 dakika  
**Durum:** ⏳ Bekliyor

**Yapılacaklar:**
- [ ] GET /api/v1/feed — query parametrelerinden tab, cursor, pageSize alır, GetFeedQuery gönderir
- [ ] Swagger'da 3 tab ile test et

---

## 📅 Week 13: Follow System & User Profiles

**Hedef:** Follow/unfollow, followers/following listeleme, user profile, update profile

---

### Task 13.1: Application — Follow Feature

**Tahmini Süre:** 2 saat  
**Durum:** ⏳ Bekliyor

**Yapılacaklar:**
- [ ] `IFollowRepositoryAsync.cs` — GetFollowersAsync (userId, pagination), GetFollowingAsync (userId, pagination), IsFollowingAsync (followerId, followingId)
- [ ] `FollowUserCommand.cs` — self-follow kontrolü (SelfFollowException), zaten takip ediyorsa ignore, following user'ın followers_count ve follower user'ın following_count'ını 1 artır
- [ ] `UnfollowUserCommand.cs` — takip ilişkisi yoksa ignore, counter'ları 1 azalt
- [ ] `GetFollowersQuery.cs` — userId'nin follower'ları, pagination, her kullanıcıda isFollowing bilgisi
- [ ] `GetFollowingQuery.cs` — userId'nin takip ettikleri, pagination

---

### Task 13.2: Application — User Profile Feature

**Tahmini Süre:** 2 saat  
**Durum:** ⏳ Bekliyor

**Yapılacaklar:**
- [ ] `IUserRepositoryAsync.cs` — GetByIdAsync, GetByUsernameAsync, UpdateAsync
- [ ] `UserProfileResponse.cs` DTO — Id, Username, Email, Bio, ProfilePhotoUrl, KarmaScore, FollowersCount, FollowingCount, IsVerified, isFollowing (authenticated user), tripCount, postCount
- [ ] `GetUserProfileQuery.cs` — username veya id ile kullanıcı profili
- [ ] `UpdateProfileCommand.cs` — sadece kendi profilini güncelleyebilir, Bio (max 300), ProfilePhotoUrl güncellenebilir
- [ ] `UpdateProfileCommandValidator.cs` — Bio max 300 karakter

---

### Task 13.3: WebApi — Follows & Users Controllers

**Tahmini Süre:** 1.5 saat  
**Durum:** ⏳ Bekliyor

**Yapılacaklar:**
- [ ] `v1/FollowsController.cs`:
  - [ ] POST /api/v1/users/{userId}/follow — FollowUserCommand
  - [ ] DELETE /api/v1/users/{userId}/follow — UnfollowUserCommand
  - [ ] GET /api/v1/users/{userId}/followers — GetFollowersQuery
  - [ ] GET /api/v1/users/{userId}/following — GetFollowingQuery
- [ ] `v1/UsersController.cs`:
  - [ ] GET /api/v1/users/{username} — GetUserProfileQuery
  - [ ] GET /api/v1/users/me — GetUserProfileQuery (authenticated user)
  - [ ] PUT /api/v1/users/me — UpdateProfileCommand

---

## 🧪 Phase 4 Test Gereksinimleri

### Unit Tests

- [ ] **CreatePostCommandValidator** — content ve photos ikisi de boş → hata, PostType = Route + TripId = null → hata
- [ ] **CreateCommentCommandValidator** — boş content → hata
- [ ] **FollowUserCommand** — self-follow → SelfFollowException
- [ ] **UpdateProfileCommandValidator** — Bio 301 karakter → hata

### Integration Tests

- [ ] **Posts_CRUD** — Create (photo tip) → GetById → Update → Delete (soft), her adım doğru
- [ ] **Posts_RouteRequiresTrip** — PostType = Route, TripId = null → 422
- [ ] **Posts_Upvote** — upvote → count arttı, duplicate → 409
- [ ] **Comments_Reply** — post'a comment → comment'e reply → doğru parent ilişkisi
- [ ] **Comments_CrossPostProtection** — A post'unun comment'ine B post'undan reply → hata
- [ ] **Feed_Latest** — 5 post oluştur → feed latest tab → 5 post created_at DESC sıralı
- [ ] **Feed_Following** — A, B'yi takip ediyor, C'yi takip etmiyor → following tab'da sadece B'nin post'ları
- [ ] **Follow_Success** — follow → followers_count arttı, following_count arttı
- [ ] **Follow_SelfFollow_Returns409** — kendini takip → 409
- [ ] **Follow_Unfollow** — unfollow → counter'lar azaldı
- [ ] **UserProfile_GetByUsername** — profil bilgileri doğru, isFollowing bilgisi doğru
- [ ] **UserProfile_Update** — bio güncelle → yeni bio doğru

---

## ✅ Phase 4 Success Metrics

- [ ] 3 tip post oluşturulabiliyor, route tipi trip gerektiriyor
- [ ] Comment + reply (1 seviye) çalışıyor, cross-post koruması aktif
- [ ] Feed 3 tab ile cursor pagination çalışıyor
- [ ] Follow/unfollow counter cache doğru güncelleniyor
- [ ] User profile + update çalışıyor
- [ ] Phase 4 testlerinin tamamı geçiyor

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
**Durum:** ⏳ Bekliyor

**Yapılacaklar:**
- [ ] `ICommunityTipRepositoryAsync.cs` — GetByTripAsync (tripId, upvote_count DESC sıralı, pagination), GetByPlaceInTripAsync (tripId, placeId)
- [ ] `TipResponse.cs` DTO — tip field'ları + user bilgisi + isUpvoted
- [ ] `CreateTipCommand.cs` — TripId zorunlu, PlaceId opsiyonel (null = rotanın geneli, dolu = mekana özel), Content zorunlu (trim boş olamaz)
- [ ] `CreateTipCommandValidator.cs` — Content boş olamaz
- [ ] `DeleteTipCommand.cs` — soft delete, tip owner kontrolü
- [ ] `UpvoteTipCommand.cs` — duplicate engeli, tip upvote_count artır
- [ ] `GetTipsByTripQuery.cs` — trip'in tüm tip'leri, place bilgisi include (varsa), upvote_count DESC sıralı

---

### Task 14.2: WebApi — CommunityTipsController

**Tahmini Süre:** 1 saat  
**Durum:** ⏳ Bekliyor

**Yapılacaklar:**
- [ ] `v1/CommunityTipsController.cs`:
  - [ ] GET /api/v1/trips/{tripId}/tips — GetTipsByTripQuery
  - [ ] POST /api/v1/trips/{tripId}/tips — CreateTipCommand
  - [ ] DELETE /api/v1/tips/{id} — DeleteTipCommand
  - [ ] POST /api/v1/tips/{id}/upvote — UpvoteTipCommand

---

## 📅 Week 15: Karma System — Events, Scoring, Farming Protection

**Hedef:** KarmaService implementasyonu, karma event'leri, puan hesaplama, farming koruması, cache güncelleme

---

### Task 15.1: Application — KarmaService

**Tahmini Süre:** 3 saat  
**Durum:** ⏳ Bekliyor

**Yapılacaklar:**
- [ ] `Application/Interfaces/IKarmaService.cs` — AwardKarmaAsync(userId, actorId, eventType, points, sourceId, sourceType)
- [ ] `Application/Services/KarmaService.cs` implementasyonu:
  - [ ] IApplicationDbContext inject et
  - [ ] AwardKarmaAsync: KarmaEvent entity oluştur, DB'ye ekle
  - [ ] Farming koruması: aynı (userId, sourceId, eventType, actorId) kombinasyonu varsa sessizce skip
  - [ ] users tablosundaki karma_score cache'ini güncelle (+ points)
  - [ ] Puan tablosu: TripPublished = +10, TripForked = +5, TipUpvoted = +2, PostUpvoted = +1, TripUpvoted = +1

---

### Task 15.2: Karma Entegrasyonu — Önceki Handler'lar

**Tahmini Süre:** 2 saat  
**Durum:** ⏳ Bekliyor

**Yapılacaklar:**
- [ ] `PublishTripCommand` handler'ına ekle: KarmaService.AwardKarmaAsync (trip owner'a +10, source = trip)
- [ ] `ForkTripCommand` handler'ına ekle: KarmaService.AwardKarmaAsync (orijinal trip owner'a +5, actor = fork yapan, source = trip)
- [ ] `UpvotePostCommand` handler'ına ekle: KarmaService.AwardKarmaAsync (post owner'a +1, actor = upvote yapan, source = post)
- [ ] `UpvoteTipCommand` handler'ına ekle: KarmaService.AwardKarmaAsync (tip owner'a +2, actor = upvote yapan, source = tip)
- [ ] `UpvoteTripCommand` handler'ına ekle: KarmaService.AwardKarmaAsync (trip owner'a +1, actor = upvote yapan, source = trip)

---

### Task 15.3: Application — Karma Query & Controller

**Tahmini Süre:** 1 saat  
**Durum:** ⏳ Bekliyor

**Yapılacaklar:**
- [ ] `GetKarmaHistoryQuery.cs` — authenticated user'ın karma event geçmişi, created_at DESC, pagination
- [ ] `KarmaEventResponse.cs` DTO — EventType, Points, SourceType, CreatedAt, ActorUsername (nullable)
- [ ] `v1/KarmaController.cs`:
  - [ ] GET /api/v1/karma/history — GetKarmaHistoryQuery

---

## 📅 Week 16: Notifications — Polymorphic, Mark Read, Unread Count

**Hedef:** NotificationService, bildirim oluşturma, listeleme, okundu işaretleme, okunmamış sayısı

---

### Task 16.1: Application — NotificationService

**Tahmini Süre:** 3 saat  
**Durum:** ⏳ Bekliyor

**Yapılacaklar:**
- [ ] `Application/Interfaces/INotificationService.cs` — CreateNotificationAsync(userId, actorId, type, targetId, targetType)
- [ ] `Application/Services/NotificationService.cs` implementasyonu:
  - [ ] IApplicationDbContext inject et
  - [ ] CreateNotificationAsync: Notification entity oluştur, DB'ye ekle
  - [ ] Follow bildirimi: target null (follow_has_no_target kuralı)
  - [ ] Upvote bildirimleri: target = ilgili içerik (post, comment, tip, trip)
  - [ ] Comment bildirimi: target = post
  - [ ] Mention bildirimi: target = post veya comment
  - [ ] Fork bildirimi: target = trip
  - [ ] Self-notification engeli: actor = user ise bildirim oluşturma

---

### Task 16.2: Notification Entegrasyonu — Önceki Handler'lar

**Tahmini Süre:** 2 saat  
**Durum:** ⏳ Bekliyor

**Yapılacaklar:**
- [ ] `FollowUserCommand` handler'ına ekle: NotificationService → follow bildirimi
- [ ] `UpvotePostCommand` handler'ına ekle: NotificationService → post_upvote bildirimi (self değilse)
- [ ] `UpvoteCommentCommand` handler'ına ekle: NotificationService → comment_upvote bildirimi
- [ ] `UpvoteTipCommand` handler'ına ekle: NotificationService → tip_upvote bildirimi
- [ ] `UpvoteTripCommand` handler'ına ekle: NotificationService → trip_upvote bildirimi
- [ ] `CreateCommentCommand` handler'ına ekle: NotificationService → comment bildirimi (post owner'a) + mention bildirim'leri (mentions listesindeki user'lara)
- [ ] `ForkTripCommand` handler'ına ekle: NotificationService → fork bildirimi

---

### Task 16.3: Application — Notification Queries & Commands

**Tahmini Süre:** 2 saat  
**Durum:** ⏳ Bekliyor

**Yapılacaklar:**
- [ ] `INotificationRepositoryAsync.cs` — GetByUserAsync (userId, is_read filtresi opsiyonel, created_at DESC, pagination), GetUnreadCountAsync (userId)
- [ ] `NotificationResponse.cs` DTO — Id, Type, TargetId, TargetType, IsRead, ReadAt, CreatedAt, Actor bilgisi (Username, ProfilePhotoUrl)
- [ ] `GetNotificationsQuery.cs` — authenticated user'ın bildirimleri, pagination
- [ ] `GetUnreadCountQuery.cs` — authenticated user'ın okunmamış bildirim sayısı
- [ ] `MarkAsReadCommand.cs` — notification owner kontrolü, is_read = true + read_at = now
- [ ] `MarkAllAsReadCommand.cs` — authenticated user'ın tüm okunmamış bildirimlerini okundu yap
- [ ] `v1/NotificationsController.cs`:
  - [ ] GET /api/v1/notifications — GetNotificationsQuery
  - [ ] GET /api/v1/notifications/unread-count — GetUnreadCountQuery
  - [ ] POST /api/v1/notifications/{id}/read — MarkAsReadCommand
  - [ ] POST /api/v1/notifications/read-all — MarkAllAsReadCommand

---

## 🧪 Phase 5 Test Gereksinimleri

### Unit Tests

- [ ] **CreateTipCommandValidator** — boş content → hata
- [ ] **KarmaService** — AwardKarmaAsync doğru event oluşturuyor, puan tablosu doğru, farming koruması çift puan engelliyor
- [ ] **NotificationService** — self-notification oluşturmuyor, follow bildirimi target null

### Integration Tests

- [ ] **Tips_CRUD** — tip oluştur → trip'in tip listesinde görünüyor → sil → görünmüyor
- [ ] **Tips_PlaceSpecific** — place_id ile tip → GetTipsByTrip'te place bilgisi dolu
- [ ] **Karma_PublishTrip** — trip publish → karma_events'te +10, user karma_score güncellendi
- [ ] **Karma_ForkTrip** — fork → orijinal owner'a +5
- [ ] **Karma_Farming** — aynı trip'i publish-unpublish-publish → sadece 1 kez +10
- [ ] **Karma_DuplicateUpvote** — aynı post'a 2 kez upvote → sadece 1 kez +1 karma
- [ ] **Notifications_Follow** — A, B'yi takip etti → B'nin bildirimlerinde "A seni takip etti"
- [ ] **Notifications_Upvote** — A, B'nin post'unu upvote etti → B'ye bildirim
- [ ] **Notifications_SelfUpvote** — A, kendi post'unu upvote etti → bildirim oluşmadı
- [ ] **Notifications_Comment** — A, B'nin post'una yorum yaptı → B'ye bildirim
- [ ] **Notifications_Mention** — A, C'yi mention etti → C'ye bildirim
- [ ] **Notifications_MarkRead** — bildirim okundu → is_read = true, read_at dolu
- [ ] **Notifications_UnreadCount** — 3 okunmamış → count = 3, 1 okundu → count = 2

---

## ✅ Phase 5 Success Metrics

- [ ] Community tip: rota geneli + mekana özel oluşturma, upvote, listeleme çalışıyor
- [ ] Karma: publish +10, fork +5, upvote +1/+2 doğru hesaplanıyor
- [ ] Farming koruması: unique index + uygulama kontrolü çift puan engelliyor
- [ ] Bildirim: 8 tip (follow, post_upvote, comment_upvote, tip_upvote, trip_upvote, comment, mention, fork) oluşuyor
- [ ] Self-notification engelleniyor
- [ ] Unread count doğru, mark read çalışıyor
- [ ] Phase 5 testlerinin tamamı geçiyor

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
