
# OmniFlow — Trip Planning Modülü PRD

---

## 1. Genel Bakış

Bu döküman, OmniFlow platformunun trip planlama modülünün backend mantığını, onboarding akışını, scoring sistemini ve veri modelini kapsar. Canlı API entegrasyonu yoktur; tüm veriler mevcut DB'den servis edilir.

---

## 2. Onboarding Akışı (Wizard)

Kullanıcı trip planlamaya başladığında aşağıdaki sorular sırayla sorulur.

### Adım 1 — Kalkış Noktası

- Kullanıcı, DB'deki **8 şehirden** birini seçer (origin).
- Bu şehir, ilk uçuşun kalkış noktasıdır.

### Adım 2 — Destinasyonlar

- Kullanıcı **1 ila 3 şehir** seçer (sıralı).
- Her şehir için ayrı tarih aralığı girilir.

**Çoklu destinasyon mantığı:**

```
Ev (Origin)
  └─► 1. Şehir  [Varış: 10 Ağu] — [Çıkış: 13 Ağu]
        └─► 2. Şehir  [Varış: 13 Ağu] — [Çıkış: 17 Ağu]
              └─► 3. Şehir  [Varış: 17 Ağu] — [Çıkış: 20 Ağu]
                    └─► Ev (Origin) [Dönüş: 20 Ağu]  ← Dönüş uçuşu
```

- Şehirler arasındaki ulaşım **yalnızca uçak** (şu aşamada).
- Her leg için DB'deki uçuş verileri listelenir.
- **Dönüş uçuşu:** Son destinasyonun çıkış tarihinde, son şehirden Origin'e dönen uçuşlar otomatik olarak listelenir. Bu uçuş seçimi zorunlu değildir (kullanıcı Custom Flight ile kendi biletini girebilir).

### Adım 3 — Kaç Kişi?

- Sayısal giriş (PersonCount).
- Uçuş fiyatı: `bilet_fiyatı × PersonCount`
- Otel fiyatı: oda tipi seçimine göre hesaplanır.

### Adım 4 — Kimlerle?

| Seçenek | Değer |
|---|---|
| Solo | `solo` |
| Çift | `couple` |
| Aile | `family` |
| Arkadaş grubu | `friends` |

> Bu seçim, places scoring'inde **group_score** olarak kullanılır.

### Adım 5 — Bütçe Tercihi

**Tier seçimi:**

| Tier | Değer |
|---|---|
| Economy | `economy` |
| Standard | `standard` |
| Premium | `premium` |

**Manuel bütçe girişi:**
- Kullanıcı tahmini toplam bütçesini girer (USD veya yerel para birimi).
- Bu rakam, fallback sistemi ve uyarılar için kullanılır.

#### Fallback Mantığı (Bütçe Çelişkisi)

Kullanıcı `Premium` seçti ama girdiği bütçe, seçilen destinasyonların premium otel + uçuş toplamını karşılamıyorsa:

1. Sistem önce `Standard`'a düşürür.
2. Hâlâ karşılamıyorsa `Economy`'e düşürür.
3. Her düşüşte kullanıcıya küçük bir **bildirim** gösterilir:
   > _"Bütçenize göre otel tercihiniz Standard olarak güncellendi."_

### Adım 6 — Vibe (Travel Style)

Kullanıcı aşağıdakilerden **maksimum 3 tane** seçer:

| # | Style |
|---|---|
| 1 | Romantik |
| 2 | Kültürel |
| 3 | Macera |
| 4 | Doğa |
| 5 | Yerel gibi yaşam |
| 6 | Relax |
| 7 | Alışveriş |
| 8 | Gastronomi |
| 9 | Influencer / Fotoğraf |
| 10 | Gece hayatı |
| 11 | Budget friendly |

### Adım 7 — Tempo

Günde kaç yer gezmek istersin?

| Seçenek | Değer | Günlük slot |
|---|---|---|
| Yavaş | `slow` | 2–3 mekan |
| Orta | `moderate` | 4–5 mekan |
| Yoğun | `fast` | 6+ mekan |

### Adım 8 — Ulaşım Tercihi (Şehir İçi)

| Seçenek |
|---|
| Yürüyerek |
| Toplu taşıma |
| Araç kiralama |

