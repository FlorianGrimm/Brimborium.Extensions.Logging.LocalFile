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

using Brimborium.Extensions.Logging.LocalFile;

using static Microsoft.Extensions.DependencyInjection.ServiceDescriptor;

namespace Microsoft.Extensions.Logging;

/// <summary>
/// Extension methods for adding Azure diagnostics logger.
/// </summary>
public static class LocalFileLoggerFactoryExtensions {
    public static ILoggingBuilder AddLocalFileLogger(
        this ILoggingBuilder builder,
        IConfiguration configuration
        ) {
        var services = builder.Services;
        var addedLocalFileLogger = TryAddEnumerable(services, Singleton<ILoggerProvider, LocalFileLoggerProvider>());
        if (addedLocalFileLogger) {
            services.AddSingleton<IConfigureOptions<LocalFileLoggerOptions>>(new LocalFileLoggerConfigureOptions(configuration, true));
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
}
