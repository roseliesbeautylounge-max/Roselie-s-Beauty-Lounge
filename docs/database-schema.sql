CREATE TABLE "ApplicationSetting" (
    "Id" TEXT NOT NULL CONSTRAINT "PK_ApplicationSetting" PRIMARY KEY,
    "Key" TEXT NOT NULL,
    "Value" TEXT NOT NULL,
    "CreatedAtUtc" TEXT NOT NULL
);


CREATE TABLE "Customer" (
    "Id" TEXT NOT NULL CONSTRAINT "PK_Customer" PRIMARY KEY,
    "FullName" TEXT NOT NULL,
    "Phone" TEXT NOT NULL,
    "Email" TEXT NOT NULL,
    "Notes" TEXT NOT NULL,
    "IsActive" INTEGER NOT NULL,
    "CreatedAtUtc" TEXT NOT NULL
);


CREATE TABLE "Permission" (
    "Id" TEXT NOT NULL CONSTRAINT "PK_Permission" PRIMARY KEY,
    "Key" TEXT NOT NULL,
    "Description" TEXT NOT NULL,
    "CreatedAtUtc" TEXT NOT NULL
);


CREATE TABLE "ProductCategory" (
    "Id" TEXT NOT NULL CONSTRAINT "PK_ProductCategory" PRIMARY KEY,
    "Name" TEXT NOT NULL,
    "IsActive" INTEGER NOT NULL,
    "CreatedAtUtc" TEXT NOT NULL
);


CREATE TABLE "Role" (
    "Id" TEXT NOT NULL CONSTRAINT "PK_Role" PRIMARY KEY,
    "Name" TEXT NOT NULL,
    "Kind" INTEGER NOT NULL,
    "CreatedAtUtc" TEXT NOT NULL
);


CREATE TABLE "ServiceCategory" (
    "Id" TEXT NOT NULL CONSTRAINT "PK_ServiceCategory" PRIMARY KEY,
    "Name" TEXT NOT NULL,
    "DisplayOrder" INTEGER NOT NULL,
    "IsActive" INTEGER NOT NULL,
    "CreatedAtUtc" TEXT NOT NULL
);


CREATE TABLE "Supplier" (
    "Id" TEXT NOT NULL CONSTRAINT "PK_Supplier" PRIMARY KEY,
    "Name" TEXT NOT NULL,
    "Contact" TEXT NOT NULL,
    "Address" TEXT NOT NULL,
    "Notes" TEXT NOT NULL,
    "CreatedAtUtc" TEXT NOT NULL
);


CREATE TABLE "RolePermission" (
    "Id" TEXT NOT NULL CONSTRAINT "PK_RolePermission" PRIMARY KEY,
    "RoleId" TEXT NOT NULL,
    "PermissionId" TEXT NOT NULL,
    "CreatedAtUtc" TEXT NOT NULL,
    CONSTRAINT "FK_RolePermission_Permission_PermissionId" FOREIGN KEY ("PermissionId") REFERENCES "Permission" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_RolePermission_Role_RoleId" FOREIGN KEY ("RoleId") REFERENCES "Role" ("Id") ON DELETE RESTRICT
);


CREATE TABLE "SalonService" (
    "Id" TEXT NOT NULL CONSTRAINT "PK_SalonService" PRIMARY KEY,
    "Name" TEXT NOT NULL,
    "CategoryId" TEXT NOT NULL,
    "Description" TEXT NOT NULL,
    "BasePrice" INTEGER NOT NULL,
    "PricingType" INTEGER NOT NULL,
    "MinimumPrice" INTEGER NULL,
    "MaximumPrice" INTEGER NULL,
    "PackageAvailable" INTEGER NOT NULL,
    "PackagePrice" INTEGER NULL,
    "DurationMinutes" INTEGER NOT NULL,
    "IsActive" INTEGER NOT NULL,
    "CostInformationComplete" INTEGER NOT NULL,
    "OwnerConfirmed" INTEGER NOT NULL,
    "CreatedAtUtc" TEXT NOT NULL,
    CONSTRAINT "FK_SalonService_ServiceCategory_CategoryId" FOREIGN KEY ("CategoryId") REFERENCES "ServiceCategory" ("Id") ON DELETE RESTRICT
);


CREATE TABLE "Product" (
    "Id" TEXT NOT NULL CONSTRAINT "PK_Product" PRIMARY KEY,
    "Name" TEXT NOT NULL,
    "Sku" TEXT NOT NULL,
    "Barcode" TEXT NOT NULL,
    "CategoryId" TEXT NULL,
    "Brand" TEXT NOT NULL,
    "SupplierId" TEXT NULL,
    "PurchaseCost" INTEGER NOT NULL,
    "SellingPrice" INTEGER NOT NULL,
    "StockUnit" TEXT NOT NULL,
    "StockQuantity" INTEGER NOT NULL,
    "ReorderLevel" INTEGER NOT NULL,
    "AverageCost" INTEGER NOT NULL,
    "InventoryValue" INTEGER NOT NULL,
    "ImagePath" TEXT NOT NULL,
    "ExpirationDateUtc" TEXT NULL,
    "IsRetail" INTEGER NOT NULL,
    "IsActive" INTEGER NOT NULL,
    "CostInformationComplete" INTEGER NOT NULL,
    "CreatedAtUtc" TEXT NOT NULL,
    CONSTRAINT "CK_Product_Stock" CHECK ("StockQuantity" >= 0 AND "AverageCost" >= 0 AND "SellingPrice" >= 0 AND "PurchaseCost" >= 0 AND "ReorderLevel" >= 0),
    CONSTRAINT "FK_Product_ProductCategory_CategoryId" FOREIGN KEY ("CategoryId") REFERENCES "ProductCategory" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_Product_Supplier_SupplierId" FOREIGN KEY ("SupplierId") REFERENCES "Supplier" ("Id") ON DELETE RESTRICT
);