> Şu aşamada bu tercih, places'lar arası mesafe hesabında yardımcı bilgi olarak saklanır; aktif filtreleme yapmaz.

---

## 3. Fiyat Hesaplama

### 3.1 Sezon Çarpanı

DB'de 6 aylık gerçek fiyat verisi bulunur. Kullanıcı bu aralık dışında bir tarih seçerse aşağıdaki çarpanlar uygulanır:

| Aylar | Sezon | Çarpan |
|---|---|---|
| 12, 1, 2 | Kış | × 1.2 |
| 3, 4, 5 | İlkbahar | × 1.1 |
| 6, 7, 8 | Yaz | × 1.5 |
| 9, 10, 11 | Sonbahar | × 1.0 |

Çarpan hem **otel** hem **uçuş** fiyatlarına uygulanır.

### 3.2 Otel Segmentasyonu

Her şehirdeki oteller, o şehrin kendi içinde fiyata göre küçükten büyüğe sıralanır ve percentile'a göre segmente edilir:

| Segment | Percentile |
|---|---|
| Economy | 0 – 20% |
| Standard | 20 – 90% |
| Premium | 90 – 100% |

> Segmentasyon şehir bazlıdır. Amsterdam'ın economy oteli Paris'in premium oteliyle aynı fiyatta olabilir.

**Otel fiyat hesabı:**

> DB'de fiyatlar **1 gece / 1 kişi** bazında saklanır.

```
toplam_otel = gunluk_fiyat_per_kisi × PersonCount × gece_sayisi × sezon_carpani
```

### 3.3 Uçuş Fiyat Hesabı

```
toplam_ucus = bilet_fiyati × PersonCount × sezon_carpani
```

Şu aşamada DB'deki uçuşlar statiktir; kullanıcının seçtiği tarihte mevcut uçuşlar listelenir.

### 3.4 Canlı Bütçe Takibi

Frontend'de ekranın köşesinde sürekli güncellenen bir sayaç olur:

```
Toplam = otel_toplam + ucus_toplam + (seçilen custom entry fiyatları)
```

Kullanıcının girdiği bütçeyi aşarsa uyarı rengi görünür.

---

## 4. Places Scoring Sistemi

### 4.1 Formül

```
final_score = group_score + style_score_avg + google_match_bonus
```

- **group_score:** Kimlerle sorusuna göre (Solo/Çift/Aile/Arkadaş)
- **style_score_avg:** Seçilen travel style'ların skorlarının **ortalaması** (toplam değil). Max 3 style seçilebildiğinden toplam almak skoru dengesiz yükseltir; ortalama alınır.
  ```
  style_score_avg = sum(style_score(category, style) for each selected style) / count(selected styles)
  ```
- **google_match_bonus:** Place'in Google'dan gelen `travelStyle` tag'i, kullanıcının seçtiği style ile eşleşirse **+10** puan. Eşleşmezse **0** (eksi almaz).

### 4.2 Puan Değerleri

| Enum | Puan |
|---|---|
| VERY_BAD | -20 |
| BAD | -10 |
| NEUTRAL | 0 |
| GOOD | +10 |
| GREAT | +20 |

### 4.3 Sıralama ve Görünürlük

- `final_score > 0` → normal sıralamada göster
- `final_score = 0` → nötr, listenin altına düşer
- `final_score < 0` → "Diğer Mekanlar" bölümüne gizlenir, kullanıcı isterse açabilir

### 4.4 Google Tag Mapping

| Kullanıcı Style | Google Tag Eşleşmesi |
|---|---|
| Romantik | Relaxation, Beach, City |
| Kültürel | Cultural, Historical, Educational, Art |
| Macera | Adventure, Hiking, Nature |
| Doğa | Nature, Beach, Hiking |
| Yerel gibi yaşam | Food & Drink, City, Shopping |
| Relax | Relaxation, Beach, Nature |
| Alışveriş | Shopping |
| Gastronomi | Food & Drink |
| Influencer / Fotoğraf | Photography, Beach, City |
| Gece hayatı | Nightlife, Entertainment |
| Budget friendly | Food & Drink, City, Shopping |

### 4.5 Group Score Tablosu

