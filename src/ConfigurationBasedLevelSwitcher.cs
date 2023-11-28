// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Brimborium.Extensions.Logging.LocalFile;

public class ConfigurationBasedLevelSwitcher : IConfigureOptions<LoggerFilterOptions> {
    protected readonly IConfiguration _configuration;
    protected readonly Type _provider;
    protected readonly string _levelKey;

    public ConfigurationBasedLevelSwitcher(IConfiguration configuration, Type provider, string levelKey) {
        this._configuration = configuration;
        this._provider = provider;
        this._levelKey = levelKey;
    }

    public void Configure(LoggerFilterOptions options) {
        options.Rules.Add(new LoggerFilterRule(this._provider.FullName, null, this.GetLogLevel(), null));
    }

    private LogLevel GetLogLevel() {
        return TextToLogLevel(this._configuration.GetSection(this._levelKey)?.Value);
    }

    private static LogLevel TextToLogLevel(string? text) {
        switch (text?.ToUpperInvariant()) {
            case "ERROR":
                return LogLevel.Error;
            case "WARNING":
                return LogLevel.Warning;
            case "INFORMATION":
                return LogLevel.Information;
            case "DEBUG":
                return LogLevel.Debug;
            case "VERBOSE":
            case "TRACE":
                return LogLevel.Trace;
            default:
                return LogLevel.None;
        }
    }
}
