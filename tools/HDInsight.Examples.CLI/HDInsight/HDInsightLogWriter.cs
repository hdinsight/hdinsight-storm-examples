using log4net;
using Microsoft.WindowsAzure.Management.HDInsight.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HDInsight.Examples.CLI
{
    /// <summary>
    /// A customized log writer class for HDInsight SDK that routes the logs to log4net
    /// </summary>
    public class HDInsightLogWriter : ILogWriter
    {
        ILog LOG;
        Severity severity;
        Verbosity verbosity;

        public HDInsightLogWriter(ILog log, Severity severity = Severity.Warning, Verbosity verbosity = Verbosity.Minimal)
        {
            this.LOG = log;
            this.severity = severity;
            this.verbosity = verbosity;
        }

        public void Log(Severity severity, Verbosity verbosity, string content)
        {
            if (severity >= this.severity && verbosity >= this.verbosity)
            {
                switch (severity)
                {
                    case Severity.Error:
                        LOG.Error(content);
                        break;
                    case Severity.Warning:
                        LOG.Warn(content);
                        break;
                    case Severity.Critical:
                    case Severity.Informational:
                    case Severity.Message:
                        LOG.Info(content);
                        break;
                    case Severity.None:
                    default:
                        break;
                }
            }
        }
    }
}
