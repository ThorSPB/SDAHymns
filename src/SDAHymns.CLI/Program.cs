using CommandLine;
using SDAHymns.CLI.Commands;

return await Parser.Default.ParseArguments<ImportOptions, ImportOrphanPptCommand>(args)
    .MapResult(
        async (ImportOptions opts) => await ImportCommandHandler.ExecuteAsync(opts),
        async (ImportOrphanPptCommand cmd) => await cmd.ExecuteAsync(),
        errs => Task.FromResult(1));
