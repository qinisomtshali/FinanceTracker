var builder = DistributedApplication.CreateBuilder(args);

// Add SQL Server as a managed resource — Aspire handles the container
var sqlServer = builder.AddSqlServer("sqlserver")
                       .AddDatabase("FinanceTrackerDb");

// Add your API project and wire it to the database
// Aspire automatically injects the connection string
builder.AddProject<Projects.FinanceTracker_API>("api")
       .WithReference(sqlServer)
       .WaitFor(sqlServer);

builder.Build().Run();