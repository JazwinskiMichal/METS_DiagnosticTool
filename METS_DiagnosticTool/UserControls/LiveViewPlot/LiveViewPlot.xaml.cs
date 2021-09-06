using LiveCharts;
using LiveCharts.Configurations;
using METS_DiagnosticTool_Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using static METS_DiagnosticTool_Utilities.VariableConfigurationHelper;

namespace METS_DiagnosticTool_UI.UserControls.LiveViewPlot
{
    /// <summary>
    /// Logika interakcji dla klasy LiveViewPlot.xaml
    /// </summary>
    public partial class LiveViewPlot : UserControl, INotifyPropertyChanged
    {
        private bool _twincatInitializedOK = false;
        private double _axisMax;
        private double _axisMin;
        private double _trend;

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
            }
        }

        public LiveViewPlot()
        {
            InitializeComponent();

            var mapper = Mappers.Xy<LiveViewDataModel>()
                .X(model => model.DateTime.Ticks)   //use DateTime.Ticks as X
                .Y(model => model.Value);           //use the value property as Y

            //lets save the mapper globally.
            Charting.For<LiveViewDataModel>(mapper);

            //the values property will store our values array
            ChartValues = new ChartValues<LiveViewDataModel>();

            //lets set how to display the X Labels
            DateTimeFormatter = value => new DateTime((long)value).ToString("HH:mm:ss");

            //AxisStep forces the distance between each separator in the X axis
            AxisStep = TimeSpan.FromSeconds(1).Ticks;
            //AxisUnit lets the axis know that we are plotting seconds
            //this is not always necessary, but it can prevent wrong labeling
            AxisUnit = TimeSpan.TicksPerSecond;

            SetAxisLimits(DateTime.Now);

            IsReading = false;

            DataContext = this;
        }

        private void SetAxisLimits(DateTime now)
        {
            AxisMax = now.Ticks + TimeSpan.FromSeconds(1).Ticks; // lets force the axis to be 1 second ahead
            AxisMin = now.Ticks - TimeSpan.FromSeconds(10).Ticks; // and 10 seconds behind
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
                    ChartValues.Clear();
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
            const int keepRecords = 1000;
            IsReading = true;

            Action<VariableConfig> readFromTread = (_varConfig) =>
            {
                try
                {
                    double _trendOld = 0;

                    while (IsReading)
                    {
                        DateTime now = DateTime.Now;

                        // Here find declared PLC Variable and read it according to provided Configuration
                        if (!string.IsNullOrEmpty(_varConfig.variableAddress))
                        {
                            switch (_varConfig.loggingType)
                            {
                                case LoggingType.Polling:
                                    // HERE NEEDS TO BE PARSING ACCORDING TO VARIABLE TYPE
                                    _trend = double.Parse(TwincatHelper.ReadPLCValues(_varConfig.variableAddress, true));

                                    now = DateTime.Now;

                                    ChartValues.Add(new LiveViewDataModel
                                    {
                                        DateTime = now,
                                        Value = _trend
                                    });

                                    SetAxisLimits(now);

                                    if (ChartValues.Count > keepRecords - 1) ChartValues.RemoveAt(0);
                                    Count = ChartValues.Count;
                                    CurrentValue = _trend;

                                    Thread.Sleep(_varConfig.pollingRefreshTime);
                                    break;

                                case LoggingType.OnChange:
                                    // HERE NEEDS TO BE PARSING ACCORDING TO VARIABLE TYPE
                                    _trend = double.Parse(TwincatHelper.ReadPLCValues(_varConfig.variableAddress, true));

                                    if ((_trend != _trendOld) || (_trendOld == 0 && _trend == 0))
                                    {
                                        now = DateTime.Now;

                                        ChartValues.Add(new LiveViewDataModel
                                        {
                                            DateTime = now,
                                            Value = _trend
                                        });

                                        SetAxisLimits(now);

                                        if (ChartValues.Count > keepRecords - 1) ChartValues.RemoveAt(0);
                                        Count = ChartValues.Count;
                                        CurrentValue = _trend;

                                        _trendOld = _trend;
                                    }
                                    break;

                                default:
                                    break;
                            }
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

        public ChartValues<LiveViewDataModel> ChartValues { get; set; }
        public Func<double, string> DateTimeFormatter { get; set; }
        public double AxisStep { get; set; }
        public double AxisUnit { get; set; }
        public bool IsReading { get; set; }

        public double AxisMax
        {
            get { return _axisMax; }
            set
            {
                _axisMax = value;
                OnPropertyChanged("AxisMax");
            }
        }
        public double AxisMin
        {
            get { return _axisMin; }
            set
            {
                _axisMin = value;
                OnPropertyChanged("AxisMin");
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

        #region INotifyPropertyChanged implementation

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName = null)
        {
            if (PropertyChanged != null)
                PropertyChanged.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}
