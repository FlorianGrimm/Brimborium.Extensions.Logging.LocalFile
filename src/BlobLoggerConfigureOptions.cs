// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Brimborium.Extensions.Logging.LocalFile;

public class BlobLoggerConfigureOptions : BatchLoggerConfigureOptions, IConfigureOptions<AzureBlobLoggerOptions> {
    private readonly IWebAppContext _context;
    private readonly Action<AzureBlobLoggerOptions> _configureOptions;

    public BlobLoggerConfigureOptions(IConfiguration configuration, IWebAppContext context, Action<AzureBlobLoggerOptions> configureOptions)
        : base(configuration, "AzureBlobEnabled", false) {
        this._context = context;
        this._configureOptions = configureOptions;
    }

    public void Configure(AzureBlobLoggerOptions options) {
        base.Configure(options);
        options.ContainerUrl = this._Configuration.GetSection("APPSETTING_DIAGNOSTICS_AZUREBLOBCONTAINERSASURL")?.Value;
        options.ApplicationName = this._context.SiteName;
        options.ApplicationInstanceId = this._context.SiteInstanceId;

        this._configureOptions(options);
    }
}
