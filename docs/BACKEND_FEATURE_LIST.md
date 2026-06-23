# OmniFlow Backend Feature List

Bu doküman, backend tarafında bulunan özellikleri ürün ve sistem odaklı biçimde listeler. Amaç, roadmap audit mantığından çıkıp doğrudan “uygulama şu anda neleri yapabiliyor, neleri kısmen yapıyor, neleri henüz yapmıyor?” sorusuna cevap vermektir.

---

## Tamamlanan Özellikler

- **Kullanıcı kayıt sistemi**
  Kullanıcılar hesap oluşturabiliyor. Register akışı validation, Identity entegrasyonu ve veritabanı kullanıcı kaydı ile birlikte çalışıyor.

- **Kullanıcı giriş sistemi**
  Kullanıcılar login olabiliyor ve JWT tabanlı authentication alabiliyor. Access token üretimi aktif.

- **Refresh token ile oturum yenileme**
  Web ve mobil kullanım senaryolarını destekleyen refresh token akışı mevcut. Token rotation mantığı backend seviyesinde bulunuyor.

- **JWT authentication ve authorization altyapısı**
  API endpoint’leri kimlik doğrulama ve yetki kontrolü ile korunuyor. Middleware ve token validation konfigürasyonu aktif.

- **Rol ve seed data altyapısı**
  Başlangıç rolleri ve varsayılan kullanıcılar seed ediliyor. Sistem ilk ayağa kalkışta temel auth verisini oluşturabiliyor.

- **Swagger / OpenAPI dokümantasyon altyapısı**
  API endpoint’leri Swagger üzerinden görülebiliyor. JWT bearer şeması tanımlı.

- **Global hata yönetimi**
  Exception’lar merkezi middleware üzerinden yönetiliyor. Validation, forbidden, not found ve conflict gibi senaryolar standart cevap formatına dönüştürülüyor.

- **Validation pipeline**
  FluentValidation ve MediatR validation davranışı aktif. Request’ler handler seviyesine gitmeden doğrulanabiliyor.

- **Soft delete altyapısı**
  Auditable entity’ler için `DeletedAt` tabanlı soft delete davranışı mevcut. Global query filter ile silinen kayıtlar otomatik gizleniyor.

- **Audit alanları otomatik yönetimi**
  `CreatedAt` ve `UpdatedAt` alanları `SaveChangesAsync` sırasında otomatik güncelleniyor.

- **PostgreSQL + EF Core + Identity entegrasyonu**
  Uygulama PostgreSQL üzerinde çalışacak şekilde hazırlanmış. Identity tabloları ve domain tabloları tek bağlam altında yönetiliyor.

- **`citext` ve `postgis` extension desteği**
  Case-insensitive text ve coğrafi veri altyapısı backend’de etkinleştirilmiş.

- **Database migration altyapısı**
  Migration dosyaları mevcut ve uygulama başlangıcında migration çalıştırma yaklaşımı kullanılıyor.

- **Places listeleme**
  Sistemdeki yerler paginated biçimde listelenebiliyor.

- **Place detay görüntüleme**
  Tek bir place kaydı ID ile getirilebiliyor.

- **Şehre göre place filtreleme**
  Belirli bir şehirdeki place kayıtları filtrelenebiliyor.

- **Admin tarafından place oluşturma**
  Yeni place verisi backend üzerinden oluşturulabiliyor.

- **Trip oluşturma**
  Kullanıcılar trip oluşturabiliyor. Yeni modelde bu akış wizard yapısına da bağlanmış durumda.

- **Trip listeleme**
  Kullanıcının kendi trip’leri pagination ile listelenebiliyor.

- **Trip detay görüntüleme**
  Belirli bir trip detayı backend’den alınabiliyor.

- **Trip güncelleme**
  Uygun durumda olan trip kayıtları owner kontrolü ile güncellenebiliyor.

- **Trip silme**
  Trip kayıtları soft delete mantığıyla kaldırılabiliyor.

