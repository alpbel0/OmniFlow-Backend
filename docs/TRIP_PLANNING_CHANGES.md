🚀 OmniFlow Trip Planning Modülü — Teknik Dönüşüm Raporu

  Bu doküman, projenin başlangıcından bugüne kadar geçirdiği evrimi, değişen veritabanı şemasını, akıllanan API uçlarını
  ve kurulan yeni iş mantığını (Business Logic) kapsamaktadır.

  ---

  1. 📂 Genel Bakış: Yolculuğun Başlangıcı vs. Bugün
  Geliştirmeye başlamadan önce sistemimiz basit bir "gezilecek yer listesi" tutan bir yapıdaydı. Bugün ise bütçeyi
  yöneten, mesafeleri hesaplayan ve zaman çakışmalarını engelleyen bir Seyahat Zekası'na dönüştü.

  ┌─────────────┬────────────────────────────────┬────────────────────────────────────────────────────────────┐
  │ Özellik     │ Eski Durum (Legacy)            │ Yeni Durum (Production-Ready)                              │
  ├─────────────┼────────────────────────────────┼────────────────────────────────────────────────────────────┤
  │ Rota Yapısı │ Tek şehir, tek ülke sınırı.    │ 10 durağa kadar sıralı Multi-destination (Çoklu şehir).    │
  │ Zaman       │ Basit bir liste (Stops).       │ Akıllı, kilitli ve buffer sürelerine sahip Timeline.       │
  │ Yönetimi    │                                │                                                            │
  │ Bütçe       │ Statik, sadece bir sayıdan     │ Dinamik Fallback (Otomatik segment düşürme) & Sezon        │
  │ Yönetimi    │ ibaret.                        │ Çarpanı.                                                   │
  │ Sıralama    │ Integer tabanlı hantal index.  │ LexoRank (Double precision) ile pürüzsüz sürükle-bırak.    │
  │ Zeka        │ Sadece popülerliğe göre öneri. │ Hub-Aware (Otele yakınlık) & Ulaşım Tercihli öneri motoru. │
  └─────────────┴────────────────────────────────┴────────────────────────────────────────────────────────────┘
  ---

  2. 🏗️ Mimari Değişim: "Timeline is King"
  Eski sistemde uçuşlar ve oteller, takvimden bağımsız (orphan) verilerdi. Yeni mimaride her şey bir TimelineEntry'dir.

   * Single Source of Truth: Uçuş, Konaklama, Taşımacılık ve Mekan ziyaretleri artık tek bir tabloda ve tek bir akışta
     toplanıyor.
   * Conflict Engine (Çakışma Motoru): Bir uçuş eklendiğinde sistem o uçuşun 2 saat öncesini ve uçuş süresini otomatik
     kilitler (IsLocked). Kullanıcı o saatte başka bir yere gidemez.
   * Buffer Mantığı: Uçuşlar için 120 dk, kara yolu taşımacılığı için 30 dk "hazırlık süresi" sistem tarafından otomatik
     hesaplanır.

  ---

  3. 📊 Veritabanı ve Şema Dönüşümü (Database Evolution)

  Silinenler (Legacy Cleanup)
   * stops tablosu: Tamamen silindi ve tarihe gömüldü.
   * trips.city ve trips.country: Geziden silindi, durak bazlı hale getirildi.

  Yeni Gelen Tablolar ve Kolonlar
   * trip_destinations: Gezinin duraklarını (Sıra, Varış/Çıkış Tarihi, Şehir) tutan tablo.
       * Kritik Kural: 10 durak sınırı veritabanı seviyesinde CHECK constraint ile zırhlandı.
   * timeline_entries: Sistemin kalbi. 5 farklı tipi (Place, CustomFlight, CustomTransport, CustomAccommodation,
     CustomEvent) destekleyen discriminator yapısı.
   * trips tablosu: Yeni kolonlarla zenginleşti: Origin, Tempo, TravelCompanion, TransportPreference, ManualBudget,
     AdjustedBudgetTier.

  ---

  4. 🧠 Akıllı Motorlar: Logic & Algorithms

  A. Bütçe Fallback Motoru
  Kullanıcı "Premium" gezi istedi ama bütçesi yetmiyorsa, sistem:
   1. Premium maliyetini hesaplar.
   2. Yetersizse Standard'a düşer.
   3. Hala yetersizse Economy'ye düşer ve kullanıcıya: "Bütçenize göre otelinizi Standard yaptık" diye bildirim döner.
   4. Sezon Çarpanı: Yaz aylarında fiyatları otomatik %50 artırarak gerçekçi bütçe sunar.

  B. Hub & Distance Engine (Mesafe Motoru)
   * Hub (Merkez): Kullanıcının oteli o günün merkezi sayılır.
   * Haversine Algoritması: Mekanlar ile otel arasındaki kuş uçuşu mesafe hesaplanır.
   * Transport Penalty: Kullanıcı "Yürüyerek" gezeceğim dediyse, 2 km'den uzak yerlerin skoru otomatik düşürülür ve
     "Diğer" kategorisine itilir.

  ---

  5. 🔌 Endpoint ve API Değişimleri (API Contract)

  Frontend'in en çok kullanacağı uçlar artık şunlar:

   1. POST /trips/wizard: 8 adımlık veriyi alır, bütçeyi hesaplar, rotayı çizer ve tek seferde zengin bir yanıt döner.
   2. GET /trips/{id}/timeline: Tüm uçuş, otel ve mekanları sıralı ve gruplanabilir şekilde döner.
   3. PUT /timeline/reorder: Sadece "Şunun arkasına koy" diyerek sürükle-bırak işlemini backend'e yaptırır.
   4. GET /trips/{id}/budget-summary: Gezi üzerinde yapılan her değişiklikten sonra (yeni uçuş seçme vb.) güncel bütçe
      tablosunu (Toplam Uçuş, Toplam Konaklama, Kalan Bütçe) verir.
   5. GET /trips/{id}/recommend-places: Otele olan uzaklığa ve ulaşıma göre filtrelenmiş akıllı yer önerileri sunar.

  ---

  6. 🛡️ Güvenlik ve İstikrar (Robustness)
   * Ownership Check: Her operasyon (Update, Delete, Fork) kullanıcının o geziye yetkisi olup olmadığını denetler.
   * Draft-Only Rule: Yayınlanmış (Published) geziler üzerinde değişiklik yapılamaz, veri tutarlılığı korunur.
   * Transactional Integrity: 10 duraklı bir gezi kaydedilirken en ufak bir hata olursa, veritabanı her şeyi geri alır
     (Rollback). Yarım kalmış veri oluşamaz.
   * Flaky Test Prevention: Testler, paylaşılan hafıza (In-Memory DB) ve rastgelelikten kaynaklanan hatalara karşı (000_
     prefix ve seeder temizliği ile) zırhlanmıştır.

  ---

  🏁 Sonuç: Neyi Başardık?
  Kanka, bugün elindeki kod; ölçeklenebilir, güvenli, akıllı ve test edilmiş bir sistemdir. Frontend tarafına
  geçtiğinde, backend'in sana sadece veri değil, bir "deneyim" sunduğunu göreceksin.
