using CommandLine;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SDAHymns.CLI.Commands;
using SDAHymns.Core.Data;
using SDAHymns.Core.Services;

// Setup dependency injection
var services = new ServiceCollection();

// Database context
services.AddDbContext<HymnsContext>(options =>
{
    var dbPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "Resources", "hymns.db");
    options.UseSqlite($"Data Source={dbPath}");
});

// Services
services.AddScoped<PowerPointParserService>();
services.AddScoped<IVerseImportService, VerseImportService>();

var serviceProvider = services.BuildServiceProvider();

// Parse command-line arguments and execute
return await Parser.Default.ParseArguments<ImportOptions, ImportOrphanPptCommand, TestPptCommand, TestVerseExtractionCommand, ImportVersesCommand>(args)
    .MapResult(
        async (ImportOptions opts) => await ImportCommandHandler.ExecuteAsync(opts),
        async (ImportOrphanPptCommand cmd) => await cmd.ExecuteAsync(),
        async (TestPptCommand cmd) => await cmd.ExecuteAsync(),
        async (TestVerseExtractionCommand cmd) => await cmd.ExecuteAsync(),
        async (ImportVersesCommand cmd) =>
        {
            using var scope = serviceProvider.CreateScope();
            var importService = scope.ServiceProvider.GetRequiredService<IVerseImportService>();
            return await ImportVersesCommand.ExecuteAsync(cmd, importService);
        },
        errs => Task.FromResult(1));
