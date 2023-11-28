// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Brimborium.Extensions.Logging.LocalFile;

public class LocalFileLoggerConfigureOptions : BatchLoggerConfigureOptions, IConfigureOptions<LocalFileLoggerOptions> {
    private readonly IWebAppContext _Context;

    public LocalFileLoggerConfigureOptions(IConfiguration configuration, IWebAppContext context, bool alwaysEnabled = false)
        : base(configuration, "LocalFileEnabled", alwaysEnabled) {
        this._Context = context;
    }
    protected string? GetCfgValue(string key) {
        if (this._Configuration is IConfigurationRoot) {
            return this._Configuration.GetSection("LocalFile" + key)?.Value;
        } else {
            return this._Configuration.GetSection(key)?.Value;
        }
    }

    public void Configure(LocalFileLoggerOptions options) {
        base.Configure(options);

        options.FileSizeLimit = TextToInt(
            GetCfgValue("SizeLimit"),
            null,
            (value) => ((value.HasValue) ? value.Value * 1024 * 1024 : null)
            );
        options.RetainedFileCountLimit = TextToInt(
            GetCfgValue("RetainedFileCountLimit"),
            31,
            (value) => (value ?? 10)
            );
        options.FlushPeriod = TextToTimeSpan(GetCfgValue("FlushPeriod")).GetValueOrDefault(TimeSpan.FromSeconds(1));
        options.IncludeScopes = TextToBoolean(GetCfgValue("IncludeScopes"));
        options.TimestampFormat = GetCfgValue("TimestampFormat");
        options.UseUtcTimestamp = TextToBoolean(GetCfgValue("UseUtcTimestamp"));
        options.IncludeEventId = TextToBoolean(GetCfgValue("IncludeEventId"));
        options.UseJSONFormat = TextToBoolean(GetCfgValue("UseJSONFormat"));

        {
            var logDirectory = GetCfgValue("LogDirectory");
            if (!string.IsNullOrEmpty(logDirectory)) {
                options.LogDirectory = logDirectory;
            }
        }
        if (string.IsNullOrEmpty(options.LogDirectory)) {
            options.LogDirectory = Path.Combine(this._Context.HomeFolder ?? ".", "LogFiles", "Application");
        }
        if (!System.IO.Path.IsPathRooted(options.LogDirectory)) {
            options.LogDirectory = System.IO.Path.GetFullPath(options.LogDirectory);
        }
    }
}

