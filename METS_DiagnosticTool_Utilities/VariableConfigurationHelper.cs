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
        public static Dictionary<string, VariableConfig> VariablesConfigs = new Dictionary<string, VariableConfig>();
        private static List<VariableConfig> VariablesConfigs_Used = new List<VariableConfig>();

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

        public static string ReadPLCVariableConfig(string xmlFullPath)
        {
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

            string _return = string.Empty;

            if(VariablesConfigs.Count > 0)
            {
                bool _variableUsed = false;
                VariableConfig _variableConfig = VariablesConfigs.ElementAt(0).Value;
                // Check does the Variable config has been already used before
                foreach (VariableConfig _variableConfig_Used in VariablesConfigs_Used)
                {
                    if (_variableConfig.variableAddress == _variableConfig_Used.variableAddress)
                    {
                        _variableUsed = true;
                        break;
                    }
                }

                if(!_variableUsed)
                {
                    // Collect Variable Config that has been used
                    VariablesConfigs_Used.Add(_variableConfig);

                    VariablesConfigs.Remove(VariablesConfigs.Keys.First());
                }

                _return = string.Concat("VariableAddress$", _variableConfig.variableAddress, ";LoggingType$", _variableConfig.loggingType, ";PollingRefreshTime$", _variableConfig.pollingRefreshTime, ";Recording$", _variableConfig.recording);

                //Logger.Log(Logger.logLevel.Warning, "Variable Config read from XML " + _return, Logger.logEvents.Blank);
            }
            //else
            //    Logger.Log(Logger.logLevel.Warning, "global variables config is empty :(" + _return, Logger.logEvents.Blank);

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
                                item.LastChild.Value = variableConfig.recording.ToString();
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
    }
}
