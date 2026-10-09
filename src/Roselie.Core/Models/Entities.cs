namespace Roselie.Core.Models;

public abstract class Entity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public enum RoleKind { Administrator, Cashier, SalonStaff }
public enum PricingType { Fixed, Range, StartsAt, Custom }
public enum SaleItemKind { Service, Product, Package, PackageRedemption }
public enum SaleStatus { Completed, PartiallyRefunded, Refunded, Voided }
public enum PaymentMethod { Cash, GCash, BankTransfer, Card }
public enum InventoryMovementType { StockIn, StockOut, Adjustment, RetailSale, ServiceConsumption, Return }
public enum AppointmentStatus { Pending, Confirmed, InProgress, Completed, Cancelled }
public enum CommissionType { None, Fixed, Percentage }
public enum PackageRevenuePolicy { RecognizeOnPurchase }

public sealed class UserAccount : Entity
{
    public string Username { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string NormalizedUsername { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public Guid RoleId { get; set; }
    public RoleKind Role { get; set; }
    public Guid? EmployeeId { get; set; }
    public bool IsActive { get; set; } = true;
    public int FailedLoginAttempts { get; set; }
    public DateTime? LockedUntilUtc { get; set; }
    public DateTime? LastLoginAtUtc { get; set; }
    public decimal MaxDiscountPercent { get; set; } = 20;
    public bool AllowCustomPrices { get; set; }
    public string? RecoveryCodeHash { get; set; }
    public string? PinHash { get; set; }
    public string? PinLookupHash { get; set; }
    public string? SuperPinHash { get; set; }
    public int SuperPinFailedAttempts { get; set; }
    public DateTime? SuperPinLockedUntilUtc { get; set; }
}

public sealed class Role : Entity
{
    public string Name { get; set; } = "";
    public RoleKind Kind { get; set; }
}

public sealed class Permission : Entity
{
    public string Key { get; set; } = "";
    public string Description { get; set; } = "";
}

public sealed class RolePermission : Entity
{
    public Guid RoleId { get; set; }
    public Guid PermissionId { get; set; }
}

public sealed class Employee : Entity
{
    public string FullName { get; set; } = "";
    public string Contact { get; set; } = "";
    public string Position { get; set; } = "Salon Staff";
    public bool IsActive { get; set; } = true;
    public CommissionType CommissionType { get; set; }
    public decimal CommissionRate { get; set; }
    public Guid? UserAccountId { get; set; }
}

public sealed class Customer : Entity
{
    public string FullName { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Email { get; set; } = "";
    public string Notes { get; set; } = "";
    public bool IsActive { get; set; } = true;
}

public sealed class ServiceCategory : Entity
{
    public string Name { get; set; } = "";
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class SalonService : Entity
{
    public string Name { get; set; } = "";
    public Guid CategoryId { get; set; }
    public string Description { get; set; } = "";
    public decimal BasePrice { get; set; }
    public PricingType PricingType { get; set; }
    public decimal? MinimumPrice { get; set; }
    public decimal? MaximumPrice { get; set; }
    public bool PackageAvailable { get; set; }
    public decimal? PackagePrice { get; set; }
    public int DurationMinutes { get; set; } = 30;
    public bool IsActive { get; set; } = true;
    public bool CostInformationComplete { get; set; }
    public bool OwnerConfirmed { get; set; }
    public bool IsDeleted { get; set; }
}

public sealed class ServicePriceRule : Entity
{
    public Guid ServiceId { get; set; }
    public string Name { get; set; } = "";
    public decimal? MinimumPrice { get; set; }
    public decimal? MaximumPrice { get; set; }
    public bool RequiresAuthorization { get; set; }
}

public sealed class ServiceChargePreset : Entity
{
    public Guid? ServiceId { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public decimal Amount { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class ProductCategory : Entity
{
    public string Name { get; set; } = "";
    public bool IsActive { get; set; } = true;
}

public sealed class Product : Entity
{
    public string Name { get; set; } = "";
    public string Sku { get; set; } = "";
    public string Barcode { get; set; } = "";
    public Guid? CategoryId { get; set; }
    public string Brand { get; set; } = "";
    public Guid? SupplierId { get; set; }
    public decimal PurchaseCost { get; set; }
    public decimal SellingPrice { get; set; }
    public string StockUnit { get; set; } = "unit";
    public decimal StockQuantity { get; set; }
    public decimal ReorderLevel { get; set; }
    public decimal AverageCost { get; set; }
    public decimal InventoryValue { get; set; }
    public string ImagePath { get; set; } = "";
    public DateTime? ExpirationDateUtc { get; set; }
    public bool IsRetail { get; set; } = true;
    public bool IsActive { get; set; } = true;
    public bool CostInformationComplete { get; set; }
}

public sealed class Supplier : Entity
{
    public string Name { get; set; } = "";
    public string Contact { get; set; } = "";
    public string Address { get; set; } = "";
    public string Notes { get; set; } = "";
}

public sealed class InventoryMovement : Entity
{
    public Guid ProductId { get; set; }
    public InventoryMovementType Type { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitCost { get; set; }
    public decimal ValueChange { get; set; }
    public decimal BalanceAfter { get; set; }
    public Guid? SaleItemId { get; set; }
    public Guid? PackageRedemptionId { get; set; }
    public Guid? RefundId { get; set; }
    public Guid RecordedByUserId { get; set; }
    public string Reason { get; set; } = "";
    public string Reference { get; set; } = "";
    public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;
}

public sealed class InventoryAverageCost : Entity
{
    public Guid ProductId { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitCost { get; set; }
    public decimal InventoryValue { get; set; }
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

public sealed class ServiceMaterialRequirement : Entity
{
    public Guid ServiceId { get; set; }
    public Guid ProductId { get; set; }
    public decimal Quantity { get; set; }
}

public sealed class ServiceMaterialConsumption : Entity
{
    public Guid ServiceId { get; set; }
    public Guid ProductId { get; set; }
    public Guid? SaleItemId { get; set; }
    public Guid? PackageRedemptionId { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitCost { get; set; }
    public decimal TotalCost { get; set; }
    public Guid RecordedByUserId { get; set; }
}

public sealed class Sale : Entity
{
    public string TransactionNumber { get; set; } = "";
    public Guid CheckoutToken { get; set; }
    public Guid? CustomerId { get; set; }
    public Guid CashierUserId { get; set; }
    public DateTime CompletedAtUtc { get; set; } = DateTime.UtcNow;
    public decimal Subtotal { get; set; }
    public decimal DiscountPercent { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal Total { get; set; }
    public decimal AmountReceived { get; set; }
    public decimal Change { get; set; }
    public decimal CostOfGoods { get; set; }
    public decimal MaterialCost { get; set; }
    public decimal CommissionCost { get; set; }
    public bool CostInformationComplete { get; set; } = true;
    public SaleStatus Status { get; set; } = SaleStatus.Completed;
    public string DiscountReason { get; set; } = "";
    public string Notes { get; set; } = "";
}

public sealed class SaleItem : Entity
{
    public Guid SaleId { get; set; }
    public Guid? CatalogItemId { get; set; }
    public Guid? PriceRuleId { get; set; }
    public SaleItemKind Kind { get; set; }
    public string Name { get; set; } = "";
    public decimal Quantity { get; set; } = 1;
    public decimal UnitPrice { get; set; }
    public decimal BaseAmount { get; set; }
    public decimal ChargeAmount { get; set; }
    public decimal Subtotal { get; set; }
    public decimal DiscountPercent { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal OrderDiscountAmount { get; set; }
    public decimal Total { get; set; }
    public decimal UnitCost { get; set; }
    public decimal CostOfGoods { get; set; }
    public decimal MaterialCost { get; set; }
    public decimal CommissionCost { get; set; }
    public Guid? EmployeeId { get; set; }
    public Guid? CustomerPackageId { get; set; }
    public bool CostInformationComplete { get; set; } = true;
}

public sealed class SaleItemCharge : Entity
{
    public Guid SaleItemId { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public decimal Amount { get; set; }
    public decimal Total { get; set; }
}

public sealed class SaleItemDiscount : Entity
{
    public Guid SaleItemId { get; set; }
    public decimal Percent { get; set; }
    public decimal Amount { get; set; }
    public bool IsOrderAllocation { get; set; }
    public string Reason { get; set; } = "";
    public Guid AuthorizedByUserId { get; set; }
}

public sealed class Payment : Entity
{
    public Guid SaleId { get; set; }
    public PaymentMethod Method { get; set; }
    public decimal Amount { get; set; }
    public decimal AppliedAmount { get; set; }
    public string Reference { get; set; } = "";
}

public sealed class Refund : Entity
{
    public Guid SaleId { get; set; }
    public string RefundNumber { get; set; } = "";
    public decimal Amount { get; set; }
    public decimal CostOfGoodsReversed { get; set; }
    public decimal MaterialCostReversed { get; set; }
    public decimal CommissionCostReversed { get; set; }
    public string Reason { get; set; } = "";
    public PaymentMethod Method { get; set; }
    public Guid AuthorizedByUserId { get; set; }
    public DateTime RefundedAtUtc { get; set; } = DateTime.UtcNow;
    public bool IsVoid { get; set; }
}

public sealed class RefundItem : Entity
{
    public Guid RefundId { get; set; }
    public Guid SaleItemId { get; set; }
    public decimal Quantity { get; set; }
    public decimal Amount { get; set; }
    public bool ReturnToStock { get; set; }
    public decimal CostOfGoodsReversed { get; set; }
    public decimal CommissionCostReversed { get; set; }
}

public sealed class Expense : Entity
{
    public string Category { get; set; } = "Miscellaneous";
    public string Description { get; set; } = "";
    public decimal Amount { get; set; }
    public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;
    public PaymentMethod PaymentMethod { get; set; }
    public string ReferenceNumber { get; set; } = "";
    public Guid RecordedByUserId { get; set; }
    public Guid? CommissionRecordId { get; set; }
    public bool IsVoided { get; set; }
    public DateTime? VoidedAtUtc { get; set; }
    public Guid? VoidedByUserId { get; set; }
    public string VoidReason { get; set; } = "";
}

public sealed class Appointment : Entity
{
    public Guid CustomerId { get; set; }
    public Guid ServiceId { get; set; }
    public Guid? EmployeeId { get; set; }
    public DateTime StartsAtUtc { get; set; }
    public DateTime EndsAtUtc { get; set; }
    public AppointmentStatus Status { get; set; }
    public string Notes { get; set; } = "";
    public bool OverlapOverride { get; set; }
    public Guid RecordedByUserId { get; set; }
    public Guid? SaleItemId { get; set; }
}

public sealed class PackageOffer : Entity
{
    public string Name { get; set; } = "";
    public Guid ServiceId { get; set; }
    public decimal Price { get; set; }
    public int TotalSessions { get; set; } = 6;
    public int? ValidityDays { get; set; }
    public bool IsActive { get; set; } = true;
    public PackageRevenuePolicy RevenuePolicy { get; set; } = PackageRevenuePolicy.RecognizeOnPurchase;
    public bool IsDeleted { get; set; }
}

public sealed class CustomerPackage : Entity
{
    public Guid CustomerId { get; set; }
    public Guid PackageOfferId { get; set; }
    public Guid ServiceId { get; set; }
    public Guid SaleItemId { get; set; }
    public string Name { get; set; } = "";
    public decimal PurchasePrice { get; set; }
    public int TotalSessions { get; set; } = 6;
    public int UsedSessions { get; set; }
    public DateTime PurchasedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ExpiresAtUtc { get; set; }
    public bool IsCancelled { get; set; }
    public int RemainingSessions => Math.Max(0, TotalSessions - UsedSessions);
}

public sealed class PackageRedemption : Entity
{
    public Guid CustomerPackageId { get; set; }
    public Guid? EmployeeId { get; set; }
    public Guid? SaleItemId { get; set; }
    public DateTime RedeemedAtUtc { get; set; } = DateTime.UtcNow;
    public int SessionsUsed { get; set; } = 1;
    public decimal MaterialCost { get; set; }
    public decimal CommissionCost { get; set; }
    public Guid RecordedByUserId { get; set; }
    public string Notes { get; set; } = "";
    public bool CostInformationComplete { get; set; }
}

public sealed class CommissionRecord : Entity
{
    public Guid EmployeeId { get; set; }
    public Guid? SaleItemId { get; set; }
    public Guid? PackageRedemptionId { get; set; }
    public CommissionType Type { get; set; }
    public decimal Rate { get; set; }
    public decimal BasisAmount { get; set; }
    public decimal Amount { get; set; }
    public DateTime EarnedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? PaidAtUtc { get; set; }
    public bool IsReversed { get; set; }
}

public sealed class EmployeeServiceCommission : Entity
{
    public Guid EmployeeId { get; set; }
    public Guid ServiceId { get; set; }
    public CommissionType Type { get; set; }
    public decimal Rate { get; set; }
}

public sealed class ReceiptRecord : Entity
{
    public Guid SaleId { get; set; }
    public string ReceiptNumber { get; set; } = "";
    public string Content { get; set; } = "";
    public string PrinterName { get; set; } = "";
    public double WidthMillimeters { get; set; } = 80;
    public int PrintCount { get; set; }
    public DateTime? LastPrintedAtUtc { get; set; }
    public Guid? LastPrintedByUserId { get; set; }
}

public sealed class AuditLog : Entity
{
    public Guid? UserId { get; set; }
    public string Action { get; set; } = "";
    public string EntityType { get; set; } = "";
    public Guid? EntityId { get; set; }
    public string Details { get; set; } = "";
    public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;
}

public sealed class ApplicationSetting : Entity
{
    public string Key { get; set; } = "";
    public string Value { get; set; } = "";
}

public sealed class BackupRecord : Entity
{
    public string FilePath { get; set; } = "";
    public string Sha256 { get; set; } = "";
    public long SizeBytes { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public DateTime VerifiedAtUtc { get; set; }
    public bool IsScheduled { get; set; }
}
