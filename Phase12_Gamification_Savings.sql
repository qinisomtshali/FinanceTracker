-- FinanceTracker Phase 12: Gamification, Savings & Financial Health
-- Run against Render PostgreSQL

-- ═══════════════════════════════════════════════════════════════
-- User Financial Profile (gamification + health score)
-- ═══════════════════════════════════════════════════════════════
CREATE TABLE IF NOT EXISTS "UserFinancialProfiles" (
    "Id" uuid NOT NULL DEFAULT gen_random_uuid(),
    "UserId" varchar(256) NOT NULL,
    "TotalPoints" int NOT NULL DEFAULT 0,
    "Level" int NOT NULL DEFAULT 1,
    "Tier" varchar(20) NOT NULL DEFAULT 'Bronze',
    "CurrentStreak" int NOT NULL DEFAULT 0,
    "LongestStreak" int NOT NULL DEFAULT 0,
    "LastActivityDate" timestamptz NULL,
    "HealthScore" int NOT NULL DEFAULT 0,
    "HealthGrade" varchar(20) NOT NULL DEFAULT 'Needs Work',
    "MonthlyBudgetAdherence" decimal(5,2) NOT NULL DEFAULT 0,
    "SavingsRate" decimal(5,2) NOT NULL DEFAULT 0,
    "DebtToIncomeRatio" decimal(5,2) NOT NULL DEFAULT 0,
    "CreatedAt" timestamptz NOT NULL DEFAULT NOW(),
    "UpdatedAt" timestamptz NOT NULL DEFAULT NOW(),
    CONSTRAINT "PK_UserFinancialProfiles" PRIMARY KEY ("Id")
);
CREATE UNIQUE INDEX IF NOT EXISTS "IX_UserFinancialProfiles_UserId" ON "UserFinancialProfiles" ("UserId");

-- ═══════════════════════════════════════════════════════════════
-- Achievements
-- ═══════════════════════════════════════════════════════════════
CREATE TABLE IF NOT EXISTS "Achievements" (
    "Id" uuid NOT NULL DEFAULT gen_random_uuid(),
    "Code" varchar(50) NOT NULL,
    "Name" varchar(100) NOT NULL,
    "Description" varchar(500) NOT NULL DEFAULT '',
    "Icon" varchar(10) NOT NULL DEFAULT '🏆',
    "Category" varchar(50) NOT NULL DEFAULT '',
    "PointsAwarded" int NOT NULL DEFAULT 0,
    "Difficulty" varchar(20) NOT NULL DEFAULT 'Easy',
    CONSTRAINT "PK_Achievements" PRIMARY KEY ("Id")
);
CREATE UNIQUE INDEX IF NOT EXISTS "IX_Achievements_Code" ON "Achievements" ("Code");

-- ═══════════════════════════════════════════════════════════════
-- User Achievements (junction table)
-- ═══════════════════════════════════════════════════════════════
CREATE TABLE IF NOT EXISTS "UserAchievements" (
    "Id" uuid NOT NULL DEFAULT gen_random_uuid(),
    "UserId" varchar(256) NOT NULL,
    "AchievementId" uuid NOT NULL,
    "UnlockedAt" timestamptz NOT NULL DEFAULT NOW(),
    CONSTRAINT "PK_UserAchievements" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_UserAchievements_Achievements" FOREIGN KEY ("AchievementId")
        REFERENCES "Achievements" ("Id") ON DELETE CASCADE
);
CREATE UNIQUE INDEX IF NOT EXISTS "IX_UserAchievements_UserId_AchievementId" ON "UserAchievements" ("UserId", "AchievementId");

-- ═══════════════════════════════════════════════════════════════
-- Point Transactions (audit log)
-- ═══════════════════════════════════════════════════════════════
CREATE TABLE IF NOT EXISTS "PointTransactions" (
    "Id" uuid NOT NULL DEFAULT gen_random_uuid(),
    "UserId" varchar(256) NOT NULL,
    "Points" int NOT NULL,
    "Reason" varchar(200) NOT NULL DEFAULT '',
    "Category" varchar(50) NOT NULL DEFAULT '',
    "EarnedAt" timestamptz NOT NULL DEFAULT NOW(),
    CONSTRAINT "PK_PointTransactions" PRIMARY KEY ("Id")
);
CREATE INDEX IF NOT EXISTS "IX_PointTransactions_UserId" ON "PointTransactions" ("UserId");
CREATE INDEX IF NOT EXISTS "IX_PointTransactions_EarnedAt" ON "PointTransactions" ("EarnedAt");