- **Trip publish etme**
  Draft durumundaki trip’ler yayınlanabiliyor.

- **Trip archive etme**
  Publish edilmiş trip’ler arşivlenebiliyor.

- **Trip upvote sistemi**
  Kullanıcılar yayınlanmış trip’leri upvote edebiliyor.

- **Trip upvote kaldırma**
  Daha önce verilmiş upvote geri alınabiliyor.

- **Trip kaydetme**
  Kullanıcılar trip’leri saved listesine ekleyebiliyor.

- **Trip kaydını kaldırma**
  Kaydedilmiş trip saved listeden çıkarılabiliyor.

- **Saved trips listeleme**
  Kullanıcının kaydettiği trip’ler ayrı endpoint üzerinden getirilebiliyor.

- **Trip fork sistemi**
  Yayınlanmış bir trip fork edilerek yeni kullanıcı için taslak kopya üretilebiliyor.

- **Explore akışı**
  Publish edilmiş trip’ler explore endpoint’i üzerinden filtrelenip listelenebiliyor.

- **Featured trips**
  Explore alanında öne çıkan trip’ler için ayrı bir endpoint mevcut.

- **Posts oluşturma**
  Kullanıcılar post paylaşabiliyor.

- **Post güncelleme**
  Post sahipleri kendi post’larını güncelleyebiliyor.

- **Post silme**
  Post silme akışı owner veya admin kontrollü olarak çalışıyor.

- **Post detay görüntüleme**
  Tekil post verisi detaylı biçimde alınabiliyor.

- **Post upvote sistemi**
  Kullanıcılar post’lara upvote verebiliyor.

- **Post upvote kaldırma**
  Verilen post upvote’u kaldırılabiliyor.

- **Kullanıcının kendi postlarını listeleme**
  Giriş yapan kullanıcının ürettiği post’lar listelenebiliyor.

- **Belirli bir kullanıcının postlarını listeleme**
  Başka bir kullanıcının post’ları backend üzerinden getirilebiliyor.

- **Beğenilen postları listeleme**
  Kullanıcının upvote ettiği post’lar ayrı endpoint üzerinden alınabiliyor.

- **Trending tags**
  Son dönemde öne çıkan etiketler backend tarafından hesaplanıp sunuluyor.

- **Comments oluşturma**
  Kullanıcılar post’lara yorum yapabiliyor.

- **Reply destekli yorum sistemi**
  1 seviye reply destekleniyor. Parent-child ilişki korunuyor.

- **Comment silme**
  Yorum sahipleri yorumlarını silebiliyor.

- **Comment upvote sistemi**
  Yorumlara upvote verilebiliyor.

- **Comment upvote kaldırma**
  Yorum upvote’u geri alınabiliyor.

- **Post bazlı comment listeleme**
  Bir post’un yorumları pagination ile getirilebiliyor.

- **Cross-post reply koruması**
  Bir post’a ait comment üzerinden başka post’a reply verilmesi engelleniyor.

- **Community tip oluşturma**
  Kullanıcılar trip bazlı topluluk önerisi bırakabiliyor.

- **Community tip silme**
  Tip sahipleri kendi tiplerini silebiliyor.

- **Community tip upvote sistemi**
  Tip’lere upvote verilebiliyor.

- **Community tip upvote kaldırma**
  Tip upvote’u geri alınabiliyor.

- **Trip bazlı community tip listeleme**
  Bir trip için bırakılmış tip’ler listelenebiliyor.

- **Takip etme sistemi**
  Kullanıcılar başka kullanıcıları takip edebiliyor.

- **Takibi bırakma sistemi**
  Kullanıcılar daha önce takip ettikleri kullanıcıları unfollow edebiliyor.

- **Followers listeleme**
  Bir kullanıcının takipçileri listelenebiliyor.

- **Following listeleme**
  Bir kullanıcının takip ettiği hesaplar listelenebiliyor.

