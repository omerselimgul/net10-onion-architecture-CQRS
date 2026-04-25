using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace ECommercialApi.Persistence
{
    static class Configuration
    {
        public static string ConnectionString
        {
            get
            {
                var configuration = new ConfigurationBuilder()
                    .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), "../../Presentation/ECommercialApi.Api"))  // genellikle Persistence projesi
                    .AddJsonFile("appsettings.json", optional: false)
                    .Build();
                return configuration.GetConnectionString("PostgreSQL");
            }
        }

    }
}
