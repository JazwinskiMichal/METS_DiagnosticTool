using Ads.Client.Common;
using Ads.Client.Winsock;
using METS_DiagnosticTool_Utilities;
using METS_DiagnosticTool_Utilities.SQLite;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
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
        #region Threads
        // Logging Thread
        private static Thread LoggingThread;
        private static CancellationTokenSource loggingThread_Work_CancellationToken;

        // Ads Client Thread
        private static Thread AdsClientThread;
        private static CancellationTokenSource adsClientThread_Work_CancellationToken;
        private static ObservableCollection<VariableConfig> plcVariablesToBeRead = new ObservableCollection<VariableConfig>();

        // Dictionary of Canecalltion Tokens
        private static Dictionary<string, CancellationTokenSource> dicCancellationTokens = new Dictionary<string, CancellationTokenSource>();
        #endregion

        // Notification Handles
        private static Dictionary<string, uint> dicNotificationHandles = new Dictionary<string, uint>();

        // Local Input Parameters
        private static string _uiFullPath;
        private static string _corePath;
        private static string _amsAddress;
        private static string _amsPort;

        private static RpcServer rabbitMQ_Server = null;
        private static bool twincat_InitializedOK = false;
        private static bool twincatADS_InitializedOK = false;
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

                // Attach Event that PLC Variable has been Deleted
                rabbitMQ_Server.PLCVariableDeleted += RabbitMQ_Server_PLCVariableDeleted;

                // Initialize ADS Client Thread (separate Thread that ADS Client lives on)
                try
                {
                    adsClientThread_Work_CancellationToken = new CancellationTokenSource();
                    AdsClientThread = new Thread(() => ADSClientThread_Work(adsClientThread_Work_CancellationToken.Token))
                    {
                        Priority = ThreadPriority.Normal
                    };
                    AdsClientThread.Start();

                    twincatADS_InitializedOK = true;
                }
                catch (Exception ex)
                {
                    Logger.Log(Logger.logLevel.Error, string.Concat("Starting ADS Client Thread exception ", ex.ToString()), Logger.logEvents.Blank);
                }
                
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
            if (rabbitMQ_Server != null)
            {
                rabbitMQ_Server.PLCVariableConfigurationTriggered -= RabbitMQ_Server_PLCVariableConfigurationTriggered;
                rabbitMQ_Server.PLCVariableDeleted -= RabbitMQ_Server_PLCVariableDeleted;
                RabbitMQHelper.CloseServerConnection();
            }

            // Dispose Logging
            foreach (CancellationTokenSource cts in dicCancellationTokens.Values)
            {
                if (cts != null)
                {
                    // Cancel the Token
                    cts.Cancel(true);
                    cts.Token.WaitHandle.WaitOne();
                    cts.Dispose();
                }
            }

            // And Clear the Dictionary of cancellation tokens
            dicCancellationTokens.Clear();

            // And Clear the Dictionary of Notification Handles
            dicNotificationHandles.Clear();

            // And Stop ADS Client Thread
            if (adsClientThread_Work_CancellationToken != null)
            {
                // Cancel the Token
                adsClientThread_Work_CancellationToken.Cancel(true);
                adsClientThread_Work_CancellationToken.Token.WaitHandle.WaitOne();
                adsClientThread_Work_CancellationToken.Dispose();
            }

            if (twincat_InitializedOK)
                TwincatHelper.Dispose();

            Logger.Log(Logger.logLevel.Information, "METS Diagnostic Tool stopped succesfully", Logger.logEvents.StoppedSuccesfully);

            return true;
        }
        #endregion

        #region RabbitMQ_Events
        private void RabbitMQ_Server_PLCVariableDeleted(object sender, string e)
        {
            // Get from the Message just PLC variable Name

            // Decode given message: XMLFileFullPath$value;VariableAddress$value
            List<string> _splitMessage = e.Split(';').ToList();

            // Create dictionary
            Dictionary<string, string> _variableConfiguration = new Dictionary<string, string>();
            foreach (string _item in _splitMessage)
            {
                string[] _config = _item.Split('$').ToArray();
                if (!_variableConfiguration.ContainsKey(_config[0]))
                    _variableConfiguration.Add(_config[0], _config[1]);
            }

            string variableAddress = _variableConfiguration["VariableAddress"];

            // Stop Logging Thread for Selected PLC Variable
            if (dicCancellationTokens.ContainsKey(variableAddress))
            {
                if (dicCancellationTokens[variableAddress] != null)
                {
                    // Cancel the Token
                    dicCancellationTokens[variableAddress].Cancel(true);
                    dicCancellationTokens[variableAddress].Token.WaitHandle.WaitOne();
                    dicCancellationTokens[variableAddress].Dispose();

                    // And Remove from Dictionary
                    dicCancellationTokens.Remove(variableAddress);
                }
            }

            // And also Remove the whole Table from SQLite
            SQLiteHelper.DeleteTable(_corePath, variableAddress);
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
                    loggingThread_Work_CancellationToken = new CancellationTokenSource();
                    LoggingThread = new Thread(() => LogginThread_Work(loggingThread_Work_CancellationToken.Token, variableConfig))
                    {
                        Priority = ThreadPriority.Normal
                    };
                    LoggingThread.Start();

                    // Collect Token to Dictionary
                    if (!dicCancellationTokens.ContainsKey(variableConfig.variableAddress))
                        dicCancellationTokens.Add(variableConfig.variableAddress, loggingThread_Work_CancellationToken);
                }
                else
                {
                    // Stop Logging Thread
                    if (dicCancellationTokens.ContainsKey(variableConfig.variableAddress))
                    {
                        if (dicCancellationTokens[variableConfig.variableAddress] != null)
                        {
                            // Cancel the Token
                            dicCancellationTokens[variableConfig.variableAddress].Cancel(true);
                            dicCancellationTokens[variableConfig.variableAddress].Token.WaitHandle.WaitOne();
                            dicCancellationTokens[variableConfig.variableAddress].Dispose();

                            // And Remove from Dictionary
                            dicCancellationTokens.Remove(variableConfig.variableAddress);
                        }
                    }
                }
            }
            else
            {
                // Stop Logging Thread
                if (dicCancellationTokens.ContainsKey(variableConfig.variableAddress))
                {
                    if (dicCancellationTokens[variableConfig.variableAddress] != null)
                    {
                        // Cancel the Token
                        dicCancellationTokens[variableConfig.variableAddress].Cancel(true);
                        dicCancellationTokens[variableConfig.variableAddress].Token.WaitHandle.WaitOne();
                        dicCancellationTokens[variableConfig.variableAddress].Dispose();

                        // And Remove from Dictionary
                        dicCancellationTokens.Remove(variableConfig.variableAddress);
                    }
                }
            }
        }
        #endregion

        #region ADS Client Thread
        private static void ADSClientThread_Work(CancellationToken cancelToken, string plcAMSNetID = "192.168.1.1.1.1", string plcIp = "192.168.1.12", ushort plcAMSPort = 851, string localAMSNetID = "192.168.1.65.2.1")
        {
            using (AdsClient client = new AdsClient(
                     amsNetIdSource: localAMSNetID,
                     ipTarget: plcIp,
                     amsNetIdTarget: plcAMSNetID,
                     amsPortTarget: plcAMSPort))
            {
                // Attach Notification Event
                client.OnNotification += Client_OnNotification;

                // Attach Notification to Observable Collection of PLC Variables to be Read
                plcVariablesToBeRead.CollectionChanged += (sender, e) => {
                   
                    if (e.Action == NotifyCollectionChangedAction.Add)
                    {
                        if (e.NewItems != null)
                        {
                            if (e.NewItems.Count > 0)
                            {
                                VariableConfig _givenVariableConfig = (VariableConfig)e.NewItems[0];
                                if (!dicNotificationHandles.ContainsKey(_givenVariableConfig.variableAddress))
                                {
                                    // Check logging Type
                                    switch (_givenVariableConfig.loggingType)
                                    {
                                        case LoggingType.Polling:
                                            dicNotificationHandles.Add(_givenVariableConfig.variableAddress, client.AddNotification<byte>(client.GetSymhandleByName(_givenVariableConfig.variableAddress), AdsTransmissionMode.Cyclic, (uint)_givenVariableConfig.pollingRefreshTime, _givenVariableConfig.variableAddress));
                                            Logger.Log(Logger.logLevel.Information, string.Concat("Added Notification info for PLC Variable Address ", _givenVariableConfig.variableAddress), Logger.logEvents.Blank);
                                            break;
                                        case LoggingType.OnChange:
                                            dicNotificationHandles.Add(_givenVariableConfig.variableAddress, client.AddNotification<byte>(client.GetSymhandleByName(_givenVariableConfig.variableAddress), AdsTransmissionMode.OnChange, 1, _givenVariableConfig.variableAddress));
                                            Logger.Log(Logger.logLevel.Information, string.Concat("Added Notification info for PLC Variable Address ", _givenVariableConfig.variableAddress), Logger.logEvents.Blank);
                                            break;
                                        default:
                                            break;
                                    }
                                }
                            }
                        }
                    }
                    else if (e.Action == NotifyCollectionChangedAction.Remove)
                    {
                        if (e.OldItems != null)
                        {
                            if (e.OldItems.Count > 0)
                            {
                                VariableConfig _givenVariableConfig = (VariableConfig)e.OldItems[0];
                                if (dicNotificationHandles.ContainsKey(_givenVariableConfig.variableAddress))
                                {
                                    // Remove the ADS Notification and from the Dictionary of Notification
                                    client.DeleteNotification(dicNotificationHandles[_givenVariableConfig.variableAddress]);

                                    dicNotificationHandles.Remove(_givenVariableConfig.variableAddress);
                                }
                            }
                        }
                    }
                };

                try
                {
                    while (true)
                    {
                        cancelToken.ThrowIfCancellationRequested();

                        // This loop is just to keep ADS Client Alive
                        // Do nothing, maybe sleep? But would it affect the Ads NOtifications?
                    }
                }
                catch (OperationCanceledException)
                {
                    // Detach Ads Notification Event
                    client.OnNotification -= Client_OnNotification;

                    // Delete Active Ads Notifications
                    client.DeleteActiveNotifications();

                    Logger.Log(Logger.logLevel.Information, "ADS Client Thread Stopped", Logger.logEvents.Blank);
                }
            }
        }

        private static void Client_OnNotification(object sender, AdsNotificationArgs e)
        {
            SQLiteHelper.SaveData(new PLCVariableDataModel { VariableName = e.Notification.UserData.ToString(), VariableValue = e.Notification.Value.ToString().ToLower(), UpdateDate = DateTime.Now.ToString("dd.MM.yyyy"), UpdateTime = DateTime.Now.ToString("HH:mm:ss.fff") });
        }
        #endregion

        #region LoggingThread
        private static void LogginThread_Work(CancellationToken cancelToken, VariableConfig variableConfig)
        {
            // Here just add a Variable to Observable Collection to indicate to ADS Client Thread that you want this particular Variable to be read
            try
            {
                while (true)
                {
                    cancelToken.ThrowIfCancellationRequested();

                    // Check does the Observable plcVariablesCollection not cointain given Variable Address
                    if (!plcVariablesToBeRead.Contains(variableConfig))
                    {
                        Logger.Log(Logger.logLevel.Warning, string.Concat("Trying to add to Observable Collection ", variableConfig.variableAddress), Logger.logEvents.Blank);
                        plcVariablesToBeRead.Add(variableConfig);
                    }
                        
                }
            }
            catch (OperationCanceledException)
            {
                if (plcVariablesToBeRead.Contains(variableConfig))
                {
                    Logger.Log(Logger.logLevel.Warning, string.Concat("Trying to remove from Observable Collection ", variableConfig.variableAddress), Logger.logEvents.Blank);
                    plcVariablesToBeRead.Remove(variableConfig);
                }
                    

                Logger.Log(Logger.logLevel.Information, string.Concat("Logging stopped for PLC Variable ", variableConfig.variableAddress), Logger.logEvents.LoggingStoppedForAVariable);
            }
        }
        #endregion
    }
}
