using CommandLine;
using CommandLine.Text; 

namespace HDInsight.Examples.CLI
{
    /// <summary>
    /// Command line options
    /// </summary>
    class Options
    {
        [Option('m', "mode", Required = true, HelpText = "Execution mode. e.g. Create, Delete, List")]
        public string Command { get; set; }

        [Option('n', "name", Required = false, HelpText = "Azure Resource name (in full), which you may want to use from a previous run.")]
        public string Name { get; set; }

        [Option('r', "region", Required = false, DefaultValue = "West US", HelpText = "Azure Region to create resources in.")]
        public string Region { get; set; }

        [HelpOption]
        public string GetUsage()
        {
            return HelpText.AutoBuild(this,
              (HelpText current) => HelpText.DefaultParsingErrorsHandler(this, current));
        }
    }
}