CREATE TABLE "PackageOffer" (
    "Id" TEXT NOT NULL CONSTRAINT "PK_PackageOffer" PRIMARY KEY,
    "Name" TEXT NOT NULL,
    "ServiceId" TEXT NOT NULL,
    "Price" INTEGER NOT NULL,
    "TotalSessions" INTEGER NOT NULL,
    "ValidityDays" INTEGER NULL,
    "IsActive" INTEGER NOT NULL,
    "RevenuePolicy" INTEGER NOT NULL,
    "CreatedAtUtc" TEXT NOT NULL,
    CONSTRAINT "FK_PackageOffer_SalonService_ServiceId" FOREIGN KEY ("ServiceId") REFERENCES "SalonService" ("Id") ON DELETE RESTRICT
);


CREATE TABLE "ServiceChargePreset" (
    "Id" TEXT NOT NULL CONSTRAINT "PK_ServiceChargePreset" PRIMARY KEY,
    "ServiceId" TEXT NULL,
    "Name" TEXT NOT NULL,
    "Description" TEXT NOT NULL,
    "Amount" INTEGER NOT NULL,
    "IsActive" INTEGER NOT NULL,
    "CreatedAtUtc" TEXT NOT NULL,
    CONSTRAINT "FK_ServiceChargePreset_SalonService_ServiceId" FOREIGN KEY ("ServiceId") REFERENCES "SalonService" ("Id") ON DELETE RESTRICT
);


CREATE TABLE "ServicePriceRule" (
    "Id" TEXT NOT NULL CONSTRAINT "PK_ServicePriceRule" PRIMARY KEY,
    "ServiceId" TEXT NOT NULL,
    "Name" TEXT NOT NULL,
    "MinimumPrice" INTEGER NULL,
    "MaximumPrice" INTEGER NULL,
    "RequiresAuthorization" INTEGER NOT NULL,
    "CreatedAtUtc" TEXT NOT NULL,
    CONSTRAINT "FK_ServicePriceRule_SalonService_ServiceId" FOREIGN KEY ("ServiceId") REFERENCES "SalonService" ("Id") ON DELETE RESTRICT
);


CREATE TABLE "InventoryAverageCost" (
    "Id" TEXT NOT NULL CONSTRAINT "PK_InventoryAverageCost" PRIMARY KEY,
    "ProductId" TEXT NOT NULL,
    "Quantity" INTEGER NOT NULL,
    "UnitCost" INTEGER NOT NULL,
    "InventoryValue" INTEGER NOT NULL,
    "UpdatedAtUtc" TEXT NOT NULL,
    "CreatedAtUtc" TEXT NOT NULL,
    CONSTRAINT "FK_InventoryAverageCost_Product_ProductId" FOREIGN KEY ("ProductId") REFERENCES "Product" ("Id") ON DELETE RESTRICT
);


CREATE TABLE "ServiceMaterialRequirement" (
    "Id" TEXT NOT NULL CONSTRAINT "PK_ServiceMaterialRequirement" PRIMARY KEY,
    "ServiceId" TEXT NOT NULL,
    "ProductId" TEXT NOT NULL,
    "Quantity" INTEGER NOT NULL,
    "CreatedAtUtc" TEXT NOT NULL,
    CONSTRAINT "FK_ServiceMaterialRequirement_Product_ProductId" FOREIGN KEY ("ProductId") REFERENCES "Product" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_ServiceMaterialRequirement_SalonService_ServiceId" FOREIGN KEY ("ServiceId") REFERENCES "SalonService" ("Id") ON DELETE RESTRICT
);


CREATE TABLE "Appointment" (
    "Id" TEXT NOT NULL CONSTRAINT "PK_Appointment" PRIMARY KEY,
    "CustomerId" TEXT NOT NULL,
    "ServiceId" TEXT NOT NULL,
    "EmployeeId" TEXT NULL,
    "StartsAtUtc" TEXT NOT NULL,
    "EndsAtUtc" TEXT NOT NULL,
    "Status" INTEGER NOT NULL,
    "Notes" TEXT NOT NULL,
    "OverlapOverride" INTEGER NOT NULL,
    "RecordedByUserId" TEXT NOT NULL,
    "SaleItemId" TEXT NULL,
    "CreatedAtUtc" TEXT NOT NULL,
    CONSTRAINT "FK_Appointment_Customer_CustomerId" FOREIGN KEY ("CustomerId") REFERENCES "Customer" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_Appointment_SalonService_ServiceId" FOREIGN KEY ("ServiceId") REFERENCES "SalonService" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_Appointment_Employee_EmployeeId" FOREIGN KEY ("EmployeeId") REFERENCES "Employee" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_Appointment_SaleItem_SaleItemId" FOREIGN KEY ("SaleItemId") REFERENCES "SaleItem" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_Appointment_UserAccount_RecordedByUserId" FOREIGN KEY ("RecordedByUserId") REFERENCES "UserAccount" ("Id") ON DELETE RESTRICT
);


