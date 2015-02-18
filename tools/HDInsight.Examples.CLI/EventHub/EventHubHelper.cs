using log4net;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using Microsoft.Azure;
using Microsoft.WindowsAzure.Management.ServiceBus;
using Microsoft.WindowsAzure.Management.ServiceBus.Models;
using System;
using Polly;
using Hyak.Common;
using System.Text;
using System.Threading;

namespace HDInsight.Examples.CLI
{
    /// <summary>
    /// A static class that provides helper methods to create Service Bus Namespace, EventHub and get their information
    /// </summary>
    public static class EventHubHelper
    {
        static readonly ILog LOG = LogManager.GetLogger(typeof(EventHubHelper));

        static CertificateCloudCredentials _certificateCredentials = null;
        public static CertificateCloudCredentials CertificateCredentials
        {
            get
            {
                if(_certificateCredentials == null)
                {
                    _certificateCredentials = 
                        new CertificateCloudCredentials(AppConfig.SubscriptionId, AppConfig.AzureManagementCertificate);
                }
                return _certificateCredentials;
            }
            set
            {
                _certificateCredentials = value;
            }
        }

        static ServiceBusManagementClient _sbClient;
        public static ServiceBusManagementClient SBClient
        {
            get
            {
                if (_sbClient == null)
                {
                    _sbClient = new ServiceBusManagementClient(CertificateCredentials);
                }
                return _sbClient;
            }
            set
            {
                _sbClient = value;
            }
        }

        public static ServiceBusNamespace SBNamespace { get; set; }
        public static NamespaceManager NSManager { get; set; }

        static EventHubDescription _ehDescription = null;
        public static EventHubDescription EHDescription
        {
            get
            {
                if (_ehDescription == null)
                {
                    EHDescription = new EventHubDescription(AppConfig.AzureResourceName)
                    {
                        MessageRetentionInDays = 1,
                        PartitionCount = 16,
                    };
                }
                return _ehDescription;
            }
            set
            {
                _ehDescription = value;
            }
        }

        static string _sbNamespaceName = null;
        public static string SBNamespaceName
        {
            get
            {
                if (String.IsNullOrWhiteSpace(_sbNamespaceName))
                {
                    _sbNamespaceName = AppConfig.AzureResourceName + "-ns";
                }
                return _sbNamespaceName;
            }
            set
            {
                _sbNamespaceName = value;
            }
        }

        public static string EHRuleName = "root";
        public static string EHRuleKey;

        public static string Status { get; set; }

