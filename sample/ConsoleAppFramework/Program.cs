using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

namespace Sample;

internal class Program {
    private static void Main(string[] args) {
        var cfgBuilder = new ConfigurationBuilder();
        cfgBuilder.AddJsonFile("appsettings.json");
        var cfg = cfgBuilder.Build();
        var cfgServices = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
        var serviceProvider = cfgServices
            .AddSingleton<IConfiguration>(cfg)
            .AddSingleton<Program>()
            .AddLogging(builder => {
                builder.AddConfiguration(cfg.GetSection("Logging"));
                builder.AddLocalFile(configure: (options) => { options.BaseDirectory = System.AppContext.BaseDirectory; });
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