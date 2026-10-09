# Architecture and accounting boundaries

The WPF shell composes database/authentication/business/backup services using dependency injection. Views bind observable view models and commands; management views share validated forms and table commands. UI forms do not directly change financial tables. The infrastructure service rechecks the authenticated session, current account and permission before writes.

Every business operation opens a short-lived EF Core context over SQLCipher. A process-level data-folder mutex prevents simultaneous app instances. A service gate serializes intra-process work. Checkout is a database transaction containing sale/item/charge/discount/payment snapshots, inventory/material movements, commissions, entitlements, receipt and audit record. Unique checkout tokens support safe retries.

There are no network services in the operating app. Cashless payment methods record the customer's externally completed payment; they do not charge an account or verify online settlement.

Database initialization uses the version0.1 normalized EF model and seed catalog. Future schema changes must use a reviewed migration and a verified backup; `EnsureCreated` is for initial installation, not an upgrade-migration engine. The generated SQL schema is included in the UI verification artifacts.

New databases use random256-bit raw SQLCipher keys, avoiding repeated password derivation on every connection. Older test-format DPAPI key payloads and portable backup envelopes remain readable. Passwords use Argon2id with64MiB memory,3iterations,2lanes and independent random salts. Backup key wrapping uses PBKDF2-SHA256/600,000iterations and AES-GCM; SQLCipher continues to encrypt database pages. DPAPI protects the local key and scheduled passphrase for the Windows account.

Receipt text is snapshotted on checkout. Reprinting does not rebuild from mutable prices or business details. Reports derive revenue/costs from the same financial snapshots, with refunds recognized in their refund period. Inventory purchases do not become operating expenses; issued retail goods/materials and explicit stock losses recognize costs. Accrued commissions are costs; linked payouts do not add another cost.

Package recognition uses purchase-date revenue. Standalone session redemptions are included as cost events; sessions included in a checkout contribute through the checkout snapshot and are excluded from the standalone cost query to avoid duplication.

No automated check replaces hardware acceptance, penetration/security review, legal/tax review or a salon operator's acceptance of its actual accounting policies.