- **User profile görüntüleme**
  Kullanıcı profili username veya ilgili endpoint’ler üzerinden görüntülenebiliyor.

- **Kendi profilini görüntüleme**
  Authenticated kullanıcı kendi profil bilgisini alabiliyor.

- **Profil güncelleme**
  Kullanıcı bio ve profil alanlarını güncelleyebiliyor.

- **Top contributors listeleme**
  Yüksek katkı sağlayan kullanıcılar backend tarafından sıralanabiliyor.

- **Suggested follows**
  Kullanıcıya önerilen takip hesapları backend tarafında hesaplanabiliyor.

- **Notifications listeleme**
  Kullanıcının bildirimleri listelenebiliyor.

- **Unread notification count**
  Okunmamış bildirim sayısı ayrı endpoint ile alınabiliyor.

- **Tekil bildirimi okundu işaretleme**
  Bir bildirim read durumuna geçirilebiliyor.

- **Tüm bildirimleri okundu işaretleme**
  Kullanıcının tüm bildirimleri topluca okundu yapılabiliyor.

- **Karma puan altyapısı**
  Karma event üretimi ve puan akışı backend tarafında işliyor.

- **Karma history görüntüleme**
  Kullanıcının karma geçmişi listelenebiliyor.

- **Follow, upvote, fork ve comment bazlı notification üretimi**
  Kullanıcı etkileşimlerinden notification tetiklenebiliyor.

- **Publish, fork ve upvote bazlı karma üretimi**
  Kullanıcı davranışlarına bağlı olarak karma puanı award edilebiliyor.

- **Block user sistemi**
  Kullanıcılar başka kullanıcıları engelleyebiliyor.

- **Unblock user sistemi**
  Önceden engellenen kullanıcıların engeli kaldırılabiliyor.

- **Blocked users listeleme**
  Kullanıcının engellediği hesaplar görüntülenebiliyor.

- **Admin post moderasyonu**
  Admin kullanıcılar post’ları listeleyip silebiliyor.

- **Admin user listeleme**
  Admin kullanıcılar sistem kullanıcılarını listeleyebiliyor.

- **Admin suspend / unsuspend sistemi**
  Kullanıcı hesapları admin tarafından askıya alınabiliyor veya geri açılabiliyor.

- **Email verification akışı**
  Kayıt sonrası email doğrulama süreci backend’de destekleniyor.

- **Verification email yeniden gönderme**
  Doğrulama maili tekrar tetiklenebiliyor.

- **Forgot password akışı**
  Şifre sıfırlama talebi oluşturulabiliyor.

- **Reset password akışı**
  Geçerli token ile şifre yenilenebiliyor.

- **SMTP tabanlı email gönderimi**
  Backend email servisi üzerinden mail akışları çalışacak şekilde hazırlanmış.

- **Media upload altyapısı**
  Görsel yükleme için blob tabanlı upload endpoint’i mevcut.

- **Profile photo upload**
  Kullanıcı profil fotoğrafını backend üzerinden yükleyebiliyor.

- **Azure Blob Storage entegrasyonu**
  Dosya yükleme akışları için blob service altyapısı bulunuyor.

- **Trip planning wizard**
  Yeni trip modeli çok adımlı wizard mantığıyla oluşturulabiliyor.

- **Multi-destination trip modeli**
  Tek şehir yerine birden fazla destinasyon içeren trip kurgusu backend’de destekleniyor.

- **Trip destinations yönetimi**
  Trip’e bağlı destinasyonlar listelenebiliyor, eklenebiliyor, güncellenebiliyor ve silinebiliyor.

- **Timeline sistemi**
  Eski stop mantığının yerine timeline entry tabanlı yeni planlama modeli kullanılıyor.

- **Timeline entry oluşturma**
  Kullanıcılar trip timeline’ına yeni entry ekleyebiliyor.

