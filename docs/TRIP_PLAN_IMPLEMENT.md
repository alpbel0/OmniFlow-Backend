# OmniFlow — Trip Planning Implementation Plan

> Bu dosya, `TRIP_PLANNING.md` PRD'sinin mevcut kod tabanına nasıl uygulanacağını detaylı açıklar.
> Her bölümde: ne değişecek, ne eklenecek, ne silinecek, hangi dosyalar etkilenecek belirtilmiştir.

---

## 0. Mevcut Durum Özeti

### Sahip Olduğumuz
- Trip: tek şehir modeli (`City`, `Country`, `StartDate`, `EndDate`), tek `TravelStyle` enum
- Stop: zaman kilitli mekan kartları (place veya custom, LexoRank sıralı)
- Flight: trip'e bağlı uçuş seçimi, `ItineraryGroupId` ile gidiş-dönüş gruplama
- Hotel: trip'e bağlı otel seçimi
- ProviderFlight / ProviderHotel: statik referans verisi (şehir bazlı uçuş/otel)
- Place: 34 kategori, BudgetTiers, TravelStyles, Rating, DurationMinutes, BestMonths, GooglePlaces alanları
- Explore endpoint: cursor pagination, popularity_score, filtreleme

### Eksik Olanlar
- Multi-destination (1-3 şehir sıralı leg)
- Onboarding wizard (8 adım)
- TravelCompanion (Solo/Couple/Family/Friends)
- Tempo (Slow/Moderate/Fast)
- Scoring sistemi (group_score + style_score + google_match_bonus)
- Sezon çarpanı ve budget fallback
- Hotel segmentasyonu (şehir bazlı percentile)
- Custom Entry kartları (4 tip: uçuş, ulaşım, konaklama, etkinlik)
- Timeline kilitleme mantığı (is_locked + buffer)
- Place recommendation (scoring'e göre sıralı öneri endpoint'i)

---

## 1. Enum Değişiklikleri

### 1.1 TravelStyle — 5 değerden 11 değere expand

**Mevcut:** `Solo, Family, Adventure, Luxury, Relax`

**Yeni (PRD'ye göre):**
```csharp
public enum TravelStyle
{
    Romantic,      // Romantik
    Cultural,      // Kültürel
    Adventure,     // Macera
    Nature,        // Doğa
    Local,         // Yerel gibi yaşam
    Relax,         // Relax
    Shopping,      // Alışveriş
    Gastronomy,    // Gastronomi
    Influencer,    // Influencer / Fotoğraf
    Nightlife,     // Gece hayatı
    Budget         // Budget friendly
}
```

**Etkilenen dosyalar:**
- `OmniFlow.Domain/Enums/TravelStyle.cs` — yeniden yaz
- `OmniFlow.Domain/Entities/Trip.cs` — `TravelStyle` alanı `List<TravelStyle>` olacak (max 3)
- `OmniFlow.Infrastructure/Configurations/TripConfiguration.cs` — TravelStyle mapping'i değişecek (tek değer → array)
- `OmniFlow.Infrastructure/Configurations/PlaceConfiguration.cs` — Place.TravelStyles array'i yeni enum'a göre
- `OmniFlow.Application/DTOs/Trip/*` — tüm trip DTO'ları
- `OmniFlow.Application/Features/Explore/Queries/*` — TravelStyle filtresi
- EF Core migration — TravelStyle kolonu text'den text[]'e dönüşecek

**KRİTİK:** Mevcut DB'de `travel_style` kolonu tek string olarak saklanıyor. Bu kolonu `text[]`'e çeviren migration gerekiyor. Mevcut veriler ("Solo", "Family" vb.) geçersiz olacak — bunu migration'da handle et veya mevcut trip'leri güncelle.

### 1.2 Yeni Enum: TravelCompanion

```csharp
public enum TravelCompanion
{
    Solo,     // Tek başına
    Couple,   // Çift
    Family,   // Aile
    Friends   // Arkadaş grubu
}
```

**Dosya:** `OmniFlow.Domain/Enums/TravelCompanion.cs` (yeni)

**Not:** PRD Adım 4 "Kimlerle?" sorusuna karşılık gelir. Bu seçim scoring'de group_score hesaplamasında kullanılır.

### 1.3 Yeni Enum: Tempo

```csharp
public enum Tempo
{
    Slow,      // Yavaş — 2-3 mekan/gün
    Moderate,  // Orta — 4-5 mekan/gün
    Fast       // Yoğun — 6+ mekan/gün
}
```

**Dosya:** `OmniFlow.Domain/Enums/Tempo.cs` (yeni)

### 1.4 Yeni Enum: TimelineEntryType

```csharp
public enum TimelineEntryType
{
    Place,                  // DB'den seçilen mekan
    CustomFlight,           // Kullanıcının kendi uçuşu
    CustomTransport,        // Tren/Otobüs/Gemi/Özel Araç
    CustomAccommodation,    // Kullanıcının kendi oteli
    CustomEvent             // Kullanıcının kendi etkinliği
}
```

**Dosya:** `OmniFlow.Domain/Enums/TimelineEntryType.cs` (yeni)

**Açıklama:** Stop entity'si yerine geçecek TimelineEntry'nin discriminator'ı. Her tip farklı buffer ve lock mantığına sahiptir.

### 1.5 Yeni Enum: TransportPreference

```csharp
public enum TransportPreference
{
    Walking,          // Yürüyerek
    PublicTransport,  // Toplu taşıma
    CarRental         // Araç kiralama
}
```

**Dosya:** `OmniFlow.Domain/Enums/TransportPreference.cs` (yeni)

**Not:** PRD Adım 8 "Ulaşım Tercihi". Mevcut `TransportMode` enum'ından farklı — bu şehir içi ulaşım tercihi, mekanlar arası mesafe hesabında yardımcı bilgi.

### 1.6 Yeni Enum: Season

```csharp
public enum Season
{
    Winter,     // Aralık, Ocak, Şubat — çarpan × 1.2
    Spring,     // Mart, Nisan, Mayıs — çarpan × 1.1
    Summer,     // Haziran, Temmuz, Ağustos — çarpan × 1.5
    Autumn      // Eylül, Ekim, Kasım — çarpan × 1.0
}
```

**Dosya:** `OmniFlow.Domain/Enums/Season.cs` (yeni)

### 1.7 PlaceCategory — PRD ile hizalama

**Mevcut:** 34 değer (15 core + 15 OSM + 4 legacy)

**PRD scoring tablosundaki 27 kategori:**

PRD'nin scoring tablosu şu kategorileri içerir:
Aquarium, Attraction, Bar, Beach, Bridge, Cafe, Castle, Cave, Church, Forest, Gallery, Historical, Information, Mall, Market, Memorial, Monument, Museum, Park, Restaurant, Shopping, Supermarket, Theater, ThemePark, Tower, Viewpoint, Zoo

**Farklar:**
- PRD'de **Lake, Waterfall, Mountain** YOK (bunlar mevcut enum'da var)
- PRD'de **Museum, Cafe, Restaurant** AYRI (mevcut enum'da da ayrı — uyumlu)
- Mevcut `Nature, Entertainment, Hotel, Transport` — PRD'de scoring yok, ama DB'de place olarak var olabilir

**Karar:** Enum'u daraltmak yerine PRD'de olmayan kategorilere scoring'de NEUTRAL (0 puan) verilecek. Bu şekilde mevcut place'ler bozulmaz.

### 1.8 Eski Enum'lar — Dikkat

- `StopAddedBy` enum'ı (Ai, User) → TimelineEntry ile birlikte kaldırılacak veya `AddedBy` olarak güncellenecek
- `FlightDirection` (Outbound, Return) → Multi-destination ile Leg 1, Leg 2, vs. olabilir, ama PRD'de gidiş-dönüş mantığı hâlâ geçerli. Değişiklik gerekmeyebilir.

---

## 2. Entity Değişiklikleri

### 2.1 Trip Entity — Büyük Değişiklik

**Dosya:** `OmniFlow.Domain/Entities/Trip.cs`

**Kaldırılacak alanlar:**
| Alan | Neden |
|------|-------|
| `City` | Tek şehir modeli kalkıyor → TripDestination collection |
| `Country` | Tek şehir modeli kalkıyor → TripDestination collection |
| `TravelStyle` (tek değer) | `List<TravelStyle>` olacak (max 3) |

**Eklenecek alanlar:**
| Alan | Tip | Açıklama |
|------|-----|----------|
| `Origin` | string | Kalkış şehri (Adım 1) |
| `OriginCountry` | string | Kalkış ülkesi |
| `TravelCompanion` | TravelCompanion | Solo/Couple/Family/Friends (Adım 4) |
| `TravelStyles` | List\<TravelStyle\> | Max 3 seçim (Adım 6) —旅途 kiểu |
| `Tempo` | Tempo | Slow/Moderate/Fast (Adım 7) |
| `TransportPreference` | TransportPreference | Walking/PublicTransport/CarRental (Adım 8) |
| `ManualBudget` | decimal? | Kullanıcının girdiği bütçe (Adım 5) |
| `BudgetTier` | BudgetTier | Zaten var, değişmez |
| `AdjustedBudgetTier` | BudgetTier? | Fallback sonrası gerçek tier (backend hesaplar) |

**Değişecek alanlar:**
| Alan | Eski | Yeni |
|------|------|------|
| `StartDate` | DateOnly | Kalkmayabilir — destinasyon bazlı tarihler |
| `EndDate` | DateOnly | Kalkaybilir — destinasyon bazlı tarihler |

**Karar noktası:** `StartDate`/`EndDate` kaldırılır mı? TripDestination'lar zaten her leg için tarih tutuyor. İlk destinasyonun varış tarihi = trip başlangıcı, son destinasyonun çıkış tarihi = trip bitişi. Ama filtreleme/explore için trip seviyesinde tarih lazım olabilir.

**Öneri:** `StartDate`/`EndDate` computed (salt) alan olarak kal, TripDestination'dan otomatik hesaplansın. Veya hesaplamayı service'te yap.

**Korunan alanlar:** `OwnerId`, `ForkedFromId`, `Title`, `Description`, `CoverPhotoUrl`, `Status`, `PersonCount`, `BudgetTier`, `EstimatedCost`, `ForkCount`, `UpvoteCount`, `ViewCount`, `PopularityScore`, `Tags`

**Yeni Navigation:**
```csharp
public ICollection<TripDestination> Destinations { get; set; } = new List<TripDestination>();
public ICollection<TimelineEntry> TimelineEntries { get; set; } = new List<TimelineEntry>();
```

**Kaldırılan Navigation:**
```csharp
// public ICollection<Stop> Stops — Stop → TimelineEntry'e dönüşecek
// Stops navigation'ı TimelineEntries ile değiştirilir
// Flights navigation'ı kalır (provider flight seçimi)
// Hotels navigation'ı kalır (provider hotel seçimi)
```

### 2.2 TripDestination — Yeni Entity

**Dosya:** `OmniFlow.Domain/Entities/TripDestination.cs` (yeni)