-- ═══════════════════════════════════════════════════════════════
-- Savings Goals
-- ═══════════════════════════════════════════════════════════════
CREATE TABLE IF NOT EXISTS "SavingsGoals" (
    "Id" uuid NOT NULL DEFAULT gen_random_uuid(),
    "UserId" varchar(256) NOT NULL,
    "Name" varchar(200) NOT NULL,
    "Icon" varchar(10) NULL DEFAULT '🎯',
    "Color" varchar(20) NULL DEFAULT '#8B5CF6',
    "TargetAmount" decimal(18,2) NOT NULL,
    "CurrentAmount" decimal(18,2) NOT NULL DEFAULT 0,
    "MonthlyContribution" decimal(18,2) NULL,
    "Priority" varchar(20) NOT NULL DEFAULT 'Medium',
    "Status" varchar(20) NOT NULL DEFAULT 'Active',
    "TargetDate" timestamptz NULL,
    "CompletedDate" timestamptz NULL,
    "CreatedAt" timestamptz NOT NULL DEFAULT NOW(),
    "UpdatedAt" timestamptz NOT NULL DEFAULT NOW(),
    CONSTRAINT "PK_SavingsGoals" PRIMARY KEY ("Id")
);
CREATE INDEX IF NOT EXISTS "IX_SavingsGoals_UserId" ON "SavingsGoals" ("UserId");

-- ═══════════════════════════════════════════════════════════════
-- Savings Deposits
-- ═══════════════════════════════════════════════════════════════
CREATE TABLE IF NOT EXISTS "SavingsDeposits" (
    "Id" uuid NOT NULL DEFAULT gen_random_uuid(),
    "SavingsGoalId" uuid NOT NULL,
    "UserId" varchar(256) NOT NULL,
    "Amount" decimal(18,2) NOT NULL,
    "Note" varchar(500) NULL,
    "DepositDate" timestamptz NOT NULL DEFAULT NOW(),
    CONSTRAINT "PK_SavingsDeposits" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_SavingsDeposits_SavingsGoals" FOREIGN KEY ("SavingsGoalId")
        REFERENCES "SavingsGoals" ("Id") ON DELETE CASCADE
);
CREATE INDEX IF NOT EXISTS "IX_SavingsDeposits_UserId" ON "SavingsDeposits" ("UserId");

-- ═══════════════════════════════════════════════════════════════
-- Savings Challenges
-- ═══════════════════════════════════════════════════════════════
CREATE TABLE IF NOT EXISTS "SavingsChallenges" (
    "Id" uuid NOT NULL DEFAULT gen_random_uuid(),
    "UserId" varchar(256) NOT NULL,
    "Type" varchar(50) NOT NULL,
    "Name" varchar(200) NOT NULL,
    "Status" varchar(20) NOT NULL DEFAULT 'Active',
    "TargetAmount" decimal(18,2) NOT NULL DEFAULT 0,
    "CurrentAmount" decimal(18,2) NOT NULL DEFAULT 0,
    "CurrentDay" int NOT NULL DEFAULT 0,
    "TotalDays" int NOT NULL DEFAULT 0,
    "ProgressData" text NULL,
    "StartDate" timestamptz NOT NULL DEFAULT NOW(),
    "EndDate" timestamptz NULL,
    "CreatedAt" timestamptz NOT NULL DEFAULT NOW(),
    "UpdatedAt" timestamptz NOT NULL DEFAULT NOW(),
    CONSTRAINT "PK_SavingsChallenges" PRIMARY KEY ("Id")
);
CREATE INDEX IF NOT EXISTS "IX_SavingsChallenges_UserId" ON "SavingsChallenges" ("UserId");

