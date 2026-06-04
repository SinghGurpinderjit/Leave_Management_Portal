namespace UserService.Extensions
{
    using Microsoft.Extensions.Options;
    using Serilog;
    using Shared.Common.Configuration;
    using Shared.Common.Messages;
    using Shared.Common.Messaging;
    using Shared.Common.Models.EmployeeService;
    using UserService.Data;
    using UserService.Repositories.Contracts;

    public static class DatabaseExtensions
    {
        public static async Task InitializeUsersDatabase(this WebApplication app)
        {
            using var scope = app.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<EmployeeDbContext>();

            try
            {
                Log.Information("Ensuring database is created...");
                await dbContext.Database.EnsureCreatedAsync();
                Log.Information("Database initialised successfully.");

                await Seed(scope);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "An error occurred while initialising the database.");
                throw;  // let the app fail fast — silent failures hide critical startup issues
            }
        }

        private static async Task Seed(IServiceScope scope)
        {
            var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
            var roleRepository = scope.ServiceProvider.GetRequiredService<IRoleRepository>();
            var messagePublisher = scope.ServiceProvider.GetRequiredService<IMessagePublisher>();
            var settings = scope.ServiceProvider.GetRequiredService<IOptions<RabbitMQSettings>>();

            await SeedUsersAsync(userRepository, roleRepository, messagePublisher, settings.Value);
        }

        // ── Users ─────────────────────────────────────────────────────────────
        private static async Task SeedUsersAsync(
            IUserRepository userRepository,
            IRoleRepository roleRepository,
            IMessagePublisher messagePublisher,
            RabbitMQSettings settings)
        {
            var roles = await roleRepository.GetAllAsync();
            var users = await userRepository.GetAllAsync();

            Log.Information($"Roles: {roles.Count()}, Users: {users.Count()}");

            if (users.Any())
            {
                Log.Information("[Instance: {Instance}] Users already seeded — skipping.", Environment.MachineName);

                foreach (var user in users)
                {
                    await InitializeLeaveBalance(messagePublisher, settings, user);
                }
                return;
            }

            Log.Information("Seeded users data.");
        }

        private static async Task InitializeLeaveBalance(IMessagePublisher messagePublisher, RabbitMQSettings settings, User employee)
        {
            Log.Information("Publishing leave balance initialization message for user: {UserId}.", employee.Id);

            await messagePublisher.PublishAsync(new UserCreatedMessage
            {
                EmployeeId = employee.Id,
                EmployeeEmail = employee.Email,
                ManagerId = employee.ManagerId
            }, settings.RoutingKey);

            Log.Information("Published leave balance initialization message for user: {UserId}.", employee.Id);
        }
    }
}