namespace FinanceTracker.Domain.Enums;

public enum InvoiceStatus
{
    Draft,
    Sent,
    Paid,
    Overdue,
    Cancelled
}

public enum TaxCountry
{
    ZA, // South Africa
    US, // United States (future)
    UK  // United Kingdom (future)
}
