using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace ArchoCybo.Infrastructure.Data;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ArchoCyboDbContext>
{
    public ArchoCyboDbContext CreateDbContext(string[] args)
    {
        var config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .Build();

        var optionsBuilder = new DbContextOptionsBuilder<ArchoCyboDbContext>();
        var conn = config.GetConnectionString("DefaultConnection");
        optionsBuilder.UseSqlServer(conn);
        return new ArchoCyboDbContext(optionsBuilder.Options);
    }
}
