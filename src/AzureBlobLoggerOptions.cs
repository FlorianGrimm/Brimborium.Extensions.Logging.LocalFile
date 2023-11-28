// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Brimborium.Extensions.Logging.LocalFile;

/// <summary>
/// Options for Azure diagnostics blob logging.
/// </summary>
public class AzureBlobLoggerOptions : BatchingLoggerOptions
{
    private string _BlobName = "applicationLog.txt";

    /// <summary>
    /// Gets or sets the last section of log blob name.
    /// Defaults to <c>"applicationLog.txt"</c>.
    /// </summary>
    public string BlobName
    {
        get { return this._BlobName; }
        set
        {
            if (string.IsNullOrEmpty(value))
            {
                throw new ArgumentException($"{nameof(this.BlobName)} must be non-empty string.", nameof(value));
            }
            this._BlobName = value;
        }
    }

    /// <summary>
    /// Gets or sets the format of the file name.
    /// Defaults to "AppName/Year/Month/Day/Hour/Identifier".
    /// </summary>
    public Func<AzureBlobLoggerContext, string> FileNameFormat { get; set; } = context =>
    {
        var timestamp = context.Timestamp;
        return $"{context.AppName}/{timestamp.Year}/{timestamp.Month:00}/{timestamp.Day:00}/{timestamp.Hour:00}/{context.Identifier}";
    };

    public string? ContainerUrl { get; set; }

    public string? ApplicationName { get; set; }

    public string? ApplicationInstanceId { get; set; }
}
