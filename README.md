# Roselie's Beauty Lounge POS

A native Windows 10/11 x64 desktop application built with C#/.NET 10 and WPF. Version 0.2.1 includes the salon interface redesign, branded dialogs and receipts, automatic full-screen startup, account security settings, personal PIN login, and Super PIN protected catalog/expense deletion. It runs locally without a browser, server, cloud login or payment gateway. Development dependency restore requires internet once; the published self-contained app and installer include the runtime and dependencies for offline operation.

## Open the application

1. Run `artifacts/installer/Roselie.Setup.exe` to install for the current Windows account, or run `artifacts/app-v0.2.1/Roselie.POS.exe` directly. `Run-Roselie-POS.bat` opens that portable release from this project folder. The installer uses a white logo for the Desktop shortcut; the application and Start Menu keep their regular icons.
2. Create your owner username and a password containing at least 12 characters.
3. Save the one-time recovery code away from the computer.
4. Fill in Settings → Business and Printer. Test the printer.
5. Review the 40 supplied service prices and 21 prepaid 5+1 offers. Prices, durations and materials are editable. Confirm the catalog with the owner.
6. Encode real products and suppliers. Use Inventory → Stock in to enter opening quantities and unit costs. Configure service consumables and employee accounts before checkout.
7. Configure scheduled backups and create a portable, passphrase-protected backup on a separate drive.

For an update, close the running POS and run the new installer. Existing business data remains in its separate data folder; security/schema updates are applied automatically. Keep a portable backup before updating a live workstation.

The production database starts with the supplied catalog and system configuration only. Customers, products, employees, appointments and sales start empty. Test accounts and sample transactions are confined to test and UI-smoke data folders under `artifacts` or the test runner's temporary directory.

## Accounts, PIN login and protected deletion

Settings → User security lets an administrator update their profile/display name, change their own password by confirming the current password, manage their sign-in PIN, and create or manage employee login accounts. Create an active employee profile first, then use **Add employee account** to assign its username, password, role, discount limit and custom-price permission. Each employee has one linked account; reset or reactivate that existing account rather than creating another.

For quick sign-in, use **Settings → User security → Manage my sign-in PIN**. The administrator's sidebar account name/avatar opens the profile/name settings. Confirm your current password and enroll a personal, unique 6–12 digit PIN. At sign-in, select **PIN** and use the number pad; the PIN identifies its account without a username field. Password login remains available. Password changes, recovery and administrator password resets remove enrolled sign-in and Super PIN credentials. PINs are protected for the current Windows account and should be enrolled again after moving a backup to another workstation.

Administrators can create/change a separate **Super PIN** in User security by confirming their current password. Select a record in Service Catalog, Packages → Package offers, or Expenses and use its **Delete** action. The branded confirmation requires a deletion reason and this masked 6–12 digit Super PIN. If it has not been enrolled, the dialog offers **Create Super PIN** before deletion can continue. The sign-in PIN does not authorize deletion.

Deleted services and package offers disappear from the catalog and future purchases while existing transaction history and purchased customer sessions remain available. Deleting an expense voids it, retains its original record and audit reason, and excludes it from active expense/profit calculations. Voiding a linked commission payout makes that accrued commission payable again; earned commission costs remain in profit reporting.

## Full-screen mode

The app opens in full screen automatically and stays in full screen through sign-out. Press F11 or use the top-bar full-screen button to toggle modes; Esc restores the window when full screen is active. The navigation button switches between the full sidebar and an icon rail. Settings → Appearance includes reduced motion and table density.

## Receipt printer setup

Install the printer's Windows driver once and connect/power on the printer. Driver installation may require its vendor installer or a downloaded driver; the POS itself operates offline afterward. For Epson printers, use the official [TM-U220 driver download page](https://download-center.epson.com/softwares/?device_id=TM-U220&language=en&os=WIN1164&region=US) or [TM-U220II driver download page](https://download-center.epson.com/softwares/?device_id=TM-U220II&language=en&os=WIN1164&region=US), selecting the exact model and Windows version.

