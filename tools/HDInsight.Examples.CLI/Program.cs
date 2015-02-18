using CommandLine;
using log4net;
using Microsoft.ServiceBus.Messaging;
using Microsoft.WindowsAzure.Management.HDInsight;
using Microsoft.WindowsAzure.Management.Storage.Models;
using System;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

[assembly: log4net.Config.XmlConfigurator(Watch = true)]

namespace HDInsight.Examples.CLI
{
    class Program
    {
        static readonly ILog LOG = LogManager.GetLogger(typeof(Program));

        static bool fail = false;

        static void Main(string[] args)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var options = new Options();
            if (Parser.Default.ParseArguments(args, options))
            {
                LOG.InfoFormat("=".PadRight(50, '='));
                LOG.InfoFormat("Command = {0}", options.Command);
                if (!String.IsNullOrWhiteSpace(options.Name))
                {
                    LOG.InfoFormat("Name = {0}", options.Name);
                    AppConfig.AzureResourceName = options.Name;
                }
                LOG.InfoFormat("Region = {0}", options.Region);
                AppConfig.AzureResourceLocation = options.Region;
                LOG.InfoFormat("=".PadRight(50, '='));
            }

            var timer = new Timer(30000);
            timer.Elapsed += ReportProgress;

            try
            {
                switch (options.Command.ToLowerInvariant())
                {
                    case "c":
                    case "create":
                        AppConfig.SaveAppConfig();
                        timer.Start();
                        AzureStorageHelper.CreateIfNotExists();
                        Parallel.Invoke(
                            () =>
                            {
                                EventHubHelper.CreateIfNotExists();
                                LOG.Info("EventHub creation complete");
                            }
                            , () =>
                            {
                                Parallel.Invoke(
                                    () =>
                                    {
                                        HDInsightHelper.CreateStormClusterIfNotExists();
                                        LOG.Info("HDInsight Storm Cluster creation complete");
                                    },
                                    () =>
                                    {
                                        HDInsightHelper.CreateHBaseClusterIfNotExists();
                                        LOG.Info("HDInsight HBase Cluster creation complete");
                                    }
                                    );
                            }
                        );
                        timer.Stop();
                        SCPNetTopologyHelper.PrepareAndSubmitTopology();
                        stopwatch.Stop();
                        break;
                    case "d":
                    case "delete":
                        Parallel.Invoke(
                            () =>
                            {
                                EventHubHelper.DeleteIfExists();
                                LOG.Info("EventHub deletion complete");
                            }
                            , () =>
                            {
                                Parallel.Invoke(
                                    () =>
                                    {
                                        HDInsightHelper.DeleteStormClusterIfExists();
                                        LOG.Info("HDInsight Storm Cluster deletion complete");
                                    },
                                    () =>
                                    {
                                        HDInsightHelper.DeleteHBaseClusterIfExists();
                                        LOG.Info("HDInsight HBase Cluster deletion complete");
                                    }
                                    );
                            }
                        );
                        AzureStorageHelper.DeleteIfExists();
                        break;
                    case "l":
                    case "list":
                        AzureStorageHelper.ListAccounts();
                        EventHubHelper.ListEventHubs();
                        HDInsightHelper.ListClusters();
                        break;
                    default:
                        throw new ApplicationException(String.Format("Unrecognized command: {0}", options.Command));
                }
                LOG.Info("All operations completed! You can use 'list' command to list all your resources.");
                LOG.InfoFormat("Time taken: {0} secs", stopwatch.Elapsed.TotalSeconds);
            }
            catch (Exception ex)
            {
                fail = true;
                if (timer.Enabled)
                {
                    timer.Stop();
                }
                LOG.Error("One or more actions failed during execution", ex);
            }

            //You can choose to monitor HBase post deployments and topology submission
            /*
            bool monitorHBase = false;
            var monitorHBaseInput = Utilities.GetUserInput("Would you like to monitor HBase cluster for recent records?");
            if (monitorHBaseInput.Equals("y", StringComparison.OrdinalIgnoreCase) ||
                monitorHBaseInput.Equals("yes", StringComparison.OrdinalIgnoreCase))
            {
                monitorHBase = true;
            }

            if (monitorHBase)
            {
                try
                {
                    //Wait a little so that the topology is in a running state
                    Thread.Sleep(Timespan.FromMinutes(1));
                    Policy.
                        Handle<Exception>().
                        WaitAndRetry(
                        3,
                        retryCount => TimeSpan.FromMinutes(Math.Pow(1, retryCount)),
                        (exception, timeSpan, context) =>
                            {
                                LOG.Warn("Monitoring HBase failed, will retry.", exception);
                            }).
                        Execute(
                        () =>
                        {
                            HBaseReaderClient.MonitorHBase();
                        });
                }
                catch (Exception ex)
                {
                    LOG.Error("Monitoring HBase failed", ex);
                }
            }
            */

            Utilities.WaitForExit(fail);
            if (fail)
            {
                Environment.Exit(-1);
            }
        }

        /// <summary>
        /// This method invokes separate Gets on all resources to get the current information
        /// To avoid collision with member static variables, it creates its own copies of resources for reporting
        /// It is invoked based on timer raised events
        /// </summary>
        /// <param name="source"></param>
        /// <param name="e"></param>
        static void ReportProgress(Object source, ElapsedEventArgs e)
        {
            StorageAccount storageAccount = null;
            EventHubDescription eventHub = null;
            ClusterDetails stormCluster = null;
            ClusterDetails hbaseCluster = null;
            try
            {
                Parallel.Invoke(
                    () =>
                    {
                        storageAccount = AzureStorageHelper.GetAccount();
                    },
                    () =>
                    {
                        eventHub = EventHubHelper.GetEventHub();
                    },
                    () =>
                    {
                        stormCluster = HDInsightHelper.GetStormCluster();
                    },
                    () =>
                    {
                        hbaseCluster = HDInsightHelper.GetHBaseCluster();
                    });
            }
            catch { } //This is just a poller, okay to fail

            var sb = new StringBuilder();

            sb.AppendLine(Environment.NewLine + "-".PadRight(60, '-'));
            sb.AppendLine("Current Status at: ".PadRight(20) + e.SignalTime);

            sb.AppendLine("StorageAccount: ".PadRight(20) +
                (storageAccount == null ?
                    "No Status" :
                    (storageAccount.Name.PadRight(20) + " - " + storageAccount.Properties.Status.ToString())
                    )
                );
            sb.AppendLine("EventHub: ".PadRight(20) +
                (eventHub == null ?
                    "No Status" :
                    (eventHub.Path.PadRight(20) + " - " + eventHub.Status.ToString())
                )
            );

            sb.AppendLine("HDInsight Storm: ".PadRight(20) +
                (stormCluster == null ?
                    "No Status" :
                    (stormCluster.Name.PadRight(20) + " - " + stormCluster.StateString)
                )
            );

            sb.AppendLine("HDInsight HBase: ".PadRight(20) +
                (hbaseCluster == null ?
                    "No Status" :
                    (hbaseCluster.Name.PadRight(20) + " - " + hbaseCluster.StateString)
                )
            );

            sb.AppendLine("-".PadRight(60, '-') + Environment.NewLine);

            LOG.DebugFormat(sb.ToString());
        }
    }
}
