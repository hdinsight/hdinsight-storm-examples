using System;
using System.Collections.Specialized;
using System.Configuration;
using System.IO;
using System.Reflection;

namespace EventHubAggregatorToHBaseTopology.Common
{
    public class AppConfig
    {
        public const string UserConfig = "UserConfig";

        public string TimestampField { get; set; }

        public TimeSpan AggregationWindow { get; set; }
        public TimeSpan EmitWindow { get; set; }

        public TimeSpan PurgeWindow { get; set; }

        public int AggregationRankerTopNCount { get; set; }

        public string EventHubFqnAddress { get; set; }
        public string EventHubNamespace { get; set; }
        public string EventHubEntityPath { get; set; }
        public string EventHubUsername { get; set; }
        public string EventHubPassword { get; set; }
        public int EventHubPartitions { get; set; }

        public string HBaseClusterUrl { get; set; }
        public string HBaseClusterUserName { get; set; }
        public string HBaseClusterUserPassword { get; set; }

        public string HBaseTableNamePrefix { get; set; }
        public string HBaseTableNameSuffix { get; set; }

        public bool HBaseOverwrite { get; set; }

        public string PrimaryKey { get; set; }
        public string SecondaryKey { get; set; }

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
            TimestampField = config.AppSettings.Settings["TimestampField"].Value;

            int window = 1;
            var result = int.TryParse(config.AppSettings.Settings["AggregationWindow"].Value, out window);
            if (result)
            {
                AggregationWindow = TimeSpan.FromMinutes(window);
            }
            else
            {
                AggregationWindow = TimeSpan.FromMinutes(1);
            }

            window = 1;
            result = int.TryParse(config.AppSettings.Settings["EmitWindow"].Value, out window);
            if (result)
            {
                EmitWindow = TimeSpan.FromMinutes(window);
            }
            else
            {
                EmitWindow = TimeSpan.FromMinutes(1);
            }

            int count = 100;
            result = int.TryParse(config.AppSettings.Settings["AggregationRankerTopNCount"].Value, out count);
            if (result)
            {
                AggregationRankerTopNCount = count;
            }
            else
            {
                AggregationRankerTopNCount = 100;
            }

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
                EventHubPartitions = 16;
            }

            HBaseClusterUrl = config.AppSettings.Settings["HBaseClusterUrl"].Value;
            HBaseClusterUserName = config.AppSettings.Settings["HBaseClusterUserName"].Value;
            HBaseClusterUserPassword = config.AppSettings.Settings["HBaseClusterUserPassword"].Value;

            HBaseTableNamePrefix = config.AppSettings.Settings["HBaseTableNamePrefix"].Value;
            HBaseTableNameSuffix = config.AppSettings.Settings["HBaseTableNameSuffix"].Value;

            var hbaseoverwrite = true;
            result = bool.TryParse(config.AppSettings.Settings["HBaseOverwrite"].Value, out hbaseoverwrite);
            if (result)
            {
                HBaseOverwrite = hbaseoverwrite;
            }
            else
            {
                HBaseOverwrite = true;
            }

            PrimaryKey = config.AppSettings.Settings["PrimaryKey"].Value;
            SecondaryKey = config.AppSettings.Settings["SecondaryKey"].Value;
        }
    }
}