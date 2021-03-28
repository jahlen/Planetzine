using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Planetzine.Common
{
    public static class ConfigurationManager
    {
        public static Dictionary<string, string> AppSettings { get; private set; }

        public static void Init(IConfiguration configuration)
        {
            AppSettings = new Dictionary<string, string>();

            // Copy the Settings section
            var settings = configuration.GetSection("Settings");
            foreach (var child in settings.GetChildren())
            {
                AppSettings.Add(child.Key, child.Value);
            }
        }
    }
}
