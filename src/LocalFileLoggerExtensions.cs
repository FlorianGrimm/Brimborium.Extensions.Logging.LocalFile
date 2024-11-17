// #define LocalFileIHostApplicationLifetime

namespace Microsoft.Extensions.Logging {
    using Brimborium.Extensions.Logging.LocalFile;

    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging.Configuration;
    using Microsoft.Extensions.Options;

    using System;

    /// <summary>
    /// Extension methods for adding Azure diagnostics logger.
    /// </summary>
    public static class LocalFileLoggerExtensions {
        public static ILoggingBuilder AddLocalFile(
            this ILoggingBuilder builder,
            IConfiguration? configuration = null,
            Action<LocalFileLoggerOptions>? configure = null
            ) {

            var services = builder.Services;
            var optionsBuilder = services.AddOptions<LocalFileLoggerOptions>();
            services.Add(ServiceDescriptor.Singleton<LocalFileLoggerProvider, LocalFileLoggerProvider>());
            services.Add(ServiceDescriptor.Singleton<ILoggerProvider>(
                static (IServiceProvider services) => {
                    return services.GetRequiredService<LocalFileLoggerProvider>();
                }));

            if (configuration is { }) {
                services.Add(ServiceDescriptor.Singleton<IConfigureOptions<LocalFileLoggerOptions>>(new LocalFileLoggerConfigureOptions(configuration)));
            } else {
                services.Add(ServiceDescriptor.Singleton<IConfigureOptions<LocalFileLoggerOptions>, LocalFileLoggerConfigureOptions>());
            }
            if (configure is { }) {
                optionsBuilder.Configure(configure);
            }

            //if (configuration is { }) {
            //    services.Add(ServiceDescriptor.Singleton<IOptionsChangeTokenSource<LocalFileLoggerOptions>>(
            //        new ConfigurationChangeTokenSource<LocalFileLoggerOptions>(configuration)));
            //}
            LoggerProviderOptions.RegisterProviderOptions<LocalFileLoggerOptions, LocalFileLoggerProvider>(builder.Services);

            return builder;
        }

        /*
        public static ILoggingBuilder AddLocalFileLogger(
           this ILoggingBuilder builder,
           IConfiguration configuration,
           Microsoft.Extensions.Hosting.IHostEnvironment hostEnvironment
           ) {
            builder.Services.AddSingleton<LocalFileLoggerProvider>();
            builder.Services
                .AddSingleton<IConfigureOptions<LocalFileLoggerOptions>>(
                    new LocalFileLoggerConfigureOptions(
                        configuration: configuration.GetSection("Logging:LocalFile"),
                        hostEnvironment: hostEnvironment
                        ))
                .AddSingleton<IOptionsChangeTokenSource<LocalFileLoggerOptions>>(
                    implementationInstance: new ConfigurationChangeTokenSource<LocalFileLoggerOptions>(configuration))
                //.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, LocalFileLoggerProvider>())
                .AddSingleton<ILoggerProvider>((sp) => sp.GetRequiredService<LocalFileLoggerProvider>());
            ;
            LoggerProviderOptions.RegisterProviderOptions<LocalFileLoggerOptions, LocalFileLoggerProvider>(builder.Services);

            return builder;
        }
        */

        /// <summary>
        /// Ensures the logs are written to disk.
        /// </summary>
        /// <param name="serviceProvider">Any serviceProvider</param>
        /// <remarks>
        /// Needed if you don't use IHostApplicationLifetime
        /// </remarks>
        public static bool FlushLocalFile(
            this IServiceProvider serviceProvider
            ) {
            if (serviceProvider.GetService<LocalFileLoggerProvider>() is { } localFileLoggerProvider) {
                localFileLoggerProvider.Flush();
                return true;
            } else {
                return false;
            }
        }
    }
}
