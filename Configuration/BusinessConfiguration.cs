using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;

namespace Joe.Business.Configuration
{
    public class BusinessConfigurationSection : ConfigurationSection
    {
        [ConfigurationProperty("DefaultCultures", DefaultValue = "en-US")]
        public String DefaultCultures { get; set; }


        [ConfigurationProperty("cacheDuration", DefaultValue = "8")]
        public int CacheDuration { get; set; }

        public static BusinessConfigurationSection Instance
        {
            get
            {
               var configuration = ConfigurationManager.GetSection("BusinessConfiguration") as BusinessConfigurationSection;

               return configuration ?? new BusinessConfigurationSection() { DefaultCultures = "en-US", CacheDuration = 8 };
            }
        }
    }
}
