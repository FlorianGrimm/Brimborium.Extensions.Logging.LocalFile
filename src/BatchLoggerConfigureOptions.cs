// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Brimborium.Extensions.Logging.LocalFile;

public class BatchLoggerConfigureOptions : IConfigureOptions<BatchingLoggerOptions> {
    protected readonly IConfiguration _Configuration;
    protected readonly string _isEnabledKey;
    protected readonly bool _AlwaysEnabled;

    public BatchLoggerConfigureOptions(IConfiguration configuration, string isEnabledKey, bool alwaysEnabled) {
        this._Configuration = configuration;
        this._isEnabledKey = isEnabledKey;
        this._AlwaysEnabled = alwaysEnabled;
    }

    public void Configure(BatchingLoggerOptions options) {
        options.IsEnabled = this._AlwaysEnabled || TextToBoolean(this._Configuration.GetSection(this._isEnabledKey)?.Value);
    }

    protected static bool TextToBoolean(string? text) {
        if (string.IsNullOrEmpty(text) ||
            !bool.TryParse(text, out var result)) {
            result = false;
        }

        return result;
    }

    protected static TimeSpan? TextToTimeSpan(string? text, TimeSpan? defaultValue = default, Func<TimeSpan?, TimeSpan?>? convert = default) {
        if (string.IsNullOrEmpty(text) ||
            !TimeSpan.TryParse(text, out var result)) {
            if (convert is not null) {
                return convert(defaultValue);
            } else {
                return defaultValue;
            }
        } else {
            if (convert is not null) {
                return convert(result);
            } else {
                return result;
            }
        }
    }

    protected static int? TextToInt(string? text, int? defaultValue = default, Func<int?, int?>? convert = default) {
        if (string.IsNullOrEmpty(text) ||
            !int.TryParse(text, out var result)) {
            if (convert is not null) {
                return convert(defaultValue);
            } else {
                return defaultValue;
            }
        } else {
            if (convert is not null) {
                return convert(result);
            } else {
                return result;
            }
        }
    }
}
