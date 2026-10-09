# COMPLETE MASTER DEVELOPMENT PROMPT
# ROSELIE'S BEAUTY LOUNGE
## Fully Offline Windows POS, Inventory & Salon Management System

---

# 1. PROJECT OVERVIEW

Develop a complete, modern, professional, secure, and user-friendly Windows desktop application for **Roselie's Beauty Lounge**.

**Application Name:** Roselie's Beauty Lounge POS & Inventory Management System

**Application Type:** Native Windows Desktop Application

**Primary Purpose:** Manage salon services, retail products, sales transactions, inventory, expenses, profit, employees, appointments, customers, packages, and receipt printing.

The application must work **100% offline** and must not require a web browser, internet connection, cloud server, or external hosting service.

The system will be used in an actual beauty salon in the Philippines. The business owner will personally encode the existing products, stocks, and additional services.

The goal is to create a reliable commercial-quality business application with a polished, elegant design inspired by Roselie's Beauty Lounge branding.

## Mandatory Requirements

- Native Windows desktop software, not a website.
- Fully functional without internet.
- Secure encrypted local database.
- Easy installation on Windows 10/11.
- Modern and professional user interface.
- Accurate financial calculations.
- Support for custom service prices and additional charges.
- Percentage-based discount system.
- Automatic inventory deduction.
- Salon packages and session tracking.
- Monthly income, expenses, and net profit.
- Customer and appointment management.
- Employee permissions and commissions.
- Thermal receipt printing.
- Daily, weekly, monthly, and yearly reports.
- Backup and restoration.
- Audit logs for important activities.
- Business-ready application after testing and applicable compliance review.

Do not create a static dashboard or a UI-only demonstration. Every feature must have working logic and persistent database integration.

---

# 2. TECHNOLOGY STACK

Use the following recommended stack:

**Programming Language:** C#

**Framework:** .NET 10

**Desktop UI:** WPF (Windows Presentation Foundation)

**Architecture:** MVVM with dependency injection

**Database:** SQLite

**Encryption:** SQLCipher

**Database Access:** Entity Framework Core with a compatible encrypted SQLite provider

**Password Security:** Argon2id password hashing

**Receipt Printing:** Windows Print API and compatible ESC/POS thermal printers

**Reporting:** PDF, CSV, and Excel export

**Installation:** Windows desktop installer

**Operating System:** Windows 10/11 64-bit

Use stable and maintained libraries.

Keep the architecture modular, testable, and easy to maintain.

Do not use Electron, ASP.NET, browser-based frameworks, or mandatory cloud services.

---

# 3. OFFICIAL BRANDING AND COLOR THEME

Follow the user's supplied Roselie's Beauty Lounge promotional reference image.

The design must use a **soft feminine, minimalist, elegant salon aesthetic**.

## Official Color Palette

- Primary Dusty Rose: `#C8797D`
- Dark Rose: `#A85E67`
- Soft Blush Pink: `#E8C3C3`
- Warm Cream: `#F3E7E7`
- Ivory Background: `#FAF6F5`
- Pure White: `#FFFFFF`
- Slate Charcoal: `#344052`
- Secondary Gray: `#77727B`
- Light Border: `#E9DADB`
- Success Green: `#27845F`
- Warning Amber: `#C88B35`
- Error Red: `#C84D58`

The palette is visually approximated from the reference and must remain configurable.

## Typography

Use Segoe UI for the application interface.

Use elegant typography for the Roselie's Beauty Lounge branding, but keep data tables, input fields, and financial information highly readable.

## Visual Design

The application must feature:

- Elegant left navigation sidebar.
- Rounded white cards.
- Warm ivory background.
- Soft pink active navigation highlights.
- Clean, well-spaced layouts.
- Subtle shadows.
- Professional icons.
- Rounded buttons.
- Clear status indicators.
- Consistent modal dialogs.
- Attractive yet readable charts.
- Responsive desktop window scaling.

