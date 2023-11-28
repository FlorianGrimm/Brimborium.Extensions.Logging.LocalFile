// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Brimborium.Extensions.Logging.LocalFile;

/// <summary>
/// Options for local file logging.
/// </summary>
public class LocalFileLoggerOptions : FileBasedLoggerOptions {
    public LocalFileLoggerOptions() {
        this.BatchSize = null;
    }
}
