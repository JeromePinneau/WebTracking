using System;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Webtracking.Middleware
{
    public class GlobalSetting
    {
        public string ConnectionString { get; set; }
        public string MapathBat { get; set; }

        public int CharsOnelink { get; set; }

        public string LinkBuilModel { get; set; }

        public static GlobalSetting GetSettings()
        {
            GlobalSetting Setting = new GlobalSetting();
            try
            {
                var builder = new ConfigurationBuilder()
                        .SetBasePath(System.IO.Directory.GetCurrentDirectory())
                        .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                        .AddEnvironmentVariables();
                IConfigurationRoot configuration = builder.Build();
                configuration.Bind(Setting);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw new Exception(ex.Message);
            }
            return Setting;
        }
    }
}
