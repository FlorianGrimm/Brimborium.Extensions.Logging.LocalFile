// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace Brimborium.Extensions.Logging.LocalFile.Test;

public class ConfigureOptionsTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    [InlineData(null)]
    [Trait("Category", "Unit")]
    public void InitializesIsEnabled(bool? enabled)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new[]
        {
                new KeyValuePair<string, string>("IsEnabledKey", enabled?.ToString())
            }).Build();

        var options = new BatchingLoggerOptions();
        new BatchLoggerConfigureOptions(configuration, "IsEnabledKey", false).Configure(options);

        Assert.Equal(enabled ?? false, options.IsEnabled);
    }
}
