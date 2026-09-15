using Bolt.Automation.Common.Enums;
using Bolt.Automation.InternalServices.Database.Contexts;
using Bolt.Automation.InternalServices.Database.Queries.Main;
using Bolt.Automation.InternalServices.Database.Queries.Payment;
using Bolt.Automation.InternalServices.Database.Services.Connections;
using LinqToDB;
using Microsoft.Extensions.DependencyInjection;

namespace Bolt.Automation.InternalServices.Extensions
{
    public static class DatabaseExtensions
    {
        public static IServiceCollection AddDatabaseService(this IServiceCollection services)
        {
            services.AddScoped<IDbConnectionService, DbConnectionService>();

            services.AddTransient<MainDbContext>(provider =>
            {
                var dbConnectionService = provider.GetRequiredService<IDbConnectionService>();
                var connStr = dbConnectionService.GetConnectionString(DatabaseType.MainDB);
                var options = new DataOptions()
                    .UseSqlServer(connStr);
                var ctx = new MainDbContext(options);
                ctx.CommandTimeout = 90;

                return ctx;
            });

            services.AddTransient<PaymentDbContext>(provider =>
            {
                var dbConnectionService = provider.GetRequiredService<IDbConnectionService>();
                var connStr = dbConnectionService.GetConnectionString(DatabaseType.PaymentDB);
                var options = new DataOptions()
                    .UseSqlServer(connStr);
                var ctx = new PaymentDbContext(options);
                ctx.CommandTimeout = 90;

                return ctx;
            });

            services.AddScoped<IMainQueries, MainQueries>();
            services.AddScoped<IPaymentQueries, PaymentQueries>();

            return services;
        }
    }
}