CREATE TABLE "AuditLog" (
    "Id" TEXT NOT NULL CONSTRAINT "PK_AuditLog" PRIMARY KEY,
    "UserId" TEXT NULL,
    "Action" TEXT NOT NULL,
    "EntityType" TEXT NOT NULL,
    "EntityId" TEXT NULL,
    "Details" TEXT NOT NULL,
    "OccurredAtUtc" TEXT NOT NULL,
    "CreatedAtUtc" TEXT NOT NULL,
    CONSTRAINT "FK_AuditLog_UserAccount_UserId" FOREIGN KEY ("UserId") REFERENCES "UserAccount" ("Id") ON DELETE RESTRICT
);


CREATE TABLE "BackupRecord" (
    "Id" TEXT NOT NULL CONSTRAINT "PK_BackupRecord" PRIMARY KEY,
    "FilePath" TEXT NOT NULL,
    "Sha256" TEXT NOT NULL,
    "SizeBytes" INTEGER NOT NULL,
    "CreatedByUserId" TEXT NULL,
    "VerifiedAtUtc" TEXT NOT NULL,
    "IsScheduled" INTEGER NOT NULL,
    "CreatedAtUtc" TEXT NOT NULL,
    CONSTRAINT "FK_BackupRecord_UserAccount_CreatedByUserId" FOREIGN KEY ("CreatedByUserId") REFERENCES "UserAccount" ("Id") ON DELETE RESTRICT
);


CREATE TABLE "CommissionRecord" (
    "Id" TEXT NOT NULL CONSTRAINT "PK_CommissionRecord" PRIMARY KEY,
    "EmployeeId" TEXT NOT NULL,
    "SaleItemId" TEXT NULL,
    "PackageRedemptionId" TEXT NULL,
    "Type" INTEGER NOT NULL,
    "Rate" INTEGER NOT NULL,
    "BasisAmount" INTEGER NOT NULL,
    "Amount" INTEGER NOT NULL,
    "EarnedAtUtc" TEXT NOT NULL,
    "PaidAtUtc" TEXT NULL,
    "IsReversed" INTEGER NOT NULL,
    "CreatedAtUtc" TEXT NOT NULL,
    CONSTRAINT "FK_CommissionRecord_Employee_EmployeeId" FOREIGN KEY ("EmployeeId") REFERENCES "Employee" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_CommissionRecord_PackageRedemption_PackageRedemptionId" FOREIGN KEY ("PackageRedemptionId") REFERENCES "PackageRedemption" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_CommissionRecord_SaleItem_SaleItemId" FOREIGN KEY ("SaleItemId") REFERENCES "SaleItem" ("Id") ON DELETE RESTRICT
);


CREATE TABLE "CustomerPackage" (
    "Id" TEXT NOT NULL CONSTRAINT "PK_CustomerPackage" PRIMARY KEY,
    "CustomerId" TEXT NOT NULL,
    "PackageOfferId" TEXT NOT NULL,
    "ServiceId" TEXT NOT NULL,
    "SaleItemId" TEXT NOT NULL,
    "Name" TEXT NOT NULL,
    "PurchasePrice" INTEGER NOT NULL,
    "TotalSessions" INTEGER NOT NULL,
    "UsedSessions" INTEGER NOT NULL,
    "PurchasedAtUtc" TEXT NOT NULL,
    "ExpiresAtUtc" TEXT NULL,
    "IsCancelled" INTEGER NOT NULL,
    "CreatedAtUtc" TEXT NOT NULL,
    CONSTRAINT "CK_Package_Sessions" CHECK ("TotalSessions" > 0 AND "UsedSessions" >= 0 AND "UsedSessions" <= "TotalSessions"),
    CONSTRAINT "FK_CustomerPackage_Customer_CustomerId" FOREIGN KEY ("CustomerId") REFERENCES "Customer" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_CustomerPackage_PackageOffer_PackageOfferId" FOREIGN KEY ("PackageOfferId") REFERENCES "PackageOffer" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_CustomerPackage_SalonService_ServiceId" FOREIGN KEY ("ServiceId") REFERENCES "SalonService" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_CustomerPackage_SaleItem_SaleItemId" FOREIGN KEY ("SaleItemId") REFERENCES "SaleItem" ("Id") ON DELETE RESTRICT
);