```csharp
public class TripDestination : BaseEntity
{
    public Guid TripId { get; set; }
    public string City { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public DateOnly ArrivalDate { get; set; }      // Varış tarihi
    public DateOnly DepartureDate { get; set; }     // Çıkış tarihi
    public int OrderIndex { get; set; }             // 1, 2, 3 (sıralı leg)
    public int NightCount { get; set; }             // Hesaplanan gece sayısı
    
    // Navigation
    public Trip? Trip { get; set; }
    public ICollection<TimelineEntry> TimelineEntries { get; set; } = new List<TimelineEntry>();
    public ICollection<ProviderFlight> LegFlights { get; set; } = new List<ProviderFlight>(); // Bu leg'in uçuşları
}
```

**Constraint'ler:**
- `order_index` 1-3 arası (CHECK: order_index BETWEEN 1 AND 3)
- `departure_date >= arrival_date` (CHECK)
- `night_count >= 1` (CHECK)
- Trip + OrderIndex unique (aynı trip'te aynı order_index olamaz)

**Mantık:**
```
Origin (kalkış şehri)
  └─► Destination 1  [Arrival: 10 Ağu] — [Departure: 13 Ağu]  (3 gece)
        └─► Destination 2  [Arrival: 13 Ağu] — [Departure: 17 Ağu]  (4 gece)
              └─► Destination 3  [Arrival: 17 Ağu] — [Departure: 20 Ağu]  (3 gece)
```

### 2.3 TimelineEntry — Yeni Entity (Stop'un yerine)

**Dosya:** `OmniFlow.Domain/Entities/TimelineEntry.cs` (yeni)

```csharp
public class TimelineEntry : AuditableBaseEntity
{
    public Guid TripId { get; set; }
    public Guid DestinationId { get; set; }        // Hangi destinasyondaki gün
    public int DayNumber { get; set; }              // Destinasyon içindeki gün numarası
    public double OrderIndex { get; set; }          // LexoRank sıralama
    
    // Entry type discriminator
    public TimelineEntryType EntryType { get; set; }
    
    // --- Place (DB mekan) ---
    public Guid? PlaceId { get; set; }              // DB'den seçilen mekan
    
    // --- Custom alanlar (tüm tipler için) ---
    public string? CustomName { get; set; }         // Özel isim
    public PlaceCategory? CustomCategory { get; set; }
    public string? CustomPhotoUrl { get; set; }
    public double? CustomLatitude { get; set; }
    public double? CustomLongitude { get; set; }
    public string? CustomDescription { get; set; }   // Açıklama / not
    
    // --- Zamanlama ---
    public TimeOnly? StartTime { get; set; }         // Başlangıç saati
    public int? DurationMinutes { get; set; }        // Süre (dakika)
    public bool IsLocked { get; set; }               // Sistem tarafından kilitli mi?
    public int? BufferMinutes { get; set; }          // Kilitli giriş öncesi/sonrası buffer
    
    // --- Uçuş özel alanları (EntryType = CustomFlight) ---
    public string? FlightFromAirport { get; set; }
    public string? FlightToAirport { get; set; }
    public string? FlightFromCity { get; set; }
    public string? FlightToCity { get; set; }
    public DateTime? FlightDepartureAt { get; set; }
    public DateTime? FlightArrivalAt { get; set; }
    public string? Airline { get; set; }
    public string? FlightNumber { get; set; }
    
    // --- Ulaşım özel alanları (EntryType = CustomTransport) ---
    public TransportMode? TransportType { get; set; } // Tren/Otobüs/Gemi/Araba
    public string? TransportFromStation { get; set; }
    public string? TransportToStation { get; set; }
    public string? TransportCompany { get; set; }     // Firma adı
    
    // --- Konaklama özel alanları (EntryType = CustomAccommodation) ---
    public DateTime? AccommodationCheckIn { get; set; }
    public DateTime? AccommodationCheckOut { get; set; }
    public string? AccommodationAddress { get; set; }
    
    // --- Fiyat ---
    public decimal Price { get; set; } = 0          // Bu kartın fiyatı (bütçe takibi için)
    public string CurrencyCode { get; set; } = "USD";
    
    // --- Provider reference (opsiyonel) ---
    public Guid? ProviderFlightId { get; set; }     // provider_flights.id referansı
    public Guid? ProviderHotelId { get; set; }       // provider_hotels.id referansı
    
    // --- Ek bilgiler ---
    public string? Notes { get; set; }
    public bool IsVisited { get; set; }
    public DateTime? VisitedAt { get; set; }
    public StopAddedBy AddedBy { get; set; } = StopAddedBy.User;
    public string? AiReasoning { get; set; }
    
    // Navigation
    public Trip? Trip { get; set; }
    public TripDestination? Destination { get; set; }
    public Place? Place { get; set; }
}
```

**Constraint'ler:**
- `entry_type_place_or_custom`: `entry_type = 'Place' AND place_id IS NOT NULL OR entry_type != 'Place'`
- Kilitli giriş mantığı:
  - `CustomFlight`: `is_locked = true`, `buffer_minutes = 120` (kalkıştan 2 saat öncesi kilitlenir)
  - `CustomTransport`: `is_locked = true`, `buffer_minutes = 30` (kalkıştan 30 dk öncesi kilitlenir)
  - `CustomAccommodation`: `is_locked = true`, `buffer_minutes = 0`
  - `CustomEvent`: `is_locked = true`, `buffer_minutes = 0`
  - `Place` (DB mekan): `is_locked = false`, `buffer_minutes = null`

### 2.4 Stop Entity — Durumu

**Karar gerekli:** Stop entity'sini tamamen kaldırıp TimelineEntry ile mi değiştirelim, yoksa Stop'u TimelineEntry'ye rename mi edelim?

**Öneri:** Yeni TimelineEntry entity'si oluştur, Stop'u bırak (migration ile soft geçiş). Eski data migration'da Stop → TimelineEntry'a taşınacak.

**Etkilenen dosyalar (Stop kaldırılırsa):**
- `OmniFlow.Domain/Entities/Stop.cs` — kaldır veya obsolete
- `OmniFlow.Infrastructure/Configurations/StopConfiguration.cs` — kaldır veya obsolete
- `OmniFlow.Application/Features/Stops/` — tüm handler'lar yeniden yazılacak
- `OmniFlow.Application/DTOs/Stop/` — tüm DTO'lar yeniden yazılacak
- `OmniFlow.WebApi/Controllers/v1/StopsController.cs` — kaldır veya TimelineController'a dönüşecek
- Tüm Stop repository ve interface'ler — kaldır

### 2.5 Place Entity — Değişiklik

**Dosya:** `OmniFlow.Domain/Entities/Place.cs`

**Eklenecek alanlar (PRD scoring için):**
| Alan | Tip | Açıklama |
|------|-----|----------|
| `GoogleTravelStyles` | List\<string\> | Google Places'den gelen travel style tag'leri (JSON array) |
| `PriceLevel` | int? | Zaten entity'de var ama EF mapping eksik — düzeltilecek |
| `ReviewCount` | int? | Zaten entity'de var ama EF mapping eksik — düzeltilecek |

**Eksik mapping'ler tamamlanacak:** PlaceConfiguration.cs'e şu property'ler eklenecek:
- `PhotoUrls` → text (JSON array olarak saklanacak)
- `PriceLevel` → integer
- `ReviewCount` → integer
- `Wikipedia`, `Wikidata`, `Wheelchair`, `Heritage`, `Fee`, `Image`, `Cuisine` → text

**Scoring için Place.Response genişletilecek:**
- `GoogleTravelStyles` alanı PlaceResponse DTO'suna eklenmeli
- Scoring servisi Place entity'sinin `Category`, `BudgetTiers`, `TravelStyles`, `GoogleTravelStyles`, `Rating`, `IsFree`, `EstimatedPrice` alanlarını kullanacak

### 2.6 Flight / Hotel — Provider İlişkisi

**Mevcut durum:** `Flight` ve `Hotel` entity'leri trip'e bağlı (seçilen/kaydedilen). `ProviderFlight` ve `ProviderHotel` bağımsız referans tabloları.

**Değişiklik:** TimelineEntry `ProviderFlightId` ve `ProviderHotelId` ile provider referansı tutabilir. Ayrıca `Flight` entity'si mevcut yapısını koruyabilir (trip'e bağlı seçilmiş uçuşlar için). 

**Öneri:** İki katmanlı yapı:
1. `ProviderFlight` / `ProviderHotel` — referans verisi, wizard'da gösterilecek seçenekler
2. TimelineEntry — `ProviderFlightId`/`ProviderHotelId` ile seçilen provider'ı işaret eder

Mevcut `Flight` ve `Hotel` entity'leri backward compatibility için korunabilir ama yeni sistemde TimelineEntry ile replace edilecekler.

### 2.7 Entity Özet Tablosu

| Entity | Durum | Açıklama |
|--------|-------|----------|
| Trip | **Değiştir** | Origin, TravelCompanion, Tempo, TransportPreference ekle; City/Country kaldır; TravelStyles → List |
| TripDestination | **Yeni** | Multi-destination leg'ler |
| TimelineEntry | **Yeni** | Stop'un yerine, 5 tipli kart sistemi |
| Stop | **Kaldır/Yeniden Adlandır** | TimelineEntry'ye migrate |
| Flight | **Korunabilir** | Backward compat veya TimelineEntry'ye migrate |
| Hotel | **Korunabilir** | Backward compat veya TimelineEntry'ye migrate |
| ProviderFlight | **Aynı** | Referans verisi, wizard'da kullanılacak |
| ProviderHotel | **Aynı** | Referans verisi, wizard'da kullanılacak |
| Place | **Güncelle** | GoogleTravelStyles ekle, eksik mapping'leri tamaml |
| TravelStyle | **Expand** | 5 → 11 değer |
| TravelCompanion | **Yeni enum** | |
| Tempo | **Yeni enum** | |
| TimelineEntryType | **Yeni enum** | |
| TransportPreference | **Yeni enum** | |
| Season | **Yeni enum** | |

---

## 3. Yeni Servisler

### 3.1 ScoringService — Places Scoring Motoru

**Dosyalar:**
- `OmniFlow.Application/Interfaces/IScoringService.cs` (yeni)
- `OmniFlow.Infrastructure/Services/ScoringService.cs` (yeni)

**Sorumluluk:**
- Verilen `TravelCompanion`, `List<TravelStyle>`, ve `PlaceCategory` → `group_score + style_score + google_match_bonus` hesapla
- Sonuç: her place için `final_score`

**Formül:**
```
final_score = group_score(category, companion) + sum(style_score(category, style)) + google_match_bonus
```