Avoid excessive gradients, dark burgundy backgrounds, neon colors, cluttered decorations, and oversized graphics.

Use floral line art sparingly on the login or welcome screen only.

**Primary design principle: Beautiful, but practical for fast cashier operation.**

---

# 4. MAIN APPLICATION NAVIGATION

Create a consistent application layout with the following navigation items:

1. Dashboard
2. Point of Sale
3. Services
4. Products
5. Inventory
6. Customers
7. Appointments
8. Employees
9. Expenses
10. Packages
11. Reports
12. Settings

## Sidebar

- Salon logo at the top.
- Clear icons and labels.
- Highlight active page.
- Collapsible sidebar.
- Logout option at the bottom.

## Top Navigation

- Current page title.
- Search when relevant.
- Current user.
- User role.
- Date and time.
- Offline status.
- Notification indicator.
- Account menu.

Keep navigation fast and consistent.

---

# 5. LOGIN AND USER AUTHENTICATION

Create a professional login page matching the salon's visual theme.

## Login Design

Use a split-screen layout.

Left side:
- Roselie's Beauty Lounge logo.
- Soft cream and blush background.
- Optional salon-themed image.
- Welcome message.
- Subtle floral elements.

Right side:
- Welcome Back heading.
- Username.
- Password.
- Password visibility toggle.
- Remember username.
- Sign In button.

## Security Requirements

- Secure password hashing.
- Role-based permissions.
- No plaintext passwords.
- No hardcoded administrator password.
- First-run administrator account creation.
- Login activity recording.
- Account lockout or login throttling.
- Session timeout.
- Secure offline account recovery.

---

# 6. DASHBOARD — COMPLETE BUSINESS OVERVIEW

Design a modern financial dashboard that gives the business owner a complete view of salon operations.

## Financial Summary Cards

Display:

1. Today's Sales
2. Monthly Income
3. Monthly Net Profit
4. Monthly Expenses
5. Total Transactions
6. Total Customers
7. Today's Appointments
8. Low Stock Products

Each card should contain:
- Small icon.
- Clear label.
- Large value.
- Optional percentage comparison against the previous period.

## Monthly Income

Calculate the salon's net sales revenue for the selected month.

Include:
- Salon services.
- Retail product sales.
- Package sales.
- Applicable discounts.
- Refunds.
- Sales adjustments.

Avoid double-counting package redemptions as new sales.

Separate sales revenue from cash collection when necessary.

## Monthly Net Profit

Use the formula:

**Net Profit = Net Sales Revenue − Cost of Goods Sold − Service Material Costs − Operating Expenses**

Include applicable employee commissions and other service-related costs.

Avoid double-counting inventory purchases and costs.

Show a warning if cost information is incomplete.

## Monthly Expenses

Include:
- Rent.
- Electricity.
- Water.
- Salaries.
- Staff commissions.
- Maintenance.
- Marketing.
- Other expenses.

## Dashboard Analytics

Create:
- Daily sales bar chart.
- Monthly income vs. net profit comparison.
- Monthly expense breakdown.
- Top-performing services.
- Best-selling retail products.
- Recent transactions.
- Low-stock notifications.
- Upcoming appointments.

Include day, week, month, and custom date filters.

The dashboard must retrieve real records from the database.

---

# 7. OFFICIAL SALON SERVICE CATALOG

Use the supplied Roselie's Beauty Lounge price list as the initial service reference.

All services must be editable by the administrator.

Create these categories and initial prices.

## CATEGORY A: GLUTA PUSH

| Service | Price |
|---|---:|
| Vitamin B12 and Vitamin C | ₱250 |
| Collagen and Placenta | ₱400 |

## CATEGORY B: GLUTA DRIP & AESTHETIC TREATMENTS