- **Timeline entry güncelleme**
  Entry bilgileri düzenlenebiliyor.

- **Timeline entry silme**
  Timeline içindeki entry’ler kaldırılabiliyor.

- **Timeline reorder**
  Timeline entry sıralaması değiştirilebiliyor.

- **Timeline visited işaretleme**
  Bir timeline entry visited veya unvisited duruma geçirilebiliyor.

- **Custom event / custom transport / custom flight / custom accommodation entry desteği**
  Timeline yalnızca place kaydıyla sınırlı değil; farklı özel entry tipleri de destekleniyor.

- **Provider catalog sistemi**
  ProviderFlight ve ProviderHotel tabloları ile referans veri altyapısı mevcut.

- **Origin cities listeleme**
  Provider flight verisinden kalkış şehirleri listelenebiliyor.

- **Provider flight listeleme**
  Public endpoint üzerinden uçuş verileri getirilebiliyor.

- **Provider hotel listeleme**
  Public endpoint üzerinden otel verileri getirilebiliyor.

- **Budget summary**
  Bir trip için gerçek zamanlı bütçe özeti backend tarafından hesaplanabiliyor.

- **Budget fallback mantığı**
  Kullanıcının bütçesi seçilen tier için yetersizse daha düşük tier’lara düşme mantığı backend’de mevcut.

- **Scoring engine**
  Place’ler kullanıcı tercihleri ve kategori eşleşmesine göre puanlanabiliyor.

- **Recommendation engine**
  Bir destinasyon için önerilen place listesi backend tarafından üretilebiliyor.

- **Timeline validation / capacity mantığı**
  Tempo, lock ve buffer kurallarıyla timeline çakışmaları ve kapasite kontrolleri yapılabiliyor.

- **Travel style çoklu seçim modeli**
  Yeni yapıda trip’ler birden fazla travel style ile tanımlanabiliyor.

- **Travel companion bazlı planlama**
  Kullanıcının solo, couple, family gibi companion tipi backend hesabına dahil ediliyor.

- **Tempo bazlı planlama**
  Günlük tempo tercihine göre timeline kapasitesi etkileniyor.

- **Transport preference bazlı planlama**
  Kullanıcının şehir içi ulaşım tercihi planlama mantığında kullanılabiliyor.

---

## Kısmen Tamamlanan Özellikler

- **Flights modülü**
  Trip’e bağlı flight verileri okunabiliyor ve gruplanmış response alınabiliyor. Ancak eski roadmap’teki `select flight` API kontratı artık mevcut değil.

- **Hotels modülü**
  Trip’e bağlı hotel verileri okunabiliyor. Ancak eski roadmap’teki `select hotel` API kontratı artık mevcut değil.

- **Flight / hotel seçim davranışı**
  Eski `POST /select` endpoint’leri yok. Bunun yerine yeni timeline modelinde `ProviderFlightId` ve `ProviderHotelId` referanslı custom entry yaklaşımı var; yani davranış tamamen kaybolmamış ama API yüzeyi değişmiş.

- **Integration test kapsamı**
  Test dosyaları geniş ve birçok feature için mevcut. Ancak bu çalışmada çözüm seviyesinde tam test geçişi doğrulanamadı.

- **CI/CD**
  Azure Pipeline mevcut ve build/publish/deploy akışı içeriyor. Ancak test step’i görünmüyor ve kalite kapısı tam değil.

- **API dokümantasyonu**
  Swagger ve response metadata güçlü durumda. Buna rağmen ayrı, son kullanıcı odaklı kapsamlı API dökümü veya collection seti görünmüyor.

- **Trip planning veri migration temizliği**
  Migration ve cleanup izleri var. Ancak eski verinin yeni modele tam nasıl taşındığı repo yüzeyinden tamamen doğrulanamıyor.

- **Dokümantasyon güncelliği**
  Projede oldukça fazla doküman var. Ancak bunların bir kısmı güncel mimariyi yansıtırken bir kısmı eski `Stop` ve eski endpoint yapısını anlatmaya devam ediyor.

