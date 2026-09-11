# R3 M-Kobi Server and Client

Install Server on the Windows computer that owns the inventory. The installer registers the `BarcodeProServer` service with delayed automatic start and recovery. Closing the administration window does not stop the service.

The central database is `%PROGRAMDATA%\BarcodePro\Server\inventory.db`. SQLite WAL mode records stock changes and movement history in one transaction. Clients use the Server API and never open the SQLite file as a network share.

## SQL product import

Open **Server Administration → Settings → SQL table import**, select a `.sql` export containing `products` INSERT statements, preview the rows, optionally select an image folder or image base URL, then choose **Import to SQLite**. Barcode and SKU matches update existing products; new rows are added. Existing stock is preserved unless the stock option is enabled. Imports are transactional.

## Client connection

1. Install Client on the other computers.
2. Use automatic discovery over UDP 5089, or enter the Server address shown by the administration screen, for example `http://HOSTNAME:5088`.
3. Enter the Server access key and test the connection.
4. Product and stock operations are then written to the central SQLite database.

The application login is `owner` / `owner`. This is separate from the network access key.

## Licensing

Both editions require a valid `license.json`. The private local generator creates a random six-digit number and expiry date. Copy the file to the activation screen and enter the number. Server licenses are stored under `%PROGRAMDATA%\R3-M-Kobi`; Client licenses are stored under `%LOCALAPPDATA%\R3-M-Kobi`. An expired license stops the application and the Server service.

## Network and permissions

Server setup requires administrator permission. The installer creates TCP 5088 and UDP 5089 firewall rules for Private and Domain profiles and prints the local and external addresses in the Server information file. Use a VPN or HTTPS gateway for internet access.