| Kategori | Solo | Çift | Aile | Arkadaş |
|---|---|---|---|---|
| Aquarium | 0 | +10 | +20 | +10 |
| Attraction | +10 | +10 | +10 | +10 |
| Bar | +20 | +10 | -20 | +20 |
| Beach | +20 | +20 | +10 | +20 |
| Bridge | +10 | +10 | 0 | +10 |
| Cafe | +10 | +20 | +10 | +10 |
| Castle | +10 | +20 | +10 | +10 |
| Cave | +10 | +10 | -10 | +10 |
| Church | +10 | +10 | +10 | +10 |
| Forest | +10 | +10 | 0 | +10 |
| Gallery | +20 | +10 | 0 | 0 |
| Historical | +20 | +10 | +10 | +10 |
| Information | 0 | 0 | 0 | 0 |
| Mall | +10 | +10 | +10 | +20 |
| Market | +20 | +10 | +10 | +10 |
| Memorial | +10 | +10 | +10 | 0 |
| Monument | +10 | +10 | +10 | 0 |
| Museum | +20 | +10 | +10 | 0 |
| Park | +10 | +20 | +20 | +10 |
| Restaurant | +10 | +20 | +20 | +20 |
| Shopping | +10 | +20 | +10 | +20 |
| Supermarket | +10 | +10 | +10 | +10 |
| Theater | +10 | +20 | +10 | +10 |
| ThemePark | 0 | +10 | +20 | +20 |
| Tower | +10 | +20 | +10 | +10 |
| Viewpoint | +20 | +20 | +10 | +20 |
| Zoo | 0 | +10 | +20 | +10 |

### 4.6 Style Score Tablosu

Kategori,Romantik,Kültürel,Macera,Doğa,Yerel,Relax,Alışveriş,Gastronomi,Influencer,Gece hayatı,Budget
Aquarium,0,0,0,+10,0,+10,0,0,+10,0,+10
Attraction,+10,+10,+10,0,+10,0,0,0,+10,+10,+10
Bar,-20,0,0,-20,+10,0,0,+10,0,+20,-10
Beach,+20,0,+10,+20,0,+20,0,0,+20,0,+20
Bridge,+10,+10,0,+10,0,+10,0,0,+20,0,0
Cafe,+20,+10,0,0,+20,+20,0,+20,+10,0,+20
Castle,+20,+20,+10,0,0,0,0,0,+20,0,0
Cave,0,+10,+20,+20,0,0,0,0,+10,0,+10
Church,+10,+20,0,0,+10,0,0,0,+10,-20,0
Forest,0,0,+20,+20,0,+10,-20,0,+10,-20,+10
Gallery,+10,+20,0,0,0,+10,0,0,+20,0,0
Historical,+10,+20,+10,0,+10,0,-20,0,+10,-20,+10
Information,0,+10,0,0,+10,0,0,0,0,0,+10
Mall,-10,-20,-20,-20,0,0,+20,0,+10,-20,-10
Market,0,+10,0,0,+20,0,+20,+20,0,0,+20
Memorial,0,+20,0,0,+10,0,-20,0,+10,-20,+20
Monument,+10,+20,0,0,+10,0,-20,0,+20,-20,+20
Museum,+10,+20,0,0,+10,+10,-20,0,+10,-20,0
Park,+20,0,+10,+20,+10,+20,-20,0,+10,-10,+20
Restaurant,+20,+10,0,0,+20,+10,0,+20,+10,+10,0
Shopping,0,-20,-20,-20,+10,0,+20,0,+10,+10,0
Supermarket,-20,-20,-20,-20,+20,0,+10,+10,-20,-20,+20
Theater,+20,+10,0,0,+10,+10,0,0,+10,+20,0
ThemePark,-20,-20,+10,0,0,0,0,0,+20,0,-10
Tower,+20,+10,+10,+10,0,0,0,0,+20,0,0
Viewpoint,+20,+10,+20,+20,0,+20,-20,0,+20,0,+10
Zoo,-20,0,0,+10,0,+10,-20,0,0,-20,+10


---

## 5. Custom Entry Kartları

Kullanıcı DB dışındaki kendi verilerini 4 farklı kart tipiyle ekleyebilir.

### 5.1 Kendi Uçuşunu Ekle (Flight)

Kullanıcının bileti önceden aldığı durumlar için.

