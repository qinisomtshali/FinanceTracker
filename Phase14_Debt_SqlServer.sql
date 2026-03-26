-- Phase 14: Debt Payoff Planner — SQL Server (Local Dev)

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Debts')
CREATE TABLE [Debts] (
    [Id] UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
    [UserId] NVARCHAR(256) NOT NULL,
    [Name] NVARCHAR(200) NOT NULL,
    [Type] NVARCHAR(50) NOT NULL DEFAULT 'Other',
    [Lender] NVARCHAR(100) NULL,
    [OriginalAmount] DECIMAL(18,2) NOT NULL,
    [CurrentBalance] DECIMAL(18,2) NOT NULL,
    [InterestRate] DECIMAL(5,2) NOT NULL,
    [MinimumPayment] DECIMAL(18,2) NOT NULL,
    [ActualPayment] DECIMAL(18,2) NOT NULL,
    [DueDay] INT NOT NULL DEFAULT 1,
    [StartDate] DATETIME2 NOT NULL,
    [Status] NVARCHAR(20) NOT NULL DEFAULT 'Active',
    [Notes] NVARCHAR(500) NULL,
    [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [UpdatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT [PK_Debts] PRIMARY KEY ([Id])
);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Debts_UserId')
CREATE INDEX [IX_Debts_UserId] ON [Debts] ([UserId]);

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'DebtPayments')
CREATE TABLE [DebtPayments] (
    [Id] UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
    [DebtId] UNIQUEIDENTIFIER NOT NULL,
    [UserId] NVARCHAR(256) NOT NULL,
    [Amount] DECIMAL(18,2) NOT NULL,
    [BalanceAfter] DECIMAL(18,2) NOT NULL,
    [Note] NVARCHAR(500) NULL,
    [PaymentDate] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT [PK_DebtPayments] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_DebtPayments_Debts] FOREIGN KEY ([DebtId]) REFERENCES [Debts] ([Id]) ON DELETE CASCADE
);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_DebtPayments_UserId')
CREATE INDEX [IX_DebtPayments_UserId] ON [DebtPayments] ([UserId]);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_DebtPayments_DebtId')
CREATE INDEX [IX_DebtPayments_DebtId] ON [DebtPayments] ([DebtId]);

PRINT 'Phase 14 Debt tables created successfully!';
