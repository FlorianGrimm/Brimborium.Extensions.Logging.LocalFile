// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Brimborium.Extensions.Logging.LocalFile;

/// <summary>
/// Options for file logging.
/// </summary>
public class FileBasedLoggerOptions : BatchingLoggerOptions {
    private int? _fileSizeLimit = null; // 10 * 1024 * 1024;
    private int? _retainedFileCountLimit = 31;
    private string _fileName = "diagnostics-";

    /// <summary>
    /// Gets or sets a strictly positive value representing the maximum log size in bytes or null for no limit.
    /// Once the log is full, no more messages will be appended.
    /// Defaults to <c>10MB</c>.
    /// </summary>
    public int? FileSizeLimit {
        get {
            return this._fileSizeLimit;
        }
        set {
            if (value.HasValue && value <= 0) {
                throw new ArgumentOutOfRangeException(nameof(value), $"{nameof(this.FileSizeLimit)} must be positive.");
            }
            this._fileSizeLimit = value;
        }
    }

    /// <summary>
    /// Gets or sets a strictly positive value representing the maximum retained file count or null for no limit.
    /// Defaults to <c>2</c>.
    /// </summary>
    public int? RetainedFileCountLimit {
        get {
            return this._retainedFileCountLimit;
        }
        set {
            if (value.HasValue && value <= 0) {
                throw new ArgumentOutOfRangeException(nameof(value), $"{nameof(this.RetainedFileCountLimit)} must be positive.");
            }
            this._retainedFileCountLimit = value;
        }
    }

    /// <summary>
    /// Gets or sets a string representing the prefix of the file name used to store the logging information.
    /// The current date, in the format YYYYMMDD will be added after the given value.
    /// Defaults to <c>diagnostics-</c>.
    /// </summary>
    public string FileName {
        get {
            return this._fileName;
        }
        set {
            if (string.IsNullOrEmpty(value)) {
                throw new ArgumentNullException(nameof(value));
            }
            this._fileName = value;
        }
    }

    public string? LogDirectory { get; set; }
}
