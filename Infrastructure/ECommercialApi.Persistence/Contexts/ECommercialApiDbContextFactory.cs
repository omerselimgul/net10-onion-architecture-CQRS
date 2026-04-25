using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System.IO;
using ECommercialApi.Persistence.Contexts;

namespace ECommercialApi.Persistence
{
    public class ECommercialApiDbContextFactory : IDesignTimeDbContextFactory<ECommercialApiDbContext>
    {
        public ECommercialApiDbContext CreateDbContext(string[] args)
        {

            var optionsBuilder = new DbContextOptionsBuilder<ECommercialApiDbContext>();
            optionsBuilder.UseNpgsql(Configuration.ConnectionString);

            return new ECommercialApiDbContext(optionsBuilder.Options);
        }
    }
}