| Service | Single Price | Package 5+1 |
|---|---:|---:|
| Brightening Drip | ₱599 | ₱2,995 |
| Flawless Drip | ₱899 | ₱4,495 |
| Ageless Drip | ₱1,499 | ₱7,495 |
| Korean Drip | ₱1,699 | ₱8,495 |
| Japan Drip | ₱1,899 | ₱9,495 |
| Slimming Shot | ₱1,499 | ₱7,495 |
| Lemon Bottle | ₱3,000 per bottle | — |

## CATEGORY C: FACIAL & SKIN CARE

| Service | Single Price | Package 5+1 |
|---|---:|---:|
| Hydra Facial | ₱599 | ₱2,995 |
| Mermaid Facial | ₱899 | ₱4,495 |
| Microneedling | ₱1,499 | ₱7,495 |
| Melasma Treatment | ₱1,399 | ₱6,995 |
| RF Arms | ₱800 | ₱4,000 |
| RF Tummy | ₱1,000 | ₱5,000 |
| RF Face | ₱600 | ₱3,000 |
| RF Chin | ₱300 | — |
| RF Eyebags | ₱250 | — |

## CATEGORY D: LASER & ADVANCED TREATMENTS

| Service | Single Price | Package 5+1 |
|---|---:|---:|
| Pico Laser Face | ₱1,199 | ₱5,995 |
| Pico Laser Underarms | ₱899 | ₱4,495 |
| Pico Laser Back | ₱1,499 | ₱7,495 |
| Pico Bikini and Butt | ₱1,599 | ₱7,995 |
| Diode Hair Removal | ₱1,199 | ₱5,995 |
| Diode Legs | ₱1,499 | ₱7,495 |
| Diode Bikini | ₱999 | ₱4,995 |
| Diode Brazilian | ₱1,999 | ₱9,995 |
| Tattoo Removal | Starts at ₱499 | — |
| Warts Removal | Starts at ₱399 | — |

## CATEGORY E: NAIL & FOOT CARE

| Service | Price |
|---|---:|
| Regular Manicure | ₱150 |
| Regular Pedicure | ₱150 |
| Gel Polish | ₱349–₱369 |
| Gel Overlay | ₱400–₱450 |
| Nail Extensions | Starts at ₱449 |
| Foot Spa with Massage | ₱450 |

## CATEGORY F: HAIR CARE & STYLING

| Service | Price |
|---|---:|
| Hair Spa | ₱300–₱500 |
| Hair Color | ₱500–₱1,000 |
| Brazilian Hair Treatment | ₱800–₱1,500 |
| Rebond | ₱1,000–₱1,500 |
| L'Oréal Rebond | ₱2,000–₱2,500 |
| High-End Rebond | ₱3,500 |

## Service Catalog Implementation

Each service must have:
- Service ID.
- Name.
- Category.
- Description.
- Base price.
- Pricing type: Fixed, Range, Starts At, or Custom.
- Minimum and maximum advertised prices where applicable.
- Package availability.
- Package price.
- Service duration.
- Required materials.
- Active/inactive status.

For ranged-price services, allow the cashier to select or enter an agreed price according to permission rules.

For services that say "Starts At," allow higher custom amounts without automatically imposing an upper limit.

Treat the uploaded catalog as initial data requiring business-owner confirmation.

Do not hardcode prices permanently.

---

# 8. POINT OF SALE MODULE

Create a professional POS interface with a fast checkout workflow.

## Left Section: Services and Products

Display:
- Search bar.
- Category filters.
- Service cards.
- Product cards.
- Optional service images.
- Price labels.
- Availability indicators.

Service categories should be accessible through quick filters.

## Right Section: Current Order

Display:
- Customer selection.
- Walk-in customer option.
- Selected services.
- Selected products.
- Quantities.
- Price adjustments.
- Discounts.
- Subtotal.
- Total.
- Checkout button.

Allow multiple services and products in a single transaction.

Support barcode scanners operating as keyboard input.

