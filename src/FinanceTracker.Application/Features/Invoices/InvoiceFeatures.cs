using MediatR;

namespace FinanceTracker.Application.Features.Invoices;

// ─── Queries ────────────────────────────────────────────────────

public record GetInvoiceByIdQuery(string UserId, Guid InvoiceId) : IRequest<InvoiceDto?>;

public record GetInvoicesQuery(
    string UserId,
    string? Status = null,
    int Page = 1,
    int PageSize = 20
) : IRequest<PagedInvoiceResultDto>;

public record GetInvoiceSummaryQuery(string UserId) : IRequest<InvoiceSummaryDto>;

// ─── Commands ───────────────────────────────────────────────────

public record CreateInvoiceCommand(
    string UserId,
    string? InvoiceNumber,
    // Sender
    string FromName,
    string? FromEmail,
    string? FromAddress,
    string? FromVatNumber,
    // Recipient
    string ToName,
    string? ToEmail,
    string? ToAddress,
    string? ToVatNumber,
    // Financials
    string Currency,
    decimal VatRate,
    decimal? DiscountPercentage,
    // Dates
    DateTime? IssueDate,
    DateTime DueDate,
    // Banking
    string? BankName,
    string? AccountHolder,
    string? AccountNumber,
    string? BranchCode,
    string? Reference,
    string? Notes,
    // Line Items
    List<CreateLineItemDto> LineItems
) : IRequest<InvoiceDto>;

public record UpdateInvoiceStatusCommand(
    string UserId,
    Guid InvoiceId,
    string Status, // Draft, Sent, Paid, Overdue, Cancelled
    DateTime? PaidDate = null
) : IRequest<bool>;

public record DeleteInvoiceCommand(string UserId, Guid InvoiceId) : IRequest<bool>;

public record DuplicateInvoiceCommand(string UserId, Guid SourceInvoiceId) : IRequest<InvoiceDto>;

// ─── DTOs ───────────────────────────────────────────────────────

public record CreateLineItemDto(
    string Description,
    decimal Quantity,
    decimal UnitPrice,
    int SortOrder
);

public record InvoiceLineItemDto(
    Guid Id,
    string Description,
    decimal Quantity,
    decimal UnitPrice,
    decimal LineTotal,
    int SortOrder
);

public record InvoiceDto(
    Guid Id,
    string InvoiceNumber,
    string Status,
    // Sender
    string FromName,
    string? FromEmail,
    string? FromAddress,
    string? FromVatNumber,
    // Recipient
    string ToName,
    string? ToEmail,
    string? ToAddress,
    string? ToVatNumber,
    // Financials
    string Currency,
    decimal Subtotal,
    decimal VatRate,
    decimal VatAmount,
    decimal Total,
    decimal? DiscountPercentage,
    decimal? DiscountAmount,
    // Dates
    DateTime IssueDate,
    DateTime DueDate,
    DateTime? PaidDate,
    // Banking
    string? BankName,
    string? AccountHolder,
    string? AccountNumber,
    string? BranchCode,
    string? Reference,
    string? Notes,
    // Line Items
    List<InvoiceLineItemDto> LineItems,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

public record PagedInvoiceResultDto(
    List<InvoiceDto> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages
);

public record InvoiceSummaryDto(
    int TotalInvoices,
    int DraftCount,
    int SentCount,
    int PaidCount,
    int OverdueCount,
    decimal TotalOutstanding,
    decimal TotalPaid,
    decimal TotalRevenue
);