CREATE TABLE "Employee" (
    "Id" TEXT NOT NULL CONSTRAINT "PK_Employee" PRIMARY KEY,
    "FullName" TEXT NOT NULL,
    "Contact" TEXT NOT NULL,
    "Position" TEXT NOT NULL,
    "IsActive" INTEGER NOT NULL,
    "CommissionType" INTEGER NOT NULL,
    "CommissionRate" INTEGER NOT NULL,
    "UserAccountId" TEXT NULL,
    "CreatedAtUtc" TEXT NOT NULL,
    CONSTRAINT "FK_Employee_UserAccount_UserAccountId" FOREIGN KEY ("UserAccountId") REFERENCES "UserAccount" ("Id") ON DELETE RESTRICT
);


CREATE TABLE "EmployeeServiceCommission" (
    "Id" TEXT NOT NULL CONSTRAINT "PK_EmployeeServiceCommission" PRIMARY KEY,
    "EmployeeId" TEXT NOT NULL,
    "ServiceId" TEXT NOT NULL,
    "Type" INTEGER NOT NULL,
    "Rate" INTEGER NOT NULL,
    "CreatedAtUtc" TEXT NOT NULL,
    CONSTRAINT "FK_EmployeeServiceCommission_Employee_EmployeeId" FOREIGN KEY ("EmployeeId") REFERENCES "Employee" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_EmployeeServiceCommission_SalonService_ServiceId" FOREIGN KEY ("ServiceId") REFERENCES "SalonService" ("Id") ON DELETE RESTRICT
);


CREATE TABLE "UserAccount" (
    "Id" TEXT NOT NULL CONSTRAINT "PK_UserAccount" PRIMARY KEY,
    "Username" TEXT NOT NULL,
    "NormalizedUsername" TEXT NOT NULL,
    "PasswordHash" TEXT NOT NULL,
    "RoleId" TEXT NOT NULL,
    "Role" INTEGER NOT NULL,
    "EmployeeId" TEXT NULL,
    "IsActive" INTEGER NOT NULL,
    "FailedLoginAttempts" INTEGER NOT NULL,
    "LockedUntilUtc" TEXT NULL,
    "LastLoginAtUtc" TEXT NULL,
    "MaxDiscountPercent" INTEGER NOT NULL,
    "AllowCustomPrices" INTEGER NOT NULL,
    "RecoveryCodeHash" TEXT NULL,
    "PinHash" TEXT NULL,
    "CreatedAtUtc" TEXT NOT NULL,
    CONSTRAINT "FK_UserAccount_Employee_EmployeeId" FOREIGN KEY ("EmployeeId") REFERENCES "Employee" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_UserAccount_Role_RoleId" FOREIGN KEY ("RoleId") REFERENCES "Role" ("Id") ON DELETE RESTRICT
);


CREATE TABLE "Expense" (
    "Id" TEXT NOT NULL CONSTRAINT "PK_Expense" PRIMARY KEY,
    "Category" TEXT NOT NULL,
    "Description" TEXT NOT NULL,
    "Amount" INTEGER NOT NULL,
    "OccurredAtUtc" TEXT NOT NULL,
    "PaymentMethod" INTEGER NOT NULL,
    "ReferenceNumber" TEXT NOT NULL,
    "RecordedByUserId" TEXT NOT NULL,
    "CommissionRecordId" TEXT NULL,
    "CreatedAtUtc" TEXT NOT NULL,
    CONSTRAINT "CK_Expense_Amount" CHECK ("Amount" >= 0),
    CONSTRAINT "FK_Expense_CommissionRecord_CommissionRecordId" FOREIGN KEY ("CommissionRecordId") REFERENCES "CommissionRecord" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_Expense_UserAccount_RecordedByUserId" FOREIGN KEY ("RecordedByUserId") REFERENCES "UserAccount" ("Id") ON DELETE RESTRICT
);


CREATE TABLE "Sale" (
    "Id" TEXT NOT NULL CONSTRAINT "PK_Sale" PRIMARY KEY,
    "TransactionNumber" TEXT NOT NULL,
    "CheckoutToken" TEXT NOT NULL,
    "CustomerId" TEXT NULL,
    "CashierUserId" TEXT NOT NULL,
    "CompletedAtUtc" TEXT NOT NULL,
    "Subtotal" INTEGER NOT NULL,
    "DiscountPercent" INTEGER NOT NULL,
    "DiscountAmount" INTEGER NOT NULL,
    "Total" INTEGER NOT NULL,
    "AmountReceived" INTEGER NOT NULL,
    "Change" INTEGER NOT NULL,
    "CostOfGoods" INTEGER NOT NULL,
    "MaterialCost" INTEGER NOT NULL,
    "CommissionCost" INTEGER NOT NULL,
    "CostInformationComplete" INTEGER NOT NULL,
    "Status" INTEGER NOT NULL,
    "DiscountReason" TEXT NOT NULL,
    "Notes" TEXT NOT NULL,
    "CreatedAtUtc" TEXT NOT NULL,
    CONSTRAINT "CK_Sale_Amounts" CHECK ("Total" >= 0 AND "DiscountAmount" >= 0 AND "Subtotal" - "DiscountAmount" = "Total"),
    CONSTRAINT "FK_Sale_Customer_CustomerId" FOREIGN KEY ("CustomerId") REFERENCES "Customer" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_Sale_UserAccount_CashierUserId" FOREIGN KEY ("CashierUserId") REFERENCES "UserAccount" ("Id") ON DELETE RESTRICT
);


