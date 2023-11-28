// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Brimborium.Extensions.Logging.LocalFile;

/// <summary>
/// The <see cref="ILoggerProvider"/> implementation that stores messages by appending them to Azure Blob in batches.
/// </summary>
[ProviderAlias("AzureAppServicesBlob")]
public class BlobLoggerProvider : BatchingLoggerProvider
{
    private readonly IOptionsMonitor<AzureBlobLoggerOptions> _options;
    private readonly Func<string, ICloudAppendBlob>? _blobReferenceFactory;
    private readonly HttpClient _httpClient;

    /// <summary>
    /// Creates a new instance of <see cref="BlobLoggerProvider"/>
    /// </summary>
    /// <param name="options">The options to use when creating a provider.</param>
    [SuppressMessage("ApiDesign", "RS0022:Constructor make noninheritable base class inheritable", Justification = "Required for backwards compatibility")]
    public BlobLoggerProvider(IOptionsMonitor<AzureBlobLoggerOptions> options)
        : this(options, null!)
    {
        this._blobReferenceFactory = name => new BlobAppendReferenceWrapper(
            options.CurrentValue.ContainerUrl ?? throw new ArgumentException("options.CurrentValue.ContainerUrl is null"),
            name,
            this._httpClient);
    }

    /// <summary>
    /// Creates a new instance of <see cref="BlobLoggerProvider"/>
    /// </summary>
    /// <param name="blobReferenceFactory">The container to store logs to.</param>
    /// <param name="options">Options to be used in creating a logger.</param>
    internal protected BlobLoggerProvider(
        IOptionsMonitor<AzureBlobLoggerOptions> options,
        Func<string, ICloudAppendBlob>? blobReferenceFactory) :
        base(options)
    {
        this._options = options;
        this._blobReferenceFactory = blobReferenceFactory;
        this._httpClient = new HttpClient();
    }

    internal protected override async Task WriteMessagesAsync(IEnumerable<LogMessage> messages, CancellationToken cancellationToken)
    {
        var eventGroups = messages.GroupBy(this.GetBlobKey);
        var options = this._options.CurrentValue;
        var identifier = options.ApplicationInstanceId + "_" + options.BlobName;

        foreach (var eventGroup in eventGroups)
        {
            var key = eventGroup.Key;
            string blobName = options.FileNameFormat(new AzureBlobLoggerContext(
                options.ApplicationName?? throw new ArgumentException("options.ApplicationName is null"),
                identifier,
                new DateTimeOffset(key.Year, key.Month, key.Day, key.Hour, 0, 0, TimeSpan.Zero)));

            if (this._blobReferenceFactory is null) {
                throw new ArgumentException("blobReferenceFactory is null.");
            }
            var blob = this._blobReferenceFactory(blobName);

            using (var stream = new MemoryStream())
            using (var writer = new StreamWriter(stream))
            {
                foreach (var logEvent in eventGroup)
                {
                    writer.Write(logEvent.Message);
                }

                await writer.FlushAsync().ConfigureAwait(false);
                var tryGetBuffer = stream.TryGetBuffer(out var buffer);
                System.Diagnostics.Debug.Assert(tryGetBuffer);
                await blob.AppendAsync(buffer, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private (int Year, int Month, int Day, int Hour) GetBlobKey(LogMessage e)
    {
        return (e.Timestamp.Year,
            e.Timestamp.Month,
            e.Timestamp.Day,
            e.Timestamp.Hour);
    }
}