Avoid forcing the user through unnecessary dialogs.

---

# 9. CUSTOM SERVICE PRICING — CRITICAL FEATURE

Create a **Customize Service** dialog whenever staff need to adjust a service price.

## Required Fields

- Service name.
- Base price.
- Custom price when permitted.
- Quantity.
- Additional charges.
- Discount percentage.
- Final amount.

## Unlimited Custom Additional Charges

Include an **+ Add Charge** button.

Each additional charge must have:
- Charge name.
- Description.
- Amount.
- Edit button.
- Delete button.

Allow adding any custom charge without changing the application code.

Examples:
- Long Hair.
- Thick Hair.
- Extra Product Used.
- Extra Treatment.
- Special Styling.
- Additional Material.
- Additional Service Time.

The cashier must be able to enter a custom amount for each charge, subject to authorization rules.

## Saved Charge Presets

Administrators can create reusable charge options.

Presets are optional shortcuts, not mandatory charges.

Example:

Service: Hair Color

Base Price: ₱500

Long Hair: +₱300

Thick Hair: +₱200

Extra Product: +₱150

Subtotal: ₱1,150

Discount: 10%

Discount Amount: ₱115

**Final Total: ₱1,035**

The calculation must update instantly.

Include:
- Save Changes.
- Cancel.
- Add to Cart.

If the customer receives multiple services, each service must have its own pricing breakdown.

---

# 10. PERCENTAGE-BASED DISCOUNT SYSTEM

The discount field must use a percentage rather than a fixed peso amount.

## Required Features

- Custom discount percentage.
- Quick buttons: 5%, 10%, 15%, 20%.
- Discounts from 0% to 100%.
- Support up to two decimal places.
- Automatic peso discount calculation.
- Authorized discount limits.
- Optional discount reason.
- Discount audit history.

## Standard Formula

Subtotal = Base Price + Additional Charges

Discount Amount = Subtotal × Discount Percentage ÷ 100

Final Price = Subtotal − Discount Amount

Use accurate decimal arithmetic and consistent rounding.

Support both item-level and whole-transaction discounts with clear rules that prevent unintended double discounting.

For discounts that involve statutory tax treatment, use separately reviewed tax-aware calculation rules.

---

# 11. CART, PAYMENT, AND CHECKOUT

Create a dedicated checkout page.

## Cart Details

Display:
- Item name.
- Quantity.
- Unit price.
- Additional charges.
- Discount.
- Line total.
- Subtotal.
- Final amount.

## Supported Payment Methods

- Cash.
- GCash, manually recorded.
- Bank Transfer, manually recorded.
- Card Payment, manually recorded.
- Split Payment.

The POS does not require an online payment gateway.

## Cash Payment

Cashier enters:
- Total amount due.
- Amount received.

The system calculates change automatically.

## Transaction Safety

- Reject negative or invalid payments.
- Prevent duplicate checkout.
- Assign unique transaction numbers.
- Save transaction snapshots.
- Save payment details.
- Deduct inventory atomically.
- Preserve transaction history.
- Support refunds and voids with proper permissions.

---

# 12. RECEIPT PRINTING SYSTEM

Support compatible 58 mm and 80 mm thermal printers.

## Required Printer Features

- Printer selection.
- Print preview.
- Test print.
- Receipt printing.
- Authorized reprinting.
- Printer configuration.
- Receipt history.

## Receipt Content

- Business name.
- Business address.
- Required taxpayer and registration details.
- Invoice/receipt number as applicable.
- Date and time.
- Cashier.
- Customer if applicable.
- Items and services.
- Base prices.
- Additional charges.
- Percentage discounts.
- Tax details where applicable.
- Total.
- Payment method.
- Amount received.
- Change.

Provide configurable settings for Philippine BIR requirements.

Do not represent the application as BIR-accredited or tax-compliant until actual applicable requirements have been checked and fulfilled.

---

