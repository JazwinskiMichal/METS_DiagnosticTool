using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace METS_DiagnosticTool_Utilities
{
    public class Logger
    {
        private static EventLog EventLog;
        private static string ServiceName = string.Empty;
        private static string ServiceName_Core = "METS_DiagnosticTool_Core";
        private static string ServiceName_UI = "METS_DiagnosticTool_UI";
        private static string ServiceLogName = "METS_DiagnosticTool";

        public enum logLevel
        {
            Error,
            Warning,
            Information
        }

        public enum logEvents
        {
            Blank = 1,

            #region Errors
            ServiceGeneralException = 99,

            StartedError = 100,

            TwincatADSFailedToConnect = 108,

            TwinatADSReadException = 113,
            TwinatADSWriteException = 114,
            TwincatADSConnectionStateException = 115,

            BridgePLCWatchdogException = 116,
            PLCNotInRunMode = 117,

            RabbitMQServerInitailiztionError = 200,
            RabbitMQServerErrorWhenReceivingMessage = 201,
            RabbitMQServerCloseConnectionError = 202,
            RabbitMQClientInitailiztionError = 203,
            RabbitMQClientCloseConnectionError = 204,

            SaveVariableConfigurationError = 300,
            ReadVariableConfigurationError = 301,
            #endregion

            #region Warnings

            #endregion

            #region Info
            Starting = 300,
            StartedSuccesfully = 301,
            StoppedSuccesfully = 302,
            TwincatADSConnectionOk = 303
            #endregion
        }

        /// <summary>
        /// Method to Log message to Event Log
        /// </summary>
        /// <param name="_logLevel">Log Level of the message</param>
        /// <param name="_message">Message in String format</param>
        /// <param name="_eventID">Event ID, to be selected from predefined Enums</param>
        public static void Log(logLevel _logLevel, string _message, logEvents _eventID, TwincatHelper.G_ET_EndPoint endPoint = TwincatHelper.G_ET_EndPoint.DiagnosticToolCore)
        {
            // Create an instance of StringBuilder. This class is in System.Text namespace
            StringBuilder sbMessage = new StringBuilder();
            sbMessage.Append(_message);

            switch (endPoint)
            {
                case TwincatHelper.G_ET_EndPoint.DiagnosticToolCore:
                    ServiceName = ServiceName_Core;
                    break;
                case TwincatHelper.G_ET_EndPoint.DiagnosticToolUI:
                    ServiceName = ServiceName_UI;
                    break;
                default:
                    ServiceName = ServiceName_Core;
                    break;
            }

            EventLog = new EventLog
            {
                Source = ServiceName,
                Log = ServiceLogName
            };

            // If the Event log source exists
            if (!EventLog.SourceExists(ServiceName))
            {
                EventLog.CreateEventSource(EventLog.Source, EventLog.Log);
            }

            // Write the exception details to the event log as an error
            switch (_logLevel)
            {
                case logLevel.Error:
                    EventLog.WriteEntry(sbMessage.ToString(), EventLogEntryType.Error, (int)_eventID);
                    break;
                case logLevel.Warning:
                    EventLog.WriteEntry(sbMessage.ToString(), EventLogEntryType.Warning, (int)_eventID);
                    break;
                case logLevel.Information:
                    EventLog.WriteEntry(sbMessage.ToString(), EventLogEntryType.Information, (int)_eventID);
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// Method to Clear Event Log
        /// </summary>
        public static void ClearAll(TwincatHelper.G_ET_EndPoint endPoint = TwincatHelper.G_ET_EndPoint.DiagnosticToolCore)
        {
            switch (endPoint)
            {
                case TwincatHelper.G_ET_EndPoint.DiagnosticToolCore:
                    ServiceName = ServiceName_Core;
                    break;
                case TwincatHelper.G_ET_EndPoint.DiagnosticToolUI:
                    ServiceName = ServiceName_UI;
                    break;
                default:
                    ServiceName = ServiceName_Core;
                    break;
            }

            EventLog = new EventLog
            {
                Source = ServiceName,
                Log = ServiceLogName
            };

            // If the Event log source exists
            if (EventLog.SourceExists(ServiceName))
            {
                try
                {
                    EventLog.Clear();
                }
                catch (System.Exception)
                {

                }
            }
        }
    }
}
