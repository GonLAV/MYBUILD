using Bolt.Automation.Common.Logging.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Bolt.Automation.Common.Logging.Mongo;

public static class MongoReportingExtensions
{
    public static IServiceCollection AddMongoReporting(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<MongoReportingOptions>(configuration.GetSection(MongoReportingOptions.SectionName));

        services.AddSingleton<IMongoContext>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<MongoReportingOptions>>();
            return new MongoContext(options);
        });

        // S3 artifact uploader
        services.AddSingleton<IS3ArtifactUploader>(sp =>
        {
            var awsSection = configuration.GetSection(AwsOptions.SectionName);
            if (!awsSection.Exists())
                return new NoOpS3ArtifactUploader();

            var awsOptions = new AwsOptions();
            awsSection.Bind(awsOptions);

            if (string.IsNullOrEmpty(awsOptions.AccessKey) || string.IsNullOrEmpty(awsOptions.S3.BucketName))
                return new NoOpS3ArtifactUploader();

            return new S3ArtifactUploader(awsOptions);
        });

        services.AddSingleton<ITestRunWriter>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<MongoReportingOptions>>();
            if (!options.Value.Enabled)
                return new NoOpTestRunWriter();

            return new TestRunWriter(
                sp.GetRequiredService<IMongoContext>(),
                options,
                sp.GetRequiredService<IS3ArtifactUploader>());
        });

        // Decorate IAutomationLogger: replace the existing registration with a
        // MongoLoggingDecorator that wraps the original logger.  When reporting is
        // disabled the raw logger is returned unchanged.
        var originalDescriptor = services.LastOrDefault(d => d.ServiceType == typeof(IAutomationLogger));
        if (originalDescriptor != null)
        {
            services.Remove(originalDescriptor);

            services.AddSingleton<IAutomationLogger>(sp =>
            {
                // Build the original logger the same way it was registered
                IAutomationLogger inner = originalDescriptor.ImplementationFactory != null
                    ? (IAutomationLogger)originalDescriptor.ImplementationFactory(sp)
                    : (IAutomationLogger)ActivatorUtilities.CreateInstance(sp,
                        originalDescriptor.ImplementationType!);

                var writer = sp.GetRequiredService<ITestRunWriter>();
                if (writer is NoOpTestRunWriter)
                    return inner;

                return new MongoLoggingDecorator(inner, writer);
            });
        }

        return services;
    }
}
