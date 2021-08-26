using LiveCharts.Geared;
using METS_DiagnosticTool_Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static METS_DiagnosticTool_Utilities.VariableConfigurationHelper;

namespace METS_DiagnosticTool_UI.UserControls.LiveViewPlot
{
    public class LiveViewPlotVm : INotifyPropertyChanged
    {
        #region Public Properties
        public bool IsReading { get; set; }
        public RelayCommand ClearCommand { get; set; }
        public GearedValues<double> Values { get; set; }
        public Func<double, string> YFormatter { get; set; }

        private RpcClient _rpcClient;
        public RpcClient rpcClient
        {
            get
            {
                return _rpcClient;
            }
            set
            {
                _rpcClient = value;

                if (_rpcClient != null)
                    _rpcClient.PLCVariableLiveViewTriggered += RpcClient_PLCVariableLiveViewTriggered;
                else
                    Logger.Log(Logger.logLevel.Error, "Rabbit MQ Client is null :(", Logger.logEvents.Blank);
            }
        }

        private double _count;
        public double Count
        {
            get { return _count; }
            set
            {
                _count = value;
                OnPropertyChanged("Count");
            }
        }

        private double _currentvalue;
        public double CurrentValue
        {
            get { return _currentvalue; }
            set
            {
                _currentvalue = value;

                OnPropertyChanged("CurrentValue");
            }
        }
        #endregion

        #region Private Fields
        private double _trend;

        private bool _twincatInitializedOK = false; 
        #endregion

        #region Constructor
        public LiveViewPlotVm()
        {
            Values = new GearedValues<double>().WithQuality(Quality.Highest);
            ClearCommand = new RelayCommand(Clear);

            YFormatter = value => value.ToString("N1", CultureInfo.InvariantCulture);
        }

        private void RpcClient_PLCVariableLiveViewTriggered(object sender, string e)
        {
            try
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

                // Initialize new Twincat Connection if not connected already
                string _amsIP = _variableConfiguration["ADSIp"];
                string _amsPort = _variableConfiguration["ADSPort"];

                if (!_twincatInitializedOK)
                    _twincatInitializedOK = TwincatHelper.TwincatInitialization(_amsIP, _amsPort, TwincatHelper.G_ET_EndPoint.DiagnosticToolUI);

                // Create new Variable Config
                bool trigger = bool.Parse(_variableConfiguration["Trigger"]);

                VariableConfig variableConfig = new VariableConfig
                {
                    variableAddress = _variableConfiguration["VariableAddress"],
                    pollingRefreshTime = int.Parse(_variableConfiguration["PollingRefreshTime"]),
                };
                bool loggingTypeParsed = Enum.TryParse(_variableConfiguration["LoggingType"], out LoggingType _loggingType);
                variableConfig.loggingType = loggingTypeParsed ? _loggingType : LoggingType.OnChange;

                if (trigger)
                {
                    // Start Live View Mode Here
                    // Read Data from PLC, based on given configuration and show it on the Plot
                    Read(variableConfig);
                }
                else
                {
                    // End Live View Mode Here
                    Values.Clear();
                    IsReading = false;
                }
            }
            catch (Exception ex)
            {
                Logger.Log(Logger.logLevel.Error, string.Concat("Exception when parsing received request for Live View ", ex.ToString()), Logger.logEvents.Blank);
            }
        }

        private void Read(VariableConfig variableConfig)
        {
            if (IsReading) return;

            //lets keep in memory only the last 20000 records,
            //to keep everything running faster
            const int keepRecords = 20000;
            IsReading = true;

            Action<VariableConfig> readFromTread = (_varConfig) =>
            {
                try
                {
                    while (IsReading)
                    {
                        // Here find declared PLC Variable and read it according to provided Configuration
                        if (!string.IsNullOrEmpty(_varConfig.variableAddress))
                        {
                            // Read PLC Value
                            string test = TwincatHelper.ReadPLCValues(_varConfig.variableAddress, false, TwincatHelper.G_ET_TagType.PLCLRealAndVBDouble);

                            // HERE NEEDS TO BE PARSING ACCORDING TO VARIABLE TYPE
                            _trend = double.Parse(TwincatHelper.ReadPLCValues(_varConfig.variableAddress, true));

                            var first = Values.DefaultIfEmpty(0).FirstOrDefault();
                            if (Values.Count > keepRecords - 1) Values.Remove(first);
                            if (Values.Count < keepRecords) Values.Add(_trend);
                            Count = Values.Count;
                            CurrentValue = _trend;

                            Thread.Sleep(1);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log(Logger.logLevel.Warning, string.Concat("Exception Live View Reading ", ex.ToString()), Logger.logEvents.Blank);
                }
            };

            //add as many tasks as you want to test this feature
            Task.Factory.StartNew(() => readFromTread(variableConfig));
        }
        #endregion

        #region User Input
        private void Clear()
        {
            Values.Clear();
        }
        #endregion

        #region NotificationProperty
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }
}