        public static void CreateIfNotExists()
        {
            EHRuleKey = SharedAccessAuthorizationRule.GenerateRandomKey();
            EHDescription.Authorization.Add(new SharedAccessAuthorizationRule(EHRuleName, EHRuleKey, new AccessRights[] { AccessRights.Manage, AccessRights.Listen, AccessRights.Send }));

            try
            {
                LOG.InfoFormat("Checking Availability - ServiceBusNamespace: {0}, Region: {1}", SBNamespaceName, AppConfig.AzureResourceLocation);
                var availabilityResponse = SBClient.Namespaces.CheckAvailability(SBNamespaceName);
                LOG.InfoFormat("Checked Availability - ServiceBusNamespace: {0}, Available: {1}", SBNamespaceName, availabilityResponse.IsAvailable);
                if (!availabilityResponse.IsAvailable)
                {
                    LOG.WarnFormat("Unavailable - ServiceBusNamespace: {0}", SBNamespaceName);
                    LOG.InfoFormat("Checking existence of the namespace in current subscription - SubscriptionId: {0}, ServiceBusNamespace: {1}",
                        AppConfig.SubscriptionId, SBNamespaceName);
                }
                else
                {
                    var namespaceResponse = SBClient.Namespaces.Create(SBNamespaceName, AppConfig.AzureResourceLocation);
                    SBNamespace = namespaceResponse.Namespace;
                    LOG.InfoFormat("Created Namespace - ServiceBusNamespace: {0}, Region: {1}, Status: {2}", SBNamespace.Name, SBNamespace.Region, SBNamespace.Status);
                    //Sleep a while to let namespace actually be active
                    Thread.Sleep(60000);
                }
            }
            catch (Exception ex)
            {
                LOG.Error(
                    String.Format("Unable to create ServiceBusNamespace - SubscriptionId: {0}, SBNamespaceName: {1}",
                    AppConfig.SubscriptionId, SBNamespaceName),
                    ex);
                throw;
            }

            LOG.InfoFormat("Getting Namespace Details - ServiceBusNamespace: {0}", SBNamespaceName);
            var namespaceConnectionString = GetNamespaceConnectionString(SBNamespaceName);
            LOG.InfoFormat("Found Namespace Details - ConnectionString: {0}", namespaceConnectionString);

            NSManager = NamespaceManager.CreateFromConnectionString(namespaceConnectionString);

            try
            {
                if (NSManager.EventHubExists(EHDescription.Path))
                {
                    EHDescription = NSManager.GetEventHub(EHDescription.Path);
                    SharedAccessAuthorizationRule rule;
                    EHDescription.Authorization.TryGetSharedAccessAuthorizationRule(EHRuleName, out rule);
                    EHRuleKey = rule.PrimaryKey;
                }
                else
                {
                    LOG.InfoFormat("Creating EventHub - Path: {0}, PartitionCount: {1}, MessageRetentionInDays: {2}",
                        EHDescription.Path, EHDescription.PartitionCount, EHDescription.MessageRetentionInDays);

                    Policy
                      .Handle<Exception>()
                      .WaitAndRetry(
                        3,
                        retryAttempt => TimeSpan.FromSeconds(Math.Pow(15, retryAttempt)),
                        (exception, timeSpan, context) =>
                        {
                            LOG.Warn(String.Format("Create failed, will retry - SubscriptionId: {0}, SBNamespaceName: {1}, EHPath: {2}",
                                AppConfig.SubscriptionId, SBNamespaceName, EHDescription.Path), exception);
                        }
                      )
                      .Execute(
                        () =>
                        {
                            EHDescription = NSManager.CreateEventHubIfNotExists(EHDescription);
                        }
                      );
                    LOG.InfoFormat("Created EventHub - Path: {0}, Status: {1}", EHDescription.Path, EHDescription.Status);
                }
                LOG.InfoFormat("EventHub Details - Path: {0}, SAS Name: {1}, SAS Key: {2}", EHDescription.Path, EHRuleName, EHRuleKey);
            }
            catch (Exception ex)
            {
                LOG.Error(String.Format("Failed to create EventHub - SubscriptionId: {0}, SBNamespaceName: {1}. EventHubName: {2}",
                    AppConfig.SubscriptionId, SBNamespaceName, EHDescription.Path), ex);
                throw;
            }
        }

        public static string GetNamespaceConnectionString(string namespaceName)
        {
            var namespaceConnectionString = string.Empty;
            try
            {
                ServiceBusNamespaceDescriptionResponse namespaceDescriptionResponse;

                //It takes some time for service bus to register the namespace, so let's retry until we succeeed in getting the details
                Policy
                  .Handle<Exception>()
                  .WaitAndRetry(
                    10,
                    retryAttempt => TimeSpan.FromSeconds(Math.Pow(5, retryAttempt)),
                    (exception, timeSpan, context) =>
                    {
                        LOG.InfoFormat("GetNamespaceDescription failed, will retry - SubscriptionId: {0}, SBNamespaceName: {1}",
                                AppConfig.SubscriptionId, namespaceName);
                    }
                  )
                  .Execute(
                    () =>
                    {
                        namespaceDescriptionResponse = SBClient.Namespaces.GetNamespaceDescription(namespaceName);
                        namespaceConnectionString = namespaceDescriptionResponse.NamespaceDescriptions[0].ConnectionString;
                    }
                  );
            }
            catch (Exception ex)
            {
                LOG.Error(
                    String.Format("Unable to get ServiceBusNamespace information - SubscriptionId: {0}, SBNamespaceName: {1}",
                    AppConfig.SubscriptionId, namespaceName),
                    ex);
                throw;
            }

            if (String.IsNullOrWhiteSpace(namespaceConnectionString))
            {
                throw new ApplicationException(String.Format("Failed to get details of ServiceBusNamespace: {0}", namespaceName));
            }
            return namespaceConnectionString;
        }