# 13. SERVICES MANAGEMENT

Create a Services page with a searchable table.

Columns:
- Service Name.
- Category.
- Base Price.
- Price Type.
- Package Availability.
- Status.
- Actions.

Provide:
- Add Service.
- Edit Service.
- Disable Service.
- Search.
- Filter.
- Configure price presets.
- Configure required materials.
- Configure package offers.

Preserve historical transaction prices even if current service prices change.

---

# 14. PRODUCTS MANAGEMENT

Create a Products page used for encoding existing beauty products.

Fields:
- Product ID.
- Product Name.
- SKU.
- Barcode.
- Category.
- Brand.
- Supplier.
- Purchase Cost.
- Selling Price.
- Stock Unit.
- Reorder Level.
- Product Image.
- Expiration Date if applicable.

Provide add, edit, search, and category management.

Support retail items and internal-use salon supplies.

---

# 15. INVENTORY MANAGEMENT

Create a separate Inventory page.

## Features

- Stock-in.
- Stock-out.
- Stock adjustments.
- Purchase cost tracking.
- Inventory movement history.
- Low-stock alerts.
- Product expiration tracking.
- Supplier management.
- Inventory valuation.

## Automatic Deduction

Retail products must deduct stock after completed sales.

Salon services must deduct configured consumables.

Example:

Hair Color service consumes:
- Hair dye.
- Developer.
- Gloves.
- Other materials.

Allow recording additional quantities actually used.

Every adjustment must preserve an audit record.

Use a consistent costing method, such as weighted-average cost.

---

# 16. PACKAGES AND SESSION TRACKING

The salon offers multiple **5+1 packages**.

Implement a complete package system.

## Package Workflow

1. Customer purchases a package.
2. Cashier selects or creates customer profile.
3. System records payment.
4. System creates a package entitlement.
5. The package includes six available sessions.
6. Each completed visit consumes one session.
7. The remaining session balance updates automatically.

Track:
- Customer.
- Package service.
- Package price.
- Purchase date.
- Total sessions.
- Used sessions.
- Remaining sessions.
- Expiration date if applicable.
- Redemption history.
- Assigned staff.

Do not record each prepaid redemption as a new paid sale.

Use a consistent financial policy for package revenue recognition, liabilities where applicable, and redemption costs.

---

# 17. CUSTOMER MANAGEMENT

Create a customer directory.

Fields:
- Customer ID.
- Full Name.
- Phone Number.
- Optional Email.
- Visit History.
- Purchase History.
- Service History.
- Active Packages.
- Appointment History.
- Optional Notes.

Support anonymous walk-in transactions.

Protect customer information with appropriate access controls.

---

# 18. APPOINTMENT MANAGEMENT

Create a modern salon appointment calendar.

Features:
- Daily view.
- Weekly view.
- Monthly view.
- New appointment.
- Reschedule.
- Cancel.
- Customer selection.
- Service selection.
- Staff assignment.
- Appointment status.

Statuses:
- Pending.
- Confirmed.
- In Progress.
- Completed.
- Cancelled.

Prevent overlapping appointments assigned to the same staff member unless explicitly overridden.

Appointments must work offline.

---

# 19. EMPLOYEE MANAGEMENT

Create an employee and account management module.

Fields:
- Employee ID.
- Full Name.
- Role.
- Contact.
- Employment Status.
- Commission Settings.
- Login Account.

## User Roles

**Administrator**
- Full system access.
- Service management.
- Product management.
- Inventory.
- Expenses.
- Profit reports.
- Users.
- Backup.
- Settings.

**Cashier**
- POS.
- Checkout.
- Customer lookup.
- Receipt printing.
- Authorized discounts.

**Salon Staff**
- Assigned appointments.
- Assigned services.
- Authorized customer records.

## Employee Commissions

Support:
- Fixed commission.
- Percentage commission.
- Commission per service.
- Commission reports.

