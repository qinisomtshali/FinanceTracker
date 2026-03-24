using MediatR;

namespace FinanceTracker.Application.Features.Categories;

// ─── Queries ────────────────────────────────────────────────────

public record GetCategoryTemplatesQuery() : IRequest<List<CategoryTemplateGroupDto>>;

// ─── Commands ───────────────────────────────────────────────────

public record ApplyCategoryTemplatesCommand(
    string UserId,
    List<string> CategoryNames
) : IRequest<ApplyTemplatesResultDto>;

// ─── DTOs ───────────────────────────────────────────────────────

public record CategoryTemplateDto(
    string Name,
    string Icon,
    string Type, // Income or Expense
    string Group // grouping like "Essentials", "Lifestyle", etc.
);

public record CategoryTemplateGroupDto(
    string Group,
    List<CategoryTemplateDto> Categories
);

public record ApplyTemplatesResultDto(
    int Added,
    int Skipped,
    List<string> AddedCategories,
    List<string> SkippedCategories
);

// ─── Handler: Get Templates (pure, no DB) ───────────────────────

public class GetCategoryTemplatesHandler : IRequestHandler<GetCategoryTemplatesQuery, List<CategoryTemplateGroupDto>>
{
    public Task<List<CategoryTemplateGroupDto>> Handle(GetCategoryTemplatesQuery request, CancellationToken ct)
    {
        var templates = CategoryTemplates.GetAll();

        var grouped = templates
            .GroupBy(t => t.Group)
            .Select(g => new CategoryTemplateGroupDto(g.Key, g.ToList()))
            .ToList();

        return Task.FromResult(grouped);
    }
}

// ─── Template Data ──────────────────────────────────────────────

public static class CategoryTemplates
{
    public static List<CategoryTemplateDto> GetAll() => new()
    {
        // ── Essentials ──────────────────────────────────────
        new("Rent / Bond", "🏠", "Expense", "Essentials"),
        new("Groceries", "🛒", "Expense", "Essentials"),
        new("Electricity & Water", "💡", "Expense", "Essentials"),
        new("Transport / Fuel", "⛽", "Expense", "Essentials"),
        new("Medical / Healthcare", "🏥", "Expense", "Essentials"),
        new("Insurance", "🛡️", "Expense", "Essentials"),
        new("Phone & Internet", "📱", "Expense", "Essentials"),

        // ── Lifestyle ───────────────────────────────────────
        new("Eating Out", "🍔", "Expense", "Lifestyle"),
        new("Entertainment", "🎬", "Expense", "Lifestyle"),
        new("Clothing", "👕", "Expense", "Lifestyle"),
        new("Subscriptions", "📺", "Expense", "Lifestyle"),
        new("Personal Care", "💇", "Expense", "Lifestyle"),
        new("Gym & Fitness", "🏋️", "Expense", "Lifestyle"),
        new("Hobbies", "🎮", "Expense", "Lifestyle"),

        // ── Financial ───────────────────────────────────────
        new("Savings", "💰", "Expense", "Financial"),
        new("Investments", "📈", "Expense", "Financial"),
        new("Retirement Fund", "🏦", "Expense", "Financial"),
        new("Debt Repayment", "💳", "Expense", "Financial"),
        new("Emergency Fund", "🆘", "Expense", "Financial"),
        new("Tax", "🧾", "Expense", "Financial"),

        // ── Education & Growth ──────────────────────────────
        new("Education / Courses", "📚", "Expense", "Education & Growth"),
        new("Books", "📖", "Expense", "Education & Growth"),
        new("Software & Tools", "💻", "Expense", "Education & Growth"),

        // ── Transport ───────────────────────────────────────
        new("Uber / Taxi", "🚕", "Expense", "Transport"),
        new("Car Maintenance", "🔧", "Expense", "Transport"),
        new("Parking & Tolls", "🅿️", "Expense", "Transport"),

        // ── Home ────────────────────────────────────────────
        new("Home Maintenance", "🔨", "Expense", "Home"),
        new("Furniture", "🛋️", "Expense", "Home"),
        new("Cleaning Supplies", "🧹", "Expense", "Home"),

        // ── Giving ──────────────────────────────────────────
        new("Gifts", "🎁", "Expense", "Giving"),
        new("Donations / Charity", "❤️", "Expense", "Giving"),
        new("Church / Tithe", "⛪", "Expense", "Giving"),

        // ── Miscellaneous ───────────────────────────────────
        new("Miscellaneous", "📦", "Expense", "Other"),
        new("Bank Fees", "🏧", "Expense", "Other"),
        new("Pets", "🐕", "Expense", "Other"),
        new("Kids / Family", "👨‍👩‍👧", "Expense", "Other"),

        // ── Income ──────────────────────────────────────────
        new("Salary", "💵", "Income", "Income"),
        new("Freelance / Side Hustle", "💼", "Income", "Income"),
        new("Business Income", "🏢", "Income", "Income"),
        new("Investment Returns", "📊", "Income", "Income"),
        new("Rental Income", "🏘️", "Income", "Income"),
        new("Interest / Dividends", "🏦", "Income", "Income"),
        new("Gifts Received", "🎉", "Income", "Income"),
        new("Government Grant", "🇿🇦", "Income", "Income"),
        new("Other Income", "💸", "Income", "Income"),
    };

    // Quick preset packs
    public static List<string> StarterPack => new()
    {
        "Salary", "Groceries", "Rent / Bond", "Transport / Fuel",
        "Electricity & Water", "Eating Out", "Entertainment",
        "Savings", "Miscellaneous"
    };

    public static List<string> CompletePack => GetAll().Select(t => t.Name).ToList();

    public static List<string> FreelancerPack => new()
    {
        "Salary", "Freelance / Side Hustle", "Business Income",
        "Groceries", "Rent / Bond", "Transport / Fuel",
        "Electricity & Water", "Phone & Internet", "Software & Tools",
        "Eating Out", "Savings", "Tax", "Investments",
        "Bank Fees", "Miscellaneous"
    };

    public static List<string> StudentPack => new()
    {
        "Government Grant", "Gifts Received", "Other Income",
        "Rent / Bond", "Groceries", "Transport / Fuel",
        "Phone & Internet", "Education / Courses", "Books",
        "Eating Out", "Entertainment", "Clothing",
        "Subscriptions", "Miscellaneous"
    };
}
