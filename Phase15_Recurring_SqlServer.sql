-- Phase 15: Recurring Transactions & Bill Calendar — SQL Server (Local Dev)

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'RecurringTransactions')
CREATE TABLE [RecurringTransactions] (
    [Id] UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
    [Name] NVARCHAR(200) NOT NULL,
    [Amount] DECIMAL(18,2) NOT NULL,
    [Description] NVARCHAR(500) NULL,
    [Type] INT NOT NULL, -- 0=Income, 1=Expense (TransactionType enum)
    [CategoryId] UNIQUEIDENTIFIER NOT NULL,
    [UserId] UNIQUEIDENTIFIER NOT NULL,
    [Frequency] NVARCHAR(20) NOT NULL DEFAULT 'Monthly',
    [DayOfMonth] INT NOT NULL DEFAULT 1,
    [DayOfWeek] INT NULL,
    [StartDate] DATETIME2 NOT NULL,
    [EndDate] DATETIME2 NULL,
    [LastGeneratedDate] DATETIME2 NULL,
    [NextDueDate] DATETIME2 NULL,
    [IsActive] BIT NOT NULL DEFAULT 1,
    [AutoGenerate] BIT NOT NULL DEFAULT 1,
    [NotifyBeforeDue] BIT NOT NULL DEFAULT 1,
    [NotifyDaysBefore] INT NOT NULL DEFAULT 2,
    [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [UpdatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT [PK_RecurringTransactions] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_RecurringTransactions_Categories] FOREIGN KEY ([CategoryId]) REFERENCES [Categories] ([Id])
);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_RecurringTransactions_UserId')
CREATE INDEX [IX_RecurringTransactions_UserId] ON [RecurringTransactions] ([UserId]);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_RecurringTransactions_NextDueDate')
CREATE INDEX [IX_RecurringTransactions_NextDueDate] ON [RecurringTransactions] ([NextDueDate]);

PRINT 'Phase 15 Recurring Transactions table created successfully!';
