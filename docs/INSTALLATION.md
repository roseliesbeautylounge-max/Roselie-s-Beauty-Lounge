# Install or open Roselie's POS

Version **0.2.1** is supplied as one installer: **Roselie.Setup.exe**. Copy it to the other PC with a USB drive. The PC needs Windows 10 or 11, 64-bit. Internet, Visual Studio and a separate .NET installation are not required to install or run the POS.

1. Close Roselie's POS if it is already running.
2. Double-click **Roselie.Setup.exe**.
3. Keep the suggested folder and **Create desktop shortcut** checked. Click **Install**.
4. Click **Open Roselie's POS**, or double-click the white Roselie's Beauty Lounge logo on the Desktop later. A shortcut with the regular application icon is also in the Start Menu.

On a new PC, create the owner account with a password of at least 12 characters and save the one-time recovery code separately.

The app opens directly as a Windows application in full screen. You do not need a terminal or build commands. F11 toggles full screen; Esc restores the window when full screen is active. Signing out keeps the full-screen display.

## Owner and employee accounts

Open **Settings → User security** to update the administrator profile/display name, change your password, manage a sign-in PIN, or add employee login accounts. Changing your own password requires the current password.

For quick access, sign in with your password, open **Settings → User security → Manage my sign-in PIN**, and enroll a personal, unique 6–12 digit sign-in PIN. The administrator's sidebar account name/avatar opens profile/name settings. On the next sign-in, choose **PIN** and enter it on the number pad. PIN sign-in has no username field. Password access remains available; password changes, account recovery or an administrator password reset removes enrolled sign-in and Super PIN credentials.

To add an employee login, create an active profile in **Employees**, then choose **Settings → User security → Add employee account**. Select that employee, enter its username/password and role, and set permitted discounts/custom pricing. **Employee login accounts** lets the administrator edit, reset, disable or reactivate existing accounts. Keep one linked login per employee; use the existing account when the employee already has one.

## Protected Delete actions

In **Settings → User security**, create a separate 6–12 digit **Super PIN** by confirming your administrator password. It can be changed there later. Your sign-in PIN is a different credential.

Select a service in **Service Catalog**, an offer in **Packages → Package offers**, or an **Expense**, then choose **Delete**. Enter the reason and masked Super PIN in the branded confirmation. Without a Super PIN, the dialog offers **Create Super PIN** before permitting deletion.

Service/package deletion removes the catalog entry from future purchases and preserves existing transaction history and prepaid customer sessions. Expense deletion voids the entry, preserves its original record and audit reason, and excludes it from active spending/profit calculations. A voided commission payout becomes payable again; the accrued commission cost remains recorded.

## Receipt printer

The POS detects printers already installed in Windows; install the vendor's Windows driver before using it. If needed, download the driver on a connected computer and transfer its installer. Use Epson's official [TM-U220 driver page](https://download-center.epson.com/softwares/?device_id=TM-U220&language=en&os=WIN1164&region=US) or [TM-U220II driver page](https://download-center.epson.com/softwares/?device_id=TM-U220II&language=en&os=WIN1164&region=US), choosing the actual model and Windows version.

Connect and power on the printer, then open **Settings → Printer**. **Refresh printers** reloads installed queues. Automatic selection prefers a single Epson TM-U220, the default physical printer, or the only physical printer; select a queue explicitly when necessary. Review the detected status and use **Test print receipt** before live sales.

TM-U220/TM-U220II is an impact ribbon printer with **57.5 / 69.5 / 76 mm** paper options. See Epson's [TM-U220 guide](https://files.support.epson.com/pdf/pos/bulk/tm-u220_trg_en_std_revh.pdf) and [TM-U220II guide](https://files.support.epson.com/pdf/pos/bulk/tm-u220ii_trg_en_rev_c.pdf). Automatic profiling uses 76 mm; disable automatic paper profiling and select the actual roll width when different. Match the Windows driver and paper guide. Other receipt profiles include 58 and 80 mm. Printer status/preview rendering is not physical verification: check the printed receipt, ribbon, alignment and cutting on your actual device.

## Records survive closing and restarting

Your records are saved in `%LOCALAPPDATA%\RoselieBeautyLounge\salon.db`; its protected key is `%LOCALAPPDATA%\RoselieBeautyLounge\database.key`. These files live separately from the installer/application folder. Closing the app, restarting Windows, updating or uninstalling the POS does not erase them. Reopen the POS under the same Windows account to continue with the saved salon. Do not delete the database or its key. Each computer/Windows account keeps its own local database.

## Move your existing salon records

1. On the old PC, open **Settings → Database** and create a portable encrypted backup. Save its passphrase separately.
2. Copy the `.rblbackup` file to the new PC.
3. Install/open the POS on the new PC and create a temporary owner account.
4. Use **Settings → Database → Restore** to select the backup and enter its passphrase.
5. Sign in using the accounts from the restored backup. Re-enroll sign-in/Super PINs and review printer/backup paths on the new PC.

Do not copy only the encrypted database file: its Windows-protected key belongs to the old account. Use portable backup/restore. Other PCs run their own local database; changes are not automatically synchronized between computers.

## Update an existing installation

Close the POS, keep a portable backup, then run the new installer. It uses the registered installation folder and updates application files. Business records and backups are kept separately. Uninstall also preserves business data.

The installer is currently unsigned. Windows may show a publisher warning depending on the PC's security settings. Do not turn off Windows protection; use a trusted copy of the installer and follow the PC administrator's policy.
