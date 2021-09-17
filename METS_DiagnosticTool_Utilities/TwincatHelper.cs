using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TwinCAT.Ads;
using TwinCAT.Ads.SumCommand;
using TwinCAT.PlcOpen;

namespace METS_DiagnosticTool_Utilities
{
    public class TwincatHelper
    {
        private static AdsSession tcClientSession;
        private static AdsConnection tcClient;
        private static AmsAddress tcAmsAddress;

        private static Dictionary<string, uint[]> _dicGlobalHandles = new Dictionary<string, uint[]>();

        public static string VariableDatatype = string.Empty;

        public enum G_ET_TagType
        {
            PLCFloatAndVBSingle = 1,
            PLCBooleanAndVBBoolean = 2,
            PLCDwordAndVBUint = 3,
            PLCIntegerAndVBShort = 4,
            PLCString = 5,
            PLCDintAndVBInt = 6,
            PLCLRealAndVBDouble = 7,
            PLCUIntegerAndVBUShort = 8,
            PLCTime = 9,
            PLCEnum = 10,
            PLCDT = 11,
            PLCTOD = 12,
            PLCDate = 13,
            PLCByte = 14,
            None = 99
        }

        public enum G_ET_TagTypeWatchdog
        {
            PLCDwordAndVBUint,
            PLCIntegerAndVBShort,
            PLCDintAndVBInt,
            PLCUIntegerAndVBUShort,
        }

        public enum G_ET_EndPoint
        {
            DiagnosticToolCore,
            DiagnosticToolUI
        }

