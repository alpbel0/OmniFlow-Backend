# OmniFlow Backend — Roadmap V2 (Yeni Özellikler / Canlı Ürün Aşaması)

> **Amaç:** Bu doküman, `BACKEND_ROADMAP_MVP.md` ile tamamlanan MVP'nin **üzerine** gelen, henüz yapılmamış backend işlerini tanımlar.
>
> **Kapsam:** Sadece yeni / eksik backend işleri. MVP'de zaten yapılmış olan (auth, trips, places, social, community, trip planning, admin, media) burada **tekrar edilmez**.
>
> **Bağlam:** Bu roadmap, `MOBILE_ROADMAP.md` ile **kilitli** çalışır. Mobil tarafta her yeni özellik, buradaki ilgili backend task'ı tamamlanmadan başlatılamaz. Mobil roadmap, buradaki task numaralarına (`B1.1`, `B2.3` gibi) doğrudan referans verir.
>
> **Mimari notu:** Backend güncel durumda `TimelineEntry + TripDestination` modelini kullanır (eski `Stop` modeli kaldırılmıştır). Tüm yeni işler bu güncel mimariyle uyumlu yazılmalıdır. Kod standartları için mevcut Clean Architecture + CQRS (MediatR) + FluentValidation + EF Core pattern'leri aynen korunur.

---

## 📋 Faz Özeti

| Faz | Konu | Mobil karşılığı | Öncelik |
|-----|------|-----------------|---------|
| **B0** | Temel & Temizlik (as-built docs, CI test, provider freshness, trip %hazır/görüntülenme/cover photo) | M3 (önkoşul) | 🔴 Yüksek |
| **B1** | Google OAuth (external login) | M7 | 🔴 Yüksek |
| **B2** | Live Trip altyapısı (Visit Log, Trip Summary, Timezone) | M8 | 🟠 Orta |
| **B3** | Push (FCM) + Notification Preferences | M9 | 🟠 Orta |
| **B4** | Collections, Global Search, Deep-link, Memories | M10 | 🟢 Düşük |
| **B5** | Moderasyon (Report, Soft Moderation, Audit Log) | M11 | 🟢 Düşük |
| **B6** | AI (Timeline Optimize + AI Chat, tool-grounded) | M12 | 🟢 Düşük |
| **B7** | Currency Servisi (günlük kur, cron + cache) | M13 | 🟢 Düşük |
| **B8** | Offline Sync & Trip Collaboration | M14 | ⚪ En son |

**Genel kural:** Her yeni entity için sırasıyla → Domain entity → EF Configuration → Migration → Application DTO → CQRS Command/Query + Validator → AutoMapper mapping → Controller → (minimal) test. Bu zincir her task'ta varsayılır.

---

## 🎯 B0 — Temel & Temizlik

### Scope

MVP sonrası borç temizliği. Yeni özellik yazmadan önce zemini düzeltir. Bu faz, mobil ekip API'ye güvenle bakabilsin diye **dokümantasyon gerçeği yansıtsın** ve CI kalite kapısı çalışsın diye gereklidir.

### Task B0.1: As-Built Dokümantasyon Güncelleme

**Tahmini Süre:** 3 saat
**Durum:** [x] Tamamlandı

- [x] `BACKEND_SCHEMA_MVP.md` → `Stop`/`StopsController`/`IStopRepositoryAsync` referanslarını `TimelineEntry`/`TripDestination` ile değiştir, 18 tablo → 24 tablo güncelle
- [x] `README.md` → "20 tables", 5 TravelStyle, eski endpoint listesi güncellensin
- [x] `CLAUDE.md` ve `AGENTS.md` → silinmiş `Stop` + `select` endpoint anlatıları kaldırılsın, güncel controller listesi yazılsın
- [x] Güncel enum listesi (25 enum), güncel migration listesi (11) yansıtılsın
- [x] Flight/Hotel `select` endpoint'lerinin kaldırıldığı ve mantığın `TimelineEntry`'ye taşındığı not düşülsün

### Task B0.2: CI/CD Kalite Kapısı

**Tahmini Süre:** 2 saat
**Durum:** [x] Tamamlandı

