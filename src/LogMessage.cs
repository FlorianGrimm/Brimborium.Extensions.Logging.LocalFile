// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Brimborium.Extensions.Logging.LocalFile;
[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
public readonly struct LogMessage
{
    public LogMessage(DateTimeOffset timestamp, string message)
    {
        this.Timestamp = timestamp;
        this.Message = message;
    }

    public DateTimeOffset Timestamp { get; }
    public string Message { get; }
}
