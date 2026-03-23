-- FinanceTracker Phase 11: Market Tracking, Currency, Tax & Invoicing
-- Run this migration against your SQL Server (local) or PostgreSQL (Render)
-- EF Core will generate this via: dotnet ef migrations add AddMarketTaxInvoice

-- ═══════════════════════════════════════════════════════════════
-- Stock Watchlist
-- ═══════════════════════════════════════════════════════════════
CREATE TABLE IF NOT EXISTS "StockWatchlistItems" (
    "Id" uuid NOT NULL DEFAULT gen_random_uuid(),
    "UserId" varchar(256) NOT NULL,
    "Symbol" varchar(20) NOT NULL,
    "Exchange" varchar(20) NOT NULL DEFAULT '',
    "Name" varchar(200) NOT NULL,
    "AlertPriceAbove" decimal(18,4) NULL,
    "AlertPriceBelow" decimal(18,4) NULL,
    "Notes" varchar(500) NULL,
    "CreatedAt" timestamptz NOT NULL DEFAULT NOW(),
    "UpdatedAt" timestamptz NOT NULL DEFAULT NOW(),
    CONSTRAINT "PK_StockWatchlistItems" PRIMARY KEY ("Id")
);

CREATE INDEX "IX_StockWatchlistItems_UserId" ON "StockWatchlistItems" ("UserId");
CREATE UNIQUE INDEX "IX_StockWatchlistItems_UserId_Symbol" ON "StockWatchlistItems" ("UserId", "Symbol");

-- ═══════════════════════════════════════════════════════════════
-- Crypto Watchlist
-- ═══════════════════════════════════════════════════════════════
CREATE TABLE IF NOT EXISTS "CryptoWatchlistItems" (
    "Id" uuid NOT NULL DEFAULT gen_random_uuid(),
    "UserId" varchar(256) NOT NULL,
    "CoinId" varchar(100) NOT NULL,
    "Symbol" varchar(20) NOT NULL,
    "Name" varchar(200) NOT NULL,
    "HoldingQuantity" decimal(18,8) NULL,
    "AverageBuyPrice" decimal(18,4) NULL,
    "Currency" varchar(10) NOT NULL DEFAULT 'USD',
    "AlertPriceAbove" decimal(18,4) NULL,
    "AlertPriceBelow" decimal(18,4) NULL,
    "Notes" varchar(500) NULL,
    "CreatedAt" timestamptz NOT NULL DEFAULT NOW(),
    "UpdatedAt" timestamptz NOT NULL DEFAULT NOW(),
    CONSTRAINT "PK_CryptoWatchlistItems" PRIMARY KEY ("Id")
);

CREATE INDEX "IX_CryptoWatchlistItems_UserId" ON "CryptoWatchlistItems" ("UserId");
CREATE UNIQUE INDEX "IX_CryptoWatchlistItems_UserId_CoinId" ON "CryptoWatchlistItems" ("UserId", "CoinId");

-- ═══════════════════════════════════════════════════════════════
-- Currency Conversions (audit log)
-- ═══════════════════════════════════════════════════════════════
CREATE TABLE IF NOT EXISTS "CurrencyConversions" (
    "Id" uuid NOT NULL DEFAULT gen_random_uuid(),
    "UserId" varchar(256) NOT NULL,
    "FromCurrency" varchar(10) NOT NULL,
    "ToCurrency" varchar(10) NOT NULL,
    "Amount" decimal(18,4) NOT NULL,
    "ConvertedAmount" decimal(18,4) NOT NULL,
    "ExchangeRate" decimal(18,8) NOT NULL,
    "Provider" varchar(100) NULL,
    "ConvertedAt" timestamptz NOT NULL DEFAULT NOW(),
    CONSTRAINT "PK_CurrencyConversions" PRIMARY KEY ("Id")
);

CREATE INDEX "IX_CurrencyConversions_UserId" ON "CurrencyConversions" ("UserId");
CREATE INDEX "IX_CurrencyConversions_ConvertedAt" ON "CurrencyConversions" ("ConvertedAt");

