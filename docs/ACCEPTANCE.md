# Salon acceptance checklist

Use a separate test Windows account or isolated test database and test products. Record the result, workstation/printer model, date and tester before approving live operation.

## Installation and offline operation

- Install with networking disconnected on Windows 10/11 x64 without a preinstalled .NET runtime.
- Verify desktop and Start Menu shortcuts, first-run setup, account creation, update with the app closed and uninstall preserving business data.
- Launch twice and confirm the second instance cannot access the same production folder.
- Navigate each page at 1080×700, 1366×768, 1600×900 and 1920×1080, and at the workstation's actual DPI setting.
- Confirm the official logo appears in the title bar, desktop shortcut and branded dialogs.
- Use F11 to enter/exit full screen and Esc to restore the previous window. Expand/collapse the sidebar.
- Check keyboard Tab/Enter/Esc, dropdowns, date selection and calendar Month/Year/Decade views. Enable reduced motion and confirm controls still work.

## Accounts and access

- Save and test the owner recovery code. Verify it rotates after recovery.
- Fail five logins and verify lockout. Test inactivity logout, including time spent in dialogs.
- Create a cashier and staff account. Check page visibility and actual write/report/refund/reprint permissions.
- Disable an account and confirm an existing session cannot continue writing.
- Sign out and sign back in several times with the correct password; the dashboard must reopen without a reused-parent error.
- In Settings → User security, change the administrator profile name and reopen the app; confirm the sidebar/dashboard use it and the login username remains unchanged. Change your password and sign back in. Create a linked employee account and check its role and permissions.
- Enroll a unique 6–12 digit PIN in User security using the current password. Sign out and use the PIN number pad without a username field; also test password login, disabling PIN, unknown-PIN lockout, and lockout across both methods.
- Enroll a separate administrator Super PIN. Delete a test service, package offer and expense using a reason and the Super PIN. Reject an incorrect PIN; retain earlier sales/customer package sessions and the voided expense audit. Confirm voided expenses leave active expense/profit calculations.
- After owner recovery, administrator password reset or a backup move to a different Windows account, verify the account requires password access and PIN re-enrollment.

## Products and inventory

- Add a real product/SKU/barcode and an internal-use supply. Encode opening stock through Inventory.
- Receive two purchases with different costs and verify weighted average and inventory value.
- Verify stock-in/out/count movements have user, reason and exact balances.
- Assign consumables to a service. Test normal and extra consumption and expired/insufficient materials.
- Confirm missing cost data generates a provisional-profit warning.
- Scan a barcode with a keyboard-wedge scanner. Check quantity changes and out-of-stock handling.

## Checkout and receipts

- Hair Color PHP500 + charges300/200/150,10% discount must total PHP1,035.
- Cash1,100 must show change65. Test noncash, references and split payment.
- Reject negative/invalid inputs, noncash excess, insufficient payment, discounts above role limits and stacked order/item discounts.
- Retry a checkout and verify one sale and one inventory deduction.
- Change a service price after sale and verify the receipt preserves the original breakdown.
- Connect the Epson TM-U220B with its Windows driver installed. Check automatic detection and queue status in Settings → Printer. Use the actual 57.5/69.5/76 mm roll width (automatic selects 76 mm) and match its driver/paper guide. Print a test receipt, then a saved receipt through Windows and compatible ESC/POS. Confirm readable amounts, ribbon output, margins, feed and cutting. Check 58/80 mm profiles on compatible printers if used.
- Verify reprint authorization and print history. A successfully queued Windows print job does not guarantee paper output; inspect the actual receipt.
- Confirm the receipt dialog shows the saved customer, cashier, charges, discounts, received amount and change; its footer stays visible while long receipt content scrolls.

## Packages and appointments

- Create a customer, sell a5+1 package and verify six sessions and a single paid sale.
- Redeem a session with materials/staff and check remaining balance, cost and commission without new revenue.
- Test exhaustion, expiration and a package assigned to another customer.
- Schedule overlapping staff appointments; reject unless an owner explicitly overrides. Reschedule, cancel and update staff statuses.

## Finance and recovery

- Add operating expenses, earned commissions and linked payouts. Verify commission cost is counted once.
- Refund a full retail transaction with/without restock. Verify revenue, stock, inventory value, paid commissions and consumed materials.
- Reconcile selected-period dashboard and reports including a refund in a later period and stock writeoffs.
- Export PDF,CSV andExcel. Open them in the salon's actual software and verify amounts, quantities and leading-zero references.
- Create a portable backup, verify it and restore on a second Windows account. Test wrong passphrase and damaged files.
- Verify the retained safety backup. Confirm backup schedule and paths after restore, USB availability and failure visibility. Reopen the application and restart the PC after saving a test sale/profile; confirm saved records remain in `%LOCALAPPDATA%\RoselieBeautyLounge\salon.db`. Keep its `database.key` and use a portable encrypted backup for a different PC.
- Complete the applicable receipt/invoicing, privacy/security, licensing and treatment-compliance reviews before business deployment.