CREATE TABLE "Payment" (
    "Id" TEXT NOT NULL CONSTRAINT "PK_Payment" PRIMARY KEY,
    "SaleId" TEXT NOT NULL,
    "Method" INTEGER NOT NULL,
    "Amount" INTEGER NOT NULL,
    "AppliedAmount" INTEGER NOT NULL,
    "Reference" TEXT NOT NULL,
    "CreatedAtUtc" TEXT NOT NULL,
    CONSTRAINT "CK_Payment_Amount" CHECK ("Amount" > 0 AND "AppliedAmount" >= 0 AND "AppliedAmount" <= "Amount"),
    CONSTRAINT "FK_Payment_Sale_SaleId" FOREIGN KEY ("SaleId") REFERENCES "Sale" ("Id") ON DELETE RESTRICT
);


CREATE TABLE "ReceiptRecord" (
    "Id" TEXT NOT NULL CONSTRAINT "PK_ReceiptRecord" PRIMARY KEY,
    "SaleId" TEXT NOT NULL,
    "ReceiptNumber" TEXT NOT NULL,
    "Content" TEXT NOT NULL,
    "PrinterName" TEXT NOT NULL,
    "WidthMillimeters" INTEGER NOT NULL,
    "PrintCount" INTEGER NOT NULL,
    "LastPrintedAtUtc" TEXT NULL,
    "LastPrintedByUserId" TEXT NULL,
    "CreatedAtUtc" TEXT NOT NULL,
    CONSTRAINT "FK_ReceiptRecord_Sale_SaleId" FOREIGN KEY ("SaleId") REFERENCES "Sale" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_ReceiptRecord_UserAccount_LastPrintedByUserId" FOREIGN KEY ("LastPrintedByUserId") REFERENCES "UserAccount" ("Id") ON DELETE RESTRICT
);


CREATE TABLE "Refund" (
    "Id" TEXT NOT NULL CONSTRAINT "PK_Refund" PRIMARY KEY,
    "SaleId" TEXT NOT NULL,
    "RefundNumber" TEXT NOT NULL,
    "Amount" INTEGER NOT NULL,
    "CostOfGoodsReversed" INTEGER NOT NULL,
    "MaterialCostReversed" INTEGER NOT NULL,
    "CommissionCostReversed" INTEGER NOT NULL,
    "Reason" TEXT NOT NULL,
    "Method" INTEGER NOT NULL,
    "AuthorizedByUserId" TEXT NOT NULL,
    "RefundedAtUtc" TEXT NOT NULL,
    "IsVoid" INTEGER NOT NULL,
    "CreatedAtUtc" TEXT NOT NULL,
    CONSTRAINT "FK_Refund_Sale_SaleId" FOREIGN KEY ("SaleId") REFERENCES "Sale" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_Refund_UserAccount_AuthorizedByUserId" FOREIGN KEY ("AuthorizedByUserId") REFERENCES "UserAccount" ("Id") ON DELETE RESTRICT
);


CREATE TABLE "SaleItem" (
    "Id" TEXT NOT NULL CONSTRAINT "PK_SaleItem" PRIMARY KEY,
    "SaleId" TEXT NOT NULL,
    "CatalogItemId" TEXT NULL,
    "PriceRuleId" TEXT NULL,
    "Kind" INTEGER NOT NULL,
    "Name" TEXT NOT NULL,
    "Quantity" INTEGER NOT NULL,
    "UnitPrice" INTEGER NOT NULL,
    "BaseAmount" INTEGER NOT NULL,
    "ChargeAmount" INTEGER NOT NULL,
    "Subtotal" INTEGER NOT NULL,
    "DiscountPercent" INTEGER NOT NULL,
    "DiscountAmount" INTEGER NOT NULL,
    "OrderDiscountAmount" INTEGER NOT NULL,
    "Total" INTEGER NOT NULL,
    "UnitCost" INTEGER NOT NULL,
    "CostOfGoods" INTEGER NOT NULL,
    "MaterialCost" INTEGER NOT NULL,
    "CommissionCost" INTEGER NOT NULL,
    "EmployeeId" TEXT NULL,
    "CustomerPackageId" TEXT NULL,
    "CostInformationComplete" INTEGER NOT NULL,
    "CreatedAtUtc" TEXT NOT NULL,
    CONSTRAINT "CK_SaleItem_Amount" CHECK ("Quantity" > 0 AND "Total" >= 0 AND "DiscountPercent" BETWEEN 0 AND 100000000),
    CONSTRAINT "FK_SaleItem_CustomerPackage_CustomerPackageId" FOREIGN KEY ("CustomerPackageId") REFERENCES "CustomerPackage" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_SaleItem_Employee_EmployeeId" FOREIGN KEY ("EmployeeId") REFERENCES "Employee" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_SaleItem_Sale_SaleId" FOREIGN KEY ("SaleId") REFERENCES "Sale" ("Id") ON DELETE RESTRICT
);


