using log4net;
using RandomStringGenerator;
using System;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Xml.Linq;

namespace HDInsight.Examples.CLI
{
    /// <summary>
    /// This class is the first that gets loaded when the application starts.
    /// It goes over the App.config to provide values for all helper classes
    /// </summary>
    public static class AppConfig
    {
        static readonly ILog LOG = LogManager.GetLogger(typeof(AppConfig));
 
        public static string AzurePublishSettingsFile { get; set; }
        public static string SubscriptionId { get; set; }
        public static X509Certificate2 AzureManagementCertificate { get; set; }
        public static string AzureManagementCertificateThumbprint { get; set; }
        public static string AzureManagementCertificatePath { get; set; }
        public static string AzureManagementCertificatePassword { get; set; }

        public static string AzureResourceName { get; set; }
        public static string AzureResourceLocation { get; set; }

        public static string AzureResourceUsername { get; set; }
        public static string AzureResourcePassword { get; set; }

        public static string SCPHostConfigFilename = "SCPHost.exe.config";
        public static string SCPNetCompilerPath { get; set; }
        public static string SCPNetTopologyDir { get; set; }
        public static string SCPHostConfigFilePath { get; set; }
        public static string SCPNetTopologyProjectBinPath { get; set; }

        public static StringGenerator StringGen = new StringGenerator(0, 0, 0, 0, CharType.LowerCase);

        static bool newRun = false;

