using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace Finance.Infrastructure
{
    public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
    {
        //public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
        public AppDbContext CreateDbContext(string[] args)
        {
            var builder = new ConfigurationBuilder()
                .AddEnvironmentVariables();

            var apiPath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "../Finance.Api"));
            var appsettings = Path.Combine(apiPath, "appsettings.json");
            var appsettingsDev = Path.Combine(apiPath, "appsettings.Development.json");
            if (File.Exists(appsettings)) builder.AddJsonFile(appsettings, optional: true);
            if (File.Exists(appsettingsDev)) builder.AddJsonFile(appsettingsDev, optional: true);

            var config = builder.Build();

            var cs = config.GetConnectionString("Default")
                     ?? config["ConnectionStrings__Default"]
                     ?? throw new InvalidOperationException("Missing ConnectionStrings:Default");

            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseNpgsql(cs, o => o.EnableRetryOnFailure())
                .Options;

            return new AppDbContext(options);
        }
    }
}