Settings → Printer detects installed Windows printer queues and offers **Refresh printers**, driver/status details, and **Test print receipt**. Automatic selection prefers a single Epson TM-U220, then the default physical printer, then the only physical printer. Select the desired queue explicitly when several are available.

Epson TM-U220/TM-U220II uses an impact ribbon and 57.5, 69.5 or 76 mm paper, as documented in Epson's [TM-U220 reference guide](https://files.support.epson.com/pdf/pos/bulk/tm-u220_trg_en_std_revh.pdf) and [TM-U220II reference guide](https://files.support.epson.com/pdf/pos/bulk/tm-u220ii_trg_en_rev_c.pdf). Automatic paper profiling uses 76 mm for a detected TM-U220; choose a manual width if the installed roll differs, and match the driver and paper guide. The POS also has 58/80 mm receipt profiles. Windows queue status and rendered previews do not prove physical output; verify paper, ribbon, alignment and cutting with the test receipt on the salon's actual printer.

## What is implemented

- First-run owner creation, Argon2id passwords, personal PIN login, administrator profile/password settings, employee account management, separate Super PIN protected deletions, offline recovery, configurable lockout/session timeout and backend role enforcement.
- SQLCipher-encrypted SQLite storage, Windows DPAPI-protected random database keys, foreign keys, uniqueness and financial constraints.
- Dashboard from recorded sales, expenses, refunds, commissions and inventory costs.
- Services, categories, pricing rules, charge presets, materials and packages.
- Products, suppliers, categories, barcode keyboard input, retail/internal-use inventory, weighted-average costs and movement history.
- POS with Service Details & Pricing, unlimited named charges, item or transaction percentage discounts, assigned employee and additional material usage.
- Cash, manually recorded GCash/transfer/card, split payments, exact change, atomic stock deduction and duplicate-checkout protection.
- Historical receipt snapshots, two-column transaction summary and paper preview, printer detection/test printing, 57.5/58/69.5/76/80 mm Windows receipt profiles, raw ESC/POS printing and authorized reprints.
- Full-sale refunds and voids with reasons, optional retail stock return and appropriate cost treatment.
- Customers, anonymous walk-ins, visits, purchase histories and package balances.
- Daily/weekly/monthly appointment calendar, editing, status changes, staff assignment and overlap validation.
- Employees, account roles, permission limits, fixed/percentage/per-service commissions and linked commission payouts.
- Expenses, prepaid package purchases, six-session balances, redemption histories and expiration.
- Fifteen report selections, date filters, print preview and PDF/Excel/CSV exports.
- Business/printer/appearance/database settings, audit history, encrypted portable backups, integrity checks, scheduled backups and verified restore with a retained safety backup.
- A self-contained per-user Windows installer with file verification, desktop/Start Menu shortcuts, update rollback and uninstall registration.

## Financial policies

Use decimal arithmetic. Round settlement money to two decimal places, midpoint away from zero. Persist money as integer centavos; quantity and weighted unit-cost fields use scaled integers with six decimal places. Percentage discounts allow two decimals and values from 0 to 100.

Additional charges are per unit and follow the line quantity. Use either line discounts or a transaction discount; stacking is rejected. Allocate transaction discounts to lines in exact cents. Only cash overpayment produces change. Noncash overpayment and insufficient payment are rejected. Financial snapshots survive catalog edits.

Revenue is recognized on prepaid package purchase. Redemptions add material and commission costs without recording another paid sale. This is the initial management accounting policy, not an automatically configured statutory revenue/tax policy.

Profit equals net sales minus retail COGS, service material costs, earned commissions, inventory writeoffs and operating expenses. Inventory purchases are assets until consumed/sold. Commission payouts reference earned records and are not charged to profit twice. Full refunds retain used material costs; paid commissions remain costs. Profit is marked provisional when configured cost information is incomplete.

All business dates use Philippine time (UTC+8), independent of the Windows timezone.

## Build and verify

Install the .NET 10 SDK (10.0.401 or a compatible later feature band), or open `Roselie.sln` in a Visual Studio version that supports .NET 10 desktop development.

```powershell
pwsh -File scripts/build.ps1
pwsh -File scripts/publish.ps1
```

