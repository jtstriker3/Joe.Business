using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;

namespace Joe.Business.Configuration
{
    public class BusinessConfigurationSection : ConfigurationSection
    {
        [ConfigurationProperty("defaultCultures", DefaultValue = "en-US")]
        public String DefaultCultures
        {
            get
            {
                return (String)this["defaultCultures"];
            }
            set
            {
                this["defaultCultures"] = value;
            }
        }


        [ConfigurationProperty("cacheDuration", DefaultValue = "8")]
        public int CacheDuration
        {
            get
            {
                return Convert.ToInt32(this["cacheDuration"]);
            }
            set
            {
                this["cacheDuration"] = value;
            }
        }

        [ConfigurationProperty("setAllValuesForList", DefaultValue = "false")]
        public Boolean SetAllValuesForList
        {
            get
            {
                return Convert.ToBoolean(this["setAllValuesForList"]);
            }
            set
            {
                this["setAllValuesForList"] = value;
            }
        }

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
