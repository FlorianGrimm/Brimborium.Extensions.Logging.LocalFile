﻿namespace Sample;
using global::Microsoft.Extensions.Logging;
using global::Microsoft.Extensions.DependencyInjection;
using global::Microsoft.Extensions.Configuration;
using global::System.Diagnostics.CodeAnalysis;


internal class Program {
    [RequiresDynamicCode("")]
    [RequiresUnreferencedCode("")]
    private static void Main(string[] args) {
        System.Console.Out.WriteLine("Start");
        var cfgBuilder = new ConfigurationBuilder();
        cfgBuilder.AddJsonFile("appsettings.json");
        var cfg = cfgBuilder.Build();
        var cfgServices = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
        var serviceProvider = cfgServices
            .AddSingleton<IConfiguration>(cfg)
            .AddSingleton<Program>()
            .AddLogging((builder) => {
                builder.AddConfiguration(cfg.GetSection("Logging"));
                builder.AddLocalFile(configure: (options) => {
                    options.BaseDirectory = System.AppContext.BaseDirectory;
                });
            })
            .BuildServiceProvider();
        serviceProvider.GetRequiredService<Program>().Run(args);
        serviceProvider.FlushLocalFile();
    }

    private readonly ILogger<Program> _logger;
    public Program(ILogger<Program> logger) {
        this._logger = logger;
    }

    private void Run(string[] args) {
        this._logger.LogTrace("LogTrace is enabled.");
        this._logger.LogDebug("LogDebug is enabled.");
        this._logger.LogInformation("LogInformation is enabled.");
        this._logger.LogWarning("LogWarning is enabled.");
        this._logger.LogError("LogError is enabled.");
        this._logger.LogWarning("Program does not implement a interface IProgram. {args}", args);
    }
}