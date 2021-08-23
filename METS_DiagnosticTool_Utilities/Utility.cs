using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static METS_DiagnosticTool_Utilities.VariableConfigurationHelper;

namespace METS_DiagnosticTool_Utilities
{
    public class Utility
    {
        public static List<string> ListOfDeclaredPLCVariables = new List<string>();

        public const string DateTimeFormat_Hour = "HH:mm:ss";
        public const string DateTimeFormat = "dd.MM.yyyy HH:mm:ss";
        public const string DateTimeFormat_WithMilisec = "dd.MM.yyyy HH:mm:ss.fff";
        public const string DateTimeFormat_HourWithMilisec = "HH:mm:ss.fff";

        /// <summary>
        /// Method to safely move a file to specified Folder
        /// </summary>
        /// <param name="_fileFullPath">File Full Path</param>
        /// <param name="_fileDir">Actual File Directory</param>
        /// <param name="_folderToBeMoved">Name of the Folder the File is going to be moved to</param>
        /// <param name="_eventID_OK">Event ID, if succesfull</param>
        /// <param name="_eventID_Exception">Event ID, if failed</param>
        public static void SafeMove(string _fileFullPath, string _fileDir, string _folderToBeMoved, Logger.logEvents _eventID_OK, Logger.logEvents _eventID_Exception)
        {
            CheckDirCreate(_fileDir, _folderToBeMoved);

            try
            {
                // First check does the file already exists in the final folder
                string _finalPath = string.Concat(_fileDir, _folderToBeMoved, @"/", Path.GetFileName(_fileFullPath));

                if (File.Exists(_finalPath))
                {
                    File.Delete(_fileFullPath);
                    Logger.Log(Logger.logLevel.Information, string.Concat("CHM File ", Path.GetFileName(_fileFullPath).ToString(), " already exists in the final path ", string.Concat(_fileDir, _folderToBeMoved)), _eventID_OK);
                }
                else
                {
                    File.Move(_fileFullPath, _finalPath);
                    //Logger.Log(Logger.logLevel.Information, string.Concat("CHM File ", Path.GetFileName(_fileFullPath).ToString(), " moved succesfully to ", string.Concat(_fileDir, _folderToBeMoved)), _eventID_OK);
                }
            }
            catch (Exception ex1)
            {
                Logger.Log(Logger.logLevel.Error, string.Concat("Exception when trying to move CHM File ", Path.GetFileName(_fileFullPath).ToString(), " to ", string.Concat(_fileDir, _folderToBeMoved), Environment.NewLine, ex1.ToString()), _eventID_Exception);
            }
        }

        /// <summary>
        /// Method to safely move a file to specified Folder
        /// </summary>
        /// <param name="_fileFullPath">File Full Path Source</param>
        /// <param name="_fileFullPathToBeMoved">File Full Path Destination</param>
        /// <param name="_eventID_OK">Event ID, if succesfull</param>
        /// <param name="_eventID_Exception">Event ID, if failed</param>
        public static void SafeMove(string _fileFullPath, string _fileFullPathToBeMoved, Logger.logEvents _eventID_OK, Logger.logEvents _eventID_Exception)
        {
            try
            {
                if (File.Exists(_fileFullPathToBeMoved))
                {
                    File.Delete(_fileFullPath);
                    Logger.Log(Logger.logLevel.Information, string.Concat("CHM File ", Path.GetFileName(_fileFullPath).ToString(), " already exists in the final path ", _fileFullPathToBeMoved), _eventID_OK);
                }
                else
                {
                    File.Move(_fileFullPath, _fileFullPathToBeMoved);
                    //Logger.Log(Logger.logLevel.Information, string.Concat("CHM File ", Path.GetFileName(_fileFullPath).ToString(), " moved succesfully to ", _fileFullPathToBeMoved), _eventID_OK);
                }
            }
            catch (Exception ex1)
            {
                Logger.Log(Logger.logLevel.Error, string.Concat("Exception when trying to move CHM File ", Path.GetFileName(_fileFullPath).ToString(), " to ", _fileFullPathToBeMoved, Environment.NewLine, ex1.ToString()), _eventID_Exception);
            }
        }

