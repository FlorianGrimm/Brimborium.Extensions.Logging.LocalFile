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

namespace Microsoft.Extensions.Logging;

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
        var optionsBuilder=services.AddOptions<LocalFileLoggerOptions>();
        services.Add(ServiceDescriptor.Singleton<LocalFileLoggerProvider, LocalFileLoggerProvider>());
        services.Add(ServiceDescriptor.Singleton<ILoggerProvider>(static (IServiceProvider services) => services.GetRequiredService<LocalFileLoggerProvider>()));

        if (configuration is { }) {
            services.Add(ServiceDescriptor.Singleton<IConfigureOptions<LocalFileLoggerOptions>>(new LocalFileLoggerConfigureOptions(configuration)));
        } else {
            services.Add(ServiceDescriptor.Singleton<IConfigureOptions<LocalFileLoggerOptions>, LocalFileLoggerConfigureOptions>());
        }
        if (configure is { }) {
            optionsBuilder.Configure(configure);
        }
        //services.Add(ServiceDescriptor.Singleton<IOptionsChangeTokenSource<LocalFileLoggerOptions>>(
        //    new ConfigurationChangeTokenSource<LocalFileLoggerOptions>(configuration)));

        LoggerProviderOptions.RegisterProviderOptions<LocalFileLoggerOptions, LocalFileLoggerProvider>(builder.Services);
        //services.TryAddEnumerable(ServiceDescriptor.Singleton<IConfigureOptions<TOptions>, LoggerProviderConfigureOptions<TOptions, TProvider>>());
        //services.TryAddEnumerable(ServiceDescriptor.Singleton<IOptionsChangeTokenSource<TOptions>, LoggerProviderOptionsChangeTokenSource<TOptions, TProvider>>());

        return builder;
    }
}