- [x] `azure-pipelines.yml`'a `dotnet test` adımı eklensin (publish'ten önce)
- [x] `.slnx` / SDK uyumsuzluğu çözülsün (SDK 9'a geç **veya** klasik `.sln` üret) — `dotnet test` çözüm seviyesinde çalışmalı
- [x] Test başarısızsa pipeline kırmızı olsun (deploy engellensin)
- [ ] (Opsiyonel) PR trigger eklensin

### Task B0.3: Provider Freshness / Data Quality Alanları

**Tahmini Süre:** 2 saat
**Durum:** [x] Tamamlandı

- [x] `ProviderFlight` / `ProviderHotel` entity'lerine `LastUpdatedAt`, `IsLiveData` (bool), `DataSnapshotDate` alanları (yoksa) eklensin
- [x] Migration
- [x] Provider response DTO'larına freshness bilgisi eklensin ("son güncelleme", "canlı değil/tahmini")
- [x] `ProvidersController` response'larında bu alanlar dönsün

### Task B0.4: AI Scaffold Kararı

**Tahmini Süre:** 30 dakika
**Durum:** [x] Tamamlandı

- [x] Boş `AiTimelineService.cs`, `AiFallbackService.cs`, `GenerateTimelineCommand.cs` dosyaları → B6'ya kadar **kaldırılsın** (kafa karışıklığı yapmasın) veya açık `// TODO B6` notuyla işaretlensin
- [x] İlgili boş interface'ler B6 tasarımına göre yeniden değerlendirilsin

### Task B0.5: User Profil Alanları Genişletme

**Tahmini Süre:** 2 saat
**Durum:** [x] Tamamlandı

> **Bağlam:** Edit Profile ekranında "Konum" ve "Seyahat Stili" alanları ekleniyor. `TravelStyle` enum'u zaten domain'de mevcut. Bu task M2 profil ekranından önce tamamlanmalı.
>
> **Mobil karşılığı:** `MOBILE_ROADMAP.md → M2 / Task 2.4`

- [x] `User` entity'sine `Location` (string?, ör. "İstanbul, Türkiye") ve `TravelStyles` (List\<TravelStyle\>?, JSON kolonu) alanları eklensin
- [x] EF Core konfigürasyonu: `TravelStyles` → `jsonb` kolonu (PostgreSQL)
- [x] Migration
- [x] `UpdateProfileRequest`'e `Location` ve `TravelStyles` alanları eklensin
- [x] `UpdateProfileCommand` ve handler güncellenmesi
- [x] `UserProfileResponse`'a `Location` ve `TravelStyles` alanları eklensin
- [x] AutoMapper mapping güncellenmesi

### Task B0.5.1: Profil Mevcut Konum Koordinatları

**Tahmini Süre:** 1 saat
**Durum:** [x] Tamamlandı

> **Bağlam:** Edit Profile ekranında "Mevcut konumu kullan" aksiyonu olacak. B0.5 şu an kullanıcıya gösterilecek `Location` text'ini saklıyor. Bu ek task, mobil cihazdan gelen GPS koordinatını da saklayarak B0.12 ve sonrası geocoding/reverse-geocoding işleri için altyapı hazırlar.
>
> **Not:** Bu task backend'de otomatik şehir/ülke üretmez. Şimdilik mobil `location` text'ini gönderebilir veya kullanıcı elle düzenleyebilir. Backend reverse geocoding B0.12 ve sonrası eklendiğinde `LocationLatitude` / `LocationLongitude` üzerinden `Location` otomatik üretilebilir.

- [x] `User` entity'sine `LocationLatitude` (double?) ve `LocationLongitude` (double?) alanları eklensin
- [x] EF Core konfigürasyonu + migration: `users.location_latitude`, `users.location_longitude`
- [x] `UpdateProfileRequest` / `UpdateProfileCommand` / handler bu alanları desteklesin
- [x] `UserProfileResponse` bu alanları dönsün
- [x] Validation:
  - [x] Latitude `-90..90`
  - [x] Longitude `-180..180`
  - [x] Koordinat update edilecekse latitude ve longitude birlikte gelsin
- [x] Unit testler: valid koordinat update, tek koordinat eksikse validation fail, range dışı validation fail

### Task B0.6: Trip Tamamlanma Yüzdesi (%hazır)

**Tahmini Süre:** 2 saat
**Durum:** [x] Tamamlandı

> **Bağlam:** My Trips ekranında Draft kartlarında "%40 hazır" gibi bir ilerleme göstergesi olacak. Migration gerekmez; `TripResponse`'a computed alan eklenir.
>
> **Mobil karşılığı:** `MOBILE_ROADMAP.md → M3 / Task 3.1`

- [x] `CompletionPercentage` (int, 0-100) alanı `TripResponse`'a eklenir — computed, DB'de saklanmaz
- [x] Hesaplama kuralları daha detaylı readiness modeliyle yapılır:
  - [x] **Temel bilgiler (15):** title dolu (+5), description dolu (+5), coverPhotoUrl var (+5)
  - [x] **Seyahat bilgileri (20):** origin/originCountry dolu (+5), startDate/endDate mantıklı (+6), personCount > 0 (+3), travelStyles seçilmiş (+6)
  - [x] **Destinasyon planı (20):** en az 1 destination var (+10), destination tarihleri dolu ve sıralı (+5), birden fazla destination varsa order düzgün (+5)
  - [x] **Timeline/itinerary (40):** en az 1 timeline entry var (+8), her destination için en az 1 entry var (+10), entry'lerde start time veya dayNumber var (+6), her destination için timed entry var (+6), yeterli timeline derinliği var (+10)
  - [x] **Bütçe (5):** estimatedCost veya manualBudget var (+5)
- [x] Sonuç 0-100 aralığında clamp edilir; eksik ilişkiler yüklenemiyorsa ilgili alt puan 0 sayılır, hesaplama hata fırlatmaz
- [x] `GetMyTripsQuery` handler'ında hesaplanır; sadece kendi trip'lerinde döner (public trip response'da 0 veya null)

---

### Task B0.7: Trip Görüntülenme Sayacı

**Tahmini Süre:** 2 saat
**Durum:** [x] Tamamlandı

> **Bağlam:** My Trips / Published tab'ında "231 Görüntülenme" gibi bir metrik gösterilecek.
>
> **Mobil karşılığı:** `MOBILE_ROADMAP.md → M3 / Task 3.1`

- [x] `Trip` entity'sine `ViewCount` (int, default 0) kolonu ekle — mevcut şemada var
- [x] EF Core konfigürasyonu: `HasDefaultValue(0)` — mevcut konfigürasyonda var
- [x] Migration — yeni migration gerekmedi; `view_count` initial migration'da mevcut
- [x] `GET /api/v1/Trips/{id}` handler'ında: **istek sahibi trip'in owner'ı değilse** `ViewCount++` (atomic increment, `ExecuteUpdateAsync`) — B0.7'de mevcut auth davranışı korunur; anonim erişim B0.10'da açıldığında aynı owner dışı kuralı çalışır
- [x] `TripResponse`'a `ViewCount` alanı eklenir — mevcut response'da var

---

### Task B0.8: Trip Kapak Fotoğrafı Yükleme

**Tahmini Süre:** 1.5 saat
**Durum:** [x] Tamamlandı

> **Bağlam:** My Trips kartlarında ve Trip Detail'de gerçek kapak fotoğrafı gösterilecek. Kullanıcı profil fotoğrafı için `POST /api/v1/users/me/profile-photo` zaten var; trip için eşdeğeri eksik.
>
> **Mobil karşılığı:** `MOBILE_ROADMAP.md → M3 / Task 3.1, 3.2`

- [x] `UploadTripCoverPhotoCommand` — multipart/form-data, owner kontrolü
- [x] Mevcut `BlobService` kullanılır (yeni altyapı gerekmez)
- [x] Trip entity'sindeki `CoverPhotoUrl` güncellenir
- [x] Endpoint: `POST /api/v1/Trips/{id}/cover-photo` → `{ "coverPhotoUrl": "..." }` döner
- [x] `CompletionPercentage` hesabında kapak fotoğrafının varlığı bu alan üzerinden kontrol edilir (B0.6 ile koordineli)

---

### Task B0.9: Trip Checklist Confirmation (Review Modu için)

**Tahmini Süre:** 2.5 saat
**Durum:** [x] Tamamlandı

> **Bağlam:** Trip Detail'in yeni "Review Modu"nda (Kategori kartları — Flights/Hotels/Mekan) kullanıcı her checklist satırını **manuel olarak** işaretleyip "hallettim" diyebiliyor. Bu işaretleme, gerçek bir `Flight`/`Hotel` kaydının var olup olmamasından **bağımsız** — kullanıcı o bacağı uçakla, trenle veya kendi imkânıyla halletmiş olabilir, sistem bunu bilmiyor/bilmesi gerekmiyor.
>
> **Mobil karşılığı:** `MOBILE_ROADMAP.md → M3 / Task 3.2` (Trip Detail — Review Modu / Kategori Kartları), tasarım detayları için `omniflow-mobile/TRIP_DETAILS_PAGE.md`

**⚠️ Kapsam düzeltmesi:** Bu mekanizma **sadece Flights (leg) ve Hotels (gece) için** gerekli. **Mekan (Food/Activities) kapsam dışı** — Mekan'ın checklist satırları zaten var olan gerçek `TimelineEntry` (Place tipi) kayıtlarıdır; "işaretli mi" durumu **entry'nin var olması** ile otomatik belirlenir, ayrı bir confirmation kaydına ihtiyaç yok. Mekan'da checkbox salt-okunur bir gösterge (entry eklenmiş ✓), manuel toggle edilmez.

**ItemKey Formatı (stabil, destinasyon reorder'ından etkilenmez — `TripDestination.Id` GUID'i üzerinden, isim/index üzerinden değil):**
- Flight leg: `flight-leg:{fromDestinationId}:{toDestinationId}` — iki ardışık `TripDestination.Id`'si
- Hotel night: `hotel-night:{destinationId}:{nightNumber}` — `nightNumber` 1'den `TripDestination.NightCount`'a kadar

**Reconciliation Kuralı (destinasyon silinir/tarih değişirse):**
- Destinasyon **silinirse** → o destinasyona referans veren tüm `TripChecklistConfirmation` kayıtları **cascade silinir** (`DeleteTripDestinationCommand` handler'ında explicit temizlik, EF cascade delete güvenilir olmayabileceği için)
- Destinasyon tarihleri değişip `NightCount` **azalırsa** → artık geçersiz `nightNumber`'a sahip confirmation'lar silinmez ama **okuma sırasında filtrelenir**: `GetTripChecklistStatusQuery`, önce güncel geçerli `ItemKey` setini (mevcut destinasyonlardan) hesaplar, sadece bunlarla eşleşen confirmation'ları döner — stale kayıtlar sessizce yok sayılır (fiziksel silme opsiyonel, doğruluk read-time filtrelemeyle garanti edilir)
- Yeni destinasyon eklenirse veya `NightCount` artarsa → yeni `ItemKey`'ler otomatik "işaretlenmemiş" (confirmation kaydı yok = unchecked) olarak başlar, ekstra işlem gerekmez

- [x] `TripChecklistConfirmation.cs` entity (hafif, `BaseEntity`) — `TripId`, `ItemKey` (string, yukarıdaki format), `IsConfirmed` (bool), `ConfirmedAt` (DateTime?)
- [x] EF Core configuration + migration — unique index (`trip_id`, `item_key`)
- [x] `ToggleChecklistItemCommand` — owner kontrolü, idempotent (aynı state'e tekrar set etmek hata vermez)
- [x] `DeleteTripDestinationCommand` handler'ına o destinasyona ait `flight-leg:` / `hotel-night:` confirmation'larının temizlenmesi eklenir
- [x] `GetTripChecklistStatusQuery` — güncel geçerli `ItemKey` setini destinasyon verisinden hesaplar, sadece bunlarla eşleşen confirmation'ları döner (stale veri read-time'da filtrelenir)
- [x] Endpoint'ler ve tam contract (`TripsController` üzerinde, `GetById`/`GetBudgetSummary` ile aynı controller — casing tutarlılığı için `/api/v1/Trips/...`):
  - `GET /api/v1/Trips/{id}/checklist` → response:
    ```json
    { "items": [
        { "itemKey": "flight-leg:{guid}:{guid}", "isConfirmed": true, "confirmedAt": "2026-07-01T10:00:00Z" },
        { "itemKey": "hotel-night:{guid}:1", "isConfirmed": false, "confirmedAt": null }
    ] }
    ```
    Sadece **güncel geçerli** `itemKey`'ler döner (stale olanlar read-time'da filtrelenmiş halde, yukarıdaki Reconciliation Kuralı'na göre). Ring hesaplaması (`seçilen/beklenen`) **client-side** yapılır — backend sadece ham confirmation state'i döner, beklenen sayı zaten mobilde mevcut destinasyon verisinden hesaplanabiliyor, backend'de tekrar hesaplamaya gerek yok
  - `PUT /api/v1/Trips/{id}/checklist/{itemKey}` — body: `{ "isConfirmed": true }`, response: **`204 No Content`** (netleşti — mobil zaten optimistic update kullandığı için response body'ye bağımlı değil, en basit kontrat bu)
- [x] **Visibility kuralı (B0.10 ile aynı desen — ayrıştırılmalı, kopyalanmamalı):**
  - **`GetTripChecklistStatusQuery` (GET):** `[AllowAnonymous]` + handler'da `ITripVisibilityService` (B0.10'da tanımlanan paylaşılan helper) kullanılır — Published → **anonim dahil herkes okuyabilir** (misafir Trip Detail'de checklist'i salt-okunur görüyor, bkz. `TRIP_DETAILS_PAGE.md`), Draft/Archived → sadece owner, yetkisiz/anonim → `EntityNotFoundException` (404)
  - **`ToggleChecklistItemCommand` (PUT):** sınıf seviyesindeki `[Authorize]`'dan (override yok) — giriş gerektirir, **ayrıca owner kontrolü** (sadece trip owner'ı checklist işaretleyebilir; misafir/anonim PUT çağırırsa 401/403, GET'te salt-okunur görebilmesiyle çelişmez çünkü ikisi ayrı action, ayrı yetki seviyesi)
- [x] **URL encoding:** `itemKey` içindeki `:` karakterleri path segment'inde teknik olarak geçerlidir (RFC 3986 `pchar` seti `:` içerir), ama HTTP client kütüphaneleri (Retrofit/OkHttp) arasında tutarlılık için **mobil taraf `itemKey`'i açıkça percent-encode eder** (`:` → `%3A`) URL'i oluştururken. ASP.NET Core route binding, path parametrelerini **otomatik url-decode eder** — backend tarafında ekstra bir işlem gerekmez, sadece bu beklenti iki taraf arasında netleşmiş olsun
- [x] Mobil tarafta **optimistic update + hata durumunda geri alma** — mevcut app-wide konvansiyonla tutarlı (bkz. `MOBILE_ROADMAP.md` "UI State Konvansiyonu" → Aksiyon durumları), yeni bir pattern gerekmez

---

### Task B0.10: 🔴 GÜVENLİK — GetTripByIdQuery Owner/Status Kontrolü Eksik

**Tahmini Süre:** 1 saat
**Durum:** [x] Tamamlandı
**Öncelik:** 🔴 Kritik — güvenlik açığı, diğer her şeyden önce yapılmalı

> **Bulgu:** `GetTripByIdQueryHandler.cs` trip'i bulup **hiçbir owner/status kontrolü yapmadan** direkt döndürüyor. Şu an authenticated herhangi bir kullanıcı, ID'sini bilirse **başkasının Draft/Archived trip'ini** tam olarak görebiliyor. Diğer command'larda (`ArchiveTripCommandHandler` vb.) owner kontrolü var, ama bu **read** path'inde (Trip Detail'in kullandığı asıl endpoint) yok.
>
> **⚠️ Ek bulgu (kod incelemesinde ortaya çıktı):** `TripsController`, `BaseApiController`'dan miras alıyor ve `BaseApiController` sınıf seviyesinde `[Authorize]` taşıyor ([BaseApiController.cs:8](omniflow-backend/OmniFlow/OmniFlow.WebApi/Controllers/BaseApiController.cs:8)). `GetById` ([TripsController.cs:61-70](omniflow-backend/OmniFlow/OmniFlow.WebApi/Controllers/v1/TripsController.cs:61)) ve `GetBudgetSummary` ([TripsController.cs:142-152](omniflow-backend/OmniFlow/OmniFlow.WebApi/Controllers/v1/TripsController.cs:142)) üzerinde **`[AllowAnonymous]` yok** — yani anonim (giriş yapmamış) bir istek şu an **handler'a hiç ulaşmadan, framework seviyesinde 401 ile reddediliyor**. Handler içine "anonim ise Published kontrolü yap" mantığı eklemek **tek başına yeterli değil**, controller seviyesinde de `[AllowAnonymous]` eklenmesi şart.
>
> **Karar (Explore ile tutarlılık):** `ExploreController` zaten `[AllowAnonymous]` + "Authentication is optional" yorumuyla anonim taramaya izin veriyor. Trip Detail/Budget Summary'nin de **gerçekten anonim** erişime açık olması gerekir — "misafir" burada sadece "başka bir login olmuş kullanıcı" değil, **giriş yapmamış kullanıcıyı da kapsar**.

- [x] `GetTripByIdQueryHandler`'a kontrol eklenir: `trip.Status != TripStatus.Published && trip.OwnerId != currentUserId` ise **`EntityNotFoundException`** fırlatılır (403/`ForbiddenException` değil — "yetkin yok" yerine "bulunamadı" denir, private trip'in var olduğu bile sızdırılmaz)
- [x] Anonim (giriş yapmamış) kullanıcı için de aynı kural geçerli — `currentUserId` yoksa `trip.Status != Published` durumunda 404 (mevcut `GetTripByIdQueryHandler`'daki `Guid.TryParse(_authenticatedUserService.UserId, ...)` pattern'i zaten anonim'i güvenli ele alıyor, `IsUpvoted`/`IsSaved` için kullanılan aynı desen)
- [x] **`[AllowAnonymous]` attribute'ü `TripsController.GetById` action'ına eklenir** — `[Authorize]` sınıf seviyesinden miras kalmasın
- [x] Integration test: guest/başka user'ın Draft/Archived trip'e erişimi 404 dönmeli; owner kendi Draft/Archived trip'ini görebilmeli; **anonim (token'sız) istek Published trip'i görebilmeli, Draft/Archived'de 404 almalı**
- [x] `EntityNotFoundException` → `404 Not Found` mapping'i mevcut `ErrorHandlerMiddleware` üzerinden HTTP integration testleriyle teyit edilir

**Ek düzeltme (Budget privacy kararı — bütçe herkese açık, anonim dahil):** `GetBudgetSummaryQueryHandler.cs:36-37`'de şu an **katı owner-only** kontrolü var (`if (trip.OwnerId != currentUserId) throw new ForbiddenException(...)`). Trip Detail'deki Toplam Bütçe satırı artık **anonim dahil herkese açık** olacağı için bu kısıtlama **`GetTripByIdQueryHandler` ile aynı duruma (status-bazlı)** çevrilir:
- [x] `GetBudgetSummaryQueryHandler`'daki katı `ForbiddenException` kuralı kaldırılır, yerine `trip.Status != TripStatus.Published && trip.OwnerId != currentUserId` → `EntityNotFoundException` konur (Published trip'lerde owner olmayan **ve anonim** de görebilir; Draft/Archived'de sadece owner)
- [x] **`[AllowAnonymous]` attribute'ü `TripsController.GetBudgetSummary` action'ına eklenir**
- [x] `GetBudgetSummaryQueryHandler`'a da `GetTripByIdQueryHandler`'daki gibi anonim-güvenli `Guid.TryParse` deseni eklenir (şu an `Guid.Parse(_authService.UserId)` kullanıyor — anonim istekte `UserId` null/boş olursa bu **exception fırlatır**, `TryParse` ile güvenli hale getirilmeli)
- [x] İlgili unit/integration testler güncellenir (mevcut "sadece owner görebilir" testleri artık "Published'da herkes + anonim, Draft/Archived'de sadece owner" olarak değişir)

**⚠️ Kapsam genişletmesi (kod incelemesinde 2 kardeş handler'da aynı sınıf hata bulundu):** Bu güvenlik açığı sadece `GetTripByIdQuery`'ye özgü değilmiş — trip'in **child kaynaklarını** okuyan diğer handler'larda da aynı desen eksik/tutarsız:

- **`GetTimelineQueryHandler.cs:39-44`** — `trip.Status != Published` durumunda `Guid.Parse(_authService.UserId)` çağırıyor (satır 41), **`TryParse` değil**. Anonim istek geldiğinde `UserId` boş/null olduğu için bu **`FormatException` fırlatır** (temiz bir 404 yerine beklenmeyen 500 hatası)
- **`GetTripDestinationsQueryHandler.cs:51`** — sadece `TripStatus.Draft` için owner-only kontrolü var, **`Archived` hiç kontrol edilmiyor**. Yani şu an **Archived bir trip'in destinasyonları owner olmayan herkese açık** — bu, B0.10'un asıl çözmeye çalıştığı sorunun aynısı, farklı bir endpoint'te
- **`GetRecommendedPlacesQueryHandler.cs:35-40`** — handler mantığı zaten doğru (`Published != status` ise owner-only), ama aynı güvensiz `Guid.Parse` (satır 37) var **ve** `TripsController.GetRecommendPlaces` action'ında (satır 155) `[AllowAnonymous]` yok — anonim istek handler'a hiç ulaşamıyor (401). Niyet doğru, uygulama eksik.

**Karar (Recommended Places görünürlüğü):** Diğer child kaynaklarla (Timeline, Budget Summary, Destinations) tutarlı olması için **Recommended Places de Published trip'lerde herkese açık (anonim dahil)** olacak — ayrı bir owner/collaborator-only istisna yapılmıyor. "Timeline'a ekle" aksiyonu zaten sadece owner'a görünür, misafir sadece görüntüler. (`omniflow-mobile/OMNIFLOW_PAGE_ARCHITECTURE.md § 8.3` güncellendi.)

**Kök neden:** Bu mantık (trip Published değilse owner-only, 404 ile) her handler'da ayrı ayrı yazılıyor, bu yüzden tutarsızlıklar/eksiklikler çıkıyor. Çözüm: **paylaşılan bir visibility helper**.

- [x] `ITripVisibilityService` (veya basit bir static extension) eklenir — `EnsureVisibleOrThrow(Trip trip, string? currentUserIdString)`: `trip.Status != Published` ise `currentUserIdString`'i güvenli `TryParse` eder, parse başarısız (anonim) veya `trip.OwnerId != currentUserId` ise `EntityNotFoundException` (404) fırlatır
- [x] `GetTripByIdQueryHandler`, `GetBudgetSummaryQueryHandler`, `GetTimelineQueryHandler`, `GetTripDestinationsQueryHandler`, `GetRecommendedPlacesQueryHandler`, **ve B0.9'un `GetTripChecklistStatusQuery`'si** — **hepsi bu paylaşılan helper'ı kullanacak şekilde güncellenir/yazılır**, kendi ad-hoc kontrollerini kaldırırlar (checklist henüz implemente edilmediği için sıfırdan bu helper'la yazılır, diğerleri mevcut ad-hoc kontrollerini helper'a taşır)
- [x] `GetTimelineQueryHandler` ve `GetRecommendedPlacesQueryHandler`'daki güvensiz `Guid.Parse` → helper üzerinden güvenli hale gelir
- [x] `GetTripDestinationsQueryHandler`'a eksik olan `Archived` kontrolü eklenir (helper kullanılınca otomatik gelir)
- [x] **`[AllowAnonymous]` attribute'ü `TripsController.GetRecommendPlaces` action'ına eklenir**
- [x] Bu handler'lardaki mevcut `ForbiddenException` (403) davranışı da **`EntityNotFoundException` (404)**'a çevrilir — B0.10'un "private trip'in varlığı bile sızdırılmasın" prensibiyle tutarlı olsun
- [x] Integration test: **6 endpoint** (GetById, BudgetSummary, Timeline, Destinations, Checklist, RecommendPlaces) için de Draft/Archived + anonim/non-owner → 404; owner → 200; Published + anonim/herkes → 200

---

### Task B0.11: Trip Unarchive / Yeniden Yayınla

**Tahmini Süre:** 1.5 saat
**Durum:** [x] Tamamlandı

> **Bağlam:** Şu an `Draft → Published → Archived` tek yönlü bir akış; Archived durumdan geri dönüş yok (sadece Delete mümkün). Trip Detail üst bar menüsünde Archived trip'ler için **"Yayına Al"** seçeneği isteniyor — bu, backend'de yeni bir komut gerektiriyor.
>
> **Mobil karşılığı:** `MOBILE_ROADMAP.md → M3 / Task 3.2` (üst bar `⋮` menü — durum bazlı içerik), tasarım detayları `omniflow-mobile/TRIP_DETAILS_PAGE.md`

- [x] `UnarchiveTripCommand` + Handler — `ArchiveTripCommandHandler` ile simetrik yapı: owner kontrolü (`ForbiddenException`), sadece `TripStatus.Archived` durumundaki trip'lere uygulanabilir (`ApiException` diğer durumlarda)
- [x] `trip.Status = TripStatus.Published` olarak günceller
- [x] Endpoint: `POST /api/v1/Trips/{id}/unarchive` *(büyük T — `TripsController`'ın `[controller]` token casing'iyle tutarlı, B0.14'teki `unpublish` ile aynı)*
- [x] Unit + integration test (owner/non-owner, yanlış durumdan unarchive denemesi)

**Owner Menü İçeriği (durum bazlı, mobil tarafta uygulanacak):**

| Trip Durumu | Menü İçeriği |
|---|---|
| **Draft** | Yayınla · Sil |
| **Published** | Arşivle · **Düzenlemek için Taslağa Al** · Paylaş · Sil |
| **Archived** | Yayına Al · Sil *(**Paylaş yok** — Archived owner-only, B0.10 gereği 404 döner; paylaşılan link işe yaramaz. Draft'ta zaten aynı sebeple Paylaş yok, Archived'da da tutarlı olsun diye kaldırıldı)* |

---

### Task B0.14: Trip Unpublish (Düzenleme İçin Taslağa Al)

**Tahmini Süre:** 1.5 saat
**Durum:** [x] Tamamlandı
**Öncelik:** 🔴 Kritik — ciddi bir ürün kısıtı çözüyor

> **Bulgu:** `UpdateTimelineEntryCommandHandler`, `CreateTimelineEntryCommandHandler`, `DeleteTimelineEntryCommandHandler`, `ReorderTimelineEntriesCommandHandler`, `UpdateTripCommandHandler` ve destinasyon CRUD handler'larının **hepsi** `trip.Status != TripStatus.Draft` ise reddediyor. Backend'de `Published → Draft` geri dönüşü **hiç yok** (sadece `Draft→Published→Archived→(B0.11 ile)Published` döngüsü var). Sonuç: **bir trip yayınlandığı anda timeline'ı sonsuza kadar donuyor** — owner bir daha hiçbir entry ekleyemez/düzenleyemez/silemez/kilit açamaz, Archived'a alıp tekrar Published yapsa bile. Bu, gerçek kullanıcı senaryosunda (yayınladıktan sonra plan değişmesi) çok kısıtlayıcı.
>
> **Mobil karşılığı:** `MOBILE_ROADMAP.md → M3 / Task 3.2` (üst bar `⋮` menü, Timeline entry Edit/Delete/Unlock aksiyonları), tasarım detayı `omniflow-mobile/TRIP_DETAILS_PAGE.md`

**Karar:** `ArchiveTripCommandHandler`'a simetrik yeni bir komut — `UnpublishTripCommand`. Draft-only kısıtları **gevşetilmiyor** (Timeline entry mutasyonları hâlâ Draft-only kalıyor), bunun yerine owner trip'i **geçici olarak Draft'a alıp** düzenleyip **tekrar Yayınla**yabiliyor.

- [x] `UnpublishTripCommand` + Handler — owner kontrolü (`ForbiddenException`), sadece `TripStatus.Published` durumundaki trip'lere uygulanabilir (`ApiException` diğer durumlarda — Archived'dan direkt Unpublish yok, önce Unarchive (B0.11) ile Published'a dönülür, sonra Unpublish edilir; state machine basit tutulur)
- [x] `trip.Status = TripStatus.Draft` olarak günceller
- [x] **Sayaçlar korunur:** `UpvoteCount`, `ForkCount`, mevcut `SavedTrip` ilişkileri **silinmez/sıfırlanmaz** — trip Draft'tayken bu sayılar sadece owner'a görünür kalır (B0.10 visibility kuralı gereği), tekrar Publish edilince aynen geri gelir
- [x] Endpoint: `POST /api/v1/Trips/{id}/unpublish`
- [x] Unit + integration test (owner/non-owner, Draft/Archived'den Unpublish denemesi reddedilmeli, sayaçların korunduğu)

**Mobil davranış:**
- [x] Üst bar Owner menüsüne **Published** durumunda **"Düzenlemek için Taslağa Al"** eklenir (yukarıdaki tabloya bakınız)
- [x] Tıklanınca **onay dialogu**: "Bu geziyi düzenlemek için yayından kaldıracaksın — düzenleme bitince tekrar yayınlaman gerekecek. Devam edilsin mi?" + İptal/Devam Et
- [x] Onaylanınca `POST /api/v1/Trips/{id}/unpublish` çağrılır, başarılı olursa trip Draft'a döner, Timeline'daki Edit/Kilidi Aç/Sil/+Detay Ekle aksiyonları artık aktif olur
- [x] Trip Draft'tayken herkese kapalıdır (B0.10), owner "Yayınla" ile düzenleme bitince tekrar Published'a alır

**Doğrulama:** `dotnet build OmniFlow\OmniFlow.sln --configuration Release`, B0.14 unit testleri ve B0.14 integration testleri geçti. Solution-level unit test gate geçti. Npgsql legacy timestamp switch sonrası provider seed timestamp problemi çözüldü; full integration suite hâlâ B0.14 dışı eski test beklentileri / ayrı endpoint davranışları nedeniyle kırılıyor.

---

### Task B0.12: TripDestination Koordinat (Geocoding)

**Tahmini Süre:** 3.5 saat *(Nominatim operasyonel gereksinimleri nedeniyle 2.5 saatten güncellendi)*
**Durum:** [x] Tamamlandı

> **Bulgu:** Trip Detail'in haritası (`omniflow-mobile/TRIP_DETAILS_PAGE.md`, MapLibre pinler + ORS yol modu) destinasyon koordinatlarına ihtiyaç duyuyor, ama `TripDestinationResponse` sadece `City`/`Country`/`ArrivalDate`/`DepartureDate`/`OrderIndex`/`NightCount` döndürüyor — **koordinat yok**. `Place` entity'sinde `Latitude`/`Longitude` zaten var (OSM/Google Places'ten önceden import edilmiş), ama bu veri statik — çalışma zamanında "bu şehri geocode et" diyebilecek bir servis **hiç yok**.
>
> **Mobil karşılığı:** `MOBILE_ROADMAP.md → M3 / Task 3.2` (Trip Detail — Map bölümü), tasarım detayı `omniflow-mobile/TRIP_DETAILS_PAGE.md`

**Karar:** Backend, destinasyon oluşturulduğunda/güncellendiğinde City+Country'yi **bir kere** geocode edip `TripDestination`'a kaydeder (mobil client-side geocoding yok, her seferinde tekrar istek atılmaz).

> **⚠️ Nominatim operasyonel kısıtlar (resmi kullanım politikası):** Public Nominatim instance'ı (`nominatim.openstreetmap.org`) **max 1 istek/saniye**, geçerli/tanımlayıcı bir `User-Agent` (jenerik/tarayıcı UA'sı değil), **sonuçların cache'lenmesi zorunlu**, ve ağır kullanımda self-host önerisi şartlarını taşıyor. Wizard tek seferde **10 destinasyona kadar** oluşturabildiği için, bunlar naif şekilde paralel/hızlı ardışık geocode edilirse hem rate-limit'e takılır hem policy ihlali olur.

- [x] `TripDestination` entity'sine `Latitude` (double?), `Longitude` (double?) alanları + migration
- [x] `IGeocodingService` arayüzü + **Nominatim** implementasyonu — `City, Country` string'ini koordinata çevirir. Arayüz sağlayıcıdan bağımsız tasarlanır (ileride self-hosted Nominatim veya başka bir servise **config ile** geçilebilsin — base URL + provider seçimi `appsettings.json`'da)
- [x] **Reverse geocoding:** `IReverseGeocodingService` (veya `IGeocodingService` içinde ayrı reverse method) eklenir — `Latitude, Longitude` değerini şehir/ülke veya formatted location text'ine çevirir. Bu altyapı B0.5.1'de saklanan `User.LocationLatitude` / `User.LocationLongitude` değerlerinden `User.Location` üretmek için kullanılacak.
- [x] **Rate limiting:** Servis içinde bir kuyruk/semaphore ile giden istekler **max 1 istek/saniye** olacak şekilde sınırlanır (10 destinasyonluk bir wizard submit'i naif paralel çağrı yerine sıraya alınır)
- [x] **Cache:** Geocode sonuçları `(City, Country)` normalize edilmiş anahtarla **kalıcı (DB tablosu)** olarak cache'lenir — production'da in-memory cache yetersiz kalır (uygulama yeniden başlayınca/birden fazla instance'ta kaybolur, Nominatim policy'sinin "sonuçları cache'le" şartını kalıcı karşılamaz). In-memory sadece **dev/local'da DB'ye ek bir L1 hız katmanı** olarak opsiyonel kullanılabilir, tek başına yeterli değil. Aynı şehir (ör. "Paris, France") birden fazla trip/kullanıcı tarafından istenirse Nominatim'e tekrar gidilmez — bu, pratikte rate-limit baskısının büyük kısmını da çözer (popüler şehirler zaten cache'te olur)
- [x] **User-Agent:** İsteklerde uygulamayı tanımlayan özel bir `User-Agent` header'ı gönderilir (ör. `OmniFlow/1.0 (+iletişim e-postası)`), jenerik/varsayılan HTTP client UA'sı kullanılmaz
- [x] **Timeout:** Makul bir timeout (ör. 5 sn) — Nominatim yanıt vermezse hata toleransı kuralına (aşağıda) düşer, wizard submit'ini süresiz bekletmez
- [x] `CreateTripDestinationCommandHandler`, `UpdateTripDestinationCommandHandler` ve `CreateTripWizardCommandHandler` (destinasyon oluşturan/güncelleyen üç nokta) geocoding çağrısını yapacak şekilde güncellenir — **senkron ve cache+rate-limit korumalı** (M3 kapsamında background job/kuyruk sistemi **bilinçli olarak kurulmuyor** — basitlik tercih edildi). **Gerçekçi zamanlama:** 1 istek/saniye limiti nedeniyle, en kötü senaryoda (10 destinasyonun **hepsi cache miss** — yani hiçbiri daha önce başka bir trip'te geocode edilmemiş) wizard submit'i **10+ saniyeye kadar uzayabilir** (timeout'larla daha da fazla). Pratikte çoğu trip popüler şehirler içerdiği için cache sayesinde çok daha hızlı olması beklenir, ama worst-case bu kadar sürebileceği bilinerek kabul ediliyor. Mobil tarafta wizard submit zaten bir loading state gösteriyor, bu süre o loading state içinde karşılanır. İleride gerçek bir kullanıcı şikayeti/performans sorunu olursa arka plan job'una geçiş değerlendirilebilir
- [x] **Hata toleransı:** Geocoding başarısız olursa (servis erişilemez, timeout, şehir bulunamaz vb.) `Latitude`/`Longitude` `null` kalır — komut **hata fırlatmaz**, destinasyon yine de oluşturulur/güncellenir; mobil tarafta o destinasyon için pin gösterilmez, diğer pinler etkilenmez
- [x] **User profile reverse geocoding entegrasyonu:** B0.5.1 koordinatları geldiğinde backend `Location` text'i otomatik üretebilecek hale getirilir. Reverse geocoding başarısız olursa profil update'i başarısız olmaz; mevcut `Location` korunur veya request'te gelen manuel `Location` kullanılır.
- [x] `TripDestinationResponse`'a `Latitude`/`Longitude` (nullable) eklenir
- [x] Unit test: geocoding başarılı/başarısız senaryoları, mevcut destinasyon davranışının bozulmadığı


**Doğrulama:** `dotnet build OmniFlow\OmniFlow.sln --configuration Release`, B0.12 unit testleri ve B0.12 hedefli integration testleri geçti. Solution-level unit test gate geçti. Npgsql legacy timestamp switch sonrası provider seed timestamp problemi çözüldü; full integration suite hâlâ B0.12 dışı eski test beklentileri / ayrı endpoint davranışları nedeniyle kırılıyor.

---

### Task B0.13: TimelineEntry → Checklist Bağlantısı (PlanningSlotKey)

**Tahmini Süre:** 1.5 saat
**Durum:** [x] Tamamlandı

> **Bulgu:** Trip Detail'in Detay Modalı, bir Flights/Hotels checklist satırı (ör. "İstanbul → Roma" leg'i) için **gerçek bir TimelineEntry'nin var olup olmadığını** göstermesi gerekiyor (bkz. `omniflow-mobile/TRIP_DETAILS_PAGE.md → Detay Modal — İçerik / Durum A vs B`). Bunu client-side **şehir adı/tarih heuristiği** ile yapmak kırılgan — aynı leg/gece için birden fazla `CustomFlight`/`CustomTransport`/`CustomAccommodation` entry'si varsa (ör. kullanıcı önce uçuş sonra taksi transferi eklediyse, veya oteli değiştirdiyse) **yanlış entry'ye bağlanma riski** var.
>
> **Mobil karşılığı:** `MOBILE_ROADMAP.md → M3 / Task 3.2`, tasarım detayı `omniflow-mobile/TRIP_DETAILS_PAGE.md → Detay Modal — İçerik`

**Karar:** `TimelineEntry`'ye opsiyonel `PlanningSlotKey` (string?) alanı eklenir — B0.9'daki `itemKey` formatıyla **aynı değer** (`flight-leg:{fromId}:{toId}` / `hotel-night:{destId}:{nightNumber}`). Bu alan **sadece** Detay Modal'ın "**+ Detay Ekle**" CTA'sından (Durum B → yeni entry oluşturma akışı) geçilen entry'lere set edilir — normal Timeline entry ekleme akışından oluşturulan entry'ler bu alanı `null` bırakır ve checklist'e otomatik bağlanmaz (checklist confirmation o entry'lerden **bağımsız, manuel** kalmaya devam eder).

- [x] `TimelineEntry` entity'sine `PlanningSlotKey` (string?, nullable) alanı + migration
- [x] **Partial unique index** — `(TripId, PlanningSlotKey)` where **`planning_slot_key IS NOT NULL AND deleted_at IS NULL`** (`TimelineEntry` soft-delete kullanıyor — `deleted_at` filtresi olmadan, silinmiş bir entry aynı slot'u "kullanılmış" gibi tutar ve o slota **yeni entry eklemeyi kalıcı olarak engeller**; bu yüzden filtreye `deleted_at IS NULL` şart)
- [x] **DTO/Command zinciri tam olarak güncellenir** (sadece prosa değil, somut dosyalar):
  - `CreateTimelineEntryRequest.cs` → opsiyonel `PlanningSlotKey` (string?) alanı eklenir
  - `CreateTimelineEntryCommand.cs` → aynı alan eklenir, controller'dan handler'a taşınır
  - `CreateTimelineEntryCommandHandler` → `PlanningSlotKey`'i yeni `TimelineEntry`'ye set eder (factory metodlarına parametre olarak eklenir veya oluşturulduktan sonra atanır)
  - `TimelineEntryResponse.cs` → `PlanningSlotKey` (string?) response'a eklenir
  - AutoMapper profili güncellenir (entity → response mapping'inde bu alan otomatik gelsin)
  - Detay Modal'ın "+ Detay Ekle" CTA'sı bu alanı doldurur, normal Timeline ekleme akışı (Task 3.13'ün standart formu) boş bırakır
- [x] **`PlanningSlotKey` sadece create'te set edilir, immutable'dır:** `UpdateTimelineEntryRequest`/`UpdateTimelineEntryCommand`'a **bu alan bilinçli olarak eklenmez** — yani bir entry oluşturulduktan sonra hangi slot'a bağlı olduğu **değiştirilemez**. Kullanıcı bir entry'yi başka bir slot'a bağlamak isterse (nadir senaryo), mevcut entry'yi **siler ve yeni bir entry oluşturur** (yeni entry oluştururken doğru `planningSlotKey` ile). Bu, slot ownership'in Update akışı üzerinden karışmasını (ör. yanlışlıkla başka bir slot'un key'ini üzerine yazma) baştan engeller
- [x] **Entry silinirse checklist confirmation'a ne olur:** `TripChecklistConfirmation` (B0.9), `TimelineEntry`'den **tamamen bağımsız bir tablo** — entry silinse bile confirmation kaydı **silinmez/değişmez**. Yani kullanıcı "İstanbul→Roma" uçuşunu ekleyip checklist'i işaretledikten sonra o entry'yi silerse, checklist **checked kalır** (kullanıcının "hallettim" beyanı entry'nin varlığından bağımsız bir gerçek) — sadece bir sonraki modal açılışında **Durum B**'ye döner (`PlanningSlotKey` eşleşen entry artık yok)
- [x] `GetTimelineQuery` response'larına `PlanningSlotKey` eklenir (mobil, Durum A/B ayrımını artık **exact match** ile yapar: `entries.any { it.planningSlotKey == itemKey }` — şehir/tarih heuristiği yok). Not: `GetTripByIdQuery` timeline entry listesi dönmediği için bu endpointte eklenecek ayrı `PlanningSlotKey` alanı yoktur.
- [x] Unit/integration test: aynı slot'a ikinci kez entry oluşturma denemesi engelleniyor; **soft-delete edilmiş bir entry'nin slot'u serbest bıraktığı** (yeni entry aynı `planningSlotKey` ile oluşturulabiliyor) test edilir; normal akıştan oluşan entry'lerin `PlanningSlotKey`'i null kalıyor; fork edilen entry'lerde eski slot key'i taşınmıyor

**Doğrulama:** `dotnet build OmniFlow\OmniFlow.sln --configuration Release`, B0.13 unit hedefleri, solution-level unit test gate ve `TimelineControllerTests` integration suite geçti. Npgsql legacy timestamp switch sonrası provider seed timestamp problemi çözüldü; full integration suite hâlâ B0.13 dışı eski test beklentileri / ayrı endpoint davranışları nedeniyle kırılıyor.

---

### Task B0.15: ORS Routing Proxy ("Yol" Modu)

**Tahmini Süre:** 2 saat
**Durum:** [x] Tamamlandı

> **Bulgu:** Map'in `Yol` modu OpenRouteService (ORS) polyline kullanıyor (bkz. `omniflow-mobile/TRIP_DETAILS_PAGE.md`), ama **kimin ORS'u çağıracağı hiç netleşmemiş**. ORS, Nominatim'in aksine **API key gerektiriyor** (ücretsiz tier'da bile). Mobil doğrudan ORS'u çağırırsa, **API key APK içine gömülür** — decompile edilip çıkarılabilir, kötüye kullanılırsa uygulamanın günlük ORS kotası (free tier'da sınırlı) tüketilip **herkes için** Yol modu çalışmaz hale gelir. Bu, Nominatim'den farklı bir risk (Nominatim key istemiyor).
>
> **Mobil karşılığı:** `omniflow-mobile/TRIP_DETAILS_PAGE.md → Map — 2 Mod` (Yol modu ORS polyline)

**Karar:** ORS çağrısı **backend üzerinden proxy'lenir** — mobil ORS'u hiç doğrudan çağırmaz, API key backend'de (appsettings/secret olarak) kalır, hiçbir zaman client'a gönderilmez.

- [x] `IRoutingService` arayuzu + OpenRouteService implementasyonu eklendi; API key backend config/secret tarafinda kalir
- [x] Null koordinat ve segment fallback davranisi uygulandi; eksik koordinatta ORS cagrisi yapilmaz
- [x] DB cache eklendi: `trip_route_caches`, trip FK cascade ve route signature destination + timeline category sinyallerini icerir
- [x] Endpoint: `GET /api/v1/trips/{tripId}/routes` -> segment bazli `Walking`/`Cycling`/`Driving` response
- [x] Visibility kurali B0.10 ile ayni: Published public, Draft/Archived owner-only, yetkisiz private read 404
- [x] ORS hata/timeout/429 durumunda endpoint 500 firlatmaz; ilgili mod bos rota doner
- [x] Unit/integration test: ORS fallback, GeoJSON parse, route response, cache invalidation, cascade cleanup ve owner/anonim/Published-Draft erisim matrisi
**Dogrulama:** `dotnet build OmniFlow\OmniFlow.sln --configuration Release`, solution-level unit gate (`435/435`), B0.15 targeted integration (`5/5`) ve full integration suite (`302/302`) gecti.

---

### Task B0.16: M6 Admin Stats & Güvenlik Tamamlama

**Tahmini Süre:** 2 saat
**Durum:** [x] Tamamlandı

> **Mobil karşılığı:** `MOBILE_ROADMAP.md → Task 6.1-6.5`

- [x] `GET /api/v1/admin/stats` — silinmemiş kullanıcı/trip/post toplamları, İstanbul takvimine göre bugün ve bu hafta sayaçları
- [x] Blocked-users listesi yalnız oturum sahibinin kendi `userId` değeri için okunabilir; başka kullanıcı hedefi `403`
- [x] Suspend/unsuspend komutu admin hesaplarını hedef alamaz; kendi hesap korumasına ek olarak diğer adminler de korunur
- [x] UTC saat kaynağı `IDateTimeService` üzerinden test edilebilir hale getirildi
- [x] Unit ve API integration testleri eklendi

---

### Definition of Done (B0)

- [x] Dokümanlar koddaki gerçeği yansıtıyor
- [x] `dotnet test` çözüm seviyesinde çalışıyor ve CI'da koşuyor
- [x] Provider verisinin tazeliği API'den görülebiliyor
- [x] User profili konum ve seyahat stili alanlarını destekliyor
- [x] Draft trip'lerde tamamlanma yüzdesi hesaplanıp dönüyor
- [x] Published trip'lerde görüntülenme sayısı (`ViewCount`, anonim dahil) doğru artıyor ve gösteriliyor
- [x] Trip kapak fotoğrafı yüklenebiliyor
- [x] Trip Detail Review modundaki checklist satırları işaretlenip kalıcı olarak saklanabiliyor
- [x] Draft/Archived trip'ler owner olmayan kullanıcılara 404 dönüyor (B0.10)
- [x] Archived trip'ler tekrar Published durumuna alınabiliyor (B0.11)
- [x] Destinasyonlar koordinat ile dönüyor, Trip Detail haritasında pinlenebiliyor (B0.12)
- [x] Checklist satırları ile TimelineEntry'ler arasında belirsizlik olmadan (exact match) bağlantı kuruluyor (B0.13)
- [x] Owner, yayınladığı bir trip'i düzenlemek için Taslağa alıp tekrar yayınlayabiliyor, sayaçlar korunuyor (B0.14)
- [x] Map'in Yol modu backend proxy üzerinden çalışıyor, ORS API key client'a hiç gitmiyor (B0.15)
- [x] M6 admin dashboard sayaçları canlı API'den geliyor; block-list gizliliği ve admin suspend koruması uygulanıyor (B0.16)

> **📝 Düşük öncelikli not (gelecekte değerlendirilebilir):** `TripsController` (`[controller]` token → `/api/v1/Trips/...`, büyük T) ile `TimelineController` (literal route override → `/api/v1/trips/...`, küçük t) arasında casing tutarsızlığı var. ASP.NET Core route matching genelde case-insensitive çalıştığı için şu an **işlevsel bir sorun yaratmıyor**, ama Retrofit contract'larında gereksiz kafa karışıklığına yol açabiliyor. Kısa vadede dokümanlarda gerçek casing'in yazılması (yapıldı) yeterli; orta/uzun vadede tüm controller route'larının lowercase literal'e standardize edilmesi düşünülebilir (B0 kapsamında zorunlu değil).

---

## 🎯 B1 — Google OAuth (External Login)

### Scope

Email/şifre + JWT akışının yanına Google ile giriş eklenir. Mobilde Google Sign-In SDK ID token üretir; backend bu token'ı doğrular, kullanıcıyı bulur/oluşturur ve kendi JWT'sini döner.

**Mobil karşılığı:** `MOBILE_ROADMAP.md → M7`

### B1 Kararları / Contract

- **Token doğrulama:** Backend sadece Google `idToken` kabul eder; access token veya authorization code bu fazın kapsamı değildir. `GoogleJsonWebSignature.ValidationSettings` içinde `Audience` olarak birden fazla client id desteklenir.
- **Allowed client id listesi:** Android ve Web client id'leri `GoogleAuthSettings.AllowedClientIds` listesinde tutulur. Yarın iOS/web eklenirse aynı listeye yeni client id eklenir; repo'ya gerçek client id yazılmaz.
- **Email doğrulama:** Google payload `email_verified=false` dönerse login reddedilir ve `401 Unauthorized` döner. Google ile kayıt olan kabul edilmiş kullanıcılar için `ApplicationUser.EmailConfirmed = true` ve domain `User.IsVerified = true` yazılır; ayrıca doğrulama maili gönderilmez.
- **Username üretimi:** Önce Google `name` normalize edilir (`Yiğit Özgür` → `yigit_ozgur`). `name` boşsa email local-part kullanılır (`yigit@gmail.com` → `yigit`). Türkçe karakterler ASCII karşılığına çevrilir, boşluklar `_` olur, geçersiz karakterler temizlenir. Çakışma varsa sıralı suffix denenir: `yigit_ozgur_1`, `yigit_ozgur_2`, ...
- **Account linking:** Aynı email ile mevcut e-posta/şifre hesabı varsa yeni kullanıcı oluşturulmaz; Google provider kaydı mevcut `ApplicationUser` hesabına bağlanır ve kullanıcının eski trip/profil verisi korunur.
- **Response contract:** Google login response'u normal email/şifre login ile aynı formatta kalır: OmniFlow JWT access token + refresh token + mevcut kullanıcı detayları. Mobil token/session yönetimi ayrı bir response shape öğrenmek zorunda kalmaz.
- **Secrets:** Local geliştirmede `dotnet user-secrets`, canlıda Azure App Service Configuration / environment variables kullanılır. Örnek env formatı: `GoogleAuth__AllowedClientIds__0`, `GoogleAuth__AllowedClientIds__1`.

### Task B1.1: Google Token Doğrulama Altyapısı

**Tahmini Süre:** 2 saat
**Durum:** [x] Tamamlandı

- [x] `Google.Apis.Auth` NuGet paketi
- [x] `GoogleAuthSettings.cs` — `AllowedClientIds: List<string>` (Android + Web client id'leri; ileride iOS eklenebilir)
- [x] `IGoogleTokenValidator` + implementasyon — gelen ID token'ı doğrular, audience listesini kontrol eder, payload (email, email_verified, name, sub, picture) döner
- [x] Geçersiz/expired/audience uyuşmayan token → `ApiException` (401)
- [x] `email_verified=false` veya email boş/null → `ApiException` (401)

**Doğrulama:** `dotnet build OmniFlow\OmniFlow.sln --configuration Release`, solution-level unit test gate (`453/453`) ve full integration suite (`313/313`) geçti.

### Task B1.2: External Login Akışı

**Tahmini Süre:** 3 saat
**Durum:** [x] Tamamlandı

- [x] `IAccountService`'e `GoogleLoginAsync(string idToken)` eklensin
- [x] Email ile mevcut `ApplicationUser` bul:
  - [x] Varsa → mevcut kullanıcıya Google provider link'i ekle/varsa tekrar kullan, JWT + refresh token üret
  - [x] Yoksa → yeni `ApplicationUser` + aynı Id ile Domain `User` oluştur; `EmailConfirmed = true`, `IsVerified = true`
- [x] Username üretimi: Google `name` → normalize; yoksa email local-part; çakışmada sıralı suffix (`_1`, `_2`, ...)
- [x] `AspNetUserLogins` üzerinden Google provider eşlemesi kaydedilsin
- [x] `GoogleLoginRequest.cs` (IdToken) + validator
- [x] `POST /api/account/google` endpoint'i — body: `{ "idToken": "..." }`, response normal login response'u ile aynı
- [x] Google ile yeni kayıt akışında doğrulama maili gönderilmez

### Task B1.3: Test & Doğrulama

**Tahmini Süre:** 1 saat
**Durum:** [x] Tamamlandı

- [x] Geçerli token → 200 + token pair
- [x] Yeni kullanıcı otomatik oluşuyor, `EmailConfirmed=true`, `IsVerified=true`, ikinci girişte aynı kullanıcı dönüyor
- [x] Mevcut email/password hesabı Google login ile aynı hesaba linkleniyor; yeni duplicate user oluşmuyor
- [x] Geçersiz token → 401
- [x] `email_verified=false` → 401
- [x] Audience listesinde olmayan client id → 401
- [x] Username çakışması sıralı suffix ile çözülüyor
- [x] Eşzamanlı aynı isimli Google kayıtları 500'e düşmeden benzersiz username alıyor
- [x] Response shape email/password login ile aynı kalıyor

**Doğrulama:** `dotnet build OmniFlow\OmniFlow.sln --configuration Release`, solution-level unit test gate (`453/453`) ve full integration suite (`313/313`) geçti.

### Definition of Done (B1)

- [x] Mobil bir Google ID token gönderip OmniFlow JWT'si alabiliyor
- [x] Hem yeni hem mevcut kullanıcı senaryosu çalışıyor
- [x] Username çakışması güvenli biçimde çözülüyor
- [x] Doğrulanmamış Google email'i kabul edilmiyor
- [x] Google client id'leri repo'ya yazılmadan config/secret üzerinden çalışıyor

---

## 🎯 B2 — Live Trip Altyapısı

### Scope

Seyahat sırasında kullanım için backend desteği: gerçek ziyaret kayıtları (Visit Log), trip kapanış özeti (Trip Summary) ve zaman dilimi normalizasyonu.

**Mobil karşılığı:** `MOBILE_ROADMAP.md → M8`

### Karar: Trip yayın durumu ve seyahat zamanı ayrıdır

- Mevcut `TripStatus` değerleri (`Draft`, `Published`, `Archived`) korunur; `Active` ve `Completed` bu enum'a eklenmez.
- Trip'in zamansal durumu veritabanında ayrıca tutulmaz. `StartDate` ve `EndDate` üzerinden `executionState` olarak hesaplanır:
  - `Upcoming`: güncel yerel tarih `< StartDate`
  - `Active`: `StartDate <=` güncel yerel tarih `<= EndDate`
  - `Completed`: güncel yerel tarih `> EndDate`
- Böylece bir trip aynı anda örneğin `status=Published` ve `executionState=Active` olabilir.
- `Archived`, seyahatin tamamlandığı anlamına gelmez; yalnızca kullanıcının trip'i arşive kaldırdığını belirtir.
- `executionState` hesaplamasında kullanılacak yerel tarih/timezone kuralı Task B2.4 kapsamında merkezi olarak uygulanır.
- Bu karar yeni bir status kolonu veya migration gerektirmez; gerekli trip response'larına hesaplanan `executionState` alanı eklenir.

### Task B2.1: Visit Log Entity

**Tahmini Süre:** 4 saat
**Durum:** [x] Tamamlandı

- [x] `PlaceVisitLog.cs` entity (`AuditableBaseEntity`, soft-delete uyumlu) — `TripId`, `TripDestinationId` (required), `TimelineEntryId` (nullable), `PlaceId` (nullable), `UserId`, `ActualCost` (decimal?), `CurrencyCode`, `Rating` (1-5?), `Note` (nullable), `VisitedAt` (UTC `DateTime`)
- [x] `PlaceVisitLogConfiguration.cs` (CHECK: rating 1-5, cost ≥ 0) + index (trip_id, user_id)
- [x] Para alanları: `ActualCost`/`ConvertedActualCost` `numeric(18,2)`, `ExchangeRate` `numeric(18,8)`; currency kodları uppercase 3 karakter
- [x] `VisitedAt` PostgreSQL `timestamp with time zone`; domain/application katmanında yalnızca UTC `DateTime` kabul edilir
- [x] Timeline entry'ye bağlı loglar için `TimelineEntryId` üzerinde `deleted_at IS NULL` filtreli unique index; bir timeline entry için en fazla bir aktif Visit Log
- [x] Bağlantı CHECK constraint'i: `TimelineEntryId` ve `PlaceId` alanlarından tam olarak biri dolu olmalı (XOR)
- [x] `TripDestinationId` foreign key + `(trip_destination_id, visited_at)` index; destination silme davranışı log geçmişini koruyacak şekilde restrict/soft-delete uyumlu olsun
- [x] Migration

**Karar — Visited ile Visit Log tek akıştır:**

- Timeline entry `visited` işaretlendiğinde aynı işlem içinde, `VisitedAt` dolu ve detay alanları boş olabilen bir Visit Log otomatik oluşturulur.
- `ActualCost`, `Rating` ve `Note` zorunlu değildir; kullanıcı bunları ilk işaretleme sırasında veya daha sonra tamamlayabilir.
- `TimelineEntry.IsVisited` / `VisitedAt` ile Visit Log birbirinden bağımsız değiştirilemez; command aynı transaction içinde iki tarafı senkron tutar.
- Timeline entry'ye bağlı Visit Log silindiğinde entry tekrar `unvisited` yapılır ve `VisitedAt` temizlenir.
- Aynı entry'nin tekrar `visited` işaretlenmesi ikinci kayıt üretmez; işlem idempotent davranır.

**Karar — Detaylı Visit Log yanlışlıkla sessizce silinmez:**

- `ActualCost`, `Rating` ve `Note` alanlarının tamamı boşsa unvisited işlemi doğrudan uygulanabilir.
- Bu alanlardan en az biri doluysa mobil, harcama/puan/notun da silineceğini açıkça belirten confirmation gösterir.
- Kullanıcı onayladığında Visit Log soft-delete edilir, timeline entry `unvisited` yapılır ve `VisitedAt` temizlenir; hepsi tek transaction içindedir.
- Backend DELETE endpoint'i owner tarafından verilmiş kesin silme talebi kabul eder; mobil confirmation'a güvenerek güvenlik/yetki kontrolü atlamaz.
- Soft-delete edilmiş kayıt unique index'i bloke etmez; aynı entry daha sonra tekrar visited yapılırsa yeni aktif Visit Log oluşturulabilir.

**Karar — Planlı ve spontane ziyaretler desteklenir:**

- Planlanmış timeline durağından oluşturulan Visit Log hedef olarak `TimelineEntryId`, konumlandırma için entry'den türetilen `TripDestinationId` ile bağlanır.
- Timeline dışındaki spontane ziyaret hedef olarak sistemde mevcut/yakındaki önerilerden seçilen `PlaceId`, konumlandırma için `TripDestinationId` ile bağlanır; ilk sürümde serbest metinle mekân oluşturulmaz.
- Spontane Visit Log, trip timeline'ına yeni entry eklemez ve yayınlanmış planı değiştirmez.
- Trip Summary hem planlı hem spontane Visit Log kayıtlarını gerçek ziyaret toplamına dahil eder; planlanan durak tamamlama oranı yalnızca timeline'a bağlı loglardan hesaplanır.

**Karar — Visit Log yalnızca ziyaret deneyimi kapsamındadır:**

- Timeline'a bağlı Visit Log yalnızca `TimelineEntryType.Place` ve `TimelineEntryType.CustomEvent` için oluşturulabilir.
- `CustomFlight`, `CustomAccommodation` ve `CustomTransport` entry'leri Visit Log/visited aksiyonu sunmaz; desteklenmeyen doğrudan API isteği stabil `VISIT_LOG_UNSUPPORTED_ENTRY_TYPE` hatasıyla reddedilir.
- Trip Summary karşılaştırması aynı kapsamı ölçer: `plannedVisitCost` (Place + CustomEvent plan fiyatları) ile `actualVisitCost` (Visit Log gerçek harcamaları).
- Uçuş, konaklama ve ulaşım mevcut Budget Summary'den "planlanan sabit giderler" olarak ayrı gösterilebilir; gerçek giderleri izlenmediği için bunlarla birleşik "gerçek trip toplamı" üretilmez.
- `Trip.EstimatedCost` veya tüm Budget Summary toplamı yalnızca Visit Log toplamıyla doğrudan karşılaştırılmaz.
- Tüm gerçek giderleri kapsayan ayrı Expense Log sistemi bu milestone'un kapsamı dışındadır.

**Karar — Plan tamamlama yalnızca planlı ziyaretlerden hesaplanır:**

- Payda, silinmemiş `Place` + `CustomEvent` timeline entry sayısı; pay, aktif Visit Log'u bulunan bu planlı entry sayısıdır.
- `visitCompletionPercentage = visitedPlannedEntryCount / plannedVisitableEntryCount × 100` olarak hesaplanır; sonuç `0..100` aralığındadır.
- Spontane Visit Log kayıtları plan tamamlama payına/paydasına girmez; `spontaneousVisitCount` olarak ayrı döner.
- Uçuş, konaklama ve ulaşım entry'leri bu yüzdeye dahil edilmez.
- Bu alan gezi hazırlık/publish completeness metriğinden farklıdır; belirsiz `completionPercentage` adı yerine `visitCompletionPercentage` kullanılır.
- Planlı ziyaret edilebilir entry yoksa yüzde `null`, sayaçlar `0` döner; sahte `%100` üretilmez.

**Karar — Her Visit Log bir trip destinasyonuna aittir:**

- `TripDestinationId` zorunludur ve aynı `TripId` altındaki aktif bir destination'ı göstermelidir.
- Planlı kayıtta destination request'ten alınmaz; backend `TimelineEntry.DestinationId` üzerinden güvenli biçimde doldurur.
- Spontane kayıtta destination ziyaret tarihinden otomatik önerilir; tek eşleşme yoksa client açıkça `TripDestinationId` gönderir.
- Planlı entry, Visit Log ve destination arasında trip/destination uyuşmazlığı varsa command validation hatası döndürür.
- Trip Summary şehir/destinasyon kırılımını `TripDestinationId` üzerinden üretir; `Place.City` string eşleşmesi kimlik kaynağı olarak kullanılmaz.
- `VisitedAt` instant'ının destination timezone'undaki yerel tarihi, bağlı destination'ın `ArrivalDate..DepartureDate` aralığında olmalıdır.

**Karar — Spontane Visit Log bir ziyaret olayıdır:**

- Aynı `PlaceId`, aynı trip/destination içinde farklı `VisitedAt` değerleriyle birden fazla spontane Visit Log'a sahip olabilir; `PlaceId` üzerinde trip-bazlı unique constraint kurulmaz.
- Timeline'a bağlı kayıtlarda bir `TimelineEntryId` için tek aktif log kuralı korunur.
- Trip Summary `totalVisitCount` (tüm ziyaret olayları) ve `uniquePlaceCount` (benzersiz mekânlar) değerlerini ayrı döndürür.
- Aynı mekâna ait tüm gerçek harcamalar toplam hesaba dahil edilir.
- Favorilerde aynı mekân tekrarlı kart üretmez; spontane kayıtta doğrudan `PlaceId`, planlı Place kaydında `TimelineEntry.PlaceId` canonical kimlik olarak kullanılır.
- `CustomEvent` ziyareti `totalVisitCount` içinde sayılır fakat `uniquePlaceCount` içine girmez; `visitedCustomEventCount` ayrı döner.
- Mekân favorileri canonical `PlaceId` bazında gruplanıp `visitCount` ile "N kez ziyaret edildi" bilgisi döner; CustomEvent favorisi kendi `TimelineEntryId` kimliğiyle ayrı item olur.

**Karar — Tekrarlı mekânın kişisel puanı ortalamadır:**

- Aynı `PlaceId` için yalnızca `Rating` değeri dolu Visit Log kayıtlarının aritmetik ortalaması alınır; puansız ziyaret paydaya girmez.
- Favori item'ı `averagePersonalRating`, `ratedVisitCount` ve toplam `visitCount` alanlarını döndürür.
- Ortalama response'ta bir ondalığa yuvarlanır; ham Visit Log rating değerleri değiştirilmez.
- Favoriler önce `averagePersonalRating` azalan, eşitlikte `visitCount` azalan, ardından son ziyaret zamanı azalan şekilde deterministik sıralanır.
- Hiç rating girilmemiş mekân favoriler listesine girmez ancak toplam/benzersiz ziyaret sayaçlarında kalır.

**Karar — Visit Log rating kişisel ve private'dır:**

- `Rating`, nullable tam sayı ve `1..5` aralığındadır; ziyaret kaydetmek için zorunlu değildir.
- Bu değer trip'in public rating'i değildir, `UpvoteCount` değerini etkilemez ve `Place.Rating` ortalamasına katılmaz.
- Public trip/profile/feed DTO'larında dönmez; yalnızca owner Visit Log ve ayrıntılı Trip Summary response'larında bulunur.
- Trip Summary "Gezinin favorileri" listesini rating girilmiş Visit Log'lardan üretir; rating yoksa bu bölüm boş/hidden olabilir.

**Karar — Visit Log notu kısa düz metindir:**

- `Note` opsiyoneldir ve en fazla 1.000 karakter kabul edilir.
- Create/update sırasında baştaki/sondaki boşluklar temizlenir; yalnızca whitespace içeren değer `null` olarak normalize edilir.
- İlk sürümde düz metindir; Markdown/rendered HTML, link preview ve medya eki desteklenmez.
- Backend validator sınırı aşan isteği stabil validation response ile reddeder; response ham HTML olarak render edilmez.

**Karar — `ActualCost` toplam ziyaret harcamasıdır:**

- `ActualCost`, ilgili ziyaret için grubun/gezinin gerçekleşen toplam harcamasını ifade eder; kişi başı tutar değildir.
- Backend `Trip.PersonCount` ile otomatik çarpma veya bölme yapmaz.
- Create/update request, response ve API dokümantasyonunda alan etiketi/açıklaması "toplam harcama" olarak netleştirilir.
- Trip Summary gerçek harcama toplamına `ActualCost` değerini bir kez ekler; kişi sayısı yalnızca bağlamsal bilgi olarak kalır.

**Karar — Eksik harcama ile sıfır harcama ayrıdır:**

- `ActualCost=null`, kullanıcının harcama bilgisi girmediği; `ActualCost=0`, bilinçli olarak ücretsiz/harcamasız ziyaret anlamına gelir.
- `null` değer toplamda sıfır kabul edilmez; yalnızca dolu değerler çevrilip toplanır.
- Trip Summary `visitsWithCostCount`, `missingCostCount` ve `isCostComplete` alanlarını döndürür.
- `missingCostCount > 0` iken gerçek harcama toplamı "kısmi" olarak işaretlenir; kesin toplam/bütçe farkı gibi sunulmaz.
- Harcama eksikliği (`missingCostCount`) ile kur dönüşümü bekleyen harcama (`pendingConversionCount`) ayrı durumlar olarak raporlanır.

**Karar — B2 ve B7 birlikte, otomatik kur dönüşümlü uygulanır:**

- `Trip` entity'sine `BaseCurrencyCode` (ISO 4217) eklenir; wizard seçimi backend'de saklanır. İlk UI seçenekleri en az `TRY`, `USD`, `EUR` olur.
- `BaseCurrencyCode` trip başlamadan ve aktif Visit Log yokken değiştirilebilir; ilk Visit Log oluştuktan veya trip `Active/Completed` olduktan sonra `TRIP_BASE_CURRENCY_LOCKED` ile reddedilir.
- Kullanıcının `PreferredCurrencyCode` tercihi daha sonra değişse bile geçmiş trip'in `BaseCurrencyCode` değeri ve conversion snapshot'ları değişmez; tercih yalnızca ikincil görüntülemeyi etkiler.
- Visit Log, kullanıcının girdiği `ActualCost` + `CurrencyCode` orijinal değerlerini her zaman korur.
- Visit Log currency varsayılanı varsa hedef Place/TimelineEntry `CurrencyCode`, yoksa `Trip.BaseCurrencyCode` olur; kullanıcı desteklenen farklı bir currency seçebilir.
- Farklı para birimindeki harcama, `VisitedAt` tarihindeki kurla trip'in `BaseCurrencyCode` değerine çevrilir.
- Visit Log üzerinde dönüşüm snapshot'ı tutulur: `ConvertedActualCost`, `ExchangeRate`, `ExchangeRateDate` ve dönüşüm hedefi `BaseCurrencyCode`.
- Aynı para biriminde kur `1` kabul edilir. Tutar, para birimi veya ziyaret tarihi değişirse dönüşüm yeniden hesaplanır.
- Trip Summary, planlanan ve gerçek ziyaret/etkinlik harcamalarını trip'in `BaseCurrencyCode` değeri üzerinden karşılaştırır; response ayrıca orijinal para birimi kırılımlarını korur.
- `plannedVisitCost` hesaplanırken her Place/CustomEvent `Price + CurrencyCode`, entry'nin destinasyon yerel tarihindeki kurla `BaseCurrencyCode` değerine normalize edilir; farklı currency değerleri doğrudan toplanmaz.
- B2'nin currency-dependent işleri B7 servisiyle aynı geliştirme diliminde tamamlanır; geçici "yalnızca para birimine göre ayrı toplam" davranışı hedeflenmez.

**Karar — Tarihsel kur destinasyonun yerel gününe göre seçilir:**

- Kur için istenen tarih, `VisitedAt` UTC instant'ının `TripDestination.Timezone` ile çevrildiği yerel takvim günüdür; UTC takvim günü doğrudan kullanılmaz.
- İstenen yerel günde hafta sonu/tatil nedeniyle kur yoksa yalnızca geçmişe doğru en yakın mevcut kur kullanılır; gelecekteki kur kullanılmaz.
- Snapshot hem `RateRequestedDate` hem gerçek `ExchangeRateDate` değerini saklar; hafta sonu örneğinde bu iki tarih farklı olabilir.
- Aynı para biriminde `rate=1` ve iki tarih de ziyaretin yerel günü olur.
- Kullanıcı ziyaret zamanını/destination'ı değiştirirse yerel gün yeniden hesaplanır ve mevcut dönüşüm snapshot'ı yenilenir.
- Frankfurter'ın döndürdüğü gerçek rate tarihi response'tan doğrulanır; istenen tarih varsayılarak kaydedilmez.

**Karar — Kur servisi hatası Visit Log kaydını engellemez:**

- Orijinal `ActualCost` + `CurrencyCode`, Frankfurter erişilemese bile kaydedilir; dış servis hatası kullanıcı verisini reddetmez.
- Visit Log'da `ConversionStatus` (`NotRequired`, `Pending`, `Completed`) tutulur. `ActualCost=null` ise `NotRequired`; aynı para birimindeki dolu kayıt `rate=1` ile doğrudan `Completed` olur.
- Dönüşüm çağrısı başarısızsa alanlar boş ve durum `Pending` kalır; background retry servisi tarihsel kuru daha sonra alıp snapshot'ı tamamlar.
- Trip Summary `pendingConversionCount` ve `isConversionComplete` döndürür. Pending kayıtlar varken çevrilmiş toplam/kalan bütçe "kısmi" olarak işaretlenir; kesin sonuç gibi sunulmaz.
- Tutar, para birimi veya ziyaret tarihi değiştirildiğinde eski snapshot temizlenir, durum yeniden `Pending` olur ve dönüşüm tekrar çalışır.

### Task B2.2: Visit Log CQRS + Controller

**Tahmini Süre:** 6 saat
**Durum:** [x] Tamamlandı

- [x] `CreateVisitLogCommand` + Validator (owner kontrolü)
- [x] `UpdateVisitLogCommand`, `DeleteVisitLogCommand`
- [x] Create/update validator: `Rating` 1–5, `ActualCost >= 0`, ISO currency, `Note <= 1000`; whitespace note normalize edilir
- [x] `GetVisitLogsByTripQuery`
- [x] DTO'lar + mapping
- [x] Koleksiyon endpoint'leri: `POST /api/v1/trips/{tripId}/visit-logs`, `GET /api/v1/trips/{tripId}/visit-logs`
- [x] Tekil kayıt endpoint'leri: `PUT /api/v1/trips/{tripId}/visit-logs/{visitLogId}`, `DELETE /api/v1/trips/{tripId}/visit-logs/{visitLogId}`
- [x] Route'taki `tripId` ile Visit Log'un gerçek `TripId` değeri eşleşmiyorsa kaynak sızıntısını önlemek için `404 Not Found`; kayıt üzerinde işlem yapılmaz
- [x] Create request tam olarak bir hedef içersin: `timelineEntryId` veya `placeId`; ikisinin birden dolu/boş olması validation hatası
- [x] Spontane create request `tripDestinationId` içersin; planlı create'te backend destination'ı timeline entry'den türetsin
- [x] Timeline hedefi yalnızca `Place`/`CustomEvent` olabilir; diğer entry türleri `VISIT_LOG_UNSUPPORTED_ENTRY_TYPE` ile reddedilir
- [x] Timeline entry için zaten Visit Log varsa doğrudan ikinci POST `409 Conflict` döndürür; tekrar visited işaretleme command'i ise idempotent no-op kalır
- [x] Planlı Visit Log delete edildiğinde timeline entry `unvisited` yapılır; spontane log delete yalnızca Visit Log'u siler
- [x] Mevcut `MarkEntryVisitedCommand`, Visit Log oluşturma/silme ve `TimelineEntry` visited alanlarını tek transaction içinde yönetecek şekilde entegre edilsin
- [x] Duplicate işaretleme, eşzamanlı istek ve rollback senaryoları için idempotency/integration testleri

**Karar — Visit Log listesi sayfalı ve filtrelenebilirdir:**

- `GET /trips/{tripId}/visit-logs` mevcut standart `PagedResponse` yapısını kullanır; varsayılan `pageNumber=1`, `pageSize=20`, maksimum `pageSize=100`.
- Varsayılan sıralama `visitedAtDesc`; desteklenen değerler `visitedAtDesc` ve `visitedAtAsc` ile sınırlandırılır.
- Aynı `VisitedAt` değerinde stabil pagination için ikincil sıralama `Id` üzerinden deterministik uygulanır.
- Filtreler: `tripDestinationId`, `source=planned|spontaneous`, UTC instant olarak `visitedFrom` ve `visitedTo`.
- Filtrede verilen destination'ın route'taki trip'e ait olduğu doğrulanır; geçersiz sort/source değeri validation hatasıdır.
- Trip Summary aggregate sorgusu liste pagination'ını kullanmaz; owner'ın tüm aktif Visit Log kayıtları üzerinden hesaplanır.

**Karar — Visit Log zaman penceresi:**

- `Upcoming` trip için Visit Log create reddedilir (`409 Conflict`, `TRIP_NOT_STARTED`).
- `Active` ve `Completed` trip owner'ı kayıt ekleyebilir/güncelleyebilir; gezi bittikten sonra unutulan ziyaretlerin geriye dönük tamamlanması desteklenir.
- `VisitedAt`, trip'in timezone-aware başlangıç/bitiş sınırları içinde olmalı ve gelecekte bir instant olamaz.
- Her kayıt ayrıca bağlı `TripDestination` yerel tarih aralığına uymalı; yanlış şehre/tarihe bağlanan ziyaret reddedilir.
- `Archived` yayın statüsü düzenlemeyi tek başına engellemez; owner erişimi ve hesaplanan `executionState`/tarih aralığı esas alınır.
- `VisitedAt`, tutar veya para birimi değişirse tarihsel kur snapshot'ı geçersiz kılınır ve dönüşüm yeniden hesaplanır.
- Sınır, gelecek zaman ve DST senaryoları için validator/unit + integration testleri yazılır.

**Karar — Visit Log hedef kimlikleri update ile değiştirilmez:**

- `TripId`, `UserId`, `TimelineEntryId` ve `PlaceId` immutable'dır; yanlış hedef seçildiyse kayıt silinip doğru hedefle yeniden oluşturulur.
- Planlı logun `TripDestinationId` değeri entry'den türediği için immutable'dır.
- Spontane logda tarih başka destination aralığına taşınacaksa update request yeni `TripDestinationId` değerini birlikte vermelidir; backend aynı-trip ve tarih uyumunu tekrar doğrular.
- Hedef kimliklerinin update DTO'sunda bulunmaması overposting riskini azaltır; spontane destination değişimi açık ayrı alan/command davranışıyla sınırlanır.
- Hızlı `MarkEntryVisited` aksiyonu `VisitedAt=nowUtc` kullanır ve yalnızca entry destination'ının aktif tarih aralığında çalışır; geçmiş/Completed backfill, tarih seçilen Visit Log create formuyla yapılır.

**Karar — Visit Log verisi private/owner-only:**

- Create/read/update/delete işlemlerinin tamamı yalnızca trip owner tarafından yapılabilir; trip `Published` olsa bile başka kullanıcı Visit Log kayıtlarını göremez.
- Gerçek harcama, kişisel not ve kesin ziyaret zamanı public trip DTO'larına veya public profile/feed response'larına eklenmez.
- `TimelineEntry.IsVisited` ve `VisitedAt` da owner-only veridir; public viewer için trip `Active` veya `Completed` olsa dahi gösterilmez.
- Timeline response alanları public viewer için `null` maplenip null-ignore serialization ile JSON'dan çıkarılır; sahte `isVisited=false` gönderilerek "ziyaret edilmedi" anlamı üretilmez.
- Owner timeline response'u mevcut visited alanlarını korur; mobil public model bu alanların yokluğunu destekler.
- Başka kullanıcının doğrudan endpoint çağrısı `403 Forbidden`; bulunmayan trip/log `404` davranışını korur.

### Task B2.3: Trip Summary Query

**Tahmini Süre:** 5 saat
**Durum:** [x] Tamamlandı

- [x] `GetTripSummaryQuery` — ziyaret edilen entry sayısı, planlanan vs gerçek ziyaret/etkinlik harcaması, toplam gün/destinasyon, en yüksek puanlı duraklar, rota özeti
- [x] `totalVisitCount`, `uniquePlaceCount` ve `visitedCustomEventCount` ayrı hesaplansın; planlı Place kimliği entry üzerinden çözülerek tekrarlı mekân favorileri canonical `PlaceId` bazında gruplanır
- [x] Tekrarlı mekân favori metriği yalnızca dolu rating'lerin ortalamasıyla hesaplanıp `averagePersonalRating`, `ratedVisitCount`, `visitCount` döndürür
- [x] `visitedPlannedEntryCount`, `plannedVisitableEntryCount`, nullable `visitCompletionPercentage` ve `spontaneousVisitCount` hesaplanıp dönülsün
- [x] `plannedVisitCost` yalnızca `Place` + `CustomEvent`; `actualVisitCost` Visit Log `ActualCost` toplamlarıdır ve `PersonCount` çarpanı uygulanmaz
- [x] Planlanan entry fiyatları da entry'nin yerel tarihindeki kurla `Trip.BaseCurrencyCode` değerine çevrilir; mixed-currency değerler ham olarak toplanmaz
- [x] Uçuş/otel/ulaşım plan giderleri ayrı breakdown olarak dönebilir; gerçek gider toplamına katılmış gibi sunulmaz
- [x] `ActualCost=null` toplamdan dışlanır, `ActualCost=0` girilmiş harcama kabul edilir; response harcama coverage sayaçlarını döndürür
- [x] `TripSummaryResponse` DTO
- [x] Endpoint: `GET /api/v1/trips/{tripId}/summary`
- [x] `Upcoming` trip'te summary üretilmez; endpoint `409 Conflict` + stabil `TRIP_NOT_STARTED` hata kodu döndürür
- [x] `Active` trip'te ilerleme özeti: bugüne kadarki ziyaretler, harcama/bütçe durumu ve mevcut favoriler
- [x] `Completed` trip'te kapanış özeti: toplam ziyaret, spontane keşifler, tahmini-gerçek harcama, favoriler ve rota/destinasyon özeti
- [x] `Draft`/`Published`/`Archived` yayın statüsü summary erişim zamanını belirlemez; owner erişiminde hesaplanan `executionState` esas alınır ve archive özeti silmez
- [x] İlgili trip response'larına hesaplanan `executionState` (`Upcoming`, `Active`, `Completed`) eklensin
- [x] Ayrıntılı Trip Summary endpoint'i owner-only olsun; published trip erişimi bu kuralı gevşetmesin
- [x] Paylaşım için public backend endpoint/token/persisted share kaydı oluşturulmaz; owner-only summary, mobilde sanitize edilmiş yerel share modeline dönüştürülür
- [x] Harcama kapsamı veya kur dönüşümü tamamlanmamış summary, paylaşım modeline kesin toplam olarak aktarılmaz (`isCostComplete && isConversionComplete` gate)
- [x] Favori durak sıralaması kişisel Visit Log rating'ine göre yapılır; bu veri public place/trip rating aggregate'i üretmez

**Karar — Rota özeti GPS geçmişi değildir:**

- Summary mevcut planlanan trip/destination rota cache'ini temel alır; yeni bir sürekli GPS tracking/location-history tablosu oluşturulmaz.
- Planlı Visit Log pini TimelineEntry koordinatından, spontane Visit Log pini bağlı `Place` koordinatından üretilir.
- Response planlanan rota/destinasyon sırası ile `plannedVisited` ve `spontaneous` tipli owner-only visit marker'larını ayrı döndürür.
- Koordinatı olmayan Visit Log harita marker listesine girmez fakat ziyaret, harcama ve summary sayaçlarından düşmez; `unmappedVisitCount` ile raporlanır.
- Harita "gerçek gidilen GPS rotası" olarak etiketlenmez; "Planlanan rota ve ziyaretler" olarak sunulur.
- Arka plan GPS geçmişi toplama, saklama ve senkronizasyon B2 kapsamı dışındadır.

### Task B2.4: Timezone Normalization

**Tahmini Süre:** 4 saat
**Durum:** [x] Tamamlandı

- [x] `TripDestination`'a `Timezone` (IANA, nullable) alanı eklensin + migration; trip destination request/response DTO'larına taşınsın
- [x] Nominatim yalnızca koordinat sağlar; `ITimeZoneResolver` koordinatı `GeoTimeZone` ile IANA timezone kimliğine dönüştürür
- [x] Wizard/create/update destination akışında geocoding koordinatı alındıktan sonra timezone otomatik çözülüp saklansın; kullanıcıdan timezone seçmesi istenmesin
- [x] `GeoTimeZone` paket sürümü sabitlensin; resolver unit testleri İstanbul, Paris ve DST kullanan en az bir bölgeyi kapsasın
- [x] Timeline'daki plan saatleri destinasyonun yerel `DateOnly + TimeOnly` değeri olarak yorumlansın; kesin olay/ziyaret/reminder zamanları UTC instant olarak saklansın
- [x] Client'a yerel tarih/saatin yanında destinasyonun IANA timezone kimliği dönülsün; cihaz timezone'u backend hesaplarında kaynak kabul edilmesin
- [x] `executionState` hesabı timezone-aware olsun: başlangıç sınırı ilk destinasyonun, bitiş sınırı son destinasyonun yerel tarihiyle değerlendirilir
- [x] Timeline/live-trip/reminder hesaplarında lokal saat ile UTC karışmasını engelleyen merkezi helper/service kullanılsın
- [x] Timezone çözülemezse cihaz timezone'una sessiz fallback yapılmasın; destination `Timezone=null` kalır, retry/eksik-veri durumu açıkça raporlanır
- [x] Push reminder ve live trip hesapları için temel atılsın

### Task B2.5: Live Trip Nearby Places Query

**Tahmini Süre:** 6 saat
**Durum:** [x] Tamamlandı

> **Mobil karşılığı:** `MOBILE_ROADMAP.md → M8 / Task 8.5`

- [x] Yeni owner-only endpoint: `POST /api/v1/trips/{tripId}/nearby-places/search`; canlı koordinat query string/log/cache yüzeyine yazılmaz
- [x] Request: `tripDestinationId`, `latitude`, `longitude`, `radiusKm` (`1|3|5`), `categoryGroup` (`All|FoodDrink|Sightseeing|Nature|Shopping`)
- [x] Response en fazla 20 `NearbyPlaceResponse`: Place kart alanları + `distanceMeters`, `isPreviouslyVisited`, `previousVisitCount`, kişiselleştirme score/tier
- [x] Yalnızca owner + timezone'u tamamlanmış `Active` trip; destination aynı trip'e ve aktif yerel tarih aralığına ait olmalı
- [x] Koordinat/radius/category validation; arama merkezi aktif destination merkezinden makul gezi alanı dışında ise kontrollü validation hatası
- [x] PostGIS `ST_DWithin` ile strict radius filtresi ve `ST_Distance` ile metre hesabı; aktif Place geography expression'ı için partial GiST index migration
- [x] Application katmanında `INearbyPlaceSearchService`; Infrastructure implementasyonu parametreli PostGIS sorgusuyla radius içindeki sınırlı aday havuzunu getirir, kişiselleştirme bu adaylar üzerinde çalışır
- [x] `Hotel`/`Transport` ve trip timeline'ında bulunan Place ID'leri çıkarılır; silinmiş/inactive Place dönmez
- [x] Önceki spontane Visit Log sayısı aggregate edilir; daha önce ziyaret edilen Place dışlanmaz fakat yeni keşiflerden sonra sıralanır
- [x] Yeni keşifler önce kişisel gezi stili/bütçe skoru, eşitlikte mesafe ve ad ile deterministik sıralanır
- [x] Mevcut `/trips/{tripId}/recommend-places` planlama endpoint'i ve otel-hub davranışı değiştirilmez
- [x] Koordinat request body'nin loglanmış kopyasına veya structured log alanlarına yazılmaz; response `Cache-Control: no-store`
- [x] Endpoint read-only olduğu için idempotency key istemez; arama merkezi ve sonuçlar backend'de kalıcılaştırılmaz
- [x] Unit/integration: auth, owner, Active/Upcoming/Completed, timezone eksik, radius sınırı, kategori mapping, timeline exclusion, repeat-visit badge, deterministik limit

### Definition of Done (B2)

- [x] Kullanıcı bir durağı gerçek harcama + puan + not ile loglayabiliyor
- [x] Visit Log listesi pagination, destination/source/date filtreleri ve stabil iki yönlü tarih sıralamasıyla alınabiliyor
- [x] Trip bitiminde planlanan vs gerçek ziyaret/etkinlik harcaması ve ziyaret metrikleri endpoint'ten alınabiliyor
- [x] Active trip ilerleme özeti döndürüyor; Upcoming trip stabil `TRIP_NOT_STARTED` hatası veriyor
- [x] Completed/Archived trip owner'ı tarih aralığı içindeki unutulmuş ziyaretleri sonradan ekleyip düzeltebiliyor
- [x] Eksik harcama ile sıfır harcama ayrılıyor; eksik veya kur bekleyen kayıt varken toplam kesin sonuç gibi gösterilmiyor
- [x] Zaman değerleri tutarlı (UTC + timezone) dönüyor
- [x] Published trip üzerinden başka kullanıcı Visit Log/ayrıntılı Trip Summary verisine erişemiyor
- [x] Public timeline response'u `IsVisited`/`VisitedAt` sızdırmıyor; owner response'u alanları doğru döndürüyor
- [x] Trip Summary paylaşımı backend'de public URL/token veya yeni erişilebilir veri yüzeyi oluşturmuyor
- [x] Rota özeti planlanan rota + Visit Log pinlerini döndürüyor; GPS geçmişi saklamıyor ve koordinatsız ziyaretleri sayaçlardan kaybetmiyor
- [x] Çok destinasyonlu ve farklı timezone'lu trip'te Live Trip günü/saati cihaz timezone'undan bağımsız doğru hesaplanıyor
- [x] Active trip owner'ı canlı/destination merkezine göre 1/3/5 km içinde, filtrelenmiş ve planla çakışmayan yakın Place önerileri alabiliyor

---

## 🎯 B3 — Push Notifications (FCM) + Preferences

### Scope

Mevcut in-app notification sistemine push katmanı eklenir. Cihaz token'ı kaydedilir, mevcut notification üretildiğinde FCM ile push gönderilir, kullanıcı tercihleri ile filtrelenir.

**Mobil karşılığı:** `MOBILE_ROADMAP.md → M9`

### Task B3.1: Push Token Kaydı

**Tahmini Süre:** 2 saat
**Durum:** [ ] Bekliyor

- [ ] `PushToken.cs` entity — `UserId`, `Token`, `Platform` (enum: Android/iOS/Web), `DeviceId` (nullable), `CreatedAt`, `LastUsedAt`
- [ ] `PushPlatform` enum
- [ ] Configuration + unique index (token) + migration
- [ ] `RegisterPushTokenCommand` / `UnregisterPushTokenCommand`
- [ ] Endpoint: `POST/DELETE /api/v1/push-tokens`

### Task B3.2: FCM Gönderim Servisi

**Tahmini Süre:** 3 saat
**Durum:** [ ] Bekliyor

- [ ] `FirebaseAdmin` NuGet + service account credential (settings/secret)
- [ ] `IPushService` + `FcmPushService` — tek kullanıcıya / token listesine push
- [ ] Geçersiz/expired token temizliği (FCM hata kodlarına göre token silme)
- [ ] DI registration

### Task B3.3: Notification → Push Entegrasyonu

**Tahmini Süre:** 2 saat
**Durum:** [ ] Bekliyor

- [ ] Mevcut `NotificationService` her notification ürettiğinde (follow, upvote, comment, mention, fork...) push tetiklensin
- [ ] Trip/uçuş reminder gibi zaman bazlı push'lar için temel (B2 timezone ile)

### Task B3.4: Notification Preferences

**Tahmini Süre:** 2.5 saat
**Durum:** [ ] Bekliyor

- [ ] `NotificationPreference.cs` entity — `UserId` + her notification tipi için bool toggle (ya da tek JSON kolon)
- [ ] Configuration + migration
- [ ] `GetNotificationPreferencesQuery` / `UpdateNotificationPreferencesCommand`
- [ ] Endpoint: `GET/PUT /api/v1/users/me/notification-preferences`
- [ ] Push gönderiminde tercih kontrolü (kapalıysa gönderme)

### Definition of Done (B3)

- [ ] Cihaz token'ı kaydedilip silinebiliyor
- [ ] Bir sosyal etkileşim gerçek bir push'a dönüşüyor
- [ ] Kullanıcı bildirim tiplerini açıp kapatabiliyor ve bu push'ı etkiliyor

> **Mobil kaynaklı ek:** `NotificationsController`'da bildirim silme endpoint'i (`DELETE /api/v1/notifications/{id}`) yok. Mobil select mode'daki "Sil" butonu şu an işlevsiz. Bu endpoint B3 sonrasına veya ayrı bir cleanup task'ına eklenebilir.

---

## 🎯 B4 — Collections, Global Search, Deep-link, Memories

### Scope

Kişisel düzenleme ve keşif katmanı. Mevcut saved-trips üzerine koleksiyonlar; çok-varlıklı global arama; paylaşılabilir trip linkleri; gezi günlüğü.

**Mobil karşılığı:** `MOBILE_ROADMAP.md → M10`

### Task B4.1: Collections

**Tahmini Süre:** 4 saat
**Durum:** [ ] Bekliyor

> **Öncelik notu (güncellendi):** Bu task artık **iki** M3 ekranı tarafından bekleniyor — Task 3.1 (My Trips → Kaydedilenler sekmesi) **ve** Task 3.2 (Trip Detail → `🔖 Kaydet` bottom sheet). Task 3.1 zaten bu bekleyişi **local mock collections** ile çözmüş durumda (kod tarafında implemente edilmiş, gerçek backend'i bekliyor). Task 3.2'nin Kaydet akışı da **aynı mock mekanizmasını** kullanmalı — B4.1'i M3'e zorunlu/bloklayıcı bağımlılık yapmak yerine, iki ekran da mock veriyle ilerler, B4.1 gerçekten implemente edildiğinde **ikisi birden** gerçek endpoint'lere geçirilir.

**Kararlar (M3 tasarım oturumundan):**
- Bir trip birden fazla koleksiyona eklenebilir (many-to-many)
- Koleksiyon kapağı: kayıttaki ilk trip'in `CoverPhotoUrl`'i otomatik kullanılır; kullanıcı sonradan manuel değiştirebilir
- Kaydetme akışı: `🔖 Kaydet` → bottom sheet → mevcut koleksiyonlar checkbox listesi + "Yeni koleksiyon" → seçim sonrası `POST` çağrısı

- [ ] `SavedCollection.cs` (UserId, Name, CoverPhotoUrl? — nullable, auto-resolved) + `SavedCollectionTrip.cs` join table (SavedCollectionId, TripId) + unique constraint (collection_id, trip_id)
- [ ] Configuration'lar + migration
- [ ] **CoverPhotoUrl otomatik çözümü:** `GetCollectionDetail` handler'ı `CoverPhotoUrl` null ise koleksiyondaki ilk trip'in `CoverPhotoUrl`'ini döner (computed, DB'ye yazılmaz)
- [ ] CQRS: `CreateCollection`, `RenameCollection`, `DeleteCollection`, `AddTripToCollection`, `RemoveTripFromCollection`, `GetMyCollections`, `GetCollectionDetail`
- [ ] Endpoint'ler:
  - `GET /api/v1/collections` — kullanıcının koleksiyonları (ad + trip sayısı + kapak)
  - `POST /api/v1/collections` — yeni koleksiyon oluştur (body: `{ "name": "..." }`)
  - `PUT /api/v1/collections/{id}` — yeniden adlandır
  - `DELETE /api/v1/collections/{id}` — koleksiyonu sil (trip'ler silinmez, sadece ilişki kalkar)
  - `POST /api/v1/collections/{id}/trips/{tripId}` — trip ekle
  - `DELETE /api/v1/collections/{id}/trips/{tripId}` — trip çıkar
  - `POST /api/v1/collections/{id}/cover-photo` — manuel kapak fotoğrafı yükle (BlobService)

### Task B4.2: Global Search

**Tahmini Süre:** 4 saat
**Durum:** [ ] Bekliyor

- [ ] `GlobalSearchQuery` — tek `q` parametresi ile users + trips + posts + places + tags arar (her biri sınırlı sayıda, gruplu sonuç)
- [ ] Block-aware (engellenen kullanıcı içeriği gizli)
- [ ] `GlobalSearchResponse` (gruplu) DTO
- [ ] Endpoint: `GET /api/v1/search?q=&type=` (type opsiyonel filtre)
- [ ] (İleride) PostgreSQL full-text / trigram index ile optimize edilebilir

### Task B4.3: Trip Deep-link / Paylaşım Metadata

**Tahmini Süre:** 2.5 saat
**Durum:** [ ] Bekliyor

- [ ] Published trip için paylaşılabilir public link/slug stratejisi
- [ ] Paylaşım metadata endpoint'i (OG-benzeri: başlık, kapak, kısa açıklama) — WhatsApp vb. önizleme için
- [ ] Deep link doğrudan trip detail'e açılacak şekilde (mobil tarafça handle edilir)

### Task B4.4: Memories / Journal

**Tahmini Süre:** 3 saat
**Durum:** [ ] Bekliyor

- [ ] `TripMemory.cs` entity — TripId, UserId, DayNumber?, Note, PhotoUrls (List), CreatedAt
- [ ] Configuration + migration
- [ ] CQRS: Create/Update/Delete/GetByTrip
- [ ] Endpoint'ler: `GET/POST/PUT/DELETE /api/v1/trips/{tripId}/memories`
- [ ] Medya için mevcut `BlobService` kullanılır

### Task B4.5: App Link Uyumlu Email Verify URL + assetlinks

**Tahmini Süre:** 2 saat
**Durum:** [ ] Bekliyor

> **Bağlam:** Şu an `AccountService` email doğrulama URL'ini `_mailSettings.FrontendVerifyUrl` ile **web frontend'ine** üretiyor; maildeki link tarayıcıda açılıyor. Bu task, mobil tarafın (`MOBILE_ROADMAP.md → Task 10.6`) doğrulama linkini **doğrudan app içinde** açabilmesi için backend desteğini ekler.
>
> **Mobil karşılığı:** `MOBILE_ROADMAP.md → M10 / Task 10.6`

- [ ] Android App Links için `assetlinks.json` (Digital Asset Links) doğrulama domaininde host'lanır (frontend host'u veya backend `/.well-known/assetlinks.json`)
- [ ] Doğrulama URL stratejisi App Link ile uyumlu hale getirilir: aynı link hem web hem app tarafından handle edilebilsin (universal link mantığı) — `FrontendVerifyUrl` buna göre ayarlanır/yeni bir setting eklenir
- [ ] `verify-email` akışının token formatı/encoding'i değişmeden korunur (mevcut `email` + `token` query parametreleri)
- [ ] Fallback: app kurulu değil / domain doğrulanmadıysa link web doğrulama sayfasına düşmeye devam eder
- [ ] Mobil ekiple koordinasyon: package name + SHA-256 imza parmak izi `assetlinks.json`'a eklenir

### Task B4.6: Trending Destinations Endpoint

**Tahmini Süre:** 2 saat
**Durum:** [ ] Bekliyor

> **Bağlam:** Home ekranındaki "İlham Al" destination kartları ve Destination Detail sayfası için backend'den şehir bazlı trending veri gerekiyor. Mevcut entity'lerde yeni tablo gerekmez; `TripDestination` üzerinden aggregation yapılır.
>
> **Mobil karşılığı:** `MOBILE_ROADMAP.md → M2 / Task 2.1` (Home "İlham Al" bölümü)

- [ ] `GetTrendingDestinationsQuery` — `TripDestination` tablosunu `city` bazında grupla, published trip sayısına göre sırala, limit (default: 10) uygula
- [ ] `TrendingDestinationResponse` DTO: `city`, `country`, `tripCount`, `coverPhotoUrl` (nullable — en çok upvote alan o şehirdeki trip'in kapak fotoğrafı)
- [ ] Endpoint: `GET /api/v1/destinations/trending?limit=10` (anonim erişime açık)
- [ ] Cache dostu: sonuç sık değişmez, ileride Redis ile önbelleğe alınabilir

### Definition of Done (B4)

- [ ] Kullanıcı koleksiyon oluşturup trip ekleyebiliyor
- [ ] Tek arama kutusundan çok tipte sonuç dönüyor
- [ ] Trip linki paylaşıldığında önizleme verisi var
- [ ] Gezi günlüğü (not + foto) eklenebiliyor
- [ ] Email doğrulama linki App Link uyumlu; app kuruluysa doğrudan app'te, değilse web'de açılıyor
- [ ] Trending destination listesi endpoint'ten alınabiliyor

---

## 🎯 B5 — Moderasyon (Report, Soft Moderation, Audit Log)

### Scope

Mevcut hard delete/suspend admin paneline kullanıcı sinyali (report), yumuşak moderasyon aksiyonları ve denetlenebilirlik (audit log) eklenir.

**Mobil karşılığı:** `MOBILE_ROADMAP.md → M11`

### Task B5.1: Report Sistemi

**Tahmini Süre:** 4 saat
**Durum:** [ ] Bekliyor

- [ ] `Report.cs` entity — ReporterId, TargetType (enum: Post/Comment/Tip/Trip/User), TargetId, Reason (enum: Spam/Harassment/FakeInfo/Inappropriate/Other), Details?, Status (enum: Pending/Reviewing/Resolved/Dismissed), CreatedAt, ResolvedById?, ResolvedAt?
- [ ] Configuration + index + migration
- [ ] `CreateReportCommand` (kullanıcı) + validator (kendini/aynı şeyi tekrar raporlama kontrolü)
- [ ] Admin: `GetReportsQuery` (filtre/status), `ResolveReportCommand`
- [ ] Endpoint'ler: `POST /api/v1/reports`, `GET /api/v1/admin/reports`, `POST /api/v1/admin/reports/{id}/resolve`

### Task B5.2: Soft Moderation

**Tahmini Süre:** 3 saat
**Durum:** [ ] Bekliyor

- [ ] İçerik görünürlük durumları: hide (public akıştan düşür ama silme), review-pending
- [ ] İlgili entity'lerde `ModerationStatus` veya mevcut `IsVisible` genişletme
- [ ] Kullanıcı geçici kısıtlama (post atamama / explore'dan düşme) — `User` üzerinde restriction alanları
- [ ] Admin command'ları + feed/explore query'lerinin bu durumları dikkate alması

### Task B5.3: Admin Audit Log

**Tahmini Süre:** 2.5 saat
**Durum:** [ ] Bekliyor

- [ ] `AdminAuditLog.cs` entity — AdminId, Action (enum), TargetType, TargetId, Detail (JSON?), CreatedAt
- [ ] Configuration + migration
- [ ] Admin aksiyonlarında (suspend/unsuspend, delete post, resolve report, hide) otomatik log yazımı (cross-cutting — behaviour veya service)
- [ ] `GetAuditLogQuery` + Endpoint: `GET /api/v1/admin/audit-log`

### Definition of Done (B5)

- [ ] Kullanıcı içerik/kullanıcı raporlayabiliyor, admin görüp çözebiliyor
- [ ] Hard delete dışında yumuşak aksiyonlar mevcut
- [ ] Tüm admin aksiyonları iz bırakıyor

---

## 🎯 B6 — AI (Timeline Optimize + AI Chat)

### Scope

`OMNIFLOW_YENI_OZELLIKLER.md` kararı: **AI tam rota üretmez; önerir ve optimize eder.** İki parça: (1) deterministik timeline optimizasyonu, (2) tool-grounded AI chat. Hallüsinasyon engeli için modele gerçek arama/tool bağlanır.

**Mobil karşılığı:** `MOBILE_ROADMAP.md → M12`

### Task B6.1: Timeline Optimization Servisi

**Tahmini Süre:** 5 saat
**Durum:** [ ] Bekliyor

- [ ] Google Maps Distance Matrix API entegrasyonu (`GoogleMapsSettings` mevcut)
- [ ] `ITimelineOptimizationService` — gün içi entry'leri mesafe/süreye göre yeniden sıralar
- [ ] `IsLocked = true` entry'ler sabit kalır; sadece esnek olanlar kilitli noktalar arasındaki segmentlerde yeniden sıralanır
- [ ] Günlük entry sayısı küçük → brute-force / basit sezgisel yeterli
- [ ] Mevcut `TimelineService`'in doğal uzantısı olarak konumlandırılır
- [ ] Endpoint: `POST /api/v1/trips/{tripId}/timeline/optimize` → **öneri döner, otomatik uygulamaz** (onaylanırsa mevcut reorder endpoint'i kullanılır)

### Task B6.2: AI Chat Altyapısı (Tool-Grounded)

**Tahmini Süre:** 6 saat
**Durum:** [ ] Bekliyor

- [ ] OpenAI function calling kurulumu (`OpenAISettings` mevcut)
- [ ] Tool tanımları: kendi DB/servislere bağlı (place search, provider flight/hotel search, recommendation)
- [ ] Akış: önce soru sorarak sınırla (bütçe, tarih, tercih) → sonra tool ile gerçek arama
- [ ] Modelin parametrik bilgisinden "uydurması" engellenir (sadece tool sonuçları)
- [ ] `POST /api/v1/ai/chat` endpoint'i (konuşma context'i ile)
- [ ] Boş `AiTimelineService`/`AiFallbackService` scaffold'ları bu tasarıma göre doldurulur veya kaldırılır

### Task B6.3: Test & Maliyet Koruması

**Tahmini Süre:** 2 saat
**Durum:** [ ] Bekliyor

- [ ] Token/maliyet sınırları, rate limit
- [ ] Tool çağrılarının deterministik kısımları unit test edilir (optimizasyon mantığı)

### Definition of Done (B6)

- [ ] Bir trip için timeline optimizasyon önerisi alınıp onaylanınca uygulanabiliyor
- [ ] AI chat gerçek veriye dayalı (tool-grounded) cevap veriyor, uydurmuyor
- [ ] Kilitli entry'ler optimizasyonda korunuyor

---

## 🎯 B7 — Currency Servisi

### Scope

Lokal para birimi ana, kullanıcının para birimi ikincil gösterilecek. Backend güncel ve tarihsel kur sağlar. B2 ile aynı geliştirme diliminde uygulanarak Visit Log ve Trip Summary dönüşümlerini ilk sürümden itibaren destekler.

**Mobil karşılığı:** `MOBILE_ROADMAP.md → M13`

### Task B7.1: Currency Domain + Kullanıcı/Trip Tercihi

**Tahmini Süre:** 3 saat
**Durum:** [x] Tamamlandı

- [x] `Trip.BaseCurrencyCode` (required, uppercase ISO 4217, ilk migration backfill `USD`) + create wizard/request/response ve trip DTO'ları
- [x] Wizard için desteklenen ilk para birimleri: `TRY`, `USD`, `EUR`; yeni trip varsayılanı kullanıcı tercihinden gelir
- [x] `User.PreferredCurrencyCode` (nullable, uppercase ISO 4217) + migration; global olarak `TRY` varsayılmasın
- [x] `PUT /api/v1/users/me/currency-preference` endpoint'i + validator; değer desteklenen aktif currency kodlarından biri olmalı
- [x] İlgili `me`/profile response'unda `PreferredCurrencyCode` dönsün
- [x] Kullanıcının tercihi yoksa mobil locale'den başlangıç önerisi üretir; kaydedilince cihazlar arasında korunur
- [x] Trip üzerinde yapılan currency değişikliği yalnızca o trip'in `BaseCurrencyCode` değerini etkiler, user tercihini değiştirmez
- [x] Trip `Active/Completed` ise veya aktif Visit Log içeriyorsa baz para birimi değişikliği `TRIP_BASE_CURRENCY_LOCKED` ile reddedilir

### Task B7.2: Frankfurter v2 Client + Kalıcı Kur Cache'i

**Tahmini Süre:** 4 saat
**Durum:** [x] Tamamlandı

- [x] Typed `HttpClient` tabanlı `IExchangeRateService`; Frankfurter v2 güncel, tarih bazlı ve tek pair sorguları
- [x] Dönüşüm backend'de `amount × rate` ile ve `decimal` aritmetiğiyle yapılır; finansal hesapta `double` kullanılmaz
- [x] `ExchangeRateSnapshot` DB cache entity'si: `BaseCurrency`, `QuoteCurrency`, `RateDate`, `Rate numeric(18,8)`, `Provider`, `FetchedAtUtc`
- [x] `(base_currency, quote_currency, rate_date, provider)` unique index; cache miss'te Frankfurter çağrısı ve idempotent upsert
- [x] İstenen tarih yoksa yalnızca önceki mevcut rate seçilir; API'nin döndürdüğü gerçek tarih saklanır
- [x] `GET /api/v1/currency/rates?base=` güncel ve `?base=&date=` tarihsel kur endpoint'i
- [x] Timeout, cancellation, kontrollü upstream hata mapping'i; API anahtarı gerektirmeyen base URL configuration'dan gelir

### Task B7.3: Visit Log Conversion + Retry

**Tahmini Süre:** 3 saat
**Durum:** [x] Tamamlandı

- [x] Visit Log conversion snapshot alanları: `ConvertedActualCost numeric(18,2)`, `ExchangeRate numeric(18,8)`, `RateRequestedDate`, `ExchangeRateDate`, `BaseCurrencyCode`, `ConversionStatus`
- [x] Visit Log önce orijinal tutarla güvenli biçimde kaydedilir; conversion başarısızlığı create/update işlemini rollback etmez
- [x] Başarılı dönüşüm snapshot'ı immutable geçmiş değer olarak saklanır; tutar/currency/visitedAt/destination değişirse yeniden hesaplanır
- [x] `Pending` kayıtlar için idempotent, bounded-batch background retry; servis restart'ında DB'den devam eder
- [x] Retry gözlemlenebilirliği için attempt count + last-attempt timestamp/log; ham exception veya kullanıcı verisi response'a sızdırılmaz
- [x] Trip Summary planlanan/gerçek ziyaret harcaması toplamlarını `Trip.BaseCurrencyCode` üzerinden hesaplar; orijinal currency kırılımlarını da döndürür

### Task B7.4: Günlük Güncel Kur Yenileme

**Tahmini Süre:** 1.5 saat
**Durum:** [x] Tamamlandı

- [x] `BackgroundService` ile desteklenen aktif currency pair'lerinin güncel kuru günde bir kez yenilenir
- [x] Hata durumunda son başarılı DB snapshot korunur; process restart in-memory cache kaybı veri kaybı oluşturmaz
- [x] Aynı gün tekrarlı çalışma unique index/upsert sayesinde duplicate snapshot üretmez

### Definition of Done (B7)

- [x] Güncel kurlar endpoint'ten alınabiliyor
- [x] Günlük otomatik güncelleme çalışıyor, dış servis düşse bile son veri duruyor
- [x] Wizard'da seçilen `TRY`/`USD`/`EUR` trip baz para birimi olarak saklanıyor
- [x] Kullanıcının tercih ettiği para birimi cihazlar arasında backend üzerinden korunuyor ve wizard varsayılanına uygulanıyor
- [x] Visit Log orijinal tutarı kaybetmeden ziyaret tarihindeki kurla çevriliyor ve geçmiş özet kur değişiminden etkilenmiyor
- [x] Kur servisi geçici olarak kapalıyken Visit Log kaydı başarılı oluyor; dönüşüm servis geri geldiğinde otomatik tamamlanıyor

---

## 🎯 B8 — Offline Sync & Trip Collaboration (En Son)

### Scope

İki ayrı ileri özellik. Collaboration ayrı bir proje büyüklüğünde; en sona bırakılır.

**Mobil karşılığı:** `MOBILE_ROADMAP.md → M14`

### Task B8.1: Offline Sync Desteği

**Tahmini Süre:** 4 saat
**Durum:** [ ] Bekliyor

- [ ] Trip verisi için `updatedSince` delta endpoint'i (mobil cache senkronizasyonu)
- [ ] Değişiklik damgaları (UpdatedAt) ile artımlı çekme
- [ ] (Opsiyonel) basit conflict resolution (last-write-wins)

### Task B8.2: Trip Collaboration

**Tahmini Süre:** 8 saat
**Durum:** [ ] Bekliyor

- [ ] `TripCollaborator.cs` (TripId, UserId, Role: Viewer/Editor, Status: Invited/Accepted) + migration
- [ ] Davet gönder/iptal/kabul akışı + CQRS
- [ ] Tüm trip handler'larında owner-only kontrolü → owner-or-editor olacak şekilde genişletme (geniş etki)
- [ ] Endpoint'ler: `GET/POST/DELETE /api/v1/trips/{tripId}/collaborators`

### Definition of Done (B8)

- [ ] Mobil, trip'i artımlı senkronize edebiliyor
- [ ] Bir trip birden fazla kullanıcı tarafından düzenlenebiliyor (rol bazlı)

---

## 🎯 B9 — Trip Planning Sayfası Backend Follow-up'ları

### Scope

Mobil Trip Planning sayfası tasarımı sırasında (`omniflow-mobile/TRIP_PLANNING_PAGE.md`) ortaya çıkan, mevcut backend'de eksik olan üç küçük ama gerekli düzeltme. Hepsi Trip Planning'in M3 akışlarının tam çalışması için gerekli.

**Mobil karşılığı:** `MOBILE_ROADMAP.md → Task 3.11-3.16` (Trip Planning)

### Task B9.1: CustomEvent Koordinat Mapping'i

**Tahmini Süre:** 1 saat
**Durum:** [x] Tamamlandı

**Sorun:** `CreateTimelineEntryCommandHandler.CreateEntryFromRequestAsync` içinde `CustomEvent` factory çağrısı (`TimelineEntry.CreateCustomEventEntry`) request'ten gelen `CustomLatitude` / `CustomLongitude` değerlerini **entity'ye set etmiyor** (CustomAccommodation set ediyor, CustomEvent atlıyor). Bu yüzden haritadan dokunarak eklenen "Mekan/Diğer" öğeleri harita pini olarak saklanamıyor.

- [x] `TimelineEntry.CreateCustomEventEntry` factory'sine `customLatitude` / `customLongitude` parametreleri eklenmesi (veya entity üzerinde set edilmesi)
- [x] `CreateTimelineEntryCommandHandler`'daki `CustomEvent` case'inde `request.CustomLatitude` / `request.CustomLongitude`'un aktarılması
- [x] Aynı mapping'in `UpdateTimelineEntry` handler'ında da doğrulanması (varsa aynı eksik giderilir)
- [x] Unit test: koordinatlı `CustomEvent` create → entity'de `CustomLatitude/Longitude` set olmuş oluyor

### Task B9.2: Publish %80 CompletionPercentage Enforce

**Tahmini Süre:** 1 saat
**Durum:** [x] Tamamlandı

**Sorun:** `TripCompletionCalculator` / `CompletionPercentage` mevcut (bkz. B0.6) ve mobil "Yayınla" butonunu bu değere göre pasifleştirecek, ancak backend publish handler'ında bu kural **doğrulanmıyor** — API'ye doğrudan istek atılırsa %80 altında bir trip yayınlanabiliyor.

- [x] Trip publish handler'ında `CompletionPercentage >= 80` kontrolü
- [x] Yetersizse net mesajla `400 Bad Request` (örn. "Trip is only %X complete; publishing requires at least 80%")
- [x] Unit test: %80 altı trip publish → 400; %80+ → başarılı

### Task B9.3: Haritadan "Mekan" CustomEvent Kilitsiz Oluşturma

**Tahmini Süre:** 1 saat
**Durum:** [x] Tamamlandı

**Sorun:** Genel kural "Place hariç tüm Custom* tipler otomatik `IsLocked = true`". Ancak haritadan "Mekan" olarak eklenen öğe teknik olarak `CustomEvent` tipinde kaydediliyor ve bu bir "fikir" (rezervasyon değil) — bu yüzden **kilitsiz** olmalı. "Diğer" olarak eklenen `CustomEvent` ise normal kural (kilitli) kalmalı.

- [x] Oluşturma bağlamına göre `IsLocked` set edilebilmesi: haritadan "Mekan" niyetli `CustomEvent` → `IsLocked = false`, "Diğer" → `IsLocked = true`
- [x] Ayrım için request'e bir işaret (örn. `IsLocked` opsiyonel alanı ya da bir "custom place" bayrağı) — mobil hangi butondan geldiğini bildiği için oluşturma anında iletir
- [x] Unit test: mekan-niyetli CustomEvent → kilitsiz; generic event → kilitli

### Definition of Done (B9)

- [x] Haritadan eklenen custom mekanlar koordinatlarıyla saklanıyor ve pin olarak dönüyor
- [x] %80 altındaki trip API üzerinden yayınlanamıyor
- [x] Haritadan "Mekan" olarak eklenen öğe kilitsiz, "Diğer" kilitli oluşuyor

---

## 🎯 B10 — Community Feed Backend Follow-up'ları

**Mobil karşılığı:** `MOBILE_ROADMAP.md → M5 — Social & Community`  
**Durum:** [x] Tamamlandı (2026-07-13)  
**Migration:** Gerekmiyor

### Task B10.1: Feed Arama, Filtreleme ve Sıralama

- [x] `GET /api/v1/feed`: geriye uyumlu ForYou/Following/Latest + `q`, `tag`, `postType`, `sort`
- [x] Case-insensitive içerik/etiket araması ve normalize etikette tam eşleşme
- [x] Latest/MostUpvoted/MostCommented için deterministik cursor; tie-break `CreatedAt` + `Id`
- [x] Following ve block görünürlük kuralları korunuyor

### Task B10.2: Post ve Rota Önizleme Sözleşmesi

- [x] `PostResponse` içine additive nullable `TripPreview` (id, title, coverPhotoUrl, primaryLocation)
- [x] Birincil konum ilk destinasyon; yoksa origin bilgisi
- [x] Route create/update yalnızca oturum sahibinin Published tripini bağlayabilir
- [x] İçerik/fotoğraf minimumu, max 5 fotoğraf ve Route için TripId guard'ları
- [x] Place alanları web/API geriye uyumluluğu için korundu

### Task B10.3: Yorum Derinliği

- [x] Cross-post parent kontrolü korundu
- [x] Reply altına reply açıklayıcı 422 validation cevabıyla reddediliyor

### Task B10.4: Doğrulama

- [x] Feed unit testleri: keyword/tag/type/sort/cursor/Following/block
- [x] Post testleri: medya limiti ve bağlanamayan trip reddi
- [x] Comment testleri: cross-post ve ikinci seviye reply reddi
- [x] Birleşik feed filtre/sort integration testi ve eski endpoint testleri
- [x] `dotnet test Tests/OmniFlow.UnitTests/OmniFlow.UnitTests.csproj --no-restore` — **481/481 başarılı**
- [x] Feed/Posts/Comments/CommunityTips integration grubu — **46/46 başarılı**

### Definition of Done (B10)

- [x] Mobil Search filtreleri server-side çalışıyor ve eski parametresiz Feed sözleşmesi bozulmuyor
- [x] Route postlar mobil için yeterli trip özeti döndürüyor
- [x] Yalnızca tek seviye yorum reply'ı backend tarafından enforce ediliyor
- [x] Yeni veritabanı migration'ı oluşmadı

---

## 📌 Genel Notlar

- Her faz bağımsız deploy edilebilir; mobil ekip ilgili fazı bekler.
- Yeni her endpoint Swagger'da `ProducesResponseType` ile dokümante edilir.
- Yeni her entity için soft-delete gerekiyorsa `AuditableBaseEntity`, gerekmiyorsa `BaseEntity` kullanılır.
- Block-aware görünürlük (`BlockVisibilityHelper`) yeni listeleme/arama endpoint'lerinde de uygulanur.
- Mobil bağımlılık etiketi formatı: mobil roadmap'te **⛔ Bağımlılık: B{faz}.{task}** olarak geçer.

---

## 💡 Gelecek Fikirler (Kapsam Dışı — Şimdilik)

### Fikir: Trip Görünürlük Kontrolü (Visibility Toggle)

**Fikir:** Kullanıcının kendi published trip'ini `🌍 Herkese açık` / `👥 Takipçilere özel` / `🔒 Sadece ben` olarak işaretleyebilmesi (Instagram private hesap mantığına benzer 3 katmanlı görünürlük).

**Motivasyon:** Şu an archive = "yayından çek, gizle" işlevi görüyor. Ancak bu ayrımı kullanıcıya daha sezgisel anlatmak için trip'e bir `Visibility` enum'u (`Public / FollowersOnly / Private`) eklenebilir. Private/FollowersOnly bir published trip Explore'dan düşer; FollowersOnly sadece owner'ı takip edenlere görünür, Private sadece owner'a. Owner her durumda Trip Detail'den görebilir ve düzenleyebilir.

**Gerektirecekleri:**
- `Trip` entity'sine `Visibility` enum alanı (migration gerekir) — `Public / FollowersOnly / Private`
- `GET /explore`, `GET /api/v1/trips`, ve **`GetTripByIdQuery` (bkz. B0.10 güvenlik düzeltmesi)** query'lerinde visibility filtresi — `FollowersOnly` için `Follow` tablosuna karşı ek bir kontrol gerekir (istekte bulunan kullanıcı owner'ı takip ediyor mu)
- `PATCH /api/v1/trips/{id}/visibility` endpoint'i
- Mobil: Trip Detail `⋮` menüsüne 3 seçenekli visibility toggle eklenmesi

**Öncelik:** ⚪ Ertelendi — B8 sonrası değerlendirilebilir. Mezuniyet projesi takvimi göz önüne alınarak şu an tasarım detayına girilmiyor, sadece fikir olarak not düşüldü.

### Fikir: Gerçek Şehir Arama / Geocoding Servisi — ✅ Tamamlandı

> **Not:** Bu fikir uygulandı. `IGeocodingService.SearchCitiesAsync()` (Nominatim `/search` proxy'si, mevcut rate-limit gate'i paylaşıyor) + `GET /api/v1/geo/cities?query=&limit=` (`GeoController`) eklendi. Mobilde `WizardCityCatalog.kt` ve `MockCityCoordinates.kt` (ikisi de hardcoded) tamamen silindi; `CitySearchViewModel` artık bu uca debounce'lu istek atıyor, hem trip wizard hem "destinasyon ekle" akışı bunu kullanıyor. Aşağıdaki orijinal not tarihsel referans için duruyor.


**Fikir:** Mobil tarafta trip wizard'daki ve "destinasyon ekle" akışındaki şehir seçimi şu an `WizardCityCatalog.kt` içinde **hardcoded ~37 şehirlik bir liste** (İngilizce isim + ülke + birkaç Türkçe arama takma adı) ile yapılıyor. Aynı şekilde destinasyon pinlerinin koordinatları da `MockCityCoordinates.kt` içinde hardcoded bir şehir→lat/lng tablosundan geliyor. Backend'de gerçek bir şehir arama/geocoding endpoint'i hiç yok — bu yüzden hem şehir seçenekleri 37 ile sınırlı hem de yazım/dil uyuşmazlıkları (örn. "Roma" yazıp "Rome" bulamama, ya da seçilen şehir adının otel/uçuş/mekan sağlayıcı verisindeki şehir adıyla birebir eşleşmemesi) sürekli hataya yol açıyor.

**Motivasyon:** Otel/uçuş/mekan arama uçları (`/providers/hotels`, `/providers/flights`, `/places/city/{city}`) şehir adını **birebir string eşleştirmesiyle** karşılaştırıyor (`ToLower()` dışında normalize yok). Sabit liste + serbest metin girişi olmayan bir yapı yerine gerçek bir geocoding/autocomplete servisi kullanılırsa hem şehir kapsamı sınırsız hale gelir hem de kanonik isim/koordinat backend'den tutarlı gelir.

**Seçenekler:**
- **Google Places Autocomplete** — en iyi UX (çoklu dil, anlık öneri, dünya çapında kapsam), ama ücretli (istek başına ücret) + API key/faturalandırma kurulumu gerekir.
- **OpenStreetMap Nominatim** — ücretsiz, proje zaten MapLibre + OpenFreeMap (OSM tabanlı) kullandığı için temaya uygun; ama saniyede 1 istek sınırı var, ağır kullanım için kendi sunucunda barındırma (self-host) önerilir, doğrudan halka açık servisi production'da kullanmak riskli.
- **Mapbox Geocoding** — ücretsiz kotası var, iyi kalite, API key + hesap gerekir.

**Önerilen yaklaşım:** Nominatim'i **backend üzerinden proxy'leyerek** çağırmak (mobilden doğrudan değil) — ücretsiz, mevcut OSM ekosistemiyle tutarlı. Bunun için backend'e yeni bir "city search" endpoint'i eklenmesi gerekir (örn. `GET /api/v1/geo/cities?query=...` → Nominatim'e proxy, sonucu normalize edip döner); mobil tarafta `WizardCityCatalog`/`MockCityCoordinates` bu endpoint'i çağıran bir implementasyonla değiştirilir, UI/arama fonksiyon imzası aynı kalabilir.

**Gerektirecekleri:**
- Backend: Nominatim (veya seçilecek servis) için bir `IGeocodingService` soyutlaması + HTTP client entegrasyonu
- Yeni endpoint: şehir adı + ülke + lat/lng dönen bir arama ucu (rate-limit/caching düşünülmeli — Nominatim kullanım politikası gereği)
- Mobil: `WizardCityCatalog.search()` ve `MockCityCoordinates.lookup()` yerine bu endpoint'i çağıran repository metodu
- Var olan trip'lerdeki destinasyonların şehir adlarının yeni kanonik isimlerle uyumlu olup olmadığının değerlendirilmesi (mevcut veri migration'ı gerekebilir)

**Öncelik:** ⚪ Ertelendi — kullanıcı talebiyle not düşüldü, şu an değerlendirme aşamasında.