CREATE TABLE "PackageRedemption" (
    "Id" TEXT NOT NULL CONSTRAINT "PK_PackageRedemption" PRIMARY KEY,
    "CustomerPackageId" TEXT NOT NULL,
    "EmployeeId" TEXT NULL,
    "SaleItemId" TEXT NULL,
    "RedeemedAtUtc" TEXT NOT NULL,
    "SessionsUsed" INTEGER NOT NULL,
    "MaterialCost" INTEGER NOT NULL,
    "CommissionCost" INTEGER NOT NULL,
    "RecordedByUserId" TEXT NOT NULL,
    "Notes" TEXT NOT NULL,
    "CostInformationComplete" INTEGER NOT NULL,
    "CreatedAtUtc" TEXT NOT NULL,
    CONSTRAINT "FK_PackageRedemption_CustomerPackage_CustomerPackageId" FOREIGN KEY ("CustomerPackageId") REFERENCES "CustomerPackage" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_PackageRedemption_Employee_EmployeeId" FOREIGN KEY ("EmployeeId") REFERENCES "Employee" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_PackageRedemption_SaleItem_SaleItemId" FOREIGN KEY ("SaleItemId") REFERENCES "SaleItem" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_PackageRedemption_UserAccount_RecordedByUserId" FOREIGN KEY ("RecordedByUserId") REFERENCES "UserAccount" ("Id") ON DELETE RESTRICT
);


CREATE TABLE "RefundItem" (
    "Id" TEXT NOT NULL CONSTRAINT "PK_RefundItem" PRIMARY KEY,
    "RefundId" TEXT NOT NULL,
    "SaleItemId" TEXT NOT NULL,
    "Quantity" INTEGER NOT NULL,
    "Amount" INTEGER NOT NULL,
    "ReturnToStock" INTEGER NOT NULL,
    "CostOfGoodsReversed" INTEGER NOT NULL,
    "CommissionCostReversed" INTEGER NOT NULL,
    "CreatedAtUtc" TEXT NOT NULL,
    CONSTRAINT "FK_RefundItem_Refund_RefundId" FOREIGN KEY ("RefundId") REFERENCES "Refund" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_RefundItem_SaleItem_SaleItemId" FOREIGN KEY ("SaleItemId") REFERENCES "SaleItem" ("Id") ON DELETE RESTRICT
);


CREATE TABLE "SaleItemCharge" (
    "Id" TEXT NOT NULL CONSTRAINT "PK_SaleItemCharge" PRIMARY KEY,
    "SaleItemId" TEXT NOT NULL,
    "Name" TEXT NOT NULL,
    "Description" TEXT NOT NULL,
    "Amount" INTEGER NOT NULL,
    "Total" INTEGER NOT NULL,
    "CreatedAtUtc" TEXT NOT NULL,
    CONSTRAINT "FK_SaleItemCharge_SaleItem_SaleItemId" FOREIGN KEY ("SaleItemId") REFERENCES "SaleItem" ("Id") ON DELETE RESTRICT
);


CREATE TABLE "SaleItemDiscount" (
    "Id" TEXT NOT NULL CONSTRAINT "PK_SaleItemDiscount" PRIMARY KEY,
    "SaleItemId" TEXT NOT NULL,
    "Percent" INTEGER NOT NULL,
    "Amount" INTEGER NOT NULL,
    "IsOrderAllocation" INTEGER NOT NULL,
    "Reason" TEXT NOT NULL,
    "AuthorizedByUserId" TEXT NOT NULL,
    "CreatedAtUtc" TEXT NOT NULL,
    CONSTRAINT "FK_SaleItemDiscount_SaleItem_SaleItemId" FOREIGN KEY ("SaleItemId") REFERENCES "SaleItem" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_SaleItemDiscount_UserAccount_AuthorizedByUserId" FOREIGN KEY ("AuthorizedByUserId") REFERENCES "UserAccount" ("Id") ON DELETE RESTRICT
);


CREATE TABLE "InventoryMovement" (
    "Id" TEXT NOT NULL CONSTRAINT "PK_InventoryMovement" PRIMARY KEY,
    "ProductId" TEXT NOT NULL,
    "Type" INTEGER NOT NULL,
    "Quantity" INTEGER NOT NULL,
    "UnitCost" INTEGER NOT NULL,
    "ValueChange" INTEGER NOT NULL,
    "BalanceAfter" INTEGER NOT NULL,
    "SaleItemId" TEXT NULL,
    "PackageRedemptionId" TEXT NULL,
    "RefundId" TEXT NULL,
    "RecordedByUserId" TEXT NOT NULL,
    "Reason" TEXT NOT NULL,
    "Reference" TEXT NOT NULL,
    "OccurredAtUtc" TEXT NOT NULL,
    "CreatedAtUtc" TEXT NOT NULL,
    CONSTRAINT "FK_InventoryMovement_PackageRedemption_PackageRedemptionId" FOREIGN KEY ("PackageRedemptionId") REFERENCES "PackageRedemption" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_InventoryMovement_Product_ProductId" FOREIGN KEY ("ProductId") REFERENCES "Product" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_InventoryMovement_Refund_RefundId" FOREIGN KEY ("RefundId") REFERENCES "Refund" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_InventoryMovement_SaleItem_SaleItemId" FOREIGN KEY ("SaleItemId") REFERENCES "SaleItem" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_InventoryMovement_UserAccount_RecordedByUserId" FOREIGN KEY ("RecordedByUserId") REFERENCES "UserAccount" ("Id") ON DELETE RESTRICT
);


