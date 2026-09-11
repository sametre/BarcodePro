# Barcode Pro ERP 2.1.0

Windows 10 (1809+) ve Windows 11 x64 için ilk dağıtım sürümü. .NET çalışma zamanı kurulum dosyalarına dahildir.

- DevExpress tarzında kompakt Windows Forms arayüzü, üst modül menüleri, alt işlemler, Windows simgeleri ve kurumsal ERP logosu.
- Server için kalıcı Windows servisi; otomatik başlangıç, kurtarma ve merkezî SQLite veritabanı.
- Client için otomatik yerel ağ keşfi, erişim anahtarı ve ortak stok işlemleri. Değişiklik yokken ürün/görseller yeniden indirilmez.
- Eşzamanlı SQLite stok değişiklikleri ve hareket geçmişi aynı transaction içinde saklanır.
- MySQL bağlantısından ve DBeaver products SQL dosyasından ürün aktarımı. Görsel klasörü veya temel web adresinden JPEG/PNG/WebP görselleri SQLite içine alma.
- Tekrar aktarımda mevcut stok ve görselleri koruma, kaynak kolonlar ve tarihleri saklama.
- TSC doğrudan TSPL baskısı ve yönetici yetkisiyle yazıcı portu düzenleme.

## Kurulum

Ana bilgisayara Server Setup, diğer bilgisayarlara Client Setup kurulur. Windows yönetici izni gerekir. Kullanıcı kodu ve şifre: **owner / owner**. Client ağ erişim anahtarı Server ekranında gösterilen rastgele anahtardır.

Veri: `%PROGRAMDATA%\BarcodePro\Server\inventory.db`. Eski yerel envanter ilk Server kurulumunda taşınır; mevcut merkezî veri güncelleme ve kaldırmada korunur. Genel GitHub paketleri müşteri ürünlerini veya bağlantı şifrelerini içermez.

Release içinde ayrıca açık istekle hazırlanan `BarcodePro-Server-2.1.0-Customer-Setup-x64.exe` bulunur. Bu paket, `products_202609101848.sql` dosyasından aktarılan 597 ürünü ve toplam 251 stok miktarını ilk Server veritabanına alır. Kaynakta yalnız görsel yolları bulunduğu için gerçek görsel dosyaları gömülmemiştir.

## Doğrulama ve sınırlar

189 uygulama kontrolü ve gerçek Windows ortamında 16 yönetici servis kontrolü geçti. Servis yeniden başlatma, CRUD, stok geçmişi, erişim anahtarı, dosya izinleri ve güvenlik duvarı kapsamı test edildi. Server/Client Setup kurulum-kaldırma ve dosya bütünlüğü ayrıca doğrulanır.

Fiziksel TSC çıktısı, müşteri MySQL sunucusu ve iki farklı fiziksel bilgisayar arasında bağlantı bu ortamda doğrulanmadı. Dış IP otomatik gösterilir; internetten erişim için VPN veya HTTPS ağ geçidi gerekir. Kurulum dosyaları kod imzalama sertifikasıyla imzalanmamıştır.

SQL dosyasındaki görsel yolları gerçek görsel dosyalarının yerine geçmez. Eksik görseller aktarım ekranında bildirilir; kullanıcı görsel klasörünü veya temel web adresini sağladığında içe alınabilir.
