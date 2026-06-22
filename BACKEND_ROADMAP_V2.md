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
| **B0** | Temel & Temizlik (as-built docs, CI test, provider freshness) | — (önkoşul) | 🔴 Yüksek |
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
**Durum:** [ ] Bekliyor

- [ ] `BACKEND_SCHEMA_MVP.md` → `Stop`/`StopsController`/`IStopRepositoryAsync` referanslarını `TimelineEntry`/`TripDestination` ile değiştir, 18 tablo → 24 tablo güncelle
- [ ] `README.md` → "20 tables", 5 TravelStyle, eski endpoint listesi güncellensin
- [ ] `CLAUDE.md` ve `AGENTS.md` → silinmiş `Stop` + `select` endpoint anlatıları kaldırılsın, güncel controller listesi yazılsın
- [ ] Güncel enum listesi (25 enum), güncel migration listesi (11) yansıtılsın
- [ ] Flight/Hotel `select` endpoint'lerinin kaldırıldığı ve mantığın `TimelineEntry`'ye taşındığı not düşülsün

### Task B0.2: CI/CD Kalite Kapısı

**Tahmini Süre:** 2 saat
**Durum:** [ ] Bekliyor

- [ ] `azure-pipelines.yml`'a `dotnet test` adımı eklensin (publish'ten önce)
- [ ] `.slnx` / SDK uyumsuzluğu çözülsün (SDK 9'a geç **veya** klasik `.sln` üret) — `dotnet test` çözüm seviyesinde çalışmalı
- [ ] Test başarısızsa pipeline kırmızı olsun (deploy engellensin)
- [ ] (Opsiyonel) PR trigger eklensin

### Task B0.3: Provider Freshness / Data Quality Alanları

**Tahmini Süre:** 2 saat
**Durum:** [ ] Bekliyor

- [ ] `ProviderFlight` / `ProviderHotel` entity'lerine `LastUpdatedAt`, `IsLiveData` (bool), `DataSnapshotDate` alanları (yoksa) eklensin
- [ ] Migration
- [ ] Provider response DTO'larına freshness bilgisi eklensin ("son güncelleme", "canlı değil/tahmini")
- [ ] `ProvidersController` response'larında bu alanlar dönsün

### Task B0.4: AI Scaffold Kararı

**Tahmini Süre:** 30 dakika
**Durum:** [ ] Bekliyor

- [ ] Boş `AiTimelineService.cs`, `AiFallbackService.cs`, `GenerateTimelineCommand.cs` dosyaları → B6'ya kadar **kaldırılsın** (kafa karışıklığı yapmasın) veya açık `// TODO B6` notuyla işaretlensin
- [ ] İlgili boş interface'ler B6 tasarımına göre yeniden değerlendirilsin

### Definition of Done (B0)

- [ ] Dokümanlar koddaki gerçeği yansıtıyor
- [ ] `dotnet test` çözüm seviyesinde çalışıyor ve CI'da koşuyor
- [ ] Provider verisinin tazeliği API'den görülebiliyor

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

---

## 🎯 B4 — Collections, Global Search, Deep-link, Memories

### Scope

Kişisel düzenleme ve keşif katmanı. Mevcut saved-trips üzerine koleksiyonlar; çok-varlıklı global arama; paylaşılabilir trip linkleri; gezi günlüğü.

**Mobil karşılığı:** `MOBILE_ROADMAP.md → M10`

### Task B4.1: Collections

**Tahmini Süre:** 4 saat
**Durum:** [ ] Bekliyor

- [ ] `Collection.cs` (UserId, Name, Description?, CoverPhotoUrl?) + `CollectionItem.cs` (CollectionId, TripId)
- [ ] Configuration'lar + unique (collection_id, trip_id) + migration
- [ ] CQRS: Create/Update/Delete collection, Add/Remove trip, GetMyCollections, GetCollectionDetail
- [ ] Endpoint'ler: `GET/POST/PUT/DELETE /api/v1/collections`, `POST/DELETE /api/v1/collections/{id}/trips/{tripId}`

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

### Definition of Done (B4)

- [ ] Kullanıcı koleksiyon oluşturup trip ekleyebiliyor
- [ ] Tek arama kutusundan çok tipte sonuç dönüyor
- [ ] Trip linki paylaşıldığında önizleme verisi var
- [ ] Gezi günlüğü (not + foto) eklenebiliyor

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
- Block-aware görünürlük (`BlockVisibilityHelper`) yeni listeleme/arama endpoint'lerinde de uygulanır.
- Mobil bağımlılık etiketi formatı: mobil roadmap'te **⛔ Bağımlılık: B{faz}.{task}** olarak geçer.
