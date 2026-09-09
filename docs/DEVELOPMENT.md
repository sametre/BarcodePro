# Geliştirme günlüğü

## Faz 1 — Mevcut mimarinin ayrıştırılması
- Oluşturulan: Models/Inventory/InventoryModels.cs, Services/Inventory/Inventory.cs, Forms/{Dashboard,Products,Inventory,Barcode,History}/MainForm.*.cs, Controls/Dashboard/StockChart.cs.
- Taşınan: ProductEditor.cs → Forms/Products/ProductEditor.cs.
- Değişen: Form1.cs, BarcodePrinter.slnx (Checks çözüme eklendi).
- Stok servisinin davranışı ve JSON alanları değişmedi; mevcut veri için migration yok.
- Doğrulama: Build 0 hata/0 uyarı; mevcut 22 kontrol başarılı. Çalışan eski uygulama dosyayı kilitlediği için ana penceresi normal kapatıldı ve derleme tekrarlandı.

## Faz 2 — Ortak UI, tema ve navigasyon
- Oluşturulan: `Controls/Common/AppControls.cs`, `Themes/ThemeManager.cs`.
- Değişen: `Form1.cs`, `BarcodePrinter.csproj`, mevcut ekranların ortak grid/button kullanımı.
- AppButton/TextBox/ComboBox/Card/Panel/DataGrid/Badge/Toggle/NumericInput/DatePicker/Dialog/Toast/Sidebar/Toolbar; Light/Dark ve açılır-kapanır sidebar. PerMonitorV2 açıldı.
- İlk UI altyapısı kontrolünde build 0 hata/0 uyarı ve 22 eski kontrol geçti. Navigasyon entegrasyonu ve tema daha sonra genişletilmiş kontrollerde doğrulandı.

## Faz 3 — Dashboard ve ürünler
- Değişen: `Forms/Dashboard/MainForm.Dashboard.cs`, `Forms/Products/MainForm.Products.cs`, `Forms/Products/ProductEditor.cs`, `Controls/Dashboard/StockChart.cs`, `Form1.cs`.
- Sekiz kart, çift serili grafik, son hareket barkodu ve ayrı kritik stok tablosu; 1366×768 için oransal satır yükseklikleri.
- 75 satırlık sayfalama, sıralama, arama, filtre, resim, çoklu seçim, kolon görünürlüğü, sağ tık ve baskı/tasarım bağlantıları eklendi.
- Ürün eski fiyatı isteğe bağlıdır. Stok hareketlerine barkod kopyası eklendi; eski kayıtlar ürün barkodundan okunur veya boş gösterilir.
- Ekranlar görüntüye çizilerek kontrol edildi; alt tabloları kesen sabit yükseklikler düzeltildi. Eski stok kontrolleri korundu.

## Faz 4 — Yazıcı keşfi ve profiller
- Oluşturulan: `Models/Printing/PrinterProfile.cs`, `Services/Printing/PrinterSettingsService.cs`.
- InstalledPrinters + GetPrinter level 2 ile sürücü, port, Windows durum bilgisi ve DPI; profil JSON kaydı. Keşif arka planda.
- Altyapı build 0 hata/0 uyarı; 22 stok/UI kontrolü geçti. Donanım durumu yalnızca Windows bildirimi olarak etiketlenir.

## Faz 5 — Etiket modeli ve ölçüler
- Oluşturulan: `Models/LabelDesigner/LabelTemplate.cs`, `Helpers/PrinterUnitConverter.cs`, `Helpers/JsonStore.cs`, `Services/Validation/ValidationService.cs`, `Services/Templates/DynamicFields.cs`.
- Tek bağımsız, polimorfik model; mm ölçü, medya/sayfa yerleşimi, font, barcode ve görüntü özellikleri. UI kontrolü serialize edilmez.
- Değişen: `Models/Inventory/InventoryModels.cs` (isteğe bağlı OldPrice).
- 203/300/600 dönüşümleri, model roundtrip, sınır ve eski veri uyumu genişletilmiş test paketinde doğrulandı.
- Devam aşamasında üç kaynak dosyası ve bir derleme cache dosyasında NUL baytları bulundu. Bozuk kaynaklar `.corrupt-backup` olarak korundu; bilinen envanter modeli önceki tanımıyla geri getirildi, iki okunamayan yardımcı yerine yeni JsonStore/DynamicFields kullanıldı. Cache yeniden üretildi; ardından 0 hata/0 uyarı ve 22 eski kontrol geçti. Kullanıcı stok dosyası değiştirilmedi.

