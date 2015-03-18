using System;
using System.Collections.Specialized;
using System.Configuration;
using System.IO;
using System.Reflection;

namespace EventCountHybridTopology
{
    public class AppConfig
    {
        public const string UserConfig = "UserConfig";

        public string EventHubFqnAddress { get; set; }
        public string EventHubNamespace { get; set; }
        public string EventHubEntityPath { get; set; }
        public string EventHubUsername { get; set; }
        public string EventHubPassword { get; set; }
        public int EventHubPartitions { get; set; }

        public long EventCountPerPartition { get; set; }

        public string SqlDbServerName { get; set; }
        public string SqlDbDatabaseName { get; set; }
        public string SqlDbUsername { get; set; }
        public string SqlDbPassword { get; set; }

        public AppConfig(string configPath = "SCPHost.exe.config")
        {
            if (!File.Exists(configPath))
            {
                //Try to find the config in current assembly directory
                configPath = Path.Combine(Directory.GetParent(Assembly.GetAssembly(this.GetType()).Location).FullName, configPath);
            }
            var map = new ExeConfigurationFileMap { ExeConfigFilename = configPath };
            
            ReadValues(ConfigurationManager.OpenMappedExeConfiguration(map, ConfigurationUserLevel.None));
        }

        public AppConfig(Configuration config)
        {
            ReadValues(config);
        }

        private void ReadValues(Configuration config)
        {
            EventHubEntityPath = config.AppSettings.Settings["EventHubEntityPath"].Value;
            EventHubFqnAddress = config.AppSettings.Settings["EventHubFqnAddress"].Value;
            EventHubNamespace = config.AppSettings.Settings["EventHubNamespace"].Value;
            EventHubPassword = config.AppSettings.Settings["EventHubPassword"].Value;
            EventHubUsername = config.AppSettings.Settings["EventHubUsername"].Value;

            var partitions = 0;
            var parseResult = int.TryParse(config.AppSettings.Settings["EventHubPartitions"].Value, out partitions);
            if (parseResult)
            {
                EventHubPartitions = partitions;
            }
            else
            {
                EventHubPartitions = 32;
            }

            long finalCountPerPartition = 0;
            parseResult = long.TryParse(config.AppSettings.Settings["EventCountPerPartition"].Value, out finalCountPerPartition);
            if (parseResult)
            {
                EventCountPerPartition = finalCountPerPartition;
            }
            else
            {
                EventCountPerPartition = 3200000;
            }

            SqlDbServerName = config.AppSettings.Settings["SqlDbServerName"].Value;
            SqlDbDatabaseName = config.AppSettings.Settings["SqlDbDatabaseName"].Value;
            SqlDbUsername = config.AppSettings.Settings["SqlDbUsername"].Value;
            SqlDbPassword = config.AppSettings.Settings["SqlDbPassword"].Value;
        }
    }
}