        /// <summary>
        /// Method to safely move a file to specified Folder
        /// </summary>
        /// <param name="_fileFullPath">File Full Path</param>
        /// <param name="_fileDir">Actual File Directory</param>
        /// <param name="_folderToBeMoved">Name of the Folder the File is going to be moved to</param>
        /// <param name="_newFileName">New File Name</param>
        /// <param name="_eventID_OK">Event ID, if succesfull</param>
        /// <param name="_eventID_Exception">Event ID, if failed</param>
        public static void SafeMove(string _fileFullPath, string _fileDir, string _folderToBeMoved, string _newFileName, Logger.logEvents _eventID_OK, Logger.logEvents _eventID_Exception)
        {
            CheckDirCreate(_fileDir, _folderToBeMoved);

            try
            {
                string _finalPath = string.Concat(_fileDir, _folderToBeMoved, @"/", _newFileName);

                if (File.Exists(_finalPath))
                {
                    File.Delete(_fileFullPath);
                    Logger.Log(Logger.logLevel.Information, string.Concat("CHM File ", Path.GetFileName(_fileFullPath).ToString(), " already exists in the final path ", string.Concat(_fileDir, _folderToBeMoved)), _eventID_OK);
                }
                else
                {
                    File.Move(_fileFullPath, _finalPath);
                    //Logger.Log(Logger.logLevel.Information, string.Concat("CHM File ", Path.GetFileName(_fileFullPath).ToString(), " moved succesfully to ", string.Concat(_fileDir, _folderToBeMoved)), _eventID_OK);
                }


            }
            catch (Exception ex1)
            {
                Logger.Log(Logger.logLevel.Error, string.Concat("Exception when trying to move CHM File ", Path.GetFileName(_fileFullPath).ToString(), " to ", string.Concat(_fileDir, _folderToBeMoved), Environment.NewLine, ex1.ToString()), _eventID_Exception);
            }
        }

        /// <summary>
        /// Method to safely delete a file
        /// </summary>
        /// <param name="_fileFullPath">File Full Path</param>
        /// <param name="_eventID_Exception">Event ID, if failed</param>
        public static void SafeDelete(string _fileFullPath, Logger.logEvents _eventID_Exception)
        {
            try
            {
                File.Delete(_fileFullPath);
            }
            catch (Exception ex1)
            {
                Logger.Log(Logger.logLevel.Error, string.Concat("Exception when trying to delete CHM File ", Path.GetFileName(_fileFullPath).ToString(), Environment.NewLine, ex1.ToString()), _eventID_Exception);
            }
        }

