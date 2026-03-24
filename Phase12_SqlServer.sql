-- FinanceTracker Phase 12: Gamification, Savings & Financial Health
-- Run in SSMS against FinanceTrackerDb (SQL Server Local)

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'UserFinancialProfiles')
CREATE TABLE [UserFinancialProfiles] (
    [Id] UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
    [UserId] NVARCHAR(256) NOT NULL,
    [TotalPoints] INT NOT NULL DEFAULT 0,
    [Level] INT NOT NULL DEFAULT 1,
    [Tier] NVARCHAR(20) NOT NULL DEFAULT 'Bronze',
    [CurrentStreak] INT NOT NULL DEFAULT 0,
    [LongestStreak] INT NOT NULL DEFAULT 0,
    [LastActivityDate] DATETIME2 NULL,
    [HealthScore] INT NOT NULL DEFAULT 0,
    [HealthGrade] NVARCHAR(20) NOT NULL DEFAULT 'Needs Work',
    [MonthlyBudgetAdherence] DECIMAL(5,2) NOT NULL DEFAULT 0,
    [SavingsRate] DECIMAL(5,2) NOT NULL DEFAULT 0,
    [DebtToIncomeRatio] DECIMAL(5,2) NOT NULL DEFAULT 0,
    [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [UpdatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT [PK_UserFinancialProfiles] PRIMARY KEY ([Id])
);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_UserFinancialProfiles_UserId')
CREATE UNIQUE INDEX [IX_UserFinancialProfiles_UserId] ON [UserFinancialProfiles] ([UserId]);

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Achievements')
CREATE TABLE [Achievements] (
    [Id] UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
    [Code] NVARCHAR(50) NOT NULL,
    [Name] NVARCHAR(100) NOT NULL,
    [Description] NVARCHAR(500) NOT NULL DEFAULT '',
    [Icon] NVARCHAR(10) NOT NULL DEFAULT N'🏆',
    [Category] NVARCHAR(50) NOT NULL DEFAULT '',
    [PointsAwarded] INT NOT NULL DEFAULT 0,
    [Difficulty] NVARCHAR(20) NOT NULL DEFAULT 'Easy',
    CONSTRAINT [PK_Achievements] PRIMARY KEY ([Id])
);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Achievements_Code')
CREATE UNIQUE INDEX [IX_Achievements_Code] ON [Achievements] ([Code]);

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'UserAchievements')
CREATE TABLE [UserAchievements] (
    [Id] UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
    [UserId] NVARCHAR(256) NOT NULL,
    [AchievementId] UNIQUEIDENTIFIER NOT NULL,
    [UnlockedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT [PK_UserAchievements] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_UserAchievements_Achievements] FOREIGN KEY ([AchievementId]) REFERENCES [Achievements] ([Id]) ON DELETE CASCADE
);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_UserAchievements_UserId_AchievementId')
CREATE UNIQUE INDEX [IX_UserAchievements_UserId_AchievementId] ON [UserAchievements] ([UserId], [AchievementId]);

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'PointTransactions')
CREATE TABLE [PointTransactions] (
    [Id] UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
    [UserId] NVARCHAR(256) NOT NULL,
    [Points] INT NOT NULL,
    [Reason] NVARCHAR(200) NOT NULL DEFAULT '',
    [Category] NVARCHAR(50) NOT NULL DEFAULT '',
    [EarnedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT [PK_PointTransactions] PRIMARY KEY ([Id])
);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_PointTransactions_UserId')
CREATE INDEX [IX_PointTransactions_UserId] ON [PointTransactions] ([UserId]);

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'SavingsGoals')
CREATE TABLE [SavingsGoals] (
    [Id] UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
    [UserId] NVARCHAR(256) NOT NULL,
    [Name] NVARCHAR(200) NOT NULL,
    [Icon] NVARCHAR(10) NULL DEFAULT N'🎯',
    [Color] NVARCHAR(20) NULL DEFAULT '#8B5CF6',
    [TargetAmount] DECIMAL(18,2) NOT NULL,
    [CurrentAmount] DECIMAL(18,2) NOT NULL DEFAULT 0,
    [MonthlyContribution] DECIMAL(18,2) NULL,
    [Priority] NVARCHAR(20) NOT NULL DEFAULT 'Medium',
    [Status] NVARCHAR(20) NOT NULL DEFAULT 'Active',
    [TargetDate] DATETIME2 NULL,
    [CompletedDate] DATETIME2 NULL,
    [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [UpdatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT [PK_SavingsGoals] PRIMARY KEY ([Id])
);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_SavingsGoals_UserId')
CREATE INDEX [IX_SavingsGoals_UserId] ON [SavingsGoals] ([UserId]);

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'SavingsDeposits')
CREATE TABLE [SavingsDeposits] (
    [Id] UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
    [SavingsGoalId] UNIQUEIDENTIFIER NOT NULL,
    [UserId] NVARCHAR(256) NOT NULL,
    [Amount] DECIMAL(18,2) NOT NULL,
    [Note] NVARCHAR(500) NULL,
    [DepositDate] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT [PK_SavingsDeposits] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_SavingsDeposits_SavingsGoals] FOREIGN KEY ([SavingsGoalId]) REFERENCES [SavingsGoals] ([Id]) ON DELETE CASCADE
);

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'SavingsChallenges')
CREATE TABLE [SavingsChallenges] (
    [Id] UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
    [UserId] NVARCHAR(256) NOT NULL,
    [Type] NVARCHAR(50) NOT NULL,
    [Name] NVARCHAR(200) NOT NULL,
    [Status] NVARCHAR(20) NOT NULL DEFAULT 'Active',
    [TargetAmount] DECIMAL(18,2) NOT NULL DEFAULT 0,
    [CurrentAmount] DECIMAL(18,2) NOT NULL DEFAULT 0,
    [CurrentDay] INT NOT NULL DEFAULT 0,
    [TotalDays] INT NOT NULL DEFAULT 0,
    [ProgressData] NVARCHAR(MAX) NULL,
    [StartDate] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [EndDate] DATETIME2 NULL,
    [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [UpdatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT [PK_SavingsChallenges] PRIMARY KEY ([Id])
);

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'FinancialTips')
CREATE TABLE [FinancialTips] (
    [Id] UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
    [Title] NVARCHAR(200) NOT NULL,
    [Content] NVARCHAR(2000) NOT NULL,
    [Category] NVARCHAR(50) NOT NULL DEFAULT '',
    [Difficulty] NVARCHAR(20) NOT NULL DEFAULT 'Beginner',
    [SourceUrl] NVARCHAR(500) NULL,
    [IsActive] BIT NOT NULL DEFAULT 1,
    CONSTRAINT [PK_FinancialTips] PRIMARY KEY ([Id])
);

PRINT 'Phase 12 tables created successfully!';
