// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Brimborium.Extensions.Logging.LocalFile;

public class AzureAppServicesFileLoggerConfigureOptions : BatchLoggerConfigureOptions, IConfigureOptions<AzureAppServicesFileLoggerOptions> {
    private readonly IWebAppContext _Context;

    public AzureAppServicesFileLoggerConfigureOptions(IConfiguration configuration, IWebAppContext context)
        : base(configuration, "AzureDriveEnabled", false) {
        this._Context = context;
    }

    public void Configure(AzureAppServicesFileLoggerOptions options) {
        base.Configure(options);
        options.LogDirectory = Path.Combine(this._Context.HomeFolder ?? ".", "LogFiles", "Application");
    }
}