| Alan | Zorunlu | Açıklama |
|---|---|---|
| Kalkış & Varış | ✅ | Örn: IST → FCO |
| Kalkış tarihi & saati | ✅ | Timeline başlangıcı |
| Varış tarihi & saati | ✅ | Timeline bitişi |
| Havayolu & Uçuş No | ❌ | UI'da logo için |
| Fiyat | ❌ | Bütçe hesabına dahil edilir |

**Algoritma etkisi:** Uçuş süresi + öncesine **2 saat buffer** eklenir. Bu blok `is_locked: true` olarak işaretlenir; sistem buraya mekan atayamaz.

### 5.2 Kendi Ulaşımını Ekle (Train / Bus / Ferry)

Avrupa içi şehirlerarası geçişler için.

| Alan | Zorunlu | Açıklama |
|---|---|---|
| Araç tipi | ✅ | Tren / Otobüs / Gemi / Özel Araç |
| Kalkış & Varış noktası | ✅ | Örn: Roma Termini → Floransa |
| Kalkış & Varış saati | ✅ | Seyahat süresini kilitler |
| Firma | ❌ | Örn: Trenitalia, Flixbus |
| Fiyat | ❌ | Bütçe hesabına dahil edilir |

**Algoritma etkisi:** Uçuştan farklı olarak öncesine sadece **30 dakika buffer** bırakılır. Blok `is_locked: true`.

### 5.3 Kendi Otelini Ekle (Accommodation)

| Alan | Zorunlu | Açıklama |
|---|---|---|
| Otel / Airbnb adı | ✅ | Serbest metin |
| Check-in tarihi | ✅ | Varsayılan 14:00 |
| Check-out tarihi | ✅ | Varsayılan 12:00 |
| Konum / Adres | ✅ | Google Maps linki → lat/lng parse edilir |
| Toplam fiyat | ❌ | Bütçe hesabına dahil edilir |

**Algoritma etkisi:** Otelin koordinatı günün **hub noktası** olarak ayarlanır. Diğer mekan önerileri bu noktaya göre mesafe hesabıyla sıralanır.

### 5.4 Kendi Mekanını / Etkinliğini Ekle (Custom Place/Event)

| Alan | Zorunlu | Açıklama |
|---|---|---|
| Mekan / Etkinlik adı | ✅ | Örn: Coldplay Konseri |
| Tarih & Başlangıç saati | ✅ | Timeline'a çakılır |
| Bitiş saati | ✅ | Veya tahmini süre |
| Kategori | ✅ | 27 kategori + "Diğer" seçeneği |

**Algoritma etkisi:** Timeline'da tam o saate `is_locked: true` event kartı eklenir. Scoring algoritması diğer önerileri bu kartın önüne ve arkasına dizer.

---

## 6. Timeline Kilitleme Mantığı

```
is_locked: true  →  sistem buraya mekan atayamaz
is_locked: false →  kullanıcı ve sistem serbestçe mekan ekleyebilir
```

| Blok Tipi | Buffer | is_locked |
|---|---|---|
| Uçuş | öncesinde 2 saat | true |
| Tren / Otobüs / Gemi | öncesinde 30 dk | true |
| Custom Event | Yok | true |
| Hotel check-in/out | Yok | true |
| System önerisi | — | false |
| Kullanıcı eklediği place | — | false |

---



---

## 7. Özet Akış

```
[Onboarding Wizard]
  ↓
Origin → Destinasyonlar & Tarihler → Kişi Sayısı
  → Kimlerle → Budget Tier + Manuel Bütçe
  → Vibe (max 3) → Tempo → Ulaşım Tercihi
  ↓
[Fallback Kontrolü]
  Bütçe < Premium maliyet → Standard'a düşür + bildirim
  Bütçe < Standard maliyet → Economy'ye düşür + bildirim
  ↓
[Sonuç Ekranı]
  Hotel kartları (budget segment'e göre sıralı)
  Uçuş kartları (gidiş + dönüş)
  Places kartları (scoring'e göre sıralı)
  ↓
[Timeline]
  Kullanıcı kartları günlere ekler
  Custom entry ekleyebilir (4 tip)
  is_locked bloklar sistem tarafından otomatik kilitlenir
  ↓
[Canlı Bütçe Takibi]
  Her ekleme/çıkarmada toplam güncellenir
  ↓
[Trip Kaydet]
  DB'ye yazılır (Trips + alt tablolar)
```