        /// <summary>
        /// Method to create Log file by replacing extension of a file
        /// </summary>
        /// <param name="chmFileXML_Filename"></param>
        /// <param name="chmFullDir"></param>
        /// <param name="serverResponse">Response from the Server</param>
        /// <param name="methodResponse">Response from the Method that is trying to get response from the Server</param>
        public static void CreateLogFile(string chmFileXML_Filename, string chmFullDir, string serverResponse, string methodResponse)
        {
            // Create CHM LOG name from the XML file name, by replacing 'xml' with 'log_fss3'
            string chmFileName_LOG = string.Concat(chmFileXML_Filename.Substring(0, chmFileXML_Filename.Length - 3), "log_fss3");

            // Create empty LOG file in the Error Directory
            using (StreamWriter sw = File.AppendText(string.Concat(chmFullDir, @"Error\", chmFileName_LOG)))
            {
                // If soapError prop is empty just save the response to the File
                sw.WriteLine(!string.IsNullOrEmpty(serverResponse) ? serverResponse : methodResponse);
                sw.Close();
            }
        }

        /// <summary>
        /// Method to safely add elements to a Dictionary, by first checking does the Key exist in the Dictionary or not
        /// </summary>
        /// <param name="_dictionary"></param>
        /// <param name="_keyToBeAdded"></param>
        /// <param name="_valueToBeAdded"></param>
        public static void SafeAddToDictionary(Dictionary<string, string> _dictionary, string _keyToBeAdded, string _valueToBeAdded)
        {
            if (!_dictionary.ContainsKey(_keyToBeAdded))
                _dictionary.Add(_keyToBeAdded, _valueToBeAdded);
        }

        /// <summary>
        /// Method to safely update Key in the Dictionary with new Value
        /// If Key does not exists in the Dictionary, it's going to be added
        /// If Key exists in the Dictionary it's value is going to be updated, ONLY IF, the new value is different than old one
        /// </summary>
        /// <param name="_dictionary"></param>
        /// <param name="_keyToBeAdded"></param>
        /// <param name="_valueToBeAdded"></param>
        public static void SafeUpdateKeyInDictionary(Dictionary<string, string> _dictionary, string _keyToBeAdded, string _valueToBeAdded)
        {
            if (!_dictionary.ContainsKey(_keyToBeAdded))
                _dictionary.Add(_keyToBeAdded, _valueToBeAdded);
            else
            {
                // Key already exists in the Dictionary so Update it, only if it's previous value is different than new one
                if (_dictionary[_keyToBeAdded] != _valueToBeAdded)
                    _dictionary[_keyToBeAdded] = _valueToBeAdded;
            }
        }

        public static void SafeUpdateKeyInDictionary(Dictionary<string, VariableConfig> _dictionary, string _keyToBeAdded, VariableConfig _valueToBeAdded)
        {
            if (!_dictionary.ContainsKey(_keyToBeAdded))
                _dictionary.Add(_keyToBeAdded, _valueToBeAdded);
            else
            {
                // Key already exists in the Dictionary so Update it, only if it's previous value is different than new one
                if (_dictionary[_keyToBeAdded].variableAddress != _valueToBeAdded.variableAddress && !string.IsNullOrEmpty(_valueToBeAdded.variableAddress))
                {
                    VariableConfig _modifiedValue = _dictionary[_keyToBeAdded];
                    _modifiedValue.variableAddress = _valueToBeAdded.variableAddress;
                    _dictionary[_keyToBeAdded] = _modifiedValue;
                }

                if (_dictionary[_keyToBeAdded].pollingRefreshTime != _valueToBeAdded.pollingRefreshTime)
                {
                    VariableConfig _modifiedValue = _dictionary[_keyToBeAdded];
                    _modifiedValue.pollingRefreshTime = _valueToBeAdded.pollingRefreshTime;

                    Logger.Log(Logger.logLevel.Warning, string.Concat("polling refresh TIME ", _valueToBeAdded.pollingRefreshTime), Logger.logEvents.Blank);

                    _dictionary[_keyToBeAdded] = _modifiedValue;
                }

                if (_dictionary[_keyToBeAdded].recording != _valueToBeAdded.recording)
                {
                    VariableConfig _modifiedValue = _dictionary[_keyToBeAdded];
                    _modifiedValue.recording = _valueToBeAdded.recording;
                    _dictionary[_keyToBeAdded] = _modifiedValue;
                }

                if (_dictionary[_keyToBeAdded].loggingType != _valueToBeAdded.loggingType)
                {
                    VariableConfig _modifiedValue = _dictionary[_keyToBeAdded];
                    _modifiedValue.loggingType = _valueToBeAdded.loggingType;
                    _dictionary[_keyToBeAdded] = _modifiedValue;
                }
            }
        }

        /// <summary>
        /// Method to safely insert a Row of string data to Linked List, so all data from all other rows is going to be copied to next positions
        /// </summary>
        /// <param name="_linkedList"></param>
        /// <param name="_dataToBeInsterted"></param>
        public static void SafeInsertToLinkedList(LinkedList<string> _linkedList, string _dataToBeInsterted)
        {
            // Insert only if first row in the Linked List is diffrent than _dataToBeInserted
            if (_linkedList.Count > 0)
            {
                if (_linkedList.ElementAt(0) != _dataToBeInsterted)
                    _linkedList.AddFirst(_dataToBeInsterted);
            }
            else
                _linkedList.AddFirst(_dataToBeInsterted);
        }

        /// <summary>
        /// Method to check does the Directory exists, if not it creates one
        /// </summary>
        /// <param name="_basePath">Base path to the Directory, for ex. C:/METS/CHM/</param>
        /// <param name="_dirName">Name of the Directory we want to check</param>
        public static void CheckDirCreate(string _basePath, string _dirName)
        {
            if (!Directory.Exists(string.Concat(_basePath, _dirName)))
                // Create Error Directory if it doesnt exist
                Directory.CreateDirectory(string.Concat(_basePath, _dirName));
        }

        /// <summary>
        /// Method to check does the Directory exists, if not creates one
        /// </summary>
        /// <param name="_fullPath">Full Path to the Directory</param>
        public static void CheckDirCreate(string _fullPath)
        {
            if (!Directory.Exists(_fullPath))
                // Create Error Directory if it doesnt exist
                Directory.CreateDirectory(_fullPath);
        }

        /// <summary>
        /// Simple check is the given string null or empty if yes, then return "string.Empty"
        /// </summary>
        /// <param name="_givenString"></param>
        /// <returns></returns>
        public static string CheckStringEmpty(string _givenString, bool _returnNothing = false)
        {
            return string.IsNullOrEmpty(_givenString) ? _returnNothing ? "" : "string.Empty" : _givenString;
        }

        /// <summary>
        /// Method to return value of a Parameter from given Arguments, based on searched Argument
        /// </summary>
        /// <param name="givenArgs"></param>
        /// <param name="searchedArg"></param>
        /// <returns></returns>
        public static string ParseArg(string[] givenArgs, string searchedArg)
        {
            string _return = string.Empty;

            foreach (string arg in givenArgs)
            {
                if (arg.StartsWith(searchedArg))
                {
                    _return = arg.Substring(arg.IndexOf(":") + 1);
                    break;
                }
            }

            return _return;
        }

        /// <summary>
        /// Method to return value from given list of strings (each item in list in format "stringName:value") based on given Key
        /// </summary>
        /// <param name="givenStrings"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public static string ParseFromString(List<string> givenStrings, string key, bool returnZeroIfStringEmpty = false)
        {
            string _return = string.Empty;

            if (givenStrings != null)
            {
                foreach (string givenString in givenStrings)
                {
                    if (givenString.StartsWith(key))
                    {
                        _return = givenString.Substring(givenString.IndexOf(":") + 1);
                        break;
                    }
                }
            }

            return returnZeroIfStringEmpty ? string.IsNullOrEmpty(_return) ? "0" : _return : _return;
        }

        /// <summary>
        /// Method to return value from string in format "stringName:value", based on given key
        /// </summary>
        /// <param name="givenString"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public static string ParseFromString(string givenString, string key)
        {
            string _return = string.Empty;

            if (givenString.Contains(key))
                _return = givenString.Substring(givenString.IndexOf(":") + 1);

            return _return;
        }

        /// <summary>
        /// Helper method to limit Integer Value
        /// </summary>
        /// <param name="value"></param>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        public static int Clamp(int value, int min, int max)
        {
            return (value < min) ? min : (value > max) ? max : value;
        }

        /// <summary>
        /// Method to return ordinal string based on given number
        /// </summary>
        /// <param name="number"></param>
        /// <returns></returns>
        public static string ReturnOrdinal(int number)
        {
            var numberAsString = number.ToString();

            // Negative and zero have no ordinal representation
            if (number < 1)
            {
                return numberAsString;
            }

            number %= 100;
            if ((number >= 11) && (number <= 13))
            {
                return numberAsString + "th";
            }

            switch (number % 10)
            {
                case 1: return numberAsString + "st";
                case 2: return numberAsString + "nd";
                case 3: return numberAsString + "rd";
                default: return numberAsString + "th";
            }
        }
    }
}
