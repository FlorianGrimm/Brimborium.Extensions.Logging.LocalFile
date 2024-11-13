// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Brimborium.Extensions.Logging.LocalFile {
    using global::Microsoft.Extensions.Configuration;
    using global::Microsoft.Extensions.Options;
    using global::System;

    internal sealed class LocalFileLoggerConfigureOptions : IConfigureOptions<LocalFileLoggerOptions> {
        private readonly IConfiguration _configuration;
        // private readonly IHostEnvironment _HostEnvironment;

        public LocalFileLoggerConfigureOptions(
            IConfiguration configuration
            //, IHostEnvironment hostEnvironment
            ) {

            this._configuration = configuration;

            //_HostEnvironment = hostEnvironment;
        }

        public void Configure(LocalFileLoggerOptions options) {
            IConfigurationSection? configurationSection;
            if (this._configuration is IConfigurationRoot configurationRoot) {
                configurationSection = configurationRoot.GetSection("Logging:LocalFile");
            } else if (this._configuration is IConfigurationSection section) {
                configurationSection = section;
            } else {
                configurationSection = default;
            }
            if (configurationSection is { }
                && configurationSection.Exists()) {
                options.IsEnabled = TextToBoolean(configurationSection.GetSection("IsEnabled")?.Value, true);

                options.FileSizeLimit = TextToInt(
                    configurationSection.GetSection("FileSizeLimit")?.Value,
                    null,
                    (value) => ((value.HasValue) ? value.Value * 1024 * 1024 : null)
                    );
                options.RetainedFileCountLimit = TextToInt(
                    configurationSection.GetSection("FileRetainedFileCountLimit")?.Value,
                    31,
                    (value) => ((value.HasValue) ? value.Value : 10)
                    );
                options.FlushPeriod = TextToTimeSpan(
                    configurationSection.GetSection("FlushPeriod")?.Value
                    ).GetValueOrDefault(
                        TimeSpan.FromSeconds(1)
                    );
                options.IncludeScopes = TextToBoolean(configurationSection.GetSection("IncludeScopes")?.Value);
                options.TimestampFormat = configurationSection.GetSection("TimestampFormat")?.Value;
                options.UseUtcTimestamp = TextToBoolean(configurationSection.GetSection("UseUtcTimestamp")?.Value);
                options.IncludeEventId = TextToBoolean(configurationSection.GetSection("IncludeEventId")?.Value);
                options.UseJSONFormat = TextToBoolean(configurationSection.GetSection("UseJSONFormat")?.Value);



                var logDirectory = configurationSection.GetSection("Directory")?.Value ?? options.LogDirectory;
                //if (string.IsNullOrEmpty(logDirectory)) {
                //    logDirectory = Path.Combine(_HostEnvironment.ContentRootPath ?? ".", "LogFiles");
                //}
                //if (string.IsNullOrEmpty(logDirectory)) {
                //    logDirectory = System.Environment.GetEnvironmentVariable("TEMP");
                //}
                if (logDirectory is { Length: > 0 } && logDirectory.Contains("%")) {
                    logDirectory = System.Environment.ExpandEnvironmentVariables(logDirectory);
                }
                if (logDirectory is { Length: > 0 } && !System.IO.Path.IsPathRooted(logDirectory)) {
                    string? baseDirectory = default;
                    if (options.BaseDirectory is { Length: > 0 }) { baseDirectory = options.BaseDirectory; }
                    if (string.IsNullOrEmpty(baseDirectory)) { options.BaseDirectory = baseDirectory = System.AppContext.BaseDirectory; }
                    logDirectory = System.IO.Path.Combine(baseDirectory, logDirectory);
                }
                if (logDirectory is { Length: > 0 } && !System.IO.Path.IsPathRooted(logDirectory)) {
                    logDirectory = System.IO.Path.GetFullPath(logDirectory);
                }
                options.LogDirectory = logDirectory;
            } else {
                options.IsEnabled = false;
            }
        }

        private static bool TextToBoolean(string? text, bool defaultValue = false)
            => string.IsNullOrEmpty(text) || !bool.TryParse(text, out var result)
                ? defaultValue
                : result;

        private static TimeSpan? TextToTimeSpan(string? text, TimeSpan? defaultValue = default, Func<TimeSpan?, TimeSpan?>? convert = default)
            => string.IsNullOrEmpty(text) || !TimeSpan.TryParse(text, out var result)
                ? convert is null ? defaultValue : convert(defaultValue)
                : convert is null ? result : convert(result);

        private static int? TextToInt(string? text, int? defaultValue = default, Func<int?, int?>? convert = default)
            => string.IsNullOrEmpty(text) || !int.TryParse(text, out var result)
                ? convert is null ? defaultValue : convert(defaultValue)
                : convert is null ? result : convert(result);
    }
}
