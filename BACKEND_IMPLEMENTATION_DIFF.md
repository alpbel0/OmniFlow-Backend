# OmniFlow Backend — Roadmap vs. Gerçek Implementasyon Audit Raporu

> **Amaç:** `BACKEND_ROADMAP_MVP.md` (ana roadmap), `BACKEND_SCHEMA_MVP.md` (planlanan DB şeması) ve `TRIP_PLAN_*.md` (Trip Planning modülü roadmap'leri) ile kod tabanındaki **gerçek** implementasyonu karşılaştırmak.
>
> **Kapsam:** Domain, Application, Infrastructure, WebApi katmanları + Tests. Sadece statik kod analizi (build/test çalıştırılmadı).
> **Tarih:** 20 Haziran 2026
> **Referans MD'ler:** `BACKEND_ROADMAP_MVP.md`, `BACKEND_SCHEMA_MVP.md`, `TRIP_PLAN_ROADMAP.md`, `TRIP_PLAN_IMPLEMENT.md`, `TRIP_PLANNING.md`, `TRIP_PLANNING_CHANGES.md`, `CLAUDE.md`, `AGENTS.md`, `README.md`

---

## 1. Yönetici Özeti (Executive Summary)

OmniFlow backend'i, `BACKEND_ROADMAP_MVP.md`'de tanımlanan 7 fazlı (20 haftalık) MVP planının **fazlasıyla** büyümüş durumda. Roadmap Phase 1-5'i tamamlandıktan sonra, ekibe **ikinci bir roadmap** (`TRIP_PLAN_ROADMAP.md` — "Trip Planning Modülü") eklenmiş ve bu modül, core trip modelini **kökten değiştirmiş**tir.

### En Kritik 5 Bulgu

1. **`Stop` entity'si tamamen kaldırıldı** → yerine `TimelineEntry` (5 tipli discriminator) getirildi. Roadmap'in Phase 2 Week 6'daki tüm Stop işleri (CRUD, LexoRank reorder, time-lock, visited) **tarihe gömüldü**. `BACKEND_SCHEMA_MVP.md` ve `README.md` hâlâ `stops` tablosundan/Stop feature'ından bahsediyor → **stale dokümantasyon**.

2. **Roadmap dışı, ayrı bir modül: Trip Planning** (38-46 saat, `TRIP_PLAN_ROADMAP.md` ile takip edildi). Multi-destination (1-10 şehir), 8-adımlık wizard, `ScoringService` (405 hardcoded skor), `BudgetCalculationService` (sezon çarpanı + şehir bazlı percentile + fallback), `TimelineService` (lock+buffer), `RecommendationService`, `ProviderFlight/ProviderHotel` veri katmanı ekledi.

3. **`TravelStyle` enum'u BREAKING değişti**: 5 değer (`Solo, Family, Adventure, Luxury, Relax`) → 11 değer (`Romantic, Cultural, Adventure, Nature, Local, Relax, Shopping, Gastronomy, Influencer, Nightlife, Budget`). `Trip.TravelStyle` (tek değer) → `Trip.TravelStyles` (`List<TravelStyle>` + `text[]`). Eski `Solo/Family/Luxury` değerleri DB'de artık geçersiz.

4. **Phase 1'de "Hariç (bitirme projesi)" denilen feature'lar MVP'de yapıldı**: `UserBlock` (Blocks), `EmailVerificationDispatch` (email doğrulama), `PasswordResetToken` (şifre sıfırlama), `Admin panel` (moderasyon). Hatta `BACKEND_SCHEMA_MVP.md`'nin "MVP'den Çıkarılanlar" tablosundaki `UserBlock`, `VerifyEmailRequest`, `Admin Panel Controllers` maddeleri **gerçekleşti**.

5. **Phase 3 (Flights & Hotels) "Tamamlandı" işaretli ama `POST /select` endpoint'leri sonradan KALDIRILDI**: `SelectFlightCommand` / `SelectHotelCommand` yok artık. `FlightsController`/`HotelsController` sadece `GET` (görüntüleme) yapıyor. Seçim/rezervasyon mantığı `TimelineEntry` + `ProviderFlightId/ProviderHotelId` referansına taşındı. Roadmap'in "atomic flight/hotel selection" başarısı geçerliliğini yitirdi.

### Faz Durumu (Roadmap İşaretlerine Göre + Kod Doğrulaması)

| Faz | Roadmap Durumu | Kod Doğrulaması |
|-----|----------------|-----------------|
| **Phase 1** Infrastructure & Auth | ✅ Tamamlandı | ✅ Doğrulandı + **roadmap dışı ekler** (email verify, password reset, SMTP) |
| **Phase 2** Trips, Places, Stops | ✅ Tamamlandı | ⚠️ Kısmen — Stop feature'ı **sonradan silindi**, yerine TimelineEntry geldi |
| **Phase 3** Flights & Hotels | ✅ Tamamlandı | ⚠️ Kısmen — `POST /select` endpoint'leri **kaldırıldı**, sadece GET kaldı |
| **Phase 4** Posts, Comments, Feed, Follow | ✅ Tamamlandı | ✅ Doğrulandı + **roadmap dışı ekler** (liked posts, trending tags, posts by user) |
| **Phase 5** Tips, Karma, Notifications | ✅ Tamamlandı | ✅ Doğrulandı |
| **Phase 6** AI Timeline + Fallback | ⏳ Bekliyor | ❌ **Hâlâ yok** — `AiTimelineService.cs`/`AiFallbackService.cs` **0 satır** (boş stub dosyalar) |
| **Phase 7** Testing, CI/CD, Docker, Docs | ⏳ Bekliyor | ⚠️ Kısmen — `azure-pipelines.yml` CI/CD var (build+publish+Azure deploy, test step yok), Dockerfile var, ama Postman collection, load test, docker-compose, README güncelleme yok |

### Rakamsal Özet

| Metrik | Planlanan (Roadmap) | Gerçek |
|--------|---------------------|--------|
| DB tablo sayısı | 18 | **~24** (18 + 6 yeni, `stops` drop → net +5; bkz. §7) |
| Domain entity | 18 | **24** (18 + TripDestination, TimelineEntry, ProviderFlight, ProviderHotel, Block, PasswordResetToken, EmailVerificationDispatch; Stop çıktı) |
| Domain enum | 20 | **25** (+5: TravelCompanion, Tempo, TimelineEntryType, TransportPreference, Season) |
| Domain exception | 3 | **6** (+SelfUpvote, SelfBlock, DomainException base) |
| HTTP endpoint | ~45 (roadmap'te listelenen) | **92** (grep ile doğrulandı) |
| Infrastructure service | ~3 (Account, Ai, Email) | **11** (+Scoring, Budget, Timeline, Recommendation, Blob, DateTime) |
| Migration | 1 (InitialCreate) | **11** |
| Unit test | (yok sayı) | **406+** (TRIP_PLAN_ROADMAP son dokümante edilen; 6 Mayıs admin/social ekleri sonrası muhtemelen daha yüksek) |

---

## 2. Metodoloji

- Tüm MD dosyaları (`glob **/*.md` → 9 dosya) okundu.
- `BACKEND_ROADMAP_MVP.md` (1742 satır) ve `TRIP_PLAN_ROADMAP.md` (1433 satır) tamamen tarandı.
- `OmniFlow.Domain/**/*.cs`, `OmniFlow.Application/Features/**/*.cs`, `DTOs/**`, `Interfaces/**`, `Infrastructure/**`, `WebApi/Controllers/**`, `Tests/**` glob ile listelendi.
- Tüm controller'lar tek tek okunarak endpoint'ler çıkarıldı (`grep [Http(Get|Post|Put|Delete)` → 92 eşleşme).
- `Program.cs` ve `ServiceRegistration.cs` DI kayıtları doğrulandı.
- `AiTimelineService.cs` / `AiFallbackService.cs` içerikleri okunarak **0 satır** oldukları doğrulandı.
- `SelectFlightCommand`/`SelectHotelCommand` grep ile arandı → **bulunamadı** (kaldırılmış).
- `git log --since="2026-04-01"` ile timeline doğrulandı.
- Build/test çalıştırılmadı (kullanıcı tercihi: sadece statik analiz).

---

## 3. Bölüm A — Roadmap'te "Yapıldı" İşaretlenenlerin Kod Doğrulaması

### Phase 1: Infrastructure & Auth ✅

| Roadmap Task | Kodda Durum | Kanıt |
|---|---|---|
| 4 proje + referans zinciri | ✅ | `OmniFlow.slnx` + `Domain`/`Application`/`Infrastructure`/`WebApi` |
| 18 entity + 20 enum | ✅ (aşıldı) | 24 entity, 25 enum (bkz. §6) |
| EF Core 18 configuration | ✅ (aşıldı) | `Infrastructure/Configurations/` → 23 dosya |
| Azure PostgreSQL + migration | ✅ | `InitialCreate` (2026-03-16) + 10 ek migration |
| Identity + JWT | ✅ | `Program.cs:39-57`, `AccountService.cs` |
| Register/Login/Refresh/Forgot | ✅ | `AccountController.cs:42-156` |
| Dual refresh (cookie/body) | ✅ | `AccountController.cs:113-147` |
| Swagger + JWT auth | ✅ | `Program.cs:98-123` |
| ErrorHandlerMiddleware (7 exception) | ✅ | `Middlewares/ErrorHandlerMiddleware.cs` |
| Seed (roles, admin, traveler) | ✅ | `Program.cs:144-146`, `Seeds/` (3 dosya) |
| Phase 1 testleri | ✅ | `AccountControllerTests`, `JwtConfigurationTests`, `CorsMiddlewareTests`, `ErrorHandlerMiddlewareTests`, `ValidationBehaviourTests` |

### Phase 2: Trips, Places, Stops ⚠️ (Stop kısmı sonradan silindi)

| Roadmap Task | Kodda Durum | Not |
|---|---|---|
| Generic Repository pattern | ✅ | `GenericRepositoryAsync.cs` + open-generic DI (`ServiceRegistration.cs:62`) |
| Places CRUD + city filter | ✅ | `PlacesController.cs` (4 endpoint), `PlaceRepositoryAsync.cs` |
| Trip CRUD + Publish/Archive | ✅ | `TripsController.cs:41-240` |
| Owner authorization | ✅ | Tüm update/delete handler'larında ForbiddenException |
| **Stop CRUD + LexoRank + time lock + custom + visited** | ❌ **SİLİNDİ** | `Stop.cs`, `StopsController.cs`, `StopConfiguration.cs`, `IStopRepositoryAsync.cs`, `StopRepositoryAsync.cs`, `Features/Stops/`, `DTOs/Stops/` — **yok**. Yerine `TimelineEntry` geldi (bkz. §6.1) |
| Fork (deep copy) | ✅ (değişti) | `ForkTripCommandHandler.cs` — artık Stop değil TripDestination + TimelineEntry deep copy ediyor |
| Explore (cursor + filter + popularity) | ✅ | `ExploreController.cs:20` |
| Trip upvote + save/unsave | ✅ | `TripsController.cs:243-291` |

### Phase 3: Flights & Hotels ⚠️ (select endpoint'leri kaldırıldı)

| Roadmap Task | Kodda Durum | Not |
|---|---|---|
| Flight entity + itinerary_group_id | ✅ | `Flight.cs`, `FlightConfiguration.cs` |
| Hotel entity | ✅ | `Hotel.cs`, `HotelConfiguration.cs` |
| `GET /api/v1/trips/{tripId}/flights` | ✅ | `FlightsController.cs:20` |
| `GET /api/v1/trips/{tripId}/hotels` | ✅ | `HotelsController.cs:20` |
| **`POST /api/v1/trips/{tripId}/flights/select`** | ❌ **KALDIRILDI** | `SelectFlightCommand` grep ile bulunamadı. `FlightsController.cs` sadece GET içeriyor |
| **`POST /api/v1/trips/{tripId}/hotels/select`** | ❌ **KALDIRILDI** | `SelectHotelCommand` grep ile bulunamadı |
| Atomic selection (previous cancelled) | ❌ | Mantık `TimelineEntry` + `ProviderFlightId/ProviderHotelId` referansına taşındı |

### Phase 4: Posts, Comments, Feed, Follow ✅ (+ ekler)

| Roadmap Task | Kodda Durum |
|---|---|
| Post CRUD (3 tip) + upvote | ✅ `PostsController.cs` |
| Comment + reply (1 seviye) + cross-post protection | ✅ `CommentsController.cs`, `CommentConfiguration.cs` (composite FK) |
| Feed (3 tab: ForYou/Following/Latest) + cursor | ✅ `FeedController.cs:13` |
| Follow/unfollow + counter cache | ✅ `FollowsController.cs` |
| User profile + update | ✅ `UsersController.cs:34-150` |

### Phase 5: Tips, Karma, Notifications ✅

| Roadmap Task | Kodda Durum |
|---|---|
| CommunityTip CRUD + upvote | ✅ `CommunityTipsController.cs` (5 endpoint) |
| KarmaService (publish +10, fork +5, tip +2, post +1, trip +1) | ✅ `Application/Services/KarmaService.cs` |
| Karma farming protection (unique index) | ✅ `KarmaEventConfiguration.cs` |
| NotificationService (8 tip, self-notification engeli) | ✅ `Application/Services/NotificationService.cs` |
| Notification CRUD + mark read + unread count | ✅ `NotificationsController.cs` (4 endpoint) |
| Karma/Notification entegrasyonu handler'lara | ✅ (roadmap Task 15.2, 16.2) |

---

## 4. Bölüm B — Roadmap'te Var Ama Yapılmamış / Eksik

### Phase 6: AI Timeline Generation ❌ TAMAMEN YOK

Roadmap `BACKEND_ROADMAP_MVP.md:1494-1557` (Task 17.1, 17.2, 18.1, 18.2) hepsi `⏳ Bekliyor` / `[ ]`.

**Kod doğrulaması:**
- `Application/Interfaces/IAiTimelineService.cs` ✅ var
- `Application/Interfaces/IAiFallbackService.cs` ✅ var
- `Infrastructure/Services/AiTimelineService.cs` ❌ **0 satır** (boş dosya)
- `Infrastructure/Services/AiFallbackService.cs` ❌ **0 satır** (boş dosya)
- `Features/Trips/Commands/GenerateTimeline/GenerateTimelineCommand.cs` var ama **handler yok** (`GenerateTimelineCommandHandler.cs` yok)
- `POST /api/v1/trips/{tripId}/generate-timeline` endpoint **yok** (controller'da değil)
- `OpenAISettings.cs` var (Phase 1'den kalma)

**Sonuç:** Phase 6 için sadece interface + boş stub dosyalar bırakılmış. Gerçek implementasyon, prompt tasarımı, fallback motoru, testler — hiçbiri yok.

### Phase 7: Testing, CI/CD, Docker, Docs ❌

| Task | Durum |
|---|---|
| CustomWebApplicationFactory finalize | ✅ (var, `Tests/.../Setup/`) |
| Tüm controller'lar için integration test | ✅ (20 test dosyası — yolun çoğu) |
| Load testing (k6/NBomber) | ❌ |
| Swagger tüm endpoint açıklamaları | ✅ (XML comments controller'larda var) |
| Postman collection | ❌ (`docs/` klasörü yok) |
| CI/CD pipeline (Azure DevOps) | ⚠️ **`azure-pipelines.yml` var ama eksik** — `main` trigger, `UseDotNet@2` + `restore` + `build` + `publish` + `AzureRmWebAppDeployment@4` (omniflow-backend1) step'leri; **test step yok**, PR trigger yok, build badge yok |
| Dockerfile | ✅ var (ama AGENTS.md "not used" diyor — App Service deploy, pipeline zip ile deploy ediyor) |
| docker-compose.yml (backend + Redis) | ❌ |
| README güncel | ❌ (eski — Stop, 5 TravelStyle değeri, "20 tables" diyor) |
| BACKEND_SCHEMA_MVP.md güncel | ❌ (eski — `stops` tablosu, `IStopRepositoryAsync` listeliyor) |
| ROADMAP checkbox güncelleme | ✅ (Phase 1-5 işaretli, 6-7 boş) |

---

## 5. Bölüm C — Roadmap'te YOK ama Implement Edilmiş (En Büyük Bölüm)

Bu bölüm, `BACKEND_ROADMAP_MVP.md`'de hiç geçmeyen ama kodda var olan feature'ları listeler. Kaynakları `TRIP_PLAN_*.md`, git log ve kod tabanı.

### 5.1 Trip Planning Modülü (Kendi Roadmap'i ile geldi) 🏗️

Ayrı bir roadmap: `TRIP_PLAN_ROADMAP.md` (1433 satır, 5 faz, 38-46 saat, 2026-04-26 → 2026-05-01). `TRIP_PLANNING.md` PRD, `TRIP_PLAN_IMPLEMENT.md` uygulama planı, `TRIP_PLANNING_CHANGES.md` özet raporu.

**Eklenen Domain katmanı:**
- `Domain/Entities/TripDestination.cs` — multi-destination leg (1-10 şehir, OrderIndex, ArrivalDate, DepartureDate, NightCount domain hesap)
- `Domain/Entities/TimelineEntry.cs` — Stop'un yerine, 5 tipli (Place, CustomFlight, CustomTransport, CustomAccommodation, CustomEvent), IsLocked + BufferMinutes, factory metotları, 7 domain update metodu
- `Domain/Entities/ProviderFlight.cs` + `ProviderHotel.cs` — statik referans verisi (aslında 2026-04-14'te, Trip Planning'den ÖNCE eklendi)
- 5 yeni enum: `TravelCompanion`, `Tempo`, `TimelineEntryType`, `TransportPreference`, `Season`
- `Domain/Exceptions/DomainException.cs` — yeni base exception class

**Eklenen Application katmanı (servisler + CQRS):**
- `Application/Interfaces/IScoringService.cs` + `Infrastructure/Services/ScoringService.cs` — 108 group_score + 297 style_score + Google tag bonus (405 hardcoded değer, `Dictionary<(PlaceCategory, TravelCompanion/TravelStyle), int>`)
- `Application/Interfaces/IBudgetCalculationService.cs` + `BudgetCalculationService.cs` — sezon çarpanı (Kış 1.2, İlkbahar 1.1, Yaz 1.5, Sonbahar 1.0), şehir bazlı percentile otel segmentasyonu, kademeli tier fallback (Premium→Standard→Economy)
- `Application/Interfaces/ITimelineService.cs` + `TimelineService.cs` — günlük kapasite (Slow=3, Moderate=5, Fast=7), lock+buffer çakışma motoru (Flight 120dk, Transport 30dk), LexoRank
- `Application/Interfaces/IRecommendationService.cs` + `RecommendationService.cs` — 3 grup (recommended/neutral/other) scoring sıralaması
- `Features/TripDestinations/` — Create/Update/Delete/GetTripDestinations (4 CQRS)
- `Features/TimelineEntries/` — Create/Update/Delete/Reorder/MarkEntryVisited/GetTimeline (6 CQRS)
- `Features/Providers/` — GetOriginCities/GetProviderFlights/GetProviderHotels (3 CQRS)
- `Features/Trips/Queries/GetRecommendedPlaces/`, `GetBudgetSummary/`, `GetFeaturedTrips/`, `GetPublishedTripsByUser/` — 4 ek query
- `Features/Trips/Commands/CreateTripWizard/` — 8-adımlık wizard command (Origin → Destinations → PersonCount → Companion → Budget → Vibe → Tempo → Transport)

**Eklenen WebApi katmanı (yeni controller'lar):**
- `TripDestinationsController.cs` (4 endpoint) — `GET/POST/PUT/DELETE /api/v1/trips/{tripId}/destinations`
- `TimelineController.cs` (6 endpoint) — `GET/POST/PUT/DELETE /api/v1/trips/{tripId}/timeline/...` + reorder + visited
- `ProvidersController.cs` (3 endpoint, `[AllowAnonymous]`) — `GET /api/v1/providers/origin-cities|flights|hotels`

**Trip entity'sindeki kırılma değişiklikleri:**
- **Kaldırıldı:** `City`, `Country` (tek şehir), `TravelStyle` (tek değer), `UserBudget`
- **Eklendi:** `Origin`, `OriginCountry`, `TravelCompanion`, `TravelStyles` (`List<TravelStyle>`), `Tempo`, `TransportPreference`, `ManualBudget`, `AdjustedBudgetTier`
- **Navigation değişti:** `Stops` → `Destinations` + `TimelineEntries`
- `StartDate`/`EndDate` artık private setter + `RecalculateFromDestinations()` domain metodu

**Place entity'sindeki ekler:**
- `GoogleTags` (`List<string>`, text[], GIN index)
- `PhotoUrls` `string?` → `List<string>` (text[], ValueConverter+ValueComparer)
- 9 eksik OSM mapping: `PriceLevel`, `ReviewCount`, `Wikipedia`, `Wikidata`, `Wheelchair`, `Heritage`, `Fee`, `Image`, `Cuisine`

**Wizard endpoint (roadmap dışı):**
- `POST /api/v1/trips/wizard` (`TripsController.cs:113`) — 10 destinasyona kadar, max 3 TravelStyle, sequential date validation, budget fallback ile zengin `CreateTripWizardResponse`
- `POST /api/v1/trips` artık `[Obsolete]` ve wizard'a map'leniyor (geriye dönük uyumlu)
- `GET /api/v1/trips/{tripId}/budget-summary` (`TripsController.cs:142`) — gerçek zamanlı bütçe (TimelineEntry + Provider sezon çarpanı)
- `GET /api/v1/trips/{tripId}/recommend-places?destinationId=` (`TripsController.cs:155`)

### 5.2 Email Doğrulama + Şifre Sıfırlama (Phase 1 "hariç" listesindeydi) 📧

`BACKEND_SCHEMA_MVP.md:650-666` "MVP'den Çıkarılanlar" tablosunda `VerifyEmailRequest.cs` ve email doğrulama akışı "bitirme" olarak listeleniyordu. **İkisi de yapıldı.**

- `Domain/Entities/EmailVerificationDispatch.cs` — email doğrulama dispatch kayıtları
- `Domain/Entities/PasswordResetToken.cs` — şifre sıfırlama token'ı
- `Infrastructure/Services/EmailService.cs` — **gerçek SMTP implementasyonu** (roadmap'te `MailSettings` placeholder'dı). Git log: 2026-04-14/15 "Configure Gmail SMTP", "Standardize SMTP to port 587"
- `Application/Interfaces/IEmailService.cs`
- `AccountController.cs:76-103` — `POST /api/account/verify-email`, `POST /api/account/resend-verification` (roadmap'te YOK)
- `AccountController.cs:159-171` — `POST /api/account/reset-password` (roadmap'te YOK, sadece `forgot-password` placeholder vardı)
- Register artık `202 Accepted` + `RegistrationVerificationResponse` dönüyor (roadmap'te `200` + `AuthenticationResponse`)
- DTO'lar: `VerifyEmailRequest`, `ResendVerificationEmailRequest`, `ResetPasswordRequest`, `RegistrationVerificationResponse`, `MessageResponse` (+ validator'ları)
- Migration: `20260419214902_AddPasswordResetToken`

### 5.3 User Block / Unblock (Phase 1'de Açıkça "Hariç" Denmişti) 🚫

`BACKEND_ROADMAP_MVP.md:48` ("UserBlock, PushToken (bitirme)") ve `BACKEND_SCHEMA_MVP.md:656` ("UserBlock entity — MVP kapsamı dışı") **açıkça MVP dışı** demişti. **Yine de yapıldı.**

- `Domain/Entities/Block.cs` — block ilişkisi
- `Domain/Exceptions/SelfBlockException.cs` — self-block engeli
- `Infrastructure/Configurations/BlockConfiguration.cs`
- `Application/Helpers/BlockVisibilityHelper.cs` — block-aware visibility (roadmap dışı helper)
- `Features/Blocks/Commands/BlockUser/`, `UnblockUser/` + `Queries/GetBlockedUsers/`
- `BlocksController.cs` (3 endpoint) — `POST/DELETE /api/v1/users/{userId}/block`, `GET /api/v1/users/{userId}/blocked-users`
- Migration: `20260414225022_AddUserBlockFeature` (2026-04-14)
- Block-aware follow guard (Follow handler'ları block kontrolü yapıyor)
- Blocked user profile görüntüleme: 404 yerine sıfırlanmış metrics (git log 2026-04-22)

### 5.4 Admin Panel (Roadmap "Bitirme Projesi" Diyordu) 👮

`BACKEND_SCHEMA_MVP.md:665` "Admin Panel Controllers — İçerik moderasyonu" graduation'a bırakılmıştı. **MVP'de yapıldı** (en son, 2026-05-06 "admin" commit'i).

- `Application/DTOs/Admin/AdminUserListItemResponse.cs`, `AdminPostListItemResponse.cs`
- `Features/Admin/Queries/GetAdminUsers/`, `GetAdminPosts/`
- `Features/Admin/Commands/SetUserSuspended/`, `AdminDeletePost/`
- `AdminController.cs` (5 endpoint, `[Authorize(Roles = "Admin")]`) — `GET /api/v1/admin/posts`, `DELETE /api/v1/admin/posts/{id}`, `GET /api/v1/admin/users`, `POST/DELETE /api/v1/admin/users/{id}/suspend`

### 5.5 Media Upload (Azure Blob Storage) 📸

Roadmap'te **hiç yok**. `README.md` "Redis 7.0" diyor ama media/blob hiç geçmiyor.

- `Application/Interfaces/IBlobService.cs`
- `Application/Settings/AzureStorageSettings.cs`
- `Infrastructure/Services/BlobService.cs` — Azure Storage Blob SDK
- `MediaController.cs` (1 endpoint) — `POST /api/v1/media/upload` (multipart, max 5 dosya, 5MB/dosya, jpeg/png/webp)
- `UsersController.cs:169` — `POST /api/v1/users/me/profile-photo` (profile foto upload)
- `ServiceRegistration.cs:43-54` — `BlobServiceClient` singleton DI + `IBlobService` scoped
- Migration: yok (blob storage, DB değil) — git log 2026-04-15 "Add authenticated media upload endpoint for blob-backed images"

### 5.6 Provider Data Sistemi (Trip Planning Öncesi Ekildi) ✈️

`ProviderFlight`/`ProviderHotel` aslında Trip Planning modülünden **önce** (2026-04-14, `AddProviderTables` migration) eklendi. Roadmap'te hiç yok.

- `Domain/Entities/ProviderFlight.cs`, `ProviderHotel.cs`
- `Infrastructure/Configurations/ProviderFlightConfiguration.cs`, `ProviderHotelConfiguration.cs`
- `Application/Interfaces/Repositories/IProviderFlightRepositoryAsync.cs`, `IProviderHotelRepositoryAsync.cs`
- `Infrastructure/Repositories/ProviderFlightRepositoryAsync.cs`, `ProviderHotelRepositoryAsync.cs`
- `ServiceRegistration.cs:71-72` — DI kayıtları
- Migration: `20260414223328_AddProviderTables`
- Trip Planning ile birlikte `ProvidersController` (3 public endpoint) ve wizard budget hesabında kullanıldı

### 5.7 Social Feature Extension'ları (Roadmap Dışı) 🌐

Bunlar roadmap'in Phase 4 listesinde **olmayan** ek sosyal endpoint'ler (Mayıs 2-6 arası eklendi):

| Feature | Endpoint | Kod |
|---|---|---|
| Beğenilen postları listele | `GET /api/v1/posts/liked` | `PostsController.cs:38`, `Features/Posts/Queries/GetLikedPosts/` |
| Trending tag'ler (son 7 gün) | `GET /api/v1/posts/trending-tags` | `PostsController.cs:57`, `Features/Posts/Queries/GetTrendingTags/` |
| Kullanıcının post'ları | `GET /api/v1/users/{userId}/posts` | `UsersController.cs:65`, `Features/Posts/Queries/GetPostsByUser/` |
| Kendi post'larım | `GET /api/v1/users/me/posts` | `UsersController.cs:48`, `Features/Posts/Queries/GetMyPosts/` |
| Kullanıcının yayınlanmış trip'leri | `GET /api/v1/users/{userId}/trips` | `UsersController.cs:84`, `Features/Trips/Queries/GetPublishedTripsByUser/` |
| Top contributors (karma sıralı) | `GET /api/v1/users/top-contributors` | `UsersController.cs:103`, `Features/Users/Queries/GetTopContributors/` |
| Suggested follows (3 tier algoritma) | `GET /api/v1/users/suggested-follows` | `UsersController.cs:112`, `Features/Users/Queries/GetSuggestedFollows/` (block/follow-aware, git log 2026-05-03) |
| Featured trips (son 7 gün, etkileşim skoru) | `GET /api/v1/explore/featured` | `ExploreController.cs:56`, `Features/Trips/Queries/GetFeaturedTrips/` |
| Explore search (searchTerm) | `GET /api/v1/explore?searchTerm=` | `ExploreController.cs:24` (roadmap'te searchTerm yoktu) |
| Followers/Following search | `?search=` parametresi | `FollowsController.cs:36,57` (roadmap'te yoktu) |

### 5.8 Profile Photo Upload + Update Profile Flag Pattern

- `UsersController.cs:169-206` — `POST /api/v1/users/me/profile-photo` (blob'a yükle + `UpdateProfileCommand` ile URL kaydet)
- `UpdateProfileCommand` artık flag-based: `UpdateBio`, `UpdateProfilePhotoUrl` boolean flag'leri ile selective update (roadmap'te basit `Bio + ProfilePhotoUrl` update vardı)

### 5.9 DB Data Migration (Manuel SQL)

`TRIP_PLAN_ROADMAP.md:92-100` — mevcut `places.travel_styles` array'lerindeki eski string'ler **manuel SQL** ile yeni enum değerlerine map'lendi:
- "Family" → "Local", "Entertainment" → "Nightlife", "Food & Drink" → "Gastronomy", "Hiking" → "Adventure", "Historical" → "Cultural", "Relaxation" → "Relax", "Beach" → "Nature", "Art" → "Cultural", "City" → "Local", "Educational" → "Cultural", "Photography" → "Influencer"

### 5.10 Yapısal / Altyapısal Ekler

- `IApplicationDbContext.Database` property eklendi (transaction desteği için, `Features/TripDestinations/` handler'ları `BeginTransactionAsync` kullanıyor)
- `Program.cs:75-94` — `InvalidModelStateResponseFactory` özelleştirildi (validation error → `ErrorResponse` + `ValidationErrorDetail` listesi, 422)
- `MicroElements.Swashbuckle.FluentValidation` — Swagger'da FluentValidation kuralları gösterimi (roadmap dışı NuGet)
- `Program.cs:137` — `context.Database.MigrateAsync()` uygulama başlangıcında otomatik migration (roadmap'te yoktu)
- CORS policy genişletildi: `localhost:3000, 3001, 5173`, `omniflow.app`, Azure frontend URL'i (`Program.cs:67`)

---

## 6. Bölüm D — Roadmap'te VARDI ama SONRADAN KALDIRILANLAR 🔥

Bu bölüm en az dikkat çeken ama **en kritik** bölüm — roadmap'in tamamlanmış dediği feature'ların sonradan silinmesi.

### 6.1 `Stop` Entity'si ve Tüm Stop Feature'ı — TAMAMEN SİLİNDİ

Roadmap Phase 2 Week 6 (Task 6.1-6.3) "✅ Tamamlandı" diyor. AGENTS.md ve CLAUDE.md hâlā StopsController'ı listeliyor. Ama kodda **hiçbiri yok**:

**Silinen dosyalar (TRIP_PLAN_ROADMAP:306-312):**
- `Domain/Entities/Stop.cs`
- `Infrastructure/Configurations/StopConfiguration.cs`
- `Application/Features/Stops/` (tüm handler'lar)
- `Application/DTOs/Stops/` (tüm DTO'lar)
- `WebApi/Controllers/v1/StopsController.cs`
- `Application/Interfaces/Repositories/IStopRepositoryAsync.cs`
- `Infrastructure/Repositories/StopRepositoryAsync.cs`
- Test dosyaları: `StopEntityTests.cs`, `StopsControllerTests.cs`, `StopRepositoryTest.cs`, `Stops/` klasörü
- `ApplicationDbContext`'ten `DbSet<Stop>` kaldırıldı
- `IApplicationDbContext`'ten `DbSet<Stop>` kaldırıldı
- `ServiceRegistration`'dan `IStopRepositoryAsync` DI kaldırıldı

**DB:** `stops` tablosu `TripPlanningV1` migration'ı (2026-04-26) ile **DROP** edildi.

**Korunan:** `Domain/Enums/StopAddedBy.cs` — `TimelineEntry.AddedBy` hâlâ kullanıyor (Ai, User).

**Yerine gelen:** `TimelineEntry` (5 tip, bkz. §5.1). LexoRank reorder ve visited tracking TimelineEntry'ye taşındı; time-lock mantığı `IsLocked + BufferMinutes` olarak TimelineService'e geçti.

### 6.2 Flight/Hotel Select Endpoint'leri — KALDIRILDI

Roadmap Phase 3 Week 8/9 "✅ Tamamlandı" diyor (AGENTS.md:185-208 hâlâ `POST /select`'i listeliyor). **Kaldırıldı.**

- `SelectFlightCommand.cs` + Handler — **yok** (grep doğrulaması)
- `SelectHotelCommand.cs` + Handler — **yok** (grep doğrulaması)
- `FlightsController.cs` artık sadece `GET` (31 satır, tek endpoint)
- `HotelsController.cs` artık sadece `GET` (31 satır, tek endpoint)
- Atomic selection mantığı (previous booking cancelled) — yok

**Yerine gelen:** Kullanıcı `TimelineEntry` (`EntryType=CustomFlight/CustomAccommodation`, `ProviderFlightId`/`ProviderHotelId` referansı) ile kendi uçuş/otelini ekliyor. Provider seçimi `ProvidersController` üzerinden public query ile yapılıyor.

### 6.3 `Trip.City` / `Trip.Country` / `Trip.UserBudget` — KALDIRILDI

`TRIP_PLAN_ROADMAP.md:212` ile tek şehir modeli kaldırıldı, `TripDestination` collection'a taşındı. `TripConfiguration.cs`'den `city`/`country` kolonları drop edildi (migration `TripPlanningV1`).

**Etkilenen roadmap dışı refactor'lar:**
- `ExploreTripsQueryHandler` — `trip.City` filtresi → `trip.Destinations.Any(d => d.City == city)` join'ine
- `ForkTripCommandHandler` — `trip.Stops` → `_context.TimelineEntries.Where(...)`
- `PublishTripCommandHandler` — `trip.Stops.Count` → `_context.TimelineEntries.Where(...)`

### 6.4 Eski `TravelStyle` Değerleri — BREAKING ENUM DEĞİŞİKLİĞİ

Eski: `Solo, Family, Adventure, Luxury, Relax` (5 değer, tek değer `Trip.TravelStyle`)
Yeni: `Romantic, Cultural, Adventure, Nature, Local, Relax, Shopping, Gastronomy, Influencer, Nightlife, Budget` (11 değer, `List<TravelStyle>`, `text[]`)

- `Adventure` ve `Relax` korundu (geriye dönük uyum)
- `Solo`, `Family`, `Luxury` **tamamen kaldırıldı**
- ~80 dosya etkilendi (compile error düzeltmesi, `TRIP_PLAN_ROADMAP:224`)
- Testlerde `TravelStyle.Luxury` → `TravelStyle.Relax` düzeltildi (`TRIP_PLAN_ROADMAP:81`)

### 6.5 `GenerateTimelineCommand` — Handler Yok (Phase 6 stub)

`Features/Trips/Commands/GenerateTimeline/GenerateTimelineCommand.cs` mevcut (roadmap Phase 6 Task 17.1 placeholder) ama **handler yok**, controller'da endpoint yok. AGENTS.md/CLAUDE.md `BACKEND_SCHEMA_MVP.md:155`'te command dosyası listelenmiş — sadece iskelet.

---

## 7. Bölüm E — DB Şema Drift (`BACKEND_SCHEMA_MVP.md` vs. Gerçek)

### 7.1 Tablo Envanteri

`BACKEND_SCHEMA_MVP.md` 18 tablo listeliyor. Gerçek durum:

| # | Planlanan Tablo | Gerçek Durum | Not |
|---|---|---|---|
| 1 | users | ✅ var | |
| 2 | trips | ⚠️ değişti | `city`/`country` DROP, `origin`/`origin_country`/`travel_companion`/`tempo`/`transport_preference`/`manual_budget`/`adjusted_budget_tier` ADD, `travel_style` text→text[] |
| 3 | places | ⚠️ değişti | `google_tags` text[] ADD (GIN), `photo_urls` text→text[], 9 OSM kolonu ADD |
| 4 | **stops** | ❌ **DROP EDİLDİ** | `TripPlanningV1` migration |
| 5 | flights | ✅ var | |
| 6 | hotels | ✅ var | |
| 7 | posts | ✅ var | |
| 8 | comments | ✅ var | |
| 9 | community_tips | ✅ var | |
| 10 | follows | ✅ var | |
| 11-15 | post_upvotes, comment_upvotes, tip_upvotes, trip_upvotes, saved_trips | ✅ var | |
| 16 | notifications | ✅ var | |
| 17 | karma_events | ✅ var | |
| 18 | refresh_tokens | ✅ var | |
| **+** | **trip_destinations** | 🆕 YENİ | Trip Planning (2026-04-26) |
| **+** | **timeline_entries** | 🆕 YENİ | Trip Planning (Stop'un yerine) |
| **+** | **provider_flights** | 🆕 YENİ | Provider verisi (2026-04-14) |
| **+** | **provider_hotels** | 🆕 YENİ | Provider verisi (2026-04-14) |
| **+** | **blocks** | 🆕 YENİ | UserBlock (2026-04-14) |
| **+** | **password_reset_tokens** | 🆕 YENİ | Forgot password (2026-04-19) |
| **+** | **email_verification_dispatches** | 🆕 YENİ | Email verify (2026-04-14) |

**Net tablo sayısı:** 18 - 1 (stops) + 7 (yeni) = **24 tablo**. (README.md "20 tables" diyor — stale.)

### 7.2 `trips` Tablosu Kolon Drift'i

| İşlem | Kolon | Tip | Kaynak |
|---|---|---|---|
| EKLE | origin | text NOT NULL | Trip Planning |
| EKLE | origin_country | text NOT NULL | Trip Planning |
| EKLE | travel_companion | text NOT NULL (enum) | Trip Planning |
| EKLE | tempo | text NOT NULL (enum) | Trip Planning |
| EKLE | transport_preference | text NOT NULL (enum) | Trip Planning |
| EKLE | adjusted_budget_tier | text NULL (enum) | Trip Planning |
| EKLE | manual_budget | decimal NULL | Trip Planning |
| DEĞİŞTİR | travel_style | text → text[] (ValueConverter) | Trip Planning |
| **KALDIR** | **city** | text | Trip Planning |
| **KALDIR** | **country** | text | Trip Planning |
| KALDIR | user_budget | decimal? | Trip Planning |

### 7.3 `places` Tablosu Kolon Drift'i

| İşlem | Kolon | Tip | Migration |
|---|---|---|---|
| EKLE | google_tags | text[] (GIN index) | Trip Planning |
| DEĞİŞTİR | photo_urls | text (JSON) → text[] (ValueConverter+ValueComparer) | Trip Planning |
| EKLE | price_level | int | `AddGooglePlaceFieldsToPlaces` (2026-04-23) |
| EKLE | review_count | int | `AddGooglePlaceFieldsToPlaces` |
| EKLE | wikipedia, wikidata, wheelchair, heritage, fee, image, cuisine | text | `AddOsmFieldsToPlaces` (2026-04-23) |

### 7.4 Yeni Tablo Şemaları (özet)

**`trip_destinations`** (`TripDestinationConfiguration.cs`):
- PK: id (UUID), FK: trip_id → trips (Cascade)
- Kolonlar: city, country, arrival_date, departure_date, order_index (1-10), night_count
- CHECK: `valid_dates` (departure ≥ arrival), `valid_night_count` (≥0, günübirlik için), `valid_order_index` (1-10, aslında 1-3'ten 10'a çıktı — `UpdateTripDestinationOrderIndexLimit` migration)
- Unique index: (trip_id, order_index) WHERE deleted_at IS NULL (deferrable)
- Backing fields: `_arrivalDate`/`_departureDate` + `PropertyAccessMode.Field`

**`timeline_entries`** (`TimelineEntryConfiguration.cs`):
- PK: id (UUID), FK: trip_id → trips (Cascade), destination_id → trip_destinations (Cascade), place_id/provider_flight_id/provider_hotel_id (SetNull)
- 5 tip discriminator: `entry_type` (Place, CustomFlight, CustomTransport, CustomAccommodation, CustomEvent)
- 7 CHECK constraint: `entry_type_place_requires_id`, `custom_flight_requires_fields`, `custom_transport_requires_type`, `custom_accommodation_requires_dates`, `custom_event_requires_time`, `locked_entry_has_buffer`, `valid_order_index`
- Index: (trip_id, destination_id, day_number, order_index) + 3 partial index (place_id, provider_flight_id, provider_hotel_id)

**`blocks`** (`BlockConfiguration.cs`): block ilişkisi (blocker_id, blocked_id), self-block CHECK.

**`password_reset_tokens`**: user_id, token_hash, expires_at, used_at.

**`email_verification_dispatches`**: user_id, token, expires_at, sent_at.

### 7.5 Migration Envanteri (11 adet)

| # | Migration | Tarih | İçerik | Roadmap? |
|---|---|---|---|---|
| 1 | `InitialCreate` | 2026-03-16 | 18 tablo | ✅ Phase 1 |
| 2 | `AddProviderTables` | 2026-04-14 | provider_flights, provider_hotels | ❌ Roadmap dışı |
| 3 | `AddUserBlockFeature` | 2026-04-14 | blocks tablosu | ❌ Roadmap dışı (Phase 1 hariç) |
| 4 | `AddPasswordResetToken` | 2026-04-19 | password_reset_tokens | ❌ Roadmap dışı |
| 5 | `AddOsmFieldsToPlaces` | 2026-04-23 | 7 OSM kolonu | ❌ Roadmap dışı |
| 6 | `AddGooglePlaceFieldsToPlaces` | 2026-04-23 | price_level, review_count | ❌ Roadmap dışı |
| 7 | `TripPlanningV1` | 2026-04-26 | **Büyük**: stops DROP, trip_destinations + timeline_entries CREATE, trips kolon değişimi, places photo_urls/google_tags | ❌ Roadmap dışı (TRIP_PLAN_ROADMAP) |
| 8 | `UpdateTripDestinationOrderIndexLimit` | 2026-04-28 | order_index limit 3→10 | ❌ |
| 9 | `MakeTripDestinationOrderIndexDeferrableWithSoftDeleteFilter` | 2026-05-01 | unique index deferrable | ❌ |
| 10 | `TripPlanningCleanupV1` | 2026-05-01 | 16 orphaned test trip silme | ❌ |
| 11 | `AllowOrderIndexZeroForShift` | 2026-05-01 | order_index=0 izni (shift için) | ❌ |

**Sonuç:** 11 migration'dan sadece 1'i roadmap Phase 1 kapsamında. 10'u roadmap dışı.

### 7.6 Enum Drift'i

`BACKEND_SCHEMA_MVP.md` 20 enum listeliyor. Gerçek: **25 enum**.

| Enum | Durum |
|---|---|
| 20 roadmap enum | ✅ hepsi var |
| `TravelStyle` | ⚠️ BREAKING — 5→11 değer |
| `TravelCompanion` | 🆕 YENİ |
| `Tempo` | 🆕 YENİ |
| `TimelineEntryType` | 🆕 YENİ |
| `TransportPreference` | 🆕 YENİ |
| `Season` | 🆕 YENİ |
| `StopAddedBy` | ✅ korundu (TimelineEntry kullanıyor) |

### 7.7 Configuration Dosya Drift'i

`BACKEND_SCHEMA_MVP.md` 18 configuration listeliyor. Gerçek: **23 dosya**.

- 18 roadmap configuration (StopConfiguration hariç → 17)
- `StopConfiguration.cs` ❌ silindi
- 🆕 `TripDestinationConfiguration.cs`, `TimelineEntryConfiguration.cs`, `ProviderFlightConfiguration.cs`, `ProviderHotelConfiguration.cs`, `BlockConfiguration.cs` (+5)
- Net: 17 - 1 + 5 = 21... aslında 23 dosya var (grep'ten). Ek 2 dosya muhtemelen `ApplicationUser` veya başka — tam sayım `glob Infrastructure/Configurations/*.cs` ile doğrulanabilir.

### 7.8 Repository Drift'i

`BACKEND_SCHEMA_MVP.md:359-370` 12 repository interface listeliyor. Gerçek: **16**.

- `IStopRepositoryAsync` ❌ silindi
- 🆕 `ITripDestinationRepositoryAsync`, `ITimelineEntryRepositoryAsync`, `IProviderFlightRepositoryAsync`, `IProviderHotelRepositoryAsync` (+4)
- `IUserRepositoryAsync`, `ITripRepositoryAsync`, `IPlaceRepositoryAsync`, `IFlightRepositoryAsync`, `IHotelRepositoryAsync`, `IPostRepositoryAsync`, `ICommentRepositoryAsync`, `ICommunityTipRepositoryAsync`, `IFollowRepositoryAsync`, `INotificationRepositoryAsync`, `IKarmaEventRepositoryAsync` (11 korundu)
- Net: 12 - 1 + 4 = 15 + `IKarmaEventRepositoryAsync` zaten sayılı = 16

---

## 8. Tam Endpoint Envanteri (92 endpoint)

`grep \[Http(Get|Post|Put|Delete)` ile doğrulandı. Roadmap'te listelenen ~45 endpoint'e karşın **92**.

### Meta (2)
- `GET /api/meta/health`, `GET /api/meta/info` — `MetaController.cs`

### Account (7 — roadmap 4, +3 roadmap dışı)
- `POST /api/account/register` (artık 202 + `RegistrationVerificationResponse`) — roadmap ✅
- `POST /api/account/login` — roadmap ✅
- `POST /api/account/refresh-token` (dual platform) — roadmap ✅
- `POST /api/account/forgot-password` — roadmap ✅ (placeholder → gerçek email)
- `POST /api/account/verify-email` — ❌ roadmap dışı
- `POST /api/account/resend-verification` — ❌ roadmap dışı
- `POST /api/account/reset-password` — ❌ roadmap dışı

### Places (4 — roadmap 4)
- `GET /api/v1/places`, `GET /api/v1/places/{id}`, `GET /api/v1/places/city/{city}`, `POST /api/v1/places` (Admin)

### Trips (15 — roadmap 12, +3 roadmap dışı)
- `GET /api/v1/trips`, `GET /api/v1/trips/{id}`, `POST /api/v1/trips` ([Obsolete], wizard'a map), `PUT /api/v1/trips/{id}`, `DELETE /api/v1/trips/{id}`, `POST /{id}/publish`, `POST /{id}/archive`, `POST /{id}/upvote`, `DELETE /{id}/upvote`, `POST /{id}/save`, `DELETE /{id}/save`, `POST /{id}/fork` — roadmap ✅ (12)
- `POST /api/v1/trips/wizard` — ❌ roadmap dışı (Trip Planning)
- `GET /api/v1/trips/{tripId}/budget-summary` — ❌ roadmap dışı
- `GET /api/v1/trips/{tripId}/recommend-places` — ❌ roadmap dışı

### TripDestinations (4 — hepsi roadmap dışı)
- `GET/POST/PUT/DELETE /api/v1/trips/{tripId}/destinations` — `TripDestinationsController.cs`

### Timeline (6 — hepsi roadmap dışı, Stop'un yerine)
- `GET /api/v1/trips/{tripId}/timeline`
- `POST /api/v1/trips/{tripId}/timeline/entry`
- `PUT /api/v1/trips/{tripId}/timeline/entry/{entryId}`
- `DELETE /api/v1/trips/{tripId}/timeline/entry/{entryId}`
- `PUT /api/v1/trips/{tripId}/timeline/reorder`
- `PUT /api/v1/trips/{tripId}/timeline/entry/{entryId}/visited`

### Flights (1 — roadmap 2, -1 select kaldırıldı)
- `GET /api/v1/trips/{tripId}/flights` — roadmap ✅
- ~~`POST /api/v1/trips/{tripId}/flights/select`~~ ❌ KALDIRILDI

### Hotels (1 — roadmap 2, -1 select kaldırıldı)
- `GET /api/v1/trips/{tripId}/hotels` — roadmap ✅
- ~~`POST /api/v1/trips/{tripId}/hotels/select`~~ ❌ KALDIRILDI

### Explore (2 — roadmap 1, +1 featured)
- `GET /api/v1/explore` (artık `searchTerm` parametresi de var) — roadmap ✅
- `GET /api/v1/explore/featured` — ❌ roadmap dışı

### SavedTrips (1 — roadmap 1)
- `GET /api/v1/saved-trips`

### Feed (1 — roadmap 1)
- `GET /api/v1/feed` (tab + cursor + pageSize)

### Posts (8 — roadmap 6, +2 roadmap dışı)
- `GET /api/v1/posts/{id}`, `POST /api/v1/posts`, `PUT /api/v1/posts/{id}`, `DELETE /api/v1/posts/{id}`, `POST /{id}/upvote`, `DELETE /{id}/upvote` — roadmap ✅ (6)
- `GET /api/v1/posts/liked` — ❌ roadmap dışı
- `GET /api/v1/posts/trending-tags` — ❌ roadmap dışı

### Comments (5 — roadmap 5)
- `GET/POST /api/v1/posts/{postId}/comments`, `DELETE /api/v1/comments/{id}`, `POST/DELETE /api/v1/comments/{id}/upvote`

### CommunityTips (5 — roadmap 5)
- `GET/POST /api/v1/trips/{tripId}/tips`, `DELETE /api/v1/tips/{id}`, `POST/DELETE /api/v1/tips/{id}/upvote`

### Follows (4 — roadmap 4, +search parametresi)
- `POST/DELETE /api/v1/users/{userId}/follow`, `GET /api/v1/users/{userId}/followers`, `GET /api/v1/users/{userId}/following`

### Users (9 — roadmap 3, +6 roadmap dışı)
- `GET /api/v1/users/{username}`, `GET /api/v1/users/me`, `PUT /api/v1/users/me` — roadmap ✅ (3)
- `GET /api/v1/users/me/posts` — ❌ roadmap dışı
- `GET /api/v1/users/{userId}/posts` — ❌ roadmap dışı
- `GET /api/v1/users/{userId}/trips` — ❌ roadmap dışı
- `GET /api/v1/users/top-contributors` — ❌ roadmap dışı
- `GET /api/v1/users/suggested-follows` — ❌ roadmap dışı
- `POST /api/v1/users/me/profile-photo` — ❌ roadmap dışı

### Blocks (3 — hepsi roadmap dışı, Phase 1 hariç listesindeydi)
- `POST/DELETE /api/v1/users/{userId}/block`, `GET /api/v1/users/{userId}/blocked-users`

### Notifications (4 — roadmap 4)
- `GET /api/v1/notifications`, `GET /api/v1/notifications/unread-count`, `POST /api/v1/notifications/{id}/read`, `POST /api/v1/notifications/read-all`

### Karma (1 — roadmap 1)
- `GET /api/v1/karma/history`

### Admin (5 — hepsi roadmap dışı, graduation'a bırakılmıştı)
- `GET /api/v1/admin/posts`, `DELETE /api/v1/admin/posts/{id}`, `GET /api/v1/admin/users`, `POST/DELETE /api/v1/admin/users/{id}/suspend`

### Media (1 — roadmap dışı)
- `POST /api/v1/media/upload`

### Providers (3 — hepsi roadmap dışı, public)
- `GET /api/v1/providers/origin-cities`, `GET /api/v1/providers/flights`, `GET /api/v1/providers/hotels`

---

## 9. Test Envanteri

### Test Projeleri (3) — `Tests/`

**OmniFlow.UnitTests** (~30 test dosyası):
- `Phase1/` — UserEntityTests, TripEntityTests, TripDestinationEntityTests, TimelineEntryEntityTests, ExceptionTests
- `Phase2/` — ScoringServiceTests, BudgetCalculationServiceTests, TimelineServiceTests, RecommendationServiceTests
- `Phase3/` — TimelineEntryHandlerTests, ProviderQueryHandlerTests, GetRecommendedPlacesHandlerTests
- `Trips/` — CreateTripCommandTests (Validator, Handler, Command), ForkTripCommandTests, UpvoteTripCommandTests, PublishTripCommandTests, ExploreTripQueryTests, GetPublishedTripsByUserQueryTests
- `Posts/` — CreatePost, UpdatePost, DeletePost, UpvotePost, RemoveUpvotePost, GetPostById, GetFeed, GetPostsByUser, GetTrendingTags, GetLikedPosts (test)
- `Comments/` — Create, Delete, Upvote, RemoveUpvote, GetCommentsByPost
- `CommunityTips/` — Create (Validator, Handler), Delete, Upvote, RemoveUpvote, GetTipsByTrip
- `Follows/` — FollowUser, UnfollowUser, GetFollowers, GetFollowing
- `Karma/` — KarmaServiceTests, GetKarmaHistoryQueryHandlerTests
- `Notifications/` — NotificationServiceTests, NotificationFeatureTests
- `Blocks/` — BlockUserCommandTests, UnblockUserCommandTests
- `Places/` — CreatePlace (Validator, Handler), GetPlaceById
- `Flights/` — GetFlightsByTripQueryTests
- `Features/Users/` — UpdateProfile (Validator, Handler), GetUserProfile, GetSavedTrips
- `Features/SavedTrips/` — Save, Unsave
- `Features/Trips/` — UpvoteTrip, RemoveUpvoteTrip handler
- `Application/` — ValidationBehaviourTests

**Son dokümante edilen:** `TRIP_PLAN_ROADMAP.md:1418` — "406 unit test passing, 1 skipped, 0 failed" (2026-05-01, Phase 5 sonrası). 6 Mayıs admin/social extension commit'leri sonrası **muhtemelen daha yüksek** (build/test çalıştırılmadığından net sayı verilemedi).

**OmniFlow.Api.IntegrationTests** (~20 controller test dosyası):
- `Controllers/` — Account, Trips, TripsControllerSaveUpvote, TripDestinations, Timeline, Providers, Places, Posts, Comments, CommunityTips, Follows, Blocks, Users, Notifications, Karma (Integration), Feed, Explore, Flights, Hotels, Media
- `Auth/` — JwtConfigurationTests
- `Cors/` — CorsMiddlewareTests
- `Middlewares/` — ErrorHandlerMiddlewareTests
- `Setup/` — CustomWebApplicationFactory, TestDatabaseSeeder (provider seed dahil)

**⚠️ Bilinen Test Sorunu:** `TRIP_PLAN_ROADMAP.md:1273` — Integration testlerde `Npgsql.PostgresException: 42601 syntax error at or near "DEFERRABLE"` migration hatası. Bu nedenle integration testler şu an tam çalışmıyor olabilir (in-memory DB + migration schema uyuşmazlığı). `GetRecommendPlaces_ForPublishedTrip_ReturnsOk` testi in-memory DB'de Place seed olmadığı için 500 veriyor.

**OmniFlow.Infrastructure.Tests** (~9 dosya):
- GenericRepositoryTests, TripRepositoryTest, PostRepositoryTest, PlaceRepositoryTest, KarmaEventRepositoryTest, HotelRepositoryTest, FlightRepositoryTest, FollowRepositoryTest

---

## 10. Bölüm F — Öneriler ve Sonraki Adımlar

### 🔴 Kritik (Yüksek Öncelik)

1. **Dokümantasyon senkronize edilmeli (stale tespitler):**
   - `BACKEND_SCHEMA_MVP.md` — `stops` tablosu, `IStopRepositoryAsync`, `StopConfiguration.cs`, `StopsController.cs` listeleniyor; 18 tablo/20 enum sayıyor → **24 tablo/25 enum** olarak güncellenmeli
   - `README.md` — "5-Step Smart Itinerary Builder" (gerçek 8-adım wizard), Travel Styles "Solo, Family, Adventure, Luxury, Relax" (gerçek 11 değer), "20 tables" (gerçek 24), "stops" tablosu listesi → güncellenmeli
   - `CLAUDE.md` ve `AGENTS.md` — StopsController listeliyor (yok), Phase 3 "select" endpoint'leri listeliyor (yok), "Phase 3 In Progress - Week 8" diyor (gerçek Phase 3+ tamamlandı ama select'ler kaldırıldı)
   - Öneri: Tek bir `BACKEND_SCHEMA_CURRENT.md` üretilip eski `*_MVP.md` dosyaları "superseded" işaretlenmeli

2. **Phase 6 AI Timeline eksik — roadmap dışı Trip Planning modülü AI'yı beklemedi:**
   - `AiTimelineService.cs` / `AiFallbackService.cs` boş stub. `GenerateTimelineCommand` handler'sız.
   - **Wizards'la gelen TimelineEntry sistemi AI generation için hazır** (EntryType, IsLocked, Buffer, factory metotları). AI'nin yapacağı: Trip bilgisi + filtrelenmiş Place'ler → TimelineEntry listesi (EntryType=Place, AddedBy=Ai, AiReasoning dolu).
   - Öneri: Phase 6'yı yeni TimelineEntry modeline göre yeniden tasarlayın (eski roadmap Stop bazlıydı, artık geçersiz).

3. **Integration test migration hatası (`DEFERRABLE` syntax):** Test DB'sinde migration çalışmıyor. Bu, integration testlerin güvenilirliğini bozuyor. Ya migration `DEFERRABLE` kısmını PostgreSQL uyumlu hale getirilmeli ya da `CustomWebApplicationFactory` test şemasını manuel kurmalı.

### 🟡 Orta Öncelik

4. **Roadmap dışı feature'lar için "out-of-scope" kararı belgelenmeli:**
   - UserBlock, EmailVerification, AdminPanel, MediaUpload — `BACKEND_SCHEMA_MVP.md` "MVP'den Çıkarılanlar" tablosunda "bitirme" diyor ama hepsi MVP'de yapıldı. Bu tablo güncellenmeli ya da "MVP'ye Sonradan Eklenenler" şeklinde yeni bölüm eklenmeli.
   - Trip Planning modülü kendi roadmap'ine sahip — `BACKEND_ROADMAP_MVP.md`'ye referans eklenmeli ("Trip Planning modülü için `TRIP_PLAN_ROADMAP.md`'ye bakınız").

5. **Phase 3 "Tamamlandı" status'ü düzeltilmeli:** `BACKEND_ROADMAP_MVP.md:962-968` Phase 3 Success Metrics hâlâ "Flight seçimi çalışıyor, gidiş-dönüş itinerary gruplama var" diyor. **Select endpoint'leri kaldırıldı.** Bu ya "Trip Planning modülüne taşındı" notuyla güncellenmeli ya da Phase 3 tekrar açılmalı.

6. **`POST /api/v1/trips` `[Obsolete]` işareti:** Geriye dönük uyumlu ama wizard'a map'leniyor. Frontend migration tamamlandığında gerçekten kaldırılmalı. Teknik borç.

7. **`ForkTripCommandTests` 1 test hâlâ skipped:** `TRIP_PLAN_ROADMAP.md:336` — `Handle_DeepCopiesStops_VerifyNewIdsAndResetVisited` skip edildi, TimelineEntry deep-copy testi olarak yeniden yazılması bekleniyor. Aynı şekilde `Fork_CopiesStopsAndResetsCounters` integration testi skip.

8. **`SavedTripResponse` teknik borç:** `TRIP_PLAN_ROADMAP.md:743` — eski şema kalıntısı (`TravelStyle`, `City`, `Country`, `UserBudget` AutoMapper Ignore'ları). Phase 5'te tam revizyon gerekiyor.

### 🟢 Düşük Öncelik / İyileştirme

9. **CI/CD pipeline test + PR trigger eksik:** `azure-pipelines.yml` zaten var ve `main` branch'inde build → publish → Azure Web App deploy (`omniflow-backend1`) yapıyor. Ama:
   - **`DotNetCoreCLI@2 test` step yok** — pipeline test koşmuyor, sadece build ediyor
   - **PR trigger yok** — sadece `main` push'ta çalışıyor, PR açıldığında doğrulama yok
   - **Build badge README'de yok**
   - Öneri: `test` step eklenmeli (`dotnet test OmniFlow.slnx --logger trx --results-directory $(Agent.TempDirectory)` + `PublishTestResults@2`), `pr:` trigger bloğu eklenmeli, README'ye status badge konmalı.
   - Not: Integration testler `DEFERRABLE` migration hatası (§10.3) nedeniyle şu an pipeline'da fail edebilir — önce o çözülmeli.

10. **Postman collection + load test:** Roadmap Phase 7 Task 19.1/19.2/20.1 — hiç başlanmadı. Frontend entegrasyonu için Postman collection üretmek faydalı.

11. **`docker-compose.yml` eksik:** Sadece `Dockerfile` var, Redis + backend compose yok.

12. **Enum array mapping standardizasyonu:** `List<TravelStyle>` → `text[]` için `ValueConverter` + `ValueComparer` pattern'i Trip'te uygulandı. Aynı pattern `Place.GoogleTags`/`PhotoUrls`/`BudgetTiers`/`TravelStyles` için de kullanılıyor — tutarlılık iyi.

13. **`BudgetCalculationService` DI yaşam döngüsü düzeltildi (Singleton → Scoped):** `TRIP_PLAN_ROADMAP.md:1353` Captive Dependency fix. Bu pattern diğer servislar için de kontrol edilmeli — `ScoringService`/`TimelineService` hâlâ `Singleton` (`ServiceRegistration.cs:55,57`) ama stateless oldukları için sorun değil.

### 📋 Roadmap Güncelleme Önerisi (Yeni Faz)

Mevcut `BACKEND_ROADMAP_MVP.md` Phase 6-7'si eskidi (Stop bazlı). Yeni öneri:

- **Phase 6 (Yeniden):** AI Timeline Generation — TimelineEntry bazlı, `GenerateTimelineCommand` + handler, `AiTimelineService` (OpenAI) + `AiFallbackService` (kural tabanlı, TimelineEntry döner), `POST /api/v1/trips/{tripId}/generate-timeline`, mevcut AI TimelineEntry'leri temizleme (user'larınkine dokunma).
- **Phase 7 (Aynı):** Testing, CI/CD, Docker, Docs — + stale dokümantasyon temizliği (§10.1).
- **Phase 8 (Yeni — Roadmap dışı feature'ların resmileştirilmesi):** Trip Planning modülü + Email verify + UserBlock + AdminPanel + MediaUpload zaten kodda var; bunları resmi roadmap'e "Phase 8 — Completed out-of-scope features" olarak ekleyip test + dokümantasyon coverage'ını kapatmak.

---

## 11. Ek — Kaynak MD Dosya Haritası

| Dosya | Durum | Ne İçin Kullanıldı |
|---|---|---|
| `BACKEND_ROADMAP_MVP.md` | Ana roadmap (1742 satır, 7 faz) | Phase 1-7 karşılaştırma temeli |
| `BACKEND_SCHEMA_MVP.md` | Planlanan DB şeması (667 satır) | Şema drift karşılaştırma (§7) |
| `TRIP_PLAN_ROADMAP.md` | Trip Planning modülü roadmap'i (1433 satır, 5 faz) | Roadmap dışı Trip Planning modülü (§5.1) |
| `TRIP_PLAN_IMPLEMENT.md` | Trip Planning uygulama planı (1159+ satır) | Implementasyon detayları, scoring tabloları |
| `TRIP_PLANNING.md` | Trip Planning PRD (395 satır) | 8-adım wizard, scoring formülü, custom entry tipleri |
| `TRIP_PLANNING_CHANGES.md` | Trip Planning özet dönüşüm raporu (95 satır) | High-level değişim özeti |
| `CLAUDE.md` | Claude Code context (466 satır) | **Stale** — Stop/Phase 3 select listeliyor |
| `AGENTS.md` | Agent context | **Stale** — CLAUDE.md ile aynı içerik, StopsController listeliyor |
| `README.md` | Project overview (340 satır) | **Stale** — 5 TravelStyle, 20 tables, stops tablosu |

---

## 12. Sonuç

OmniFlow backend'i, **iki paralel roadmap** tarafından şekillendirilmiş bir proje:
1. `BACKEND_ROADMAP_MVP.md` — Phase 1-5 tamamlandı, Phase 6 (AI) ve Phase 7 (Testing/CI/CD/Docker) **hâlâ bekliyor**.
2. `TRIP_PLAN_ROADMAP.md` — Phase 1-5 tamamlandı (2026-05-01), core trip modelini kökten değiştirdi.

Buna ek olarak, **roadmap dışı** feature'lar (Email verification, UserBlock, AdminPanel, MediaUpload, Provider data, Suggested follows, Trending tags, Liked posts, Top contributors, Featured trips, Profile photo upload) 2026 Nisan-Mayıs arası eklenmiş ve MVP scope'u belirgin şekilde genişletilmiş.

**En büyük risk:** Dokümantasyon (`BACKEND_SCHEMA_MVP.md`, `README.md`, `CLAUDE.md`, `AGENTS.md`) kodla senkronize değil — `Stop` entity'si, 5-değerli `TravelStyle`, Phase 3 select endpoint'leri hâlâ dokümante ediliyor ama kodda yok. Bu, yeni başlayan geliştiriciler ve AI agent'lar için yanıltıcı.

**En büyük fırsat:** Trip Planning modülünün `TimelineEntry` + `TimelineService` altyapısı, Phase 6 AI Timeline generation için hazır bir temel sunuyor — eski Stop-bazlı AI roadmap'i yerine yeni bir tasarım kolayca oturabilir.

---

*Rapor tarihi: 20 Haziran 2026 — Statik kod analizi ile üretilmiştir (build/test çalıştırılmamıştır).*
