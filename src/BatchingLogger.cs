// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Brimborium.Extensions.Logging.LocalFile;

[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
public class BatchingLogger : ILogger {
    private static readonly ObjectPool<StringBuilder> _StringBuilderPool = (new Microsoft.Extensions.ObjectPool.DefaultObjectPoolProvider()).CreateStringBuilderPool();

    //private object x = (new Microsoft.Extensions.ObjectPool.DefaultObjectPoolProvider()).CreateStringBuilderPool();
    //_Pool = new DefaultObjectPoolProvider().CreateStringBuilderPool();
    private readonly BatchingLoggerProvider _provider;
    private readonly string _category;

    public BatchingLogger(BatchingLoggerProvider loggerProvider, string categoryName) {
        this._provider = loggerProvider;
        this._category = categoryName;
    }

    public IDisposable BeginScope<TState>(TState state)
        where TState : notnull
        => this._provider.ScopeProvider?.Push(state) ?? NullScope.Instance;

    public bool IsEnabled(LogLevel logLevel) {
        return (this._provider.IsEnabled) && (logLevel != LogLevel.None);
    }

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter) {
        DateTimeOffset now = this._provider.UseUtcTimestamp ? DateTimeOffset.UtcNow : DateTimeOffset.Now;
        this.Log(now, logLevel, eventId, state, exception, formatter);
    }

    private static byte[] crlf = new byte[] { 13, 10 };

    public void Log<TState>(DateTimeOffset timestamp, LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) {
        if (!this.IsEnabled(logLevel)) {
            return;
        }

        if (this._provider.UseJSONFormat) {
            //string message = formatter(state, exception);
            //if (exception == null && message == null) {
            //    return;
            //}
            string message = "";
            if (exception is not null) {
                message = formatter(state, exception);
            }
            int DefaultBufferSize = 1024 + message.Length;
            using (var output = new PooledByteBufferWriter(DefaultBufferSize)) {
                using (var writer = new Utf8JsonWriter(output, this._provider.JsonWriterOptions)) {
                    writer.WriteStartObject();
                    var timestampFormat = this._provider.TimestampFormat ?? "u"; //"yyyy-MM-dd HH:mm:ss.fff zzz";
                    //if (timestampFormat != null) {
                    //DateTimeOffset dateTimeOffset = this._provider.UseUtcTimestamp ? DateTimeOffset.UtcNow : DateTimeOffset.Now;
                    writer.WriteString("Timestamp", timestamp.ToString(timestampFormat));
                    //}
                    writer.WriteNumber("EventId", eventId.Id);
                    writer.WriteString("LogLevel", GetLogLevelString(logLevel));
                    writer.WriteString("Category", this._category);
                    if (!string.IsNullOrEmpty(message)) {
                        writer.WriteString("Message", message);
                    }

                    if (exception != null) {
                        string exceptionMessage = exception.ToString();
                        if (!this._provider.JsonWriterOptions.Indented) {
                            exceptionMessage = exceptionMessage.Replace(Environment.NewLine, " ");
                        }
                        writer.WriteString(nameof(Exception), exceptionMessage);
                    }

                    if (state != null) {
                        writer.WriteStartObject(nameof(state));
                        // writer.WriteString("Message", state.ToString());
                        if (state is IReadOnlyCollection<KeyValuePair<string, object>> stateProperties) {
                            foreach (KeyValuePair<string, object> item in stateProperties) {
                                if (item.Key == "{OriginalFormat}") {
                                    WriteItem(writer, item);
                                    break;
                                } else {
                                }
                            }
                            foreach (KeyValuePair<string, object> item in stateProperties) {
                                if (item.Key == "{OriginalFormat}") {
                                    //
                                } else {
                                    WriteItem(writer, item);
                                }
                            }
                        }
                        writer.WriteEndObject();
                    }
                    WriteScopeInformation(writer, this._provider.ScopeProvider);
                    writer.WriteEndObject();
                    writer.Flush();
                    //if ((output.WrittenCount + 2) < output.Capacity) { }
                    output.Write(new ReadOnlySpan<byte>(crlf));
                }
                message = Encoding.UTF8.GetString(output.WrittenMemory.Span);
            }
            this._provider.AddMessage(timestamp, message);
        } else {

            //var builder = new StringBuilder(1024);
            var builder = _StringBuilderPool.Get();
            var timestampFormat = this._provider.TimestampFormat ?? "yyyy-MM-dd HH:mm:ss.fff zzz";
            builder.Append(timestamp.ToString(timestampFormat /*"yyyy-MM-dd HH:mm:ss.fff zzz"*/, CultureInfo.InvariantCulture));
            builder.Append(" [");
            //builder.Append(logLevel.ToString());
            builder.Append(GetLogLevelString(logLevel));
            builder.Append("] ");
            builder.Append(this._category);

            var scopeProvider = this._provider.ScopeProvider;
            if (scopeProvider != null) {
                scopeProvider.ForEachScope((scope, stringBuilder) => {
                    stringBuilder.Append(" => ").Append(scope);
                }, builder);

                //builder.AppendLine(":");
                builder.Append(":");
            } else {
                builder.Append(": ");
            }

            if (this._provider.IncludeEventId) {
                builder.Append(eventId.Id.ToString("d6"));
                builder.Append(": ");
            }
            var message = formatter(state, exception);
            builder.Append(message);
            //.Replace(Environment.NewLine, "; ").Replace("\r", "; ").Replace("\n", "; ")
            if (exception != null) {
                //builder.AppendLine(exception.ToString()).Replace(Environment.NewLine, "; ");
                builder.Append(exception.ToString());
            }

            builder.Replace(Environment.NewLine, "; ");
            builder.Replace("\r", "; ");
            builder.Replace("\n", "; ");
            builder.AppendLine();
            this._provider.AddMessage(timestamp, builder.ToString());
            
            builder.Clear();
            _StringBuilderPool.Return(builder);
        }
    }

    private static string GetLogLevelString(LogLevel logLevel) {
        return logLevel switch {
            LogLevel.Trace => "Trace",
            LogLevel.Debug => "Debug",
            LogLevel.Information => "Information",
            LogLevel.Warning => "Warning",
            LogLevel.Error => "Error",
            LogLevel.Critical => "Critical",
            _ => throw new ArgumentOutOfRangeException(nameof(logLevel))
        };
    }

    private void WriteScopeInformation(Utf8JsonWriter writer, IExternalScopeProvider? scopeProvider) {
        if (this._provider.IncludeScopes && scopeProvider != null) {
            writer.WriteStartArray("Scopes");
            scopeProvider.ForEachScope((scope, state) => {
                if (scope is IEnumerable<KeyValuePair<string, object>> scopeItems) {
                    state.WriteStartObject();
                    state.WriteString("Message", scope.ToString());
                    foreach (KeyValuePair<string, object> item in scopeItems) {
                        WriteItem(state, item);
                    }
                    state.WriteEndObject();
                } else {
                    state.WriteStringValue(ToInvariantString(scope));
                }
            }, writer);
            writer.WriteEndArray();
        }
    }

    private static void WriteItem(Utf8JsonWriter writer, KeyValuePair<string, object> item) {
        var key = item.Key;
        switch (item.Value) {
            case bool boolValue:
                writer.WriteBoolean(key, boolValue);
                break;
            case byte byteValue:
                writer.WriteNumber(key, byteValue);
                break;
            case sbyte sbyteValue:
                writer.WriteNumber(key, sbyteValue);
                break;
            case char charValue:
#if NETCOREAPP
                writer.WriteString(key, MemoryMarshal.CreateSpan(ref charValue, 1));
#else
                    writer.WriteString(key, charValue.ToString());
#endif
                break;
            case decimal decimalValue:
                writer.WriteNumber(key, decimalValue);
                break;
            case double doubleValue:
                writer.WriteNumber(key, doubleValue);
                break;
            case float floatValue:
                writer.WriteNumber(key, floatValue);
                break;
            case int intValue:
                writer.WriteNumber(key, intValue);
                break;
            case uint uintValue:
                writer.WriteNumber(key, uintValue);
                break;
            case long longValue:
                writer.WriteNumber(key, longValue);
                break;
            case ulong ulongValue:
                writer.WriteNumber(key, ulongValue);
                break;
            case short shortValue:
                writer.WriteNumber(key, shortValue);
                break;
            case ushort ushortValue:
                writer.WriteNumber(key, ushortValue);
                break;
            case null:
                writer.WriteNull(key);
                break;
            default:
                writer.WriteString(key, ToInvariantString(item.Value));
                break;
        }
    }

    private static string? ToInvariantString(object? obj) => Convert.ToString(obj, CultureInfo.InvariantCulture);
}
