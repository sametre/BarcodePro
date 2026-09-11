BARCODE PRO 2.1.0 - SERVER / CLIENT

SQLITE 3 VE MYSQL ICE AKTARMA
Urunler, stok hareketleri, silme ve duzenleme islemleri inventory.db SQLite 3
veritabaninda transaction ile saklanir. Eski inventory.json ilk acilista otomatik
aktarilir ve inventory.json.migrated.bak olarak korunur.
Server Yonetimi > Ayarlar > MySQL ice aktar ekraninda tablo ve kolonlari esleyin;
Baglantiyi test et > Urunleri getir > Envantere aktar sirasini izleyin.

YONETICI VE TSC PORT IZNI
Server ve Client uygulamalari Windows yonetici yetkisiyle acilir.
Windows UAC penceresinde Evet secilmelidir.
TSC yazici FILE: / PORTPROMPT: portunda kalirsa uygulama yonetici yetkisiyle
fiziksel USB, WSD veya IP portunu yazici kuyruguna atar ve etiketi RAW TSPL basar.

SERVER VE CLIENT KURULUMU
Ortak verinin duracağı ana bilgisayara Barcode Pro Server kurulur.
Server, Windows servisi olarak bilgisayar açıldığında otomatik çalışır.
Server ekranını kapatsanız veya Windows oturumunu kapatsanız da çalışmaya devam eder.
Kurulum yönetici izniyle TCP 5088 ve UDP 5089 güvenlik duvarı kurallarını
yalnızca özel/etki alanı ağlarında yerel alt ağ için açar. Servis için ayrı bir
Windows hesabı ve veri klasörü izni oluşturur. Hata halinde servis yeniden başlar.
Server ekranı yerel IP, bilgisayar adı, dış IP ve rastgele erişim anahtarını gösterir.
Bağlantı adresleri ayrıca masaüstündeki bilgi dosyasına yazılır; anahtar bu dosyada yer almaz.
Diğer bilgisayarlara Barcode Pro Client kurulur.
Client açılışında "Sunucuları otomatik bul" ile sunucuyu seçin veya adresini yazın.
Erişim anahtarını Server ekranından kopyalayın. API anahtarı owner değildir.
Ürün ve stok değişiklikleri Server bilgisayarındaki ortak envantere kaydedilir.
Server Yönetimi aynı merkezi SQLite veritabanını kullanır.
Otomatik bulma aynı yerel ağ içindir. Dış IP bilgisi tek başına dışarıdan erişim sağlamaz;
internet üzerindeki client için kurumsal VPN veya HTTPS ağ geçidi gerekir.

ÜRÜN SQL DOSYASI VE GÖRSELLER
Ayarlar > SQL dosyası içe aktar bölümünden DBeaver ürün SQL dosyası seçilebilir.
Dosyadaki alanlar ürün, stok ve görsel alanlarına dönüştürülür; işlemler SQLite'a kaydolur.
SQL dosyası görsellerin yalnızca dosya yollarını içeriyorsa gerçek görsel dosyaları
ayrıca sağlanmalıdır. Eksik görseller aktarım raporunda listelenir.
Müşteri ürünlerini içeren kurulum, yeni Server veritabanını hazır ürünlerle başlatır.
Mevcut Server veritabanı korunur; ek ürünler içe aktarma ekranından alınabilir.

İLK GİRİŞ
Kullanıcı kodu: owner
Şifre: owner
Bu beta sürümünde giriş bilgileri sabittir.

TSC DOĞRUDAN BASKI DÜZELTMESİ
TSC seçildiğinde doğrudan Raw TSPL otomatik seçilir.
FILE: / PORTPROMPT: portu seçiliyse baskı başlamaz.
Uygulama boşta olan gerçek USB portunu otomatik bulur ve TSC kuyruğuna bağlar.
Windows izni yetmezse yalnızca port ataması için yönetici onayı gösterilir.
Birden fazla boş port bulunursa baskı ekranından port seçilip bağlanır.

FİRMA VE YAZICI
Ayarlar > Firma ve etiket bölümünde firma adını ve logoyu kaydedin.
TTP-244CE şablonu oluştur düğmesi firmanıza ait yeni etiket ekler.
TSC TTP-244CE için 203 DPI, 60 x 40 mm etiket, 2 mm aralık kullanılır.
Rulonuz farklıysa şablon ölçüsünü ve etiket aralığını düzenleyin.
Yazıcılar menüsünde Windows'a kurulmuş cihazı seçin, TTP-244CE profili
düğmesine ve ardından Profili kaydet düğmesine basın.
TSC yazıcı sürücüsü bu kuruluma dahil değildir; cihaz sürücüsü ayrıca kurulmalıdır.
İlk fiziksel baskıda bir etiketle ölçü, kayma ve okunabilirliği kontrol edin.

MYSQL
Ayarlar > MySQL bağlantısı bölümüne müşteri sunucusunu ve kolonlarını girin.
Güvenlik için "TLS zorunlu - Hosting uyumlu" seçeneği önerilir.
1042 hatası çoğunlukla TLS değildir. Hosting panelindeki Uzak MySQL / Allowed
Hosts listesine bu bilgisayarın IP adresini ekleyin; port, güvenlik duvarı,
bind-address ve MySQL kullanıcısının host iznini kontrol edin.
Bağlantıyı test et > Ürünleri getir > Envantere aktar sırasını izleyin.
Aktarım tek yönlüdür; yerel stok işlemleri MySQL'e geri yazılmaz.
Sunucu şifresi bu Windows hesabına bağlı şifrelenerek saklanır.
Geliştiriciye ait veri veya bağlantı şifresi kurulum paketinde bulunmaz.

KURULUM VE VERİLER
Windows 10 (1809 ve sonrası) / Windows 11, 64 bit.
.NET çalışma zamanı dahildir; kurulum sırasında internet gerekmez.
Server ve Client kurulumları ve yönetim arayüzleri yönetici izni ister.
Program: %PROGRAMFILES%\Barcode Pro Server veya Barcode Pro Client
Server verileri: %PROGRAMDATA%\BarcodePro\Server\inventory.db
Client ayarları: %LOCALAPPDATA%\BarcodePro
Kaldırma, ürünleri ve kullanıcı ayarlarını silmez.
Server güncellemesi servisi durdurur, dosyaları yeniler ve servisi tekrar başlatır.
İlk Server geçişinde mevcut kullanıcının eski inventory.db/JSON dosyası güvenli
SQLite yedeği ile merkezi klasöre taşınır; kaynak dosya korunur.

BETA DOĞRULAMA
Giriş, şablon, barkod, stok ve MySQL aktarım kuralları otomatik kontrol edildi.
Müşterinin canlı MySQL sunucusu ve fiziksel TSC baskısı henüz doğrulanmadı.
Kurulum dosyası kod imzalama sertifikasıyla imzalanmamıştır.


