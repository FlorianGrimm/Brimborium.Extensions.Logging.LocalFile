// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Brimborium.Extensions.Logging.LocalFile.Test;

internal class AzureAppServicesTestFileLoggerProvider : AzureAppServicesFileLoggerProvider
{
    internal ManualIntervalControl IntervalControl { get; } = new ManualIntervalControl();

    public AzureAppServicesTestFileLoggerProvider(
        string path,
        string fileName = "LogFile.",
        int maxFileSize = 32_000,
        int maxRetainedFiles = 100)
        : base(new OptionsWrapperMonitor<AzureAppServicesFileLoggerOptions>(new AzureAppServicesFileLoggerOptions()
        {
            LogDirectory = path,
            FileName = fileName,
            FileSizeLimit = maxFileSize,
            RetainedFileCountLimit = maxRetainedFiles,
            IsEnabled = true
        }))
    {
    }

    protected override Task IntervalAsync(TimeSpan interval, CancellationToken cancellationToken)
    {
        return IntervalControl.IntervalAsync();
    }
}
