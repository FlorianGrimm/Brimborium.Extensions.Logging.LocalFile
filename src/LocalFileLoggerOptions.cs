// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Brimborium.Extensions.Logging.LocalFile {
    using global::System;
    using global::System.Text.Json;

    /// <summary>
    /// Options for local file logging.
    /// </summary>
    public sealed class LocalFileLoggerOptions {
        private int? _batchSize = null;
        private int? _backgroundQueueSize; // = 1000;
        private TimeSpan _flushPeriod = TimeSpan.FromSeconds(1);
        private int? _fileSizeLimit = null; // 10 * 1024 * 1024;
        private int? _retainedFileCountLimit = 31;
        private string _fileName = "diagnostics-";

        /// <summary>
        /// Gets or sets a strictly positive value representing the maximum log size in bytes or null for no limit.
        /// Once the log is full, no more messages will be appended.
        /// Defaults to <c>10MB</c>.
        /// </summary>
        public int? FileSizeLimit {
            get => this._fileSizeLimit;
            set => this._fileSizeLimit = !value.HasValue || !(value <= 0)
                    ? value
                    : throw new ArgumentOutOfRangeException(nameof(value), $"{nameof(this.FileSizeLimit)} must be positive.");
        }

        /// <summary>
        /// Gets or sets a strictly positive value representing the maximum retained file count or null for no limit.
        /// Defaults to <c>2</c>.
        /// </summary>
        public int? RetainedFileCountLimit {
            get => this._retainedFileCountLimit;
            set => this._retainedFileCountLimit = !value.HasValue || !(value <= 0)
                    ? value
                    : throw new ArgumentOutOfRangeException(nameof(value), $"{nameof(this.RetainedFileCountLimit)} must be positive.");
        }

        /// <summary>
        /// Gets or sets a string representing the prefix of the file name used to store the logging information.
        /// The current date, in the format YYYYMMDD will be added after the given value.
        /// Defaults to <c>diagnostics-</c>.
        /// </summary>
        public string FileName {
            get => this._fileName;
            set => this._fileName = !string.IsNullOrEmpty(value)
                    ? value
                    : throw new ArgumentNullException(nameof(value));
        }

        public string? BaseDirectory { get; set; }

        public string? LogDirectory { get; set; } = "LogFiles/Application";


        /// <summary>
        /// Gets or sets the period after which logs will be flushed to the store.
        /// </summary>
        public TimeSpan FlushPeriod {
            get => this._flushPeriod;
            set => this._flushPeriod = (value > TimeSpan.Zero)
                ? value
                : throw new ArgumentOutOfRangeException(nameof(value), $"{nameof(this.FlushPeriod)} must be positive.");
        }

        /// <summary>
        /// Gets or sets the maximum size of the background log message queue or null for no limit.
        /// After maximum queue size is reached log event sink would start blocking.
        /// Defaults to <c>1000</c>.
        /// </summary>
        public int? BackgroundQueueSize {
            get => this._backgroundQueueSize;
            set => this._backgroundQueueSize = !value.HasValue || value.Value < 0 ? null : value;
        }

        /// <summary>
        /// Gets or sets a maximum number of events to include in a single batch or null for no limit.
        /// </summary>
        /// Defaults to <c>null</c>.
        public int? BatchSize {
            get => this._batchSize;
            set => this._batchSize = !value.HasValue || value.Value < 0 ? null : value;
        }

        /// <summary>
        /// Gets or sets value indicating if logger accepts and queues writes.
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether scopes should be included in the message.
        /// Defaults to <c>false</c>.
        /// </summary>
        public bool IncludeScopes { get; set; }

        /// <summary>
        /// Gets or sets format string used to format timestamp in logging messages. Defaults to <c>null</c>.
        /// </summary>
        //[StringSyntax(StringSyntaxAttribute.DateTimeFormat)] dotnet 7?
        public string? TimestampFormat { get; set; }

        /// <summary>
        /// Gets or sets indication whether or not UTC timezone should be used to format timestamps in logging messages. Defaults to <c>false</c>.
        /// </summary>
        public bool UseUtcTimestamp { get; set; }

        public bool IncludeEventId { get; set; }

        public bool UseJSONFormat { get; set; }

        /// <summary>
        /// Gets or sets JsonWriterOptions.
        /// </summary>
        public JsonWriterOptions JsonWriterOptions { get; set; }
    }
}