        static AppConfig()
        {
            try
            {
                LOG.InfoFormat("-".PadRight(50, '-'));
                LOG.InfoFormat("AppConfig Settings:");
                LOG.InfoFormat("-".PadRight(50, '-'));

                #region Azure Settings
                SubscriptionId = ConfigurationManager.AppSettings["SubscriptionId"];

                #region Azure Credentials
                AzurePublishSettingsFile = ConfigurationManager.AppSettings["AzurePublishSettingsFile"];
                AzureManagementCertificateThumbprint = ConfigurationManager.AppSettings["ManagementCertificateThumbprint"];
                AzureManagementCertificatePath = ConfigurationManager.AppSettings["ManagementCertificate"];
                AzureManagementCertificatePassword = ConfigurationManager.AppSettings["ManagementCertificatePassword"];

                if (!String.IsNullOrWhiteSpace(AzurePublishSettingsFile))
                {
                    if (!File.Exists(AzurePublishSettingsFile))
                    {
                        LOG.ErrorFormat("Please provide a valid path for AzurePublishSettingsFile in App.config. Path: {0}",
                            AzurePublishSettingsFile);
                        throw new FileNotFoundException(AzurePublishSettingsFile);
                    }

                    if (String.IsNullOrEmpty(SubscriptionId))
                    {
                        ChooseSubscriptionFromPublishSettings(AzurePublishSettingsFile);
                    }
                    else
                    {
                        var fs = File.OpenRead(AzurePublishSettingsFile);
                        var doc = XDocument.Load(fs);
                        var subscription = doc.Descendants("Subscription").Where(xd => xd.Attribute("Id").Value.Equals(SubscriptionId, StringComparison.OrdinalIgnoreCase)).First();
                        var certBytes = Convert.FromBase64String(subscription.Attribute("ManagementCertificate").Value);
                        AzureManagementCertificate = new X509Certificate2(certBytes);
                    }
                }
                else if (!String.IsNullOrWhiteSpace(AzureManagementCertificateThumbprint))
                {
                    LOG.Info("Finding certificate with thumprint: {0} in ");
                    var store = new X509Store(StoreLocation.CurrentUser);
                    store.Open(OpenFlags.ReadOnly);
                    var certificates = store.Certificates.Find(X509FindType.FindByThumbprint, AzureManagementCertificateThumbprint, true);

                    if (certificates.Count == 0)
                    {
                        throw new ApplicationException("No certificate found.");
                    }

                    LOG.Info("Certificate found.");
                }
                else if (!String.IsNullOrWhiteSpace(AzureManagementCertificatePath) && !String.IsNullOrWhiteSpace(AzureManagementCertificatePassword))
                {
                    AzureManagementCertificate = new X509Certificate2(AzureManagementCertificatePath, AzureManagementCertificatePassword);
                }
                else
                {
                    newRun = true;
                    ImportPublishSettings();
                }
                LOG.InfoFormat("SubscriptionId: {0}", SubscriptionId);
                LOG.InfoFormat("AzureManagementCertificate: {0}", AzureManagementCertificate.Thumbprint);

                if (String.IsNullOrWhiteSpace(SubscriptionId) || AzureManagementCertificate == null)
                {
                    throw new ArgumentNullException("No Azure Management Credentials found in application configuration");
                }

                #endregion

                AzureResourceName = ConfigurationManager.AppSettings["AzureResourceNameFull"];
                if (String.IsNullOrWhiteSpace(AzureResourceName))
                {
                    var resourceNamePrefix = ConfigurationManager.AppSettings["AzureResourceNamePrefix"];
                    if (String.IsNullOrWhiteSpace(resourceNamePrefix))
                    {
                        resourceNamePrefix = StringGen.GenerateString(10).ToLowerInvariant();
                    }

                    var dateSuffix = DateTime.Now.ToString("yyyyMMddHHmm");

                    AzureResourceName = resourceNamePrefix + dateSuffix;
                }

                //Cap the AzureResourceName to 24 as Azure Storage Account Names cannot be longer than 24 characters
                if (AzureResourceName.Length > 24)
                {
                    AzureResourceName = AzureResourceName.Substring(0, 24);
                }

                var importantMessage = "-".PadRight(10, '-') + "IMPORTANT - Use this name to track all your Azure Resources" + "-".PadRight(10, '-');
                LOG.DebugFormat(importantMessage);
                LOG.DebugFormat("AzureResourceName = {0}", AzureResourceName);
                LOG.DebugFormat("-".PadRight(importantMessage.Length, '-'));
                LOG.InfoFormat("AzureResourceName = {0}", AzureResourceName);

                AzureResourceLocation = ConfigurationManager.AppSettings["AzureResourceLocation"];
                if (String.IsNullOrWhiteSpace(AzureResourceLocation))
                {
                    AzureResourceLocation = "West US";
                }
                LOG.InfoFormat("AzureResourceLocation = {0}", AzureResourceLocation);

                AzureResourceUsername = ConfigurationManager.AppSettings["AzureResourceUsername"];
                if (String.IsNullOrWhiteSpace(AzureResourceUsername))
                {
                    AzureResourceUsername = "azureadmin";
                }
                LOG.InfoFormat("AzureResourceUsername = {0}", AzureResourceUsername);

                AzureResourcePassword = ConfigurationManager.AppSettings["AzureResourcePassword"];
                if (String.IsNullOrWhiteSpace(AzureResourcePassword))
                {
                    StringGen.MinLowerCaseChars = 1;
                    StringGen.MinNumericChars = 1;
                    StringGen.MinSpecialChars = 1;
                    StringGen.MinUpperCaseChars = 1;
                    StringGen.FillRest = CharType.LowerCase;

                    AzureResourcePassword = StringGen.GenerateString(12);
                }
                LOG.InfoFormat("AzureResourcePassword = {0}", AzureResourcePassword);
                #endregion

                #region SCPNet
                SCPNetTopologyDir = Path.Combine(ConfigurationManager.AppSettings["SCPNetTopologyDir"]);
                SCPNetCompilerPath = Path.Combine(SCPNetTopologyDir, ConfigurationManager.AppSettings["SCPNetCompilerPath"]);
                SCPHostConfigFilePath = Path.Combine(SCPNetTopologyDir, SCPHostConfigFilename);
                SCPNetTopologyProjectBinPath = Path.Combine(SCPNetTopologyDir, ConfigurationManager.AppSettings["DefaultBinPath"]);
                #endregion

                LOG.InfoFormat("-".PadRight(50, '-'));
            }
            catch (Exception ex)
            {
                LOG.Error("Failed to initialize AppConfig.", ex);
                Utilities.WaitForExit();
                Environment.Exit(-1);
            }
        }

