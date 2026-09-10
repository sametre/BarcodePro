using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using BarcodePrinter.Helpers;
using MySqlConnector;

namespace BarcodePrinter;

public sealed class MySqlSourceSettings
{
    public string Host { get; set; } = "";
    public uint Port { get; set; } = 3306;
    public string Database { get; set; } = "";
    public string Username { get; set; } = "";
    public string ProtectedPassword { get; set; } = "";
    public MySqlSslMode SslMode { get; set; } = MySqlSslMode.Required;
    public string Table { get; set; } = "products";
    public Dictionary<string,string> Columns { get; set; } = new()
    {
        [nameof(Product.Name)]="name", [nameof(Product.Barcode)]="barcode", [nameof(Product.Sku)]="sku",
        [nameof(Product.Price)]="price", [nameof(Product.Stock)]="stock",
        [nameof(Product.Cost)]="", [nameof(Product.Category)]="", [nameof(Product.Minimum)]="",
        [nameof(Product.Maximum)]="", [nameof(Product.Unit)]="", [nameof(Product.Description)]="",
        [nameof(Product.Active)]="", [nameof(Product.OnMenu)]="", [nameof(Product.OldPrice)]=""
    };
    public void SetPassword(string password) => ProtectedPassword = Convert.ToBase64String(ProtectedData.Protect(Encoding.UTF8.GetBytes(password), null, DataProtectionScope.CurrentUser));
    public string GetPassword() => string.IsNullOrEmpty(ProtectedPassword) ? "" : Encoding.UTF8.GetString(ProtectedData.Unprotect(Convert.FromBase64String(ProtectedPassword), null, DataProtectionScope.CurrentUser));
    public static MySqlSourceSettings Load(string path) => File.Exists(path) ? JsonStore.Read<MySqlSourceSettings>(path) : new();
    public void Save(string path) => JsonStore.Save(path,this);
}

