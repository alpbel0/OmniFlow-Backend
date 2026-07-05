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
**Durum:** [ ] Bekliyor

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

- [ ] `TripChecklistConfirmation.cs` entity (hafif, `BaseEntity`) — `TripId`, `ItemKey` (string, yukarıdaki format), `IsConfirmed` (bool), `ConfirmedAt` (DateTime?)
- [ ] EF Core configuration + migration — unique index (`trip_id`, `item_key`)
- [ ] `ToggleChecklistItemCommand` — owner kontrolü, idempotent (aynı state'e tekrar set etmek hata vermez)
- [ ] `DeleteTripDestinationCommand` handler'ına o destinasyona ait `flight-leg:` / `hotel-night:` confirmation'larının temizlenmesi eklenir
- [ ] `GetTripChecklistStatusQuery` — güncel geçerli `ItemKey` setini destinasyon verisinden hesaplar, sadece bunlarla eşleşen confirmation'ları döner (stale veri read-time'da filtrelenir)
- [ ] Endpoint'ler ve tam contract (`TripsController` üzerinde, `GetById`/`GetBudgetSummary` ile aynı controller — casing tutarlılığı için `/api/v1/Trips/...`):
  - `GET /api/v1/Trips/{id}/checklist` → response:
    ```json
    { "items": [
        { "itemKey": "flight-leg:{guid}:{guid}", "isConfirmed": true, "confirmedAt": "2026-07-01T10:00:00Z" },
        { "itemKey": "hotel-night:{guid}:1", "isConfirmed": false, "confirmedAt": null }
    ] }
    ```
    Sadece **güncel geçerli** `itemKey`'ler döner (stale olanlar read-time'da filtrelenmiş halde, yukarıdaki Reconciliation Kuralı'na göre). Ring hesaplaması (`seçilen/beklenen`) **client-side** yapılır — backend sadece ham confirmation state'i döner, beklenen sayı zaten mobilde mevcut destinasyon verisinden hesaplanabiliyor, backend'de tekrar hesaplamaya gerek yok
  - `PUT /api/v1/Trips/{id}/checklist/{itemKey}` — body: `{ "isConfirmed": true }`, response: **`204 No Content`** (netleşti — mobil zaten optimistic update kullandığı için response body'ye bağımlı değil, en basit kontrat bu)
- [ ] **Visibility kuralı (B0.10 ile aynı desen — ayrıştırılmalı, kopyalanmamalı):**
  - **`GetTripChecklistStatusQuery` (GET):** `[AllowAnonymous]` + handler'da `ITripVisibilityService` (B0.10'da tanımlanan paylaşılan helper) kullanılır — Published → **anonim dahil herkes okuyabilir** (misafir Trip Detail'de checklist'i salt-okunur görüyor, bkz. `TRIP_DETAILS_PAGE.md`), Draft/Archived → sadece owner, yetkisiz/anonim → `EntityNotFoundException` (404)
  - **`ToggleChecklistItemCommand` (PUT):** sınıf seviyesindeki `[Authorize]`'dan (override yok) — giriş gerektirir, **ayrıca owner kontrolü** (sadece trip owner'ı checklist işaretleyebilir; misafir/anonim PUT çağırırsa 401/403, GET'te salt-okunur görebilmesiyle çelişmez çünkü ikisi ayrı action, ayrı yetki seviyesi)
- [ ] **URL encoding:** `itemKey` içindeki `:` karakterleri path segment'inde teknik olarak geçerlidir (RFC 3986 `pchar` seti `:` içerir), ama HTTP client kütüphaneleri (Retrofit/OkHttp) arasında tutarlılık için **mobil taraf `itemKey`'i açıkça percent-encode eder** (`:` → `%3A`) URL'i oluştururken. ASP.NET Core route binding, path parametrelerini **otomatik url-decode eder** — backend tarafında ekstra bir işlem gerekmez, sadece bu beklenti iki taraf arasında netleşmiş olsun
- [ ] Mobil tarafta **optimistic update + hata durumunda geri alma** — mevcut app-wide konvansiyonla tutarlı (bkz. `MOBILE_ROADMAP.md` "UI State Konvansiyonu" → Aksiyon durumları), yeni bir pattern gerekmez

---

### Task B0.10: 🔴 GÜVENLİK — GetTripByIdQuery Owner/Status Kontrolü Eksik

**Tahmini Süre:** 1 saat
**Durum:** [ ] Bekliyor
**Öncelik:** 🔴 Kritik — güvenlik açığı, diğer her şeyden önce yapılmalı

