﻿using METS_DiagnosticTool_Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Topshelf;
using Topshelf.StartParameters;

namespace METS_DiagnosticTool_Core
{
    class Program
    {
        private static string _bridgeUIFullPath = @"C:\Users\MIHOW\source\repos\METS_DiagnosticTool\METS_DiagnosticTool\bin\Release\METS_DiagnosticTool_UI.exe";
        private static string _amsAddress = "192.168.1.11.1.1";
        private static string _amsPort = "801";

        private static Dictionary<string, string> inputParameters;

        private static bool doingUninstall;
        private static bool killUI;

        static void Main(string[] args)
        {
            // Check are we doing uninstall of the Bridge
            doingUninstall = UIHelper.CheckUninstall(args);

            // Get All Input Parameters
            inputParameters = UIHelper.GetInputParameters(System.Reflection.Assembly.GetExecutingAssembly().Location, args);

            // Main Topshelf Magic code
            TopshelfExitCode exitCode = HostFactory.Run(x =>
            {
                x.EnableStartParameters();
                x.Service<METS_DiagnosticTool_Main>(s =>
                {
                    s.ConstructUsing(metsDiagnosticTool => new METS_DiagnosticTool_Main(_amsAddress, _amsPort));
                    s.WhenStarted((metsDiagnosticTool, hostControl) => metsDiagnosticTool.Start(hostControl));
                    s.WhenStopped((metsDiagnosticTool, hostControl) => metsDiagnosticTool.Stop(hostControl));
                });

                x.WithStartParameter("ADSIp", n => _amsAddress = n);
                x.WithStartParameter("ADSPort", n => _amsPort = n);

                x.RunAsLocalSystem();
                x.StartManually();

                x.AfterUninstall(() =>
                {
                    // Clear Windows Event Logs
                    Logger.ClearAll();

                    // Kill every instance of the UI
                    killUI = true;
                });

                x.OnException(ex =>
                {
                    Logger.Log(Logger.logLevel.Error, string.Concat("General exception ", ex.ToString()), Logger.logEvents.ServiceGeneralException);
                });

                x.SetServiceName("METSDiagnosticTool");
                x.SetDisplayName("METS Diagnostic Tool");
                x.SetDescription("Example Description");

                x.UnhandledExceptionPolicy = Topshelf.Runtime.UnhandledExceptionPolicyCode.LogErrorOnly;
            });

            int exitCodeValue = (int)Convert.ChangeType(exitCode, exitCode.GetTypeCode());
            Environment.ExitCode = exitCodeValue;

            // Start UI
            // But not when doing uninstall
            if (!doingUninstall)
            {
                UIHelper.StartUI(inputParameters["-BridgeCorePath:"],
                                             string.IsNullOrEmpty(inputParameters["-BridgeUIPath:"]) ? _bridgeUIFullPath : inputParameters["-BridgeUIPath:"],
                                             string.IsNullOrEmpty(inputParameters["-ADSIp:"]) ? _amsAddress : inputParameters["-ADSIp:"],
                                             string.IsNullOrEmpty(inputParameters["-ADSPort:"]) ? _amsPort : inputParameters["-ADSPort:"]);
            }

            if (killUI)
                UIHelper.KillUI();
        }
    }
}
