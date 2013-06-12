using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;
namespace Joe.Business
{
    static class Configuration
    {
        public static int CacheDuration
        {
            get
            {
                return Convert.ToInt32(ConfigurationManager.AppSettings["BusinessCacheDuration"] ?? "8");
            }
        }
    }
}