        public static EventHubDescription GetEventHub()
        {
            return GetEventHub(SBNamespaceName, EHDescription.Path);
        }

        public static EventHubDescription GetEventHub(string namespaceName, string ehPath)
        {
            var nsManager = NamespaceManager.CreateFromConnectionString(GetNamespaceConnectionString(namespaceName));
            var ehDescription = nsManager.GetEventHub(ehPath);
            return ehDescription;
        }

        public static void ListEventHubs()
        {
            LOG.InfoFormat("Listing ServiceBus Namespaces - SubscriptionId: {0}", SBClient.Credentials.SubscriptionId);
            var response = SBClient.Namespaces.List();
            var i = 1;
            foreach (var ns in response.Namespaces)
            {
                LOG.InfoFormat("\r\nNamespace {0}:\r\n{1}", i++, ServiceBusNamespaceAsString(ns));
                NSManager = NamespaceManager.CreateFromConnectionString(GetNamespaceConnectionString(ns.Name));
                var eventHubs = NSManager.GetEventHubs();
                var j = 1;
                foreach (var eh in eventHubs)
                {
                    LOG.InfoFormat("\tEventHub {0}:\r\n\t{1}", j++, EventHubDescriptionAsString(eh));
                }
            }
        }

        public static void DeleteIfExists()
        {
            try
            {
                Policy
                  .Handle<Exception>()
                  .WaitAndRetry(
                    3,
                    retryAttempt => TimeSpan.FromSeconds(Math.Pow(5, retryAttempt)),
                    (exception, timeSpan, context) =>
                    {
                        LOG.Warn(String.Format("Delete failed, will retry - SubscriptionId: {0}, SBNamespaceName: {1}",
                            AppConfig.SubscriptionId, SBNamespaceName), exception);
                    }
                  )
                  .Execute(
                    () =>
                    {
                        try
                        {
                            var response = SBClient.Namespaces.Delete(SBNamespaceName);
                        }
                        catch (CloudException cex)
                        {
                            if (cex.Response.StatusCode == System.Net.HttpStatusCode.NotFound)
                            {
                                LOG.InfoFormat("Not Found - SubscriptionId: {0}, SBNamespaceName: {1}, Response: {2}",
                                    AppConfig.SubscriptionId, SBNamespaceName, cex.Response.StatusCode);
                            }
                            else
                            {
                                throw;
                            }
                        }
                        LOG.InfoFormat("Delete successful - SubscriptionId: {0}, SBNamespaceName: {1}",
                            AppConfig.SubscriptionId, SBNamespaceName);
                    }
                  );
            }
            catch (Exception ex)
            {
                LOG.Error(String.Format("Failed to delete ServiceBusNamespace - SubscriptionId: {0}, SBNamespaceName: {1}",
                    AppConfig.SubscriptionId, SBNamespaceName), ex);
                throw;
            }
        }

        public static string ServiceBusNamespaceAsString(ServiceBusNamespace ns)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Name: ".PadRight(30) + ns.Name);
            sb.AppendLine("Region: ".PadRight(30) + ns.Region);
            sb.AppendLine("NamespaceType: ".PadRight(30) + ns.NamespaceType);
            sb.AppendLine("ServiceBusEndpoint: ".PadRight(30) + ns.ServiceBusEndpoint);
            sb.AppendLine("Status: ".PadRight(30) + ns.Status);
            sb.AppendLine("CreatedAt: ".PadRight(30) + ns.CreatedAt);
            return sb.ToString();
        }

        public static string EventHubDescriptionAsString(EventHubDescription ehd)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Path: ".PadRight(30) + ehd.Path);
            sb.AppendLine("PartitionCount: ".PadRight(30) + ehd.PartitionCount);
            sb.AppendLine("MessageRetentionInDays: ".PadRight(30) + ehd.MessageRetentionInDays);
            sb.AppendLine("Status: ".PadRight(30) + ehd.Status);
            sb.AppendLine("CreatedAt: ".PadRight(30) + ehd.CreatedAt);
            return sb.ToString();
        }
    }
}
