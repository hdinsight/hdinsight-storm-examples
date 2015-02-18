using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace EventHubAggregatorToHBaseTopology.Common
{
    public class WebRequestLog
    {
        public static Random random = new Random();

        public string Timestamp { get; set; }
        public string Client { get; set; }
        public string Uri { get; set; }
        public string Method { get; set; }
        public string Result { get; set; }

        public static List<string> RandomClientIps = new List<string>() { "127.0.0.1", "10.9.8.7", "192.168.1.1", "255.255.255.255", "123.123.123.123" };
        public static List<string> RandomUris = new List<string>() { "/foo", "/bar", "/foo/bar", "/spam", "/eggs", "/spam/eggs" };
        public static List<string> RandomMethods = new List<string>() { "GET", "PUT", "DELETE" };
        public static List<string> RandomResponses = new List<string>() { 
                HttpStatusCode.OK.ToString(),
                HttpStatusCode.BadRequest.ToString(),
                HttpStatusCode.NotFound.ToString(),
                HttpStatusCode.InternalServerError.ToString()};

        public static WebRequestLog GetRandomWebRequestLog()
        {
            return new WebRequestLog()
            {
                Timestamp = DateTime.UtcNow.ToString(),
                Client = WebRequestLog.RandomClientIps[random.Next(WebRequestLog.RandomClientIps.Count)],
                Uri = WebRequestLog.RandomUris[random.Next(WebRequestLog.RandomUris.Count)],
                Method = WebRequestLog.RandomMethods[random.Next(WebRequestLog.RandomMethods.Count)],
                Result = WebRequestLog.RandomResponses[random.Next(WebRequestLog.RandomResponses.Count)]
            };
        }
        public static string GetRandomWebRequestLogAsJson()
        {
            var wrl = GetRandomWebRequestLog();
            return JsonConvert.SerializeObject(wrl);
        }
    }
}