        public static bool TwincatInitialization(string _amsAddress, string _amsPort, G_ET_EndPoint endPoint = G_ET_EndPoint.DiagnosticToolCore)
        {
            //Initialize TwinCAT Client Class
            // Connect to target
            bool _plcConnectedAndRunning = false;
            bool _parsePort = Int16.TryParse(_amsPort, out short _parsedPort);
            AdsErrorCode _adsError = AdsErrorCode.NoError;

            bool _exceptionOcurred = false;

            if (_parsePort)
            {
                try
                {
                    if (tcClient == null)
                    {
                        tcAmsAddress = new AmsAddress(_amsAddress, _parsedPort);
                        tcClientSession = new AdsSession(tcAmsAddress);

                        //tcClient.Connect(new AmsAddress(_amsAddress, _parsedPort));
                        tcClient = (AdsConnection)tcClientSession.Connect();

                        if (tcClient.IsConnected)
                        {
                            AdsErrorCode _tryReadState = tcClient.TryReadState(out StateInfo _adsState);
                            if (_tryReadState == AdsErrorCode.NoError)
                            {
                                AdsState _plcStateMode = tcClient.ReadState().AdsState;

                                if (_plcStateMode == AdsState.Run)
                                    _plcConnectedAndRunning = true;
                                else
                                    Logger.Log(Logger.logLevel.Error, string.Concat("PLC not in Run Mode. Actual PLC mode is ", _plcStateMode.ToString()),
                                               Logger.logEvents.PLCNotInRunMode, endPoint);
                            }
                            else
                                _adsError = _tryReadState;
                        }
                    }
                    else
                    {
                        if (CheckPLCConnectionState())
                            _plcConnectedAndRunning = true;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log(Logger.logLevel.Error, string.Concat("Twincat Intialization exception ", ex.Message),
                               Logger.logEvents.TwincatADSFailedToConnect, endPoint);
                    _exceptionOcurred = true;
                }

            }

            if (!_exceptionOcurred)
            {
                if (_plcConnectedAndRunning)
                    Logger.Log(Logger.logLevel.Information, string.Concat("Succesfully connected to TwinCAT ADS on Ip Address ", _amsAddress, " and Port ", _amsPort),
                               Logger.logEvents.TwincatADSConnectionOk, endPoint);
                else
                {
                    if (_adsError != AdsErrorCode.NoError)
                        Logger.Log(Logger.logLevel.Error, string.Concat("Could not connected to TwinCAT ADS on Ip Address ", _amsAddress, " and Port ", _amsPort, "; ADS Error ", _adsError),
                                   Logger.logEvents.TwincatADSFailedToConnect, endPoint);
                }
            }

            return _plcConnectedAndRunning;
        }

        private static bool CheckPLCConnectionState()
        {
            bool _return = false;

            try
            {
                if (tcClient != null)
                {
                    if (tcClient.IsConnected)
                    {
                        AdsErrorCode _tryReadState = tcClient.TryReadState(out StateInfo _adsState);
                        if (_tryReadState == AdsErrorCode.NoError)
                        {
                            AdsState _plcStateMode = tcClient.ReadState().AdsState;

                            if (_plcStateMode == AdsState.Run)
                                _return = true;
                            else
                            {
                                Logger.Log(Logger.logLevel.Error, string.Concat("PLC not in Run Mode. Actual PLC mode is ", _plcStateMode.ToString()), Logger.logEvents.PLCNotInRunMode);
                                return false;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                //Logger.Log(Logger.logLevel.Error, string.Concat("Check PLC Connection state exception ", ex.ToString()), Logger.logEvents.TwincatADSConnectionStateException);
            }

            return _return;
        }

        public static void Dispose()
        {
            try
            {
                if (tcClient != null)
                {
                    if (tcClient.IsConnected)
                    {
                        tcClient.Disconnect();
                        tcClient.Dispose();
                    }
                }
            }
            catch (Exception)
            {
            }
        }

        public static bool CheckPLCVariableExistance(string i_sTagName)
        {
            bool _return = false;
            ITcAdsSymbol5 _symbolInfo = null;
            try
            {
                _symbolInfo = (ITcAdsSymbol5)tcClient.ReadSymbolInfo(i_sTagName);
            }
            catch (Exception)
            {
                
            }
            finally
            {
                if (_symbolInfo != null && _symbolInfo.DataType.IsPrimitive)
                    _return = true;
            }

            return _return;
        }

        public static G_ET_TagType GetSymbolType(string i_sTagName)
        {
            VariableDatatype = string.Empty;
            G_ET_TagType _return = 0;

            try
            {
                ITcAdsSymbol5 symbol = (ITcAdsSymbol5)tcClient.ReadSymbolInfo(i_sTagName);

                if(symbol != null)
                {
                    string _returnValue;
                    // Special Treatment for Enum
                    if (symbol.DataType.HasEnumInfo)
                    {
                        _returnValue = "ENUM";

                        VariableDatatype = string.Concat(_returnValue, ":", symbol.TypeName.ToUpper());
                    }
                    else
                    {
                        _returnValue = symbol.TypeName.ToUpper();

                        VariableDatatype = _returnValue;
                    }
                        
                    // Special treatment for STRING(nnnnn)
                    if (_returnValue.Contains("STRING"))
                        _returnValue = "STRING";

                    if (_returnValue.Contains("INT16"))
                        _returnValue = "INT";

                    if (_returnValue.Contains("UINT16"))
                        _returnValue = "UINT";

                    switch (_returnValue)
                    {
                        case "BOOL":
                            _return = G_ET_TagType.PLCBooleanAndVBBoolean;
                            break;
                        case "BYTE":
                            _return = G_ET_TagType.PLCByte;
                            break;
                        case "WORD":
                            _return = G_ET_TagType.PLCDwordAndVBUint;
                            break;
                        case "DWORD":
                            _return = G_ET_TagType.PLCDwordAndVBUint;
                            break;
                        case "SINT":
                            _return = G_ET_TagType.PLCIntegerAndVBShort;
                            break;
                        case "USINT":
                            _return = G_ET_TagType.PLCUIntegerAndVBUShort;
                            break;
                        case "INT":
                            _return = G_ET_TagType.PLCIntegerAndVBShort;
                            break;
                        case "UINT":
                            _return = G_ET_TagType.PLCUIntegerAndVBUShort;
                            break;
                        case "DINT":
                            _return = G_ET_TagType.PLCDintAndVBInt;
                            break;
                        case "UDINT":
                            _return = G_ET_TagType.PLCUIntegerAndVBUShort;
                            break;
                        case "REAL":
                            _return = G_ET_TagType.PLCFloatAndVBSingle;
                            break;
                        case "LREAL":
                            _return = G_ET_TagType.PLCLRealAndVBDouble;
                            break;
                        case "STRING":
                            _return = G_ET_TagType.PLCString;
                            break;
                        case "TIME":
                            _return = G_ET_TagType.PLCTime;
                            break;
                        case "DATE":
                            _return = G_ET_TagType.PLCDate;
                            break;
                        case "TIME_OF_DAY":
                            _return = G_ET_TagType.PLCTOD;
                            break;
                        case "DATE_AND_TIME":
                            _return = G_ET_TagType.PLCDT;
                            break;
                        case "ENUM":
                            _return = G_ET_TagType.PLCEnum;
                            break;
                        default:
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log(Logger.logLevel.Error, string.Concat("Twincat ADS read symbol type exception for Symbol ", string.IsNullOrEmpty(i_sTagName) ? "string.Empty" : i_sTagName, Environment.NewLine, ex.ToString()), Logger.logEvents.TwinatADSReadException);
            }

            return _return;
        }

        public static ADSSumReadHelper PrepareAdsSumRead(string i_sTagName)
        {
            List<string> instancePaths = new List<string> { i_sTagName };

            SumCreateHandles createHandlesCommand = new SumCreateHandles(tcClient, instancePaths);
            uint[] handles = createHandlesCommand.CreateHandles();
            
            //Collect created handles to global list of handles to Release them at the end of Application life
            if (!_dicGlobalHandles.ContainsKey(i_sTagName))
                _dicGlobalHandles.Add(i_sTagName, handles);
            else
                _dicGlobalHandles[i_sTagName] = handles;

            List<Type> valueTypes = new List<Type>();

            //Foreach Symbol it's type has to be specified
            ITcAdsSymbol5 symbol = (ITcAdsSymbol5)tcClient.ReadSymbolInfo(i_sTagName);

            if (symbol.DataType.ManagedType.Name == "TIME")
                valueTypes.Add(typeof(TIME));
            else if (symbol.DataType.ManagedType.Name == "TOD")
                valueTypes.Add(typeof(TOD));
            else if (symbol.DataType.ManagedType.Name == "DATE")
                valueTypes.Add(typeof(DATE));
            else if (symbol.DataType.ManagedType.Name == "DT")
                valueTypes.Add(typeof(DT));
            else
                valueTypes.Add(Type.GetType(symbol.DataType.ManagedType.FullName));

            SumHandleRead readCommand_SumHandle = new SumHandleRead(tcClient, handles, valueTypes.ToArray());

            ADSSumReadHelper _return = new ADSSumReadHelper { SymbolPath = i_sTagName, SymbolCommand = readCommand_SumHandle, SymbolDataType = symbol.DataType };
            return _return;
        }

        public static void ClearAllAdsSumRead()
        {
            foreach (uint[] _handle in _dicGlobalHandles.Values)
            {
                SumReleaseHandles releaseCommand = new SumReleaseHandles(tcClient, _handle);
                releaseCommand.ReleaseHandles();
            }

            _dicGlobalHandles.Clear();
        }

        public static void RemoveAdsSumRead(string i_sTagName)
        {
            if (_dicGlobalHandles.ContainsKey(i_sTagName))
            {
                SumReleaseHandles releaseCommand = new SumReleaseHandles(tcClient, _dicGlobalHandles[i_sTagName]);
                releaseCommand.ReleaseHandles();

                _dicGlobalHandles.Remove(i_sTagName);
            }
        }

        public static string PLCAdsSumReadPLCValues(ADSSumReadHelper adsSumReadHelper)
        {
            adsSumReadHelper.SymbolCommand.TryRead(out object[] values, out AdsErrorCode[] _errorCodes);

            //Get all error Codes that are indicating error
            var _foundErros = _errorCodes.Select((item, index) => new
            {
                ErrorCode = item,
                Index = index
            }).Where(i => i.ErrorCode != AdsErrorCode.NoError);

            foreach (var _foundError in _foundErros)
            {
                //Found Error
                Logger.Log(Logger.logLevel.Error, string.Concat("Error ", _foundError.ErrorCode.ToString(), " when trying to read Symbol ", adsSumReadHelper.SymbolPath[_foundError.Index],
                    "; Ads Error code ", (uint)_foundError.ErrorCode), Logger.logEvents.Blank);
            }

            string _key = adsSumReadHelper.SymbolPath;
            if (!string.IsNullOrEmpty(_key))
            {
                if (_key.Substring(0, 1).Contains("."))
                    _key = _key.Substring(1, _key.Length - 1);

                if (adsSumReadHelper.SymbolDataType.Name == "DATE")
                {
                    bool _dateParsed = DATE.TryParse(values[0].ToString(), out DATE _date);
                    return _date.Date.Date.ToString("dd.MM.yyyy");
                }
                else if (adsSumReadHelper.SymbolDataType.Name == "TIME")
                {
                    bool _timeParsed = TIME.TryParse(values[0].ToString(), out TIME _time);
                    return _time.Time.ToString("HH:mm:ss.fff");
                }
                else if (adsSumReadHelper.SymbolDataType.Name == "DATE_AND_TIME")
                {
                    bool _dtParsed = DT.TryParse(values[0].ToString(), out DT _dt);
                    return _dt.Date.ToString("dd.MM.yyyy HH:mm:ss.fff");
                }
                else if (adsSumReadHelper.SymbolDataType.Name == "TIME_OF_DAY")
                {
                    bool _todParsed = TOD.TryParse(values[0].ToString(), out TOD _dt);
                    return _dt.Time.ToString();
                }
                else
                    return values[0].ToString();
            }
            else
                return "string.Empty";
        }

        public static string ReadPLCValues(string i_sTagName)
        {
            string _return = string.Empty;

            if (CheckPLCConnectionState())
            {
                try
                {
                    int iVarHandle = 0;

                    switch (GetSymbolType(i_sTagName))
                    {
                        case G_ET_TagType.PLCFloatAndVBSingle:
                            _return = tcClient.ReadSymbol(i_sTagName, typeof(float), false).ToString();
                            _return = string.IsNullOrEmpty(_return) ? 0.ToString() : _return;
                            break;
                        case G_ET_TagType.PLCBooleanAndVBBoolean:
                            _return = tcClient.ReadSymbol(i_sTagName, typeof(bool), false).ToString();
                            _return = string.IsNullOrEmpty(_return) ? false.ToString() : _return;
                            break;
                        case G_ET_TagType.PLCDwordAndVBUint:
                            _return = tcClient.ReadSymbol(i_sTagName, typeof(uint), false).ToString();
                            _return = string.IsNullOrEmpty(_return) ? 0.ToString() : _return;
                            break;
                        case G_ET_TagType.PLCIntegerAndVBShort:
                            _return = tcClient.ReadSymbol(i_sTagName, typeof(short), false).ToString();
                            _return = string.IsNullOrEmpty(_return) ? 0.ToString() : _return;
                            break;
                        case G_ET_TagType.PLCString:
                            _return = tcClient.ReadSymbol(i_sTagName, typeof(string), false).ToString();
                            _return = string.IsNullOrEmpty(_return) ? "string.Empty" : _return;
                            break;
                        case G_ET_TagType.PLCDintAndVBInt:
                            _return = tcClient.ReadSymbol(i_sTagName, typeof(int), false).ToString();
                            _return = string.IsNullOrEmpty(_return) ? 0.ToString() : _return;
                            break;
                        case G_ET_TagType.PLCLRealAndVBDouble:
                            _return = tcClient.ReadSymbol(i_sTagName, typeof(double), false).ToString();
                            _return = string.IsNullOrEmpty(_return) ? 0.ToString() : _return;
                            break;
                        case G_ET_TagType.PLCUIntegerAndVBUShort:
                            _return = tcClient.ReadSymbol(i_sTagName, typeof(ushort), false).ToString();
                            _return = string.IsNullOrEmpty(_return) ? 0.ToString() : _return;
                            break;
                        case G_ET_TagType.PLCTime:
                            iVarHandle = tcClient.CreateVariableHandle(i_sTagName);
                            _return = tcClient.ReadAny(iVarHandle, typeof(TIME)).ToString();
                            _return = string.IsNullOrEmpty(_return) ? 0.ToString() : _return;
                            tcClient.DeleteVariableHandle(iVarHandle);
                            break;
                        case G_ET_TagType.PLCDate:
                            iVarHandle = tcClient.CreateVariableHandle(i_sTagName);
                            _return = tcClient.ReadAny(iVarHandle, typeof(DATE)).ToString();
                            _return = string.IsNullOrEmpty(_return) ? 0.ToString() : _return;
                            tcClient.DeleteVariableHandle(iVarHandle);
                            break;
                        case G_ET_TagType.PLCDT:
                            iVarHandle = tcClient.CreateVariableHandle(i_sTagName);
                            _return = tcClient.ReadAny(iVarHandle, typeof(DT)).ToString();
                            _return = string.IsNullOrEmpty(_return) ? 0.ToString() : _return;
                            tcClient.DeleteVariableHandle(iVarHandle);
                            break;
                        case G_ET_TagType.PLCTOD:
                            iVarHandle = tcClient.CreateVariableHandle(i_sTagName);
                            _return = tcClient.ReadAny(iVarHandle, typeof(TOD)).ToString();
                            _return = string.IsNullOrEmpty(_return) ? 0.ToString() : _return;
                            tcClient.DeleteVariableHandle(iVarHandle);
                            break;
                        case G_ET_TagType.PLCEnum:
                            iVarHandle = tcClient.CreateVariableHandle(i_sTagName);
                            _return = tcClient.ReadAny(iVarHandle, typeof(short)).ToString();
                            _return = string.IsNullOrEmpty(_return) ? 0.ToString() : _return;
                            tcClient.DeleteVariableHandle(iVarHandle);
                            break;
                        case G_ET_TagType.PLCByte:
                            _return = tcClient.ReadSymbol(i_sTagName, typeof(byte), false).ToString();
                            _return = string.IsNullOrEmpty(_return) ? 0.ToString() : _return;
                            break;
                    }
                }
                //catch (AdsException ex1)
                //{

                //}
                catch (Exception ex)
                {
                    Logger.Log(Logger.logLevel.Error, string.Concat("Twincat ADS read exception for Symbol ", string.IsNullOrEmpty(i_sTagName) ? "string.Empty" : i_sTagName, Environment.NewLine, ex.ToString()), Logger.logEvents.TwinatADSReadException);
                }
            }
            //else
            //    Logger.Log(Logger.logLevel.Warning, "Check connection state false", Logger.logEvents.Blank);

            return _return;
        }

        public static void WritePLCValues(string i_sTagName, G_ET_TagType eTagType, string i_sValue)
        {
            if (!CheckPLCConnectionState())
                return;
            else
            {
                int iVarHandle;

                try
                {
                    if (!string.IsNullOrEmpty(i_sTagName))
                    {
                        switch (eTagType)
                        {

                            case G_ET_TagType.PLCFloatAndVBSingle:
                                tcClient.WriteSymbol(i_sTagName, Convert.ToSingle(i_sValue), false);
                                break;
                            case G_ET_TagType.PLCBooleanAndVBBoolean:
                                tcClient.WriteSymbol(i_sTagName, Convert.ToBoolean(i_sValue), false);
                                break;
                            case G_ET_TagType.PLCDwordAndVBUint:
                                tcClient.WriteSymbol(i_sTagName, Convert.ToUInt32(i_sValue), false);
                                break;
                            case G_ET_TagType.PLCIntegerAndVBShort:
                                tcClient.WriteSymbol(i_sTagName, Convert.ToInt16(i_sValue), false);
                                break;
                            case G_ET_TagType.PLCString:
                                using (AdsStream dataStream = new AdsStream(i_sValue.Length + 1))
                                {
                                    using (BinaryWriter writer = new BinaryWriter(dataStream, Encoding.ASCII))
                                    {
                                        writer.Write(i_sValue.ToCharArray());
                                        writer.Write((char)0);

                                        iVarHandle = tcClient.CreateVariableHandle(i_sTagName);
                                        tcClient.Write(iVarHandle, dataStream);
                                        tcClient.DeleteVariableHandle(iVarHandle);
                                    }
                                }
                                break;
                            case G_ET_TagType.PLCDintAndVBInt:
                                tcClient.WriteSymbol(i_sTagName, Convert.ToInt32(i_sValue), false);
                                break;
                            case G_ET_TagType.PLCLRealAndVBDouble:
                                tcClient.WriteSymbol(i_sTagName, Convert.ToDouble(i_sValue), false);
                                break;
                            case G_ET_TagType.PLCUIntegerAndVBUShort:
                                tcClient.WriteSymbol(i_sTagName, Convert.ToUInt16(i_sValue), false);
                                break;
                            case G_ET_TagType.PLCTime:
                                iVarHandle = tcClient.CreateVariableHandle(i_sTagName);
                                tcClient.WriteAny(iVarHandle, Convert.ToUInt16(i_sValue));
                                tcClient.DeleteVariableHandle(iVarHandle);
                                break;
                            case G_ET_TagType.PLCEnum:
                                iVarHandle = tcClient.CreateVariableHandle(i_sTagName);
                                tcClient.WriteAny(iVarHandle, Convert.ToInt16(i_sValue));
                                tcClient.DeleteVariableHandle(iVarHandle);
                                break;
                            case G_ET_TagType.PLCByte:
                                tcClient.WriteSymbol(i_sTagName, Convert.ToByte(i_sValue), false);
                                break;
                            default:
                                break;
                        }
                    }
                    else
                        Logger.Log(Logger.logLevel.Error, string.Concat("Twincat ADS write exception for Symbol ", string.IsNullOrEmpty(i_sTagName) ? "string.Empty" : i_sTagName), Logger.logEvents.TwinatADSWriteException);
                }
                catch (AdsException ex1)
                {
                    //Logger.Log(Logger.logLevel.Error, string.Concat("Twincat ADS write exception for Symbol ", string.IsNullOrEmpty(i_sTagName) ? "string.Empty" : i_sTagName, Environment.NewLine, ex1.ToString()), Logger.logEvents.TwinatADSWriteException);
                }
                catch (Exception ex)
                {
                    Logger.Log(Logger.logLevel.Error, string.Concat("Twincat ADS write exception for Symbol ", string.IsNullOrEmpty(i_sTagName) ? "string.Empty" : i_sTagName, Environment.NewLine, ex.ToString()), Logger.logEvents.TwinatADSWriteException);
                }
            }
        }
    }

    public class ADSSumReadHelper
    {
        public string SymbolPath { get; set; }
        public dynamic SymbolCommand { get; set; }
        public ITcAdsDataType SymbolDataType { get; set; }
    }
}
