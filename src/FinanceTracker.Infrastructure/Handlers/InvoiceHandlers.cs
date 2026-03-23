using MediatR;
using Microsoft.EntityFrameworkCore;
using FinanceTracker.Application.Features.Invoices;
using FinanceTracker.Domain.Entities;
using FinanceTracker.Infrastructure.Data;

namespace FinanceTracker.Infrastructure.Handlers;

// ─── Create Invoice ─────────────────────────────────────────────

public class CreateInvoiceHandler : IRequestHandler<CreateInvoiceCommand, InvoiceDto>
{
    private readonly ApplicationDbContext _db;
    public CreateInvoiceHandler(ApplicationDbContext db) => _db = db;

    public async Task<InvoiceDto> Handle(CreateInvoiceCommand request, CancellationToken ct)
    {
        // Auto-generate invoice number if not provided
        var invoiceNumber = request.InvoiceNumber;
        if (string.IsNullOrWhiteSpace(invoiceNumber))
        {
            var count = await _db.Invoices.CountAsync(i => i.UserId == request.UserId, ct);
            invoiceNumber = $"INV-{DateTime.UtcNow:yyyy}-{(count + 1):D3}";
        }

        // Build line items
        var lineItems = request.LineItems.Select(li => new InvoiceLineItem
        {
            Description = li.Description,
            Quantity = li.Quantity,
            UnitPrice = li.UnitPrice,
            LineTotal = Math.Round(li.Quantity * li.UnitPrice, 2),
            SortOrder = li.SortOrder
        }).ToList();

        // Calculate totals
        var subtotal = lineItems.Sum(li => li.LineTotal);
        var discountAmount = request.DiscountPercentage.HasValue
            ? Math.Round(subtotal * request.DiscountPercentage.Value / 100, 2)
            : 0m;
        var afterDiscount = subtotal - discountAmount;
        var vatAmount = Math.Round(afterDiscount * request.VatRate / 100, 2);
        var total = afterDiscount + vatAmount;

        var invoice = new Invoice
        {
            UserId = request.UserId,
            InvoiceNumber = invoiceNumber,
            Status = "Draft",
            FromName = request.FromName,
            FromEmail = request.FromEmail,
            FromAddress = request.FromAddress,
            FromVatNumber = request.FromVatNumber,
            ToName = request.ToName,
            ToEmail = request.ToEmail,
            ToAddress = request.ToAddress,
            ToVatNumber = request.ToVatNumber,
            Currency = request.Currency,
            Subtotal = subtotal,
            VatRate = request.VatRate,
            VatAmount = vatAmount,
            Total = total,
            DiscountPercentage = request.DiscountPercentage,
            DiscountAmount = discountAmount > 0 ? discountAmount : null,
            IssueDate = request.IssueDate ?? DateTime.UtcNow,
            DueDate = request.DueDate,
            BankName = request.BankName,
            AccountHolder = request.AccountHolder,
            AccountNumber = request.AccountNumber,
            BranchCode = request.BranchCode,
            Reference = request.Reference,
            Notes = request.Notes,
            LineItems = lineItems
        };

        _db.Invoices.Add(invoice);
        await _db.SaveChangesAsync(ct);

        return MapToDto(invoice);
    }

    private static InvoiceDto MapToDto(Invoice inv) => new(
        Id: inv.Id,
        InvoiceNumber: inv.InvoiceNumber,
        Status: inv.Status,
        FromName: inv.FromName,
        FromEmail: inv.FromEmail,
        FromAddress: inv.FromAddress,
        FromVatNumber: inv.FromVatNumber,
        ToName: inv.ToName,
        ToEmail: inv.ToEmail,
        ToAddress: inv.ToAddress,
        ToVatNumber: inv.ToVatNumber,
        Currency: inv.Currency,
        Subtotal: inv.Subtotal,
        VatRate: inv.VatRate,
        VatAmount: inv.VatAmount,
        Total: inv.Total,
        DiscountPercentage: inv.DiscountPercentage,
        DiscountAmount: inv.DiscountAmount,
        IssueDate: inv.IssueDate,
        DueDate: inv.DueDate,
        PaidDate: inv.PaidDate,
        BankName: inv.BankName,
        AccountHolder: inv.AccountHolder,
        AccountNumber: inv.AccountNumber,
        BranchCode: inv.BranchCode,
        Reference: inv.Reference,
        Notes: inv.Notes,
        LineItems: inv.LineItems.OrderBy(li => li.SortOrder).Select(li => new InvoiceLineItemDto(
            li.Id, li.Description, li.Quantity, li.UnitPrice, li.LineTotal, li.SortOrder
        )).ToList(),
        CreatedAt: inv.CreatedAt,
        UpdatedAt: inv.UpdatedAt
    );
}

// ─── Get Invoice By ID ──────────────────────────────────────────

