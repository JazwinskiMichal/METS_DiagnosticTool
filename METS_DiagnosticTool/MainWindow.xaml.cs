using METS_DiagnosticTool_Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using static METS_DiagnosticTool_Utilities.VariableConfigurationHelper;

namespace METS_DiagnosticTool_UI
{
    /// <summary>
    /// Logika interakcji dla klasy MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static string _corePath = string.Empty;

        private static int _rowCount;

        private UserControls.UserInputWithIndicator _row;

        public MainWindow()
        {
            InitializeComponent();

            // Initialize Rabbit MQ Client
            RabbitMQHelper.InitializeClient();

            // Get command Line Arguments Passed from Core
            string[] args = Environment.GetCommandLineArgs();

            if (args.Length > 1)
                _corePath = System.IO.Path.GetDirectoryName(args[1]);

            // Check if other instance of UI is already running
            if (UIHelper.CheckUIRunning())
            {
                // Show old Instance of the UI and kill myself
                UIHelper.ShowUI();
                Environment.Exit(0);
            }
        }

        private void _row_CorrectVariableDeleted(object sender, string e)
        {
            if (Utility.ListOfDeclaredPLCVariables.Contains(e))
                Utility.ListOfDeclaredPLCVariables.Remove(e);
        }

        private void _row_CorrectVariableAdded(object sender, string e)
        {
            Utility.ListOfDeclaredPLCVariables.Add(e);
        }

        private void Label1_AddNewVariableClicked(object sender, EventArgs e)
        {
            //// Create new Control
            //_row = new UserControls.UserInputWithIndicator();

            //// Inject information about Core full Path
            //_row.corePath = _corePath;

            //// Attach Events
            //_row.AddNewVariableClicked += Label1_AddNewVariableClicked;
            //_row.DeleteVariableClicked += Label1_DeleteVariableClicked;
            //_row.CorrectVariableAdded += _row_CorrectVariableAdded;
            //_row.CorrectVariableDeleted += _row_CorrectVariableDeleted;

            //// Add row to the Grid for the Control
            //RowDefinition rowDefinition = new RowDefinition();
            //rowDefinition.Height = new GridLength(0, GridUnitType.Auto);
            //mainGrid.RowDefinitions.Add(rowDefinition);

            //// Set row position of the Control
            //Grid.SetRow(_row, _rowCount);

            //// Add Control to the Grid
            //mainGrid.Children.Add(_row);

            //scrollViewer.ScrollToEnd();

            //_rowCount++;
        }

        private void Label1_DeleteVariableClicked(object sender, UserControls.UserInputWithIndicator e)
        {
            // Delete specific row from the Grid -> thus delete specific Control
            if (mainGrid.Children.Count > 1)
            {
                int _rowToBeDeleted = Grid.GetRow(e);

                mainGrid.Children.Remove(e);
                mainGrid.RowDefinitions.RemoveAt(_rowToBeDeleted);

                // Reposition all the Controls on rows after deleted one
                for (int i = _rowToBeDeleted; i <= mainGrid.Children.Count - 1; i++)
                {
                    Grid.SetRow(mainGrid.Children[i], Grid.GetRow(mainGrid.Children[i]) - 1);
                }

                _rowCount--;
            }
        }

        private void Grid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            //label1.TakeFocusAway();
            //label2.TakeFocusAway();
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Get Variable Configurations String
            Task<string> _getVariablesConfiguration = RabbitMQHelper.SendToServer_ReadPLCVarConfig(RabbitMQHelper.RoutingKeys[(int)RabbitMQHelper.RoutingKeysDictionary.plcVarConfigRead],
                                                                                                   string.Concat(_corePath, @"\XML\VariablesConfiguration.xml"));
            await _getVariablesConfiguration;

            string _varibalesConfigurationString = _getVariablesConfiguration.Result;