## Faz 6 — Tasarım çalışma alanı
- Oluşturulan: `Controls/Designer/LabelCanvas.cs`, `Forms/Designer/LabelDesignerForm.cs`.
- Araç kutusu / mm kağıt / özellik paneli; 11 hazır boyut ve özel ölçü; ürün örneği veya seçili ürünle tasarım.
- Form açılışı ve gerçek çizim bitmap'i kontrol edildi. Sonra DPI rasterıyla ekran önizlemesi eşleştirildi.

## Faz 7 — Düzenleme, cetvel ve geçmiş
- Oluşturulan: `Services/Templates/DesignerHistory.cs`.
- Değişen: `LabelCanvas.cs`, `LabelDesignerForm.cs`.
- Sürükleme, çoklu seçim, köşe resize, nudge, silme, kopyalama/yapıştırma/çoğaltma, z-order; 1/2/5 mm grid/snap ve mm cetvel.
- 60 snapshot, Ctrl+Z/Y; 35 geri alma ve gerçek form komutları test edildi. `Scale` üye gizleme uyarısı `PixelsPerMm` adıyla giderildi.

## Faz 8 — Çizim ve barkod öğeleri
- Oluşturulan: `Printing/Rendering/LabelPreviewRenderer.cs`; `NuGet.Config`.
- Değişen: proje paket referansı (ZXing.Net 0.16.11), model, designer.
- Metin, dinamik alan, büyük fiyat, QR, dokuz barkod formatı, resim/logo ve şekil. EAN checksum, barkod kutusunun modül genişliği kontrolü.
- Dokuz barkod formatı üretilip ZXing ile geri çözüldü; Türkçe QR dahil başarılı.

## Faz 9 — TSPL renderer
- Oluşturulan: `Printing/TSPL/TsplCommandBuilder.cs`, `Printing/TSPL/TsplLabelRenderer.cs`.
- mm/DPI hesapları, SIZE/GAP/BLINE/DIRECTION/REFERENCE/CLS ve ikili BITMAP/PRINT. Native TEXT/BARCODE/QRCODE/BOX/BAR yardımcıları da merkezî builder'da bulunur.
- Üretim yolu Türkçe ve font tutarlılığı için ortak raster çıktısını kullanır. TSPL'de LINE yerine BAR doğru şekilde kullanılır.
- Header/trailer, üç DPI, media, bitmap veri uzunluğu, komut enjeksiyon reddi ve raster piksel/polarite eşliği kontrol edildi.

## Faz 10 — RAW spooler
- Oluşturulan: `Printing/Windows/WindowsPrinterInterop.cs` (SafeHandle + RawPrinterService).
- Open/StartDoc/StartPage/Write/EndPage/EndDoc/Close merkezî P/Invoke. Kısmi yazım döngüsü; hata halinde AbortPrinter ve güvenli handle kapanışı.
- Build doğrulandı. Fiziksel yazıcıya veri gönderilmedi; gerçek spooler/hardware kabulü ayrı doğrulanmalı.

## Faz 11 — Windows Driver baskısı
- Oluşturulan: `Printing/Windows/WindowsPrintService.cs`.
- PrintDocument, mm'den hundredths-inch özel kağıt, hard-margin telafisi, mevcutsa DPI seçimi ve çok sayfalı çıktı.
- UI thread dışında yürütülür; sürücü kağıt/çözünürlük davranışı fiziksel kabul testine bağlıdır.