-- ═══════════════════════════════════════════════════════════════
-- Financial Tips
-- ═══════════════════════════════════════════════════════════════
CREATE TABLE IF NOT EXISTS "FinancialTips" (
    "Id" uuid NOT NULL DEFAULT gen_random_uuid(),
    "Title" varchar(200) NOT NULL,
    "Content" varchar(2000) NOT NULL,
    "Category" varchar(50) NOT NULL DEFAULT '',
    "Difficulty" varchar(20) NOT NULL DEFAULT 'Beginner',
    "SourceUrl" varchar(500) NULL,
    "IsActive" boolean NOT NULL DEFAULT true,
    CONSTRAINT "PK_FinancialTips" PRIMARY KEY ("Id")
);

-- ═══════════════════════════════════════════════════════════════
-- SEED DATA: Achievements
-- ═══════════════════════════════════════════════════════════════
INSERT INTO "Achievements" ("Code", "Name", "Description", "Icon", "Category", "PointsAwarded", "Difficulty") VALUES
('FIRST_TRANSACTION', 'First Steps', 'Log your first transaction', '👣', 'Tracking', 50, 'Easy'),
('LOG_10_TRANSACTIONS', 'Getting Started', 'Log 10 transactions', '📝', 'Tracking', 25, 'Easy'),
('LOG_50_TRANSACTIONS', 'Consistent Tracker', 'Log 50 transactions', '📊', 'Tracking', 75, 'Medium'),
('LOG_100_TRANSACTIONS', 'Dedicated Logger', 'Log 100 transactions', '🔥', 'Tracking', 150, 'Hard'),
('FIRST_BUDGET', 'Budget Beginner', 'Create your first budget', '💰', 'Budgeting', 30, 'Easy'),
('UNDER_BUDGET_3', 'Budget Boss', 'Stay under budget for 3 consecutive months', '🎯', 'Budgeting', 100, 'Medium'),
('UNDER_BUDGET_6', 'Budget Master', 'Stay under budget for 6 consecutive months', '👑', 'Budgeting', 250, 'Hard'),
('FIRST_SAVINGS_GOAL', 'Saver Starter', 'Create your first savings goal', '🐷', 'Saving', 20, 'Easy'),
('COMPLETE_SAVINGS_GOAL', 'Goal Getter', 'Complete a savings goal', '🏆', 'Saving', 100, 'Medium'),
('EMERGENCY_FUND_1M', 'Safety Net', 'Save 1 month of expenses in emergency fund', '🛡️', 'Saving', 75, 'Medium'),
('EMERGENCY_FUND_3M', 'Fully Protected', 'Save 3 months of expenses in emergency fund', '🏰', 'Saving', 200, 'Hard'),
('EMERGENCY_FUND_6M', 'Financial Fortress', 'Save 6 months of expenses in emergency fund', '💎', 'Saving', 500, 'Epic'),
('STREAK_7', 'Week Warrior', '7-day logging streak', '🔥', 'Streak', 25, 'Easy'),
('STREAK_30', 'Month Champion', '30-day logging streak', '⚡', 'Streak', 100, 'Medium'),
('STREAK_90', 'Quarter Legend', '90-day logging streak', '🌟', 'Streak', 300, 'Hard'),
('STREAK_365', 'Year of Discipline', '365-day logging streak', '💫', 'Streak', 1000, 'Legendary'),
('FIRST_INVOICE', 'Business Starter', 'Create your first invoice', '📄', 'Business', 20, 'Easy'),
('INVOICE_PAID', 'Money In', 'Get your first invoice paid', '💵', 'Business', 30, 'Easy'),
('COMPLETE_CHALLENGE', 'Challenge Accepted', 'Complete a savings challenge', '🏅', 'Saving', 200, 'Medium'),
('HEALTH_SCORE_70', 'Financially Fit', 'Reach a financial health score of 70+', '💪', 'Health', 150, 'Medium'),
('HEALTH_SCORE_90', 'Financial Excellence', 'Reach a financial health score of 90+', '🌈', 'Health', 500, 'Epic'),
('SAVINGS_RATE_20', 'Super Saver', 'Maintain a 20%+ savings rate for a month', '🚀', 'Saving', 100, 'Medium'),
('LEVEL_5', 'Rising Star', 'Reach Level 5', '⭐', 'Level', 50, 'Easy'),
('LEVEL_10', 'Finance Pro', 'Reach Level 10', '🌟', 'Level', 200, 'Hard'),
('LEVEL_15', 'Money Master', 'Reach Level 15 (Diamond tier)', '💎', 'Level', 500, 'Legendary')
ON CONFLICT ("Code") DO NOTHING;

