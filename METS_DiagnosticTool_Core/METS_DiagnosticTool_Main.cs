using METS_DiagnosticTool_Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Topshelf;

namespace METS_DiagnosticTool_Core
{
    class METS_DiagnosticTool_Main : ServiceControl
    {
        // Local Input Parameters
        private static string _amsAddress;
        private static string _amsPort;

        private static bool rabbitMQ_InitializedOK = false;
        private static bool twincat_InitializedOK = false;

        #region Constructor
        public METS_DiagnosticTool_Main(string amsAddress, string amsPort)
        {
            _amsAddress = amsAddress;
            _amsPort = amsPort;
        }
        #endregion

        #region Windows Service Start Stop Methods
        public bool Start(HostControl hostControl)
        {
            Logger.Log(Logger.logLevel.Information, string.Concat("METS Diagnostic Tool started with parameters:",
                                                            Environment.NewLine, "ADS Ip ", Utility.CheckStringEmpty(_amsAddress),
                                                            Environment.NewLine, "ADS Port ", Utility.CheckStringEmpty(_amsPort)), Logger.logEvents.Starting);

            // Initialize Rabbit MQ Server
            rabbitMQ_InitializedOK = RabbitMQHelper.InitializeServer();

            if (rabbitMQ_InitializedOK)
            {
                // Initialuize Twincat
                twincat_InitializedOK = TwincatHelper.TwincatInitialization(_amsAddress, _amsPort);

                if (twincat_InitializedOK)
                    Logger.Log(Logger.logLevel.Information, "METS Diagnostic Tool Started OK", Logger.logEvents.StartedSuccesfully);
                else
                    Logger.Log(Logger.logLevel.Error, "METS Diagnostic Tool Started error", Logger.logEvents.StartedError);
            }
            else
                Logger.Log(Logger.logLevel.Error, "METS Diagnostic Tool Started error", Logger.logEvents.StartedError);

            return true;
        }

        public bool Stop(HostControl hostControl)
        {
            if (rabbitMQ_InitializedOK)
                RabbitMQHelper.CloseServerConnection();

            if (twincat_InitializedOK)
                TwincatHelper.Dispose();

            Logger.Log(Logger.logLevel.Information, "METS Diagnostic Tool stopped succesfully", Logger.logEvents.StoppedSuccesfully);

            return true;
        }
        #endregion
    }
}
