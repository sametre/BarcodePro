# R3 M-Kobi 2.1.0

## What is included

- A persistent Windows Server service with central SQLite storage.
- A LAN Client with automatic discovery, access key authentication and synchronized stock operations.
- Product CRUD, stock ledger, dashboard, label designer and SQL `products` import.
- Direct TSC/TTP-244CE TSPL printing with the configured Code 39 Full ASCII profile.
- Compact gray top navigation, transparent R3 branding and small aligned controls.
- Six-digit expiring license activation for Server and Client.

## Installation

Install Server on the host computer and Client on other computers. Both setup programs request Windows administrator permission. The application login is `owner` / `owner`; the Server access key is shown in its administration screen.

The customer Server package contains the 597 imported products and opening stock seed. Generic public packages contain no customer inventory. Existing Server data is preserved during upgrades.

## Validation

The release passes 191 automated checks covering SQLite transactions, SQL import, image embedding, barcode validation, TSC routing, printer ports, Server/Client synchronization and network access control.