CREATE TABLE "ServiceMaterialConsumption" (
    "Id" TEXT NOT NULL CONSTRAINT "PK_ServiceMaterialConsumption" PRIMARY KEY,
    "ServiceId" TEXT NOT NULL,
    "ProductId" TEXT NOT NULL,
    "SaleItemId" TEXT NULL,
    "PackageRedemptionId" TEXT NULL,
    "Quantity" INTEGER NOT NULL,
    "UnitCost" INTEGER NOT NULL,
    "TotalCost" INTEGER NOT NULL,
    "RecordedByUserId" TEXT NOT NULL,
    "CreatedAtUtc" TEXT NOT NULL,
    CONSTRAINT "FK_ServiceMaterialConsumption_PackageRedemption_PackageRedemptionId" FOREIGN KEY ("PackageRedemptionId") REFERENCES "PackageRedemption" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_ServiceMaterialConsumption_Product_ProductId" FOREIGN KEY ("ProductId") REFERENCES "Product" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_ServiceMaterialConsumption_SaleItem_SaleItemId" FOREIGN KEY ("SaleItemId") REFERENCES "SaleItem" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_ServiceMaterialConsumption_SalonService_ServiceId" FOREIGN KEY ("ServiceId") REFERENCES "SalonService" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_ServiceMaterialConsumption_UserAccount_RecordedByUserId" FOREIGN KEY ("RecordedByUserId") REFERENCES "UserAccount" ("Id") ON DELETE RESTRICT
);


CREATE UNIQUE INDEX "IX_ApplicationSetting_Key" ON "ApplicationSetting" ("Key");


CREATE INDEX "IX_Appointment_CustomerId" ON "Appointment" ("CustomerId");


CREATE INDEX "IX_Appointment_EmployeeId" ON "Appointment" ("EmployeeId");


CREATE INDEX "IX_Appointment_RecordedByUserId" ON "Appointment" ("RecordedByUserId");


CREATE INDEX "IX_Appointment_SaleItemId" ON "Appointment" ("SaleItemId");


CREATE INDEX "IX_Appointment_ServiceId" ON "Appointment" ("ServiceId");


CREATE INDEX "IX_AuditLog_UserId" ON "AuditLog" ("UserId");


CREATE INDEX "IX_BackupRecord_CreatedByUserId" ON "BackupRecord" ("CreatedByUserId");


CREATE INDEX "IX_CommissionRecord_EmployeeId" ON "CommissionRecord" ("EmployeeId");


CREATE INDEX "IX_CommissionRecord_PackageRedemptionId" ON "CommissionRecord" ("PackageRedemptionId");


CREATE INDEX "IX_CommissionRecord_SaleItemId" ON "CommissionRecord" ("SaleItemId");


CREATE INDEX "IX_CustomerPackage_CustomerId" ON "CustomerPackage" ("CustomerId");


CREATE INDEX "IX_CustomerPackage_PackageOfferId" ON "CustomerPackage" ("PackageOfferId");


CREATE INDEX "IX_CustomerPackage_SaleItemId" ON "CustomerPackage" ("SaleItemId");


CREATE INDEX "IX_CustomerPackage_ServiceId" ON "CustomerPackage" ("ServiceId");


CREATE INDEX "IX_Employee_UserAccountId" ON "Employee" ("UserAccountId");


CREATE UNIQUE INDEX "IX_EmployeeServiceCommission_EmployeeId_ServiceId" ON "EmployeeServiceCommission" ("EmployeeId", "ServiceId");


CREATE INDEX "IX_EmployeeServiceCommission_ServiceId" ON "EmployeeServiceCommission" ("ServiceId");


CREATE UNIQUE INDEX "IX_Expense_CommissionRecordId" ON "Expense" ("CommissionRecordId") WHERE "CommissionRecordId" IS NOT NULL;


CREATE INDEX "IX_Expense_RecordedByUserId" ON "Expense" ("RecordedByUserId");


CREATE UNIQUE INDEX "IX_InventoryAverageCost_ProductId" ON "InventoryAverageCost" ("ProductId");


CREATE INDEX "IX_InventoryMovement_PackageRedemptionId" ON "InventoryMovement" ("PackageRedemptionId");


CREATE INDEX "IX_InventoryMovement_ProductId" ON "InventoryMovement" ("ProductId");


CREATE INDEX "IX_InventoryMovement_RecordedByUserId" ON "InventoryMovement" ("RecordedByUserId");


CREATE INDEX "IX_InventoryMovement_RefundId" ON "InventoryMovement" ("RefundId");


CREATE INDEX "IX_InventoryMovement_SaleItemId" ON "InventoryMovement" ("SaleItemId");


CREATE INDEX "IX_PackageOffer_ServiceId" ON "PackageOffer" ("ServiceId");


