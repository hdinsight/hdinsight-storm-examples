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

namespace SCPNetTopologySubmitter
{
    /// <summary>
    /// A SCPNet topology submission client
    /// </summary>
    public class Program
    {
        const string SCPAPI_Format_String = "/scpapi/api/SCPAPI/{0}";

        static void Main(string[] args)
        {
            if(args.Length < 5)
            {
                Console.WriteLine("SCPNetTopologySubmitter usage:");
                Console.WriteLine("SCPNetTopologySubmitter.exe <cluster_url> <cluster_username> <cluster_password> <spec_file_path> <package_file_path>");
                Console.WriteLine();
                Environment.Exit(1);
            }

            var clusterUrl = args[0];
            var clusterUsername = args[1];
            var clusterPassword = args[2];
            var specFile = args[3];
            var packageFile = args[4];

            bool error = false;
            try
            {
                Console.WriteLine("SCPNetTopologySubmitter: Submitting topology");
                Console.WriteLine("ClusterUrl: ".PadRight(15) + clusterUrl);
                Console.WriteLine("SpecFile: ".PadRight(15) + specFile);
                Console.WriteLine("PackageFile: ".PadRight(15) + packageFile);
                Console.WriteLine();

                var task = TopologySubmit(clusterUrl, clusterUsername, clusterPassword, specFile, packageFile);
                var result = task.Wait(120000);
                if (!result)
                {
                    error = true;
                }
                else
                {
                    Console.WriteLine(task.Result);
                }
            }
            catch (Exception ex)
            {
                error = true;
                Console.WriteLine(ex.ToString());
            }

            var prevForeColor = Console.ForegroundColor;
            var prevBackColor = Console.BackgroundColor;
            Console.BackgroundColor = ConsoleColor.Black;

            if (error)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("SCPNetTopologySubmitter: ERROR - Failed to submit topology!");
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("SCPNetTopologySubmitter: SUCCESS - Topology submitted successfully!");
            }

            Console.ForegroundColor = prevForeColor;
            Console.BackgroundColor = prevBackColor;

            if (error)
            {
                Environment.Exit(1);
            }
        }

        public static async Task<string> TopologySubmit(string clusterUrl, string clusterUsername, string clusterPassword, string specFile, string packageFile)
        {
            if (!File.Exists(specFile) || !File.Exists(packageFile))
            {
                throw new FileNotFoundException("Could not find the spec file or package file. Make sure you have published the SCPNet topology project.");
            }

            var specFileInfo = new FileInfo(specFile);
            var resourceFileInfo = new FileInfo(packageFile);

            string action = string.Format("{0}", "TopologySubmit");
            HttpClientHandler handler = new HttpClientHandler()
            {
                Credentials = new NetworkCredential()
                {
                    UserName = clusterUsername,
                    Password = clusterPassword
                }
            };

            var sb = new StringBuilder();

            using (var client = new HttpClient(handler))
            {
                client.BaseAddress = new Uri(clusterUrl);
                var wc = new WebClient();

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

                sb.AppendLine("Response StatusCode: " + response.StatusCode);
                string responseBody = await response.Content.ReadAsStringAsync();
                sb.AppendLine("Response Content: ");
                var unescapedResponseBody = Regex.Unescape(responseBody);
                sb.AppendLine(unescapedResponseBody);

                if (response.StatusCode != HttpStatusCode.Created)
                {
                    sb.AppendLine(
                        String.Format(
                        "Topology submission did return 'Created' status code. " +
                        "SpecFile: {0}, StatusCode: {1}",
                        specFile,
                        response.StatusCode));
                    throw new ApplicationException(sb.ToString());
                }
            }
            return sb.ToString();
        }
    }
}
