internal class Program {
    private static void Main(string[] args) {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Logging.AddLocalFile(
                configure: (options) => {
                    options.BaseDirectory = System.AppContext.BaseDirectory;
                    options.LogDirectory = "LogFiles";
                });

        builder.Services.AddControllers();

        var app = builder.Build();

        // Configure the HTTP request pipeline.

        app.UseHttpsRedirection();

        app.UseAuthorization();

        app.MapControllers();

        app.Run();
    }
}