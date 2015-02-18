using log4net;
using System.Configuration;

namespace HDInsight.Examples.CLI
{
    /// <summary>
    /// This helper class allows us to persist the App.config values that we generate so that they can be re-used for next run
    /// </summary>
    public static class AppConfigWriter
    {
        static readonly ILog LOG = LogManager.GetLogger(typeof(AppConfigWriter));

        public static void CurrentAppConfig(string appConfigPath = null)
        {
            LOG.InfoFormat("Updating current app config. Path: {0}", appConfigPath ?? "App.config");
            Configuration config = null;

            if (appConfigPath == null)
            {
                config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            }
            else
            {
                ExeConfigurationFileMap fileMap = new ExeConfigurationFileMap();
                fileMap.ExeConfigFilename = appConfigPath;

                // Open another config file
                config =
                   ConfigurationManager.OpenMappedExeConfiguration(fileMap,
                   ConfigurationUserLevel.None);
            }

            if (config != null)
            {
                if (AppConfig.AzurePublishSettingsFile != null)
                {
                    config.AppSettings.Settings["AzurePublishSettingsFile"].Value = AppConfig.AzurePublishSettingsFile;
                }
                if (AppConfig.SubscriptionId != null)
                {
                    config.AppSettings.Settings["SubscriptionId"].Value = AppConfig.SubscriptionId;
                }
                if (AppConfig.AzureManagementCertificateThumbprint != null)
                {
                    config.AppSettings.Settings["AzureManagementCertificateThumbprint"].Value =
                        AppConfig.AzureManagementCertificateThumbprint;
                }
                if (AppConfig.AzureManagementCertificatePath != null)
                {
                    config.AppSettings.Settings["AzureManagementCertificatePath"].Value =
                        AppConfig.AzureManagementCertificatePath;
                }
                if (AppConfig.AzureManagementCertificatePassword != null)
                {
                    config.AppSettings.Settings["AzureManagementCertificatePassword"].Value =
                        AppConfig.AzureManagementCertificatePassword;
                }
                if (AppConfig.AzureResourceName != null)
                {
                    config.AppSettings.Settings["AzureResourceNameFull"].Value =
                        AppConfig.AzureResourceName;
                }
                if (AppConfig.AzureResourceUsername != null)
                {
                    config.AppSettings.Settings["AzureResourceUsername"].Value =
                        AppConfig.AzureResourceUsername;
                }
                if (AppConfig.AzureResourcePassword != null)
                {
                    config.AppSettings.Settings["AzureResourcePassword"].Value =
                        AppConfig.AzureResourcePassword;
                }
                config.Save(ConfigurationSaveMode.Full);  // Save changes
                LOG.InfoFormat("Updated current app config successfully. Path: {0}", config.FilePath);
            }
        }

        public static void UpdateSCPHostConfig(params string[] scpHostConfigFilePaths)
        {
            foreach (var scpHostConfigFilePath in scpHostConfigFilePaths)
            {
                LOG.InfoFormat("Updating SCPHost config. Path: {0}", scpHostConfigFilePath);
                ExeConfigurationFileMap fileMap = new ExeConfigurationFileMap();
                fileMap.ExeConfigFilename = scpHostConfigFilePath;

                // Open another config file
                Configuration config =
                   ConfigurationManager.OpenMappedExeConfiguration(fileMap,
                   ConfigurationUserLevel.None);

                //config.AppSettings.Settings["EventHubFqnAddress"].Value = "servicebus.windows.net";
                config.AppSettings.Settings["EventHubNamespace"].Value = EventHubHelper.SBNamespaceName;
                config.AppSettings.Settings["EventHubEntityPath"].Value = EventHubHelper.EHDescription.Path;
                config.AppSettings.Settings["EventHubUsername"].Value = EventHubHelper.EHRuleName;
                config.AppSettings.Settings["EventHubPassword"].Value = EventHubHelper.EHRuleKey;
                config.AppSettings.Settings["EventHubPartitions"].Value = EventHubHelper.EHDescription.PartitionCount.ToString();

                config.AppSettings.Settings["HBaseClusterUrl"].Value = HDInsightHelper.HBaseCluster.ConnectionUrl;
                config.AppSettings.Settings["HBaseClusterUserName"].Value = HDInsightHelper.HBaseCluster.HttpUserName;
                config.AppSettings.Settings["HBaseClusterUserPassword"].Value = HDInsightHelper.HBaseCluster.HttpPassword;

                config.Save(ConfigurationSaveMode.Full);  // Save changes
                LOG.InfoFormat("Updated SCPHost config successfully. Path: {0}", scpHostConfigFilePath);
            }
        }
    }
}