---

## Henüz Implement Edilmeyen Özellikler

- **AI timeline generation**
  AI ile otomatik timeline üretimi için isimlendirilmiş arayüz ve dosya izleri var, fakat çalışan implementasyon görünmüyor.

- **AI fallback engine**
  AI başarısız olduğunda devreye girecek fallback service tasarlanmış ama fiili kodu boş durumda.

- **Generate timeline command akışı**
  `GenerateTimelineCommand` dosya izi mevcut olmasına rağmen handler ve çalışan endpoint akışı görünmüyor.

- **Eski roadmap biçimindeki flight selection endpoint’i**
  `POST /api/v1/trips/{tripId}/flights/select` şu an bulunmuyor.

- **Eski roadmap biçimindeki hotel selection endpoint’i**
  `POST /api/v1/trips/{tripId}/hotels/select` şu an bulunmuyor.

- **Load testing altyapısı**
  k6, NBomber, Bombardier veya benzeri load test araçlarına dair repo içinde somut iz bulunmuyor.

- **docker-compose tabanlı lokal orkestrasyon**
  Dockerfile mevcut, fakat backend’i diğer servislerle birlikte ayağa kaldıran compose tabanlı akış görünmüyor.

- **Postman collection / dış API tüketici paketi**
  Swagger mevcut olsa da ayrı bir Postman koleksiyonu veya benzeri hazır tüketim paketi görünmüyor.

- **Tam güncel tekil kaynak doküman**
  Mevcut sistemi tek başına doğru anlatan, eski roadmap’leri tamamen geride bırakan yeni bir “source of truth” dokümanı henüz görünmüyor.

---

## Roadmap Dışı Eklenen Özellikler

- **Trip planning modülü**
  Ana backend roadmap’te bu seviyede ayrı bir modül olarak tarif edilmeyen çok daha kapsamlı bir planning yapısı sonradan eklenmiş.

- **User block özelliği**
  İlk MVP kapsamı dışında düşünülmesine rağmen backend’e eklenmiş.

- **Admin panel backend’i**
  Post moderasyonu ve user suspension akışları roadmap dışı genişleme olarak eklenmiş.

- **Media upload ve blob storage**
  Dosya yükleme tarafı başlangıç roadmap’inin üzerinde bir genişleme olarak bulunuyor.

- **Email verification**
  Basit auth akışının ötesine geçip email doğrulama süreci eklenmiş.

- **Reset password akışı**
  Kullanıcı yaşam döngüsünü güçlendiren ek auth özelliği olarak sonradan eklenmiş.

- **Suggested follows**
  Sosyal etkileşimi artıran öneri endpoint’i roadmap’in ötesinde eklenmiş.

- **Top contributors**
  Katkı bazlı kullanıcı listeleme özelliği eklenmiş.

- **Liked posts**
  Kullanıcının beğendiği post’ları ayrı listeleme özelliği eklenmiş.

- **Trending tags**
  Topluluk içeriği için trend etiket endpoint’i eklenmiş.

- **Featured trips**
  Explore deneyimini güçlendiren öne çıkan trip listesi eklenmiş.

- **Provider data sistemi**
  Public provider endpoints ve provider tabloları ile planning akışını destekleyen ayrı veri katmanı eklenmiş.

- **Profile photo upload**
  Kullanıcı profil deneyimini tamamlayan medya tabanlı ek özellik eklenmiş.

- **Google / OSM metadata genişlemesi**
  Place modeli ilk şemaya göre çok daha zengin hale getirilmiş.

---

## Not

Bu liste, ürün ve sistem özelliklerini “güncel backend davranışı” üzerinden özetler. Eski roadmap’lerde adı geçen ama artık yerini başka bir yapıya bırakmış feature’lar, burada mümkün olduğunca güncel mimariye göre ifade edilmiştir.

