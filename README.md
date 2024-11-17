# Brimborium.Extensions.Logging.LocalFile

Microsoft.Extensions.Logging Logger to local file for .Net Core/.Net Framework

The logger will write the logs after a short time (FlushPeriod).

The logger do this in a loop writing, waiting repeat.
After 10 loop of no logs the loop will goto sleep until the next log is added. This is usefull for a application that sleep sometime - so no CPU time is needed.

# Sample

- program.cs
```c#
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
                options.BaseDirectory = System.AppContext.BaseDirectory;
                options.LogDirectory = "LogFiles";
            });
        builder.Services.AddHostedService<HostedService>();
        var app = builder.Build();
        await app.RunAsync();
    }
}
```

- appsettings.json
```JSON
{
  "Logging": {
    "LocalFile": {
      "LogLevel": {
        "Sample": "Debug"
      }
    }
  }
}
```


# Using by copy the source

To ensure the last logs are written

If you using Microsoft.Extensions.Hosting please enable
```c#
#define LocalFileIHostApplicationLifetime
```

**or**

If you use not the Hosting
use at the very end.
```c#
serviceProvider.FlushLocalFile();
```

## nuget 
Not yet

## LocalFileLoggerOptions

The `LocalFileLoggerOptions` class provides various configuration options for logging to a local file. Below are the available options:

### Properties

- **FileSizeLimit** (`int?`): 
  - Description: Maximum log size in bytes or `null` for no limit. Once the log is full, no more messages will be appended.
  - Default: `10 MB`
  - Constraints: Must be a positive value.

- **RetainedFileCountLimit** (`int?`): 
  - Description: Maximum retained file count or `null` for no limit.
  - Default: `31`
  - Constraints: Must be a positive value.

- **FileName** (`string`): 
  - Description: Prefix of the file name used to store the logging information. The current date in the format `YYYYMMDD` will be added after the given value.
  - Default: `diagnostics-`
  - Constraints: Cannot be `null` or empty.

- **BaseDirectory** (`string?`): 
  - Description: Base directory where log files will be stored. Needed to enable logging if `LogDirectory` is relative.

- **LogDirectory** (`string?`): 
  - Description: Directory where log files will be stored. Needed to enable logging if `LogDirectory` is relative, then `BaseDirectory` is also needed.

- **FlushPeriod** (`TimeSpan`): 
  - Description: Period after which logs will be flushed to the store.
  - Default: `1 second`
  - Constraints: Must be greater than `TimeSpan.Zero`.

- **BackgroundQueueSize** (`int?`): 
  - Description: Maximum size of the background log message queue or `null` for no limit. After the maximum queue size is reached, the log event sink would start blocking.
  - Default: `1000`
  - Constraints: Must be a non-negative value.

- **BatchSize** (`int?`): 
  - Description: Maximum number of events to include in a single batch or `null` for no limit.
  - Default: `null`
  - Constraints: Must be a non-negative value.

- **IsEnabled** (`bool`): 
  - Description: Indicates if the logger accepts and queues writes.
  - Default: `true`

- **IncludeScopes** (`bool`): 
  - Description: Indicates whether scopes should be included in the message.
  - Default: `false`

- **TimestampFormat** (`string?`): 
  - Description: Format string used to format the timestamp in logging messages.
  - Default: `null`

- **UseUtcTimestamp** (`bool`): 
  - Description: Indicates whether UTC timezone should be used to format timestamps in logging messages.
  - Default: `false`

- **IncludeEventId** (`bool`): 
  - Description: Indicates whether event IDs should be included in the log messages.
  - Default: `false`

- **UseJSONFormat** (`bool`): 
  - Description: Indicates whether the log messages should be formatted as JSON.
  - Default: `false`

- **NewLineReplacement** (`string?`): 
  - Description: String that will replace new line characters in log messages.
  - Default: `"; "`

- **JsonWriterOptions** (`JsonWriterOptions`): 
  - Description: Options for the JSON writer.
