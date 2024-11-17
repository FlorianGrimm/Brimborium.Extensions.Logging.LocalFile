namespace Sample;

using global::Microsoft.Extensions.DependencyInjection;
using global::Microsoft.Extensions.Hosting;

using global::Microsoft.Extensions.Logging;

using global::System.Threading;

internal class Program {
    private static async Task Main(string[] args) {
        var builder = new HostApplicationBuilder(args);
        builder.Logging.AddConfiguration(builder.Configuration.GetSection("Logging"));
        builder.Logging.AddLocalFile(
            configure: (options) => {
                options.BaseDirectory = GetFolderPath();
                options.LogDirectory = "LogFiles";
            });
        builder.Services.AddHostedService<HostedService>();
        builder.Services.AddLazyGetService();
        var app = builder.Build();
        await app.RunAsync();
    }
    private static string GetFolderPath([System.Runtime.CompilerServices.CallerFilePath] string? callerFilePath = default) {
        return System.IO.Path.GetDirectoryName(callerFilePath) ?? throw new Exception("CannotBe");
    }
}

internal class HostedService : BackgroundService {
    private readonly LazyGetRequiredService<IHostApplicationLifetime> _lazyHostApplicationLifetime;
    private readonly ILogger<HostedService> _logger;

    public HostedService(
        LazyGetRequiredService<IHostApplicationLifetime> lazyHostApplicationLifetime,
        ILogger<HostedService> logger
        ) {
        this._lazyHostApplicationLifetime = lazyHostApplicationLifetime;
        this._logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
        this._logger.LogInformation("Start");
        this._logger.LogTrace("LogTrace");
        this._logger.LogDebug("LogDebug");
        this._logger.LogInformation("LogInformation");
        this._logger.LogWarning("LogWarning");
        this._logger.LogError("LogError");
        await Task.Delay(TimeSpan.FromSeconds(1));
        this._logger.LogInformation("- fini -");
        this._lazyHostApplicationLifetime.GetService().StopApplication();
    }
}