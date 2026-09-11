# MySQL'den SQLite envanterine ürün aktarımı

## DBeaver SQL dosyası

2.1.0 ayrıca `INSERT INTO products (...) VALUES (...)` dışa aktarım dosyalarını okuyabilir. SQL dosyası çalıştırılmaz; yalnızca sabit ürün değerleri ayrıştırılır. SQL yorumları, çoklu satırlar, Türkçe metin ve kaçış karakterleri desteklenir. MySQL sunucusuna bağlanmadan bu dosyadan ürün aktarabilirsiniz.

İçe aktarım ekranında dosyayı seçin, varsa görsel klasörünü veya görsellerin temel web adresini belirtin ve önizleyin. Örneğin kaynak yol `products/a.webp` ise temel adres `https://firma.example/storage/` olabilir. Gerçek görseller dosyada bulunmaz; yollar korunur ve yüklenemeyenler özette bildirilir. JPEG/PNG/WebP görseller SQLite'a gömülerek Client bilgisayarlarda da gösterilir. Kaynaktaki kimlik, vergi, marka, kategori/birim kimlikleri, yer, tarih ve diğer kolonlar kaynak alanlarında saklanır. İlişkili tablonun adları dosyada yoksa kategori ve birim kimlikleri gösterilir.

Barkodu boş ürünlere kararlı yerel barkod üretilir. Yinelenen kaynak SKU için özgün kod korunup ayırt edici ek kullanılır; tekrar eden barkod aktarımı durdurur. İkinci aktarım aynı ürünleri çoğaltmaz. Mevcut stoklar ancak stok yenileme kutusu seçilirse değiştirilir. MySQL bağlantısı ekranındaki kolon eşleştirme kuralları aşağıdadır.

Ayarlar → MySQL bağlantısı ekranından Laravel uygulamasının kullandığı MySQL sunucusuna bağlanılır. Bu özellik tek yönlü, kullanıcı tarafından başlatılan ürün aktarımıdır. Yerel stok giriş/çıkışları MySQL'e gönderilmez; sürekli veya çift yönlü senkronizasyon yapılmaz.

1. Sunucu, port (varsayılan 3306), veritabanı, kullanıcı ve şifreyi girin. Sunucunun masaüstü bilgisayardan erişilebilir olması ve kullanıcının ürün tablosunda SELECT yetkisi bulunması gerekir.
2. Güvenlik seçimi varsayılan olarak TLS ve sunucu sertifikası doğrulamasıdır. Sunucu yapılandırmanıza uygun seçimi kullanın. Sertifika doğrulama hataları uygulama tarafından otomatik olarak atlanmaz.
3. Ürün tablosunu veya bir VIEW adını girin. Varsayılan `products` yalnızca başlangıç değeridir; Laravel projelerinin tablo adları farklı olabilir.
4. Alan eşleştirme sekmesinde gerçek kolon adlarını girin. Ürün adı, barkod ve SKU zorunludur. Başlangıçta `name`, `barcode`, `sku`, `price`, `stock` önerilir. Diğer alanlar isteğe bağlıdır. Kullanılmayan alanın kolonunu boş bırakın.
5. Kaydet → Bağlantıyı test et → Ürünleri getir sırasını izleyin. Test, tablo ve kolonlar üzerinde LIMIT 0 sorgusu çalıştırır; ürün önizlemesi ayrı okunur.
6. Önizlemeyi inceleyip Envantere aktar düğmesini kullanın. Mevcut stokları sunucudaki miktara getirmek için alttaki kutuyu ayrıca işaretleyin.

## Eşleştirme kuralları

- Barkod ve SKU, büyük/küçük harf duyarsız eşleştirilir. Baştaki barkod sıfırlarının korunması için MySQL kolonunu metin olarak tutun.
- Barkod ve SKU farklı yerel ürünlere eşleşirse veya kaynakta tekrar varsa tüm aktarım reddedilir.
- Yeni ürünün eşleştirilmiş stok miktarı açılış hareketi oluşturur. Mevcut ürün stokları varsayılan olarak korunur. Stok yenileme seçilirse fark, açıklamalı stok düzeltmesi olarak kaydedilir.
- Eşleştirilmemiş alanlar mevcut üründe korunur; yeni üründe uygulamanın varsayılanları kullanılır (örneğin stok 0, minimum 5, maksimum 100, aktif/menüde true).
- Aktif ve menü durumları 0/1 veya true/false olmalıdır. Eşleştirilmiş sayı ve durum alanlarında NULL reddedilir. Fiyat/stok negatif olamaz; maksimum stok minimumdan küçük olamaz.
- Kaynakta olmayan yerel ürünler silinmez. Ürün görselleri ve uzak oluşturulma/güncellenme tarihleri bu aktarımda alınmaz.
- Kategori gibi ilişkili alanlar için adları düz kolon olarak döndüren bir VIEW kullanılabilir. Serbest SQL çalıştırma yoktur; tablo ve kolon tanımlayıcıları doğrulanır.
- Tek aktarım sınırı 50.000 üründür; sınır aşılırsa kısmi aktarım yapılmaz. Bağlantı 10 saniye, sorgu 30 saniye, ekran işlemi toplam 2 dakika zaman aşımına sahiptir; okuma iptal edilebilir.
- Tüm ürünler doğrulandıktan sonra tek SQLite transaction içinde kaydedilir. Hata durumunda transaction geri alınır; ürünler ve hareketler değişmeden kalır.

## Ayarların saklanması

Bağlantı bilgileri `inventory.db` yanında `mysql-connection.json` dosyasındadır. Şifre Windows DPAPI CurrentUser ile şifrelenir; düz metin şifre ve tam bağlantı dizesi günlüğe yazılmaz. Dosya başka bilgisayara/Windows hesabına taşındığında şifre yeniden girilmelidir. Envanter JSON dışa aktarımı bağlantı şifresini içermez.

## Doğrulama

Otomatik kontroller: kolon/tablo enjeksiyonu reddi, kültürden bağımsız sayı dönüşümü, barkod sıfırları, durum alanları, şifreli ayar gidiş/dönüşü, tekrar aktarım, kimlik çakışması, stok koruma, stok farkı kaydı, toplu geri alma. Gerçek sunucu adresi ve erişim bilgileri sağlanmadığı için canlı MySQL sorgusu doğrulanmadı.

Kullanılan sürücü: MySqlConnector 2.6.2. Bağlantı ve TLS seçenekleri: [MySqlConnector resmi belgeleri](https://mysqlconnector.net/connection-options/), [TLS doğrulama modları](https://mysqlconnector.net/api/mysqlconnector/mysqlsslmodetype/).
