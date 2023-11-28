// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Runtime.CompilerServices;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Options;

using static Microsoft.Extensions.DependencyInjection.ServiceDescriptor;

namespace Brimborium.Extensions.Logging.LocalFile;

/// <summary>
/// Extension methods for adding Azure diagnostics logger.
/// </summary>
public static class LocalFileLoggerFactoryExtensions {
    /// <summary>
    /// Adds an Azure Web Apps diagnostics logger.
    /// </summary>
    /// <param name="builder">The extension method argument</param>
    /// <returns></returns>
    public static ILoggingBuilder AddWebAppDiagnostics(
        this ILoggingBuilder builder, IConfiguration configuration) {
        var context = WebAppContext.Default;
        // Only add the provider if we're in Azure WebApp. That cannot change once the apps started
        return builder.AddWebAppDiagnostics(context, configuration, _ => { });
    }
    public static ILoggingBuilder AddWebAppDiagnostics(
        this ILoggingBuilder builder,
        IConfiguration configuration,
        Action<AzureBlobLoggerOptions> configureBlobLoggerOptions) {
        var context = WebAppContext.Default;
        // Only add the provider if we're in Azure WebApp. That cannot change once the apps started
        return builder.AddWebAppDiagnostics(context, configuration, configureBlobLoggerOptions);
    }

    /// <summary>
    /// Adds an Azure Web Apps diagnostics logger.
    /// </summary>
    /// <param name="builder">The extension method argument</param>
    /// <returns></returns>
    public static ILoggingBuilder AddWebAppDiagnostics(this ILoggingBuilder builder) {
        var context = WebAppContext.Default;

        // Only add the provider if we're in Azure WebApp. That cannot change once the apps started
        return builder.AddWebAppDiagnostics(context, "appsettings.json", _ => { });
    }