public class GetInvoiceByIdHandler : IRequestHandler<GetInvoiceByIdQuery, InvoiceDto?>
{
    private readonly ApplicationDbContext _db;
    public GetInvoiceByIdHandler(ApplicationDbContext db) => _db = db;

    public async Task<InvoiceDto?> Handle(GetInvoiceByIdQuery request, CancellationToken ct)
    {
        var inv = await _db.Invoices
            .Include(i => i.LineItems)
            .FirstOrDefaultAsync(i => i.Id == request.InvoiceId && i.UserId == request.UserId, ct);

        if (inv == null) return null;

        return new InvoiceDto(
            inv.Id, inv.InvoiceNumber, inv.Status,
            inv.FromName, inv.FromEmail, inv.FromAddress, inv.FromVatNumber,
            inv.ToName, inv.ToEmail, inv.ToAddress, inv.ToVatNumber,
            inv.Currency, inv.Subtotal, inv.VatRate, inv.VatAmount, inv.Total,
            inv.DiscountPercentage, inv.DiscountAmount,
            inv.IssueDate, inv.DueDate, inv.PaidDate,
            inv.BankName, inv.AccountHolder, inv.AccountNumber, inv.BranchCode, inv.Reference, inv.Notes,
            inv.LineItems.OrderBy(li => li.SortOrder).Select(li => new InvoiceLineItemDto(
                li.Id, li.Description, li.Quantity, li.UnitPrice, li.LineTotal, li.SortOrder
            )).ToList(),
            inv.CreatedAt, inv.UpdatedAt
        );
    }
}

// ─── Get Invoices (Paged) ───────────────────────────────────────

public class GetInvoicesHandler : IRequestHandler<GetInvoicesQuery, PagedInvoiceResultDto>
{
    private readonly ApplicationDbContext _db;
    public GetInvoicesHandler(ApplicationDbContext db) => _db = db;

    public async Task<PagedInvoiceResultDto> Handle(GetInvoicesQuery request, CancellationToken ct)
    {
        var query = _db.Invoices
            .Where(i => i.UserId == request.UserId);

        if (!string.IsNullOrWhiteSpace(request.Status))
            query = query.Where(i => i.Status == request.Status);

        var totalCount = await query.CountAsync(ct);
        var totalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize);

        var invoices = await query
            .OrderByDescending(i => i.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Include(i => i.LineItems)
            .ToListAsync(ct);

        var items = invoices.Select(inv => new InvoiceDto(
            inv.Id, inv.InvoiceNumber, inv.Status,
            inv.FromName, inv.FromEmail, inv.FromAddress, inv.FromVatNumber,
            inv.ToName, inv.ToEmail, inv.ToAddress, inv.ToVatNumber,
            inv.Currency, inv.Subtotal, inv.VatRate, inv.VatAmount, inv.Total,
            inv.DiscountPercentage, inv.DiscountAmount,
            inv.IssueDate, inv.DueDate, inv.PaidDate,
            inv.BankName, inv.AccountHolder, inv.AccountNumber, inv.BranchCode, inv.Reference, inv.Notes,
            inv.LineItems.OrderBy(li => li.SortOrder).Select(li => new InvoiceLineItemDto(
                li.Id, li.Description, li.Quantity, li.UnitPrice, li.LineTotal, li.SortOrder
            )).ToList(),
            inv.CreatedAt, inv.UpdatedAt
        )).ToList();

        return new PagedInvoiceResultDto(items, totalCount, request.Page, request.PageSize, totalPages);
    }
}

// ─── Get Invoice Summary ────────────────────────────────────────

public class GetInvoiceSummaryHandler : IRequestHandler<GetInvoiceSummaryQuery, InvoiceSummaryDto>
{
    private readonly ApplicationDbContext _db;
    public GetInvoiceSummaryHandler(ApplicationDbContext db) => _db = db;

    public async Task<InvoiceSummaryDto> Handle(GetInvoiceSummaryQuery request, CancellationToken ct)
    {
        var invoices = await _db.Invoices
            .Where(i => i.UserId == request.UserId)
            .ToListAsync(ct);

        return new InvoiceSummaryDto(
            TotalInvoices: invoices.Count,
            DraftCount: invoices.Count(i => i.Status == "Draft"),
            SentCount: invoices.Count(i => i.Status == "Sent"),
            PaidCount: invoices.Count(i => i.Status == "Paid"),
            OverdueCount: invoices.Count(i => i.Status == "Overdue"),
            TotalOutstanding: invoices.Where(i => i.Status == "Sent" || i.Status == "Overdue").Sum(i => i.Total),
            TotalPaid: invoices.Where(i => i.Status == "Paid").Sum(i => i.Total),
            TotalRevenue: invoices.Sum(i => i.Total)
        );
    }
}

// ─── Update Invoice Status ──────────────────────────────────────

