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

    [Fact][Trait("Category","Unit")]
    public void InitializesLogDirectory()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new[]
        {
                new KeyValuePair<string, string>("APPSETTING_DIAGNOSTICS_AZUREBLOBCONTAINERSASURL", "http://container/url")
            }).Build();

        var contextMock = new Mock<IWebAppContext>();
        contextMock.SetupGet(c => c.HomeFolder).Returns("Home");

        var options = new AzureAppServicesFileLoggerOptions();
        new AzureAppServicesFileLoggerConfigureOptions(configuration, contextMock.Object).Configure(options);

        Assert.Equal(Path.Combine("Home", "LogFiles", "Application"), options.LogDirectory);
    }

    [Fact][Trait("Category","Unit")]
    public void InitializesBlobUriSiteInstanceAndName()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new[]
        {
                new KeyValuePair<string, string>("APPSETTING_DIAGNOSTICS_AZUREBLOBCONTAINERSASURL", "http://container/url")
            }).Build();

        var contextMock = new Mock<IWebAppContext>();
        contextMock.SetupGet(c => c.HomeFolder).Returns("Home");
        contextMock.SetupGet(c => c.SiteInstanceId).Returns("InstanceId");
        contextMock.SetupGet(c => c.SiteName).Returns("Name");

        var options = new AzureBlobLoggerOptions();
        new BlobLoggerConfigureOptions(configuration, contextMock.Object, options => options.FileNameFormat = _ => "FilenameFormat").Configure(options);

        Assert.Equal("http://container/url", options.ContainerUrl);
        Assert.Equal("InstanceId", options.ApplicationInstanceId);
        Assert.Equal("Name", options.ApplicationName);
        Assert.Equal("FilenameFormat", options.FileNameFormat(new AzureBlobLoggerContext("", "", DateTimeOffset.MinValue)));
    }
}
