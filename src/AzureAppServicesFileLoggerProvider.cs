// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable IDE0079 // Remove unnecessary suppression

namespace Brimborium.Extensions.Logging.LocalFile;

/// <summary>
/// A <see cref="BatchingLoggerProvider"/> which writes out to a file.
/// </summary>
[ProviderAlias("AzureAppServicesFile")]
public class AzureAppServicesFileLoggerProvider : BatchingLoggerProvider {
    private readonly string? _Path;
    private readonly string _FileName;
    private readonly int? _MaxFileSize;
    private readonly int? _MaxRetainedFiles;

    /// <summary>
    /// Creates a new instance of <see cref="AzureAppServicesFileLoggerProvider"/>.
    /// </summary>
    /// <param name="options">The options to use when creating a provider.</param>
    [SuppressMessage("ApiDesign", "RS0022:Constructor make noninheritable base class inheritable", Justification = "Required for backwards compatibility")]
    public AzureAppServicesFileLoggerProvider(IOptionsMonitor<AzureAppServicesFileLoggerOptions> options) : base(options) {
        var loggerOptions = options.CurrentValue;
        this._Path = loggerOptions.LogDirectory;
        this._FileName = loggerOptions.FileName;
        this._MaxFileSize = loggerOptions.FileSizeLimit;
        this._MaxRetainedFiles = loggerOptions.RetainedFileCountLimit;
    }

    internal protected override async Task WriteMessagesAsync(IEnumerable<LogMessage> messages, CancellationToken cancellationToken) {
        if (this._Path is null) { throw new System.ArgumentException("_path is null"); }

        Directory.CreateDirectory(this._Path);

        foreach (var group in messages.GroupBy(this.GetGrouping)) {
            var fullName = this.GetFullName(group.Key);
            var fileInfo = new FileInfo(fullName);
            if (this._MaxFileSize > 0 && fileInfo.Exists && fileInfo.Length > this._MaxFileSize) {
                return;
            }

            try {
                using (var streamWriter = File.AppendText(fullName)) {
                    foreach (var item in group) {
                        await streamWriter.WriteAsync(item.Message).ConfigureAwait(false);
                    }
                    streamWriter.Close();
                }
            } catch (System.Exception error) {
                System.Console.Error.WriteLine(error.ToString());
            }
        }

        this.RollFiles();
    }

    private string GetFullName((int Year, int Month, int Day) group) {
        if (this._Path is null) { throw new System.ArgumentException("_path is null"); }

        return Path.Combine(this._Path, $"{this._FileName}{group.Year:0000}{group.Month:00}{group.Day:00}.txt");
    }

    private (int Year, int Month, int Day) GetGrouping(LogMessage message) {
        return (message.Timestamp.Year, message.Timestamp.Month, message.Timestamp.Day);
    }

    private void RollFiles() {
        if (this._Path is null) { throw new System.ArgumentException("_path is null"); }

        try {
            if (this._MaxRetainedFiles > 0) {
                var files = new DirectoryInfo(this._Path)
                    .GetFiles(this._FileName + "*")
                    .OrderByDescending(f => f.Name)
                    .Skip(this._MaxRetainedFiles.Value);

                foreach (var item in files) {
                    item.Delete();
                }
            }
        } catch (System.Exception error) {
            System.Console.Error.WriteLine(error.ToString());
        }
    }
}