## Faz 12 — Ortak baskı önizleme
- Değişen: `LabelPreviewRenderer.cs`, `LabelCanvas.cs`, baskı formu.
- Sheet render satır/sütun yerleşimi, ofset ve ölçek uygular; baskı ve ekran aynı modeli/çizimi kullanır.
- 50×30 mm raster boyutları üç DPI'da, 2×5 yerleşim ve TSPL piksel eşliği test edildi.

## Faz 13 — Toplu barkod yazdırma
- Oluşturulan: `Forms/Printing/BarcodePrintForm.cs`.
- Arama/okutma, ürün sepeti, satır adetleri, adet çarpanı, yazıcı/şablon/DPI/media ve gerçek sayfa bazlı önizleme.
- En fazla 10.000 etiket; geçersiz adetler reddedilir. Yazıcı olmadığında önizleme açık kalır. Form kontrolü fiziksel baskı yapmaz.

## Faz 14 — Şablon yönetimi ve otomatik kayıt
- Oluşturulan: `Services/Templates/TemplateService.cs`.
- Değişen: `Forms/Printing/MainForm.Printing.cs`, designer.
- Yeni/kaydet/farklı kaydet/kopyala/sil/import/export; 7 hazır şablon. Görseller JSON'a gömülü.
- 1,6 saniye debounce ile ayrı taslak; yeni tasarımlar da Taslak kurtar ekranında erişilebilir.
- Atomic backup, preset render, taslak keşfi ve geri yükleme kontrol edildi.

## Faz 15 — Kalibrasyon ve ayarlar
- Oluşturulan: `Forms/Printers/PrintersForm.cs`.
- Değişen: `Forms/Settings/MainForm.Settings.cs`, yazıcı modelleri.
- Yazıcı bazında X/Y ofset, yatay/dikey ölçek, DPI ve baskı modu. Calibration özellik panelinde genişletilebilir.
- 50×30 mm test etiketi, 10 mm çizgi, merkez ve cihaz bilgisi. Signed offset ve ölçek sınırları test edildi.

## Faz 16 — Kuyruk ve hata yönetimi
- Oluşturulan: `Services/Printing/PrintQueueService.cs`.
- Değişen: `Forms/Printing/MainForm.Printing.cs`, `Program.cs`.
- Kalıcı Queued/Printing/Completed/Failed kayıtları, ürün/şablon anlık kopyası, seri arka plan işleri, tekrar baskı ve hata ayrıntıları.
- Completed yalnızca spooler teslimidir. Açılışta yarım kalmış işler otomatik basılmaz. Hata günlükleri AppData/logs altındadır.
- Başlangıç queue kaydı başarısızsa bellek kaydı geri alınır; native yazım hatasında job başarısız olur. Fiziksel teslim/kağıt/elektrik kesintisi test edilmedi.

## Faz 17 — Genişletilmiş kontroller
- Oluşturulan: `BarcodePrinter.Checks/PrintingChecks.cs`.
- Değişen: `BarcodePrinter.Checks/Program.cs` (eski 22 kontrol aynen korunarak yeni paket çağrılır).
- Testler kendi geçici inventory/template dosyalarını kullanır. Ana form testine veri yolu enjekte edilir; gerçek kullanıcı stoğu değiştirilmez.
- İlerleyen entegrasyon noktalarında 83 → 97 → 99 kontrol geçti. Son ek test TSPL raster piksel eşliğidir; nihai toplam aşağıda kaydedilir.

