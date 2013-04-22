using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using Mono.Options;
using murray.common;
using murray.common.winservice;

namespace keepitup
{
    /// <summary>
    /// Try to keep a windows service running. Start/Restart it.
    /// </summary>
    class Program
    {
        enum ExitCode
        {
            Success = 0,
            Warning = 1,
            Error = 2
        }

        private static bool _showDebug;

        static int Main(string[] args)
        {

            var _startTime = DateTime.Now;
            bool showHelp = false;
            _showDebug = false;
            bool pauseWhenFinished = false;

            
            string serviceName = null;
            string machineName = null;
            int timeoutMilliseconds = 60000;
            bool stopService = false;


            var p = new OptionSet() {
                { "s|serviceName=", "[required, string=name of service]",  x => serviceName = x },
                { "m|machineName=", "[optional, string=name of machine that service is running on]",  x => machineName = x },
                { "t|timeoutMilliseconds=", "[optional, force a recalculation of all images, default=" + timeoutMilliseconds + "]",   x => timeoutMilliseconds = str.ToInt(x)},
                { "stop|stopService", "[optional, override - stops the service]",   x => stopService = str.ToBool(stopService)},
                

                //standard options for command line utils
                { "d|debug", "[optional, show debug details (verbose), default="+_showDebug + "]",   x => _showDebug = x != null},
                { "pause|pauseWhenFinished", "[optional, pause output window with a ReadLine when finished, default="+pauseWhenFinished + "]",   x => pauseWhenFinished = (x != null)},
                { "h|?|help", "show the help options",   x => showHelp = x != null },
            };
            List<string> extraArgs = p.Parse(args);


            if (showHelp)
            {
                p.WriteOptionDescriptions(Console.Out);
                return (int)ExitCode.Warning;
            }


            Console.WriteLine();
            Console.WriteLine("keepitup by @AaronKMurray");
            if (_showDebug)
            {
                Console.WriteLine("using options:");
                Console.WriteLine("\t serviceName:\t" + serviceName);
                Console.WriteLine("\t machineName:\t" + (machineName ?? "[local: "+server.GetMachineName() +"]"));
                Console.WriteLine("\t timeoutMilliseconds:\t" + timeoutMilliseconds);
                Console.WriteLine();
            }

            var exitCode = ExitCode.Error;

            ServiceController service = null;
            try
            {
                service = WinServiceHelper.GetServiceController(serviceName, machineName);

                ServiceControllerStatus status = ServiceControllerStatus.Stopped; //default value in case exception occurs

                try
                {
                    status = WinServiceHelper.GetServiceStatus(service);

                    if (WinServiceHelper.IsServiceRunning(status))
                    {
                        if (stopService)
                        {
                            //special case
                            exitCode = DoStopService(service, timeoutMilliseconds);
                        }
                        else
                        {
                            //good/normal case - service is up and running
                            exitCode = ExitCode.Success;
                        }
                    }
                    else
                    {
                        //bad case - service not running
                        exitCode = DoRestartService(service, timeoutMilliseconds);
                    }

                }
                catch (Exception ex)
                {
                    exitCode = ExitCode.Error;
                    if (_showDebug)
                        Console.WriteLine("Error trying to get service [{0} on '{1}'] status: {2}, error: {3}", serviceName, machineName ?? "local", status, ex.Message);
                }

            }
            catch (Exception ex)
            {
                exitCode = ExitCode.Error;
                if (_showDebug)
                    Console.WriteLine("Error trying to connect to service [{0} on '{1}']: {2}", serviceName, machineName ?? "local", ex.Message);
            }

   


            Console.WriteLine();


            if (_showDebug)
            {
                Console.WriteLine("Complete at " + DateTime.Now.ToLongTimeString() + ". Took " + DateTime.Now.Subtract(_startTime).TotalSeconds + " seconds to run");
            }

            if (pauseWhenFinished)
            {
                Console.WriteLine("Press any key to complete");
                Console.ReadLine(); //just here to pause the output window during testing
            }
            return (int)exitCode;

        }



        private static ExitCode DoRestartService(ServiceController pService, int timeoutMilliseconds)
        {
            var exitCode = ExitCode.Error;
            try
            {
                var started = WinServiceHelper.RestartService(pService, timeoutMilliseconds);
                if (started)
                {
                    exitCode = ExitCode.Success;
                }
                else
                {
                    exitCode = ExitCode.Error;
                    if (_showDebug)
                        Console.WriteLine("Unable to restart service. Status: {0}", pService.Status);
                }
            }
            catch (Exception ex)
            {
                exitCode = ExitCode.Error;
                if (_showDebug)
                    Console.WriteLine("Error trying to restart service: {0}", ex.Message);
            }
            return exitCode;
        }

        private static ExitCode DoStopService(ServiceController pService, int timeoutMilliseconds)
        {
            var exitCode = ExitCode.Error;
            try
            {
                var stopped = WinServiceHelper.StopService(pService, timeoutMilliseconds);
                if (stopped)
                {
                    exitCode = ExitCode.Success;
                }
                else
                {
                    exitCode = ExitCode.Error;
                    if (_showDebug)
                        Console.WriteLine("Unable to stop service. Status: {0}", pService.Status);
                }
            }
            catch (Exception ex)
            {
                exitCode = ExitCode.Error;
                if (_showDebug)
                    Console.WriteLine("Error trying to stop service: {0}", ex.Message);
            }
            return exitCode;
        }

    }
}
