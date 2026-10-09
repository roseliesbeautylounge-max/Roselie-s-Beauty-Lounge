# Interface and functionality audit — version 0.2.1

Verified on 9 October 2026. All UI runs use a fresh SQLCipher database under `artifacts`; production customer, stock, account and transaction data is not used for testing.

## Changes delivered

| Area | Implemented behavior |
| --- | --- |
| Shared controls | Dusty rose buttons, rounded fields/dropdowns/date pickers, calendar modes, slim scrollbars, tooltips, focus/disabled states and reduced motion |
| Shell | Automatic fullscreen, grouped icon navigation, compact rail, editable profile name, vector sign-out icon, Desktop-only white logo, Philippine-time clock and F11/Esc restoration |
| POS | Service/product/package modes, category chips, aligned card actions, Current Transaction, quantity controls, Service Details & Pricing, charges/discount breakdown and payment validation |
| Modals | Branded reusable header/close control, consistent padding and fixed footers; long forms/body content can scroll without hiding actions |
| Receipt | Two-column saved transaction summary/paper preview, aligned amounts, printer-friendly logo, 57.5/58/69.5/76/80 mm Windows documents, saved text fallback and audited reprints |
| Management | Shared search/filter/table/card layouts, meaningful empty states, visible selection-dependent actions and structured forms |
| Appointments | Day/week/month views, six-week month calendar, day selection, visit filters and existing staff conflict validation |
| Settings | Seven sections with persistent navigation, fixed Save actions, unsaved-edit warning, logo controls, security/PIN settings and backup operations |
| Accounts | Password access, username-free number-pad PIN login, unique personal PINs, editable owner profile, password change and employee account forms, Windows protection and persisted lockout |
| Protected deletion | Separate administrator Super PIN, reason and inline validation; deleted catalog entries/voided expenses preserve transaction history and audit data |
| Printer | Installed queue discovery/refresh, readiness checks, automatic Epson TM-U220 profile, Windows and ESC/POS output; failed RAW finalization does not record a successful print |
| Persistence | FULL synchronous writes, complete WAL checkpoints, protected backup targets, recoverable database/key replacement and independent process reopening verification |

## Defects corrected during the audit

- A reused toast panel remained attached to an earlier shell after sign-out. The new shell now detaches it before reuse. The smoke run signs out and back in with PIN, then repeats password sign-in twice.
- Missing or incorrectly scoped foreground resources caused `DependencyProperty.UnsetValue` errors in employee and transaction views. Shared dictionaries now provide valid palette resources.
- Native/default dropdown and calendar appearances, tight action spacing, oversized receipt actions and misaligned service-card buttons were replaced with shared styles and appropriate layouts.
- Catalog editing rejected seeded services whose duration was zero. The form now accepts the persisted valid range.
- Search/filter changes could leave a hidden record selected and its contextual actions enabled. Selection now follows the visible list.
- Table header double-clicks could open the selected row for editing. Row editing now requires a row target.
- Lookups loaded after bindings could miss their selected value. They now load before editor bindings; existing inactive appointment references remain available for preserving saved visits.
- Editing expenses could lose a stored custom category or its capitalization. Existing values are retained.
- Single-choice filters are hidden; appointment, package, expense and commission filters perform actual filtering.
- The interface timeout used a hardcoded value. It now uses the persisted authentication setting.
- Restoring the expanded sidebar added margins that forced unnecessary scrolling. Expanded spacing now matches the initial layout.
- FlowDocument receipt table columns used Grid-style star sizing, which wrapped saved identifiers vertically. Explicit paper-relative widths now keep metadata and amounts inside thermal margins.
- Calendar year/decade grids required explicit mode visibility triggers and the correct four-column/three-row layout. Weekday headers use the template resource expected by WPF.

## Automated evidence

- Release solution build: zero warnings and zero errors.
- Regression suite: **103 passed, 0 failed, 0 skipped**, including 21 PIN security/migration cases. Evidence: `tests/Roselie.Tests/TestResults/ui-pin-validation.trx`.
- The final isolated UI run passed **55 checks** and produced 54 screen/dialog captures plus two printer-page renders. Evidence is recorded in `artifacts/ui-final-02/smoke-results.json`, with screenshots and generated database schema.
- All twelve modules render at 1366×768. Dashboard, POS, Reports and Settings additionally render at 1600×900 and 1920×1080. POS, Appointments and Settings render at 1080×700.
- Additional captures cover all Settings sections, the icon rail, appointment day/week/month, six management forms, pricing, payment, receipt, transaction history with a selected sale, PIN enrollment and PIN sign-in.
- Assertions cover full-screen entry/restoration; a committed PHP1,035 sale with PHP65 change; 58/80 mm receipt pagination and saved values; legacy receipt fallback; PIN sign-in after sign-out; and repeated password sign-in without a parent-reuse exception.
- Source audit confirmed visible commands invoke actual business, navigation or printing operations. Disabled contextual actions require a visible selection or the appropriate role. Payment saving prevents a second commit while processing and preserves retry identity after a failed save.

## Data and verification limits

Additive schema updates retain existing accounts, records and financial snapshots. PIN and Super PIN verifiers are Argon2id-derived and protected by Windows DPAPI for the current Windows account. A separate protected lookup key identifies unique sign-in PINs without storing the digits. Portable backups restored under a different account require password access and PIN re-enrollment. Receipt formatting reads saved receipt values and does not recalculate historical amounts. Saved records remain in the encrypted AppData database across app/PC restarts; the smoke audit reopens its isolated database in a separate process and compares persisted table values.

UI rendering verifies layout and document pagination; it cannot establish paper output, cutter behavior, scanner behavior or Windows DPI/font behavior on every workstation. Windows/ESC-POS printing still needs the actual salon printer. Complete `ACCEPTANCE.md` on the deployment workstation before live sales.

WPF template behavior was checked against the primary [CalendarItem implementation](https://github.com/dotnet/wpf/blob/main/src/Microsoft.DotNet.Wpf/src/PresentationFramework/System/Windows/Controls/Primitives/CalendarItem.cs).