## Faz 18 — UI/UX ve yayın doğrulaması
- Değişen: dashboard oransal yerleşimi, ortak header seçim rengi, sidebar yazı boyutu, pencere çalışma alanı sınırı, baskı önizleme sayfası, README.md.
- Dashboard, ürünler ve designer görüntüleri gözle incelendi. Sidebar açık/kapalı, dark tema ve designer klavye komutları programatik kontrol edildi.
- Bağımlı fazların doğrulamaları bütünleşik çözüm üzerinde yapıldı; her ara faz için ayrı sürüm/commit üretilmedi. `TODO` veya NotImplementedException kullanılmadı.
- Production donanım kabulü tamamlanmış sayılmaz: fiziksel TSC ölçüsü, kağıt sensörü ve gerçek çok monitör DPI kontrolleri README'de açıkça belirtilmiştir.

## Nihai doğrulama sonucu
- `dotnet build BarcodePrinter.slnx`: başarılı, **0 hata / 0 uyarı**.
- `dotnet run --project BarcodePrinter.Checks`: **100 kontrol başarılı**; ilk 22 stok/UI kontrolü dahil.
- `dotnet publish BarcodePrinter/BarcodePrinter.csproj -c Release -o artifacts/BarcodePro`: başarılı.
- Çalıştırılabilir dağıtım: `artifacts/BarcodePro/BarcodePrinter.exe` (yanındaki DLL/runtime dosyaları birlikte tutulmalı; .NET 8 Desktop Runtime gerekir).
- Son build sırasında yeniden açılan debug uygulaması exe dosyasını kilitledi. Normal pencere kapatma sonrası build ve testler yeniden çalıştırılarak temiz sonuç alındı.
- Test ekran görüntüleri geçici `BarcodePro-checks-*` klasörlerinde oluşturuldu; kullanıcı envanterine örnek ürün eklenmedi ve fiziksel baskı gönderilmedi.

## Üst ribbon ve kompakt ikonlu arayüz güncellemesi
- Sol navigasyon kaldırıldı. Tüm ana bölümler üstte Giriş / Stok Yönetimi / Etiket ve Baskı / Raporlar / Yönetim altında gruplandı.
- Yeni dosyalar: Controls/Common/AppRibbon.cs, AppIcons.cs, AppContextMenu.cs.
- Ortak AppButton yaklaşık 28 px yükseklikte, 16 px DPI ölçekli çizgi ikonuyla kullanılıyor. İkonlar uygulama içinde vektör çizilir; font/emoji veya ücretli DevExpress bağımlılığı yoktur.
- Form1.cs, ThemeManager.cs ve AppControls.cs; mavi kurumsal renkler, kompakt toolbar ve kategori durumu için güncellendi.
- Tasarım araç kutusu üste taşındı. Canvas sağ tık menüsü kopyala/yapıştır/çoğalt/sil/katman/undo/redo/tümünü seç komutlarını çalıştırır.
- Ürün sağ tık menüsü korundu ve ikonlandırıldı; gridlerde hücre/satır kopyalama, baskı sepetinde kaldırma/önizleme komutları eklendi.
- Değişen diğer dosyalar: MainForm.Products.cs, MainForm.Dashboard.cs, LabelCanvas.cs, LabelDesignerForm.cs, BarcodePrintForm.cs, PrintingChecks.cs.
- 1366×768 Dashboard ve Designer görüntüleri incelendi. İlk kategoriye ait görünür panel sorunu tespit edilip düzeltildi ve testle korundu.
- `dotnet build BarcodePrinter.slnx`: 0 hata / 0 uyarı; Checks: 107 başarılı. Release dağıtımı artifacts/BarcodePro altında güncellendi.