-- ═══════════════════════════════════════════════════════════════
-- Tax Calculations (saved history)
-- ═══════════════════════════════════════════════════════════════
CREATE TABLE IF NOT EXISTS "TaxCalculations" (
    "Id" uuid NOT NULL DEFAULT gen_random_uuid(),
    "UserId" varchar(256) NOT NULL,
    "TaxYear" varchar(20) NOT NULL,
    "Country" varchar(10) NOT NULL DEFAULT 'ZA',
    "GrossIncome" decimal(18,2) NOT NULL,
    "TaxableIncome" decimal(18,2) NOT NULL,
    "TaxAmount" decimal(18,2) NOT NULL,
    "EffectiveRate" decimal(5,2) NOT NULL,
    "MedicalAidCredits" decimal(18,2) NULL,
    "RetirementDeduction" decimal(18,2) NULL,
    "Age" int NULL,
    "TaxBracketDetails" text NULL,
    "CalculatedAt" timestamptz NOT NULL DEFAULT NOW(),
    CONSTRAINT "PK_TaxCalculations" PRIMARY KEY ("Id")
);

CREATE INDEX "IX_TaxCalculations_UserId" ON "TaxCalculations" ("UserId");

-- ═══════════════════════════════════════════════════════════════
-- Invoices
-- ═══════════════════════════════════════════════════════════════
CREATE TABLE IF NOT EXISTS "Invoices" (
    "Id" uuid NOT NULL DEFAULT gen_random_uuid(),
    "UserId" varchar(256) NOT NULL,
    "InvoiceNumber" varchar(50) NOT NULL,
    "Status" varchar(20) NOT NULL DEFAULT 'Draft',
    -- Sender
    "FromName" varchar(200) NOT NULL,
    "FromEmail" varchar(200) NULL,
    "FromAddress" varchar(500) NULL,
    "FromVatNumber" varchar(50) NULL,
    -- Recipient
    "ToName" varchar(200) NOT NULL,
    "ToEmail" varchar(200) NULL,
    "ToAddress" varchar(500) NULL,
    "ToVatNumber" varchar(50) NULL,
    -- Financials
    "Currency" varchar(10) NOT NULL DEFAULT 'ZAR',
    "Subtotal" decimal(18,2) NOT NULL,
    "VatRate" decimal(5,2) NOT NULL DEFAULT 15,
    "VatAmount" decimal(18,2) NOT NULL,
    "Total" decimal(18,2) NOT NULL,
    "DiscountPercentage" decimal(5,2) NULL,
    "DiscountAmount" decimal(18,2) NULL,
    -- Dates
    "IssueDate" timestamptz NOT NULL DEFAULT NOW(),
    "DueDate" timestamptz NOT NULL,
    "PaidDate" timestamptz NULL,
    -- Banking
    "BankName" varchar(100) NULL,
    "AccountHolder" varchar(200) NULL,
    "AccountNumber" varchar(50) NULL,
    "BranchCode" varchar(20) NULL,
    "Reference" varchar(100) NULL,
    "Notes" varchar(1000) NULL,
    "CreatedAt" timestamptz NOT NULL DEFAULT NOW(),
    "UpdatedAt" timestamptz NOT NULL DEFAULT NOW(),
    CONSTRAINT "PK_Invoices" PRIMARY KEY ("Id")
);

CREATE INDEX "IX_Invoices_UserId" ON "Invoices" ("UserId");
CREATE UNIQUE INDEX "IX_Invoices_UserId_InvoiceNumber" ON "Invoices" ("UserId", "InvoiceNumber");
CREATE INDEX "IX_Invoices_Status" ON "Invoices" ("Status");

-- ═══════════════════════════════════════════════════════════════
-- Invoice Line Items
-- ═══════════════════════════════════════════════════════════════
CREATE TABLE IF NOT EXISTS "InvoiceLineItems" (
    "Id" uuid NOT NULL DEFAULT gen_random_uuid(),
    "InvoiceId" uuid NOT NULL,
    "Description" varchar(500) NOT NULL,
    "Quantity" decimal(10,2) NOT NULL DEFAULT 1,
    "UnitPrice" decimal(18,2) NOT NULL,
    "LineTotal" decimal(18,2) NOT NULL,
    "SortOrder" int NOT NULL DEFAULT 0,
    CONSTRAINT "PK_InvoiceLineItems" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_InvoiceLineItems_Invoices" FOREIGN KEY ("InvoiceId")
        REFERENCES "Invoices" ("Id") ON DELETE CASCADE
);