**group_score tablosu:** 27 kategori × 4 companion = 108 değer (PRD'de tanımlı)

**style_score tablosu:** 27 kategori × 11 style = 297 değer (PRD'de tanımlı)

**Önemli:** `CalculateStyleScoreAverage` — seçilen style'ların skorlarını topla, style sayısına böl (int truncate). 1 style seçilirse direkt skor döner.

**google_match_bonus:** Place'in `GoogleTravelStyles` listesi, kullanıcının seçtiği `TravelStyles` ile eşleşirse her eşleşme için +10 puan

**Implementasyon yaklaşımı:**
- Scoring tabloları `Dictionary<(PlaceCategory, TravelCompanion), int>` ve `Dictionary<(PlaceCategory, TravelStyle), int>` olarak kod içine hardcode edilecek
- Performans için `static readonly` dictionary'ler kullanılacak
- İleriye dönük: DB tablosuna taşınabilir ama MVP için hardcode yeterli

**Metot imzaları:**
```csharp
public interface IScoringService
{
    int CalculateGroupScore(PlaceCategory category, TravelCompanion companion);
    int CalculateStyleScore(PlaceCategory category, TravelStyle style);
    int CalculateStyleScoreAverage(PlaceCategory category, List<TravelStyle> styles);
    // Toplam değil ortalama: sum(scores) / count(styles) → balance için
    int CalculateGoogleMatchBonus(List<string> googleTravelStyles, List<TravelStyle> selectedStyles);
    int CalculateFinalScore(PlaceCategory category, TravelCompanion companion, List<TravelStyle> styles, List<string> googleTags);
    List<ScoredPlaceResponse> ScoreAndSortPlaces(List<Place> places, TravelCompanion companion, List<TravelStyle> styles);
}
```

### 3.2 BudgetCalculationService — Fiyat & Fallback Hesaplama

**Dosyalar:**
- `OmniFlow.Application/Interfaces/IBudgetCalculationService.cs` (yeni)
- `OmniFlow.Infrastructure/Services/BudgetCalculationService.cs` (yeni)

**Sorumluluklar:**

#### 3.2.1 Sezon Çarpanı
```csharp
decimal GetSeasonMultiplier(DateOnly date);
// Aralık/Ocak/Şubat → 1.2
// Mart/Nisan/Mayıs → 1.1
// Haziran/Temmuz/Ağustos → 1.5
// Eylül/Ekim/Kasım → 1.0
```

#### 3.2.2 Otel Segmentasyonu
```csharp
BudgetTier SegmentHotel(decimal pricePerNight, string city);
// Şehir bazlı percentile hesaplama:
// Economy → 0-20%, Standard → 20-90%, Premium → 90-100%
// DB'deki provider_hotels tablosundan şehrin fiyat range'ini hesapla
```

#### 3.2.3 Uçuş Fiyat Hesabı
```csharp
decimal CalculateFlightCost(Guid providerFlightId, int personCount, DateOnly travelDate);
// bilet_fiyati × PersonCount × sezon_carpani
```

#### 3.2.4 Otel Fiyat Hesabı
```csharp
decimal CalculateHotelCost(Guid providerHotelId, int personCount, int nightCount, DateOnly checkInDate);
// DB'de fiyat 1 gece / 1 kişi bazında saklanır
// gunluk_fiyat_per_kisi × personCount × nightCount × sezon_carpani
```

#### 3.2.5 Bütçe Fallback
```csharp
BudgetFallbackResult CalculateBudgetFallback(
    decimal manualBudget, 
    BudgetTier selectedTier, 
    List<TripDestination> destinations, 
    int personCount);
// Kullanıcının girdiği bütçe ile seçilen tier'ın maliyetini karşılaştır
// Premium karşılamıyorsa → Standard'a düşür
// Standard karşılamıyorsa → Economy'e düşür
// Her düşüşte bildirim mesajı oluştur
```

### 3.3 RecommendationService — Place Önerisi

**Dosyalar:**
- `OmniFlow.Application/Interfaces/IRecommendationService.cs` (yeni)
- `OmniFlow.Application/Features/Trips/Queries/GetRecommendedPlaces/GetRecommendedPlacesQuery.cs`
- `OmniFlow.Application/Features/Trips/Queries/GetRecommendedPlaces/GetRecommendedPlacesQueryHandler.cs`

**Sorumluluk:**
- Verilen destinasyon şehri + TravelCompanion + TravelStyles + BudgetTier + Tempo → scoring'e göre sıralı place listesi döndür
- Tempo'ya göre günlük mekan sayısı hesapla: Slow=3, Moderate=5, Fast=7
- final_score > 0 → ana liste, = 0 → nötr liste, < 0 → "Diğer Mekanlar" listesi
- Pagination + cursor desteği

**Endpoint:**
```
GET /api/v1/trips/{tripId}/recommend-places?destinationId={guid}
```

**Response:**
```json
{
  "recommended": [...],    // final_score > 0
  "neutral": [...],        // final_score = 0
  "other": [...],          // final_score < 0
  "budgetFallback": null | { "adjustedTier": "Standard", "message": "..." }
}
```

### 3.4 TimelineService — Timeline Yönetimi

**Dosyalar:**
- `OmniFlow.Application/Interfaces/ITimelineService.cs` (yeni)
- `OmniFlow.Infrastructure/Services/TimelineService.cs` (yeni)

**Sorumluluklar:**
- TimelineEntry ekleme/güncelleme/silme/reorder
- Custom entry ekleme (4 tip)
- is_locked + buffer hesaplama
- Kilitli blokların çakışma kontrolü (zaman çakışması varsa hata)
- Günlük mekan kapasitesi hesaplama (Tempo'ya göre)
- Provider seçimi sonrası otomatik TimelineEntry oluşturma (uçuş, otel)

---

## 4. Yeni Endpoint'ler

### 4.1 Wizard — Trip Oluşturma

```
POST /api/v1/trips/wizard/start
Body: { origin, originCountry }
Response: { tripId, step: 1 }

PUT /api/v1/trips/{tripId}/wizard/destinations
Body: [{ city, country, arrivalDate, departureDate, orderIndex }]
Response: { tripId, destinations, availableFlights }

PUT /api/v1/trips/{tripId}/wizard/details
Body: { personCount, travelCompanion, budgetTier, manualBudget, travelStyles: [], tempo, transportPreference }
Response: { tripId, budgetCheck: { adjustedTier, warnings } }

POST /api/v1/trips/{tripId}/wizard/complete
Response: { tripId, status: "Draft", recommendedPlaces }
```

**Alternatif:** Tek bir endpoint'te tüm wizard datasını biriktirip sonunda trip oluşturma:
```
POST /api/v1/trips
Body: {origin, originCountry, destinations: [...], personCount, travelCompanion, budgetTier, manualBudget, travelStyles: [], tempo, transportPreference}
```

**Öneri:** MVP için tek endpoint yaklaşımı daha basit. Frontend wizard'da adım adım state tutar, son adımda tek POST ile gönderir. İleriye dönük step-by-step API eklenebilir.

### 4.2 Place Recommendation

```
GET /api/v1/trips/{tripId}/recommend-places?destinationId={guid}
Response: List<ScoredPlaceResponse> (grouped by visibility)
```

### 4.3 Budget Summary

```
GET /api/v1/trips/{tripId}/budget-summary
Response: {
  totalFlightCost,
  totalHotelCost,
  totalActivityCost,
  totalCost,
  manualBudget,
  budgetTier,
  adjustedBudgetTier,
  seasonMultiplier,
  warnings: []
}
```

### 4.4 Timeline Entry CRUD

```
GET    /api/v1/trips/{tripId}/timeline                    # Tüm timeline'ı getir
GET    /api/v1/trips/{tripId}/timeline?destinationId={id}  # Belirli destinasyonun timeline'ı
POST   /api/v1/trips/{tripId}/timeline/entry               # Yeni entry ekle (5 tip)
PUT    /api/v1/trips/{tripId}/timeline/entry/{entryId}      # Entry güncelle
DELETE /api/v1/trips/{tripId}/timeline/entry/{entryId}      # Entry sil
PUT    /api/v1/trips/{tripId}/timeline/reorder              # Entry'leri yeniden sırala
```

### 4.5 Provider Data — Wizard İçin

```
GET /api/v1/providers/flights?fromCity={city}&toCity={city}&date={date}&personCount={n}
GET /api/v1/providers/hotels?city={city}&checkIn={date}&checkOut={date}&budgetTier={tier}&personCount={n}
GET /api/v1/providers/origin-cities    # Mevcut DB'deki kalkış şehirleri listesi
```

**Mevcut endpoint'ler:**
- `GET /api/v1/trips/{tripId}/flights` — Trip'e kaydedilmiş uçuşlar
- `GET /api/v1/trips/{tripId}/hotels` — Trip'e kaydedilmiş oteller

**Yeni endpoint'ler** provider verilerini sorgulamak için gerekli (wizard sırasında seçim yapabilmek).

### 4.5.1 Dönüş Uçuşu Mantığı

Wizard destinasyonlar kaydedildikten sonra backend otomatik olarak dönüş uçuşu seçeneklerini hazırlar:

```
fromCity = lastDestination.City
toCity   = trip.Origin
date     = lastDestination.DepartureDate
```

Bu bilgi `POST /api/v1/trips/{tripId}/wizard/complete` response'una `returnFlights: [...]` olarak eklenir. Kullanıcı:
- Listeden bir uçuş seçerse → `TimelineEntry (EntryType=Place, ProviderFlightId=...)` oluşturulur
- Kendi biletini girerse → `TimelineEntry (EntryType=CustomFlight)` oluşturulur
- Hiçbirini seçmezse → dönüş uçuşu timeline'a eklenmez

**Fiyat hesabına dahil edilmesi:** Seçilen dönüş uçuşu `BudgetSummary.totalFlightCost`'a eklenir.

### 4.6 Destinations CRUD

```
POST   /api/v1/trips/{tripId}/destinations           # Destinasyon ekle
PUT    /api/v1/trips/{tripId}/destinations/{destId}   # Destinasyon güncelle
DELETE /api/v1/trips/{tripId}/destinations/{destId}   # Destinasyon sil
GET    /api/v1/trips/{tripId}/destinations            # Tüm destinasyonları listele
```

---

## 5. Veritabanı Değişiklikleri

### 5.1 Yeni Tablolar

**trip_destinations:**
| Kolon | Tip | Constraint |
|-------|-----|-----------|
| id | UUID | PK |
| trip_id | UUID | FK → trips.id (Cascade) |
| city | text | NOT NULL |
| country | text | NOT NULL |
| arrival_date | date | NOT NULL |
| departure_date | date | NOT NULL |
| order_index | int | NOT NULL, CHECK: 1-3 |
| night_count | int | NOT NULL, CHECK ≥ 1 |
| created_at | timestamptz | |

**Indexes:**
- `idx_trip_destinations_trip_order` ON (trip_id, order_index) UNIQUE
- `idx_trip_destinations_city` ON (city) WHERE deleted_at IS NULL

**timeline_entries:**
| Kolon | Tip | Constraint |
|-------|-----|-----------|
| id | UUID | PK |
| trip_id | UUID | FK → trips.id (Cascade) |
| destination_id | UUID | FK → trip_destinations.id (Cascade) |
| day_number | int | NOT NULL |
| order_index | double precision | NOT NULL (LexoRank) |
| entry_type | text | NOT NULL, CHECK: IN ('Place','CustomFlight','CustomTransport','CustomAccommodation','CustomEvent') |
| place_id | UUID? | FK → places.id (SetNull) |
| custom_name | text? | |
| custom_category | text? | |
| custom_photo_url | text? | |
| custom_latitude | double? | |
| custom_longitude | double? | |
| custom_description | text? | |
| start_time | time? | |
| duration_minutes | int? | |
| is_locked | bool | DEFAULT false |
| buffer_minutes | int? | |
| flight_from_airport | text? | |
| flight_to_airport | text? | |
| flight_from_city | text? | |
| flight_to_city | text? | |
| flight_departure_at | timestamp? | |
| flight_arrival_at | timestamp? | |
| airline | text? | |
| flight_number | text? | |
| transport_type | text? | |
| transport_from_station | text? | |
| transport_to_station | text? | |
| transport_company | text? | |
| accommodation_check_in | timestamp? | |
| accommodation_check_out | timestamp? | |
| accommodation_address | text? | |
| price | decimal | DEFAULT 0 |
| currency_code | text | DEFAULT 'USD' |
| provider_flight_id | UUID? | FK → provider_flights.id (SetNull) |
| provider_hotel_id | UUID? | FK → provider_hotels.id (SetNull) |
| notes | text? | |
| is_visited | bool | DEFAULT false |
| visited_at | timestamptz? | |
| added_by | text | DEFAULT 'User' |
| ai_reasoning | text? | |
| created_at | timestamptz | |
| updated_at | timestamptz | |
| deleted_at | timestamptz? | Soft delete |

**CHECK Constraints:**
- `entry_type_place_requires_id`: `entry_type = 'Place' AND place_id IS NOT NULL OR entry_type != 'Place'`
- `custom_flight_requires_fields`: `entry_type != 'CustomFlight' OR (flight_from_airport IS NOT NULL AND flight_to_airport IS NOT NULL AND flight_departure_at IS NOT NULL AND flight_arrival_at IS NOT NULL)`
- `custom_transport_requires_type`: `entry_type != 'CustomTransport' OR transport_type IS NOT NULL`
- `custom_accommodation_requires_dates`: `entry_type != 'CustomAccommodation' OR (accommodation_check_in IS NOT NULL AND accommodation_check_out IS NOT NULL)`
- `custom_event_requires_time`: `entry_type != 'CustomEvent' OR (start_time IS NOT NULL AND duration_minutes IS NOT NULL)`
- `locked_entry_has_buffer`: `is_locked = true AND buffer_minutes IS NOT NULL OR is_locked = false`
- `valid_order_index`: `order_index > 0`

**Indexes:**
- `idx_timeline_entries_trip_dest_day_order` ON (trip_id, destination_id, day_number, order_index)
- `idx_timeline_entries_place` ON (place_id) WHERE place_id IS NOT NULL
- `idx_timeline_entries_provider_flight` ON (provider_flight_id) WHERE provider_flight_id IS NOT NULL
- `idx_timeline_entries_provider_hotel` ON (provider_hotel_id) WHERE provider_hotel_id IS NOT NULL

### 5.2 Değişen Tablolar

**trips tablosu değişiklikleri:**

| İşlem | Kolon | Tip | Açıklama |
|-------|-------|-----|----------|
| EKLE | origin | text | NOT NULL, kalkış şehri |
| EKLE | origin_country | text | NOT NULL, kalkış ülkesi |
| EKLE | travel_companion | text | NOT NULL, enum: Solo/Couple/Family/Friends |
| EKLE | tempo | text | NOT NULL, enum: Slow/Moderate/Fast |
| EKLE | transport_preference | text | NOT NULL, enum: Walking/PublicTransport/CarRental |
| EKLE | adjusted_budget_tier | text | NULL, fallback sonrası tier |
| EKLE | manual_budget | decimal | NULL, kullanıcının girdiği bütçe |
| DEĞİŞTİR | travel_style | text → text[] | Tek değer → array (max 3) |
| KALDIR | city | text | TripDestination'a taşındı |
| KALDIR | country | text | TripDestination'a taşındı |

**Not:** `start_date` ve `end_date` kolonları kesinlikle kaldırılmayacak — TripDestination'dan hesaplanacak ama filtreleme için kalacak (computed değer).

### 5.3 places tablosu güncellemeleri

| İşlem | Kolon | Tip | Açıklama |
|-------|-------|-----|----------|
| EKLE | google_travel_styles | text[] | Google Places'ten gelen travel style tag'leri |
| EKLE | photo_urls | text | JSON array |
| EKLE | price_level | int | Google Places 0-4 |
| EKLE | review_count | int | Google review sayısı |
| EKLE | wikipedia | text? | OSM |
| EKLE | wikidata | text? | OSM |
| EKLE | wheelchair | text? | OSM |
| EKLE | heritage | text? | OSM |
| EKLE | fee | text? | OSM |
| EKLE | image | text? | OSM |
| EKLE | cuisine | text? | OSM |

**Index eklenecek:**
- `idx_places_google_travel_styles_gin` ON google_travel_styles (GIN index)

---

## 6. Migration Stratejisi

### 6.1 Kademeli Migration Planı

**Migration 1: Yeni enum'lar ve Place güncellemesi**
- TravelStyle enum'ı expand et (5 → 11 değer)
- Yeni enum'lar: TravelCompanion, Tempo, TimelineEntryType, TransportPreference, Season
- Place tablosuna yeni kolonlar ekle (google_travel_styles, ve eksik mapping'ler)
- Place tablosundaki mevcut `travel_style` kolonunu text[]'e çevir

**Migration 2: Yeni tablolar ve Trip güncellemesi**
- `trip_destinations` tablosu oluştur
- `timeline_entries` tablosu oluştur
- `trips` tablosuna yeni kolonlar ekle (origin, origin_country, travel_companion, tempo, transport_preference, adjusted_budget_tier, manual_budget)
- `trips.travel_style` kolonunu text[]'e çevir (migration içinde mevcut verileri dönüştür)
- `trips` tablosundan city, country kolonlarını kaldır (nullable yap, sonradan drop)

**Migration 3: Data migration**
- Mevcut Trip'lerin City/Country verilerini TripDestination'a taşı (her trip için 1 destinasyon oluştur)
- Mevcut Stop'ları TimelineEntry'ya taşı (entry_type = 'Place' olarak)
- Mevcut Flight/Hote'ları ilişkilendir

**Migration 4: Cleanup**
- `stops` tablosunu drop et (artık kullanılmıyorsa)
- `trips.city` ve `trips.country` kolonlarını drop et
- Kullanılmayan eski kolonları temizle

### 6.2 Backward Compatibility

- Eski endpoint'ler (`/api/v1/trips/{id}/stops`) → yeni endpoint'e redirect veya deprecated bırakılabilir
- Fork edilen trip'lerin TripDestination'ları da deep copy edilmeli
- Explore endpoint'i mevcut haliyle çalışmaya devam etmeli (URL değişmemeli)

---

## 7. Uygulama Sırası

### Faz 1: Temel Altyapı (Enum'lar + Entity'ler + Migration)

1. **Yeni enum dosyaları oluştur:**
   - `TravelCompanion.cs`, `Tempo.cs`, `TimelineEntryType.cs`, `TransportPreference.cs`, `Season.cs`
2. **TravelStyle enum'ı expand et** (5 → 11 değer)
3. **TripDestination entity'si oluştur** + Configuration
4. **TimelineEntry entity'si oluştur** + Configuration
5. **Trip entity'sini güncelle** (yeni alanlar, List<TravelStyle>)
6. **Place entity'sini güncelle** (GoogleTravelStyles, eksik mapping'ler)
7. **PlaceConfiguration.cs güncelle** (9 eksik alan)
8. **TripConfiguration.cs güncelle** (yeni kolonlar, travel_style → array)
9. **ApplicationDbContext.cs güncelle** (yeni DbSet'ler)
10. **ExploreTripsQueryHandler güncelle** — Trip.City kaldırıldığı için şehir filtresi `TripDestination` tablosuna join yapacak şekilde düzeltilmeli
11. **Migration oluştur ve uygula**

### Faz 2: Servisler (Scoring + Budget)

1. **ScoringService** — group_score + style_score + google_match_bonus tabloları ve hesaplama
2. **BudgetCalculationService** — sezon çarpanı, otel segmentasyonu, fallback hesaplama
3. **RecommendationService** — scoring'e göre place sıralama ve öneri
4. **TimelineService** — timeline entry yönetimi, kilitleme, buffer hesaplama

### Faz 3: DTO'lar + CQRS Handler'lar

1. **Trip DTO'ları** — CreateTripRequest (wizard), UpdateTripRequest, TripResponse (destinations + timeline)
2. **TripDestination DTO'ları** — Create, Update, Response
3. **TimelineEntry DTO'ları** — Create (5 tip), Update, Response, Reorder
4. **Wizard DTO'ları** — StartTripWizardRequest, AddDestinationsRequest, SetTripDetailsRequest
5. **Budget DTO'ları** — BudgetSummaryResponse, BudgetFallbackResponse
6. **Recommendation DTO'ları** — ScoredPlaceResponse (final_score ile)
7. **Command/Query Handler'lar** — her DTO için

### Faz 4: Controller'lar

1. **TripsController güncelle** — wizard endpoint'leri, yeni trip response format
2. **TripDestinationsController** — CRUD
3. **TimelineController** — entry CRUD + reorder + custom entries
4. **ProvidersController** — uçuş/otel sorgulama (wizard için)
5. **BudgetController** — budget summary hesaplama (opsiyonel, trip içinde de hesaplanabilir)
6. **PlacesController güncelle** — scoring endpoint'i

### Faz 5: Test + Dokümantasyon

1. **Unit test'ler** — ScoringService, BudgetCalculationService, TimelineService, her handler
2. **Integration test'ler** — wizard akışı, timeline CRUD, budget fallback, scoring
3. **Swagger güncellemesi** — tüm yeni endpoint'ler
4. **Migration test** — mevcut verilerin doğru taşınması

---

## 8. Scoring Tablosu — Hardcode Implementasyonu

### 8.1 Group Score Dictionary

```csharp
// Anahtar: (PlaceCategory, TravelCompanion)
// Değer: int puan
private static readonly Dictionary<(PlaceCategory, TravelCompanion), int> GroupScoreTable = new()
{
    // Aquarium
    { (PlaceCategory.Aquarium, TravelCompanion.Solo), 0 },
    { (PlaceCategory.Aquarium, TravelCompanion.Couple), 10 },
    { (PlaceCategory.Aquarium, TravelCompanion.Family), 20 },
    { (PlaceCategory.Aquarium, TravelCompanion.Friends), 10 },
    // Attraction
    { (PlaceCategory.Attraction, TravelCompanion.Solo), 10 },
    { (PlaceCategory.Attraction, TravelCompanion.Couple), 10 },
    { (PlaceCategory.Attraction, TravelCompanion.Family), 10 },
    { (PlaceCategory.Attraction, TravelCompanion.Friends), 10 },
    // Bar
    { (PlaceCategory.Bar, TravelCompanion.Solo), 20 },
    { (PlaceCategory.Bar, TravelCompanion.Couple), 10 },
    { (PlaceCategory.Bar, TravelCompanion.Family), -20 },
    { (PlaceCategory.Bar, TravelCompanion.Friends), 20 },
    // Beach
    { (PlaceCategory.Beach, TravelCompanion.Solo), 20 },
    { (PlaceCategory.Beach, TravelCompanion.Couple), 20 },
    { (PlaceCategory.Beach, TravelCompanion.Family), 10 },
    { (PlaceCategory.Beach, TravelCompanion.Friends), 20 },
    // Bridge
    { (PlaceCategory.Bridge, TravelCompanion.Solo), 10 },
    { (PlaceCategory.Bridge, TravelCompanion.Couple), 10 },
    { (PlaceCategory.Bridge, TravelCompanion.Family), 0 },
    { (PlaceCategory.Bridge, TravelCompanion.Friends), 10 },
    // Cafe
    { (PlaceCategory.Cafe, TravelCompanion.Solo), 10 },
    { (PlaceCategory.Cafe, TravelCompanion.Couple), 20 },
    { (PlaceCategory.Cafe, TravelCompanion.Family), 10 },
    { (PlaceCategory.Cafe, TravelCompanion.Friends), 10 },
    // Castle
    { (PlaceCategory.Castle, TravelCompanion.Solo), 10 },
    { (PlaceCategory.Castle, TravelCompanion.Couple), 20 },
    { (PlaceCategory.Castle, TravelCompanion.Family), 10 },
    { (PlaceCategory.Castle, TravelCompanion.Friends), 10 },
    // Cave
    { (PlaceCategory.Cave, TravelCompanion.Solo), 10 },
    { (PlaceCategory.Cave, TravelCompanion.Couple), 10 },
    { (PlaceCategory.Cave, TravelCompanion.Family), -10 },
    { (PlaceCategory.Cave, TravelCompanion.Friends), 10 },
    // Church
    { (PlaceCategory.Church, TravelCompanion.Solo), 10 },
    { (PlaceCategory.Church, TravelCompanion.Couple), 10 },
    { (PlaceCategory.Church, TravelCompanion.Family), 10 },
    { (PlaceCategory.Church, TravelCompanion.Friends), 10 },
    // Forest
    { (PlaceCategory.Forest, TravelCompanion.Solo), 10 },
    { (PlaceCategory.Forest, TravelCompanion.Couple), 10 },
    { (PlaceCategory.Forest, TravelCompanion.Family), 0 },
    { (PlaceCategory.Forest, TravelCompanion.Friends), 10 },
    // Gallery
    { (PlaceCategory.Gallery, TravelCompanion.Solo), 20 },
    { (PlaceCategory.Gallery, TravelCompanion.Couple), 10 },
    { (PlaceCategory.Gallery, TravelCompanion.Family), 0 },
    { (PlaceCategory.Gallery, TravelCompanion.Friends), 0 },
    // Historical
    { (PlaceCategory.Historical, TravelCompanion.Solo), 20 },
    { (PlaceCategory.Historical, TravelCompanion.Couple), 10 },
    { (PlaceCategory.Historical, TravelCompanion.Family), 10 },
    { (PlaceCategory.Historical, TravelCompanion.Friends), 10 },
    // Information
    { (PlaceCategory.Information, TravelCompanion.Solo), 0 },
    { (PlaceCategory.Information, TravelCompanion.Couple), 0 },
    { (PlaceCategory.Information, TravelCompanion.Family), 0 },
    { (PlaceCategory.Information, TravelCompanion.Friends), 0 },
    // Mall
    { (PlaceCategory.Mall, TravelCompanion.Solo), 10 },
    { (PlaceCategory.Mall, TravelCompanion.Couple), 10 },
    { (PlaceCategory.Mall, TravelCompanion.Family), 10 },
    { (PlaceCategory.Mall, TravelCompanion.Friends), 20 },
    // Market
    { (PlaceCategory.Market, TravelCompanion.Solo), 20 },
    { (PlaceCategory.Market, TravelCompanion.Couple), 10 },
    { (PlaceCategory.Market, TravelCompanion.Family), 10 },
    { (PlaceCategory.Market, TravelCompanion.Friends), 10 },
    // Memorial
    { (PlaceCategory.Memorial, TravelCompanion.Solo), 10 },
    { (PlaceCategory.Memorial, TravelCompanion.Couple), 10 },
    { (PlaceCategory.Memorial, TravelCompanion.Family), 10 },
    { (PlaceCategory.Memorial, TravelCompanion.Friends), 0 },
    // Monument
    { (PlaceCategory.Monument, TravelCompanion.Solo), 10 },
    { (PlaceCategory.Monument, TravelCompanion.Couple), 10 },
    { (PlaceCategory.Monument, TravelCompanion.Family), 10 },
    { (PlaceCategory.Monument, TravelCompanion.Friends), 0 },
    // Museum
    { (PlaceCategory.Museum, TravelCompanion.Solo), 20 },
    { (PlaceCategory.Museum, TravelCompanion.Couple), 10 },
    { (PlaceCategory.Museum, TravelCompanion.Family), 10 },
    { (PlaceCategory.Museum, TravelCompanion.Friends), 0 },
    // Park
    { (PlaceCategory.Park, TravelCompanion.Solo), 10 },
    { (PlaceCategory.Park, TravelCompanion.Couple), 20 },
    { (PlaceCategory.Park, TravelCompanion.Family), 20 },
    { (PlaceCategory.Park, TravelCompanion.Friends), 10 },
    // Restaurant
    { (PlaceCategory.Restaurant, TravelCompanion.Solo), 10 },
    { (PlaceCategory.Restaurant, TravelCompanion.Couple), 20 },
    { (PlaceCategory.Restaurant, TravelCompanion.Family), 20 },
    { (PlaceCategory.Restaurant, TravelCompanion.Friends), 20 },
    // Shopping
    { (PlaceCategory.Shopping, TravelCompanion.Solo), 10 },
    { (PlaceCategory.Shopping, TravelCompanion.Couple), 20 },
    { (PlaceCategory.Shopping, TravelCompanion.Family), 10 },
    { (PlaceCategory.Shopping, TravelCompanion.Friends), 20 },
    // Supermarket
    { (PlaceCategory.Supermarket, TravelCompanion.Solo), 10 },
    { (PlaceCategory.Supermarket, TravelCompanion.Couple), 10 },
    { (PlaceCategory.Supermarket, TravelCompanion.Family), 10 },
    { (PlaceCategory.Supermarket, TravelCompanion.Friends), 10 },
    // Theater
    { (PlaceCategory.Theater, TravelCompanion.Solo), 10 },
    { (PlaceCategory.Theater, TravelCompanion.Couple), 20 },
    { (PlaceCategory.Theater, TravelCompanion.Family), 10 },
    { (PlaceCategory.Theater, TravelCompanion.Friends), 10 },
    // ThemePark
    { (PlaceCategory.ThemePark, TravelCompanion.Solo), 0 },
    { (PlaceCategory.ThemePark, TravelCompanion.Couple), 10 },
    { (PlaceCategory.ThemePark, TravelCompanion.Family), 20 },
    { (PlaceCategory.ThemePark, TravelCompanion.Friends), 20 },
    // Tower
    { (PlaceCategory.Tower, TravelCompanion.Solo), 10 },
    { (PlaceCategory.Tower, TravelCompanion.Couple), 20 },
    { (PlaceCategory.Tower, TravelCompanion.Family), 10 },
    { (PlaceCategory.Tower, TravelCompanion.Friends), 10 },
    // Viewpoint
    { (PlaceCategory.Viewpoint, TravelCompanion.Solo), 20 },
    { (PlaceCategory.Viewpoint, TravelCompanion.Couple), 20 },
    { (PlaceCategory.Viewpoint, TravelCompanion.Family), 10 },
    { (PlaceCategory.Viewpoint, TravelCompanion.Friends), 20 },
    // Zoo
    { (PlaceCategory.Zoo, TravelCompanion.Solo), 0 },
    { (PlaceCategory.Zoo, TravelCompanion.Couple), 10 },
    { (PlaceCategory.Zoo, TravelCompanion.Family), 20 },
    { (PlaceCategory.Zoo, TravelCompanion.Friends), 10 },
};
```

### 8.2 Style Score Dictionary

```csharp
// Anahtar: (PlaceCategory, TravelStyle)
// Değer: int puan (-20, -10, 0, +10, +20)
// PRD 4.6 tablosundan çeviri
private static readonly Dictionary<(PlaceCategory, TravelStyle), int> StyleScoreTable = new()
{
    // Aquarium
    { (PlaceCategory.Aquarium, TravelStyle.Romantic), 0 },
    { (PlaceCategory.Aquarium, TravelStyle.Cultural), 0 },
    { (PlaceCategory.Aquarium, TravelStyle.Adventure), 0 },
    { (PlaceCategory.Aquarium, TravelStyle.Nature), 10 },
    { (PlaceCategory.Aquarium, TravelStyle.Local), 0 },
    { (PlaceCategory.Aquarium, TravelStyle.Relax), 10 },
    { (PlaceCategory.Aquarium, TravelStyle.Shopping), 0 },
    { (PlaceCategory.Aquarium, TravelStyle.Gastronomy), 0 },
    { (PlaceCategory.Aquarium, TravelStyle.Influencer), 10 },
    { (PlaceCategory.Aquarium, TravelStyle.Nightlife), 0 },
    { (PlaceCategory.Aquarium, TravelStyle.Budget), 10 },
    // Attraction
    { (PlaceCategory.Attraction, TravelStyle.Romantic), 10 },
    { (PlaceCategory.Attraction, TravelStyle.Cultural), 10 },
    { (PlaceCategory.Attraction, TravelStyle.Adventure), 10 },
    { (PlaceCategory.Attraction, TravelStyle.Nature), 0 },
    { (PlaceCategory.Attraction, TravelStyle.Local), 10 },
    { (PlaceCategory.Attraction, TravelStyle.Relax), 0 },
    { (PlaceCategory.Attraction, TravelStyle.Shopping), 0 },
    { (PlaceCategory.Attraction, TravelStyle.Gastronomy), 0 },
    { (PlaceCategory.Attraction, TravelStyle.Influencer), 10 },
    { (PlaceCategory.Attraction, TravelStyle.Nightlife), 10 },
    { (PlaceCategory.Attraction, TravelStyle.Budget), 10 },
    // Bar
    { (PlaceCategory.Bar, TravelStyle.Romantic), -20 },
    { (PlaceCategory.Bar, TravelStyle.Cultural), 0 },
    { (PlaceCategory.Bar, TravelStyle.Adventure), 0 },
    { (PlaceCategory.Bar, TravelStyle.Nature), -20 },
    { (PlaceCategory.Bar, TravelStyle.Local), 10 },
    { (PlaceCategory.Bar, TravelStyle.Relax), 0 },
    { (PlaceCategory.Bar, TravelStyle.Shopping), 0 },
    { (PlaceCategory.Bar, TravelStyle.Gastronomy), 10 },
    { (PlaceCategory.Bar, TravelStyle.Influencer), 0 },
    { (PlaceCategory.Bar, TravelStyle.Nightlife), 20 },
    { (PlaceCategory.Bar, TravelStyle.Budget), -10 },
    // Beach
    { (PlaceCategory.Beach, TravelStyle.Romantic), 20 },
    { (PlaceCategory.Beach, TravelStyle.Cultural), 0 },
    { (PlaceCategory.Beach, TravelStyle.Adventure), 10 },
    { (PlaceCategory.Beach, TravelStyle.Nature), 20 },
    { (PlaceCategory.Beach, TravelStyle.Local), 0 },
    { (PlaceCategory.Beach, TravelStyle.Relax), 20 },
    { (PlaceCategory.Beach, TravelStyle.Shopping), 0 },
    { (PlaceCategory.Beach, TravelStyle.Gastronomy), 0 },
    { (PlaceCategory.Beach, TravelStyle.Influencer), 20 },
    { (PlaceCategory.Beach, TravelStyle.Nightlife), 0 },
    { (PlaceCategory.Beach, TravelStyle.Budget), 20 },
    // Bridge
    { (PlaceCategory.Bridge, TravelStyle.Romantic), 10 },
    { (PlaceCategory.Bridge, TravelStyle.Cultural), 10 },
    { (PlaceCategory.Bridge, TravelStyle.Adventure), 0 },
    { (PlaceCategory.Bridge, TravelStyle.Nature), 10 },
    { (PlaceCategory.Bridge, TravelStyle.Local), 0 },
    { (PlaceCategory.Bridge, TravelStyle.Relax), 10 },
    { (PlaceCategory.Bridge, TravelStyle.Shopping), 0 },
    { (PlaceCategory.Bridge, TravelStyle.Gastronomy), 0 },
    { (PlaceCategory.Bridge, TravelStyle.Influencer), 20 },
    { (PlaceCategory.Bridge, TravelStyle.Nightlife), 0 },
    { (PlaceCategory.Bridge, TravelStyle.Budget), 0 },
    // Cafe
    { (PlaceCategory.Cafe, TravelStyle.Romantic), 20 },
    { (PlaceCategory.Cafe, TravelStyle.Cultural), 10 },
    { (PlaceCategory.Cafe, TravelStyle.Adventure), 0 },
    { (PlaceCategory.Cafe, TravelStyle.Nature), 0 },
    { (PlaceCategory.Cafe, TravelStyle.Local), 20 },
    { (PlaceCategory.Cafe, TravelStyle.Relax), 20 },
    { (PlaceCategory.Cafe, TravelStyle.Shopping), 0 },
    { (PlaceCategory.Cafe, TravelStyle.Gastronomy), 20 },
    { (PlaceCategory.Cafe, TravelStyle.Influencer), 10 },
    { (PlaceCategory.Cafe, TravelStyle.Nightlife), 0 },
    { (PlaceCategory.Cafe, TravelStyle.Budget), 20 },
    // Castle
    { (PlaceCategory.Castle, TravelStyle.Romantic), 20 },
    { (PlaceCategory.Castle, TravelStyle.Cultural), 20 },
    { (PlaceCategory.Castle, TravelStyle.Adventure), 10 },
    { (PlaceCategory.Castle, TravelStyle.Nature), 0 },
    { (PlaceCategory.Castle, TravelStyle.Local), 0 },
    { (PlaceCategory.Castle, TravelStyle.Relax), 0 },
    { (PlaceCategory.Castle, TravelStyle.Shopping), 0 },
    { (PlaceCategory.Castle, TravelStyle.Gastronomy), 0 },
    { (PlaceCategory.Castle, TravelStyle.Influencer), 20 },
    { (PlaceCategory.Castle, TravelStyle.Nightlife), 0 },
    { (PlaceCategory.Castle, TravelStyle.Budget), 0 },
    // Cave
    { (PlaceCategory.Cave, TravelStyle.Romantic), 0 },
    { (PlaceCategory.Cave, TravelStyle.Cultural), 10 },
    { (PlaceCategory.Cave, TravelStyle.Adventure), 20 },
    { (PlaceCategory.Cave, TravelStyle.Nature), 20 },
    { (PlaceCategory.Cave, TravelStyle.Local), 0 },
    { (PlaceCategory.Cave, TravelStyle.Relax), 0 },
    { (PlaceCategory.Cave, TravelStyle.Shopping), 0 },
    { (PlaceCategory.Cave, TravelStyle.Gastronomy), 0 },
    { (PlaceCategory.Cave, TravelStyle.Influencer), 10 },
    { (PlaceCategory.Cave, TravelStyle.Nightlife), 0 },
    { (PlaceCategory.Cave, TravelStyle.Budget), 10 },
    // Church
    { (PlaceCategory.Church, TravelStyle.Romantic), 10 },
    { (PlaceCategory.Church, TravelStyle.Cultural), 20 },
    { (PlaceCategory.Church, TravelStyle.Adventure), 0 },
    { (PlaceCategory.Church, TravelStyle.Nature), 0 },
    { (PlaceCategory.Church, TravelStyle.Local), 10 },
    { (PlaceCategory.Church, TravelStyle.Relax), 0 },
    { (PlaceCategory.Church, TravelStyle.Shopping), 0 },
    { (PlaceCategory.Church, TravelStyle.Gastronomy), 0 },
    { (PlaceCategory.Church, TravelStyle.Influencer), 10 },
    { (PlaceCategory.Church, TravelStyle.Nightlife), -20 },
    { (PlaceCategory.Church, TravelStyle.Budget), 0 },
    // Forest
    { (PlaceCategory.Forest, TravelStyle.Romantic), 0 },
    { (PlaceCategory.Forest, TravelStyle.Cultural), 0 },
    { (PlaceCategory.Forest, TravelStyle.Adventure), 20 },
    { (PlaceCategory.Forest, TravelStyle.Nature), 20 },
    { (PlaceCategory.Forest, TravelStyle.Local), 0 },
    { (PlaceCategory.Forest, TravelStyle.Relax), 10 },
    { (PlaceCategory.Forest, TravelStyle.Shopping), -20 },
    { (PlaceCategory.Forest, TravelStyle.Gastronomy), 0 },
    { (PlaceCategory.Forest, TravelStyle.Influencer), 10 },
    { (PlaceCategory.Forest, TravelStyle.Nightlife), -20 },
    { (PlaceCategory.Forest, TravelStyle.Budget), 10 },
    // Gallery
    { (PlaceCategory.Gallery, TravelStyle.Romantic), 10 },
    { (PlaceCategory.Gallery, TravelStyle.Cultural), 20 },
    { (PlaceCategory.Gallery, TravelStyle.Adventure), 0 },
    { (PlaceCategory.Gallery, TravelStyle.Nature), 0 },
    { (PlaceCategory.Gallery, TravelStyle.Local), 0 },
    { (PlaceCategory.Gallery, TravelStyle.Relax), 10 },
    { (PlaceCategory.Gallery, TravelStyle.Shopping), 0 },
    { (PlaceCategory.Gallery, TravelStyle.Gastronomy), 0 },
    { (PlaceCategory.Gallery, TravelStyle.Influencer), 20 },
    { (PlaceCategory.Gallery, TravelStyle.Nightlife), 0 },
    { (PlaceCategory.Gallery, TravelStyle.Budget), 0 },
    // Historical
    { (PlaceCategory.Historical, TravelStyle.Romantic), 10 },
    { (PlaceCategory.Historical, TravelStyle.Cultural), 20 },
    { (PlaceCategory.Historical, TravelStyle.Adventure), 10 },
    { (PlaceCategory.Historical, TravelStyle.Nature), 0 },
    { (PlaceCategory.Historical, TravelStyle.Local), 10 },
    { (PlaceCategory.Historical, TravelStyle.Relax), 0 },
    { (PlaceCategory.Historical, TravelStyle.Shopping), -20 },
    { (PlaceCategory.Historical, TravelStyle.Gastronomy), 0 },
    { (PlaceCategory.Historical, TravelStyle.Influencer), 10 },
    { (PlaceCategory.Historical, TravelStyle.Nightlife), -20 },
    { (PlaceCategory.Historical, TravelStyle.Budget), 10 },
    // Information
    { (PlaceCategory.Information, TravelStyle.Romantic), 0 },
    { (PlaceCategory.Information, TravelStyle.Cultural), 10 },
    { (PlaceCategory.Information, TravelStyle.Adventure), 0 },
    { (PlaceCategory.Information, TravelStyle.Nature), 0 },
    { (PlaceCategory.Information, TravelStyle.Local), 10 },
    { (PlaceCategory.Information, TravelStyle.Relax), 0 },
    { (PlaceCategory.Information, TravelStyle.Shopping), 0 },
    { (PlaceCategory.Information, TravelStyle.Gastronomy), 0 },
    { (PlaceCategory.Information, TravelStyle.Influencer), 0 },
    { (PlaceCategory.Information, TravelStyle.Nightlife), 0 },
    { (PlaceCategory.Information, TravelStyle.Budget), 10 },
    // Mall
    { (PlaceCategory.Mall, TravelStyle.Romantic), -10 },
    { (PlaceCategory.Mall, TravelStyle.Cultural), -20 },
    { (PlaceCategory.Mall, TravelStyle.Adventure), -20 },
    { (PlaceCategory.Mall, TravelStyle.Nature), -20 },
    { (PlaceCategory.Mall, TravelStyle.Local), 0 },
    { (PlaceCategory.Mall, TravelStyle.Relax), 0 },
    { (PlaceCategory.Mall, TravelStyle.Shopping), 20 },
    { (PlaceCategory.Mall, TravelStyle.Gastronomy), 0 },
    { (PlaceCategory.Mall, TravelStyle.Influencer), 10 },
    { (PlaceCategory.Mall, TravelStyle.Nightlife), -20 },
    { (PlaceCategory.Mall, TravelStyle.Budget), -10 },
    // Market
    { (PlaceCategory.Market, TravelStyle.Romantic), 0 },
    { (PlaceCategory.Market, TravelStyle.Cultural), 10 },
    { (PlaceCategory.Market, TravelStyle.Adventure), 0 },
    { (PlaceCategory.Market, TravelStyle.Nature), 0 },
    { (PlaceCategory.Market, TravelStyle.Local), 20 },
    { (PlaceCategory.Market, TravelStyle.Relax), 0 },
    { (PlaceCategory.Market, TravelStyle.Shopping), 20 },
    { (PlaceCategory.Market, TravelStyle.Gastronomy), 20 },
    { (PlaceCategory.Market, TravelStyle.Influencer), 0 },
    { (PlaceCategory.Market, TravelStyle.Nightlife), 0 },
    { (PlaceCategory.Market, TravelStyle.Budget), 20 },
    // Memorial
    { (PlaceCategory.Memorial, TravelStyle.Romantic), 0 },
    { (PlaceCategory.Memorial, TravelStyle.Cultural), 20 },
    { (PlaceCategory.Memorial, TravelStyle.Adventure), 0 },
    { (PlaceCategory.Memorial, TravelStyle.Nature), 0 },
    { (PlaceCategory.Memorial, TravelStyle.Local), 10 },
    { (PlaceCategory.Memorial, TravelStyle.Relax), 0 },
    { (PlaceCategory.Memorial, TravelStyle.Shopping), -20 },
    { (PlaceCategory.Memorial, TravelStyle.Gastronomy), 0 },
    { (PlaceCategory.Memorial, TravelStyle.Influencer), 10 },
    { (PlaceCategory.Memorial, TravelStyle.Nightlife), -20 },
    { (PlaceCategory.Memorial, TravelStyle.Budget), 20 },
    // Monument
    { (PlaceCategory.Monument, TravelStyle.Romantic), 10 },
    { (PlaceCategory.Monument, TravelStyle.Cultural), 20 },
    { (PlaceCategory.Monument, TravelStyle.Adventure), 0 },
    { (PlaceCategory.Monument, TravelStyle.Nature), 0 },
    { (PlaceCategory.Monument, TravelStyle.Local), 10 },
    { (PlaceCategory.Monument, TravelStyle.Relax), 0 },
    { (PlaceCategory.Monument, TravelStyle.Shopping), -20 },
    { (PlaceCategory.Monument, TravelStyle.Gastronomy), 0 },
    { (PlaceCategory.Monument, TravelStyle.Influencer), 20 },
    { (PlaceCategory.Monument, TravelStyle.Nightlife), -20 },
    { (PlaceCategory.Monument, TravelStyle.Budget), 20 },
    // Museum
    { (PlaceCategory.Museum, TravelStyle.Romantic), 10 },
    { (PlaceCategory.Museum, TravelStyle.Cultural), 20 },
    { (PlaceCategory.Museum, TravelStyle.Adventure), 0 },
    { (PlaceCategory.Museum, TravelStyle.Nature), 0 },
    { (PlaceCategory.Museum, TravelStyle.Local), 10 },
    { (PlaceCategory.Museum, TravelStyle.Relax), 10 },
    { (PlaceCategory.Museum, TravelStyle.Shopping), -20 },
    { (PlaceCategory.Museum, TravelStyle.Gastronomy), 0 },
    { (PlaceCategory.Museum, TravelStyle.Influencer), 10 },
    { (PlaceCategory.Museum, TravelStyle.Nightlife), -20 },
    { (PlaceCategory.Museum, TravelStyle.Budget), 0 },
    // Park
    { (PlaceCategory.Park, TravelStyle.Romantic), 20 },
    { (PlaceCategory.Park, TravelStyle.Cultural), 0 },
    { (PlaceCategory.Park, TravelStyle.Adventure), 10 },
    { (PlaceCategory.Park, TravelStyle.Nature), 20 },
    { (PlaceCategory.Park, TravelStyle.Local), 10 },
    { (PlaceCategory.Park, TravelStyle.Relax), 20 },
    { (PlaceCategory.Park, TravelStyle.Shopping), -20 },
    { (PlaceCategory.Park, TravelStyle.Gastronomy), 0 },
    { (PlaceCategory.Park, TravelStyle.Influencer), 10 },
    { (PlaceCategory.Park, TravelStyle.Nightlife), -10 },
    { (PlaceCategory.Park, TravelStyle.Budget), 20 },
    // Restaurant
    { (PlaceCategory.Restaurant, TravelStyle.Romantic), 20 },
    { (PlaceCategory.Restaurant, TravelStyle.Cultural), 10 },
    { (PlaceCategory.Restaurant, TravelStyle.Adventure), 0 },
    { (PlaceCategory.Restaurant, TravelStyle.Nature), 0 },
    { (PlaceCategory.Restaurant, TravelStyle.Local), 20 },
    { (PlaceCategory.Restaurant, TravelStyle.Relax), 10 },
    { (PlaceCategory.Restaurant, TravelStyle.Shopping), 0 },
    { (PlaceCategory.Restaurant, TravelStyle.Gastronomy), 20 },
    { (PlaceCategory.Restaurant, TravelStyle.Influencer), 10 },
    { (PlaceCategory.Restaurant, TravelStyle.Nightlife), 10 },
    { (PlaceCategory.Restaurant, TravelStyle.Budget), 0 },
    // Shopping (place category)
    { (PlaceCategory.Shopping, TravelStyle.Romantic), 0 },
    { (PlaceCategory.Shopping, TravelStyle.Cultural), -20 },
    { (PlaceCategory.Shopping, TravelStyle.Adventure), -20 },
    { (PlaceCategory.Shopping, TravelStyle.Nature), -20 },
    { (PlaceCategory.Shopping, TravelStyle.Local), 10 },
    { (PlaceCategory.Shopping, TravelStyle.Relax), 0 },
    { (PlaceCategory.Shopping, TravelStyle.Shopping), 20 },
    { (PlaceCategory.Shopping, TravelStyle.Gastronomy), 0 },
    { (PlaceCategory.Shopping, TravelStyle.Influencer), 10 },
    { (PlaceCategory.Shopping, TravelStyle.Nightlife), 10 },
    { (PlaceCategory.Shopping, TravelStyle.Budget), 0 },
    // Supermarket
    { (PlaceCategory.Supermarket, TravelStyle.Romantic), -20 },
    { (PlaceCategory.Supermarket, TravelStyle.Cultural), -20 },
    { (PlaceCategory.Supermarket, TravelStyle.Adventure), -20 },
    { (PlaceCategory.Supermarket, TravelStyle.Nature), -20 },
    { (PlaceCategory.Supermarket, TravelStyle.Local), 20 },
    { (PlaceCategory.Supermarket, TravelStyle.Relax), 0 },
    { (PlaceCategory.Supermarket, TravelStyle.Shopping), 10 },
    { (PlaceCategory.Supermarket, TravelStyle.Gastronomy), 10 },
    { (PlaceCategory.Supermarket, TravelStyle.Influencer), -20 },
    { (PlaceCategory.Supermarket, TravelStyle.Nightlife), -20 },
    { (PlaceCategory.Supermarket, TravelStyle.Budget), 20 },
    // Theater
    { (PlaceCategory.Theater, TravelStyle.Romantic), 20 },
    { (PlaceCategory.Theater, TravelStyle.Cultural), 10 },
    { (PlaceCategory.Theater, TravelStyle.Adventure), 0 },
    { (PlaceCategory.Theater, TravelStyle.Nature), 0 },
    { (PlaceCategory.Theater, TravelStyle.Local), 10 },
    { (PlaceCategory.Theater, TravelStyle.Relax), 10 },
    { (PlaceCategory.Theater, TravelStyle.Shopping), 0 },
    { (PlaceCategory.Theater, TravelStyle.Gastronomy), 0 },
    { (PlaceCategory.Theater, TravelStyle.Influencer), 10 },
    { (PlaceCategory.Theater, TravelStyle.Nightlife), 20 },
    { (PlaceCategory.Theater, TravelStyle.Budget), 0 },
    // ThemePark
    { (PlaceCategory.ThemePark, TravelStyle.Romantic), -20 },
    { (PlaceCategory.ThemePark, TravelStyle.Cultural), -20 },
    { (PlaceCategory.ThemePark, TravelStyle.Adventure), 10 },
    { (PlaceCategory.ThemePark, TravelStyle.Nature), 0 },
    { (PlaceCategory.ThemePark, TravelStyle.Local), 0 },
    { (PlaceCategory.ThemePark, TravelStyle.Relax), 0 },
    { (PlaceCategory.ThemePark, TravelStyle.Shopping), 0 },
    { (PlaceCategory.ThemePark, TravelStyle.Gastronomy), 0 },
    { (PlaceCategory.ThemePark, TravelStyle.Influencer), 20 },
    { (PlaceCategory.ThemePark, TravelStyle.Nightlife), 0 },
    { (PlaceCategory.ThemePark, TravelStyle.Budget), -10 },
    // Tower
    { (PlaceCategory.Tower, TravelStyle.Romantic), 20 },
    { (PlaceCategory.Tower, TravelStyle.Cultural), 10 },
    { (PlaceCategory.Tower, TravelStyle.Adventure), 10 },
    { (PlaceCategory.Tower, TravelStyle.Nature), 10 },
    { (PlaceCategory.Tower, TravelStyle.Local), 0 },
    { (PlaceCategory.Tower, TravelStyle.Relax), 0 },
    { (PlaceCategory.Tower, TravelStyle.Shopping), 0 },
    { (PlaceCategory.Tower, TravelStyle.Gastronomy), 0 },
    { (PlaceCategory.Tower, TravelStyle.Influencer), 20 },
    { (PlaceCategory.Tower, TravelStyle.Nightlife), 0 },
    { (PlaceCategory.Tower, TravelStyle.Budget), 0 },
    // Viewpoint
    { (PlaceCategory.Viewpoint, TravelStyle.Romantic), 20 },
    { (PlaceCategory.Viewpoint, TravelStyle.Cultural), 10 },
    { (PlaceCategory.Viewpoint, TravelStyle.Adventure), 20 },
    { (PlaceCategory.Viewpoint, TravelStyle.Nature), 20 },
    { (PlaceCategory.Viewpoint, TravelStyle.Local), 0 },
    { (PlaceCategory.Viewpoint, TravelStyle.Relax), 20 },
    { (PlaceCategory.Viewpoint, TravelStyle.Shopping), -20 },
    { (PlaceCategory.Viewpoint, TravelStyle.Gastronomy), 0 },
    { (PlaceCategory.Viewpoint, TravelStyle.Influencer), 20 },
    { (PlaceCategory.Viewpoint, TravelStyle.Nightlife), 0 },
    { (PlaceCategory.Viewpoint, TravelStyle.Budget), 10 },
    // Zoo
    { (PlaceCategory.Zoo, TravelStyle.Romantic), -20 },
    { (PlaceCategory.Zoo, TravelStyle.Cultural), 0 },
    { (PlaceCategory.Zoo, TravelStyle.Adventure), 0 },
    { (PlaceCategory.Zoo, TravelStyle.Nature), 10 },
    { (PlaceCategory.Zoo, TravelStyle.Local), 0 },
    { (PlaceCategory.Zoo, TravelStyle.Relax), 10 },
    { (PlaceCategory.Zoo, TravelStyle.Shopping), -20 },
    { (PlaceCategory.Zoo, TravelStyle.Gastronomy), 0 },
    { (PlaceCategory.Zoo, TravelStyle.Influencer), 0 },
    { (PlaceCategory.Zoo, TravelStyle.Nightlife), -20 },
    { (PlaceCategory.Zoo, TravelStyle.Budget), 10 },
};
```

### 8.3 Google Tag Mapping

```csharp
// PRD 4.4 tablosundan çeviri
private static readonly Dictionary<TravelStyle, List<string>> GoogleTagMapping = new()
{
    { TravelStyle.Romantic,    new() { "Relaxation", "Beach", "City" } },
    { TravelStyle.Cultural,    new() { "Cultural", "Historical", "Educational", "Art" } },
    { TravelStyle.Adventure,   new() { "Adventure", "Hiking", "Nature" } },
    { TravelStyle.Nature,      new() { "Nature", "Beach", "Hiking" } },
    { TravelStyle.Local,       new() { "Food & Drink", "City", "Shopping" } },
    { TravelStyle.Relax,       new() { "Relaxation", "Beach", "Nature" } },
    { TravelStyle.Shopping,    new() { "Shopping" } },
    { TravelStyle.Gastronomy,  new() { "Food & Drink" } },
    { TravelStyle.Influencer,  new() { "Photography", "Beach", "City" } },
    { TravelStyle.Nightlife,   new() { "Nightlife", "Entertainment" } },
    { TravelStyle.Budget,      new() { "Food & Drink", "City", "Shopping" } },
};
```

### 8.4 Sezon Çarpanı

```csharp
private static readonly Dictionary<int, decimal> SeasonMultipliers = new()
{
    { 12, 1.2m }, { 1, 1.2m }, { 2, 1.2m },   // Kış
    { 3, 1.1m },  { 4, 1.1m }, { 5, 1.1m },   // İlkbahar
    { 6, 1.5m },  { 7, 1.5m }, { 8, 1.5m },   // Yaz
    { 9, 1.0m },  { 10, 1.0m }, { 11, 1.0m }, // Sonbahar
};
```

---

## 9. Karar Noktaları (Açık Sorular)

### 9.1 Trip.StartDate / EndDate

TripDestination'dan hesaplanabilir ama explore/filtreleme için trip seviyesinde tarih lazım mı?

**Öneri:** Kal, TripDestination'dan computed olarak güncellensin.

### 9.2 Stop vs TimelineEntry Migration

Mevcut Stop'ları tamamen kaldırıp TimelineEntry'ye mi migrate edelim, yoksa backward compatibility için ikisini paralel tutalım mı?

**Öneri:** Temiz geçiş — Stop kaldırılır, data migration ile TimelineEntry'ye taşınır. Eski StopsController deprecated yapılır.

### 9.3 Flight/Hotel Entity Durumu

Mevcut Flight ve Hotel entity'leri trip'e bağlı seçilen uçuş/otel kayıtları. TimelineEntry CustomFlight/CustomAccommodation ile aynı ama daha detaylı. İkisini de tutmak gerekiyor mu?

**Seçenekler:**
- A) Flight + Hotel entity'leri backward compatibility için korunur, yeni wizard ile TimelineEntry kullanılır
- B) Flight + Hotel kaldırılır, tümü TimelineEntry'ye taşınır
- C) Provider verisi için select edildiğinde hem Flight/Hotel kaydı hem TimelineEntry oluşturulur

**Öneri:** Seçenek C — Provider uçuş/otel seçildiğinde hem Flight/Hotel kaydı oluşturulur (detail için) hem de TimelineEntry oluşturulur (timeline gösterim için). Custom entry'ler sadece TimelineEntry.

### 9.4 Eski TravelStyle Veri Sorunu

Mevcut Trip'lerde `travel_style` kolonu tek string olarak saklanıyor ("Solo", "Family" vb.). Yeni sistemde `text[]` olacak. Migration'da:
- "Solo" → `["Budget"]` (en yakın mapping)
- "Family" → `["Family"]` (aynı kalır, ama yeni enum'da "Family" yok — bu sorun!)
- "Adventure" → `["Adventure"]` (aynı kalır)
- "Luxury" → `["Relax"]` (en yakın mapping)
- "Relax" → `["Relax"]` (aynı)

**PRD'de "Family" yok, "Family" yerine kullanıcının TravelCompanion seçmesinden geliyor.** Yani mevcut "Family" TravelStyle'lı trip'ler neye map edilecek?

**Öneri:** `TravelStyle.Family` → `TravelStyle.Local` veya `TravelStyle.Relax` olarak map'lenebilir. Veya migration'da default `[]` (boş array) bırakılıp kullanıcı güncellemesi beklenebilir.

### 9.5 Explore Endpoint Uyumluluğu

Mevcut Explore endpoint'i `city`, `budgetTier`, `travelStyle`, `tags` filtreleri ile çalışıyor. Yeni TravelStyle value'ları ile uyumlu mu olacak?

**Öneri:** Evet, TravelStyle artık List olduğu için `travelStyle` filtresi "herhangi biri eşleşirse" mantığıyla çalışmalı. Mevcut endpoint yapısı korunabilir.

### 9.6 8 Şehir ve Provider Data

PRD "DB'deki 8 şehir" diyor. ProviderFlight ve ProviderHotel'de kaç şehir var? Seeded data mı yoksa API'den çekilmiş mi?

**Bilinen:** Provider data API'den çekilmiş (Google Places + OSM enrichment ile). Seed data yok. Mevcut Places şehir sayısı bilinmiyor — DB'den sorgulanmalı.

---

## 10. Dosya Etki Matrisi

### Yeni Dosyalar
| Dosya | Açıklama |
|-------|----------|
| `Domain/Enums/TravelCompanion.cs` | Yeni enum |
| `Domain/Enums/Tempo.cs` | Yeni enum |
| `Domain/Enums/TimelineEntryType.cs` | Yeni enum |
| `Domain/Enums/TransportPreference.cs` | Yeni enum |
| `Domain/Enums/Season.cs` | Yeni enum |
| `Domain/Entities/TripDestination.cs` | Yeni entity |
| `Domain/Entities/TimelineEntry.cs` | Yeni entity |
| `Infrastructure/Configurations/TripDestinationConfiguration.cs` | Yeni EF config |
| `Infrastructure/Configurations/TimelineEntryConfiguration.cs` | Yeni EF config |
| `Infrastructure/Repositories/TripDestinationRepositoryAsync.cs` | Yeni repo |
| `Infrastructure/Repositories/TimelineEntryRepositoryAsync.cs` | Yeni repo |
| `Infrastructure/Repositories/ProviderFlightRepositoryAsync.cs` | Yeni repo |
| `Infrastructure/Repositories/ProviderHotelRepositoryAsync.cs` | Yeni repo |
| `Infrastructure/Services/ScoringService.cs` | Scoring motoru |
| `Infrastructure/Services/BudgetCalculationService.cs` | Bütçe hesaplama |
| `Infrastructure/Services/TimelineService.cs` | Timeline yönetimi |
| `Application/Interfaces/IScoringService.cs` | Scoring interface |
| `Application/Interfaces/IBudgetCalculationService.cs` | Budget interface |
| `Application/Interfaces/ITimelineService.cs` | Timeline interface |
| `Application/Interfaces/IRecommendationService.cs` | Recommendation interface |
| `Application/Interfaces/ITripDestinationRepositoryAsync.cs` | Dest repo interface |
| `Application/Interfaces/ITimelineEntryRepositoryAsync.cs` | Timeline repo interface |
| `Application/Interfaces/IProviderFlightRepositoryAsync.cs` | Provider flight repo |
| `Application/Interfaces/IProviderHotelRepositoryAsync.cs` | Provider hotel repo |
| `Application/DTOs/TripDestination/*` | Dest DTO'ları |
| `Application/DTOs/TimelineEntry/*` | Timeline DTO'ları |
| `Application/DTOs/Trip/CreateTripWizardRequest.cs` | Wizard request |
| `Application/DTOs/Trip/BudgetSummaryResponse.cs` | Budget response |
| `Application/DTOs/Trip/ScoredPlaceResponse.cs` | Scored place |
| `Application/Features/TripDestinations/*` | Dest CRUD CQRS |
| `Application/Features/TimelineEntries/*` | Timeline CRUD CQRS |
| `Application/Features/Trips/Queries/GetRecommendedPlaces/*` | Recommendation |
| `Application/Features/Trips/Queries/GetBudgetSummary/*` | Budget summary |
| `Application/Features/Providers/Queries/GetProviderFlights/*` | Flight listing + return flight |
| `Application/Features/Providers/Queries/GetProviderHotels/*` | Hotel listing |
| `Application/Features/Providers/Queries/GetOriginCities/*` | Kalkış şehirleri listesi |
| `WebApi/Controllers/v1/TripDestinationsController.cs` | Yeni controller |
| `WebApi/Controllers/v1/TimelineController.cs` | Yeni controller |
| `WebApi/Controllers/v1/ProvidersController.cs` | Yeni controller |

### Değişen Dosyalar
| Dosya | Değişiklik |
|-------|-----------|
| `Domain/Enums/TravelStyle.cs` | 5 → 11 değer |
| `Domain/Entities/Trip.cs` | Origin, TravelCompanion, Tempo, TransportPreference, ManualBudget, AdjustedBudgetTier ekle; City/Country kaldır; TravelStyle → List<TravelStyle> |
| `Domain/Entities/Place.cs` | GoogleTravelStyles ekle |
| `Infrastructure/Configurations/TripConfiguration.cs` | Yeni kolonlar, travel_style → array, city/country kaldır |
| `Infrastructure/Configurations/PlaceConfiguration.cs` | 9 eksik mapping ekle + GIN index google_travel_styles |
| `Infrastructure/Contexts/ApplicationDbContext.cs` | DbSet<TripDestination>, DbSet<TimelineEntry> ekle |
| `Application/DTOs/Trip/*` | Tüm trip DTO'ları güncelle |
| `Application/Features/Trips/Commands/CreateTrip*` | Wizard request ile uyumlu hale getir |
| `Application/Features/Trips/Queries/ExploreTrips*` | TravelStyle List filtreleme |
| `Application/Mappings/GeneralProfile.cs` | Yeni mapping'ler ekle |
| `WebApi/Controllers/v1/TripsController.cs` | Wizard endpoint'leri ekle |
| `WebApi/Controllers/v1/StopsController.cs` | Deprecated → TimelineController |
| `WebApi/Extensions/ServiceExtensions.cs` | Yeni servis DI |
| `WebApi/Program.cs` | Yeni servis registration |

### Kaldırılacak Dosyalar (Opsiyonel)
| Dosya | Açıklama |
|-------|----------|
| `Domain/Entities/Stop.cs` | TimelineEntry'ye Replace |
| `Infrastructure/Configurations/StopConfiguration.cs` | TimelineEntryConfiguration'ye replace |
| `Application/Features/Stops/*` | TimelineEntries feature'ına replace |
| `Application/DTOs/Stop/*` | TimelineEntry DTO'larına replace |
| `Domain/Enums/StopAddedBy.cs` | TimelineEntry ile uyumlu hale getir veya kaldır |

---

## 11. Tahmini İş Yükü

| Faz | Tahmini Süre | Açıklama |
|-----|-------------|----------|
| Faz 1: Enum'lar + Entity'ler + Migration | 6-8 saat | Yeni entity'ler, config'ler, migration, data migration |
| Faz 2: Servisler | 8-10 saat | Scoring (108+297 hardcoded değer), Budget, Timeline, Recommendation |
| Faz 3: DTO'lar + CQRS | 10-12 saat | Wizard, Timeline, Dest, Provider, Budget DTO ve handler'ları |
| Faz 4: Controller'lar | 4-5 saat | Yeni endpoint'ler, güncellenen endpoint'ler |
| Faz 5: Test + Cleanup | 6-8 saat | Unit test'ler, integration test'ler, migration test |
| **Toplam** | **34-43 saat** | |

---

## 12. Riskler ve Dikkat Edilmesi Gerekenler

1. **Data Migration Riski:** Mevcut Trip'lerin City/Country verisi TripDestination'a taşınırken data kaybı olabilir. Migration dikkatli yazılmalı.

2. **TravelStyle Migration:** 5 değerden 11 değere geçişte mevcut "Family" ve "Luxury" değerleri yeni enum'da yok. Data migration stratejisi netleşmeli.

3. **Stop → TimelineEntry Migration:** Mevcut Stop'ların entry_type'ı "Place" olarak taşınacak. Custom stop'lar doğru tip'e map edilmeli.

4. **Scoring Tabloları:** 108 group_score + 297 style_score değeri hardcode ediliyor. İleriye dönük DB tablosuna taşınabilir ama MVP için yeterli.

5. **Provider Data Yoğunluğu:** Mevcut DB'de kaç şehir, kaç Place, kaç ProviderFlight/ProviderHotel var? Seeded data yok, API'den çekilmiş. Test için yeni seed data gerekebilir.

6. **Performance:** Scoring hesaplama her place içp +in 2 dictionary looku google_match bonus. 100 place için negligible. 1000+ place için cache düşünülebilir.

7. **Backward Compatibility:** Mevcut StopsController, ExploreController, TripsController endpoint'leri kırılmamalı. Yeni sistem mevcut yapının yanına eklenmeli.