## Hizalama ve yerleşim düzenlemesi
- Ribbon grup komutları serbest akış yerine iki satırlık TableLayoutPanel hücrelerine alındı; kategori ve komut genişlikleri ortaklaştırıldı.
- AppToolbar metin kutusu, seçim kutusu, etiket ve butonların satır merkezlerini aynı ölçüye bağlar; taşmada satırlar aynı aralıklarla devam eder.
- Yeni AppFieldGrid ve AppSection (Controls/Common/AppLayouts.cs): ürün ve stok formlarında sabit etiket sütunu, esnek editör sütunu, ortak satır yüksekliği.
- Toplu baskı ekranı üç başlıklı, kenarlı panele ayrıldı. Ürün ve sepet tablolarının üst/alt kenarları eşitlendi. Yazıcı seçenekleri kompakt etiket/editör satırlarına dönüştürüldü.
- Designer dosya komutları, görünüm seçenekleri ve nesne araçları ayrı düzenli üst satırlara ayrıldı. Başlangıç etiket boyutu seçim kutusunda görünür.
- Dashboard alt tablolarına aynı yükseklikte başlık satırları uygulandı. Hareket geçmişi araç çubuğu ortak hizalama bileşenine taşındı.
- Kontroller: 110 başarılı; ribbon hücrelerinin sınırları 1100, 1366 ve 1920 pencere genişliklerinde kontrol edildi. Dashboard, designer, ürün formu ve baskı ekranı görselleri incelendi.

## Referans ekranına göre kompakt modül menüsü
- Geniş ribbon ve ikinci komut paneli ana pencereden kaldırıldı. Kullanıcının ikinci ekran görüntüsündeki tek sıra ikon-üstte/yazı-altta yapısı esas alındı.
- Yeni `AppModuleMenu` ve `AppWorkspaceTabs`: Giriş, Ürünler, Stok, Etiket, Baskı, Raporlar, Yazıcılar, Ayarlar açılır menüleri; Kapat komutu ve sağda arama. Alt satır açık ekran sekmeleridir.
- Her modül gerçek işlem alt menülerine bağlandı. Sekmeler içerik ekranını yeniden açar; kaydedilmiş veriler ortak servisten güncel okunur.
- Dashboard giriş satırı, kartlar ve grafik sabit kompakt yüksekliklere alındı. Yeni ürün düğmesi pencere yüksekliğiyle büyümez; kart başlıklarının AutoSize/Dock çakışması giderildi.
- Değişen: Form1.cs, MainForm.Dashboard.cs, ThemeManager.cs, AppIcons.cs, PrintingChecks.cs. Yeni: Controls/Common/AppModuleMenu.cs.
- Eski ribbon testleri yeni modül/alt menü/sekme testleriyle değiştirildi: 108 kontrol başarılı. 1100/1366/1920 genişliklerde üst alanın iki kompakt satırda kaldığı doğrulandı; 1920 görüntü incelendi.

## Sade dashboard ve uygulama kimliği
- Dashboard: ortalanmış 1480 px çalışma alanı, sekiz kompakt ikonlu kart, sade haftalık grafik, hizalı işlem ve stok uyarısı tabloları; boş veri açıklamaları.
- AppWindow: ortak logolu başlık, taşıma, küçültme, büyütme/geri yükleme ve kapatma düğmeleri. Ana pencere ve AppDialog türevlerinde kullanılır; yerel yeniden boyutlandırma çerçevesi korunur.
- Segoe UI 9–9.5 pt ortak yazı tipleri, kompakt tablo satırları ve metin alanları.
- Assets/BarcodePro.ico (16–256 px) EXE simgesi; Assets/BarcodePro.png (512 px) logo. Yeniden üretim: dotnet run --project BarcodePrinter.Checks -c Release -- --brand-assets BarcodePrinter/Assets
- Release derlemesi ve yayın başarılı; 110 kontrol geçti. Başlığın menüyü örtmediği ve logo/simge varlığı da kontrol edilir. Görsel: docs/images/dashboard-refresh.png.