            Logger.Log(Logger.logLevel.Warning, string.Concat("recieved variable configuration string", _varibalesConfigurationString), Logger.logEvents.Blank);

            try
            {
                if (!string.IsNullOrEmpty(_varibalesConfigurationString))
                {
                    // Decode given Variable Configuration string to VariableConfig
                    List<string> _splitVariables = _varibalesConfigurationString.Split('#').ToList();

                    // Create dictionary
                    Dictionary<string, VariableConfig> _localDictionary = new Dictionary<string, VariableConfig>();

                    // DECODE HERE GIVEN LONG STRING OF VARIABLES CONFIGURATION AND SAVE IT TO _localDictionary, so later it could be used to inject data to each User Control
                    // Message is going to be given in format #VariableAddress$value;PollingRefreshTime$value... etc
                    foreach (string _variable in _splitVariables)
                    {
                        if (_variable.Contains(";"))
                        {
                            VariableConfig _localVariableConfig = new VariableConfig();

                            List<string> _splitVariablesValues = _variable.Split(';').ToList();

                            string _variableConfigurationKey = string.Empty;

                            foreach (string _variableValue in _splitVariablesValues)
                            {
                                string[] _config = _variableValue.Split('$').ToArray();

                                // First check does the Dictionary already containt a Key
                                if (_config[0] == "VariableAddress")
                                {
                                    _variableConfigurationKey = _config[1];
                                    _localVariableConfig.variableAddress = _config[1];

                                    Logger.Log(Logger.logLevel.Warning, string.Concat("VariableAddress ", _config[1]), Logger.logEvents.Blank);
                                }
                                else if(_config[0] == "PollingRefreshTime")
                                {
                                    _localVariableConfig.pollingRefreshTime = int.Parse(_config[1]);

                                    Logger.Log(Logger.logLevel.Warning, string.Concat("PollingRefreshTime ", int.Parse(_config[1]).ToString()), Logger.logEvents.Blank);
                                }
                                else if(_config[0] == "Recording")
                                {
                                    _localVariableConfig.recording = bool.Parse(_config[1]);

                                    Logger.Log(Logger.logLevel.Warning, string.Concat("Recording ", _config[1]), Logger.logEvents.Blank);
                                }
                                else if(_config[0] == "LoggingType")
                                {
                                    bool loggingTypeParsed = Enum.TryParse(_config[1], out LoggingType _loggingType);
                                    _localVariableConfig.loggingType = loggingTypeParsed ? _loggingType : LoggingType.OnChange;
                                }

                                Utility.SafeUpdateKeyInDictionary(_localDictionary, _variableConfigurationKey, _localVariableConfig);
                            }
                        }
                    }

                    foreach (KeyValuePair<string, VariableConfig> _variableConfig in _localDictionary)
                    {
                        // Initialize First Row
                        _row = new UserControls.UserInputWithIndicator(_variableConfig.Value);

                        // Inject information about Core full Path
                        _row.corePath = _corePath;

                        // Attach Events
                        _row.AddNewVariableClicked += Label1_AddNewVariableClicked;
                        _row.DeleteVariableClicked += Label1_DeleteVariableClicked;
                        _row.CorrectVariableAdded += _row_CorrectVariableAdded;
                        _row.CorrectVariableDeleted += _row_CorrectVariableDeleted;

                        // Add row to the Grid for the Control
                        RowDefinition rowDefinition = new RowDefinition();
                        rowDefinition.Height = new GridLength(0, GridUnitType.Auto);
                        mainGrid.RowDefinitions.Add(rowDefinition);

                        // Set row position of the Control
                        Grid.SetRow(_row, _rowCount);

                        // Add Control to the Grid
                        mainGrid.Children.Add(_row);

                        scrollViewer.ScrollToEnd();

                        _rowCount++;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log(Logger.logLevel.Warning, string.Concat("Exception when parsing variable configuration string ", ex.ToString()), Logger.logEvents.Blank);
            }
        }
    }
}
