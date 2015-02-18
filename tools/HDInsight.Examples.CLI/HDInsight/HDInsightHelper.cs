using log4net;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Management.HDInsight;
using Microsoft.WindowsAzure.Management.HDInsight.ClusterProvisioning.Data;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace HDInsight.Examples.CLI
{
    /// <summary>
    /// A static helper class that helps in creating HDInsight Storm & HBase clusters and get their information
    /// </summary>
    public static class HDInsightHelper
    {
        static readonly ILog LOG = LogManager.GetLogger(typeof(HDInsightHelper));

        static IHDInsightClient _client = null;
        public static IHDInsightClient Client
        {
            get
            {
                if (_client == null)
                {
                    LOG.Info("Connecting to HDInsight Service.");
                    _client = HDInsightClient.Connect(new HDInsightCertificateCredential()
                    {
                        Certificate = AppConfig.AzureManagementCertificate,
                        SubscriptionId = new Guid(AppConfig.SubscriptionId)
                    });
                    LOG.Info("Connected to HDInsight Service.");
                    _client.AddLogWriter(new HDInsightLogWriter(LOG));
                }
                return _client;
            }
            set
            {
                _client = value;
            }
        }

        static string _stormClusterName = null;
        public static string StormClusterName
        {
            get
            {
                if (string.IsNullOrWhiteSpace(_stormClusterName))
                {
                    _stormClusterName = (AppConfig.AzureResourceName + ClusterType.Storm.ToString()).ToLowerInvariant();
                }
                return _stormClusterName;
            }
            set
            {
                _stormClusterName = value.ToLowerInvariant();
            }
        }

        public static ClusterDetails StormCluster { get; set; }

        static string _hbaseClusterName = null;
        public static string HBaseClusterName
        {
            get
            {
                if (string.IsNullOrWhiteSpace(_hbaseClusterName))
                {
                    _hbaseClusterName = (AppConfig.AzureResourceName + ClusterType.HBase.ToString()).ToLowerInvariant();
                }
                return _hbaseClusterName;
            }
            set
            {
                _hbaseClusterName = value.ToLowerInvariant();
            }
        }
        public static ClusterDetails HBaseCluster { get; set; }

        public static int HDInsightClusterSize = 4;
        
        public static ClusterDetails CreateStormClusterIfNotExists()
        {
            StormCluster = CreateClusterIfNotExists(ClusterType.Storm);
            return StormCluster;
        }

        public static ClusterDetails CreateHBaseClusterIfNotExists()
        {
            HBaseCluster = CreateClusterIfNotExists(ClusterType.HBase);
            return HBaseCluster;
        }

        public static ClusterDetails CreateClusterIfNotExists(ClusterType clusterType)
        {
            var name = AppConfig.AzureResourceName + clusterType.ToString().ToLowerInvariant();
            LOG.Info(String.Format("Creating Cluster: Name = {0}, Type = {1}", name, clusterType));

            var parameters = new ClusterCreateParametersV2()
            {
                Name = name,
                ClusterSizeInNodes = HDInsightClusterSize,
                Location = AppConfig.AzureResourceLocation,
                ClusterType = clusterType,
                HeadNodeSize = "Large",
                DataNodeSize = "Large",
                ZookeeperNodeSize = "Medium",
                UserName = AppConfig.AzureResourceUsername,
                Password = AppConfig.AzureResourcePassword,
                DefaultStorageAccountName = AzureStorageHelper.Name,
                DefaultStorageAccountKey = AzureStorageHelper.PrimaryKey,
                DefaultStorageContainer = AppConfig.AzureResourceName,
                Version = "3.2",
                //VirtualNetworkId = VirtualNetworkHelper.VNetId,
                //SubnetName = VirtualNetworkHelper.SubnetName,
            };
            return CreateClusterIfNotExists(parameters);
        }

        public static ClusterDetails CreateClusterIfNotExists(ClusterCreateParametersV2 parameters)
        {
            try
            {
                LOG.InfoFormat("Checking if the HDInsight cluster exists - SubscriptionId: {0}, Name: {1}", 
                    AppConfig.SubscriptionId, parameters.Name);

                var cluster = Client.GetCluster(parameters.Name);
                if (cluster == null)
                {
                    LOG.InfoFormat("Submitting a new HDInsight cluster create request - - SubscriptionId: {0}, Name: {1}",
                        AppConfig.SubscriptionId, parameters.Name);
                    cluster = Client.CreateCluster(parameters);
                }
                else
                {
                    LOG.InfoFormat("Skipping creation, found an existing HDInsight Cluster with same name. Name: {0}", parameters.Name);
                }
                if (cluster.State != ClusterState.Operational && cluster.State != ClusterState.Running)
                {
                    throw new ApplicationException(
                        String.Format("HDInsight Cluster is not in a operational or running state. Name: {0}, State: {1}", 
                        cluster.Name, cluster.StateString));
                }
                LOG.InfoFormat("HDInsight cluster created. Details:\r\n{0}", ClusterDetailsAsString(cluster));
                return cluster;
            }
            catch (Exception ex)
            {
                LOG.Error(
                    String.Format("Failed to create cluster - SubscriptionId: {0}, Name: {1}", AppConfig.SubscriptionId, parameters.Name),
                    ex);
                throw;
            }
        }

        public static void DeleteStormClusterIfExists()
        {
            DeleteClusterIfExists(ClusterType.Storm);
        }

        public static void DeleteHBaseClusterIfExists()
        {
            DeleteClusterIfExists(ClusterType.HBase);
        }

        public static void DeleteClusterIfExists(ClusterType clusterType)
        {
            var name = AppConfig.AzureResourceName + clusterType.ToString().ToLowerInvariant();
            LOG.Info(String.Format("Deleting Cluster: Name = {0}, Type = {1}", name, clusterType));
            DeleteClusterIfExists(name);
        }

        public static void DeleteClusterIfExists(string name)
        {
            try
            {
                LOG.Info("Deleting HDInsight cluster: " + name);
                Client.DeleteCluster(name);
                LOG.Info("Deleted HDInsight cluster: " + name);
            }
            catch (HDInsightClusterDoesNotExistException)
            {
                LOG.InfoFormat("Success! HDInsight cluster does not exist - Name: {0}", name);
            }
            catch (Exception ex)
            {
                LOG.Error(String.Format("Error occured while deleting HDInsight cluster - Name: {0}", name), ex);
                throw;
            }
        }

        public static ClusterDetails GetStormCluster()
        {
            return GetCluster(StormClusterName);
        }

        public static ClusterDetails GetHBaseCluster()
        {
            return GetCluster(HBaseClusterName);
        }

        public static ClusterDetails GetCluster(string name)
        {
            return Client.GetCluster(name);
        }

        public static void ListClusters()
        {
            try
            {
                LOG.InfoFormat("Listing HDInsight clusters - SubscriptionId: {0}", Client.Credentials.SubscriptionId);
                var clusters = Client.ListClusters();

                LOG.InfoFormat("HDInsight Clusters found - Count : {0}", clusters.Count);

                var i = 1;
                foreach (var c in clusters)
                {
                    LOG.InfoFormat("Cluster {0}:\r\n{1}", i++, ClusterDetailsAsString(c));
                }
            }
            catch (Exception ex)
            {
                LOG.Error(
                    String.Format("Failed to get cluster information - SubscriptionId: {0}", AppConfig.SubscriptionId), 
                    ex);
                throw;
            }
        }

        public static string ClusterDetailsAsString(ClusterDetails c)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Name: ".PadRight(30) + c.Name);
            sb.AppendLine("Location: ".PadRight(30) + c.Location);
            sb.AppendLine("ClusterType: ".PadRight(30) + c.ClusterType);
            sb.AppendLine("Version: ".PadRight(30) + c.Version);
            sb.AppendLine("ClusterSizeInNodes: ".PadRight(30) + c.ClusterSizeInNodes);
            sb.AppendLine("State: ".PadRight(30) + c.State);
            sb.AppendLine("CreatedDate: ".PadRight(30) + c.CreatedDate);
            sb.AppendLine("DefaultStorageAccount: ".PadRight(30) + (c.DefaultStorageAccount == null ? "null" : c.DefaultStorageAccount.Name));
            sb.AppendLine("ConnectionUrl: ".PadRight(30) + c.ConnectionUrl);
            sb.AppendLine("HttpUserName: ".PadRight(30) + c.HttpUserName);
            sb.AppendLine("HttpPassword: ".PadRight(30) + c.HttpPassword);
            if (!String.IsNullOrWhiteSpace(c.VirtualNetworkId))
            {
                sb.AppendLine("VirtualNetworkId: ".PadRight(30) + c.VirtualNetworkId);
            }
            if (!String.IsNullOrWhiteSpace(c.SubnetName))
            {
                sb.AppendLine("SubnetName: ".PadRight(30) + c.SubnetName);
            }
            return sb.ToString();
        }
    }
}
