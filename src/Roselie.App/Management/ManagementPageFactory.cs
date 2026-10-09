using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using Roselie.Core.Models;
using Roselie.Infrastructure;

namespace Roselie.App.Management;

/// <summary>Creates database-backed management modules for the authenticated application shell.</summary>
public static partial class ManagementPageFactory
{
    public static void ShowAccountManagement(BusinessService service, UserAccount user, AuthService auth)
    {
        if (user.Role != RoleKind.Administrator) throw new UnauthorizedAccessException("Administrator access is required to manage login accounts.");
        ArgumentNullException.ThrowIfNull(auth);
        new Builder(service, user, auth).Accounts();
    }

    public static void ShowEmployeeAccountCreation(BusinessService service, UserAccount user, AuthService auth)
    {
        if (user.Role != RoleKind.Administrator) throw new UnauthorizedAccessException("Administrator access is required to create employee login accounts.");
        ArgumentNullException.ThrowIfNull(auth);
        new Builder(service, user, auth).CreateEmployeeLogin();
    }

    public static bool ShowSuperPinSettings(AuthService auth, UserAccount user)
    {
        if (user.Role != RoleKind.Administrator) throw new UnauthorizedAccessException("Administrator access is required to configure a Super PIN.");
        ArgumentNullException.ThrowIfNull(auth);
        return SuperPinSettingsDialog.Show(auth, user);
    }

    public static FrameworkElement Create(string pageName, BusinessService service, UserAccount user, AuthService? auth = null)
    {
        if (user.Role != RoleKind.Administrator && pageName is not ("Customers" or "Appointments" or "Packages"))
            throw new UnauthorizedAccessException("Administrator access is required for this module.");
        var builder = new Builder(service, user, auth);
        return pageName switch
        {
            "Services" => builder.Services(), "Products" => builder.Products(), "Inventory" => builder.Inventory(),
            "Customers" => builder.Customers(), "Appointments" => builder.Appointments(), "Employees" => builder.Employees(),
            "Expenses" => builder.Expenses(), "Packages" => builder.Packages(),
            _ => throw new ArgumentException($"Unknown management page: {pageName}", nameof(pageName))
        };
    }

