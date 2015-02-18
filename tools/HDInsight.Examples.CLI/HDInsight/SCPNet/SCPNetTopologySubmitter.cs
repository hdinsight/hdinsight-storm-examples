using log4net;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace HDInsight.Examples.CLI
{
    /// <summary>
    /// A SCPNet topology submission client
    /// </summary>
    public class SCPNetTopologySubmitter
    {
        static readonly ILog LOG = LogManager.GetLogger(typeof(SCPNetTopologySubmitter));
        const string SCPAPI_Format_String = "/scpapi/api/SCPAPI/{0}";

        public static async Task TopologySubmit(string specFile, string resourceFile)
        {
            if (!File.Exists(specFile))
            {
                throw new FileNotFoundException("Make sure you have published the SCPNet project", specFile);
            }

            if (!File.Exists(resourceFile))
            {
                throw new FileNotFoundException("Make sure you have published the SCPNet project", resourceFile);
            }

            var specFileInfo = new FileInfo(specFile);
            var resourceFileInfo = new FileInfo(resourceFile);

            string action = string.Format("{0}", "TopologySubmit");
            HttpClientHandler handler = new HttpClientHandler()
            {
                Credentials = new NetworkCredential()
                {
                    UserName = HDInsightHelper.StormCluster.HttpUserName,
                    Password = HDInsightHelper.StormCluster.HttpPassword
                }
            };
            using (var client = new HttpClient(handler))
            {
                client.BaseAddress = new Uri(HDInsightHelper.StormCluster.ConnectionUrl);
                var wc = new WebClient();

                LOG.DebugFormat("Submitting topology - SpecFile: {0}, PackageFile: {1}", 
                    specFileInfo.FullName, specFileInfo.FullName);
                string url = string.Format(SCPAPI_Format_String, action);
                var multipart = new MultipartFormDataContent();

                var specFileContent = new FileContent(specFileInfo.FullName);
                specFileContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment")
                {
                    FileName = specFileInfo.Name
                };
                var resourceFileContent = new FileContent(resourceFileInfo.FullName);
                resourceFileContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment")
                {
                    FileName = resourceFileInfo.Name
                };

                multipart.Add(specFileContent);
                multipart.Add(resourceFileContent);

                HttpResponseMessage response = client.PostAsync(url, multipart).Result;

                LOG.Info("TopologySubmit StatusCode: " + response.StatusCode);
                string responseBody = await response.Content.ReadAsStringAsync();
                LOG.Info("TopologySubmit Response Content: ");
                var unescapedResponseBody = Regex.Unescape(responseBody);

                if (response.StatusCode == HttpStatusCode.Created)
                {
                    LOG.Info(unescapedResponseBody);
                    LOG.DebugFormat("Topology submitted successfully! - SpecFile: {0}, PackageFile: {1}",
                    specFileInfo.FullName, specFileInfo.FullName);
                }
                else
                {
                    var message = String.Format(
                        "Topology submission did return 'Created' status code. " +
                        "SpecFile: {0}, StatusCode: {1}",
                        specFile,
                        response.StatusCode);
                    LOG.Error(message);
                    LOG.Error(unescapedResponseBody);
                    throw new ApplicationException(message);
                }
            }
        }
    }
}