-- ═══════════════════════════════════════════════════════════════
-- SEED DATA: Financial Tips (SA-focused)
-- ═══════════════════════════════════════════════════════════════
INSERT INTO "FinancialTips" ("Title", "Content", "Category", "Difficulty") VALUES
('The 50/30/20 Rule', 'Allocate 50% of your income to needs (rent, food, transport), 30% to wants (entertainment, dining out), and 20% to savings and debt repayment. This is a simple framework to start budgeting.', 'Budgeting', 'Beginner'),
('Pay Yourself First', 'Set up a debit order to transfer money to your savings account on payday, before you spend on anything else. Even R500/month adds up to R6,000/year plus interest.', 'Saving', 'Beginner'),
('Track Every Rand', 'Log all your expenses, no matter how small. That R15 coffee and R30 parking adds up. Most people are shocked when they see where their money actually goes.', 'Tracking', 'Beginner'),
('Emergency Fund Basics', 'Aim to save 3-6 months of living expenses in a high-interest savings account. TymeBank GoalSave and African Bank notice deposits offer some of the best rates in SA.', 'Saving', 'Beginner'),
('Tax-Free Savings Account', 'Open a TFSA — you can invest up to R36,000/year (R500,000 lifetime) and pay zero tax on the returns. Available at most SA brokerages like EasyEquities.', 'Investing', 'Intermediate'),
('Retirement Annuity Benefits', 'Contributing to an RA reduces your taxable income by up to 27.5% (max R350,000). If you earn R500k, putting R50k into an RA saves you ~R13k in tax.', 'Tax', 'Intermediate'),
('The Latte Factor', 'Small daily expenses compound. Spending R50/day on takeaway coffee = R18,250/year. That same amount invested at 8% for 10 years = R271,000. Cook at home more.', 'Saving', 'Beginner'),
('Avoid Store Credit', 'Store cards charge up to 21% interest. If you buy a R5,000 appliance on 24-month store credit, you could pay over R6,500 total. Save up and buy cash.', 'Debt', 'Beginner'),
('Review Subscriptions Monthly', 'Cancel unused gym memberships, streaming services, and app subscriptions. The average South African wastes R300-R500/month on forgotten subscriptions.', 'Budgeting', 'Beginner'),
('Negotiate Your Bills', 'Call your insurance, phone, and internet providers annually to negotiate better rates. Loyalty rarely pays — new customer deals are almost always better.', 'Saving', 'Intermediate'),
('Invest in ETFs', 'SA index funds like the Satrix 40 or Ashburton 1200 give you diversified exposure to top companies for as little as R1/month on EasyEquities. Low fees, long-term growth.', 'Investing', 'Intermediate'),
('The 72 Rule', 'Divide 72 by your interest rate to estimate how long it takes to double your money. At 8% annual return, your money doubles in 9 years. Start investing early.', 'Investing', 'Beginner'),
('Medical Aid Tax Credits', 'You get R364/month credit for the main member, R364 for the first dependant, and R246 for additional dependants. Make sure you claim these on your tax return.', 'Tax', 'Intermediate'),
('Build Multiple Income Streams', 'Dont rely on one salary. Consider freelancing, selling on Takealot/Shopify, tutoring, or offering your skills on platforms like Fiverr or Upwork.', 'Saving', 'Advanced'),
('Automate Everything', 'Set up automatic debit orders for savings, investments, and bill payments. Automation removes the temptation to spend and ensures consistency.', 'Budgeting', 'Beginner')
ON CONFLICT DO NOTHING;
