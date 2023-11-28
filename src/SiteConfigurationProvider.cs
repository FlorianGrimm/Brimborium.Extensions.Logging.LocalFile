// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;

using Microsoft.Extensions.Configuration;

namespace Brimborium.Extensions.Logging.LocalFile;

public class SiteConfigurationProvider {
    public static IConfiguration GetAzureLoggingConfiguration(
        IWebAppContext context,
        string configurationFile) {
        var configurationBuilder = new ConfigurationBuilder()
            .AddEnvironmentVariables();
        if (!string.IsNullOrEmpty(context.HomeFolder)) {
            var settingsFolder = Path.Combine(context.HomeFolder, "site", "diagnostics");
            var settingsFile = Path.Combine(settingsFolder, "settings.json");
            configurationBuilder
                .AddJsonFile(settingsFile, optional: true, reloadOnChange: true);
        }
        if (!string.IsNullOrEmpty(configurationFile)) {
            configurationBuilder
                .AddJsonFile(configurationFile, optional: true, reloadOnChange: true);
        }
        return configurationBuilder.Build();
    }
}
