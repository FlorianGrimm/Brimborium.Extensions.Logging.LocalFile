// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable IDE0079 // Remove unnecessary suppression

namespace Brimborium.Extensions.Logging.LocalFile;

[ProviderAlias("LocalFile")]
public class LocalFileLoggerProvider : BatchingLoggerProvider {
    public readonly string? Path;
    public readonly string FileName;
    public readonly int? MaxFileSize;
    public readonly int? MaxRetainedFiles;

    /// <summary>
    /// Creates a new instance of <see cref="AzureAppServicesFileLoggerProvider"/>.
    /// </summary>
    /// <param name="options">The options to use when creating a provider.</param>
    [SuppressMessage("ApiDesign", "RS0022:Constructor make noninheritable base class inheritable", Justification = "Required for backwards compatibility")]
    public LocalFileLoggerProvider(
        IOptionsMonitor<LocalFileLoggerOptions> options
        ) : base(options) {
        var loggerOptions = options.CurrentValue;
        this.Path = string.IsNullOrEmpty(loggerOptions.LogDirectory) ? null : System.IO.Path.GetFullPath(loggerOptions.LogDirectory);
        this.FileName = loggerOptions.FileName;
        this.MaxFileSize = loggerOptions.FileSizeLimit;
        this.MaxRetainedFiles = loggerOptions.RetainedFileCountLimit;
    }

    internal protected override async Task WriteMessagesAsync(IEnumerable<LogMessage> messages, CancellationToken cancellationToken) {
        if (this.Path is null) { throw new System.ArgumentException("Path is null"); }

        Directory.CreateDirectory(this.Path);

        foreach (var group in messages.GroupBy(this.GetGrouping)) {
            var fullName = this.GetFullName(group.Key);
            var fileInfo = new FileInfo(fullName);
            if (this.MaxFileSize.HasValue && this.MaxFileSize > 0 && fileInfo.Exists && fileInfo.Length > this.MaxFileSize) {
                return;
            }
            try {
                using (var streamWriter = File.AppendText(fullName)) {
                    foreach (var item in group) {
                        await streamWriter.WriteAsync(item.Message).ConfigureAwait(false);
                    }
                    await streamWriter.FlushAsync().ConfigureAwait(false);
                    await streamWriter.DisposeAsync().ConfigureAwait(false);
                    // streamWriter.Close();
                }
            } catch (System.Exception error) {
                System.Console.Error.WriteLine(error.ToString());
            }
        }

        this.RollFiles();
    }

    private string GetFullName((int Year, int Month, int Day) group) {
        if (this.Path is null) { throw new System.ArgumentException("_path is null"); }

        return System.IO.Path.Combine(this.Path, $"{this.FileName}{group.Year:0000}{group.Month:00}{group.Day:00}.txt");
    }

    private (int Year, int Month, int Day) GetGrouping(LogMessage message) {
        return (message.Timestamp.Year, message.Timestamp.Month, message.Timestamp.Day);
    }

    private void RollFiles() {
        if (this.Path is null) { throw new System.ArgumentException("_path is null"); }

        try {
            if (this.MaxRetainedFiles > 0) {
                var files = new DirectoryInfo(this.Path)
                    .GetFiles(this.FileName + "*")
                    .OrderByDescending(f => f.Name)
                    .Skip(this.MaxRetainedFiles.Value);

                foreach (var item in files) {
                    item.Delete();
                }
            }
        } catch (System.Exception error) {
            System.Console.Error.WriteLine(error.ToString());
        }
    }
}