public class UpdateInvoiceStatusHandler : IRequestHandler<UpdateInvoiceStatusCommand, bool>
{
    private readonly ApplicationDbContext _db;
    public UpdateInvoiceStatusHandler(ApplicationDbContext db) => _db = db;

    public async Task<bool> Handle(UpdateInvoiceStatusCommand request, CancellationToken ct)
    {
        var invoice = await _db.Invoices
            .FirstOrDefaultAsync(i => i.Id == request.InvoiceId && i.UserId == request.UserId, ct);

        if (invoice == null) return false;

        invoice.Status = request.Status;
        if (request.Status == "Paid")
            invoice.PaidDate = request.PaidDate ?? DateTime.UtcNow;
        invoice.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        return true;
    }
}

// ─── Delete Invoice ─────────────────────────────────────────────

public class DeleteInvoiceHandler : IRequestHandler<DeleteInvoiceCommand, bool>
{
    private readonly ApplicationDbContext _db;
    public DeleteInvoiceHandler(ApplicationDbContext db) => _db = db;

    public async Task<bool> Handle(DeleteInvoiceCommand request, CancellationToken ct)
    {
        var invoice = await _db.Invoices
            .FirstOrDefaultAsync(i => i.Id == request.InvoiceId && i.UserId == request.UserId, ct);

        if (invoice == null) return false;

        _db.Invoices.Remove(invoice);
        await _db.SaveChangesAsync(ct);
        return true;
    }
}

// ─── Duplicate Invoice ──────────────────────────────────────────

public class DuplicateInvoiceHandler : IRequestHandler<DuplicateInvoiceCommand, InvoiceDto>
{
    private readonly ApplicationDbContext _db;
    public DuplicateInvoiceHandler(ApplicationDbContext db) => _db = db;

    public async Task<InvoiceDto> Handle(DuplicateInvoiceCommand request, CancellationToken ct)
    {
        var source = await _db.Invoices
            .Include(i => i.LineItems)
            .FirstOrDefaultAsync(i => i.Id == request.SourceInvoiceId && i.UserId == request.UserId, ct);

        if (source == null)
            throw new InvalidOperationException("Invoice not found");

        var count = await _db.Invoices.CountAsync(i => i.UserId == request.UserId, ct);
        var newInvoice = new Invoice
        {
            UserId = request.UserId,
            InvoiceNumber = $"INV-{DateTime.UtcNow:yyyy}-{(count + 1):D3}",
            Status = "Draft",
            FromName = source.FromName,
            FromEmail = source.FromEmail,
            FromAddress = source.FromAddress,
            FromVatNumber = source.FromVatNumber,
            ToName = source.ToName,
            ToEmail = source.ToEmail,
            ToAddress = source.ToAddress,
            ToVatNumber = source.ToVatNumber,
            Currency = source.Currency,
            Subtotal = source.Subtotal,
            VatRate = source.VatRate,
            VatAmount = source.VatAmount,
            Total = source.Total,
            DiscountPercentage = source.DiscountPercentage,
            DiscountAmount = source.DiscountAmount,
            IssueDate = DateTime.UtcNow,
            DueDate = DateTime.UtcNow.AddDays(30),
            BankName = source.BankName,
            AccountHolder = source.AccountHolder,
            AccountNumber = source.AccountNumber,
            BranchCode = source.BranchCode,
            Reference = source.Reference,
            Notes = source.Notes,
            LineItems = source.LineItems.Select(li => new InvoiceLineItem
            {
                Description = li.Description,
                Quantity = li.Quantity,
                UnitPrice = li.UnitPrice,
                LineTotal = li.LineTotal,
                SortOrder = li.SortOrder
            }).ToList()
        };

        _db.Invoices.Add(newInvoice);
        await _db.SaveChangesAsync(ct);

        return new InvoiceDto(
            newInvoice.Id, newInvoice.InvoiceNumber, newInvoice.Status,
            newInvoice.FromName, newInvoice.FromEmail, newInvoice.FromAddress, newInvoice.FromVatNumber,
            newInvoice.ToName, newInvoice.ToEmail, newInvoice.ToAddress, newInvoice.ToVatNumber,
            newInvoice.Currency, newInvoice.Subtotal, newInvoice.VatRate, newInvoice.VatAmount, newInvoice.Total,
            newInvoice.DiscountPercentage, newInvoice.DiscountAmount,
            newInvoice.IssueDate, newInvoice.DueDate, newInvoice.PaidDate,
            newInvoice.BankName, newInvoice.AccountHolder, newInvoice.AccountNumber, newInvoice.BranchCode, newInvoice.Reference, newInvoice.Notes,
            newInvoice.LineItems.OrderBy(li => li.SortOrder).Select(li => new InvoiceLineItemDto(
                li.Id, li.Description, li.Quantity, li.UnitPrice, li.LineTotal, li.SortOrder
            )).ToList(),
            newInvoice.CreatedAt, newInvoice.UpdatedAt
        );
    }
}
