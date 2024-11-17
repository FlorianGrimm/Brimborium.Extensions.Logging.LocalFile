using System.Buffers;
using System.Runtime.CompilerServices;
using System.Text;

internal class Program {
    private static void Main(string[] args) {
        var slnFolder=GetFolderPath();

        string[] listFileName = new[] {
            "LocalFileLoggerExtensions.cs",
            "LocalFileLoggerOptions.cs",
            "LocalFileLoggerConfigureOptions.cs",
            "LocalFileLogger.cs",
            "LocalFileLoggerProvider.cs",
            "LazyResolveService.cs",
            "InternalLogger.cs",
            "LogMessage.cs",
            "NullScope.cs",
            "PooledByteBufferWriter.cs"
        };
        Console.WriteLine(slnFolder);
        StringBuilder sb = new StringBuilder(16*1024);
        foreach(var fileName in listFileName) {
            var fullName=System.IO.Path.Combine(slnFolder, "src", fileName);
            Console.WriteLine(fullName);
            var content = System.IO.File.ReadAllText(fullName);
            sb.AppendLine(content);
        }
        {
            var outFullName = System.IO.Path.Combine(slnFolder, "singlefile", "LocalFileLogger.cs");
            System.IO.File.WriteAllText(outFullName, sb.ToString());
        }
    }

    private static string GetFolderPath([CallerFilePath] string? callerFilePath=default) {
        return System.IO.Path.GetDirectoryName(System.IO.Path.GetDirectoryName(callerFilePath)) ?? throw new Exception("CannotBe");
    }
}