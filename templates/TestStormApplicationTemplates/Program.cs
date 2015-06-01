using Microsoft.SCP;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestStormApplicationTemplates
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Test_AzureDocumentDBWriterStormApplication.Generator();
            //Test_AzureDocumentDBWriterStormApplication.Run();
            
            Test_AzureDocumentDBLookupStormApplication.Generator();
            //Test_AzureDocumentDBLookupStormApplication.Run();

            Test_AzureEventHubsWriterStormApplication.Generator();
            //Test_AzureEventHubsWriterStormApplication.Run();

            Test_AzureHDInsightHBaseWriterStormApplication.Generator();
            //Test_AzureHDInsightHBaseWriterStormApplication.Run();

            Test_AzureHDInsightHBaseLookupStormApplication.Generator();
            //Test_AzureHDInsightHBaseLookupStormApplication.Run();

            Test_SqlAzureWriterStormApplication.Generator();
            //Test_SqlAzureWriterStormApplication.Run();

            Console.WriteLine("Press a key to exit...");
            Console.ReadKey();
        }
    }
}