`build.ps1` restores packages, builds Release and runs automated tests. `publish.ps1` runs those checks, creates a self-contained x64 app, calculates payload SHA-256 hashes and builds the installer. `-SkipTests` is available for publishing a build already tested in the current session. With all dependency/runtime packages already cached, `publish.ps1 -SkipTests -Offline` publishes without an online NuGet audit; run a connected dependency audit before commercial release.

To validate the UI in a **new isolated folder**, without changing production data:

```powershell
dotnet src/Roselie.App/bin/Release/net10.0-windows/Roselie.POS.dll --ui-smoke artifacts/ui-smoke-new
```

The smoke runner creates its own owner, renders all pages at several sizes, opens pricing/payment/receipt and management dialogs, checks calendar modes, receipt pagination, full-screen restoration and repeated password/PIN sign-ins. It writes screenshots, schema and `smoke-results.json`. The runner uses test transactions only in its isolated database. Do not reuse a previous smoke folder. See `docs/UI-VALIDATION.md` for the completed audit and verification scope.

To verify the bundled installer payload without installing:

```powershell
artifacts/installer/Roselie.Setup.exe --verify-payload artifacts/installer-check
```

## Business data and recovery

Normal business files live at `%LOCALAPPDATA%\RoselieBeautyLounge`, separate from application binaries. The saved `salon.db` database and its Windows-protected `database.key` persist across app closes, Windows restarts, application updates and uninstall. Reopening under the same Windows account loads the same records instead of creating a new salon. The encrypted database needs its key; do not delete either file. Each Windows account/computer has its own local data folder.

Portable `.rblbackup` files contain the encrypted database and a passphrase-protected key. They can be restored on another Windows account through Settings → Database after creating a temporary first-run owner. Restore validates checksums, encryption, SQLite integrity, foreign keys and login records before replacing the database. It keeps a `BeforeRestore-*` encrypted safety backup and ends the current session. Sign in using the accounts from the restored backup, then review printer and backup paths on the new computer.

Scheduled backups run while the application is open. A saved scheduled passphrase is DPAPI protected. Without a scheduled passphrase, automatic backups are tied to the current Windows account; keep separate portable backups as well. Failed automatic backup status is recorded in `backup-status.txt` and shown in Settings. Passwords and database keys are never hardcoded in the production application.

## Deployment testing still required

This build is a deployment-testing candidate. Automated tests and UI rendering cannot guarantee the absence of every error. Run the acceptance checklist in `docs/ACCEPTANCE.md` using the salon's actual Windows workstation, barcode scanner and printer before live sales.

Receipt output is labelled as an internal sales receipt. No BIR accreditation, VAT/statutory discount rules or official invoice compliance is claimed. Those rules require the owner's actual registration information and applicable review. Medical/treatment permissions are not established by entering a service in this catalog.

Refunds currently cover complete transactions. Partial item refunds and partially used package refunds need a further agreed policy and workflow. The installer is unsigned; commercial distribution should include code signing. Inventory is maintained per product; expiration is tracked at product level rather than separate purchase batches. PDF export uses a basic Latin built-in font and labels currency PHP; non-Latin customer text requires an embedded-font PDF extension. Existing customer images are stored as local file paths; move image assets with the workstation if needed.

## Structure

- `src/Roselie.Core`: entities, supplied catalog and financial calculation engine.
- `src/Roselie.Infrastructure`: EF Core schema, encrypted database, authentication, CRUD, transactions, reports, receipts, backups and audit.
- `src/Roselie.App`: WPF theme, MVVM commands, application shell, POS and management screens.
- `src/Roselie.Setup`: native installer and uninstaller.
- `tests/Roselie.Tests`: financial, authentication, encryption, transaction, inventory, package, refund, backup and export tests.
- `requirements.md`: the original supplied specification.

Library behavior was checked against [Microsoft's SQLite provider documentation](https://learn.microsoft.com/en-us/ef/core/providers/sqlite/limitations) and the [official SQLCipher API](https://www.zetetic.net/sqlcipher/sqlcipher-api/).