Include eligible commissions in the profitability calculations.

---

# 20. EXPENSE MANAGEMENT

Create an Expenses module.

Fields:
- Expense ID.
- Category.
- Description.
- Amount.
- Date.
- Payment Method.
- Reference Number.
- Recorded By.

Categories:
- Rent.
- Electricity.
- Water.
- Salaries.
- Commissions.
- Marketing.
- Maintenance.
- Miscellaneous.

Expenses must automatically appear in monthly expense reporting and appropriate profit calculations.

Prevent double-counting.

---

# 21. REPORTS AND ANALYTICS

Create a professional reporting center.

Reports:
1. Daily Sales.
2. Weekly Sales.
3. Monthly Income.
4. Monthly Net Profit.
5. Monthly Expenses.
6. Service Sales.
7. Product Sales.
8. Inventory Movements.
9. Low Stock.
10. Employee Performance.
11. Employee Commissions.
12. Discounts.
13. Refunds and Voids.
14. Payment Methods.
15. Package Purchases and Redemptions.

## Report Features

- Date range filters.
- Month/year selection.
- Print preview.
- Print report.
- PDF export.
- Excel export.
- CSV export.

Dashboard totals and report totals must always reconcile under the same accounting policies.

---

# 22. DATABASE DESIGN

Create normalized database tables for:

- Users
- Roles
- Permissions
- Employees
- Customers
- ServiceCategories
- Services
- ServicePriceRules
- ServiceChargePresets
- Products
- ProductCategories
- Suppliers
- InventoryMovements
- InventoryCostLayers or AverageCosts
- ServiceMaterialRequirements
- ServiceMaterialConsumption
- Sales
- SaleItems
- SaleItemCharges
- SaleItemDiscounts
- Payments
- Refunds
- Expenses
- Appointments
- Packages
- CustomerPackages
- PackageRedemptions
- CommissionRecords
- ReceiptRecords
- AuditLogs
- ApplicationSettings
- BackupRecords

Use appropriate primary keys, foreign keys, indexes, and database constraints.

Ensure monetary values are persisted using exact representations.

Store historical prices and financial snapshots rather than relying on changeable catalog prices.

---

# 23. OFFLINE SECURITY AND BACKUP

Use SQLCipher to encrypt the SQLite database.

Implement:
- Strong password hashing.
- Secure encryption key handling.
- Role-based access.
- Audit logs.
- Database transaction safety.
- Backup integrity checks.
- Encrypted local backups.
- Manual backup.
- Scheduled backup.
- USB backup export.
- Restore from backup.

Keep business data in the appropriate Windows application-data directory.

Do not store the production database in the installation folder.

Do not hardcode database keys.

The application must work without cloud authentication.

---

# 24. SETTINGS MODULE

Create organized tabs.

## Business Information
- Salon Name.
- Address.
- Contact Number.
- Business Logo.
- Registration Details.

## Printer
- Printer Selection.
- Receipt Width.
- Test Print.
- Receipt Preview.

## Pricing
- Default Price Rules.
- Charge Presets.
- Discount Limits.
- Custom Price Permissions.

## Employees and Users
- Account Management.
- Roles.
- Permissions.
- Password Settings.

## Database
- Backup.
- Restore.
- Database Status.
- Integrity Check.

## Appearance
- Official Salon Theme.
- Display Scaling.
- Table Density.

Use the supplied pink design as the default theme.

---

# 25. REQUIRED UI SCREENS

Create each page separately.

1. Login Page
2. Dashboard
3. POS Service/Product Selection
4. Customize Service Price
5. Cart and Checkout
6. Payment Successful
7. Receipt Preview
8. Services Management
9. Products Management
10. Inventory Management
11. Customers
12. Appointments
13. Employees
14. Expenses
15. Packages
16. Reports
17. Settings

## UI Consistency Requirements

