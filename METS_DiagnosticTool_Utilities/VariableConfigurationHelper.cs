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
        private static string xmlFullPath = string.Empty;

        public struct VariableConfig
        {
            public string variableAddress;
            public LoggingType loggingType;
            public int pollingRefreshTime;
            public bool recording;
        }

        public enum LoggingType
        {
            Polling,
            OnChange
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
            xmlFullPath = _variableConfiguration["XMLFileFullPath"];

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
                string xPath = string.Concat("descendant::VariableConfig[variableAddress='", variableConfig.variableAddress, "']");
                XmlNode variableNode = rootNode.SelectSingleNode(xPath);

                if (variableNode != null)
                {
                    // If it exists update it
                    foreach (XmlNode item in variableNode)
                    {
                        switch (item.Name)
                        {
                            case "loggingType":
                                item.LastChild.Value = variableConfig.loggingType.ToString();
                                break;
                            case "polingRefreshTime":
                                item.LastChild.Value = variableConfig.pollingRefreshTime.ToString();
                                break;
                            case "recording":
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
