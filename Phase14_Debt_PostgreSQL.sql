-- Phase 14: Debt Payoff Planner — PostgreSQL (Render)

CREATE TABLE IF NOT EXISTS "Debts" (
    "Id" uuid NOT NULL DEFAULT gen_random_uuid(),
    "UserId" varchar(256) NOT NULL,
    "Name" varchar(200) NOT NULL,
    "Type" varchar(50) NOT NULL DEFAULT 'Other',
    "Lender" varchar(100) NULL,
    "OriginalAmount" decimal(18,2) NOT NULL,
    "CurrentBalance" decimal(18,2) NOT NULL,
    "InterestRate" decimal(5,2) NOT NULL,
    "MinimumPayment" decimal(18,2) NOT NULL,
    "ActualPayment" decimal(18,2) NOT NULL,
    "DueDay" int NOT NULL DEFAULT 1,
    "StartDate" timestamptz NOT NULL,
    "Status" varchar(20) NOT NULL DEFAULT 'Active',
    "Notes" varchar(500) NULL,
    "CreatedAt" timestamptz NOT NULL DEFAULT NOW(),
    "UpdatedAt" timestamptz NOT NULL DEFAULT NOW(),
    CONSTRAINT "PK_Debts" PRIMARY KEY ("Id")
);
CREATE INDEX IF NOT EXISTS "IX_Debts_UserId" ON "Debts" ("UserId");

CREATE TABLE IF NOT EXISTS "DebtPayments" (
    "Id" uuid NOT NULL DEFAULT gen_random_uuid(),
    "DebtId" uuid NOT NULL,
    "UserId" varchar(256) NOT NULL,
    "Amount" decimal(18,2) NOT NULL,
    "BalanceAfter" decimal(18,2) NOT NULL,
    "Note" varchar(500) NULL,
    "PaymentDate" timestamptz NOT NULL DEFAULT NOW(),
    CONSTRAINT "PK_DebtPayments" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_DebtPayments_Debts" FOREIGN KEY ("DebtId")
        REFERENCES "Debts" ("Id") ON DELETE CASCADE
);
CREATE INDEX IF NOT EXISTS "IX_DebtPayments_UserId" ON "DebtPayments" ("UserId");
CREATE INDEX IF NOT EXISTS "IX_DebtPayments_DebtId" ON "DebtPayments" ("DebtId");