Every screen must:
- Use the official palette.
- Use matching sidebar navigation.
- Maintain consistent spacing.
- Have aligned cards and tables.
- Use proper text contrast.
- Avoid clipping and overlap.
- Include appropriate loading and empty states.
- Provide working buttons and input validation.

Use beautiful design without sacrificing speed or usability.

---

# 26. CALCULATION AND QUALITY ASSURANCE

Create a centralized financial calculation engine.

Test:

- Base service price.
- Additional charges.
- Multiple additional charges.
- Percentage discounts.
- Multiple items.
- Quantities.
- Cash payments.
- Change.
- Partial/split payments.
- Package sales.
- Package redemption.
- Product costs.
- Service material costs.
- Employee commissions.
- Expenses.
- Monthly income.
- Net profit.
- Refunds.
- Inventory deductions.
- Rounding behavior.

Use exact arithmetic for money.

Specify one rounding policy and test all financial edge cases.

The receipt, database transaction, dashboard, and financial reports must show consistent figures.

---

# 27. WINDOWS INSTALLATION

Generate a real Windows installer.

Include:
- Application executable.
- Desktop shortcut.
- Start Menu shortcut.
- First-run setup.
- Admin creation.
- Database initialization.
- Business information setup.
- Printer configuration.
- Backup directory setup.
- Service catalog initialization.
- Product encoding interface.

The installer must not require a permanent internet connection.

Provide setup instructions.

---

# 28. IMPLEMENTATION PHASES

## Phase 1 — Foundation
- Create the complete WPF solution.
- Define architecture.
- Build the reusable design system.
- Build login and application shell.
- Configure database and authentication.

## Phase 2 — Services and Products
- Import initial salon services from the provided list.
- Build service management.
- Build product encoding.
- Build pricing rules.
- Build inventory.

## Phase 3 — Core POS
- Service selection.
- Custom price adjustments.
- Percentage discounts.
- Cart.
- Checkout.
- Payments.
- Receipt printing.

## Phase 4 — Salon Management
- Customers.
- Appointments.
- Employees.
- Commissions.
- Packages.
- Expenses.

## Phase 5 — Financial Analytics
- Dashboard.
- Monthly income.
- Monthly net profit.
- Expense tracking.
- Reporting.
- Charts.

## Phase 6 — Security and Deployment
- Access controls.
- Audit logs.
- Encrypted backups.
- Automated testing.
- Printer tests.
- Installer.
- User documentation.

Each phase must result in compiling, functioning code.

Do not generate placeholder buttons or nonfunctional workflows as if they were complete.

---

# 29. FINAL DEVELOPMENT INSTRUCTIONS

Act as a senior Windows desktop software architect, C# developer, database engineer, UI/UX designer, and QA engineer.

Build this system as a real production-oriented application.

**Development priorities:**

1. Accurate calculations.
2. Reliable offline transactions.
3. Data integrity and security.
4. Easy cashier workflow.
5. Elegant, professional design.
6. Complete inventory integration.
7. Reliable receipt printing.
8. Accurate profit and expense reporting.
9. Maintainable application architecture.
10. Reliable installation and backup.

Do not replace the Windows application with a browser-based implementation.

Do not invent real business records.

Use the provided salon service prices as the initial editable catalog.

Build the application systematically and ensure all major features are connected.

**START IMPLEMENTATION:**

First generate the complete Visual Studio solution structure, required dependencies, database schema, and reusable WPF design system.

Then implement the Login, Dashboard, Services Catalog, and POS interface, including custom additional charges and percentage discounts.

Provide complete working source code, build instructions, and automated tests.

Continue implementing subsequent phases until the entire application is functional and ready for real-world deployment testing.

**FINAL GOAL: A COMPLETE, SECURE, BEAUTIFUL, FULLY OFFLINE WINDOWS POS AND INVENTORY MANAGEMENT SYSTEM FOR ROSELIE'S BEAUTY LOUNGE.**