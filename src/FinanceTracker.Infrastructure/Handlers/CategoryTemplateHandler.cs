using MediatR;
using Microsoft.EntityFrameworkCore;
using FinanceTracker.Application.Features.Categories;
using FinanceTracker.Domain.Entities;
using FinanceTracker.Domain.Enums;
using FinanceTracker.Infrastructure.Data;

namespace FinanceTracker.Infrastructure.Handlers;

public class ApplyCategoryTemplatesHandler : IRequestHandler<ApplyCategoryTemplatesCommand, ApplyTemplatesResultDto>
{
    private readonly ApplicationDbContext _db;
    public ApplyCategoryTemplatesHandler(ApplicationDbContext db) => _db = db;

    public async Task<ApplyTemplatesResultDto> Handle(ApplyCategoryTemplatesCommand request, CancellationToken ct)
    {
        var userId = Guid.Parse(request.UserId);
        var allTemplates = CategoryTemplates.GetAll();

        // Get user's existing category names (case-insensitive)
        var existingNames = await _db.Categories
            .Where(c => c.UserId == userId)
            .Select(c => c.Name.ToLower())
            .ToListAsync(ct);

        var added = new List<string>();
        var skipped = new List<string>();

        foreach (var name in request.CategoryNames)
        {
            // Find the template
            var template = allTemplates.FirstOrDefault(t =>
                t.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

            if (template == null)
            {
                skipped.Add(name);
                continue;
            }

            // Skip if user already has this category
            if (existingNames.Contains(template.Name.ToLower()))
            {
                skipped.Add(template.Name);
                continue;
            }

            var category = new Category
            {
                Name = template.Name,
                Type = Enum.Parse<TransactionType>(template.Type),
                UserId = userId
            };

            _db.Categories.Add(category);
            added.Add(template.Name);
            existingNames.Add(template.Name.ToLower()); // prevent duplicates within batch
        }

        if (added.Count > 0)
            await _db.SaveChangesAsync(ct);

        return new ApplyTemplatesResultDto(
            Added: added.Count,
            Skipped: skipped.Count,
            AddedCategories: added,
            SkippedCategories: skipped
        );
    }
}