    /// <summary>
    /// Adds an Azure Web Apps diagnostics logger.
    /// </summary>
    /// <param name="builder">The extension method argument.</param>
    /// <param name="configureBlobLoggerOptions">An Action to configure the <see cref="AzureBlobLoggerOptions"/>.</param>
    /// <returns></returns>
    public static ILoggingBuilder AddWebAppDiagnostics(this ILoggingBuilder builder, string configurationFile, Action<AzureBlobLoggerOptions> configureBlobLoggerOptions) {
        var context = WebAppContext.Default;

        // Only add the provider if we're in Azure WebApp. That cannot change once the apps started
        return builder.AddWebAppDiagnostics(context, configurationFile, configureBlobLoggerOptions);
    }

    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public static ILoggingBuilder AddWebAppDiagnostics(
        this ILoggingBuilder builder,
        IWebAppContext context,
        string configurationFile,
        Action<AzureBlobLoggerOptions> configureBlobLoggerOptions) {
        builder.AddConfiguration();

        var configuration = SiteConfigurationProvider.GetAzureLoggingConfiguration(context, configurationFile);
        if (configuration is not null) {
            builder.AddWebAppDiagnostics(context, configuration, configureBlobLoggerOptions);
        }
        return builder;
    }

    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public static ILoggingBuilder AddWebAppDiagnostics(
        this ILoggingBuilder builder,
        IWebAppContext context,
        IConfiguration configuration,
        Action<AzureBlobLoggerOptions> configureBlobLoggerOptions) {
        var isRunningInAzureWebApp = context.IsRunningInAzureWebApp;

        var services = builder.Services;

        var addedAzureFileLogger = (isRunningInAzureWebApp) && TryAddEnumerable(services, Singleton<ILoggerProvider, AzureAppServicesFileLoggerProvider>());
        var addedBlobLogger = (isRunningInAzureWebApp) && TryAddEnumerable(services, Singleton<ILoggerProvider, BlobLoggerProvider>());
        var addedLocalFileLogger = TryAddEnumerable(services, Singleton<ILoggerProvider, LocalFileLoggerProvider>());

        if (addedAzureFileLogger || addedBlobLogger || addedLocalFileLogger) {
            services.AddSingleton(context);
            services.AddSingleton<IOptionsChangeTokenSource<LoggerFilterOptions>>(
                new ConfigurationChangeTokenSource<LoggerFilterOptions>(configuration));
        }

        if (addedAzureFileLogger) {
            services.AddSingleton<IConfigureOptions<LoggerFilterOptions>>(CreateAzureFileFilterConfigureOptions(configuration));
            services.AddSingleton<IConfigureOptions<AzureAppServicesFileLoggerOptions>>(new AzureAppServicesFileLoggerConfigureOptions(configuration, context));
            services.AddSingleton<IOptionsChangeTokenSource<AzureAppServicesFileLoggerOptions>>(
                new ConfigurationChangeTokenSource<AzureAppServicesFileLoggerOptions>(configuration));
            LoggerProviderOptions.RegisterProviderOptions<AzureAppServicesFileLoggerOptions, AzureAppServicesFileLoggerProvider>(builder.Services);
        }

        if (addedBlobLogger) {
            services.AddSingleton<IConfigureOptions<LoggerFilterOptions>>(CreateBlobFilterConfigureOptions(configuration));
            services.AddSingleton<IConfigureOptions<AzureBlobLoggerOptions>>(new BlobLoggerConfigureOptions(configuration, context, configureBlobLoggerOptions));
            services.AddSingleton<IOptionsChangeTokenSource<AzureBlobLoggerOptions>>(
                new ConfigurationChangeTokenSource<AzureBlobLoggerOptions>(configuration));
            LoggerProviderOptions.RegisterProviderOptions<AzureBlobLoggerOptions, BlobLoggerProvider>(builder.Services);
        }

        if (addedLocalFileLogger) {
            //if (services.Any(sd => sd.ServiceType == typeof(IConfigureOptions<LoggerFilterOptions>))) {
            //} else {
            //    services.AddSingleton<IConfigureOptions<LoggerFilterOptions>>(CreateLocalFileFilterConfigureOptions(configuration));
            //}
            services.AddSingleton<IConfigureOptions<LocalFileLoggerOptions>>(new LocalFileLoggerConfigureOptions(configuration, context));
            services.AddSingleton<IOptionsChangeTokenSource<LocalFileLoggerOptions>>(
                new ConfigurationChangeTokenSource<LocalFileLoggerOptions>(configuration));
            LoggerProviderOptions.RegisterProviderOptions<LocalFileLoggerOptions, LocalFileLoggerProvider>(builder.Services);
        }

        return builder;
    }
    public static ILoggingBuilder AddLocalFileLogger(
        this ILoggingBuilder builder,
        IConfiguration configuration) {
        var context = WebAppContext.Default;
        return AddLocalFileLogger(builder, context, configuration);
    }
    public static ILoggingBuilder AddLocalFileLogger(
        this ILoggingBuilder builder,
        WebAppContext context,
        IConfiguration configuration
        ) {
        var services = builder.Services;
        var addedLocalFileLogger = TryAddEnumerable(services, Singleton<ILoggerProvider, LocalFileLoggerProvider>());
        if (addedLocalFileLogger) {
            services.AddSingleton<IConfigureOptions<LocalFileLoggerOptions>>(new LocalFileLoggerConfigureOptions(configuration, context, true));
            services.AddSingleton<IOptionsChangeTokenSource<LocalFileLoggerOptions>>(
                new ConfigurationChangeTokenSource<LocalFileLoggerOptions>(configuration));
            LoggerProviderOptions.RegisterProviderOptions<LocalFileLoggerOptions, LocalFileLoggerProvider>(builder.Services);
        }
        return builder;
    }

    private static bool TryAddEnumerable(IServiceCollection collection, ServiceDescriptor descriptor) {
        var beforeCount = collection.Count;
        collection.TryAddEnumerable(descriptor);
        return beforeCount != collection.Count;
    }

    private static ConfigurationBasedLevelSwitcher CreateBlobFilterConfigureOptions(IConfiguration config) {
        return new ConfigurationBasedLevelSwitcher(
            configuration: config,
            provider: typeof(BlobLoggerProvider),
            levelKey: "AzureBlobTraceLevel");
    }

    private static ConfigurationBasedLevelSwitcher CreateAzureFileFilterConfigureOptions(IConfiguration config) {
        return new ConfigurationBasedLevelSwitcher(
            configuration: config,
            provider: typeof(AzureAppServicesFileLoggerProvider),
            levelKey: "AzureDriveTraceLevel");
    }

    private static ConfigurationBasedLevelSwitcher CreateLocalFileFilterConfigureOptions(IConfiguration config) {
        return new ConfigurationBasedLevelSwitcher(
            configuration: config,
            provider: typeof(LocalFileLoggerProvider),
            levelKey: "LocalFileTraceLevel");
    }
}
