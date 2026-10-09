using Microsoft.EntityFrameworkCore;
using Roselie.Core.Finance;
using Roselie.Core.Catalog;
using Roselie.Core.Models;

namespace Roselie.Infrastructure;

public sealed partial class BusinessService
{
    private readonly DatabaseService _database;
    private readonly AuthService _auth;
    public BusinessService(DatabaseService database, AuthService auth) { _database = database; _auth = auth; }
    private UserAccount Require(UserAccount? user = null, bool admin = false) => _auth.Require(user ?? _auth.CurrentUser ?? throw new UnauthorizedAccessException("Sign in before continuing."), admin);
    public List<Product> ListMaterialProducts(UserAccount user)
    {
        var account=Require(user);
        lock(_database.Gate)
        {
            using var db=_database.OpenContext();var products=db.Set<Product>().AsNoTracking().Where(p=>p.IsActive).ToList();
            if(account.Role==RoleKind.Administrator) return products;
            return products.Select(p=>new Product {Id=p.Id,Name=p.Name,Sku=p.Sku,StockUnit=p.StockUnit,StockQuantity=p.StockQuantity,IsRetail=p.IsRetail,IsActive=p.IsActive,ExpirationDateUtc=p.ExpirationDateUtc}).ToList();
        }
    }
    public List<ServicePriceRule> ListPriceRules(UserAccount user,Guid serviceId)
    {
        var account=Require(user);lock(_database.Gate) {using var db=_database.OpenContext();return db.Set<ServicePriceRule>().AsNoTracking().Where(r=>r.ServiceId==serviceId && (!r.RequiresAuthorization || account.AllowCustomPrices)).OrderBy(r=>r.Name).ToList();}
    }
    public List<T> List<T>(UserAccount? user = null) where T : Entity
    {
        var account = Require(user);
        lock (_database.Gate)
        {
            using var db = _database.OpenContext();
            var type = typeof(T);
            var publicSelectors = new[] { typeof(SalonService),typeof(ServiceCategory),typeof(Product),typeof(ProductCategory),typeof(ServiceChargePreset),typeof(ServiceMaterialRequirement),typeof(PackageOffer),typeof(Employee) };
            if (account.Role != RoleKind.Administrator && !publicSelectors.Contains(type) && type != typeof(Customer) && type != typeof(CustomerPackage) && type != typeof(Appointment) && type != typeof(Sale) && type != typeof(SaleItem) && type != typeof(SaleItemCharge) && type != typeof(SaleItemDiscount) && type != typeof(Payment) && type != typeof(ReceiptRecord) && type != typeof(PackageRedemption))
                throw new UnauthorizedAccessException("This account cannot view these records.");
            var rows = db.Set<T>().AsNoTracking().ToList();
            if(type == typeof(SalonService))rows = rows.Cast<SalonService>().Where(s => !s.IsDeleted).Cast<T>().ToList();
            if(type == typeof(PackageOffer))rows = rows.Cast<PackageOffer>().Where(p => !p.IsDeleted).Cast<T>().ToList();
            if(type == typeof(Expense))rows = rows.Cast<Expense>().Where(e => !e.IsVoided).Cast<T>().ToList();
            if (type == typeof(UserAccount)) foreach (var a in rows.Cast<UserAccount>()) { a.PasswordHash = ""; a.RecoveryCodeHash = null; a.PinHash = null;a.PinLookupHash = null;a.SuperPinHash = null; }
            if (account.Role == RoleKind.Administrator) return rows;
            if (type == typeof(Employee)) return rows.Cast<Employee>().Select(e => new Employee { Id=e.Id,FullName=e.FullName,IsActive=e.IsActive,Position=e.Position }).Cast<T>().ToList();
            if (type == typeof(Product)) return rows.Cast<Product>().Where(p => p.IsRetail).Select(p => new Product { Id=p.Id,Name=p.Name,Sku=p.Sku,Barcode=p.Barcode,CategoryId=p.CategoryId,Brand=p.Brand,SellingPrice=p.SellingPrice,StockUnit=p.StockUnit,StockQuantity=p.StockQuantity,ReorderLevel=p.ReorderLevel,ImagePath=p.ImagePath,ExpirationDateUtc=p.ExpirationDateUtc,IsRetail=p.IsRetail,IsActive=p.IsActive }).Cast<T>().ToList();
            var saleIds = db.Set<Sale>().Where(s => s.CashierUserId == account.Id).Select(s => s.Id).ToHashSet();
            var lineIds = db.Set<SaleItem>().Where(s => saleIds.Contains(s.SaleId)).Select(s => s.Id).ToHashSet();
            if (type == typeof(Sale)) return rows.Cast<Sale>().Where(s => saleIds.Contains(s.Id)).Select(s => { s.CostOfGoods=0; s.MaterialCost=0; s.CommissionCost=0; return s; }).Cast<T>().ToList();
            if (type == typeof(SaleItem)) return rows.Cast<SaleItem>().Where(s => lineIds.Contains(s.Id)).Select(s => { s.UnitCost=0;s.CostOfGoods=0;s.MaterialCost=0;s.CommissionCost=0;return s; }).Cast<T>().ToList();
            if (type == typeof(SaleItemCharge)) return rows.Cast<SaleItemCharge>().Where(s => lineIds.Contains(s.SaleItemId)).Cast<T>().ToList();
            if (type == typeof(SaleItemDiscount)) return rows.Cast<SaleItemDiscount>().Where(s => lineIds.Contains(s.SaleItemId)).Cast<T>().ToList();
            if (type == typeof(Payment)) return rows.Cast<Payment>().Where(s => saleIds.Contains(s.SaleId)).Cast<T>().ToList();
            if (type == typeof(ReceiptRecord)) return rows.Cast<ReceiptRecord>().Where(s => saleIds.Contains(s.SaleId)).Cast<T>().ToList();
            if (type == typeof(PackageRedemption) && account.Role == RoleKind.Cashier) return rows.Cast<PackageRedemption>().Select(a=>{a.CommissionCost=0;a.MaterialCost=0;return a;}).Cast<T>().ToList();
            if (account.Role == RoleKind.SalonStaff)
            {
                var customerIds = db.Set<Appointment>().Where(a => a.EmployeeId == account.EmployeeId && account.EmployeeId != null).Select(a=>a.CustomerId).ToHashSet();
                if (type == typeof(Customer)) return rows.Cast<Customer>().Where(c=>customerIds.Contains(c.Id)).Cast<T>().ToList();
                if (type == typeof(CustomerPackage)) return rows.Cast<CustomerPackage>().Where(c=>customerIds.Contains(c.CustomerId)).Cast<T>().ToList();
                if (type == typeof(Appointment)) return rows.Cast<Appointment>().Where(a=>account.EmployeeId != null && a.EmployeeId == account.EmployeeId).Cast<T>().ToList();
                if (type == typeof(PackageRedemption)) return rows.Cast<PackageRedemption>().Where(a=>account.EmployeeId != null && a.EmployeeId == account.EmployeeId).Select(a=>{a.CommissionCost=0;a.MaterialCost=0;return a;}).Cast<T>().ToList();
            }
            return rows;
        }
    }
    public T Save<T>(T entity, UserAccount user) where T : Entity
    {
        var account = Require(user);
        var type = typeof(T);
        var allowed = new[] {typeof(Customer),typeof(SalonService),typeof(ServiceCategory),typeof(Product),typeof(ProductCategory),typeof(Supplier),typeof(Employee),typeof(Appointment),typeof(Expense),typeof(PackageOffer),typeof(ServiceChargePreset),typeof(ServicePriceRule),typeof(ServiceMaterialRequirement),typeof(EmployeeServiceCommission),typeof(UserAccount)};
        if (!allowed.Contains(type)) throw new InvalidOperationException("Use the dedicated transaction workflow to modify this record.");
        if (account.Role != RoleKind.Administrator && type != typeof(Customer) && type != typeof(Appointment)) throw new UnauthorizedAccessException("Administrator access is required.");
        lock (_database.Gate)
        {
            using var db = _database.OpenContext(); using var transaction = db.Database.BeginTransaction();
            var existing = db.Set<T>().Find(entity.Id);
            ValidateEntity(db,entity,existing,account);
            if (existing is null) db.Add(entity); else { entity.CreatedAtUtc = existing.CreatedAtUtc; db.Entry(existing).CurrentValues.SetValues(entity); }
            if(entity is SalonService savedService) SynchronizeStandardPackage(db,savedService);
            if(entity is PackageOffer savedOffer)
            {
                var offerService=db.Set<SalonService>().Find(savedOffer.ServiceId)!;
                if(savedOffer.Id==StandardPackageId(offerService)) {offerService.PackageAvailable=savedOffer.IsActive;offerService.PackagePrice=savedOffer.Price;}
            }
            AuthService.Audit(db,account.Id,existing is null ? "Record created" : "Record updated",entity.Id,type.Name,type.Name);
            db.SaveChanges(); transaction.Commit();
            if(entity is UserAccount savedAccount) {savedAccount.PasswordHash="";savedAccount.RecoveryCodeHash=null;savedAccount.PinHash=null;savedAccount.PinLookupHash=null;savedAccount.SuperPinHash=null;}
            return entity;
        }
    }
    public void Remove<T>(Guid id,UserAccount user) where T:Entity
    {
        var account = Require(user,true);
        var allowed = new[] { typeof(ServiceMaterialRequirement),typeof(EmployeeServiceCommission),typeof(ServiceChargePreset),typeof(ServicePriceRule) };
        if (!allowed.Contains(typeof(T))) throw new InvalidOperationException("Historical records cannot be deleted. Disable the record instead.");
        lock (_database.Gate) { using var db=_database.OpenContext(); var entity=db.Set<T>().Find(id)??throw new ArgumentException("Record not found."); db.Remove(entity); AuthService.Audit(db,account.Id,"Configuration removed",id,typeof(T).Name,typeof(T).Name); db.SaveChanges(); }
    }
    private static void ValidateEntity<T>(SalonDbContext db,T entity,T? existing,UserAccount account) where T:Entity
    {
        foreach (var property in entity.GetType().GetProperties().Where(p => p.PropertyType.IsEnum)) if (!Enum.IsDefined(property.PropertyType, property.GetValue(entity)!)) throw new ArgumentException($"Invalid {property.Name} value.");
        static void Name(string name) { if (string.IsNullOrWhiteSpace(name) || name.Length>200) throw new ArgumentException("A name containing 1 to 200 characters is required."); }
        switch(entity)
        {
            case Customer c: Name(c.FullName); if(account.Role==RoleKind.SalonStaff) throw new UnauthorizedAccessException("Cashier or administrator access is required to edit customers."); break;
            case ServiceCategory c: Name(c.Name);break;
            case ProductCategory c: Name(c.Name);break;
            case Supplier s: Name(s.Name);break;
            case SalonService s:
                if(s.IsDeleted || (existing as SalonService)?.IsDeleted == true)throw new ArgumentException("Use the Super PIN deletion workflow. Deleted services cannot be edited.");
                if(s.PackageAvailable && db.Set<PackageOffer>().Any(p => p.Id == StandardPackageId(s) && p.IsDeleted))throw new ArgumentException("The standard package was deleted. Create a separate package offer instead of re-enabling it.");
                Name(s.Name); FinancialEngine.ValidateMoney(s.BasePrice); if(s.DurationMinutes<0 || s.DurationMinutes>1440) throw new ArgumentException("Service duration must be from 0 to 1440 minutes.");
                if(s.MinimumPrice.HasValue) FinancialEngine.ValidateMoney(s.MinimumPrice.Value); if(s.MaximumPrice.HasValue) FinancialEngine.ValidateMoney(s.MaximumPrice.Value);
                if(s.PricingType==PricingType.Range && (!s.MinimumPrice.HasValue || !s.MaximumPrice.HasValue || s.MinimumPrice>s.MaximumPrice)) throw new ArgumentException("A ranged service needs valid minimum and maximum prices.");
                if(s.PackagePrice.HasValue) FinancialEngine.ValidateMoney(s.PackagePrice.Value);if(s.PackageAvailable && !s.PackagePrice.HasValue) throw new ArgumentException("Enter a price for the standard 5+1 package.");break;
            case Product p:
                Name(p.Name); FinancialEngine.ValidateMoney(p.SellingPrice); if(p.PurchaseCost<0 || p.PurchaseCost!=decimal.Round(p.PurchaseCost,6) || p.ReorderLevel<0) throw new ArgumentException("Product cost and reorder level must be valid nonnegative values.");
                var old=existing as Product; if((old is null && (p.StockQuantity!=0 || p.AverageCost!=0 || p.InventoryValue!=0)) || (old!=null && (p.StockQuantity!=old.StockQuantity || p.AverageCost!=old.AverageCost || p.InventoryValue!=old.InventoryValue))) throw new ArgumentException("Change stock and inventory costs through Stock In / Adjust Stock.");
                p.Sku=p.Sku.Trim();p.Barcode=p.Barcode.Trim();break;
            case Employee e: Name(e.FullName); if(!Enum.IsDefined(e.CommissionType) || e.CommissionRate<0 || (e.CommissionType==CommissionType.Percentage && e.CommissionRate>100)) throw new ArgumentException("Commission settings are invalid.");break;
            case PackageOffer p:
                if(p.IsDeleted || (existing as PackageOffer)?.IsDeleted == true)throw new ArgumentException("Use the Super PIN deletion workflow. Deleted package offers cannot be edited.");
                Name(p.Name);FinancialEngine.ValidateMoney(p.Price);if(p.TotalSessions<=0 || p.TotalSessions>10000 || p.ValidityDays is <=0) throw new ArgumentException("Package session count and validity must be positive.");
                var oldOffer=existing as PackageOffer;var originalService=oldOffer==null?null:db.Set<SalonService>().Find(oldOffer.ServiceId);
                if(originalService!=null && oldOffer!.Id==StandardPackageId(originalService) && (p.TotalSessions!=6 || p.ServiceId!=oldOffer.ServiceId)) throw new ArgumentException("A standard 5+1 offer keeps its six sessions and service. Create a separate offer for different terms.");break;
            case ServiceChargePreset p:Name(p.Name);FinancialEngine.ValidateMoney(p.Amount);break;
            case ServicePriceRule p:Name(p.Name);if(p.MinimumPrice.HasValue) FinancialEngine.ValidateMoney(p.MinimumPrice.Value);if(p.MaximumPrice.HasValue) FinancialEngine.ValidateMoney(p.MaximumPrice.Value);if(p.MinimumPrice>p.MaximumPrice) throw new ArgumentException("Minimum price exceeds maximum price.");break;
            case ServiceMaterialRequirement r:FinancialEngine.ValidateQuantity(r.Quantity); if(!db.Set<Product>().Any(p=>p.Id==r.ProductId && p.IsActive)) throw new ArgumentException("Select an active material product.");break;
            case EmployeeServiceCommission r:if(r.Rate<0 || (r.Type==CommissionType.Percentage && r.Rate>100)) throw new ArgumentException("Commission rate is invalid.");break;
            case UserAccount u:
                var oldUser=existing as UserAccount??throw new ArgumentException("Create accounts through the account creation workflow.");
                Name(u.Username); if(u.Username.Trim().Length<3) throw new ArgumentException("Username must contain at least 3 characters.");
                FinancialEngine.ValidatePercent(u.MaxDiscountPercent);u.NormalizedUsername=AuthService.Normalize(u.Username); u.PasswordHash=oldUser.PasswordHash;u.RecoveryCodeHash=oldUser.RecoveryCodeHash;u.PinHash=oldUser.PinHash;u.PinLookupHash=oldUser.PinLookupHash;u.SuperPinHash=oldUser.SuperPinHash;u.SuperPinFailedAttempts=oldUser.SuperPinFailedAttempts;u.SuperPinLockedUntilUtc=oldUser.SuperPinLockedUntilUtc;u.FailedLoginAttempts=oldUser.FailedLoginAttempts;u.LockedUntilUtc=oldUser.LockedUntilUtc;u.LastLoginAtUtc=oldUser.LastLoginAtUtc;
                if(oldUser.Role==RoleKind.Administrator && oldUser.IsActive && (!u.IsActive || u.Role!=RoleKind.Administrator) && !db.Set<UserAccount>().Any(x=>x.Id!=u.Id && x.Role==RoleKind.Administrator && x.IsActive)) throw new ArgumentException("The last active administrator cannot be disabled or demoted.");
                u.RoleId=db.Set<Role>().Single(r=>r.Kind==u.Role).Id;break;
            case Expense e:
                if(e.IsVoided || (existing as Expense)?.IsVoided == true || e.VoidedAtUtc.HasValue || e.VoidedByUserId.HasValue || e.VoidReason.Length > 0)throw new ArgumentException("Use the Super PIN expense void workflow. Voided expenses cannot be edited.");
                FinancialEngine.ValidateMoney(e.Amount);if(e.Amount<=0 || string.IsNullOrWhiteSpace(e.Description)) throw new ArgumentException("Enter an expense description and positive amount.");e.RecordedByUserId=account.Id;
                if(e.Category.Equals("Inventory",StringComparison.OrdinalIgnoreCase) || e.Category.Equals("Inventory Purchases",StringComparison.OrdinalIgnoreCase) || e.Category.Equals("Inventory writeoffs",StringComparison.OrdinalIgnoreCase)) throw new ArgumentException("Record inventory purchases and losses through the Inventory page. Their costs enter profit when sold, consumed, or written off.");
                if(e.Category.Equals("Commissions",StringComparison.OrdinalIgnoreCase) && !e.CommissionRecordId.HasValue) throw new ArgumentException("Record commission payouts against an earned commission to avoid counting commission costs twice.");
                if(e.CommissionRecordId.HasValue) { var commission=db.Set<CommissionRecord>().Find(e.CommissionRecordId.Value)??throw new ArgumentException("Commission not found."); if(commission.IsReversed || e.Amount!=commission.Amount || db.Set<Expense>().Any(x=>x.Id!=e.Id && x.CommissionRecordId==e.CommissionRecordId && !x.IsVoided)) throw new ArgumentException("This commission payout is invalid or already recorded.");e.Category="Commissions";commission.PaidAtUtc=e.OccurredAtUtc; }
                break;
            case Appointment a:
                if(a.StartsAtUtc>=a.EndsAtUtc) throw new ArgumentException("Appointment end must be after its start."); if(a.EndsAtUtc-a.StartsAtUtc>TimeSpan.FromDays(1)) throw new ArgumentException("Appointments cannot exceed 24 hours.");
                a.RecordedByUserId=account.Id;
                var priorAppointment=existing as Appointment;
                if((priorAppointment==null || a.CustomerId!=priorAppointment.CustomerId) && !db.Set<Customer>().Any(c=>c.Id==a.CustomerId && c.IsActive)) throw new ArgumentException("Select an active customer.");
                if((priorAppointment==null || a.ServiceId!=priorAppointment.ServiceId) && !db.Set<SalonService>().Any(s=>s.Id==a.ServiceId && s.IsActive)) throw new ArgumentException("Select an active service.");
                if(a.EmployeeId.HasValue && (priorAppointment==null || a.EmployeeId!=priorAppointment.EmployeeId) && !db.Set<Employee>().Any(e=>e.Id==a.EmployeeId && e.IsActive)) throw new ArgumentException("Select an active employee.");
                if(account.Role==RoleKind.SalonStaff) { var oldAppointment=existing as Appointment; if(account.EmployeeId is null || oldAppointment is null || oldAppointment.EmployeeId!=account.EmployeeId || a.EmployeeId!=account.EmployeeId || a.CustomerId!=oldAppointment.CustomerId || a.ServiceId!=oldAppointment.ServiceId || a.StartsAtUtc!=oldAppointment.StartsAtUtc || a.EndsAtUtc!=oldAppointment.EndsAtUtc) throw new UnauthorizedAccessException("Staff may update the status of their assigned appointments only."); }
                if(a.OverlapOverride && account.Role!=RoleKind.Administrator && !(priorAppointment?.OverlapOverride==true && a.StartsAtUtc==priorAppointment.StartsAtUtc && a.EndsAtUtc==priorAppointment.EndsAtUtc && a.EmployeeId==priorAppointment.EmployeeId)) throw new UnauthorizedAccessException("Administrator access is required to override a schedule conflict.");
                if(a.Status!=AppointmentStatus.Cancelled && a.EmployeeId.HasValue && !a.OverlapOverride && db.Set<Appointment>().Any(x=>x.Id!=a.Id && x.EmployeeId==a.EmployeeId && x.Status!=AppointmentStatus.Cancelled && x.StartsAtUtc<a.EndsAtUtc && x.EndsAtUtc>a.StartsAtUtc)) throw new ArgumentException("This employee already has an appointment during the selected time.");
                break;
        }
    }
    private static Guid StandardPackageId(SalonService service)
    {
        var seedService=InitialCatalog.Create().Services.FirstOrDefault(s=>s.Id==service.Id);
        return InitialCatalog.StableId(seedService==null?"standard-package:"+service.Id:"package:"+seedService.Name);
    }
    private static void SynchronizeStandardPackage(SalonDbContext db,SalonService service)
    {
        var id=StandardPackageId(service);var offer=db.Set<PackageOffer>().Find(id);
        if(offer==null && service.PackageAvailable) {offer=new PackageOffer {Id=id,ServiceId=service.Id,TotalSessions=6};db.Add(offer);}
        if(offer!=null) {offer.Name=service.Name+" — 5+1 sessions";offer.Price=service.PackagePrice??offer.Price;offer.IsActive=service.PackageAvailable && service.IsActive && !offer.IsDeleted && !service.IsDeleted;}
    }
    public string GetSetting(string key,string fallback="")
    {
        var account=Require();if(account.Role!=RoleKind.Administrator && (key.StartsWith("Backup",StringComparison.OrdinalIgnoreCase) || key.StartsWith("Security",StringComparison.OrdinalIgnoreCase))) throw new UnauthorizedAccessException("Administrator access is required for this setting.");lock(_database.Gate) {using var db=_database.OpenContext();return db.Set<ApplicationSetting>().AsNoTracking().SingleOrDefault(s=>s.Key==key)?.Value??fallback;}
    }
    public void SetSetting(string key,string value,UserAccount user)
    {
        var account=Require(user,true);if(string.IsNullOrWhiteSpace(key) || key.Length>100) throw new ArgumentException("Invalid setting key.");
        if(key=="Security.SessionTimeoutMinutes" && (!int.TryParse(value,out var timeout) || timeout is <1 or >120)) throw new ArgumentException("Session timeout must be 1 to 120 minutes.");
        if(key=="Security.LockoutAttempts" && (!int.TryParse(value,out var attempts) || attempts is <3 or >10)) throw new ArgumentException("Lockout must follow 3 to 10 unsuccessful attempts.");
        if(key=="Security.LockoutMinutes" && (!int.TryParse(value,out var duration) || duration is <5 or >1440)) throw new ArgumentException("Lockout duration must be 5 to 1440 minutes.");
        if(key=="BackupScheduleHours" && (!int.TryParse(value,out var hours) || hours is <0 or >8760)) throw new ArgumentException("Backup interval must be 0 (disabled) to 8760 hours.");
        lock(_database.Gate) {using var db=_database.OpenContext();var setting=db.Set<ApplicationSetting>().SingleOrDefault(s=>s.Key==key);if(setting is null) {setting=new ApplicationSetting{Key=key};db.Add(setting);}setting.Value=value;AuthService.Audit(db,account.Id,"Setting updated",setting.Id,key,"ApplicationSetting");db.SaveChanges();}
        if(key.StartsWith("Security.",StringComparison.Ordinal)) _auth.ReloadSecuritySettings();
    }
}