> **Bulgu:** `GetTripByIdQueryHandler.cs` trip'i bulup **hiçbir owner/status kontrolü yapmadan** direkt döndürüyor. Şu an authenticated herhangi bir kullanıcı, ID'sini bilirse **başkasının Draft/Archived trip'ini** tam olarak görebiliyor. Diğer command'larda (`ArchiveTripCommandHandler` vb.) owner kontrolü var, ama bu **read** path'inde (Trip Detail'in kullandığı asıl endpoint) yok.
>
> **⚠️ Ek bulgu (kod incelemesinde ortaya çıktı):** `TripsController`, `BaseApiController`'dan miras alıyor ve `BaseApiController` sınıf seviyesinde `[Authorize]` taşıyor ([BaseApiController.cs:8](omniflow-backend/OmniFlow/OmniFlow.WebApi/Controllers/BaseApiController.cs:8)). `GetById` ([TripsController.cs:61-70](omniflow-backend/OmniFlow/OmniFlow.WebApi/Controllers/v1/TripsController.cs:61)) ve `GetBudgetSummary` ([TripsController.cs:142-152](omniflow-backend/OmniFlow/OmniFlow.WebApi/Controllers/v1/TripsController.cs:142)) üzerinde **`[AllowAnonymous]` yok** — yani anonim (giriş yapmamış) bir istek şu an **handler'a hiç ulaşmadan, framework seviyesinde 401 ile reddediliyor**. Handler içine "anonim ise Published kontrolü yap" mantığı eklemek **tek başına yeterli değil**, controller seviyesinde de `[AllowAnonymous]` eklenmesi şart.
>
> **Karar (Explore ile tutarlılık):** `ExploreController` zaten `[AllowAnonymous]` + "Authentication is optional" yorumuyla anonim taramaya izin veriyor. Trip Detail/Budget Summary'nin de **gerçekten anonim** erişime açık olması gerekir — "misafir" burada sadece "başka bir login olmuş kullanıcı" değil, **giriş yapmamış kullanıcıyı da kapsar**.

- [ ] `GetTripByIdQueryHandler`'a kontrol eklenir: `trip.Status != TripStatus.Published && trip.OwnerId != currentUserId` ise **`EntityNotFoundException`** fırlatılır (403/`ForbiddenException` değil — "yetkin yok" yerine "bulunamadı" denir, private trip'in var olduğu bile sızdırılmaz)
- [ ] Anonim (giriş yapmamış) kullanıcı için de aynı kural geçerli — `currentUserId` yoksa `trip.Status != Published` durumunda 404 (mevcut `GetTripByIdQueryHandler`'daki `Guid.TryParse(_authenticatedUserService.UserId, ...)` pattern'i zaten anonim'i güvenli ele alıyor, `IsUpvoted`/`IsSaved` için kullanılan aynı desen)
- [ ] **`[AllowAnonymous]` attribute'ü `TripsController.GetById` action'ına eklenir** — `[Authorize]` sınıf seviyesinden miras kalmasın
- [ ] Integration test: guest/başka user'ın Draft/Archived trip'e erişimi 404 dönmeli; owner kendi Draft/Archived trip'ini görebilmeli; **anonim (token'sız) istek Published trip'i görebilmeli, Draft/Archived'de 404 almalı**

**Ek düzeltme (Budget privacy kararı — bütçe herkese açık, anonim dahil):** `GetBudgetSummaryQueryHandler.cs:36-37`'de şu an **katı owner-only** kontrolü var (`if (trip.OwnerId != currentUserId) throw new ForbiddenException(...)`). Trip Detail'deki Toplam Bütçe satırı artık **anonim dahil herkese açık** olacağı için bu kısıtlama **`GetTripByIdQueryHandler` ile aynı duruma (status-bazlı)** çevrilir:
- [ ] `GetBudgetSummaryQueryHandler`'daki katı `ForbiddenException` kuralı kaldırılır, yerine `trip.Status != TripStatus.Published && trip.OwnerId != currentUserId` → `EntityNotFoundException` konur (Published trip'lerde owner olmayan **ve anonim** de görebilir; Draft/Archived'de sadece owner)
- [ ] **`[AllowAnonymous]` attribute'ü `TripsController.GetBudgetSummary` action'ına eklenir**
- [ ] `GetBudgetSummaryQueryHandler`'a da `GetTripByIdQueryHandler`'daki gibi anonim-güvenli `Guid.TryParse` deseni eklenir (şu an `Guid.Parse(_authService.UserId)` kullanıyor — anonim istekte `UserId` null/boş olursa bu **exception fırlatır**, `TryParse` ile güvenli hale getirilmeli)
- [ ] İlgili unit/integration testler güncellenir (mevcut "sadece owner görebilir" testleri artık "Published'da herkes + anonim, Draft/Archived'de sadece owner" olarak değişir)