## DevExpress benzeri pencere teması ve MySQL aktarımı
- Yerel gri pencere kenarı yerine tek piksellik tema çerçevesi; başlık, menü, tablo kenarlıkları ve seçim renkleri ortak mavi-gri tema kullanır. Yeniden boyutlandırma, görev çubuğunu koruyan büyütme ve doğru ölçülere geri yükleme kontrol edildi.
- Ayarlar iki sekmeye ayrıldı: MySQL bağlantısı / Yerel veri ve yazıcı. Eşleştirme ve önizleme ayrı alt sekmelerdir.
- MySqlProductSource, MySqlConnectionPanel ve atomik Inventory.ImportProducts eklendi. Şifre DPAPI ile saklanır; kaynağa sadece SELECT yapılır. Bağlantı testi, iptal, zaman aşımı, önizleme ve isteğe bağlı stok yenileme bulunur.
- MySqlConnector 2.6.2 ve System.Security.Cryptography.ProtectedData 10.0.12 eklendi. Canlı sunucu bilgileri olmadığı için gerçek bağlantı testi bekliyor.
- 125 kontrol başarılı. Kurulum/alan kuralları: docs/MYSQL-CONNECTION.md. Görseller: docs/images/dashboard-refresh.png ve docs/images/mysql-settings.png.

## 2.1.0-beta.1 müşteri kurulum paketi
- Program başlangıcında ortalanmış LoginForm; sabit ve büyük/küçük harf duyarlı owner / owner. İptal veya kapatma ana uygulamayı açmaz.
- Ayarlar > Firma ve etiket: firma adı ve normalize edilen logo yerel company.json içinde saklanır. Yeni TTP-244CE şablonuna gömülür; mevcut tasarımlar değiştirilmez. Giriş ekranı kayıtlı firma kimliğini gösterir.
- TTP-244CE hazır şablonu 60x40 mm, 203 DPI, 2 mm aralıklı, Code128 barkodlu. Yazıcı profil komutu 203 DPI / Raw TSPL oluşturur; baskı ve önizleme 108 mm kafa sınırını doğrular. Kaynak: https://www.sembolbarkod.com/wp-content/uploads/2021/02/ttp-244-ce-brosur.pdf, teknik özellikler sayfası.
- .NET dahil win-x64 yayın, Inno Setup Türkçe kurulum, kullanıcı bazlı dizin, masaüstü/başlat menüsü kısayolları ve kaldırma desteği. Kurulum/kaldırma kullanıcı veri dizinine dokunmaz.
- Build: installer/Build-Setup.ps1. Kurulum testi: installer/Test-Setup.ps1. Paket: artifacts/installer/BarcodePro-2.1.0-beta.1-Setup-x64.exe; SHA256 dosyası aynı dizinde.
- 135 otomatik kontrol geçti; 467 yayın dosyasının kurulum sonrası SHA256 eşleşmesi, .NET dahil paket yapısı, kaldırma kaydı ve kaldırma doğrulandı. Temiz müşteri Windows ortamında çalıştırma, canlı MySQL ve fiziksel baskı henüz doğrulanmadı. Setup kod imzalama sertifikasıyla imzalanmadı.

## 2.1.0-beta.2 TSC PRN düzeltmesi
- PrinterRouting, TSC adı/sürücüsü ve TTP-244CE modelini kullanarak doğrudan Raw TSPL moduna yönlendirir. Eski profil kalibrasyonu korunur, 244CE için 203 DPI uygulanır.
- FILE:/PORTPROMPT: ve dosya yolu portları kuyruk ön kontrolünde ve RAW gönderimi başlamadan tekrar reddedilir. RAW DOC_INFO output file null olarak açıkça belirlenir. WindowsDriver yolunda PrintToFile=false ayarlanır.
- Baskı ekranında bağlantı portu, TSC için kilitli doğrudan baskı seçimi ve yanlış port açıklaması vardır. Windows sistemindeki portlar otomatik değiştirilmez.
- 151 kontrol başarılı. TSC seçim UI akışı, eski profiller, sürücüden algılama, fiziksel/yanlış portlar test edildi. Beta 2 kurulum/kaldırma ve 467 dosya bütünlüğü doğrulandı. Geliştirme bilgisayarında yalnızca sanal yazıcılar bulunduğundan fiziksel çıktı doğrulanmadı.
- Kaynak: https://learn.microsoft.com/en-us/dotnet/api/system.drawing.printing.printersettings.printtofile
