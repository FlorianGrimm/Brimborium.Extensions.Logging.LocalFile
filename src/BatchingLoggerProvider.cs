// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#pragma warning disable CA2208 // Instantiate argument exceptions correctly

namespace Brimborium.Extensions.Logging.LocalFile;

/// <summary>
/// A provider of <see cref="BatchingLogger"/> instances.
/// </summary>
public abstract class BatchingLoggerProvider : ILoggerProvider, ISupportExternalScope {
    private readonly List<LogMessage> _CurrentBatch = new();
    private readonly TimeSpan _Interval;
    private readonly int? _QueueSize;
    private readonly int? _BatchSize;
    private readonly IDisposable? _OptionsChangeToken;

    private int _MessagesDropped;

    private BlockingCollection<LogMessage>? _MessageQueue;
    private Task? _OutputTask;
    private CancellationTokenSource? _CancellationTokenSource;

    private bool _IncludeScopes;
    private IExternalScopeProvider? _ScopeProvider;

    internal protected IExternalScopeProvider? ScopeProvider => this._IncludeScopes ? this._ScopeProvider : null;

    internal protected bool IncludeScopes => this._IncludeScopes;

    internal protected BatchingLoggerProvider(IOptionsMonitor<BatchingLoggerOptions> options) {
        // NOTE: Only IsEnabled is monitored

        var loggerOptions = options.CurrentValue;
        if (loggerOptions.BatchSize <= 0) {
            throw new ArgumentOutOfRangeException(nameof(loggerOptions.BatchSize), $"{nameof(loggerOptions.BatchSize)} must be a positive number.");
        }
        if (loggerOptions.FlushPeriod <= TimeSpan.Zero) {
            throw new ArgumentOutOfRangeException(nameof(loggerOptions.FlushPeriod), $"{nameof(loggerOptions.FlushPeriod)} must be longer than zero.");
        }

        this._Interval = loggerOptions.FlushPeriod;
        this._BatchSize = loggerOptions.BatchSize;
        this._QueueSize = loggerOptions.BackgroundQueueSize;

        this._OptionsChangeToken = options.OnChange(this.UpdateOptions);
        this.UpdateOptions(options.CurrentValue);
    }



    /// <summary>
    /// Checks if the queue is enabled.
    /// </summary>
    public bool IsEnabled { get; private set; }

    public bool UseJSONFormat { get; private set; }

    public bool IncludeEventId { get; private set; }

    public JsonWriterOptions JsonWriterOptions { get; private set; }

    /// <summary>
    /// Gets or sets format string used to format timestamp in logging messages. Defaults to <c>null</c>.
    /// </summary>
    //[StringSyntax(StringSyntaxAttribute.DateTimeFormat)]
    public string? TimestampFormat { get; set; }

    /// <summary>
    /// Gets or sets indication whether or not UTC timezone should be used to format timestamps in logging messages. Defaults to <c>false</c>.
    /// </summary>
    public bool UseUtcTimestamp { get; set; }

    private void UpdateOptions(BatchingLoggerOptions options) {
        var oldIsEnabled = this.IsEnabled;
        this.IsEnabled = options.IsEnabled;
        this.UseJSONFormat = options.UseJSONFormat;
        this.TimestampFormat = options.TimestampFormat;
        this.UseUtcTimestamp = options.UseUtcTimestamp;
        this.IncludeEventId = options.IncludeEventId;
        this.JsonWriterOptions = options.JsonWriterOptions;

        this._IncludeScopes = options.IncludeScopes;

        if (oldIsEnabled != this.IsEnabled) {
            if (this.IsEnabled) {
                this.Start();
            } else {
                this.Stop();
            }
        }
    }

    internal protected abstract Task WriteMessagesAsync(IEnumerable<LogMessage> messages, CancellationToken token);

