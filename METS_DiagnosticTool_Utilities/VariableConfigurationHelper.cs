using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace METS_DiagnosticTool_Utilities
{
    public class VariableConfigurationHelper
    {
        #region Private Fields
        private static List<string> UsedPLCVariables = new List<string>();
        #endregion

        #region Public Fields
        public const string inputPlaceHolderText = "Enter PLC Variable Address here...";

        public struct VariableConfig
        {
            [XmlElement("VariableAddress")]
            public string variableAddress;
            [XmlElement("LoggingType")]
            public LoggingType loggingType;
            [XmlElement("PollingRefreshTime")]
            public int pollingRefreshTime;
            [XmlElement("Recording")]
            public bool recording;
        }

        public enum LoggingType
        {
            Polling,
            OnChange
        }

        [XmlRoot("VariablesConfiguration")]
        public class VariableConfigurationCollection
        {
            [XmlElement("VariableConfig")]
            public List<VariableConfig> VariableConfig;
        }
        #endregion

        #region Public Methods
        public static string ReadPLCVariableConfigs(string xmlFullPath)
        {
            string _return = string.Empty;

            Dictionary<string, VariableConfig> _localDictionary = null;

            try
            {
                Dictionary<string, VariableConfig> VariablesConfigs = new Dictionary<string, VariableConfig>();

                // Deserialize whole XML file to temporary Class VariableConfigurationCollection, then collect all data in nice and tidy List of Variable Configuration
                if (File.Exists(xmlFullPath))
                {
                    using (TextReader reader = new StreamReader(xmlFullPath))
                    {
                        XmlSerializer serializer = new XmlSerializer(typeof(VariableConfigurationCollection));
                        VariableConfigurationCollection _variablesConfigurationCollection = (VariableConfigurationCollection)serializer.Deserialize(reader);

                        foreach (VariableConfig item in _variablesConfigurationCollection.VariableConfig)
                        {
                            if (!VariablesConfigs.ContainsKey(item.variableAddress))
                                VariablesConfigs.Add(item.variableAddress, item);
                        }
                    }
                }

                _localDictionary = VariablesConfigs;
            }
            catch (Exception ex)
            {
                Logger.Log(Logger.logLevel.Error, string.Concat("Exception when reading Variables Configurations ", ex.ToString()), Logger.logEvents.ReadVariableConfigurationError);
            }

            if (_localDictionary != null)
            {
                // Encode Variable Config in Single string
                foreach (KeyValuePair<string, VariableConfig> _variableConfig in _localDictionary)
                {
                    _return += string.Concat("#VariableAddress$", _variableConfig.Value.variableAddress, ";PollingRefreshTime$", _variableConfig.Value.pollingRefreshTime.ToString(),
                                              ";Recording$", _variableConfig.Value.recording.ToString(), ";LoggingType$", _variableConfig.Value.loggingType.ToString());
                }
            }

            return _return;
        }

        public static bool SavePLCVariableConfig(string message)
        {
            // Append to existance XML file.
            // Save file to a location where Core is located

            bool _return = false;

            // Decode given message
            List<string> _splitMessage = message.Split(';').ToList();

            // Create dictionary
            Dictionary<string, string> _variableConfiguration = new Dictionary<string, string>();
            foreach (string _item in _splitMessage)
            {
                string[] _config = _item.Split('$').ToArray();
                if (!_variableConfiguration.ContainsKey(_config[0]))
                    _variableConfiguration.Add(_config[0], _config[1]);
            }

            // Create new Variable Config
            string xmlFullPath = _variableConfiguration["XMLFileFullPath"];

            VariableConfig variableConfig = new VariableConfig
            {
                variableAddress = _variableConfiguration["VariableAddress"],
                pollingRefreshTime = int.Parse(_variableConfiguration["PollingRefreshTime"]),
                recording = bool.Parse(_variableConfiguration["Recording"])
            };
            bool loggingTypeParsed = Enum.TryParse(_variableConfiguration["LoggingType"], out LoggingType _loggingType);
            variableConfig.loggingType = loggingTypeParsed ? _loggingType : LoggingType.OnChange;

            // Base on gathered information append to a XML file
            // First chekc does XML Folder Exist if not create it
            Utility.CheckDirCreate(Path.GetDirectoryName(xmlFullPath));

            try
            {
                XmlDocument xmlFile = new XmlDocument();

                // Check does the file exists if not create dummy version
                if (!File.Exists(xmlFullPath))
                {
                    xmlFile = new XmlDocument();
                    XmlDeclaration xmlDeclaration = xmlFile.CreateXmlDeclaration("1.0", "UTF-8", null);
                    XmlElement root = xmlFile.DocumentElement;
                    xmlFile.InsertBefore(xmlDeclaration, root);

                    // Root
                    XmlElement element1 = xmlFile.CreateElement(string.Empty, "VariablesConfiguration", string.Empty);
                    xmlFile.AppendChild(element1);

                    xmlFile.Save(xmlFullPath);
                }

                // Load XML File
                xmlFile.Load(xmlFullPath);

                // First check does the element is already in the XML
                XmlNode rootNode = xmlFile.DocumentElement;
                string xPath = string.Concat("descendant::VariableConfig[VariableAddress='", variableConfig.variableAddress, "']");
                XmlNode variableNode = rootNode.SelectSingleNode(xPath);

                if (variableNode != null)
                {
                    // If it exists update it
                    foreach (XmlNode item in variableNode)
                    {
                        switch (item.Name)
                        {
                            case "LoggingType":
                                item.LastChild.Value = variableConfig.loggingType.ToString();
                                break;
                            case "PolingRefreshTime":
                                item.LastChild.Value = variableConfig.pollingRefreshTime.ToString();
                                break;
                            case "Recording":
                                item.LastChild.Value = variableConfig.recording.ToString().ToLower();
                                break;
                            default:
                                break;
                        }
                    }
                }
                else
                {
                    rootNode = xmlFile.GetElementsByTagName("VariablesConfiguration")[0];
                    System.Xml.XPath.XPathNavigator nav = rootNode.CreateNavigator();
                    XmlSerializerNamespaces emptyNamepsaces = new XmlSerializerNamespaces(new[] { XmlQualifiedName.Empty });

                    using (XmlWriter writer = nav.AppendChild())
                    {
                        XmlSerializer serializer = new XmlSerializer(variableConfig.GetType());
                        writer.WriteWhitespace("");
                        serializer.Serialize(writer, variableConfig, emptyNamepsaces);
                        writer.Close();
                    }
                }


                xmlFile.Save(xmlFullPath);

                _return = true;
            }
            catch (Exception ex)
            {
                Logger.Log(Logger.logLevel.Error, string.Concat("Save Variable Configuration error ", ex.ToString()), Logger.logEvents.SaveVariableConfigurationError);
            }

            return _return;
        }

        public static bool DeletePLCVariableConfig(string message)
        {
            // Load XML file, find PLC variable to delete and save updated XML file, UI is going to be updated by itself
            bool _return = false;

            // Decode given message: XMLFileFullPath$value;VariableAddress$value
            List<string> _splitMessage = message.Split(';').ToList();

            // Create dictionary
            Dictionary<string, string> _variableConfiguration = new Dictionary<string, string>();
            foreach (string _item in _splitMessage)
            {
                string[] _config = _item.Split('$').ToArray();
                if (!_variableConfiguration.ContainsKey(_config[0]))
                    _variableConfiguration.Add(_config[0], _config[1]);
            }

            string xmlFullPath = _variableConfiguration["XMLFileFullPath"];
            string variableAddress = _variableConfiguration["VariableAddress"];

            // Also delete Variable Config from UsedPLCVariables list
            if (UsedPLCVariables.Contains(variableAddress))
                UsedPLCVariables.Remove(variableAddress);

            try
            {
                // Load XML File
                XmlDocument xmlFile = new XmlDocument();
                xmlFile.Load(xmlFullPath);

                // First check does the element is already in the XML
                XmlNode rootNode = xmlFile.DocumentElement;
                string xPath = string.Concat("descendant::VariableConfig[VariableAddress='", variableAddress, "']");
                XmlNode variableNode = rootNode.SelectSingleNode(xPath);

                if (variableNode != null)
                {
                    variableNode.ParentNode.RemoveChild(variableNode);
                    xmlFile.Save(xmlFullPath);

                    _return = true;
                }
            }
            catch (Exception ex)
            {
                Logger.Log(Logger.logLevel.Error, string.Concat("Save Variable Configuration error when deleting a Variable ", ex.ToString()), Logger.logEvents.SaveVariableConfigurationError);
            }

            return _return;
        }

        public static bool CheckDoesThePLCVariableBeenUsed(string variableAddress)
        {
            // Method to check does the PLC Variable been already used in the UI. For ex. declaring the same variable again.
            bool _return = false;

            if (UsedPLCVariables.Contains(variableAddress))
            {
                _return = true;

                //Logger.Log(Logger.logLevel.Warning, string.Concat(variableAddress, " already exists in the Used Variables"), Logger.logEvents.Blank);
            }
            else
            {
                if (variableAddress != inputPlaceHolderText)
                {
                    UsedPLCVariables.Add(variableAddress);
                    //Logger.Log(Logger.logLevel.Warning, string.Concat("Just added to global List of used Variables ", variableAddress), Logger.logEvents.Blank);
                }
            }

            return _return;
        }

        public static bool RemoveNotSavedPLCVariable(string variableAddress)
        {
            bool _return = false;

            if (UsedPLCVariables.Contains(variableAddress))
            {
                UsedPLCVariables.Remove(variableAddress);

                //Logger.Log(Logger.logLevel.Warning, string.Concat("Removed not Saved PLC Variabel ", variableAddress), Logger.logEvents.Blank);

                _return = true;
            }

            return _return;
        }
        #endregion
    }
}
