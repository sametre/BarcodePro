namespace BarcodePrinter.Services.Network;

public static class ServerDataMigration
{
    /// <summary>Creates the central database once. SQLite backup includes committed WAL records.</summary>
    public static string Migrate(string sourcePath, string targetRoot)
    {
        targetRoot = Path.GetFullPath(targetRoot);
        Directory.CreateDirectory(targetRoot);
        var destination = Path.Combine(targetRoot, "inventory.db");
        if (File.Exists(destination)) return destination;
        if (!File.Exists(sourcePath) && File.Exists(Path.ChangeExtension(sourcePath, ".json"))) sourcePath = Path.ChangeExtension(sourcePath, ".json");
        var temporary = Path.Combine(targetRoot, "migration-" + Guid.NewGuid().ToString("N") + ".db");
        try
        {
            if (File.Exists(sourcePath))
            {
                var source = new Inventory(sourcePath);
                source.BackupTo(temporary);
                var migrated = new Inventory(temporary);
                foreach (var product in migrated.Data.Products.Where(p => p.ImageData is not { Length: > 0 } && File.Exists(p.ImagePath)).ToList())
                {
                    // Preserve legacy local images under the service data directory.
                    var images = Path.Combine(targetRoot, "images"); Directory.CreateDirectory(images);
                    var imagePath = Path.Combine(images, product.Id.ToString("N") + Path.GetExtension(product.ImagePath));
                    if (!File.Exists(imagePath)) File.Copy(product.ImagePath, imagePath);
                    var updated = product.Copy(); updated.ImagePath = imagePath; migrated.SaveProduct(updated);
                }
                // Checkpoint by taking a second consistent backup, with no open destination connection.
                var finalized = temporary + ".ready";
                try { migrated.BackupTo(finalized); File.Move(finalized, destination, overwrite: false); }
                finally { if (File.Exists(finalized)) File.Delete(finalized); }
            }
            else
            {
                var empty = new Inventory(temporary); empty.BackupTo(destination);
            }
            return destination;
        }
        finally
        {
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            foreach (var suffix in new[] { "", "-wal", "-shm" }) if (File.Exists(temporary + suffix)) File.Delete(temporary + suffix);
        }
    }
}