        public static void ImportSubscription()
        {
            var publishSettingsURL = "https://manage.windowsazure.com/publishsettings";
            
            LOG.DebugFormat("Launching '{0}' to download the Azure publish settings file. PLEASE NOTE the download path for next step.",
                publishSettingsURL);

            LOG.DebugFormat("Alternatively, you can download your publish settings file by visiting: {0}", publishSettingsURL);

            try
            {
                Utilities.LaunchProcess("cmd.exe", "/c start " + publishSettingsURL);
                LOG.DebugFormat("Launched '{0}' to download the Azure publish settings file.",
                    publishSettingsURL);
            }
            catch (Exception ex)
            {
                LOG.Warn("Failed to launch " + publishSettingsURL, ex);
                LOG.WarnFormat("Please download your publish settings file manually from: {0}", publishSettingsURL);
            }
        }

        public static void ImportPublishSettings()
        {
            bool importSuccess = false;
            bool downloadedPublishSettings = false;
            do
            {
                if (!downloadedPublishSettings)
                {
                    downloadedPublishSettings = true;
                    ImportSubscription();
                }

                var path = Utilities.GetUserInput("Please provide the path to your downloaded Azure publishsettings file (from the previous step)");
                if (!String.IsNullOrWhiteSpace(path))
                {
                    if (!File.Exists(path))
                    {
                        LOG.Error("Could not find any file at: " + path);
                        continue;
                    }

                    importSuccess = ChooseSubscriptionFromPublishSettings(path);
                }
            }
            while (!importSuccess);
            LOG.InfoFormat("Import successful! - SubscriptionId: {0}, Certificate: {1}", 
                SubscriptionId, AzureManagementCertificate.Thumbprint);
        }

        public static bool ChooseSubscriptionFromPublishSettings(string path)
        {
            try
            {
                AzurePublishSettingsFile = path;
                var fs = File.OpenRead(path);
                var doc = XDocument.Load(fs);

                var subscriptions = doc.Descendants("Subscription").ToList();

                if (subscriptions.Count >= 0)
                {
                    var validIndex = false;
                    var index = 0;
                    do
                    {
                        index = 0;
                        for (int i = 0; i < subscriptions.Count; i++)
                        {
                            LOG.InfoFormat("Subscription {0}: Name = {1}, Id = {2}",
                                i + 1,
                                subscriptions[i].Attribute("Name").Value,
                                subscriptions[i].Attribute("Id").Value);
                        }
                        var id = Utilities.GetUserInput(
                            String.Format("Pick a valid subscription number (1-{0})", subscriptions.Count));
                        int.TryParse(id, out index);
                        index = index - 1;
                        if (index >= 0 && index < subscriptions.Count)
                        {
                            validIndex = true;
                        }
                    }
                    while (!validIndex);
                    SubscriptionId = subscriptions[index].Attribute("Id").Value;
                    var certBytes = Convert.FromBase64String(subscriptions[index].
                        Attribute("ManagementCertificate").Value);
                    AzureManagementCertificate = new X509Certificate2(certBytes);
                    return true;
                }
            }
            catch (Exception ex)
            {
                LOG.Warn("Unexpected exception, try again", ex);
            }
            return false;
        }

        public static void SaveAppConfig()
        {
            try
            {
                LOG.Debug("Your current execution value are being automatically saved in the App.Config inside the bin folder. " + 
                    "This will help you resume the application or re-do actions again later.");
                AppConfigWriter.CurrentAppConfig();
                if (newRun)
                {
                    LOG.Debug("The next option is only for first time execution and allows you to save your configuration in the example project's App.config itself. " +
                        "This allows you to retain your configurations across builds or code syncs.");
                    var choice =
                        Utilities.GetUserInput(
                        "Would you like to save your Azure credentials and current run values in the example project's App.Config? (y/n)");
                    if (choice.Equals("y", StringComparison.OrdinalIgnoreCase))
                    {
                        AppConfigWriter.CurrentAppConfig(@"..\..\App.config");
                    }
                }
            }
            catch (Exception ex)
            {
                LOG.Warn("Faild to save Azure credentials.", ex);
            }
        }
    }
}
