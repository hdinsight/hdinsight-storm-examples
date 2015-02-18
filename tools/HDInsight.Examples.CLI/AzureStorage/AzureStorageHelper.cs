using Hyak.Common;
using log4net;
using Microsoft.Azure;
using Microsoft.WindowsAzure.Management.Storage;
using Microsoft.WindowsAzure.Management.Storage.Models;
using Polly;
using System;
using System.Text;

namespace HDInsight.Examples.CLI
{
    /// <summary>
    /// A static helper class that provides methods to create Azure Storage accounts and get their information
    /// </summary>
    public static class AzureStorageHelper
    {
        static readonly ILog LOG = LogManager.GetLogger(typeof(AzureStorageHelper));

        static CertificateCloudCredentials _certificateCredentials = null;
        public static CertificateCloudCredentials CertificateCredentials
        {
            get
            {
                if (_certificateCredentials == null)
                {
                    _certificateCredentials = new CertificateCloudCredentials(AppConfig.SubscriptionId, AppConfig.AzureManagementCertificate);
                }
                return _certificateCredentials;
            }
            set
            {
                _certificateCredentials = value;
            }
        }

        static StorageManagementClient _smClient = null;
        public static StorageManagementClient SMClient
        {
            get
            {
                if (_smClient == null)
                {
                    _smClient = new StorageManagementClient(CertificateCredentials);
                }
                return _smClient;
            }
            set
            {
                _smClient = value;
            }
        }

        public static StorageAccount Account { get; set; }
        public static string Name { get; set; }
        public static string PrimaryKey { get; set; }

        public static void CreateIfNotExists()
        {
            try
            {
                CheckNameAvailabilityResponse availabilityResponse = null;
                LOG.InfoFormat("Checking Availability - StorageAccount: {0}, Region: {1}", AppConfig.AzureResourceName, AppConfig.AzureResourceLocation);
                Policy.
                    Handle<Exception>().
                    WaitAndRetry(
                        5, 
                        retryCount => TimeSpan.FromSeconds(Math.Pow(5, retryCount))).
                    Execute(
                    () =>
                    {
                        availabilityResponse = SMClient.StorageAccounts.CheckNameAvailability(AppConfig.AzureResourceName);
                    }
                );
                LOG.InfoFormat("Checked Availability - StorageAccount: {0}, Available: {1}", AppConfig.AzureResourceName, availabilityResponse.IsAvailable);
                if (!availabilityResponse.IsAvailable)
                {
                    LOG.WarnFormat("Unavailable - StorageAccount: {0}", AppConfig.AzureResourceName);
                    LOG.InfoFormat("Checking existence in current subscription - SubscriptionId: {0}, StorageAccount: {1}",
                        AppConfig.SubscriptionId, AppConfig.AzureResourceName);
                }
                else
                {
                    LOG.InfoFormat("Creating StorageAccount - StorageAccount: {0}, Available: {1}", AppConfig.AzureResourceName, availabilityResponse.IsAvailable);
                    var operationResponse = SMClient.StorageAccounts.Create(new Microsoft.WindowsAzure.Management.Storage.Models.StorageAccountCreateParameters()
                    {
                        AccountType = "Standard_GRS",
                        Name = AppConfig.AzureResourceName,
                        Location = AppConfig.AzureResourceLocation,
                    });
                    LOG.InfoFormat("Created StorageAccount - StorageAccount: {0}, Status: {1}", AppConfig.AzureResourceName, operationResponse.Status);
                }

                LOG.InfoFormat("Getting StorageAccount Information - StorageAccount: {0}", AppConfig.AzureResourceName);
                Policy
                  .Handle<Exception>()
                  .WaitAndRetry(
                    10,
                    retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    (exception, timeSpan, context) =>
                    {
                        LOG.Warn(String.Format("Failed to get StorageAccount information, will retry - SubscriptionId: {0}, StorageAccount: {1}",
                            AppConfig.SubscriptionId, AppConfig.AzureResourceName), exception);
                    }
                  )
                  .Execute(
                    () =>
                    {
                        var storageAccountResponse = SMClient.StorageAccounts.Get(AppConfig.AzureResourceName);
                        Account = storageAccountResponse.StorageAccount;
                        Name = storageAccountResponse.StorageAccount.Name;
                        var storageAccountKeyResponse = SMClient.StorageAccounts.GetKeys(AppConfig.AzureResourceName);
                        PrimaryKey = storageAccountKeyResponse.PrimaryKey;
                    });
            }
            catch (Exception ex)
            {
                LOG.Error(String.Format("Unable to get storage information - SubscriptionId: {0}, StorageAccount: {1}",
                    AppConfig.SubscriptionId, AppConfig.AzureResourceName), ex);
                throw;
            }

            if (String.IsNullOrWhiteSpace(Name) || String.IsNullOrWhiteSpace(PrimaryKey))
            {
                throw new ApplicationException(String.Format("Failed to get details of StorageAccount: {0}", AppConfig.AzureResourceName));
            }

            LOG.InfoFormat("Found Storage Details - Name: {0}, PrimaryKey: {1}", Name, PrimaryKey);
        }

