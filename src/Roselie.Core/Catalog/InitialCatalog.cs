using System.Security.Cryptography;
using System.Text;
using Roselie.Core.Models;

namespace Roselie.Core.Catalog;

public sealed record CatalogSeed(
    IReadOnlyList<ServiceCategory> Categories,
    IReadOnlyList<SalonService> Services,
    IReadOnlyList<PackageOffer> Packages,
    IReadOnlyList<Role> Roles,
    IReadOnlyList<Permission> Permissions,
    IReadOnlyList<RolePermission> RolePermissions,
    IReadOnlyList<ApplicationSetting> Settings);

/// <summary>Only the supplied service catalog and configuration are initialized; no business transactions or customer records.</summary>
public static class InitialCatalog
{
    public static Guid StableId(string key) => new(SHA256.HashData(Encoding.UTF8.GetBytes("roselie:" + key))[..16]);

    public static CatalogSeed Create(DateTime? createdAtUtc = null)
    {
        var timestamp = createdAtUtc ?? DateTime.UtcNow;
        var categoryNames = new[]
        {
            "Gluta Push", "Gluta Drip & Aesthetic Treatments", "Facial & Skin Care",
            "Laser & Advanced Treatments", "Nail & Foot Care", "Hair Care & Styling"
        };
        var categories = categoryNames.Select((name, index) => new ServiceCategory
        {
            Id = StableId("category:" + name), Name = name, DisplayOrder = index + 1, CreatedAtUtc = timestamp
        }).ToArray();
        var services = new List<SalonService>();
        var packages = new List<PackageOffer>();
        void Add(int categoryIndex, string name, decimal price, decimal? packagePrice = null,
            PricingType type = PricingType.Fixed, decimal? maximum = null, string description = "")
        {
            var service = new SalonService
            {
                Id = StableId("service:" + name), CreatedAtUtc = timestamp, Name = name,
                CategoryId = categories[categoryIndex].Id, BasePrice = price,
                PricingType = type, MinimumPrice = type is PricingType.Range or PricingType.StartsAt ? price : null,
                MaximumPrice = maximum, PackageAvailable = packagePrice.HasValue, PackagePrice = packagePrice,
                Description = description, DurationMinutes = 0, OwnerConfirmed = false,
                CostInformationComplete = false, IsActive = true
            };
            services.Add(service);
            if (packagePrice.HasValue)
                packages.Add(new PackageOffer
                {
                    Id = StableId("package:" + name), CreatedAtUtc = timestamp, ServiceId = service.Id,
                    Name = name + " — 5+1 sessions", Price = packagePrice.Value, TotalSessions = 6,
                    RevenuePolicy = PackageRevenuePolicy.RecognizeOnPurchase
                });
        }

        Add(0, "Vitamin B12 and Vitamin C", 250);
        Add(0, "Collagen and Placenta", 400);
        Add(1, "Brightening Drip", 599, 2995);
        Add(1, "Flawless Drip", 899, 4495);
        Add(1, "Ageless Drip", 1499, 7495);
        Add(1, "Korean Drip", 1699, 8495);
        Add(1, "Japan Drip", 1899, 9495);
        Add(1, "Slimming Shot", 1499, 7495);
        Add(1, "Lemon Bottle", 3000, description: "Price per bottle.");
        Add(2, "Hydra Facial", 599, 2995);
        Add(2, "Mermaid Facial", 899, 4495);
        Add(2, "Microneedling", 1499, 7495);
        Add(2, "Melasma Treatment", 1399, 6995);
        Add(2, "RF Arms", 800, 4000);
        Add(2, "RF Tummy", 1000, 5000);
        Add(2, "RF Face", 600, 3000);
        Add(2, "RF Chin", 300);
        Add(2, "RF Eyebags", 250);
        Add(3, "Pico Laser Face", 1199, 5995);
        Add(3, "Pico Laser Underarms", 899, 4495);
        Add(3, "Pico Laser Back", 1499, 7495);
        Add(3, "Pico Bikini and Butt", 1599, 7995);
        Add(3, "Diode Hair Removal", 1199, 5995);
        Add(3, "Diode Legs", 1499, 7495);
        Add(3, "Diode Bikini", 999, 4995);
        Add(3, "Diode Brazilian", 1999, 9995);
        Add(3, "Tattoo Removal", 499, type: PricingType.StartsAt);
        Add(3, "Warts Removal", 399, type: PricingType.StartsAt);
        Add(4, "Regular Manicure", 150);
        Add(4, "Regular Pedicure", 150);
        Add(4, "Gel Polish", 349, type: PricingType.Range, maximum: 369);
        Add(4, "Gel Overlay", 400, type: PricingType.Range, maximum: 450);
        Add(4, "Nail Extensions", 449, type: PricingType.StartsAt);
        Add(4, "Foot Spa with Massage", 450);
        Add(5, "Hair Spa", 300, type: PricingType.Range, maximum: 500);
        Add(5, "Hair Color", 500, type: PricingType.Range, maximum: 1000);
        Add(5, "Brazilian Hair Treatment", 800, type: PricingType.Range, maximum: 1500);
        Add(5, "Rebond", 1000, type: PricingType.Range, maximum: 1500);
        Add(5, "L'Oréal Rebond", 2000, type: PricingType.Range, maximum: 2500);
        Add(5, "High-End Rebond", 3500);

        var roles = Enum.GetValues<RoleKind>().Select(kind => new Role
        {
            Id = StableId("role:" + kind), Name = kind == RoleKind.SalonStaff ? "Salon Staff" : kind.ToString(),
            Kind = kind, CreatedAtUtc = timestamp
        }).ToArray();
        var permissionKeys = new[]
        {
            "pos", "checkout", "customers.read", "customers.write", "appointments.read", "appointments.write",
            "receipts.print", "receipts.reprint", "discounts", "prices.override", "services.manage", "products.manage",
            "inventory.manage", "employees.manage", "expenses.manage", "packages.manage", "reports.financial",
            "refunds", "voids", "users.manage", "backup", "settings", "audit.read"
        };
        var permissions = permissionKeys.Select(key => new Permission
        {
            Id = StableId("permission:" + key), Key = key, Description = key, CreatedAtUtc = timestamp
        }).ToArray();
        var cashierKeys = new HashSet<string>
        {
            "pos", "checkout", "customers.read", "customers.write", "appointments.read", "appointments.write",
            "receipts.print", "discounts"
        };
        var staffKeys = new HashSet<string> { "customers.read", "appointments.read" };
        var rolePermissions = (from role in roles from permission in permissions
            where role.Kind == RoleKind.Administrator ||
                  role.Kind == RoleKind.Cashier && cashierKeys.Contains(permission.Key) ||
                  role.Kind == RoleKind.SalonStaff && staffKeys.Contains(permission.Key)
            select new RolePermission
            {
                Id = StableId("role-permission:" + role.Kind + ":" + permission.Key), CreatedAtUtc = timestamp,
                RoleId = role.Id, PermissionId = permission.Id
            }).ToArray();
        var defaults = new Dictionary<string, string>
        {
            ["BusinessName"] = "Roselie's Beauty Lounge",
            ["BusinessAddress"] = "", ["BusinessContact"] = "", ["TaxRegistrationDetails"] = "",
            ["Business.TaxpayerId"] = "", ["Business.LogoPath"] = "",
            ["Currency"] = "PHP", ["TimeZone"] = "Asia/Manila",
            ["ThemeRose"] = "#C8797D", ["Appearance.DarkRose"] = "#A85E67",
            ["Appearance.Blush"] = "#E8C3C3", ["Appearance.Cream"] = "#F3E7E7",
            ["Appearance.Background"] = "#FAF6F5", ["Appearance.Text"] = "#344052",
            ["Appearance.TableDensity"] = "Comfortable", ["Appearance.DisplayScale"] = "100",
            ["PrinterName"] = "", ["ReceiptWidth"] = "80",
            ["Pricing.CashierDiscountLimit"] = "20", ["Pricing.CustomPricesRequireAuthorization"] = "true",
            ["Tax.Mode"] = "NotConfigured", ["Tax.ComplianceReviewed"] = "false",
            ["Security.SessionTimeoutMinutes"] = "15", ["Security.LockoutAttempts"] = "5",
            ["Security.LockoutMinutes"] = "15", ["BackupDirectory"] = "",
            ["BackupScheduleHours"] = "0", ["BackupPassphraseProtected"] = "",
            ["Accounting.PackageRevenuePolicy"] = "RecognizeOnPurchase",
            ["Accounting.InventoryCosting"] = "WeightedAverage", ["Catalog.OwnerConfirmed"] = "false"
        };
        var settings = defaults.Select(item => new ApplicationSetting
        {
            Id = StableId("setting:" + item.Key), CreatedAtUtc = timestamp, Key = item.Key, Value = item.Value
        }).ToArray();
        return new CatalogSeed(categories, services.AsReadOnly(), packages.AsReadOnly(), roles, permissions, rolePermissions, settings);
    }
}
