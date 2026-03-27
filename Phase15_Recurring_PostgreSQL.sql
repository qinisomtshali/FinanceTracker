-- Phase 15: Recurring Transactions & Bill Calendar — PostgreSQL (Render)

CREATE TABLE IF NOT EXISTS "RecurringTransactions" (
    "Id" uuid NOT NULL DEFAULT gen_random_uuid(),
    "Name" varchar(200) NOT NULL,
    "Amount" decimal(18,2) NOT NULL,
    "Description" varchar(500) NULL,
    "Type" int NOT NULL, -- 0=Income, 1=Expense
    "CategoryId" uuid NOT NULL,
    "UserId" uuid NOT NULL,
    "Frequency" varchar(20) NOT NULL DEFAULT 'Monthly',
    "DayOfMonth" int NOT NULL DEFAULT 1,
    "DayOfWeek" int NULL,
    "StartDate" timestamptz NOT NULL,
    "EndDate" timestamptz NULL,
    "LastGeneratedDate" timestamptz NULL,
    "NextDueDate" timestamptz NULL,
    "IsActive" boolean NOT NULL DEFAULT true,
    "AutoGenerate" boolean NOT NULL DEFAULT true,
    "NotifyBeforeDue" boolean NOT NULL DEFAULT true,
    "NotifyDaysBefore" int NOT NULL DEFAULT 2,
    "CreatedAt" timestamptz NOT NULL DEFAULT NOW(),
    "UpdatedAt" timestamptz NOT NULL DEFAULT NOW(),
    CONSTRAINT "PK_RecurringTransactions" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_RecurringTransactions_Categories" FOREIGN KEY ("CategoryId") REFERENCES "Categories" ("Id")
);
CREATE INDEX IF NOT EXISTS "IX_RecurringTransactions_UserId" ON "RecurringTransactions" ("UserId");
CREATE INDEX IF NOT EXISTS "IX_RecurringTransactions_NextDueDate" ON "RecurringTransactions" ("NextDueDate");