        public static void DeleteIfExists()
        {
            LOG.InfoFormat("Deleting StorageAccount - SubscriptionId: {0}, StorageAccount: {1}",
                            AppConfig.SubscriptionId, AppConfig.AzureResourceName);

            try
            {
                Policy
                  .Handle<Exception>()
                  .WaitAndRetry(
                    3,
                    retryAttempt => TimeSpan.FromSeconds(Math.Pow(5, retryAttempt)),
                    (exception, timeSpan, context) =>
                    {
                        LOG.Warn(String.Format("Delete failed, will retry - SubscriptionId: {0}, StorageAccount: {1}",
                            AppConfig.SubscriptionId, AppConfig.AzureResourceName), exception);
                    }
                  )
                  .Execute(
                    () =>
                    {
                        try
                        {
                            var response = SMClient.StorageAccounts.Delete(AppConfig.AzureResourceName);
                        }
                        catch (CloudException cex)
                        {
                            if (cex.Response.StatusCode == System.Net.HttpStatusCode.NotFound)
                            {
                                LOG.InfoFormat("Not Found - SubscriptionId: {0}, StorageAccount: {1}, Response: {2}",
                                    AppConfig.SubscriptionId, AppConfig.AzureResourceName, cex.Response.StatusCode);
                            }
                            else
                            {
                                throw;
                            }
                        }
                        LOG.InfoFormat("Delete successful - SubscriptionId: {0}, StorageAccount: {1}",
                            AppConfig.SubscriptionId, AppConfig.AzureResourceName);
                    });
            }
            catch (Exception ex)
            {
                LOG.Error(String.Format("Failed to delete StorageAccount - SubscriptionId: {0}, StorageAccount: {1}",
                    AppConfig.SubscriptionId, AppConfig.AzureResourceName), ex);
                throw;
            }
        }

        public static StorageAccount GetAccount()
        {
            return GetAccount(AppConfig.AzureResourceName);
        }

        public static StorageAccount GetAccount(string name)
        {
            try
            {
                var response = SMClient.StorageAccounts.Get(name);
                return response.StorageAccount;
            }
            catch(CloudException cex)
            {
                if (cex.Response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    LOG.InfoFormat("Not Found - SubscriptionId: {0}, StorageAccount: {1}, Response: {2}",
                        AppConfig.SubscriptionId, AppConfig.AzureResourceName, cex.Response.StatusCode);
                }
            }
            return null;
        }

        public static void ListAccounts()
        {
            LOG.InfoFormat("Listing Storage Accounts - SubscriptionId: {0}", SMClient.Credentials.SubscriptionId);
            var response = SMClient.StorageAccounts.List();
            var i = 1;
            foreach (var sa in response.StorageAccounts)
            {
                LOG.InfoFormat("StorageAccount {0}:\r\n{1}", i++, StorageAccountAsString(sa));
            }
        }

        public static string StorageAccountAsString(StorageAccount sa)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Name: ".PadRight(30) + sa.Name);
            sb.AppendLine("Location: ".PadRight(30) + sa.Properties.Location);
            return sb.ToString();
        }
    }
}