CREATE INDEX "IX_PackageRedemption_CustomerPackageId" ON "PackageRedemption" ("CustomerPackageId");


CREATE INDEX "IX_PackageRedemption_EmployeeId" ON "PackageRedemption" ("EmployeeId");


CREATE INDEX "IX_PackageRedemption_RecordedByUserId" ON "PackageRedemption" ("RecordedByUserId");


CREATE INDEX "IX_PackageRedemption_SaleItemId" ON "PackageRedemption" ("SaleItemId");


CREATE INDEX "IX_Payment_SaleId" ON "Payment" ("SaleId");


CREATE UNIQUE INDEX "IX_Permission_Key" ON "Permission" ("Key");


CREATE UNIQUE INDEX "IX_Product_Barcode" ON "Product" ("Barcode") WHERE "Barcode" <> '';


CREATE INDEX "IX_Product_CategoryId" ON "Product" ("CategoryId");


CREATE UNIQUE INDEX "IX_Product_Sku" ON "Product" ("Sku") WHERE "Sku" <> '';


CREATE INDEX "IX_Product_SupplierId" ON "Product" ("SupplierId");


CREATE INDEX "IX_ReceiptRecord_LastPrintedByUserId" ON "ReceiptRecord" ("LastPrintedByUserId");


CREATE UNIQUE INDEX "IX_ReceiptRecord_SaleId" ON "ReceiptRecord" ("SaleId");


CREATE INDEX "IX_Refund_AuthorizedByUserId" ON "Refund" ("AuthorizedByUserId");


CREATE UNIQUE INDEX "IX_Refund_SaleId" ON "Refund" ("SaleId");


CREATE INDEX "IX_RefundItem_RefundId" ON "RefundItem" ("RefundId");


CREATE INDEX "IX_RefundItem_SaleItemId" ON "RefundItem" ("SaleItemId");


CREATE UNIQUE INDEX "IX_Role_Kind" ON "Role" ("Kind");


CREATE INDEX "IX_RolePermission_PermissionId" ON "RolePermission" ("PermissionId");


CREATE UNIQUE INDEX "IX_RolePermission_RoleId_PermissionId" ON "RolePermission" ("RoleId", "PermissionId");


CREATE INDEX "IX_Sale_CashierUserId" ON "Sale" ("CashierUserId");


CREATE UNIQUE INDEX "IX_Sale_CheckoutToken" ON "Sale" ("CheckoutToken");


CREATE INDEX "IX_Sale_CustomerId" ON "Sale" ("CustomerId");


CREATE UNIQUE INDEX "IX_Sale_TransactionNumber" ON "Sale" ("TransactionNumber");


CREATE INDEX "IX_SaleItem_CustomerPackageId" ON "SaleItem" ("CustomerPackageId");


CREATE INDEX "IX_SaleItem_EmployeeId" ON "SaleItem" ("EmployeeId");


CREATE INDEX "IX_SaleItem_SaleId" ON "SaleItem" ("SaleId");


CREATE INDEX "IX_SaleItemCharge_SaleItemId" ON "SaleItemCharge" ("SaleItemId");


CREATE INDEX "IX_SaleItemDiscount_AuthorizedByUserId" ON "SaleItemDiscount" ("AuthorizedByUserId");


CREATE INDEX "IX_SaleItemDiscount_SaleItemId" ON "SaleItemDiscount" ("SaleItemId");


CREATE INDEX "IX_SalonService_CategoryId" ON "SalonService" ("CategoryId");


CREATE INDEX "IX_ServiceChargePreset_ServiceId" ON "ServiceChargePreset" ("ServiceId");


CREATE INDEX "IX_ServiceMaterialConsumption_PackageRedemptionId" ON "ServiceMaterialConsumption" ("PackageRedemptionId");


CREATE INDEX "IX_ServiceMaterialConsumption_ProductId" ON "ServiceMaterialConsumption" ("ProductId");


CREATE INDEX "IX_ServiceMaterialConsumption_RecordedByUserId" ON "ServiceMaterialConsumption" ("RecordedByUserId");


CREATE INDEX "IX_ServiceMaterialConsumption_SaleItemId" ON "ServiceMaterialConsumption" ("SaleItemId");


CREATE INDEX "IX_ServiceMaterialConsumption_ServiceId" ON "ServiceMaterialConsumption" ("ServiceId");


CREATE INDEX "IX_ServiceMaterialRequirement_ProductId" ON "ServiceMaterialRequirement" ("ProductId");


CREATE UNIQUE INDEX "IX_ServiceMaterialRequirement_ServiceId_ProductId" ON "ServiceMaterialRequirement" ("ServiceId", "ProductId");


CREATE INDEX "IX_ServicePriceRule_ServiceId" ON "ServicePriceRule" ("ServiceId");


CREATE INDEX "IX_UserAccount_EmployeeId" ON "UserAccount" ("EmployeeId");


CREATE UNIQUE INDEX "IX_UserAccount_NormalizedUsername" ON "UserAccount" ("NormalizedUsername");


CREATE INDEX "IX_UserAccount_RoleId" ON "UserAccount" ("RoleId");


