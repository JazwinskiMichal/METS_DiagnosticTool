using LiveCharts;
using LiveCharts.Configurations;
using METS_DiagnosticTool_Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using static METS_DiagnosticTool_Utilities.VariableConfigurationHelper;

namespace METS_DiagnosticTool_UI.UserControls.LiveViewPlot
{
    /// <summary>
    /// Logika interakcji dla klasy LiveViewPlot.xaml
    /// </summary>
    public partial class LiveViewPlot : UserControl, INotifyPropertyChanged
    {
        #region Private Fields
        private RpcClient _rpcClient;
        private bool _twincatInitializedOK = false;
        private double _axisXMax;
        private double _axisXMin;
        private double _axisYMax;
        private double _axisYMin;
        private double _trend;
        private double _count;
        private double _currentvalue;
        private bool _showAutoScaleInactive = true;
        private bool _showAutoScaleActive = false;

        private double _oldMaxValue = 0;
        private double _oldMinValue = 0;

        private DateTime _lastChangeTime = DateTime.Now;
        #endregion

        #region Public Properties
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

        public ChartValues<LiveViewDataModel> ChartValues { get; set; }
        
        public Func<double, string> DateTimeFormatter { get; set; }
        public double AxisXStep { get; set; }
        public double AxisXUnit { get; set; }
        public double AxisYStep { get; set; }
        public double AxisYUnit { get; set; }

        public bool IsReading { get; set; }

        public bool ShowAutoScaleInactive
        {
            get { return _showAutoScaleInactive; }
            set
            {
                _showAutoScaleInactive = value;
                OnPropertyChanged("ShowAutoScaleInactive");
            }
        }

        public bool ShowAutoScaleActive
        {
            get { return _showAutoScaleActive; }
            set
            {
                _showAutoScaleActive = value;
                OnPropertyChanged("ShowAutoScaleActive");
            }
        }

        public bool ShowAutoscaleButtons { get; set; } = false;

        public double AxisXMax
        {
            get { return _axisXMax; }
            set
            {
                _axisXMax = value;
                OnPropertyChanged("AxisXMax");
            }
        }

        public double AxisXMin
        {
            get { return _axisXMin; }
            set
            {
                _axisXMin = value;
                OnPropertyChanged("AxisXMin");
            }
        }

        public double AxisYMax
        {
            get { return _axisYMax; }
            set
            {
                _axisYMax = value;
                OnPropertyChanged("AxisYMax");
            }
        }

        public double AxisYMin
        {
            get { return _axisYMin; }
            set
            {
                _axisYMin = value;
                OnPropertyChanged("AxisYMin");
            }
        }

        public double Count
        {
            get { return _count; }
            set
            {
                _count = value;
                OnPropertyChanged("Count");
            }
        }

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

        #region Constructor
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
            AxisXStep = TimeSpan.FromSeconds(1).Ticks;
            //AxisUnit lets the axis know that we are plotting seconds
            //this is not always necessary, but it can prevent wrong labeling
            AxisXUnit = TimeSpan.TicksPerSecond;

            SetAxisYLimits(100);

            SetAxisXLimits(DateTime.Now);

            IsReading = false;

            DataContext = this;
        }
        #endregion

        #region Private Methods
        private void SetAxisXLimits(DateTime now)
        {
            AxisXMax = now.Ticks + TimeSpan.FromSeconds(1).Ticks; // lets force the axis to be 1 second ahead
            AxisXMin = now.Ticks - TimeSpan.FromSeconds(10).Ticks; // and 10 seconds behind
        }

        private void SetAxisYLimits(double value)
        {
            if(value > 0)
            {
                if (value > _oldMaxValue)
                {
                    _oldMaxValue = value;

                    AxisYMax = _oldMaxValue;
                }
            }
            else
            {
                if(value < _oldMinValue)
                {
                    _oldMinValue = value;

                    AxisYMin = _oldMinValue;
                }
            }
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

            //lets keep in memory only the last 1000 records,
            //to keep everything running faster, and that's also exact length of a whole X axis
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

                                    SetAxisXLimits(now);
                                    SetAxisYLimits(_trend);

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

                                        if(_lastChangeTime != now)
                                        {
                                            SetAxisXLimits(_lastChangeTime);

                                            _lastChangeTime = now;
                                        }

                                        SetAxisYLimits(_trend);

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
        #endregion

        #region User Input
        private void InjectStopOnClick(object sender, RoutedEventArgs e)
        {
            ChartValues.Clear();
        }

        private void autoScaleON_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            ShowAutoScaleActive = false;
            ShowAutoScaleInactive = true;
        }

        private void autoscaleOFF_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            ShowAutoScaleActive = true;
            ShowAutoScaleInactive = false;
        }
        #endregion

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
