using log4net;
using System;
using System.Diagnostics;

namespace HDInsight.Examples.CLI
{
    /// <summary>
    /// A utility methods class
    /// </summary>
    public static class Utilities
    {
        static readonly ILog LOG = LogManager.GetLogger(typeof(Utilities));

        public static string GetUserInput(string message)
        {
            LOG.InfoFormat("Getting user input for message: {0}", message);
            var prevColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write(message + " :  ");
            var answer = Console.ReadLine();
            Console.ForegroundColor = prevColor;
            Console.WriteLine();
            return answer;
        }

        public static void WaitForExit(bool forceWait = false)
        {
            if (Debugger.IsAttached || forceWait)
            {
                Console.WriteLine("Press a key to exit...");
                Console.ReadKey();
            }
        }

        /// <summary>
        /// Blocking executable launch
        /// </summary>
        /// <param name="exePath"></param>
        /// <param name="exeArgs"></param>
        /// <param name="workingDir"></param>
        public static void RunExecutable(string exePath, string exeArgs, string workingDir = null)
        {
            LOG.InfoFormat("Running executable - Path: {0}, Args: {1}, WorkingDir: {2}",
                exePath, exeArgs, workingDir);

            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.CreateNoWindow = false;
            startInfo.UseShellExecute = false;
            startInfo.FileName = exePath;
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;
            startInfo.Arguments = exeArgs;

            if (!String.IsNullOrWhiteSpace(workingDir))
            {
                startInfo.WorkingDirectory = workingDir;
            }

            try
            {
                using (Process exeProcess = Process.Start(startInfo))
                {
                    exeProcess.WaitForExit();
                    if (exeProcess.ExitCode != 0)
                    {
                        var message = String.Format("Executable returned non-zero exit code. Path: {0}, Code: {1}",
                            exePath, exeProcess.ExitCode);
                        throw new ApplicationException(message);
                    }
                }
            }
            catch(Exception ex)
            {
                var message = String.Format("Executable failed. Path: {0}, Args: {1}, WorkingDir: {2}",
                    exePath, exeArgs, workingDir);
                LOG.Error(message, ex);
                throw new ApplicationException(message, ex);
            }

            LOG.InfoFormat("Executable run successfully - Path: {0}, Args: {1}, WorkingDir: {2}",
                exePath, exeArgs, workingDir);
        }

        /// <summary>
        /// Non blocking process launch
        /// </summary>
        /// <param name="exePath"></param>
        /// <param name="exeArgs"></param>
        /// <param name="workingDir"></param>
        public static void LaunchProcess(string exePath, string exeArgs, string workingDir = null)
        {
            LOG.InfoFormat("Launch Process - Path: {0}, Args: {1}, WorkingDir: {2}",
                exePath, exeArgs, workingDir);

            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = exePath;
            startInfo.Arguments = exeArgs;

            if (!String.IsNullOrWhiteSpace(workingDir))
            {
                startInfo.WorkingDirectory = workingDir;
            }

            try
            {
                Process exeProcess = Process.Start(startInfo);
            }
            catch (Exception ex)
            {
                var message = String.Format("Launch failed. Path: {0}, Args: {1}, WorkingDir: {2}",
                    exePath, exeArgs, workingDir);
                LOG.Error(message, ex);
                throw new ApplicationException(message, ex);
            }

            LOG.InfoFormat("Process launched successfully - Path: {0}, Args: {1}, WorkingDir: {2}",
                exePath, exeArgs, workingDir);
        }
    }
}
