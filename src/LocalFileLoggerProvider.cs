#pragma warning disable IDE0058 // Expression value is never used
#pragma warning disable IDE0079 // Remove unnecessary suppression

namespace Brimborium.Extensions.Logging.LocalFile {
    using global::Microsoft.Extensions.Logging;
    using global::Microsoft.Extensions.Options;
    using global::System;
    using global::System.Collections.Concurrent;
    using global::System.Collections.Generic;
    using global::System.IO;
    using global::System.Linq;
    using global::System.Text.Json;
    using global::System.Threading;
    using global::System.Threading.Tasks;

#if LocalFileIHostApplicationLifetime
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
#endif

    [ProviderAlias("LocalFile")]
    public sealed partial class LocalFileLoggerProvider
        : ILoggerProvider, ISupportExternalScope {
        // values from the options
        private readonly string? _path;
        private DateTime _nextCheckPath;
        private DateTime _nextCheckRollFiles;
        private readonly bool _isPathValid;
        private readonly string _fileName;
        private readonly int? _maxFileSize;
        private readonly int? _maxRetainedFiles;
        private readonly string? _newLineReplacement;
        private readonly TimeSpan _interval;
        private readonly int? _queueSize;
        private readonly int? _batchSize;
        private readonly TimeSpan _flushPeriod;
        private bool _IsEnabled;

        // changes
        private IDisposable? _optionsChangeToken;

        // message sink 
        private CancellationTokenSource? _stopTokenSource;
        private BlockingCollection<LogMessage>? _messageQueue;
        private List<LogMessage> _currentBatchPool = new(1024);
        private int _messagesDropped;

        // loop
        private Task? _outputTask;

        // handle cool down
        private readonly SemaphoreSlim _semaphoreProcessMessageQueueWrite = new(1, 1);
        private readonly SemaphoreSlim _semaphoreProcessMessageQueueIdle = new(1, 1);
        private const long _processMessageQueueWatchDogReset = 10;
        private long _processMessageQueueWatchDog = _processMessageQueueWatchDogReset;

        private IExternalScopeProvider? _scopeProvider;
        private int _workingState;

        /// <summary>
        /// Creates a new instance of <see cref="LocalFileLoggerProvider"/>.
        /// </summary>
        /// <param name="options">The options to use when creating a provider.</param>
        public LocalFileLoggerProvider(
            IOptionsMonitor<LocalFileLoggerOptions> options) {
            var loggerOptions = options.CurrentValue;
            if (loggerOptions.BatchSize <= 0) {
                throw new ArgumentOutOfRangeException("loggerOptions.BatchSize", $"{nameof(loggerOptions.BatchSize)} must be a positive number.");
            }
            if (loggerOptions.FlushPeriod <= TimeSpan.Zero) {
                throw new ArgumentOutOfRangeException("loggerOptions.FlushPeriod", $"{nameof(loggerOptions.FlushPeriod)} must be longer than zero.");
            }
            {
                if (loggerOptions.LogDirectory is { Length: > 0 } logDirectory) {
                    string? path = default;
                    if (loggerOptions.BaseDirectory is { Length: > 0 } baseDirectory) {
                        path = System.IO.Path.Combine(baseDirectory, logDirectory);
                    } else if (loggerOptions.LogDirectory is { Length: > 0 }) {
                        path = logDirectory;
                    }
                    if (path is { Length: > 0 }
                        && System.IO.Path.IsPathRooted(path)) {
                        this._path = path;
                        this._isPathValid = true;
                    }
                }

                this._nextCheckPath = DateTime.MinValue;
                this._nextCheckRollFiles = DateTime.MinValue;

                this._fileName = loggerOptions.FileName;
                this._maxFileSize = loggerOptions.FileSizeLimit;
                this._maxRetainedFiles = loggerOptions.RetainedFileCountLimit;
                if (loggerOptions.NewLineReplacement is { Length: 4 } newLineReplacement) {
                    this._newLineReplacement = loggerOptions.NewLineReplacement;
                } else {
                    this._newLineReplacement = null;
                }

                this._interval = loggerOptions.FlushPeriod;
                this._batchSize = loggerOptions.BatchSize;
                this._queueSize = loggerOptions.BackgroundQueueSize;
                this._flushPeriod = loggerOptions.FlushPeriod;
            }
            this._optionsChangeToken = options.OnChange(this.UpdateOptions);
            this.UpdateOptions(options.CurrentValue);
        }

        internal IExternalScopeProvider? ScopeProvider => this.IncludeScopes ? this._scopeProvider : null;

        internal bool IncludeScopes { get; private set; }

        internal bool IsEnabled => this._IsEnabled && this._isPathValid;

        internal bool UseJSONFormat { get; private set; }

        internal bool IncludeEventId { get; private set; }

        internal string? NewLineReplacement => this._newLineReplacement;

        public JsonWriterOptions JsonWriterOptions { get; private set; }

        /// <summary>
        /// Gets or sets format string used to format timestamp in logging messages. Defaults to <c>null</c>.
        /// </summary>
        //[StringSyntax(StringSyntaxAttribute.DateTimeFormat)]
        public string? TimestampFormat { get; set; }

        /// <summary>
        /// Gets or sets indication whether or not UTC time zone should be used to format timestamps in logging messages. Defaults to <c>false</c>.
        /// </summary>
        public bool UseUtcTimestamp { get; set; }

        private void UpdateOptions(LocalFileLoggerOptions options) {
            this._IsEnabled = options.IsEnabled;
            this.UseJSONFormat = options.UseJSONFormat;
            this.TimestampFormat = options.TimestampFormat;
            this.UseUtcTimestamp = options.UseUtcTimestamp;
            this.IncludeEventId = options.IncludeEventId;
            this.JsonWriterOptions = options.JsonWriterOptions;

            this.IncludeScopes = options.IncludeScopes;
        }


        // LocalFileLogger will call this
        internal void AddMessage(DateTimeOffset timestamp, string message) {
            if (!this.IsEnabled) {
                return;
            }

            if (this.EnsureMessageQueue(out var messageQueue, out _)
                || (this._workingState <= 0)) {
                // The first time AddMessage is called EnsureMessageQueue will return true since the _messageQueue was created.
                this.Start();
            }

            if (!messageQueue.IsAddingCompleted) {
                try {
                    if (!messageQueue.TryAdd(
                       item: new LogMessage(timestamp, message),
                        millisecondsTimeout: 0,
                        cancellationToken: (this._stopTokenSource is null)
                            ? CancellationToken.None
                            : this._stopTokenSource.Token)) {
                        _ = System.Threading.Interlocked.Increment(ref this._messagesDropped);
                    } else {
                        try {
                            if (0 == this._semaphoreProcessMessageQueueIdle.CurrentCount) {
                                this._semaphoreProcessMessageQueueIdle.Release();
                            }
                        } catch {
                        }
                    }
                } catch {
                    //cancellation token canceled or CompleteAdding called
                }
            }
        }

        private bool EnsureMessageQueue(out BlockingCollection<LogMessage> messageQueue, out CancellationTokenSource stopTokenSource) {
            if ((_messageQueue) is null || _stopTokenSource is null) {
                lock (this._semaphoreProcessMessageQueueWrite) {
                    if (_messageQueue is null || _stopTokenSource is null) {
                        // messageQueue
                        var concurrentMessageQueue = new ConcurrentQueue<LogMessage>();
                        if (this._queueSize == null) {
                            messageQueue = new BlockingCollection<LogMessage>(concurrentMessageQueue);
                        } else {
                            messageQueue = new BlockingCollection<LogMessage>(concurrentMessageQueue, this._queueSize.Value);
                        }

                        stopTokenSource = new CancellationTokenSource();

                        this._messageQueue = messageQueue;
                        this._stopTokenSource = stopTokenSource;
                        System.Threading.Interlocked.MemoryBarrier();
                        return true;
                    }
                }
            }

            {
                messageQueue = this._messageQueue;
                stopTokenSource = this._stopTokenSource;
                return false;
            }
        }

        internal void Start() {
            if (0 < this._workingState) { return; }
            if (!this._isPathValid) { return; }

            lock (this) {
                if (0 < this._workingState) { return; }

                _ = this.EnsureMessageQueue(out _, out _);
                this._outputTask = Task.Run(this.ProcessLogQueue);
                this.StartHostApplicationLifetime();
                this._workingState = 1;
            }
        }

        partial void StartHostApplicationLifetime();

        internal void Stop() {
            if (this._workingState <= 0) { return; }

            lock (this) {
                if (this._workingState <= 0) { return; }

                this._workingState = -1;

                var stopTokenSource = this._stopTokenSource;
                this._stopTokenSource = default;
                var messageQueue = this._messageQueue;
                this._messageQueue = default;
                var outputTask = this._outputTask;
                this._outputTask = default;

                stopTokenSource?.Cancel();
                messageQueue?.CompleteAdding();
                try {
                    this._outputTask?.Wait(this._interval);
                } catch (TaskCanceledException) {
                } catch (AggregateException ex) when (ex.InnerExceptions.Count == 1 && ex.InnerExceptions[0] is TaskCanceledException) {
                }

                this._workingState = 0;
            }
        }

        private async Task ProcessLogQueue() {
            this.EnsureMessageQueue(out var messageQueue, out var stopTokenSource);

            try {
                this._processMessageQueueWatchDog = 0;
                while (!stopTokenSource.IsCancellationRequested) {
                    var didFlushContent = await this.FlushAsync(stopTokenSource.Token);
                    if (didFlushContent) {
                        // content was written - so wait and repeat
                        this._processMessageQueueWatchDog = _processMessageQueueWatchDogReset;
                        if (stopTokenSource.IsCancellationRequested) { return; }

                        await Task.Delay(this._flushPeriod, stopTokenSource.Token).ConfigureAwait(false);
                        continue;
                    } else {
                        if (0 <= this._processMessageQueueWatchDog) {
                            this._processMessageQueueWatchDog--;
                        }
                        if (0 < this._processMessageQueueWatchDog) {
                            // no content was written - and - so wait for a time.
                            await Task.Delay(this._flushPeriod, stopTokenSource.Token)
                                .ConfigureAwait(false);
                        } else {
                            // no content was written - and long time nothing happened - so wait for idle.
                            try {
                                await this._semaphoreProcessMessageQueueIdle
                                    .WaitAsync(stopTokenSource.Token)
                                    .ConfigureAwait(false);
                            } catch { }
                        }
                    }
                }
            } catch (System.OperationCanceledException) {
                // good bye
            } catch (Exception error) {
                InternalLogger.GetInstance().Fail(error);
            }
        }
        /// <summary>
        /// Flush the remaining log content to disk.
        /// </summary>
        /// <param name="cancellationToken">stop me</param>
        /// <returns></returns>
        public async Task<bool> FlushAsync(CancellationToken cancellationToken) {
            await this._semaphoreProcessMessageQueueWrite.WaitAsync();
            try {
                return await FlushInner(cancellationToken).ConfigureAwait(false);
            } finally {
                this._semaphoreProcessMessageQueueWrite.Release();
            }
        }

        private async Task<bool> FlushInner(CancellationToken cancellationToken) {
            if (!(this._messageQueue is { } messageQueue)) { return false; }

            var limit = this._batchSize ?? int.MaxValue;

#pragma warning disable CS8601 // Possible null reference assignment.
            List<LogMessage> currentBatch =
                System.Threading.Interlocked.Exchange<List<LogMessage>?>(ref this._currentBatchPool, default)
                ?? new(1024);
#pragma warning restore CS8601 // Possible null reference assignment.
            while (limit > 0 && messageQueue.TryTake(out var message)) {
                currentBatch.Add(message);
                limit--;
            }

            var messagesDropped = Interlocked.Exchange(ref this._messagesDropped, 0);
            if (messagesDropped != 0) {
                currentBatch.Add(new LogMessage(DateTimeOffset.UtcNow, $"{messagesDropped} message(s) dropped because of queue size limit. Increase the queue size or decrease logging verbosity to avoid {Environment.NewLine}"));
            }

            if (currentBatch.Count > 0) {
                try {
                    await this.WriteMessagesAsync(currentBatch, cancellationToken).ConfigureAwait(false);
                    currentBatch.Clear();
#pragma warning disable CS8601 // Possible null reference assignment.
                    System.Threading.Interlocked.Exchange<List<LogMessage>?>(ref this._currentBatchPool, currentBatch);
#pragma warning restore CS8601 // Possible null reference assignment.
                } catch {
                    // ignored
                }
                return true;
            } else {
                return false;
            }
        }

        /// <summary>
        /// Flush the remaining log content to disk - use this only at the end.
        /// Otherwise Use FlushAsync().GetAwaiter().GetResult();
        /// </summary>
        public void Flush()
            => this.FlushInner(CancellationToken.None).GetAwaiter().GetResult();

        private async Task WriteMessagesAsync(IEnumerable<LogMessage> messages, CancellationToken cancellationToken) {
            if (!this.IsEnabled
                || !(this._path is { Length: > 0 })
                ) {
                return;
            }

            var utcNow = DateTime.UtcNow;
            try {
                if (this._nextCheckPath < utcNow) { 
                    this._nextCheckPath = utcNow.AddHours(1);
                    Directory.CreateDirectory(this._path);
                }
            } catch (System.Exception error) {
                System.Console.Error.WriteLine(error.ToString());
                return;
            }

            foreach (var group in messages.GroupBy(this.GetGrouping)) {
                var fullName = this.GetFullName(group.Key);
                var fileInfo = new FileInfo(fullName);
                if (this._maxFileSize.HasValue && this._maxFileSize > 0 && fileInfo.Exists && fileInfo.Length > this._maxFileSize) {
                    return;
                }
                try {
                    using (var streamWriter = File.AppendText(fullName)) {
                        foreach (var item in group) {
                            await streamWriter.WriteAsync(item.Message).ConfigureAwait(false);
                        }
#if NET8_0_OR_GREATER
                        await streamWriter.FlushAsync(cancellationToken).ConfigureAwait(false);
                        await streamWriter.DisposeAsync();
#else
                        streamWriter.Flush();
#endif
                    }
                } catch (System.Exception error) {
                    System.Console.Error.WriteLine(error.ToString());

                    // folder deleted? disk full?
                    this._nextCheckPath = DateTime.MinValue;
                    this._nextCheckRollFiles = DateTime.MinValue;
                }
            }

            try {
                if (this._nextCheckRollFiles < utcNow) {
                    this._nextCheckRollFiles = utcNow.AddHours(1);
                    this.RollFiles();
                }
            } catch (System.Exception error) {
                System.Console.Error.WriteLine(error.ToString());
            }
        }

        private string GetFullName((int Year, int Month, int Day) group) {
            if (this._path is null) { throw new System.ArgumentException("_path is null"); }

            return Path.Combine(this._path, $"{this._fileName}{group.Year:0000}{group.Month:00}{group.Day:00}.txt");
        }

        private (int Year, int Month, int Day) GetGrouping(LogMessage message) {
            return (message.Timestamp.Year, message.Timestamp.Month, message.Timestamp.Day);
        }

        private void RollFiles() {
            if (this._path is null) { throw new System.ArgumentException("_path is null"); }

            if (this._maxRetainedFiles > 0) {
                try {
                    var files = new DirectoryInfo(this._path)
                        .GetFiles(this._fileName + "*")
                        .OrderByDescending(f => f.Name)
                        .Skip(this._maxRetainedFiles.Value);

                    foreach (var item in files) {
                        try {
                            item.Delete();
                        } catch (System.Exception error) {
                            System.Console.Error.WriteLine(error.ToString());
                        }
                    }
                } catch (System.Exception error) {
                    System.Console.Error.WriteLine(error.ToString());
                }
            }

#if false
            if (_maxRetainedFiles > 0) {
                try {
                    var files = new DirectoryInfo(_path)
                        .GetFiles("stdout*")
                        .OrderByDescending(f => f.Name)
                        .Skip(_maxRetainedFiles.Value);

                    foreach (var item in files) {
                        try {
                            item.Delete();
                        } catch (System.Exception error) {
                            System.Console.Error.WriteLine(error.ToString());
                        }
                    }
                } catch (System.Exception error) {
                    System.Console.Error.WriteLine(error.ToString());
                }
            }
#endif

        }

        /// <inheritdoc/>
        public void Dispose() {
            using (this._optionsChangeToken) {
                this._optionsChangeToken = default;
            }
            if (0 < this._workingState) {
                this._messageQueue?.CompleteAdding();

                try {
                    this._outputTask?.Wait(this._flushPeriod);
                } catch (TaskCanceledException) {
                } catch (AggregateException ex) when (ex.InnerExceptions.Count == 1 && ex.InnerExceptions[0] is TaskCanceledException) {
                }

                this.Stop();
            }
            using (this._stopTokenSource) {
                this._stopTokenSource = default;
            }
            this.DisposeHostApplicationLifetime();
        }

        partial void DisposeHostApplicationLifetime();

        /// <summary>
        /// Creates a <see cref="LocalFileLogger"/> with the given <paramref name="categoryName"/>.
        /// </summary>
        /// <param name="categoryName">The name of the category to create this logger with.</param>
        /// <returns>The <see cref="LocalFileLogger"/> that was created.</returns>
        public ILogger CreateLogger(string categoryName) => new LocalFileLogger(this, categoryName);

        /// <summary>
        /// Sets the scope on this provider.
        /// </summary>
        /// <param name="scopeProvider">Provides the scope.</param>
        void ISupportExternalScope.SetScopeProvider(IExternalScopeProvider scopeProvider) {
            this._scopeProvider = scopeProvider;
        }
    }

#if LocalFileIHostApplicationLifetime

