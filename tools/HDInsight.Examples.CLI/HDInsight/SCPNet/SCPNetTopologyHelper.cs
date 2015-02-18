using log4net;
using System;
using System.Configuration;
using System.IO;

namespace HDInsight.Examples.CLI
{
    /// <summary>
    /// A helper class with methods to update SCPHost.exe.config for SCPNet topologies
    /// And methods for preparation and submission of SCPNet topologies
    /// </summary>
    public static class SCPNetTopologyHelper
    {
        static readonly ILog LOG = LogManager.GetLogger(typeof(SCPNetTopologyHelper));

        public static void PrepareAndSubmitTopology()
        {
            PrepareEHTopologyConfig();
            GenerateSCPNetTopologyPackagesAndSubmit();
        }

        public static void PrepareEHTopologyConfig()
        {
            LOG.Info("Preparing SCPHost config for topology submission");
            if (!File.Exists(AppConfig.SCPHostConfigFilePath))
            {
                var message = String.Format("Cannot find SCPHost.exe.config. Path: {0}", AppConfig.SCPHostConfigFilePath);
                LOG.Error(message);
                throw new ApplicationException(message);
            }

            LOG.Info("Creating backup of SCPHost config");
            File.Copy(AppConfig.SCPHostConfigFilePath, AppConfig.SCPHostConfigFilePath + ".bak", true);

            //Update the config in project and also update in bin dir
            AppConfigWriter.UpdateSCPHostConfig(
                //AppConfig.SCPHostConfigFilePath, 
                Path.Combine(AppConfig.SCPNetTopologyProjectBinPath, AppConfig.SCPHostConfigFilename));
        }

        public static void GenerateSCPNetTopologyPackagesAndSubmit()
        {
            LOG.Info("Generating SCP Packages for topology submission");

            var topologyClasses = ConfigurationManager.AppSettings["SCPNetTopologyClasses"].
                Split(new char[] {',',';'}, StringSplitOptions.RemoveEmptyEntries);

            try
            {
                //Let's try to delete some unneccassary files from the build to keep package size smaller
                LOG.InfoFormat("Deleting ScpPackage directory");

                var scpPackagePath = Path.Combine(AppConfig.SCPNetTopologyProjectBinPath, "ScpPackage");
                if (Directory.Exists(scpPackagePath))
                {
                    Directory.Delete(Path.Combine(AppConfig.SCPNetTopologyProjectBinPath, "ScpPackage"), true);
                }
            }
            catch { } //This error can be ignored
            
            foreach(var topologyClass in topologyClasses)
            {
                var topologyClassParts = topologyClass.Split(new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
                var topologyName = topologyClassParts[topologyClassParts.Length - 1];

                var scpCArgs = string.Format("generate-spec -assembly {0} -spec {1}.spec -class {2}",
                    Path.Combine(AppConfig.SCPNetTopologyProjectBinPath, "EventHubAggregatorToHBaseTopology.dll"),
                    topologyName,
                    topologyClass);
                Utilities.RunExecutable(AppConfig.SCPNetCompilerPath, scpCArgs);

                scpCArgs = string.Format("package -cSharpTarget {0} -packageFile {1}.zip  -javaDependencies {2}",
                    Path.Combine(AppConfig.SCPNetTopologyProjectBinPath),
                    topologyName,
                    Path.Combine(AppConfig.SCPNetTopologyDir, "jars")
                    );
                Utilities.RunExecutable(AppConfig.SCPNetCompilerPath, scpCArgs);
            }
            LOG.Info("SCP packages generated successfully!");

            foreach (var topologyClass in topologyClasses)
            {
                var topologyClassParts = topologyClass.Split(new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
                var topologyName = topologyClassParts[topologyClassParts.Length - 1];

                LOG.InfoFormat("Submitting topology: {0}", topologyName);
                var task = SCPNetTopologySubmitter.TopologySubmit(topologyName + ".spec", topologyName + ".zip");
                var result = task.Wait(60000);
                if (result)
                {
                    LOG.InfoFormat("Submitted topology: {0}", topologyName);
                }
                else
                {
                    LOG.ErrorFormat("Failed to submit topology: {0}", topologyName);
                }
            }
        }
    }
}
