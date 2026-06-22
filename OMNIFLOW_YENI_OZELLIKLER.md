# OmniFlow - Mobile Icin Planlanan Yeni Ozellikler

> Bu dokuman, mevcut backend audit'lerinin (`BACKEND_FEATURE_LIST`, `ROADMAP_IMPLEMENTATION_AUDIT`) uzerine, mobile gelistirme icin konusulan yeni ozellikleri konsolide eder.
> Her madde `[Mobile]` (sadece UI/client isi) ya da `[Backend]` / `[Backend + Mobile]` (backend'de de yeni is gerektirir) olarak etiketlenmistir.

---

## 1. Onboarding & Auth

- **[Mobile]** Intro/onboarding ekranlari - Login/Register'dan once 3-4 sayfalik swipe ile gecilen tanitim akis.
- **[Backend + Mobile]** Google ile giris - Su an sadece email/password + JWT var. Backend'e external login (Google OAuth) + `ApplicationUser`'a baglama eklenmesi gerekiyor. Mobile'da Google Sign-In SDK entegrasyonu.

---

## 2. Live Trip Mode (Seyahat Sirasinda)

- **[Mobile]** Trip baslat - GPS konum + lokal saat dilimi alinir, ilgili gunun plani otomatik gosterilir.
- **[Mobile]** Timeline + harita ayni ekranda birlikte gorunur (`TimelineEntry` koordinatlari haritada pinlenir).
- **[Backend + Mobile]** Yakindaki mekanlar onerisi - hibrit yaklasim: kendi `places` tablosu + kapsama disi bolgeler icin canli API.
- **[Mevcut]** Ziyaret isaretleme - `TimelineEntry` visited/unvisited ozelligi zaten backend'de var.
- **[Backend]** Visit Log (yeni) - kullanicinin gercekte ne odedigi + puan + yorum. Yeni alan/entity gerekir (`TimelineEntry`'ye ek alan ya da ayri `PlaceVisitLog`).
- **[Backend + Mobile]** Trip sonrasi ozet ekrani - trip bittikten sonra kullaniciya kac yer ziyaret ettigi, tahmini vs gercek harcama, en sevilen duraklar, toplam gun/rota ozeti gibi kapanis bilgileri sunulur. Bu hem retention hem de paylasilabilirlik icin cok degerlidir.
- **[Backend + Mobile]** Memories / journal mode - Visit Log'un bir ust katmani olarak kullanici gezi sirasinda kisa notlar, fotograflar ve "bugun ne yaptim" kayitlari birakabilir. Zamanla trip sadece rota degil, paylasilabilir bir ani defterine donusur. Ileride bunun ustune ayri bir kolaj/ani derleme deneyimi de kurulabilir.
- **[Backend + Mobile]** Offline destek - trip verisi cihazda cache'lenmeli, internet olmadan da timeline/harita goruntulenebilmeli. Live Trip Mode'un asil kullanim senaryosu zaten internetsiz/roaming ortamlar.

---

## 3. Veri Stratejisi (Otel / Ucak / Diger Ulasim)

- **[Backend]** Otel verisi - periyodik/offline snapshot yontemi surdurulecek (8 sehir, 10-15 otel, X aylik veri - mevcut demo yaklasimi).
- **[Backend]** Ucak verisi - sinirli route seti (ornegin 8-10 populer hat), periyodik snapshot. Anlik/canli fiyat takibi bitirme fazina (Amadeus API) birakiliyor.
- **[Mobile]** Kapsam disi sehir/mekan - Chrome Custom Tabs ile 4-5 site (Booking.com vb.) acilir, kullanici kendi arar. Uygulamadan cikmis hissi vermez.
- **[Mobile]** Otobus/diger ulasim turleri - ayni Custom Tabs mantigi, veri cekmeye calisilmiyor.
- **Onemli karar** Booking.com linki sadece Custom Tab'da acilir, oradan scrape edilmez. ToS riski ve teknik kirilganlik nedeniyle veri ihtiyaci varsa offline/periyodik snapshot yontemi tercih edilir.
- **[Backend]** Crowdsourced veri - kullanici visit log'lari zamanla kendi veri kaynaklarindan biri haline gelir, ozellikle kapsanmayan/ucra yerler icin.
- **[Backend]** Provider freshness / data quality gorunurlugu - ucus ve otel verileri snapshot mantigiyla sunuluyorsa, kullaniciya verinin ne kadar guncel oldugu gosterilmelidir. "Son guncelleme", "canli fiyat degil", "tahmini veri" gibi aciklamalar guven icin onemlidir.

---

## 4. AI Asistan - Generator Degil, Recommend/Optimize

- **Tasarim karari** AI tam rota uretmeyecek. Kullanici zaten AI'nin verdigi rotayi oldugu gibi kullanmiyor; bunun yerine onerme ve optimize etme rolu ustlenecek.
- **[Backend]** Timeline optimizasyon onerisi (siralama, mesafe/sure azaltma)
  - Bu saf LLM isi degil, deterministik bir optimizasyon problemi.
  - `IsLocked=true` olan entry'ler sabit kalir; sadece esnek olanlar kilitli noktalara gore bolunen segmentler icinde yeniden siralanir.
  - Stop'lar arasi gercek sure icin Google Maps Distance Matrix API kullanilir.
  - Gunluk entry sayisi kucuk oldugundan brute-force veya basit sezgisel algoritmalar yeterli olur.
  - Sonuc otomatik uygulanmaz; kullaniciya onerilir. Onaylanirsa mevcut reorder endpoint'i kullanilir.
  - Mimari olarak mevcut `TimelineService`'in dogal uzantisi olur.
- **[Backend]** AI Chat - once soru sorarak sinirlandirir (butce, tarih, tercih), sonra arama yapar.
- **Kritik** LLM'e gercek arama/tool baglanmali (function calling + search API/kendi veri tabani). Modelin kendi parametrik bilgisinden "bulmasi"na izin verilmeyecek; hallusinasyon riski yuksek.
- **Not** Mevcut `ScoringService` ve `RecommendationService` zaten bu mantigin temelini atiyor; sifirdan baslanmiyor.

---

## 5. Sosyal / Destek

- **[Mevcut]** Kullanici sorulari/destek - ayri bir destek sistemi kurulmuyor, mevcut community/post/comment/tip sistemi uzerinden hallediliyor.
- **[Backend + Mobile]** Trip paylasma / deep link - WhatsApp vb. uzerinden paylasilan link, dogrudan ilgili trip detail ekranina acilir. Mevcut fork sistemiyle dogal olarak ortusur.
- **[Backend + Mobile]** Global arama - kullanici, trip, post, place ve tag aramasi tek bir arama deneyiminde birlestirilir. Canli urun asamasinda kesif ve erisilebilirlik icin onemli bir katman olur.
- **[Backend + Mobile]** Bookmark / collections - mevcut `saved trips` yapisi korunur ama bunun ustune kullanicilarin kendi koleksiyonlarini olusturabildigi yeni bir yapi eklenir. Ornek: "Italy Ideas", "Weekend Trips", "Honeymoon".
- **[Backend + Mobile]** Trip collaboration - tek owner modelinin yanina davet edilen collaborator mantigi eklenir. Bir trip birden fazla kullanici tarafindan birlikte duzenlenebilir; ozellikle grup seyahati ve cift planlamasi icin cok degerlidir.
- **[Backend + Mobile]** Report sistemi - kullanicilar post, comment, tip, trip ve kullanici profillerini raporlayabilir. Boylece mevcut admin paneline, kullanicidan moderasyona sinyal tasiyan resmi bir akis eklenir.
- **[Backend]** Soft moderation / shadow controls - sadece hard delete veya suspend degil, daha kontrollu moderasyon aksiyonlari eklenir:
  - Icerigi tamamen silmeden public akistan dusurme
  - Icerigi `review pending` durumuna alma
  - Kullanicinin paylasim yapmasini gecici sure dondurma
  - Trip gorunurlugunu azaltma veya explore'dan cikarma
  - Kullaniciyi tamamen banlamadan belirli aksiyonlardan kisitlama
  Bu yaklasim, yanlis pozitif moderasyon kararlarinda geri donusu kolaylastirir ve operasyonel esneklik saglar.

---

## 6. Bildirim & Izinler

- **[Backend + Mobile]** Push notification (FCM) - roadmap'te daha sonraya birakilmisti; plana geri alindi. Trip/ucus hatirlatmalari ve sosyal bildirimler icin gerekli.
- **[Backend + Mobile]** Notification preferences - kullanici hangi bildirim turlerini almak istedigini secebilir. Push eklenecekse bu tercih katmani zorunlu hale gelir.
- **[Mobile]** Izin akislarI (konum, bildirim, kamera/galeri) - toplu onboarding'de degil, gerektigi an (just-in-time) istenecek.

---

## 7. Para Birimi Gosterimi

- **Karar** Lokal para birimi buyuk/ana, kullanicinin kendi para birimi altinda kucuk/ikincil gosterilecek.
- **[Backend]** Kur verisi - gunluk guncellenen ucretsiz bir kur API'si ile, gunluk cron job + cache.

---

## 8. Canli Operasyon & Veri Tutarliligi

- **[Backend]** Timezone normalization - Live Trip Mode, push reminder, visit log ve timeline hesaplarinda zaman dilimi yonetimi ayri bir feature olarak ele alinmali. Trip'in gectigi ulkenin lokal saati ile kullanicinin cihaz saati karistirilmamali.
- **[Backend]** Audit log / admin action history - admin hangi kullaniciyi ne zaman suspend etti, hangi post'u sildi, hangi icerigi hide etti gibi aksiyonlar kayit altina alinmali. Moderasyonun izlenebilir ve denetlenebilir olmasi icin gereklidir.

---

## Ozet: Backend'e Cikan Yeni Isler

1. Google OAuth (external login)
2. Visit Log entity/alanlari (gercek harcama + puan + yorum)
3. Trip summary verisi
4. Memories / journal veri modeli
5. Offline sync destegi icin endpoint/veri yapisi
6. Push notification altyapisi (FCM token kaydi, tetikleme)
7. AI Chat + tool-grounded search (function calling mimarisi)
8. Doviz kuru servisi (gunluk cron + cache)
9. Trip deep link / paylasim metadata'si
10. Global search altyapisi
11. Collections / bookmark yapisi
12. Trip collaboration / invite modeli
13. Report sistemi
14. Soft moderation aksiyonlari
15. Notification preferences
16. Timezone normalization
17. Audit log / admin action history
18. Provider freshness / data quality alanlari

## Ozet: Sadece Mobile/UI Isi Olanlar

1. Onboarding swipe ekranlari
2. Live Trip Mode UI (timeline + harita birlikte)
3. Chrome Custom Tabs entegrasyonu (Booking.com vb. + otobus siteleri)
4. Izin isteme akislarI (just-in-time)
5. Para birimi cift gosterim UI'i

---

## Onceliklendirme Notu

### 1. Hemen yapilmali / bugunku demoya deger katar

- Onboarding ekranlari - kolay, mobile-only, demo'da iyi gorunur
- Google login - backend'e is cikariyor ama kullanici beklentisi yuksek, erken yapilmali
- Para birimi cift gosterimi - kucuk, gorsel cila
- Izin akislarI - zorunlu, kucuk
- Provider freshness gostergesi - bedavaya yakin, guven hissi veriyor

### 2. Orta oncelik - gercek wow faktoru, staj ortasina kadar hedeflenebilir

- Live Trip Mode (cekirdek hali) - timeline + harita + visited isaretleme. Bu listenin en degerli parcasi, cunku uygulamanin asil kullanim anini, yani seyahat sirasini gosteriyor. Offline kismi ilk versiyonda atlanabilir.
- Trip sonrasi ozet ekrani - ucuz, paylasilabilir, mentore "bitmis bir deneyim" hissi verir.
- Trip paylasma / deep link - viral degeri yuksek, orta efor.

### 3. Simdilik beklesin (iyi fikir ama bu donem icin fazla / erken)

- Trip collaboration - ayri proje gibi, kesinlikle sonraya.
- AI Chat (tool-grounded search) - Phase 6 AI'nin kendisi bile yok, once o temel kurulmadan ustune chat katmani koymak ters siralama olur.
- Global search, Collections, Memories / Journal - guzel ama olmazsa eksik degil; mentore gosterecegin bir akista bunlar olmasa kimse fark etmez.
- Soft moderation, Report sistemi, Audit log, Notification preferences - bunlar gorunmez / admin tarafi isler. Mentor bir ekran goruntusune bakarken bunlari goremez; urun degeri var ama demo degeri dusuk. Bu yuzden simdilik ayri tutulabilir.
- Timezone normalization - ayri bir "ozellik" gibi degil, Live Trip Mode'u insa ederken icine gomulu bir muhendislik gerekliligi olarak ele alinmali.
- Visit Log / Crowdsourced veri, Offline sync - Live Trip Mode'un ileri versiyonu, ilk demo bunlarsiz da ayakta durur.
