using METS_DiagnosticTool_Utilities;
using METS_DiagnosticTool_Utilities.SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Topshelf;
using static METS_DiagnosticTool_Utilities.VariableConfigurationHelper;

namespace METS_DiagnosticTool_Core
{
    class METS_DiagnosticTool_Main : ServiceControl
    {
        #region Private Fields
        // Threads
        private static Thread LoggingThread;

        // Threads Cancellation Tokens
        private static CancellationTokenSource loggingThread_Work_CancellationToken;

        // Local Input Parameters
        private static string _uiFullPath;
        private static string _corePath;
        private static string _amsAddress;
        private static string _amsPort;

        private static RpcServer rabbitMQ_Server = null;
        private static bool twincat_InitializedOK = false;
        #endregion

        #region Constructor
        public METS_DiagnosticTool_Main(string corePath, string uiFullPath, string amsAddress, string amsPort)
        {
            _corePath = corePath;
            _uiFullPath = uiFullPath;
            _amsAddress = amsAddress;
            _amsPort = amsPort;
        }
        #endregion

        #region Windows Service Start Stop Methods
        public bool Start(HostControl hostControl)
        {
            Logger.Log(Logger.logLevel.Information, string.Concat("METS Diagnostic Tool started with parameters:",
                                                            Environment.NewLine, "Core Path ", Utility.CheckStringEmpty(_corePath),
                                                            Environment.NewLine, "UI Path ", Utility.CheckStringEmpty(_uiFullPath),
                                                            Environment.NewLine, "ADS Ip ", Utility.CheckStringEmpty(_amsAddress),
                                                            Environment.NewLine, "ADS Port ", Utility.CheckStringEmpty(_amsPort)), Logger.logEvents.Starting);

            // Initialize Rabbit MQ Server
            rabbitMQ_Server = RabbitMQHelper.InitializeServer();

            if (rabbitMQ_Server != null)
            {
                // Attach Event that PLC Variable Configuration has been Triggered
                rabbitMQ_Server.PLCVariableConfigurationTriggered += RabbitMQ_Server_PLCVariableConfigurationTriggered;

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

        private void RabbitMQ_Server_PLCVariableConfigurationTriggered(object sender, string e)
        {
            // Decode given message
            List<string> _splitMessage = e.Split(';').ToList();

            // Create dictionary
            Dictionary<string, string> _variableConfiguration = new Dictionary<string, string>();
            foreach (string _item in _splitMessage)
            {
                string[] _config = _item.Split('$').ToArray();
                if (!_variableConfiguration.ContainsKey(_config[0]))
                    _variableConfiguration.Add(_config[0], _config[1]);
            }

            // Create new Variable Config
            bool trigger = bool.Parse(_variableConfiguration["Trigger"]);

            VariableConfig variableConfig = new VariableConfig
            {
                variableAddress = _variableConfiguration["VariableAddress"],
                pollingRefreshTime = int.Parse(_variableConfiguration["PollingRefreshTime"]),
                recording = bool.Parse(_variableConfiguration["Recording"])
            };
            bool loggingTypeParsed = Enum.TryParse(_variableConfiguration["LoggingType"], out LoggingType _loggingType);
            variableConfig.loggingType = loggingTypeParsed ? _loggingType : LoggingType.OnChange;


            // When Received Trigger True then Variable Config has been Saved
            // When Received False then Variable Config is in Edit Mode

            // Here, start logging a variable based on given configuration
            if (trigger)
            {
                // Start Logging Thread if Recording is active
                if (variableConfig.recording)
                {
                    //Logger.Log(Logger.logLevel.Warning, string.Concat("Received Trigger TRUE, trying to start Logging PLC Variable ", variableConfig.variableAddress), Logger.logEvents.Blank);

                    loggingThread_Work_CancellationToken = new CancellationTokenSource();
                    LoggingThread = new Thread(() => LogginThread_Work(loggingThread_Work_CancellationToken.Token, variableConfig))
                    {
                        Priority = ThreadPriority.Normal
                    };
                    LoggingThread.Start();
                }
                else
                {
                    //Logger.Log(Logger.logLevel.Warning, string.Concat("Received Trigger FALSE, trying to stop Logging PLC Variable ", variableConfig.variableAddress), Logger.logEvents.Blank);

                    // Stop Logging Thread
                    if (loggingThread_Work_CancellationToken != null)
                    {
                        loggingThread_Work_CancellationToken.Cancel(true);
                        loggingThread_Work_CancellationToken.Token.WaitHandle.WaitOne();
                        loggingThread_Work_CancellationToken.Dispose();
                    }
                }
            }
            else
            {
                //Logger.Log(Logger.logLevel.Warning, string.Concat("Received Trigger FALSE, trying to stop Logging PLC Variable ", variableConfig.variableAddress), Logger.logEvents.Blank);

                // Stop Logging Thread
                if (loggingThread_Work_CancellationToken != null)
                {
                    loggingThread_Work_CancellationToken.Cancel(true);
                    loggingThread_Work_CancellationToken.Token.WaitHandle.WaitOne();
                    loggingThread_Work_CancellationToken.Dispose();
                }
            }
        }

        public bool Stop(HostControl hostControl)
        {
            if (rabbitMQ_Server != null)
            {
                rabbitMQ_Server.PLCVariableConfigurationTriggered -= RabbitMQ_Server_PLCVariableConfigurationTriggered;
                RabbitMQHelper.CloseServerConnection();
            }

            if (twincat_InitializedOK)
                TwincatHelper.Dispose();

            Logger.Log(Logger.logLevel.Information, "METS Diagnostic Tool stopped succesfully", Logger.logEvents.StoppedSuccesfully);

            return true;
        }
        #endregion

        #region LoggingThread
        private static void LogginThread_Work(CancellationToken cancelToken, VariableConfig variableConfig)
        {
            try
            {
                bool bLock = false;
                string _value = string.Empty;

                Logger.Log(Logger.logLevel.Information, string.Concat("Logging started for PLC Variable ", variableConfig.variableAddress, " with Logging Configuration", Environment.NewLine,
                                                                        "Logging Type ", variableConfig.loggingType == LoggingType.Polling ? string.Concat("Polling with Refresh Time ", variableConfig.pollingRefreshTime.ToString(),"ms") : "On Change"),
                                                                        Logger.logEvents.LoggingStoppedForAVariable);

                while (true)
                {
                    cancelToken.ThrowIfCancellationRequested();

                    // Here Read the PLC Value of the given Variable
                    switch (variableConfig.loggingType)
                    {
                        case LoggingType.Polling:

                            _value = TwincatHelper.ReadPLCValues(variableConfig.variableAddress, true).ToString();

                            // And do the logging to the SQLite
                            SQLiteHelper.SaveData(new PLCVariableDataModel { VariableName = variableConfig.variableAddress, VariableValue = string.IsNullOrEmpty(_value) ? "string.Empty" : _value, UpdateDate = DateTime.Now.ToString("dd.MM.yyyy"), UpdateTime = DateTime.Now.ToString("HH:mm:ss.fff") });

                            Task.Delay(variableConfig.pollingRefreshTime).Wait();
                            break;

                        case LoggingType.OnChange:

                            _value = TwincatHelper.ReadPLCValues(variableConfig.variableAddress, true).ToString();

                            PLCVariableDataModel _lastValueModel = SQLiteHelper.GetLastRow(variableConfig.variableAddress);

                            if (_lastValueModel != null)
                            {
                                string _lastValue = _lastValueModel.VariableValue;

                                if (_value != _lastValue && !string.IsNullOrEmpty(_value) && !string.IsNullOrEmpty(_lastValue))
                                {
                                    SQLiteHelper.SaveData(new PLCVariableDataModel { VariableName = variableConfig.variableAddress, VariableValue = string.IsNullOrEmpty(_value) ? "string.Empty" : _value, UpdateDate = DateTime.Now.ToString("dd.MM.yyyy"), UpdateTime = DateTime.Now.ToString("HH:mm:ss.fff") });
                                    bLock = false;
                                }
                                else if (string.IsNullOrEmpty(_value) || string.IsNullOrEmpty(_lastValue))
                                {
                                    // If String empty insert it only once
                                    if (!bLock)
                                    {
                                        SQLiteHelper.SaveData(new PLCVariableDataModel { VariableName = variableConfig.variableAddress, VariableValue = string.IsNullOrEmpty(_value) ? "string.Empty" : _value, UpdateDate = DateTime.Now.ToString("dd.MM.yyyy"), UpdateTime = DateTime.Now.ToString("HH:mm:ss.fff") });
                                        bLock = true;
                                    }
                                }
                            }
                            else
                                SQLiteHelper.SaveData(new PLCVariableDataModel { VariableName = variableConfig.variableAddress, VariableValue = string.IsNullOrEmpty(_value) ? "string.Empty" : _value, UpdateDate = DateTime.Now.ToString("dd.MM.yyyy"), UpdateTime = DateTime.Now.ToString("HH:mm:ss.fff") });

                            break;

                        default:
                            break;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                Logger.Log(Logger.logLevel.Information, string.Concat("Logging stopped for PLC Variable ", variableConfig.variableAddress) , Logger.logEvents.LoggingStoppedForAVariable);
            }
        }
        #endregion
    }
}