    public sealed partial class LocalFileLoggerProvider {
        private readonly LazyGetService<IHostApplicationLifetime>? _lazyLifetime;

        private bool _lifetimeRegistered;
        private CancellationTokenRegistration _flushRegistered;
        private CancellationTokenRegistration _disposeRegistered;

        /// <summary>
        /// Creates a new instance of <see cref="LocalFileLoggerProvider"/>.
        /// </summary>
        /// <param name="options">The options to use when creating a provider.</param>
        public LocalFileLoggerProvider(
            LazyGetService<Microsoft.Extensions.Hosting.IHostApplicationLifetime> lazyLifetime,
            IOptionsMonitor<LocalFileLoggerOptions> options
            ) : this(options) {
            this._lazyLifetime = lazyLifetime;
        }

        partial void StartHostApplicationLifetime() {
            if ((!this._lifetimeRegistered)
                && (this._lazyLifetime?.GetService() is { } lifetime)) {
                this._flushRegistered = lifetime.ApplicationStopping.Register(() => this.Flush());
                this._disposeRegistered = lifetime.ApplicationStopped.Register(() => this.Dispose());
                this._lifetimeRegistered = true;
            }
        }

        partial void DisposeHostApplicationLifetime() {
            if (this._lifetimeRegistered) {
                using (this._flushRegistered) {
                    using (this._disposeRegistered) {
                        this._lifetimeRegistered = false;
                        this._flushRegistered = default;
                        this._disposeRegistered = default;
                    }
                }
            }
        }

    }
#endif
}
