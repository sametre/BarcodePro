R3 M-KOBI 2.1.0
================

Install the Server package on the computer that owns the shared inventory.
Install the Client package on the other Windows computers.

LOGIN
-----
Application login: owner / owner
The network access key is displayed by the Server administration window.

LICENSE
-------
Before the first start, select the license.json created by the private local
R3LicenseGenerator and enter its six-digit number. The Server license is kept
under %PROGRAMDATA%\R3-M-Kobi and the Client license under
%LOCALAPPDATA%\R3-M-Kobi. Expired licenses stop the application and service.

SQL IMPORT
----------
Server Administration > Settings > SQL table import accepts products INSERT
exports. Select the .sql file, preview it, optionally choose an image folder,
then import the products into central SQLite storage.

PRINTING
--------
The TSC TTP-244CE profile uses direct TSPL output, Code 39 Full ASCII,
0.50 mm X dimension and Windows-1252. Select the physical USB or network
printer port; FILE and PRN destinations are rejected.

NETWORK
-------
The Server uses TCP 5088 and UDP 5089 discovery. The installer creates the
Private and Domain firewall rules and writes local connection information.