    private async Task ProcessLogQueue() {
        if (this._CancellationTokenSource is null) { throw new ArgumentException("_cancellationTokenSource is null"); }
        if (this._MessageQueue is null) { throw new ArgumentException("_messageQueue is null"); }

        while (!this._CancellationTokenSource.IsCancellationRequested) {
            var limit = this._BatchSize ?? int.MaxValue;

            while (limit > 0 && this._MessageQueue.TryTake(out var message)) {
                this._CurrentBatch.Add(message);
                limit--;
            }

            var messagesDropped = Interlocked.Exchange(ref this._MessagesDropped, 0);
            if (messagesDropped != 0) {
                this._CurrentBatch.Add(new LogMessage(DateTimeOffset.Now, $"{messagesDropped} message(s) dropped because of queue size limit. Increase the queue size or decrease logging verbosity to avoid this.{Environment.NewLine}"));
            }

            if (this._CurrentBatch.Count > 0) {
                try {
                    await this.WriteMessagesAsync(this._CurrentBatch, this._CancellationTokenSource.Token).ConfigureAwait(false);
                } catch {
                    // ignored
                }

                this._CurrentBatch.Clear();
            } else {
                await this.IntervalAsync(this._Interval, this._CancellationTokenSource.Token).ConfigureAwait(false);
            }
        }
    }

    /// <summary>
    /// Wait for the given <see cref="TimeSpan"/>.
    /// </summary>
    /// <param name="interval">The amount of time to wait.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> that can be used to cancel the delay.</param>
    /// <returns>A <see cref="Task"/> which completes when the <paramref name="interval"/> has passed or the <paramref name="cancellationToken"/> has been canceled.</returns>
    protected virtual Task IntervalAsync(TimeSpan interval, CancellationToken cancellationToken) {
        return Task.Delay(interval, cancellationToken);
    }

    internal protected void AddMessage(DateTimeOffset timestamp, string message) {
        if (this._MessageQueue is null) { throw new ArgumentException("_messageQueue is null"); }

        if (!this._MessageQueue.IsAddingCompleted) {
            try {
                if (!this._MessageQueue.TryAdd(
                   item: new LogMessage(timestamp, message),
                    millisecondsTimeout: 0,
                    cancellationToken: (this._CancellationTokenSource is null)
                    ? CancellationToken.None
                    : this._CancellationTokenSource.Token)) {
                    Interlocked.Increment(ref this._MessagesDropped);
                }
            } catch {
                //cancellation token canceled or CompleteAdding called
            }
        }
    }

    private void Start() {
        this._MessageQueue = this._QueueSize == null ?
            new BlockingCollection<LogMessage>(new ConcurrentQueue<LogMessage>()) :
            new BlockingCollection<LogMessage>(new ConcurrentQueue<LogMessage>(), this._QueueSize.Value);

        this._CancellationTokenSource = new CancellationTokenSource();
        this._OutputTask = Task.Run(this.ProcessLogQueue);
    }

    private void Stop() {
        this._CancellationTokenSource?.Cancel();
        this._MessageQueue?.CompleteAdding();

        try {
            this._OutputTask?.Wait(this._Interval);
        } catch (TaskCanceledException) {
        } catch (AggregateException ex) when (ex.InnerExceptions.Count == 1 && ex.InnerExceptions[0] is TaskCanceledException) {
        }
    }

    /// <inheritdoc/>
    public void Dispose() {
        this._OptionsChangeToken?.Dispose();
        if (this.IsEnabled) {
            this.Stop();
        }
    }

    /// <summary>
    /// Creates a <see cref="BatchingLogger"/> with the given <paramref name="categoryName"/>.
    /// </summary>
    /// <param name="categoryName">The name of the category to create this logger with.</param>
    /// <returns>The <see cref="BatchingLogger"/> that was created.</returns>
    public ILogger CreateLogger(string categoryName) {
        return new BatchingLogger(this, categoryName);
    }

    /// <summary>
    /// Sets the scope on this provider.
    /// </summary>
    /// <param name="scopeProvider">Provides the scope.</param>
    void ISupportExternalScope.SetScopeProvider(IExternalScopeProvider scopeProvider) {
        this._ScopeProvider = scopeProvider;
    }
}
