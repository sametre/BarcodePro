using BarcodePrinter.Services.Licensing;

var days = args.Length > 0 && int.TryParse(args[0], out var parsed) ? parsed : 365;
var output = args.Length > 1 ? Path.GetFullPath(args[1]) : Path.Combine(Environment.CurrentDirectory, "license.json");
var license = LicenseService.Create(days);
Directory.CreateDirectory(Path.GetDirectoryName(output)!);
File.WriteAllText(output, System.Text.Json.JsonSerializer.Serialize(license, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
Console.WriteLine($"R3 M-Kobi license created (6 digits): {license.Key}");
Console.WriteLine($"Expires (UTC): {license.ExpiresAtUtc:O}");
Console.WriteLine($"File: {output}");
