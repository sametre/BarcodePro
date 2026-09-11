# Server ve Client — 2.1.0

Server Setup.exe dosyasını ana Windows bilgisayarında yönetici olarak çalıştırın. Kurulum, `BarcodeProServer` Windows servisini gecikmeli otomatik başlatma ve hata sonrası yeniden başlatma ile kaydeder. Server yönetim penceresini kapatmak servisi durdurmaz.

Merkezî ürünler ve hareketler `%PROGRAMDATA%\BarcodePro\Server\inventory.db` içinde tutulur. WAL kullanan SQLite, stok güncellemesiyle hareket kaydını aynı transaction içinde yazar. Her değişiklik veritabanındaki güncel stoğu okur; ayrı yönetim penceresi ve birden fazla istemci aynı dosyayı paylaşır. İstemciler SQLite dosyasını ağ paylaşımı olarak açmaz; Server API üzerinden işlem yapar.

## İlk veri ve güncelleme

Kurulumda mevcut merkezî veritabanı varsa olduğu gibi korunur. Yoksa aynı kullanıcının önceki `%LOCALAPPDATA%\BarcodePro\inventory.db` veya `inventory.json` envanteri taşınır. Bunlar da yoksa kurulumla paketlenen ilk ürün verisi kullanılır. Taşıma SQLite Backup API kullanır, böylece WAL içindeki kayıtlar da alınır. Eski JSON dosyası ve migration yedeği korunur. Kaldırma işlemi merkezî veriyi silmez.

Mevcut envantere yeni SQL ürün dosyası eklemek için Server Yönetimi → Ayarlar → SQL dosyasından ürün aktarımı kullanılır. Barkod/SKU ile eşleşen ürünler güncellenir; bulunmayanlar eklenir. Mevcut stokları değiştirme seçeneği varsayılan olarak kapalıdır. Tüm toplu aktarım tek transaction içinde gerçekleşir.

## İstemci bağlantısı

1. Diğer bilgisayarlara Client Setup.exe kurun.
2. Client bağlantı ekranı yerel ağdaki Server'ı UDP 5089 üzerinden arar. Bulamazsa Server ekranındaki `http://bilgisayar-adı:5088` veya yerel IP adresini girin.
3. Server ekranında gösterilen kurulum başına rastgele erişim anahtarını Client'a girin. Ürün uygulamasının beta kullanıcı kodu ve şifresi `owner / owner`; ağ erişim anahtarı bundan farklıdır.
4. Bağlantıyı test edip kaydedin. Ürün ekleme/düzenleme, silme ve stok hareketleri Server SQLite veritabanına gider. F5 veya sayfa geçişiyle güncel veri alınır; istemci açıkken periyodik yenileme de yapılır.

## Yetkiler ve dış ağ

Setup ve uygulama yönetici yetkisi ister. Servis kendi `NT SERVICE\BarcodeProServer` hesabında çalışır; veri klasörüne servis, SYSTEM ve yöneticiler erişebilir. Windows güvenlik duvarında yalnızca uygulamanın TCP 5088 ve keşif UDP 5089 kuralları açılır; Özel/Etki Alanı profilleri ve yerel alt ağ ile sınırlıdır.

Dış IP, internet varsa otomatik sorgulanır ve bilgi olarak gösterilir. Dış IP göstermek modem/NAT/CGNAT engelini kaldırmaz. Farklı şubeleri bağlamak için kurumsal VPN veya HTTPS ağ geçidi gerekir; uygulama modem şifresi istemez, UPnP ile otomatik port açmaz. VPN farklı alt ağ kullanıyorsa ağ yöneticisi kuralın uzak adres kapsamına o VPN ağını eklemelidir. Kurulum bağlantı bilgileri dosyasını masaüstüne de yazar.

## Yönetici testi

Normal `dotnet run --project BarcodePrinter.Checks` servis kaydı yapmadan API ve SQLite kurallarını sınar. `installer/Test-Service.ps1` (varsa) ayrı servis adı, port ve geçici veri klasörüyle yönetici testi yapar. Test sonucunu gerçek müşteri ağ erişimi ve fiziksel TSC baskı kabul testinden ayrı değerlendirin.