**⚠️ Kapsam genişletmesi (kod incelemesinde 2 kardeş handler'da aynı sınıf hata bulundu):** Bu güvenlik açığı sadece `GetTripByIdQuery`'ye özgü değilmiş — trip'in **child kaynaklarını** okuyan diğer handler'larda da aynı desen eksik/tutarsız:

- **`GetTimelineQueryHandler.cs:39-44`** — `trip.Status != Published` durumunda `Guid.Parse(_authService.UserId)` çağırıyor (satır 41), **`TryParse` değil**. Anonim istek geldiğinde `UserId` boş/null olduğu için bu **`FormatException` fırlatır** (temiz bir 404 yerine beklenmeyen 500 hatası)
- **`GetTripDestinationsQueryHandler.cs:51`** — sadece `TripStatus.Draft` için owner-only kontrolü var, **`Archived` hiç kontrol edilmiyor**. Yani şu an **Archived bir trip'in destinasyonları owner olmayan herkese açık** — bu, B0.10'un asıl çözmeye çalıştığı sorunun aynısı, farklı bir endpoint'te
- **`GetRecommendedPlacesQueryHandler.cs:35-40`** — handler mantığı zaten doğru (`Published != status` ise owner-only), ama aynı güvensiz `Guid.Parse` (satır 37) var **ve** `TripsController.GetRecommendPlaces` action'ında (satır 155) `[AllowAnonymous]` yok — anonim istek handler'a hiç ulaşamıyor (401). Niyet doğru, uygulama eksik.

**Karar (Recommended Places görünürlüğü):** Diğer child kaynaklarla (Timeline, Budget Summary, Destinations) tutarlı olması için **Recommended Places de Published trip'lerde herkese açık (anonim dahil)** olacak — ayrı bir owner/collaborator-only istisna yapılmıyor. "Timeline'a ekle" aksiyonu zaten sadece owner'a görünür, misafir sadece görüntüler. (`omniflow-mobile/OMNIFLOW_PAGE_ARCHITECTURE.md § 8.3` güncellendi.)

**Kök neden:** Bu mantık (trip Published değilse owner-only, 404 ile) her handler'da ayrı ayrı yazılıyor, bu yüzden tutarsızlıklar/eksiklikler çıkıyor. Çözüm: **paylaşılan bir visibility helper**.

- [ ] `ITripVisibilityService` (veya basit bir static extension) eklenir — `EnsureVisibleOrThrow(Trip trip, string? currentUserIdString)`: `trip.Status != Published` ise `currentUserIdString`'i güvenli `TryParse` eder, parse başarısız (anonim) veya `trip.OwnerId != currentUserId` ise `EntityNotFoundException` (404) fırlatır
- [ ] `GetTripByIdQueryHandler`, `GetBudgetSummaryQueryHandler`, `GetTimelineQueryHandler`, `GetTripDestinationsQueryHandler`, `GetRecommendedPlacesQueryHandler`, **ve B0.9'un `GetTripChecklistStatusQuery`'si** — **hepsi bu paylaşılan helper'ı kullanacak şekilde güncellenir/yazılır**, kendi ad-hoc kontrollerini kaldırırlar (checklist henüz implemente edilmediği için sıfırdan bu helper'la yazılır, diğerleri mevcut ad-hoc kontrollerini helper'a taşır)
- [ ] `GetTimelineQueryHandler` ve `GetRecommendedPlacesQueryHandler`'daki güvensiz `Guid.Parse` → helper üzerinden güvenli hale gelir
- [ ] `GetTripDestinationsQueryHandler`'a eksik olan `Archived` kontrolü eklenir (helper kullanılınca otomatik gelir)
- [ ] **`[AllowAnonymous]` attribute'ü `TripsController.GetRecommendPlaces` action'ına eklenir**
- [ ] Bu handler'lardaki mevcut `ForbiddenException` (403) davranışı da **`EntityNotFoundException` (404)**'a çevrilir — B0.10'un "private trip'in varlığı bile sızdırılmasın" prensibiyle tutarlı olsun
- [ ] Integration test: **6 endpoint** (GetById, BudgetSummary, Timeline, Destinations, Checklist, RecommendPlaces) için de Draft/Archived + anonim/non-owner → 404; owner → 200; Published + anonim/herkes → 200

---

### Task B0.11: Trip Unarchive / Yeniden Yayınla

**Tahmini Süre:** 1.5 saat
**Durum:** [ ] Bekliyor

> **Bağlam:** Şu an `Draft → Published → Archived` tek yönlü bir akış; Archived durumdan geri dönüş yok (sadece Delete mümkün). Trip Detail üst bar menüsünde Archived trip'ler için **"Yayına Al"** seçeneği isteniyor — bu, backend'de yeni bir komut gerektiriyor.
>
> **Mobil karşılığı:** `MOBILE_ROADMAP.md → M3 / Task 3.2` (üst bar `⋮` menü — durum bazlı içerik), tasarım detayları `omniflow-mobile/TRIP_DETAILS_PAGE.md`

- [ ] `UnarchiveTripCommand` + Handler — `ArchiveTripCommandHandler` ile simetrik yapı: owner kontrolü (`ForbiddenException`), sadece `TripStatus.Archived` durumundaki trip'lere uygulanabilir (`ApiException` diğer durumlarda)
- [ ] `trip.Status = TripStatus.Published` olarak günceller
- [ ] Endpoint: `POST /api/v1/Trips/{id}/unarchive` *(büyük T — `TripsController`'ın `[controller]` token casing'iyle tutarlı, B0.14'teki `unpublish` ile aynı)*
- [ ] Unit + integration test (owner/non-owner, yanlış durumdan unarchive denemesi)

**Owner Menü İçeriği (durum bazlı, mobil tarafta uygulanacak):**

| Trip Durumu | Menü İçeriği |
|---|---|
| **Draft** | Yayınla · Sil |
| **Published** | Arşivle · **Düzenlemek için Taslağa Al** · Paylaş · Sil |
| **Archived** | Yayına Al · Sil *(**Paylaş yok** — Archived owner-only, B0.10 gereği 404 döner; paylaşılan link işe yaramaz. Draft'ta zaten aynı sebeple Paylaş yok, Archived'da da tutarlı olsun diye kaldırıldı)* |

---

### Task B0.14: 🔴 KRİTİK — Trip Unpublish (Düzenleme İçin Taslağa Al)

**Tahmini Süre:** 1.5 saat
**Durum:** [ ] Bekliyor
**Öncelik:** 🔴 Kritik — ciddi bir ürün kısıtı çözüyor

> **Bulgu:** `UpdateTimelineEntryCommandHandler`, `CreateTimelineEntryCommandHandler`, `DeleteTimelineEntryCommandHandler`, `ReorderTimelineEntriesCommandHandler`, `UpdateTripCommandHandler` ve destinasyon CRUD handler'larının **hepsi** `trip.Status != TripStatus.Draft` ise reddediyor. Backend'de `Published → Draft` geri dönüşü **hiç yok** (sadece `Draft→Published→Archived→(B0.11 ile)Published` döngüsü var). Sonuç: **bir trip yayınlandığı anda timeline'ı sonsuza kadar donuyor** — owner bir daha hiçbir entry ekleyemez/düzenleyemez/silemez/kilit açamaz, Archived'a alıp tekrar Published yapsa bile. Bu, gerçek kullanıcı senaryosunda (yayınladıktan sonra plan değişmesi) çok kısıtlayıcı.
>
> **Mobil karşılığı:** `MOBILE_ROADMAP.md → M3 / Task 3.2` (üst bar `⋮` menü, Timeline entry Edit/Delete/Unlock aksiyonları), tasarım detayı `omniflow-mobile/TRIP_DETAILS_PAGE.md`

**Karar:** `ArchiveTripCommandHandler`'a simetrik yeni bir komut — `UnpublishTripCommand`. Draft-only kısıtları **gevşetilmiyor** (Timeline entry mutasyonları hâlâ Draft-only kalıyor), bunun yerine owner trip'i **geçici olarak Draft'a alıp** düzenleyip **tekrar Yayınla**yabiliyor.

- [ ] `UnpublishTripCommand` + Handler — owner kontrolü (`ForbiddenException`), sadece `TripStatus.Published` durumundaki trip'lere uygulanabilir (`ApiException` diğer durumlarda — Archived'dan direkt Unpublish yok, önce Unarchive (B0.11) ile Published'a dönülür, sonra Unpublish edilir; state machine basit tutulur)
- [ ] `trip.Status = TripStatus.Draft` olarak günceller
- [ ] **Sayaçlar korunur:** `UpvoteCount`, `ForkCount`, mevcut `SavedTrip` ilişkileri **silinmez/sıfırlanmaz** — trip Draft'tayken bu sayılar sadece owner'a görünür kalır (B0.10 visibility kuralı gereği), tekrar Publish edilince aynen geri gelir
- [ ] Endpoint: `POST /api/v1/Trips/{id}/unpublish`
- [ ] Unit + integration test (owner/non-owner, Draft/Archived'den Unpublish denemesi reddedilmeli, sayaçların korunduğu)

**Mobil davranış:**
- [ ] Üst bar Owner menüsüne **Published** durumunda **"Düzenlemek için Taslağa Al"** eklenir (yukarıdaki tabloya bakınız)
- [ ] Tıklanınca **onay dialogu**: "Bu geziyi düzenlemek için yayından kaldıracaksın — düzenleme bitince tekrar yayınlaman gerekecek. Devam edilsin mi?" + İptal/Devam Et
- [ ] Onaylanınca `POST /api/v1/Trips/{id}/unpublish` çağrılır, başarılı olursa trip Draft'a döner, Timeline'daki Edit/Kilidi Aç/Sil/+Detay Ekle aksiyonları artık aktif olur
- [ ] Trip Draft'tayken herkese kapalıdır (B0.10), owner "Yayınla" ile düzenleme bitince tekrar Published'a alır

---

### Task B0.12: TripDestination Koordinat (Geocoding)

**Tahmini Süre:** 3.5 saat *(Nominatim operasyonel gereksinimleri nedeniyle 2.5 saatten güncellendi)*
**Durum:** [ ] Bekliyor

> **Bulgu:** Trip Detail'in haritası (`omniflow-mobile/TRIP_DETAILS_PAGE.md`, MapLibre pinler + ORS yol modu) destinasyon koordinatlarına ihtiyaç duyuyor, ama `TripDestinationResponse` sadece `City`/`Country`/`ArrivalDate`/`DepartureDate`/`OrderIndex`/`NightCount` döndürüyor — **koordinat yok**. `Place` entity'sinde `Latitude`/`Longitude` zaten var (OSM/Google Places'ten önceden import edilmiş), ama bu veri statik — çalışma zamanında "bu şehri geocode et" diyebilecek bir servis **hiç yok**.
>
> **Mobil karşılığı:** `MOBILE_ROADMAP.md → M3 / Task 3.2` (Trip Detail — Map bölümü), tasarım detayı `omniflow-mobile/TRIP_DETAILS_PAGE.md`

**Karar:** Backend, destinasyon oluşturulduğunda/güncellendiğinde City+Country'yi **bir kere** geocode edip `TripDestination`'a kaydeder (mobil client-side geocoding yok, her seferinde tekrar istek atılmaz).

> **⚠️ Nominatim operasyonel kısıtlar (resmi kullanım politikası):** Public Nominatim instance'ı (`nominatim.openstreetmap.org`) **max 1 istek/saniye**, geçerli/tanımlayıcı bir `User-Agent` (jenerik/tarayıcı UA'sı değil), **sonuçların cache'lenmesi zorunlu**, ve ağır kullanımda self-host önerisi şartlarını taşıyor. Wizard tek seferde **10 destinasyona kadar** oluşturabildiği için, bunlar naif şekilde paralel/hızlı ardışık geocode edilirse hem rate-limit'e takılır hem policy ihlali olur.

- [ ] `TripDestination` entity'sine `Latitude` (double?), `Longitude` (double?) alanları + migration
- [ ] `IGeocodingService` arayüzü + **Nominatim** implementasyonu — `City, Country` string'ini koordinata çevirir. Arayüz sağlayıcıdan bağımsız tasarlanır (ileride self-hosted Nominatim veya başka bir servise **config ile** geçilebilsin — base URL + provider seçimi `appsettings.json`'da)
- [ ] **Reverse geocoding:** `IReverseGeocodingService` (veya `IGeocodingService` içinde ayrı reverse method) eklenir — `Latitude, Longitude` değerini şehir/ülke veya formatted location text'ine çevirir. Bu altyapı B0.5.1'de saklanan `User.LocationLatitude` / `User.LocationLongitude` değerlerinden `User.Location` üretmek için kullanılacak.
- [ ] **Rate limiting:** Servis içinde bir kuyruk/semaphore ile giden istekler **max 1 istek/saniye** olacak şekilde sınırlanır (10 destinasyonluk bir wizard submit'i naif paralel çağrı yerine sıraya alınır)
- [ ] **Cache:** Geocode sonuçları `(City, Country)` normalize edilmiş anahtarla **kalıcı (DB tablosu)** olarak cache'lenir — production'da in-memory cache yetersiz kalır (uygulama yeniden başlayınca/birden fazla instance'ta kaybolur, Nominatim policy'sinin "sonuçları cache'le" şartını kalıcı karşılamaz). In-memory sadece **dev/local'da DB'ye ek bir L1 hız katmanı** olarak opsiyonel kullanılabilir, tek başına yeterli değil. Aynı şehir (ör. "Paris, France") birden fazla trip/kullanıcı tarafından istenirse Nominatim'e tekrar gidilmez — bu, pratikte rate-limit baskısının büyük kısmını da çözer (popüler şehirler zaten cache'te olur)
- [ ] **User-Agent:** İsteklerde uygulamayı tanımlayan özel bir `User-Agent` header'ı gönderilir (ör. `OmniFlow/1.0 (+iletişim e-postası)`), jenerik/varsayılan HTTP client UA'sı kullanılmaz
- [ ] **Timeout:** Makul bir timeout (ör. 5 sn) — Nominatim yanıt vermezse hata toleransı kuralına (aşağıda) düşer, wizard submit'ini süresiz bekletmez
- [ ] `CreateTripDestinationCommandHandler`, `UpdateTripDestinationCommandHandler` ve `CreateTripWizardCommandHandler` (destinasyon oluşturan/güncelleyen üç nokta) geocoding çağrısını yapacak şekilde güncellenir — **senkron ve cache+rate-limit korumalı** (M3 kapsamında background job/kuyruk sistemi **bilinçli olarak kurulmuyor** — basitlik tercih edildi). **Gerçekçi zamanlama:** 1 istek/saniye limiti nedeniyle, en kötü senaryoda (10 destinasyonun **hepsi cache miss** — yani hiçbiri daha önce başka bir trip'te geocode edilmemiş) wizard submit'i **10+ saniyeye kadar uzayabilir** (timeout'larla daha da fazla). Pratikte çoğu trip popüler şehirler içerdiği için cache sayesinde çok daha hızlı olması beklenir, ama worst-case bu kadar sürebileceği bilinerek kabul ediliyor. Mobil tarafta wizard submit zaten bir loading state gösteriyor, bu süre o loading state içinde karşılanır. İleride gerçek bir kullanıcı şikayeti/performans sorunu olursa arka plan job'una geçiş değerlendirilebilir
- [ ] **Hata toleransı:** Geocoding başarısız olursa (servis erişilemez, timeout, şehir bulunamaz vb.) `Latitude`/`Longitude` `null` kalır — komut **hata fırlatmaz**, destinasyon yine de oluşturulur/güncellenir; mobil tarafta o destinasyon için pin gösterilmez, diğer pinler etkilenmez
- [ ] **User profile reverse geocoding entegrasyonu:** B0.5.1 koordinatları geldiğinde backend `Location` text'i otomatik üretebilecek hale getirilir. Reverse geocoding başarısız olursa profil update'i başarısız olmaz; mevcut `Location` korunur veya request'te gelen manuel `Location` kullanılır.
- [ ] `TripDestinationResponse`'a `Latitude`/`Longitude` (nullable) eklenir
- [ ] Unit test: geocoding başarılı/başarısız senaryoları, mevcut destinasyon davranışının bozulmadığı

---

### Task B0.13: TimelineEntry → Checklist Bağlantısı (PlanningSlotKey)

**Tahmini Süre:** 1.5 saat
**Durum:** [ ] Bekliyor

> **Bulgu:** Trip Detail'in Detay Modalı, bir Flights/Hotels checklist satırı (ör. "İstanbul → Roma" leg'i) için **gerçek bir TimelineEntry'nin var olup olmadığını** göstermesi gerekiyor (bkz. `omniflow-mobile/TRIP_DETAILS_PAGE.md → Detay Modal — İçerik / Durum A vs B`). Bunu client-side **şehir adı/tarih heuristiği** ile yapmak kırılgan — aynı leg/gece için birden fazla `CustomFlight`/`CustomTransport`/`CustomAccommodation` entry'si varsa (ör. kullanıcı önce uçuş sonra taksi transferi eklediyse, veya oteli değiştirdiyse) **yanlış entry'ye bağlanma riski** var.
>
> **Mobil karşılığı:** `MOBILE_ROADMAP.md → M3 / Task 3.2`, tasarım detayı `omniflow-mobile/TRIP_DETAILS_PAGE.md → Detay Modal — İçerik`

**Karar:** `TimelineEntry`'ye opsiyonel `PlanningSlotKey` (string?) alanı eklenir — B0.9'daki `itemKey` formatıyla **aynı değer** (`flight-leg:{fromId}:{toId}` / `hotel-night:{destId}:{nightNumber}`). Bu alan **sadece** Detay Modal'ın "**+ Detay Ekle**" CTA'sından (Durum B → yeni entry oluşturma akışı) geçilen entry'lere set edilir — normal Timeline entry ekleme akışından oluşturulan entry'ler bu alanı `null` bırakır ve checklist'e otomatik bağlanmaz (checklist confirmation o entry'lerden **bağımsız, manuel** kalmaya devam eder).

- [ ] `TimelineEntry` entity'sine `PlanningSlotKey` (string?, nullable) alanı + migration
- [ ] **Partial unique index** — `(TripId, PlanningSlotKey)` where **`planning_slot_key IS NOT NULL AND deleted_at IS NULL`** (`TimelineEntry` soft-delete kullanıyor — `deleted_at` filtresi olmadan, silinmiş bir entry aynı slot'u "kullanılmış" gibi tutar ve o slota **yeni entry eklemeyi kalıcı olarak engeller**; bu yüzden filtreye `deleted_at IS NULL` şart)
- [ ] **DTO/Command zinciri tam olarak güncellenir** (sadece prosa değil, somut dosyalar):
  - `CreateTimelineEntryRequest.cs` → opsiyonel `PlanningSlotKey` (string?) alanı eklenir
  - `CreateTimelineEntryCommand.cs` → aynı alan eklenir, controller'dan handler'a taşınır
  - `CreateTimelineEntryCommandHandler` → `PlanningSlotKey`'i yeni `TimelineEntry`'ye set eder (factory metodlarına parametre olarak eklenir veya oluşturulduktan sonra atanır)
  - `TimelineEntryResponse.cs` → `PlanningSlotKey` (string?) response'a eklenir
  - AutoMapper profili güncellenir (entity → response mapping'inde bu alan otomatik gelsin)
  - Detay Modal'ın "+ Detay Ekle" CTA'sı bu alanı doldurur, normal Timeline ekleme akışı (Task 3.13'ün standart formu) boş bırakır
- [ ] **`PlanningSlotKey` sadece create'te set edilir, immutable'dır:** `UpdateTimelineEntryRequest`/`UpdateTimelineEntryCommand`'a **bu alan bilinçli olarak eklenmez** — yani bir entry oluşturulduktan sonra hangi slot'a bağlı olduğu **değiştirilemez**. Kullanıcı bir entry'yi başka bir slot'a bağlamak isterse (nadir senaryo), mevcut entry'yi **siler ve yeni bir entry oluşturur** (yeni entry oluştururken doğru `planningSlotKey` ile). Bu, slot ownership'in Update akışı üzerinden karışmasını (ör. yanlışlıkla başka bir slot'un key'ini üzerine yazma) baştan engeller
- [ ] **Entry silinirse checklist confirmation'a ne olur:** `TripChecklistConfirmation` (B0.9), `TimelineEntry`'den **tamamen bağımsız bir tablo** — entry silinse bile confirmation kaydı **silinmez/değişmez**. Yani kullanıcı "İstanbul→Roma" uçuşunu ekleyip checklist'i işaretledikten sonra o entry'yi silerse, checklist **checked kalır** (kullanıcının "hallettim" beyanı entry'nin varlığından bağımsız bir gerçek) — sadece bir sonraki modal açılışında **Durum B**'ye döner (`PlanningSlotKey` eşleşen entry artık yok)
- [ ] `GetTripByIdQuery`/`GetTimelineQuery` response'larına `PlanningSlotKey` eklenir (mobil, Durum A/B ayrımını artık **exact match** ile yapar: `entries.any { it.planningSlotKey == itemKey }` — şehir/tarih heuristiği yok)
- [ ] Unit test: aynı slot'a ikinci kez entry oluşturma denemesi engelleniyor (unique constraint); **soft-delete edilmiş bir entry'nin slot'u serbest bıraktığı** (yeni entry aynı `planningSlotKey` ile oluşturulabiliyor) test edilir; normal akıştan oluşan entry'lerin `PlanningSlotKey`'i null kalıyor; entry silinince ilgili checklist confirmation'ın **değişmediği** test edilir

---

### Task B0.15: ORS Routing Proxy ("Yol" Modu)

**Tahmini Süre:** 2 saat
**Durum:** [ ] Bekliyor

> **Bulgu:** Map'in `Yol` modu OpenRouteService (ORS) polyline kullanıyor (bkz. `omniflow-mobile/TRIP_DETAILS_PAGE.md`), ama **kimin ORS'u çağıracağı hiç netleşmemiş**. ORS, Nominatim'in aksine **API key gerektiriyor** (ücretsiz tier'da bile). Mobil doğrudan ORS'u çağırırsa, **API key APK içine gömülür** — decompile edilip çıkarılabilir, kötüye kullanılırsa uygulamanın günlük ORS kotası (free tier'da sınırlı) tüketilip **herkes için** Yol modu çalışmaz hale gelir. Bu, Nominatim'den farklı bir risk (Nominatim key istemiyor).
>
> **Mobil karşılığı:** `omniflow-mobile/TRIP_DETAILS_PAGE.md → Map — 2 Mod` (Yol modu ORS polyline)

**Karar:** ORS çağrısı **backend üzerinden proxy'lenir** — mobil ORS'u hiç doğrudan çağırmaz, API key backend'de (appsettings/secret olarak) kalır, hiçbir zaman client'a gönderilmez.

- [ ] `IRoutingService` arayüzü + **OpenRouteService** implementasyonu (Directions API) — destinasyon koordinat sırasını alır, polyline/koordinat listesi döner
- [ ] **Null koordinat filtresi (ORS'a gitmeden önce):** Handler, ORS'a istek atmadan önce `Latitude`/`Longitude`'u `null` olan destinasyonları **sıradan çıkarır** — sadece geçerli koordinatlı destinasyonlar ORS'a gönderilir (mobil tarafın "null koordinatlı destinasyonu atla, bir sonraki geçerliye direkt bağlan" kuralıyla aynı mantık, backend'de de uygulanır). Bu filtre olmadan ORS'a eksik/geçersiz koordinat gönderilirse ORS hata döner, gereksiz yere hata-path'inden fallback'e düşülür ve ORS kotası boşa harcanır
- [ ] **Cache:** Bir trip'in rotası, destinasyonlar değişmediği sürece **aynı kalır** — hesaplanan polyline trip başına cache'lenir (DB'de bir alan veya ayrı küçük tablo), destinasyon eklenir/silinir/sıralanırsa cache geçersiz kılınıp yeniden hesaplanır. Bu hem ORS'un kendi rate-limit'ini (free tier'da dakikada/günde sınırlı istek) korur hem performansı artırır
- [ ] Endpoint: `GET /api/v1/Trips/{id}/route` → `{ "coordinates": [[lat,lng], [lat,lng], ...] }` (mobil bunu MapLibre'de polyline olarak çizer)
- [ ] Visibility kuralı diğerleriyle aynı (B0.10'un paylaşılan helper'ı) — Published'da herkese açık, Draft/Archived'de owner-only
- [ ] **Hata toleransı:** ORS erişilemez/timeout olursa endpoint hata döner, mobil tarafta zaten var olan "sessizce Kuş Bakışı'na fallback" davranışı devreye girer (backend hatası = mobilde ORS başarısızlığıyla aynı şekilde ele alınır)
- [ ] Unit test: cache invalidation (destinasyon değişince yeniden hesaplanıyor), owner/anonim/Published-Draft erişim matrisi

---

### Definition of Done (B0)

- [ ] Dokümanlar koddaki gerçeği yansıtıyor
- [ ] `dotnet test` çözüm seviyesinde çalışıyor ve CI'da koşuyor
- [ ] Provider verisinin tazeliği API'den görülebiliyor
- [ ] User profili konum ve seyahat stili alanlarını destekliyor
- [ ] Draft trip'lerde tamamlanma yüzdesi hesaplanıp dönüyor
- [ ] Published trip'lerde görüntülenme sayısı (`ViewCount`, anonim dahil) doğru artıyor ve gösteriliyor
- [ ] Trip kapak fotoğrafı yüklenebiliyor
- [ ] Trip Detail Review modundaki checklist satırları işaretlenip kalıcı olarak saklanabiliyor
- [ ] Draft/Archived trip'ler owner olmayan kullanıcılara 404 dönüyor (B0.10)
- [ ] Archived trip'ler tekrar Published durumuna alınabiliyor (B0.11)
- [ ] Destinasyonlar koordinat ile dönüyor, Trip Detail haritasında pinlenebiliyor (B0.12)
- [ ] Checklist satırları ile TimelineEntry'ler arasında belirsizlik olmadan (exact match) bağlantı kuruluyor (B0.13)
- [ ] Owner, yayınladığı bir trip'i düzenlemek için Taslağa alıp tekrar yayınlayabiliyor, sayaçlar korunuyor (B0.14)
- [ ] Map'in Yol modu backend proxy üzerinden çalışıyor, ORS API key client'a hiç gitmiyor (B0.15)

> **📝 Düşük öncelikli not (gelecekte değerlendirilebilir):** `TripsController` (`[controller]` token → `/api/v1/Trips/...`, büyük T) ile `TimelineController` (literal route override → `/api/v1/trips/...`, küçük t) arasında casing tutarsızlığı var. ASP.NET Core route matching genelde case-insensitive çalıştığı için şu an **işlevsel bir sorun yaratmıyor**, ama Retrofit contract'larında gereksiz kafa karışıklığına yol açabiliyor. Kısa vadede dokümanlarda gerçek casing'in yazılması (yapıldı) yeterli; orta/uzun vadede tüm controller route'larının lowercase literal'e standardize edilmesi düşünülebilir (B0 kapsamında zorunlu değil).

---

## 🎯 B1 — Google OAuth (External Login)

### Scope

Email/şifre + JWT akışının yanına Google ile giriş eklenir. Mobilde Google Sign-In SDK ID token üretir; backend bu token'ı doğrular, kullanıcıyı bulur/oluşturur ve kendi JWT'sini döner.

**Mobil karşılığı:** `MOBILE_ROADMAP.md → M7`

### Task B1.1: Google Token Doğrulama Altyapısı

**Tahmini Süre:** 2 saat
**Durum:** [ ] Bekliyor

- [ ] `Google.Apis.Auth` NuGet paketi
- [ ] `GoogleAuthSettings.cs` (ClientId — web + android client id'leri)
- [ ] `IGoogleTokenValidator` + implementasyon — gelen ID token'ı doğrular, payload (email, name, sub, picture) döner
- [ ] Geçersiz/expired token → `ApiException` (401)

### Task B1.2: External Login Akışı

**Tahmini Süre:** 3 saat
**Durum:** [ ] Bekliyor

- [ ] `IAccountService`'e `GoogleLoginAsync(string idToken)` eklensin
- [ ] Email ile mevcut `ApplicationUser` bul:
  - [ ] Varsa → JWT + refresh token üret (mevcut akış)
  - [ ] Yoksa → yeni `ApplicationUser` + aynı Id ile Domain `User` oluştur (username Google adından türetilir + benzersizleştirilir), `IsVerified = true`
- [ ] `AspNetUserLogins` üzerinden Google provider eşlemesi kaydedilsin
- [ ] `GoogleLoginRequest.cs` (IdToken) + validator
- [ ] `POST /api/account/google` endpoint'i (dual platform: web cookie / mobile body — mevcut refresh pattern'i ile aynı)

### Task B1.3: Test & Doğrulama

**Tahmini Süre:** 1 saat
**Durum:** [ ] Bekliyor

- [ ] Geçerli token → 200 + token pair
- [ ] Yeni kullanıcı otomatik oluşuyor, ikinci girişte aynı kullanıcı dönüyor
- [ ] Geçersiz token → 401

### Definition of Done (B1)

- [ ] Mobil bir Google ID token gönderip OmniFlow JWT'si alabiliyor
- [ ] Hem yeni hem mevcut kullanıcı senaryosu çalışıyor
- [ ] Username çakışması güvenli biçimde çözülüyor

---

## 🎯 B2 — Live Trip Altyapısı

### Scope

Seyahat sırasında kullanım için backend desteği: gerçek ziyaret kayıtları (Visit Log), trip kapanış özeti (Trip Summary) ve zaman dilimi normalizasyonu.

**Mobil karşılığı:** `MOBILE_ROADMAP.md → M8`

### Task B2.1: Visit Log Entity

**Tahmini Süre:** 2.5 saat
**Durum:** [ ] Bekliyor

- [ ] `PlaceVisitLog.cs` entity — `TripId`, `TimelineEntryId` (nullable), `PlaceId` (nullable), `UserId`, `ActualCost` (decimal?), `CurrencyCode`, `Rating` (1-5?), `Note` (nullable), `VisitedAt` (DateTime)
- [ ] `PlaceVisitLogConfiguration.cs` (CHECK: rating 1-5, cost ≥ 0) + index (trip_id, user_id)
- [ ] Migration

### Task B2.2: Visit Log CQRS + Controller

**Tahmini Süre:** 2.5 saat
**Durum:** [ ] Bekliyor

- [ ] `CreateVisitLogCommand` + Validator (owner kontrolü)
- [ ] `UpdateVisitLogCommand`, `DeleteVisitLogCommand`
- [ ] `GetVisitLogsByTripQuery`
- [ ] DTO'lar + mapping
- [ ] Endpoint'ler: `POST/GET/PUT/DELETE /api/v1/trips/{tripId}/visit-logs`

### Task B2.3: Trip Summary Query

**Tahmini Süre:** 3 saat
**Durum:** [ ] Bekliyor

- [ ] `GetTripSummaryQuery` — ziyaret edilen entry sayısı, tahmini vs gerçek harcama (visit log'lardan), toplam gün/destinasyon, en yüksek puanlı duraklar, rota özeti
- [ ] `TripSummaryResponse` DTO
- [ ] Endpoint: `GET /api/v1/trips/{tripId}/summary`
- [ ] Sadece tamamlanmış/aktif trip'ler için anlamlı veri

### Task B2.4: Timezone Normalization

**Tahmini Süre:** 3 saat
**Durum:** [ ] Bekliyor

- [ ] `TripDestination`'a `Timezone` (IANA, nullable) alanı (yoksa) eklensin + migration
- [ ] Tüm zaman alanlarının UTC saklama stratejisi netleştirilsin; client'a destinasyon timezone'u ile birlikte dönülsün
- [ ] Timeline/budget hesaplarında lokal saat ile cihaz saati karışmasın (helper)
- [ ] Push reminder ve live trip hesapları için temel atılsın

### Definition of Done (B2)

- [ ] Kullanıcı bir durağı gerçek harcama + puan + not ile loglayabiliyor
- [ ] Trip bitiminde özet (tahmini vs gerçek) endpoint'ten alınabiliyor
- [ ] Zaman değerleri tutarlı (UTC + timezone) dönüyor

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

Lokal para birimi ana, kullanıcının para birimi ikincil gösterilecek. Backend günlük kur sağlar.

**Mobil karşılığı:** `MOBILE_ROADMAP.md → M13`

### Task B7.1: Exchange Rate Servisi

**Tahmini Süre:** 2.5 saat
**Durum:** [ ] Bekliyor

- [ ] Ücretsiz bir kur API'si entegrasyonu (`IExchangeRateService`)
- [ ] Sonuç cache'lenir (Redis planlı; yoksa in-memory / DB tablosu)
- [ ] `GET /api/v1/currency/rates?base=` endpoint'i

### Task B7.2: Günlük Cron

**Tahmini Süre:** 1.5 saat
**Durum:** [ ] Bekliyor

- [ ] `BackgroundService` (hosted) ile günde 1 kez kur güncelleme
- [ ] Hata durumunda son başarılı snapshot korunur

### Definition of Done (B7)

- [ ] Güncel kurlar endpoint'ten alınabiliyor
- [ ] Günlük otomatik güncelleme çalışıyor, dış servis düşse bile son veri duruyor

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
