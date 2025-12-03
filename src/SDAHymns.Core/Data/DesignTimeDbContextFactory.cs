using Microsoft.EntityFrameworkCore.Design;

namespace SDAHymns.Core.Data;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<HymnsContext>
{
    public HymnsContext CreateDbContext(string[] args)
    {
        // Get solution root directory (two levels up from project directory)
        var projectDir = Directory.GetCurrentDirectory();
        var solutionRoot = Directory.GetParent(projectDir)?.Parent?.FullName
            ?? throw new InvalidOperationException("Could not find solution root");
        var dbPath = Path.Combine(solutionRoot, "Resources", "hymns.db");

        var optionsBuilder = new DbContextOptionsBuilder<HymnsContext>();
        optionsBuilder.UseSqlite($"Data Source={dbPath}");

        return new HymnsContext(optionsBuilder.Options);
    }
}