    private sealed partial class Builder(BusinessService service, UserAccount user, AuthService? auth)
    {
        private List<T> Read<T>() where T : Entity => service.List<T>(user).ToList();
        private void Save<T>(T entity) where T : Entity => service.Save(entity, user);
        private string Name<T>(Guid? id, Func<T, string> label) where T : Entity => id == null ? "—" : Read<T>().FirstOrDefault(e => e.Id == id) is T item ? label(item) : "Archived record";
        private IReadOnlyList<Choice> Choices<T>(Func<T, string> name, bool optional = false, Func<T, bool>? filter = null) where T : Entity
        {
            var result = new List<Choice>(); if (optional) result.Add(new Choice(null, "None / unassigned"));
            result.AddRange(Read<T>().Where(filter ?? (_ => true)).OrderBy(name).Select(e => new Choice(e.Id, name(e)))); return result;
        }
        private static IReadOnlyList<FieldSpec> Fields(params FieldSpec[] fields) => fields;
        private static TableColumn<T> Col<T>(string header, Func<T, object?> value, string? format = null, double width = 1, Func<T, FrameworkElement>? content = null) => new(header, value, format, width, content);
        private static string Status(bool active) => active ? "Active" : "Inactive";
        private static string Peso(decimal value) => ManagementUi.Money(value);
        private static TimeZoneInfo SalonZone => TimeZoneInfo.FindSystemTimeZoneById("Singapore Standard Time");
        private static DateTime Local(DateTime utc) => TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), SalonZone);
        private static DateTime Utc(DateTime local) => TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(local, DateTimeKind.Unspecified), SalonZone);
        private void Show(string title, FrameworkElement page) => ManagementUi.ShowDetails(title, page);

        public FrameworkElement Services()
        {
            var categoryNames = Read<ServiceCategory>().ToDictionary(c => c.Id, c => c.Name);
            Guid? selectedCategory = null;
            var page = new EntityPage<SalonService>(service, user, "Service Catalog", "Your editable service catalog. Confirm starting prices, configure consumables, and preserve historical sale prices.", "service",
            [Col<SalonService>("Service", s => s.Name, width: 2), Col<SalonService>("Category", s => categoryNames.GetValueOrDefault(s.CategoryId, "Uncategorized"), width: 2), Col<SalonService>("Base price", s => Peso(s.BasePrice)), Col<SalonService>("Pricing", s => s.PricingType), Col<SalonService>("Package", s => s.PackageAvailable ? Peso(s.PackagePrice ?? 0) : "—"), Col<SalonService>("Status", s => Status(s.IsActive)), Col<SalonService>("Confirmed", s => s.OwnerConfirmed ? "Yes" : "Review required")],
            s => Fields(new("Name", "Service name", true), new("CategoryId", "Category", true, Choices: Choices<ServiceCategory>(c => c.Name)), new("Description", "Description", MultiLine: true), new("BasePrice", "Base price (₱)", true, Min: 0), new("PricingType", "Pricing type"), new("MinimumPrice", "Minimum advertised price (₱)", Min: 0), new("MaximumPrice", "Maximum advertised price (₱)", Help: "Leave empty for Starts At pricing.", Min: 0), new("DurationMinutes", "Duration (minutes)", true, Help: "0 keeps the duration unconfirmed. Enter the salon's actual duration when available.", Min: 0, Max: 1440), new("PackageAvailable", "Package available"), new("PackagePrice", "Package price (₱)", Min: 0), new("CostInformationComplete", "Cost information complete", Help: "Enable after every required consumable is configured with a reliable cost."), new("OwnerConfirmed", "Catalog reviewed by owner"), new("IsActive", "Active service")),
            [new("Materials", s => Materials(s)), new("Charge presets", s => Presets(s)), new("Package offers", s => PackageOffers(s.Id)), new("Price rules", s => PriceRules(s)), new("Delete service", s => DeleteService(s))],
            scope: rows => rows.Where(s => selectedCategory == null || s.CategoryId == selectedCategory),
            validate: s => { if (s.MinimumPrice > s.MaximumPrice) throw new InvalidOperationException("Maximum price must be at least the minimum price."); if (s.PricingType == PricingType.Range && (s.MinimumPrice == null || s.MaximumPrice == null)) throw new InvalidOperationException("Range pricing requires a minimum and maximum advertised price."); if (s.PackageAvailable && s.PackagePrice == null) throw new InvalidOperationException("Enter the package price when packages are available."); });
            var categoryFilter = new ComboBox { ItemsSource = Choices<ServiceCategory>(c => c.Name).Prepend(new Choice(null, "All categories")).ToList(), MinWidth = 200, MinHeight = 36 };
            categoryFilter.SelectedIndex = 0;
            categoryFilter.SelectionChanged += (_, _) => { selectedCategory = (categoryFilter.SelectedItem as Choice)?.Value as Guid?; page.Model.Reload(); };
            page.Model.Reloading += () => { categoryNames.Clear(); foreach (var category in Read<ServiceCategory>()) categoryNames[category.Id] = category.Name; };
            page.Toolbar.Children.Add(ManagementUi.Button("Categories", new UiCommand(() => { ServiceCategories(); categoryFilter.ItemsSource = Choices<ServiceCategory>(c => c.Name).Prepend(new Choice(null, "All categories")).ToList(); categoryFilter.SelectedIndex = 0; page.Model.Reload(); })));
            page.Toolbar.Children.Add(categoryFilter);
            void Summary() { var rows = page.Model.Items; page.SummaryRegion.Children.Clear(); page.SummaryRegion.Children.Add(ManagementUi.MetricRow(ManagementUi.Metric("Active services", rows.Count(s => s.IsActive).ToString(), "Available in Point of Sale", "✦"), ManagementUi.Metric("Owner review", rows.Count(s => !s.OwnerConfirmed).ToString(), "Catalog entries to confirm", "◇", "#C88B35"), ManagementUi.Metric("Package services", rows.Count(s => s.PackageAvailable).ToString(), "Prepaid treatment options", "▧"))); }
            page.Model.Reloaded += Summary; Summary();
            page.SetCardView(s =>
            {
                var card = new StackPanel(); card.Children.Add(Ui.Badge(categoryNames.GetValueOrDefault(s.CategoryId, "Service"), size: 10));
                var name = ManagementUi.Text(s.Name, 17, ManagementUi.Ink, true); name.Margin = new Thickness(0, 12, 0, 8); card.Children.Add(name);
                card.Children.Add(ManagementUi.Text(Peso(s.BasePrice), 25, Ui.Brush("#A85E67"), true));
                var info = ManagementUi.Text(s.DurationMinutes > 0 ? $"{s.DurationMinutes} minutes · {s.PricingType}" : $"{s.PricingType} · Duration to confirm", 12, ManagementUi.Muted); info.Margin = new Thickness(0, 6, 0, 14); card.Children.Add(info);
                card.Children.Add(Ui.StatusPill(s.IsActive ? s.OwnerConfirmed ? "Active" : "Review required" : "Inactive")); return card;
            });
            return page;
        }

        private void ServiceCategories() => Show("Service categories", new EntityPage<ServiceCategory>(service, user, "Service categories", "Organize the service catalog and choose the display order.", "category",
            [Col<ServiceCategory>("Category", c => c.Name, width: 3), Col<ServiceCategory>("Position", c => c.DisplayOrder), Col<ServiceCategory>("Status", c => Status(c.IsActive))],
            _ => Fields(new("Name", "Category name", true), new("DisplayOrder", "Display position", Min: 0), new("IsActive", "Active category"))));

        private void Materials(SalonService salonService)
        {
            var names = Read<Product>().ToDictionary(p => p.Id, p => $"{p.Name} ({p.StockUnit})");
            Show($"Materials · {salonService.Name}", new EntityPage<ServiceMaterialRequirement>(service, user, "Required materials", $"Quantities consumed automatically for one {salonService.Name} session.", "material",
            [Col<ServiceMaterialRequirement>("Product", m => names.GetValueOrDefault(m.ProductId, "Archived product"), width: 3), Col<ServiceMaterialRequirement>("Quantity per session", m => m.Quantity)],
            _ => Fields(new("ProductId", "Consumable product", true, Choices: Choices<Product>(p => $"{p.Name} ({p.StockUnit})", filter: p => p.IsActive)), new("Quantity", "Quantity per service", true, Min: 0.0001m)),
            [new("Remove requirement", m => { if (ManagementUi.Confirm("Remove this material requirement from future sessions?")) service.Remove<ServiceMaterialRequirement>(m.Id, user); })],
            scope: rows => rows.Where(m => m.ServiceId == salonService.Id), initialize: m => m.ServiceId = salonService.Id, canDisable: false));
        }

        private void Presets(SalonService salonService) => Show($"Charges · {salonService.Name}", new EntityPage<ServiceChargePreset>(service, user, "Optional charge presets", "Reusable shortcuts for this service. Cashiers can still enter authorized custom charges.", "charge preset",
            [Col<ServiceChargePreset>("Name", c => c.Name, width: 2), Col<ServiceChargePreset>("Description", c => c.Description, width: 3), Col<ServiceChargePreset>("Amount", c => Peso(c.Amount)), Col<ServiceChargePreset>("Status", c => Status(c.IsActive))],
            _ => Fields(new("Name", "Charge name", true), new("Description", "Description", MultiLine: true), new("Amount", "Additional charge (₱)", true, Min: 0), new("IsActive", "Active preset")),
            scope: rows => rows.Where(c => c.ServiceId == salonService.Id), initialize: c => c.ServiceId = salonService.Id));

        private void PriceRules(SalonService salonService) => Show($"Price rules · {salonService.Name}", new EntityPage<ServicePriceRule>(service, user, "Price rules", "Named price variants used to document authorized pricing for this service.", "price rule",
            [Col<ServicePriceRule>("Name", r => r.Name, width: 2), Col<ServicePriceRule>("Minimum", r => r.MinimumPrice is decimal min ? Peso(min) : "—"), Col<ServicePriceRule>("Maximum", r => r.MaximumPrice is decimal max ? Peso(max) : "—"), Col<ServicePriceRule>("Requires authorization", r => r.RequiresAuthorization ? "Yes" : "No")],
            _ => Fields(new("Name", "Rule name", true), new("MinimumPrice", "Minimum price (₱)", Min: 0), new("MaximumPrice", "Maximum price (₱)", Min: 0), new("RequiresAuthorization", "Requires authorization")),
            [new("Remove rule", r => { if (ManagementUi.Confirm("Remove this price rule?")) service.Remove<ServicePriceRule>(r.Id, user); })],
            scope: rows => rows.Where(r => r.ServiceId == salonService.Id), initialize: r => r.ServiceId = salonService.Id,
            validate: r => { if (r.MinimumPrice > r.MaximumPrice) throw new InvalidOperationException("Maximum price must be at least the minimum price."); }, canDisable: false));

        public FrameworkElement Products()
        {
            var categories = Read<ProductCategory>().ToDictionary(c => c.Id, c => c.Name);
            var suppliers = Read<Supplier>().ToDictionary(s => s.Id, s => s.Name);
            var page = new EntityPage<Product>(service, user, "Products", "Encode retail products and internal supplies. Record opening and purchased quantities through Inventory.", "product",
            [Col<Product>("Product", p => p.Name, width: 2), Col<Product>("SKU / barcode", p => string.IsNullOrWhiteSpace(p.Barcode) ? p.Sku : $"{p.Sku} · {p.Barcode}", width: 2), Col<Product>("Category", p => p.CategoryId == null ? "—" : categories.GetValueOrDefault(p.CategoryId.Value, "—")), Col<Product>("Price", p => Peso(p.SellingPrice)), Col<Product>("Stock", p => $"{p.StockQuantity:0.######} {p.StockUnit}"), Col<Product>("Use", p => p.IsRetail ? "Retail" : "Internal"), Col<Product>("Supplier", p => p.SupplierId == null ? "—" : suppliers.GetValueOrDefault(p.SupplierId.Value, "—")), Col<Product>("Status", p => Status(p.IsActive))],
            _ => Fields(new("Name", "Product name", true), new("Sku", "SKU"), new("Barcode", "Barcode", Help: "A keyboard barcode scanner can enter this field."), new("CategoryId", "Category", Choices: Choices<ProductCategory>(c => c.Name, true)), new("Brand", "Brand"), new("SupplierId", "Supplier", Choices: Choices<Supplier>(s => s.Name, true)), new("PurchaseCost", "Default purchase cost (₱)", Min: 0), new("SellingPrice", "Selling price (₱)", true, Min: 0), new("StockUnit", "Stock unit", true, Help: "Examples: bottle, piece, gram, milliliter."), new("ReorderLevel", "Low stock threshold", Min: 0), new("ImagePath", "Product image file path", Help: "Use a local image file available on this computer."), new("ExpirationDateUtc", "Expiration date"), new("IsRetail", "Available for retail sale"), new("CostInformationComplete", "Purchase cost is confirmed"), new("IsActive", "Active product")),
            [new("Movement history", p => ProductHistory(p))]);
            page.Model.Reloading += () => { categories.Clear(); foreach (var item in Read<ProductCategory>()) categories[item.Id] = item.Name; suppliers.Clear(); foreach (var item in Read<Supplier>()) suppliers[item.Id] = item.Name; };
            page.Toolbar.Children.Add(ManagementUi.Button("Categories", new UiCommand(() => { ProductCategories(); page.Model.Reload(); })));
            page.Toolbar.Children.Add(ManagementUi.Button("Suppliers", new UiCommand(() => { Suppliers(); page.Model.Reload(); })));
            void Summary() { var rows = page.Model.Items; page.SummaryRegion.Children.Clear(); page.SummaryRegion.Children.Add(ManagementUi.MetricRow(ManagementUi.Metric("Retail catalog", rows.Count(p => p.IsRetail && p.IsActive).ToString(), "Products available to sell", "▣"), ManagementUi.Metric("Salon supplies", rows.Count(p => !p.IsRetail && p.IsActive).ToString(), "Consumables for treatments", "◇"), ManagementUi.Metric("Low stock", rows.Count(p => p.IsActive && p.StockQuantity <= p.ReorderLevel).ToString(), "At or below reorder level", "!", "#C88B35"))); }
            page.Model.Reloaded += Summary; Summary();
            page.SetCardView(p =>
            {
                var card = new StackPanel(); card.Children.Add(Ui.Badge(p.IsRetail ? "RETAIL PRODUCT" : "SALON SUPPLY", size: 10));
                var identity = new Grid { Margin = new Thickness(0, 14, 0, 0) }; identity.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); identity.ColumnDefinitions.Add(new ColumnDefinition()); identity.Children.Add(ManagementUi.ProductThumbnail(p.ImagePath));
                var label = new StackPanel { VerticalAlignment = VerticalAlignment.Center }; var name = ManagementUi.Text(p.Name, 17, ManagementUi.Ink, true); name.Margin = new Thickness(0, 0, 0, 4); label.Children.Add(name); label.Children.Add(ManagementUi.Text($"{p.Brand} · {p.Sku}".Trim(' ', '·'), 12, ManagementUi.Muted)); Grid.SetColumn(label, 1); identity.Children.Add(label); card.Children.Add(identity);
                var price = ManagementUi.Text(Peso(p.SellingPrice), 25, Ui.Brush("#A85E67"), true); price.Margin = new Thickness(0, 10, 0, 6); card.Children.Add(price);
                card.Children.Add(ManagementUi.Text($"{p.StockQuantity:0.######} {p.StockUnit} on hand", 12, ManagementUi.Muted)); var status = Ui.StatusPill(!p.IsActive ? "Inactive" : p.StockQuantity <= p.ReorderLevel ? "Low stock" : "Active"); status.Margin = new Thickness(0, 12, 0, 0); card.Children.Add(status); return card;
            });
            return page;
        }

        private void ProductCategories() => Show("Product categories", new EntityPage<ProductCategory>(service, user, "Product categories", "Organize your encoded retail products and consumables.", "category",
            [Col<ProductCategory>("Category", c => c.Name, width: 3), Col<ProductCategory>("Status", c => Status(c.IsActive))],
            _ => Fields(new("Name", "Category name", true), new("IsActive", "Active category"))));

        private void Suppliers() => Show("Suppliers", new EntityPage<Supplier>(service, user, "Suppliers", "Local supplier contact information for purchasing and stock records.", "supplier",
            [Col<Supplier>("Supplier", s => s.Name, width: 2), Col<Supplier>("Contact", s => s.Contact), Col<Supplier>("Address", s => s.Address, width: 2)],
            _ => Fields(new("Name", "Supplier name", true), new("Contact", "Contact details"), new("Address", "Address", MultiLine: true), new("Notes", "Notes", MultiLine: true)), canDisable: false));

        public FrameworkElement Inventory()
        {
            var page = new EntityPage<Product>(service, user, "Inventory", "Every stock change keeps its quantity, reason, employee and cost history. Incoming stock uses weighted average cost.", "product",
            [Col<Product>("Product", p => p.Name, width: 2), Col<Product>("On hand", p => $"{p.StockQuantity:0.######} {p.StockUnit}"), Col<Product>("Average cost", p => Peso(p.AverageCost)), Col<Product>("Stock value", p => Peso(p.InventoryValue)), Col<Product>("Reorder at", p => p.ReorderLevel), Col<Product>("Expiration", p => p.ExpirationDateUtc?.ToString("dd MMM yyyy") ?? "—"), Col<Product>("Alert", p => p.StockQuantity <= p.ReorderLevel ? "LOW STOCK" : "In stock")],
            _ => [], [new("+ Stock in", p => StockChange(p, 1)), new("− Stock out", p => StockChange(p, -1)), new("Set counted stock", p => StockChange(p, 0)), new("Movement history", p => ProductHistory(p))],
            canAdd: false, canEdit: false, canDisable: false);
            void Summary()
            {
                var rows = page.Model.Items; page.SummaryRegion.Children.Clear();
                page.SummaryRegion.Children.Add(ManagementUi.MetricRow(ManagementUi.Metric("Inventory valuation", Peso(rows.Sum(p => p.InventoryValue)), "Recorded weighted average cost", "▥"), ManagementUi.Metric("Low stock", rows.Count(p => p.IsActive && p.StockQuantity <= p.ReorderLevel).ToString(), "Products to replenish", "!", "#C88B35"), ManagementUi.Metric("Expiring soon", rows.Count(p => p.IsActive && p.ExpirationDateUtc != null && p.ExpirationDateUtc <= DateTime.UtcNow.AddDays(30)).ToString(), "Expiration within 30 days", "◇", "#C88B35")));
            }
            page.Model.Reloaded += Summary; Summary();
            page.Toolbar.Children.Add(ManagementUi.Button("All movements", new UiCommand(() => MovementHistory(null))));
            page.Toolbar.Children.Add(ManagementUi.Button("Suppliers", new UiCommand(Suppliers)));
            return page;
        }

        private void StockChange(Product product, int direction)
        {
            var draft = new StockDraft { Quantity = direction == 0 ? product.StockQuantity : 1, UnitCost = product.PurchaseCost > 0 ? product.PurchaseCost : product.AverageCost };
            var title = direction switch { 1 => "Stock in", -1 => "Stock out", _ => "Set counted stock" };
            EntityForm.Edit(draft, $"{title} · {product.Name}", Fields(
                new("Quantity", direction == 0 ? $"Counted quantity ({product.StockUnit})" : $"Quantity ({product.StockUnit})", true, Min: direction == 0 ? 0 : 0.0001m),
                new("UnitCost", "Purchase cost per unit (₱)", true, Help: direction == 1 ? "Updates weighted average cost for incoming stock." : "Outgoing stock uses its existing weighted average cost.", Min: 0),
                new("Reason", "Reason / purchase reference", true, MultiLine: true)),
                d =>
                {
                    if (direction == 0) service.SetStockCount(product.Id, d.Quantity, d.Reason, user);
                    else service.AdjustStock(product.Id, d.Quantity * direction, d.UnitCost, d.Reason, user);
                });
        }

        private void ProductHistory(Product product) => MovementHistory(product.Id);
        private void MovementHistory(Guid? productId)
        {
            var products = Read<Product>().ToDictionary(p => p.Id, p => p.Name);
            var rows = Read<InventoryMovement>().Where(m => productId == null || m.ProductId == productId).OrderByDescending(m => m.OccurredAtUtc).Select(m => new { Date = Local(m.OccurredAtUtc), Product = products.GetValueOrDefault(m.ProductId, "Archived product"), m.Type, m.Quantity, Cost = Peso(m.UnitCost), Balance = m.BalanceAfter, m.Reason }).ToList();
            var panel = new StackPanel(); panel.Children.Add(ManagementUi.Text(productId == null ? "Inventory movement history" : products.GetValueOrDefault(productId.Value, "Product history"), 24, ManagementUi.Ink, true));
            var note = ManagementUi.Text("Recorded stock entries and usage retain their original quantity, unit cost and reason.", 13, ManagementUi.Muted); note.Margin = new Thickness(0, 8, 0, 22); panel.Children.Add(note);
            var grid = ManagementUi.Table(rows, ("Date", "Date", "dd MMM yyyy HH:mm"), ("Product", "Product", null), ("Type", "Type", null), ("Change", "Quantity", "0.####"), ("Unit cost", "Cost", null), ("Balance", "Balance", "0.####"), ("Reason", "Reason", null)); grid.MaxHeight = 430;
            panel.Children.Add(ManagementUi.Card(rows.Count == 0 ? ManagementUi.Empty("No stock activity yet", "Stock purchases, counted corrections, sales and service consumption will appear here.") : grid));
            var count = ManagementUi.Text($"{rows.Count:N0} recorded movements", 11, ManagementUi.Muted); count.Margin = new Thickness(0, 6, 0, 0); panel.Children.Add(count);
            ManagementUi.ShowDetails("Inventory history", panel);
        }

        public FrameworkElement Customers()
        {
            List<CustomerPackage> packages = [];
            var page = new EntityPage<Customer>(service, user, "Customers", "A personal view of every client, visit and prepaid treatment.", "customer",
            [Col<Customer>("Full name", c => c.FullName, width: 2, content: c => ManagementUi.Profile(c.FullName, c.Phone)), Col<Customer>("Phone", c => c.Phone), Col<Customer>("Email", c => c.Email, width: 2), Col<Customer>("Notes", c => c.Notes, width: 2), Col<Customer>("Status", c => Status(c.IsActive))],
            _ => Fields(new("FullName", "Full name", true), new("Phone", "Phone number"), new("Email", "Email (optional)"), new("Notes", "Private customer notes", MultiLine: true), new("IsActive", "Active customer")),
            [new("Visit & purchase history", c => CustomerHistory(c)), new("Packages & sessions", c => CustomerPackages(c.Id))],
            validate: c => { if (!string.IsNullOrWhiteSpace(c.Email) && !Regex.IsMatch(c.Email, @"^[^\s@]+@[^\s@]+\.[^\s@]+$")) throw new InvalidOperationException("Enter a valid email address or leave it empty."); }, canAdd: user.Role != RoleKind.SalonStaff, canEdit: user.Role != RoleKind.SalonStaff, canDisable: user.Role == RoleKind.Administrator);
            void Summary() { packages = Read<CustomerPackage>(); page.SummaryRegion.Children.Clear(); page.SummaryRegion.Children.Add(ManagementUi.MetricRow(ManagementUi.Metric("Client directory", page.Model.Items.Count.ToString(), "Authorized customer records", "♙"), ManagementUi.Metric("Active customers", page.Model.Items.Count(c => c.IsActive).ToString(), "Ready for visits and bookings", "◇"), ManagementUi.Metric("Prepaid packages", packages.Count(p => !p.IsCancelled && p.RemainingSessions > 0 && (p.ExpiresAtUtc == null || p.ExpiresAtUtc >= DateTime.UtcNow)).ToString(), "Available treatment entitlements", "✦"))); }
            page.Model.Reloaded += Summary; Summary();
            page.SetCardView(c => { var card = new StackPanel(); card.Children.Add(ManagementUi.Profile(c.FullName, c.Phone)); var email = ManagementUi.Text(string.IsNullOrWhiteSpace(c.Email) ? "No email recorded" : c.Email, 12, ManagementUi.Muted); email.Margin = new Thickness(0, 14, 0, 10); card.Children.Add(email); card.Children.Add(Ui.StatusPill(c.IsActive ? "Active" : "Inactive")); return card; }); return page;
        }

        private void CustomerHistory(Customer customer)
        {
            var panel = new StackPanel(); panel.Children.Add(ManagementUi.Profile(customer.FullName, string.Join(" · ", new[] { customer.Phone, customer.Email }.Where(s => !string.IsNullOrWhiteSpace(s)))));
            var status = Ui.StatusPill(customer.IsActive ? "Active" : "Inactive"); status.Margin = new Thickness(0, 16, 0, 18); panel.Children.Add(status);
            var sales = Read<Sale>().Where(s => s.CustomerId == customer.Id).OrderByDescending(s => s.CompletedAtUtc).ToList();
            var available = Read<CustomerPackage>().Count(p => p.CustomerId == customer.Id && !p.IsCancelled && p.RemainingSessions > 0 && (p.ExpiresAtUtc == null || p.ExpiresAtUtc >= DateTime.UtcNow));
            var completedVisits = Read<Appointment>().Count(a => a.CustomerId == customer.Id && a.Status == AppointmentStatus.Completed);
            panel.Children.Add(ManagementUi.MetricRow(ManagementUi.Metric("Transactions", sales.Count.ToString(), "Visible purchase history entries", "◈"), ManagementUi.Metric("Completed visits", completedVisits.ToString(), "Completed appointment records", "◇"), ManagementUi.Metric("Available packages", available.ToString(), "Prepaid treatments to continue", "✦")));
            if (!string.IsNullOrWhiteSpace(customer.Notes)) { var notes = new StackPanel(); notes.Children.Add(ManagementUi.Text("Customer notes", 14, ManagementUi.Ink, true)); var copy = ManagementUi.Text(customer.Notes, 13, ManagementUi.Muted); copy.Margin = new Thickness(0, 8, 0, 0); notes.Children.Add(copy); panel.Children.Add(ManagementUi.Card(notes)); }
            Section(panel, "Purchase history", ManagementUi.Table(sales.Select(s => new { Date = Local(s.CompletedAtUtc), s.TransactionNumber, Total = Peso(s.Total), s.Status }).ToList(), ("Date", "Date", "dd MMM yyyy HH:mm"), ("Transaction", "TransactionNumber", null), ("Total", "Total", null), ("Status", "Status", null)), sales.Count);
            var saleIds = sales.Select(s => s.Id).ToHashSet();
            var items = Read<SaleItem>().Where(i => saleIds.Contains(i.SaleId)).ToList();
            Section(panel, "Service & product history", ManagementUi.Table(items, ("Service / item", "Name", null), ("Type", "Kind", null), ("Quantity", "Quantity", "0.####"), ("Paid total", "Total", "₱#,##0.00")), items.Count);
            var services = Read<SalonService>().ToDictionary(s => s.Id, s => s.Name);
            var appointments = Read<Appointment>().Where(a => a.CustomerId == customer.Id).OrderByDescending(a => a.StartsAtUtc).Select(a => new { Date = Local(a.StartsAtUtc), Service = services.GetValueOrDefault(a.ServiceId, "Archived service"), a.Status, a.Notes }).ToList();
            Section(panel, "Appointment history", ManagementUi.Table(appointments, ("Date", "Date", "dd MMM yyyy HH:mm"), ("Service", "Service", null), ("Status", "Status", null), ("Notes", "Notes", null)), appointments.Count);
            ManagementUi.ShowDetails("Customer history", panel);
        }

        private static void Section(StackPanel panel, string title, FrameworkElement table, int count)
        {
            var heading = ManagementUi.Text($"{title} · {count}", 18, ManagementUi.Ink, true); heading.Margin = new Thickness(0, 24, 0, 10); panel.Children.Add(heading);
            table.MaxHeight = 280; panel.Children.Add(ManagementUi.Card(count == 0 ? ManagementUi.Empty("No records yet", "Recorded activity will remain available in this history.") : table, new Thickness(12)));
        }

        public FrameworkElement Expenses()
        {
            var page = new EntityPage<Expense>(service, user, "Expenses", "Track recorded spending with a clear view of operating expenses and commission payouts.", "expense",
            [Col<Expense>("Date", e => Local(e.OccurredAtUtc), "dd MMM yyyy"), Col<Expense>("Category", e => e.Category), Col<Expense>("Description", e => e.Description, width: 3), Col<Expense>("Amount", e => Peso(e.Amount)), Col<Expense>("Payment", e => e.PaymentMethod), Col<Expense>("Reference", e => e.ReferenceNumber)],
            _ => [], [new("Delete expense", e => DeleteExpense(e))],
            initialize: e => { e.RecordedByUserId = user.Id; e.OccurredAtUtc = Local(DateTime.UtcNow).Date; }, canDisable: false,
            editor: e =>
            {
                if (e.OccurredAtUtc.Kind == DateTimeKind.Utc) e.OccurredAtUtc = Local(e.OccurredAtUtc).Date;
                var fields = new List<FieldSpec>();
                if (e.CommissionRecordId == null) { fields.Add(new("Category", "Expense category", true, Choices: new[] { e.Category }.Concat(new[] { "Rent", "Electricity", "Water", "Salaries", "Marketing", "Maintenance", "Miscellaneous" }).Where(c => !string.IsNullOrWhiteSpace(c)).Distinct(StringComparer.OrdinalIgnoreCase).Select(c => new Choice(c, c)).ToList())); fields.Add(new("Amount", "Amount (₱)", true, Min: 0.01m)); }
                fields.AddRange([new("Description", "Description", true, MultiLine: true), new("OccurredAtUtc", "Expense date", true), new("PaymentMethod", "Payment method"), new("ReferenceNumber", "Reference number")]);
                return EntityForm.Edit(e, e.CommissionRecordId == null ? "Expense details" : $"Commission payout · {Peso(e.Amount)}", fields, entry => { entry.RecordedByUserId = user.Id; entry.OccurredAtUtc = Utc(entry.OccurredAtUtc.Date); Save(entry); });
            });
            void Summary()
            {
                var today = Local(DateTime.UtcNow); var rows = page.Model.Items.Where(e => Local(e.OccurredAtUtc).Month == today.Month && Local(e.OccurredAtUtc).Year == today.Year).ToList();
                page.SummaryRegion.Children.Clear(); page.SummaryRegion.Children.Add(ManagementUi.MetricRow(ManagementUi.Metric("Recorded this month", Peso(rows.Sum(e => e.Amount)), today.ToString("MMMM yyyy") + " · expense entries", "◈"), ManagementUi.Metric("Operating entries", Peso(rows.Where(e => e.CommissionRecordId == null).Sum(e => e.Amount)), "Excludes linked commission payouts", "◇"), ManagementUi.Metric("Commission payouts", Peso(rows.Where(e => e.CommissionRecordId != null).Sum(e => e.Amount)), "Already accrued in profit reporting", "♧")));
                var categories = new WrapPanel();
                foreach (var group in rows.GroupBy(e => e.Category).OrderByDescending(g => g.Sum(e => e.Amount))) { var badge = Ui.Badge($"{group.Key}  {Peso(group.Sum(e => e.Amount))}", "#FFFFFF", "#77727B"); badge.Margin = new Thickness(0, 0, 8, 10); categories.Children.Add(badge); }
                if (rows.Count == 0) categories.Children.Add(ManagementUi.Text("No expenses recorded this month. Add an expense to begin your spending history.", 12, ManagementUi.Muted));
                categories.Margin = new Thickness(0, 0, 0, 10); page.SummaryRegion.Children.Add(categories);
            }
            page.Model.Reloaded += Summary; Summary(); return page;
        }

        public FrameworkElement Packages()
        {
            var panel = new DockPanel();
            var header = new StackPanel();
            var note = ManagementUi.Text("Prepaid treatment journeys · purchases are recorded through Point of Sale; completed visits consume existing sessions.", 13, ManagementUi.Muted); note.Margin = new Thickness(0, 0, 0, 12); header.Children.Add(note);
            DockPanel.SetDock(header, Dock.Top); panel.Children.Add(header);
            var selectors = new StackPanel { Orientation = Orientation.Horizontal }; header.Children.Add(selectors);
            var content = new ContentControl { HorizontalContentAlignment = HorizontalAlignment.Stretch, VerticalContentAlignment = VerticalAlignment.Stretch }; panel.Children.Add(content);
            FrameworkElement? sessions = null; FrameworkElement? offers = null;
            void Select(bool showOffers)
            {
                selectors.Children.Clear(); selectors.Children.Add(Ui.Pill("Customer sessions", () => Select(false), !showOffers));
                if (user.Role == RoleKind.Administrator) selectors.Children.Add(Ui.Pill("Package offers", () => Select(true), showOffers));
                content.Content = showOffers ? (offers ??= PackageOfferPage(null)) : (sessions ??= CustomerPackagePage(null));
            }
            Select(false); return panel;
        }

        private FrameworkElement PackageOfferPage(Guid? serviceId)
        {
            var serviceNames = Read<SalonService>().ToDictionary(s => s.Id, s => s.Name);
            var page = new EntityPage<PackageOffer>(service, user, "Package offers", "Treatment bundles with editable prices and session counts. Existing purchases keep their original terms.", "package",
            [Col<PackageOffer>("Offer", p => p.Name, width: 2), Col<PackageOffer>("Service", p => serviceNames.GetValueOrDefault(p.ServiceId, "Archived service"), width: 2), Col<PackageOffer>("Price", p => Peso(p.Price)), Col<PackageOffer>("Sessions", p => p.TotalSessions), Col<PackageOffer>("Validity", p => p.ValidityDays == null ? "No expiry" : $"{p.ValidityDays} days"), Col<PackageOffer>("Status", p => Status(p.IsActive))],
            _ => Fields(new("Name", "Package name", true), new("ServiceId", "Service", true, Choices: Choices<SalonService>(s => s.Name)), new("Price", "Package price (₱)", true, Min: 0), new("TotalSessions", "Included sessions", true, Min: 1), new("ValidityDays", "Validity (days)", Help: "Leave empty for no expiry. Applies to new purchases.", Min: 1), new("IsActive", "Active offer")),
            [new("Delete package", p => DeletePackageOffer(p))],
            scope: rows => rows.Where(p => serviceId == null || p.ServiceId == serviceId), initialize: p => { if (serviceId != null) { p.ServiceId = serviceId.Value; var s = Read<SalonService>().Single(s => s.Id == serviceId.Value); p.Name = $"{s.Name} 5+1"; p.Price = s.PackagePrice ?? s.BasePrice * 5; } });
            page.SetCardView(p =>
            {
                var card = new StackPanel(); card.Children.Add(Ui.Badge($"{p.TotalSessions} SESSIONS", "#FAF0F0", "#A85E67", 10)); var name = ManagementUi.Text(p.Name, 18, ManagementUi.Ink, true); name.Margin = new Thickness(0, 14, 0, 6); card.Children.Add(name);
                card.Children.Add(ManagementUi.Text(serviceNames.GetValueOrDefault(p.ServiceId, "Archived service"), 12, ManagementUi.Muted)); var price = ManagementUi.Text(Peso(p.Price), 27, Ui.Brush("#A85E67"), true); price.Margin = new Thickness(0, 14, 0, 8); card.Children.Add(price);
                card.Children.Add(ManagementUi.Text(p.ValidityDays == null ? "No session expiry" : $"Valid for {p.ValidityDays} days after purchase", 12, ManagementUi.Muted)); var status = Ui.StatusPill(p.IsActive ? "Active" : "Inactive"); status.Margin = new Thickness(0, 14, 0, 0); card.Children.Add(status); return card;
            }, true); return page;
        }
        private void PackageOffers(Guid serviceId) => Show("Package offers", PackageOfferPage(serviceId));

        public void DeleteService(SalonService salonService) => Delete("Delete service", salonService.Name,
            "This service will be removed from the catalog and future transactions. Existing transactions, appointment history, and prepaid customer sessions remain available.",
            (pin, reason) => service.DeleteService(salonService.Id, pin, user, reason));

        public void DeletePackageOffer(PackageOffer offer) => Delete("Delete package", $"{offer.Name} · {offer.TotalSessions} sessions · {Peso(offer.Price)}",
            "This offer will be removed from the catalog and future purchases. Customers can continue using sessions they already purchased, with their original price and expiry terms.",
            (pin, reason) => service.DeletePackageOffer(offer.Id, pin, user, reason));

        public void DeleteExpense(Expense expense) => Delete("Delete expense", $"{expense.Description} · {Peso(expense.Amount)}",
            $"{expense.Category} · {Local(expense.OccurredAtUtc):dd MMM yyyy}. This expense will be voided and removed from active spending totals. Its original record and deletion reason remain in the audit history." +
            (expense.CommissionRecordId != null ? " The accrued commission remains payable and its payout can be recorded again." : ""),
            (pin, reason) => service.VoidExpense(expense.Id, pin, user, reason));

        private void Delete(string title, string target, string description, Action<string, string> commit)
        {
            if (auth == null) throw new InvalidOperationException("The account service is unavailable. Reopen this page from the application navigation.");
            if (user.Role != RoleKind.Administrator) throw new UnauthorizedAccessException("Administrator access is required to delete records.");
            ProtectedDeleteDialog.Show(title, target, description, () => auth.IsSuperPinEnabled(user), () => ShowSuperPinSettings(auth, user), commit);
        }

        private void CustomerPackages(Guid customerId) => Show("Customer packages", CustomerPackagePage(customerId));
        private FrameworkElement CustomerPackagePage(Guid? customerId)
        {
            var names = Read<Customer>().ToDictionary(c => c.Id, c => c.FullName);
            var page = new EntityPage<CustomerPackage>(service, user, "Customer sessions", "Select a package to redeem a visit or explore its treatment history.", "customer package",
            [Col<CustomerPackage>("Customer", p => names.GetValueOrDefault(p.CustomerId, "Archived customer"), width: 2), Col<CustomerPackage>("Package", p => p.Name, width: 2), Col<CustomerPackage>("Purchased", p => Local(p.PurchasedAtUtc), "dd MMM yyyy"), Col<CustomerPackage>("Paid", p => Peso(p.PurchasePrice)), Col<CustomerPackage>("Used / total", p => $"{p.UsedSessions} / {p.TotalSessions}"), Col<CustomerPackage>("Remaining", p => p.RemainingSessions), Col<CustomerPackage>("Expiry", p => p.ExpiresAtUtc == null ? null : Local(p.ExpiresAtUtc.Value), content: p => ManagementUi.Text(p.ExpiresAtUtc == null ? "No expiry" : Local(p.ExpiresAtUtc.Value).ToString("dd MMM yyyy"), 12, ManagementUi.Muted)), Col<CustomerPackage>("Status", p => p.IsCancelled ? "Cancelled" : p.ExpiresAtUtc < DateTime.UtcNow ? "Expired" : p.RemainingSessions == 0 ? "Completed" : "Available")],
            _ => [], [new("Redeem one session", p => Redeem(p)), new("Redemption history", p => RedemptionHistory(p))],
            scope: rows => rows.Where(p => customerId == null || p.CustomerId == customerId), canAdd: false, canEdit: false, canDisable: false);
            void Summary()
            {
                var rows = page.Model.Items; var available = rows.Where(p => !p.IsCancelled && p.RemainingSessions > 0 && (p.ExpiresAtUtc == null || p.ExpiresAtUtc >= DateTime.UtcNow)).ToList();
                page.SummaryRegion.Children.Clear(); page.SummaryRegion.Children.Add(ManagementUi.MetricRow(ManagementUi.Metric("Active packages", available.Count.ToString(), "Paid entitlements available to use", "✦"), ManagementUi.Metric("Sessions remaining", available.Sum(p => p.RemainingSessions).ToString(), "Across available packages", "◇"), ManagementUi.Metric("Expiring soon", available.Count(p => p.ExpiresAtUtc != null && p.ExpiresAtUtc <= DateTime.UtcNow.AddDays(30)).ToString(), "Available packages within 30 days", "!", "#C88B35")));
            }
            page.Model.Reloaded += Summary; Summary();
            page.SetCardView(p =>
            {
                var card = new StackPanel(); var status = p.IsCancelled ? "Cancelled" : p.ExpiresAtUtc < DateTime.UtcNow ? "Expired" : p.RemainingSessions == 0 ? "Completed" : "Available";
                card.Children.Add(Ui.StatusPill(status)); var name = ManagementUi.Text(p.Name, 18, ManagementUi.Ink, true); name.Margin = new Thickness(0, 12, 0, 4); card.Children.Add(name); card.Children.Add(ManagementUi.Text(names.GetValueOrDefault(p.CustomerId, "Archived customer"), 13, ManagementUi.Muted));
                var balance = ManagementUi.Text($"{p.RemainingSessions} sessions remaining", 22, Ui.Brush("#A85E67"), true); balance.Margin = new Thickness(0, 18, 0, 9); card.Children.Add(balance);
                card.Children.Add(new ProgressBar { Maximum = p.TotalSessions, Value = p.UsedSessions, Height = 7, BorderThickness = new Thickness(0), Foreground = ManagementUi.Rose, Background = Ui.Brush("#F3E7E7"), ToolTip = $"{p.UsedSessions} of {p.TotalSessions} sessions completed" });
                var visits = ManagementUi.Text($"{p.UsedSessions} of {p.TotalSessions} visits completed · paid {Peso(p.PurchasePrice)}", 11, ManagementUi.Muted); visits.Margin = new Thickness(0, 7, 0, 14); card.Children.Add(visits);
                var soon = p.ExpiresAtUtc != null && p.ExpiresAtUtc >= DateTime.UtcNow && p.ExpiresAtUtc <= DateTime.UtcNow.AddDays(30);
                card.Children.Add(Ui.Badge(p.ExpiresAtUtc == null ? "No expiry date" : $"{(soon ? "Expires soon" : "Expires")} · {Local(p.ExpiresAtUtc.Value):dd MMM yyyy}", soon ? "#FEF3C7" : "#F8F8FA", soon ? "#C88B35" : "#77727B", 10)); return card;
            }, true); return page;
        }

        private void Redeem(CustomerPackage package)
        {
            if (package.IsCancelled || package.RemainingSessions <= 0 || package.ExpiresAtUtc < DateTime.UtcNow) throw new InvalidOperationException("This package is cancelled, expired or has no sessions remaining.");
            var draft = new RedemptionDraft { EmployeeId = user.EmployeeId };
            EntityForm.Edit(draft, $"Complete session · {package.Name}", Fields(new FieldSpec("EmployeeId", "Assigned staff", user.Role == RoleKind.SalonStaff, Choices: Choices<Employee>(e => e.FullName, user.Role != RoleKind.SalonStaff, e => e.IsActive && (user.Role != RoleKind.SalonStaff || e.Id == user.EmployeeId)))),
                d => { if (!ManagementUi.Confirm($"Complete one {package.Name} session for {Name<Customer>(package.CustomerId, c => c.FullName)}? Stock and session balances will update.")) throw new InvalidOperationException("Session completion cancelled."); service.RedeemPackage(package.Id, d.EmployeeId, user); });
        }

        private void RedemptionHistory(CustomerPackage package)
        {
            var employees = Read<Employee>().ToDictionary(e => e.Id, e => e.FullName);
            var rows = Read<PackageRedemption>().Where(r => r.CustomerPackageId == package.Id).OrderByDescending(r => r.RedeemedAtUtc).Select(r => new { Date = Local(r.RedeemedAtUtc), Staff = r.EmployeeId == null ? "Unassigned" : employees.GetValueOrDefault(r.EmployeeId.Value, "Archived staff"), r.SessionsUsed, Materials = Peso(r.MaterialCost), Commission = Peso(r.CommissionCost) }).ToList();
            var panel = new StackPanel(); panel.Children.Add(ManagementUi.Text(package.Name, 24, ManagementUi.Ink, true)); panel.Children.Add(ManagementUi.Text($"{package.RemainingSessions} of {package.TotalSessions} sessions remaining", 15, ManagementUi.Muted));
            var progress = new ProgressBar { Maximum = package.TotalSessions, Value = package.UsedSessions, Height = 8, BorderThickness = new Thickness(0), Foreground = ManagementUi.Rose, Background = Ui.Brush("#F3E7E7"), Margin = new Thickness(0, 20, 0, 12), ToolTip = $"{package.UsedSessions} sessions completed" }; panel.Children.Add(progress);
            panel.Children.Add(ManagementUi.Text(package.ExpiresAtUtc == null ? $"Purchased {Local(package.PurchasedAtUtc):dd MMM yyyy} · no session expiry" : $"Purchased {Local(package.PurchasedAtUtc):dd MMM yyyy} · expires {Local(package.ExpiresAtUtc.Value):dd MMM yyyy}", 12, ManagementUi.Muted));
            var columns = new List<(string Header, string Path, string? Format)> { ("Date", "Date", "dd MMM yyyy HH:mm"), ("Staff", "Staff", null), ("Sessions", "SessionsUsed", null) };
            if (user.Role == RoleKind.Administrator) columns.AddRange([("Materials", "Materials", null), ("Commission", "Commission", null)]);
            Section(panel, "Completed visits", ManagementUi.Table(rows, columns.ToArray()), rows.Count); ManagementUi.ShowDetails("Session history", panel);
        }
    }

    private sealed class StockDraft { public decimal Quantity { get; set; } public decimal UnitCost { get; set; } public string Reason { get; set; } = ""; }
    private sealed class RedemptionDraft { public Guid? EmployeeId { get; set; } }
}
