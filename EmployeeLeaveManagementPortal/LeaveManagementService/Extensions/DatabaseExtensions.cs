using LeaveManagementService.Data;
using Serilog;

namespace LeaveManagementService.Extensions;

public static class DatabaseExtensions
{
    public static void InitializeLeavesMgmtDatabase(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<LeavesMgmtDbContext>();

        try
        {
            Log.Information("Ensuring Database is created...");

            dbContext.Database.EnsureCreated();

            Log.Information("Database created successfully");

            // Seed data if not exists
        }
        catch (Exception ex)
        {
            Log.Error(ex, "failed to initlalize database");
            throw;
        }
    }
}