public sealed class MySqlProductSource
{
    public const int MaximumRows=50000;
    private readonly MySqlSourceSettings settings;
    public MySqlProductSource(MySqlSourceSettings settings) => this.settings=JsonStore.Clone(settings);
    public static string Identifier(string name) => Regex.IsMatch(name,@"\A[A-Za-z_][A-Za-z0-9_]*\z") ? "`"+name+"`" : throw new InvalidOperationException("Tablo ve kolon adları harf, rakam ve alt çizgi içermeli; harf veya alt çizgiyle başlamalıdır.");
    public static Dictionary<string,string> ValidateMapping(MySqlSourceSettings settings)
    {
        var allowed=new MySqlSourceSettings().Columns.Keys.ToHashSet();
        var columns=settings.Columns.Where(x=>!string.IsNullOrWhiteSpace(x.Value)).ToDictionary(x=>x.Key,x=>x.Value.Trim());
        foreach(var required in new[]{nameof(Product.Name),nameof(Product.Barcode),nameof(Product.Sku)})
            if(!columns.ContainsKey(required))throw new InvalidOperationException("Ürün adı, barkod ve SKU kolonları zorunludur.");
        Identifier(settings.Table);
        foreach(var c in columns){if(!allowed.Contains(c.Key))throw new InvalidOperationException("Bilinmeyen ürün alanı.");Identifier(c.Value);}
        return columns;
    }
    public static string BuildSelect(MySqlSourceSettings settings, bool test=false)
    {
        var columns=ValidateMapping(settings);
        return "SELECT "+string.Join(", ",columns.Select(c=>Identifier(c.Value)+" AS "+Identifier(c.Key)))+" FROM "+Identifier(settings.Table)+" LIMIT "+(test?0:MaximumRows+1).ToString(CultureInfo.InvariantCulture);
    }
    private MySqlConnection Connection()
    {
        return new MySqlConnection(BuildConnectionString(settings));
    }
    public static string BuildConnectionString(MySqlSourceSettings settings)
    {
        if(string.IsNullOrWhiteSpace(settings.Host)||string.IsNullOrWhiteSpace(settings.Database)||string.IsNullOrWhiteSpace(settings.Username)||settings.Port is 0 or >65535)
            throw new InvalidOperationException("Sunucu, veritabanı, kullanıcı ve geçerli port girin.");
        if(!new[]{MySqlSslMode.VerifyFull,MySqlSslMode.VerifyCA,MySqlSslMode.Required,MySqlSslMode.Preferred,MySqlSslMode.Disabled}.Contains(settings.SslMode))throw new InvalidOperationException("Geçersiz TLS seçimi.");
        return new MySqlConnectionStringBuilder{Server=settings.Host.Trim(),Port=settings.Port,Database=settings.Database.Trim(),UserID=settings.Username.Trim(),Password=settings.GetPassword(),SslMode=settings.SslMode,ConnectionTimeout=10,DefaultCommandTimeout=30,AllowLoadLocalInfile=false,PersistSecurityInfo=false}.ConnectionString;
    }
    public async Task TestAsync(CancellationToken cancellationToken)
    {
        var sql=BuildSelect(settings,true);await using var connection=Connection();await connection.OpenAsync(cancellationToken);
        await using var command=new MySqlCommand(sql,connection);await using var reader=await command.ExecuteReaderAsync(cancellationToken);
    }
    public static string FriendlyError(MySqlException error,MySqlSourceSettings settings)
    {
        return FriendlyError(error.Number,AllMessages(error),settings);
    }
    public static string FriendlyError(int number,string details,MySqlSourceSettings settings)
    {
        string message=details.ToLowerInvariant();
        if(number==1042)return $"MySQL 1042: Sunucu bağlantıyı kuramadı veya istemci adresinin adını çözemedi. Bu çoğu zaman TLS hatası değildir. {settings.Host}:{settings.Port} erişimini, güvenlik duvarını, MySQL bind-address ayarını ve kullanıcının bu bilgisayarın IP adresinden bağlanma iznini kontrol edin.";
        if(message.Contains("certificate")||message.Contains("sertifika")||message.Contains("ssl")||message.Contains("tls")||message.Contains("authenticationexception"))
            return settings.SslMode==MySqlSslMode.VerifyFull?"TLS sertifikası doğrulanamadı. Hosting standart bir sertifika sunmuyorsa Güvenlik alanından ‘TLS zorunlu · Hosting uyumlu’ seçin. Bağlantı şifreli kalır; sunucu adı doğrulaması yapılmaz.":"TLS bağlantısı kurulamadı. Sunucuda TLS desteğini ve kullanılan MySQL portunu kontrol edin; yalnızca güvenilen yerel ağda TLS kapalı seçeneğini kullanın.";
        if(number==1045)return "MySQL 1045: Kullanıcı adı veya şifre hatalı ya da bu kullanıcıya uzak bağlantı izni verilmemiş.";
        if(number==1049)return "MySQL 1049: Veritabanı adı bulunamadı. Laravel .env dosyasındaki DB_DATABASE değerini kontrol edin.";
        if(message.Contains("timed out")||message.Contains("timeout"))return $"Bağlantı zaman aşımına uğradı. {settings.Host}:{settings.Port} dış ağdan erişilemiyor olabilir; hosting güvenlik duvarına bu bilgisayarın IP adresini ekleyin.";
        return $"MySQL bağlantısı veya sorgusu başarısız (kod {number}). Sunucu, port, kullanıcı izni, TLS modu, tablo ve kolonları kontrol edin.";
    }
    private static string AllMessages(Exception error){var parts=new List<string>();for(Exception? current=error;current!=null;current=current.InnerException)parts.Add(current.GetType().Name+": "+current.Message);return string.Join(" | ",parts);}
    public async Task<List<Product>> ReadAsync(CancellationToken cancellationToken)
    {
        var sql=BuildSelect(settings);await using var connection=Connection();await connection.OpenAsync(cancellationToken);
        await using var command=new MySqlCommand(sql,connection);await using var reader=await command.ExecuteReaderAsync(cancellationToken);
        var result=new List<Product>();
        while(await reader.ReadAsync(cancellationToken))
        {
            if(result.Count==MaximumRows)throw new InvalidOperationException("50.000 ürün sınırı aşıldı. Daha dar bir veritabanı görünümü (VIEW) seçin; hiçbir ürün aktarılmadı.");
            var values=new Dictionary<string,object?>();for(int i=0;i<reader.FieldCount;i++)values[reader.GetName(i)]=reader.IsDBNull(i)?null:reader.GetValue(i);
            try{result.Add(MapRow(values));}catch(Exception ex)when(ex is FormatException or OverflowException or InvalidOperationException){throw new InvalidOperationException($"{result.Count+1}. ürün satırındaki alan biçimi geçersiz. Sayı ve durum kolonlarını kontrol edin.");}
        }
        return result;
    }
    public static Product MapRow(IReadOnlyDictionary<string,object?> values)
    {
        var product=new Product();
        foreach(var pair in values)
        {
            var property=typeof(Product).GetProperty(pair.Key)??throw new InvalidOperationException("Bilinmeyen ürün alanı.");
            if(pair.Value==null){if(property.PropertyType==typeof(string))property.SetValue(product,"");else if(property.PropertyType==typeof(decimal?))property.SetValue(product,null);else throw new FormatException("Sayısal veya durum alanı NULL olamaz.");continue;}
            object value=property.PropertyType==typeof(string)?Convert.ToString(pair.Value,CultureInfo.InvariantCulture)!.Trim():property.PropertyType==typeof(bool)?ParseBoolean(pair.Value):Convert.ToDecimal(pair.Value,CultureInfo.InvariantCulture);
            property.SetValue(product,value);
        }
        return product;
    }
    private static bool ParseBoolean(object value) => Convert.ToString(value,CultureInfo.InvariantCulture)?.ToLowerInvariant() switch
    {
        "1" or "true"=>true,"0" or "false"=>false,_=>throw new FormatException("Durum 0/1 veya true/false olmalıdır.")
    };
}
