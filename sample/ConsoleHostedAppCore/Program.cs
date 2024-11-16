namespace ConsoleHostedAppCore;

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
                // options.BaseDirectory = System.AppContext.BaseDirectory; 
                options.BaseDirectory = GetFolderPath();
            });
        builder.Services.AddHostedService<HostedService>();
        var app = builder.Build();
        await app.RunAsync();
    }
    private static string GetFolderPath([System.Runtime.CompilerServices.CallerFilePath] string? callerFilePath = default) {
        return System.IO.Path.GetDirectoryName(callerFilePath) ?? throw new Exception("CannotBe");
    }
}

internal class HostedService : BackgroundService {
    private readonly IHostApplicationLifetime _hostApplicationLifetime;
    private readonly ILogger<HostedService> _logger;

    public HostedService(
        IHostApplicationLifetime hostApplicationLifetime,
        ILogger<HostedService> logger
        ) {
        this._hostApplicationLifetime = hostApplicationLifetime;
        this._logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
        this._logger.LogInformation("Start");
        await Task.Delay(TimeSpan.FromSeconds(1));
        this._logger.LogInformation("- fini -");
        this._hostApplicationLifetime.StopApplication();
    }
}