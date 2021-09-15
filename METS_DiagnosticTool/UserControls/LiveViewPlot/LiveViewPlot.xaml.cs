using LiveCharts;
using LiveCharts.Configurations;
using METS_DiagnosticTool_Utilities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Threading;
using static METS_DiagnosticTool_Utilities.VariableConfigurationHelper;

namespace METS_DiagnosticTool_UI.UserControls.LiveViewPlot
{
    /// <summary>
    /// Logika interakcji dla klasy LiveViewPlot.xaml
    /// </summary>
    public partial class LiveViewPlot : UserControl, INotifyPropertyChanged, IDisposable
    {
        #region Private Fields
        #region Global
        private RpcClient _rpcClient;
        private bool _twincatInitializedOK = false;

        private string _variableType = string.Empty;
        #endregion

        #region LiveView Cartesian Chart
        private double _axisXMax;
        private double _axisXMin;
        private double _axisYMax;
        private double _axisYMin;
        private double _trend;
        private double _count;
        private string _currentvalue;
        private string _previousValue;
        private string _registeredAt;
        private bool _showPreviousValues = false;
        private string _currentTime;
        private bool _showCartesianChart = true;
        #endregion

        #region Live View ListBox
        private bool _showListBox = false;
        private bool _showPauseRestart = false;
        private bool _bPause = false;
        private bool _bNewItemAdded = false;
        private double _fScrollVerticalOffset = 0;

        private object _lock = new object();
        #endregion
        #endregion

        #region Public Properties
        #region Global Public Properties
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

        public string VariableType
        {
            get { return _variableType; }
            set
            {
                _variableType = value;
                OnPropertyChanged("VariableType");
            }
        }
        #endregion

        #region Live View Chart
        public ChartValues<LiveViewDataModel> ChartValues { get; set; }

        public Func<double, string> DateTimeFormatter { get; set; }
        public double AxisXStep { get; set; }
        public double AxisXUnit { get; set; }
        public double AxisYStep { get; set; }
        public double AxisYUnit { get; set; }

        public bool IsReadingCartesianChart { get; set; }

        public bool ShowPreviousValues
        {
            get { return _showPreviousValues; }
            set
            {
                _showPreviousValues = value;
                OnPropertyChanged("ShowPreviousValues");
            }
        }

        public bool ShowCartesianChart
        {
            get { return _showCartesianChart; }
            set
            {
                _showCartesianChart = value;
                OnPropertyChanged("ShowCartesianChart");
            }
        }

        public bool ShowListBox
        {
            get { return _showListBox; }
            set
            {
                _showListBox = value;
                OnPropertyChanged("ShowListBox");
            }
        }

        public string RegisteredAt
        {
            get { return _registeredAt; }
            set
            {
                _registeredAt = value;
                OnPropertyChanged("RegisteredAt");
            }
        }

        public string PreviousValue
        {
            get { return _previousValue; }
            set
            {
                _previousValue = value;
                OnPropertyChanged("PreviousValue");
            }
        }

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

        public string CurrentValue
        {
            get { return _currentvalue; }
            set
            {
                _currentvalue = value;

                OnPropertyChanged("CurrentValue");
            }
        }

        public string CurrentTime
        {
            get { return _currentTime; }
            set
            {
                _currentTime = value;

                OnPropertyChanged("CurrentTime");
            }
        }
        #endregion

        #region Live View ListBox
        public bool IsReadingListBox { get; set; }

        public int LstBoxDataSizeLimit { get; set; } = 200;

        private LimitedSizeObservableCollection<LiveViewListBoxDataModel> _lstBoxLiveViewData;
        public LimitedSizeObservableCollection<LiveViewListBoxDataModel> LstBoxLiveViewData
        {
            get { return _lstBoxLiveViewData; }
            set
            {
                if (value != this._lstBoxLiveViewData)
                    _lstBoxLiveViewData = value;
                OnPropertyChanged("LstBoxLiveViewData");
            }
        }

        public bool ShowPauseRestart
        {
            get { return _showPauseRestart; }
            set
            {
                _showPauseRestart = value;
                OnPropertyChanged("ShowPauseRestart");
            }
        }

        public bool Pause
        {
            get { return _bPause; }
            set
            {
                _bPause = value;
                OnPropertyChanged("Pause");
            }
        }

        public bool NewItemAdded
        {
            get { return _bNewItemAdded; }
            set
            {
                _bNewItemAdded = value;
                OnPropertyChanged("NewItemAdded");
            }
        }

        public double ScrollVerticalOffset
        {
            get { return _fScrollVerticalOffset; }
            set
            {
                _fScrollVerticalOffset = value;
                OnPropertyChanged("ScrollVerticalOffset");
            }
        }
        #endregion
        #endregion

        #region Constructor
        public LiveViewPlot()
        {
            InitializeComponent();

            CartesianMapper<LiveViewDataModel> mapper = Mappers.Xy<LiveViewDataModel>()
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

            AxisYMin = 0;

            SetAxisXLimits(DateTime.Now);

            IsReadingCartesianChart = false;
            IsReadingListBox = false;

            DataContext = this;
        }

        public void Dispose()
        {
            _rpcClient.PLCVariableLiveViewTriggered -= RpcClient_PLCVariableLiveViewTriggered;
            if (ChartValues != null) ChartValues.Clear();
            if (LstBoxLiveViewData != null)
            {
                LstBoxLiveViewData.Clear();
                LstBoxLiveViewData.CollectionChanged -= LstBoxLiveViewData_CollectionChanged;
            }
            IsReadingCartesianChart = false;
            IsReadingListBox = false;
            DataContext = null;
        }
        #endregion

        #region Private Methods
        private void SetAxisXLimits(DateTime now)
        {
            AxisXMax = now.Ticks + TimeSpan.FromSeconds(1).Ticks; // lets force the axis to be 1 second ahead
            AxisXMin = now.Ticks - TimeSpan.FromSeconds(10).Ticks;
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
                    // Based on Variable Type Show either Cartesian Chart or ListBox for string Values
                    switch (TwincatHelper.GetSymbolType(variableConfig.variableAddress))
                    {
                        case TwincatHelper.G_ET_TagType.PLCFloatAndVBSingle:
                        case TwincatHelper.G_ET_TagType.PLCBooleanAndVBBoolean:
                        case TwincatHelper.G_ET_TagType.PLCDwordAndVBUint:
                        case TwincatHelper.G_ET_TagType.PLCIntegerAndVBShort:
                        case TwincatHelper.G_ET_TagType.PLCDintAndVBInt:
                        case TwincatHelper.G_ET_TagType.PLCLRealAndVBDouble:
                        case TwincatHelper.G_ET_TagType.PLCUIntegerAndVBUShort:
                        case TwincatHelper.G_ET_TagType.PLCByte:
                            ShowCartesianChart = true;
                            ShowListBox = false;

                            // Get Variable Datatype Name
                            VariableType = TwincatHelper.VariableDatatype;

                            ReadCartesianChart(variableConfig);
                            break;
                        case TwincatHelper.G_ET_TagType.PLCTime:
                        case TwincatHelper.G_ET_TagType.PLCEnum:
                        case TwincatHelper.G_ET_TagType.PLCString:
                        case TwincatHelper.G_ET_TagType.PLCDate:
                        case TwincatHelper.G_ET_TagType.PLCTOD:
                        case TwincatHelper.G_ET_TagType.PLCDT:
                            // Initialize Observable collection for List Box Live View
                            LstBoxLiveViewData = new LimitedSizeObservableCollection<LiveViewListBoxDataModel>(LstBoxDataSizeLimit);
                            BindingOperations.EnableCollectionSynchronization(LstBoxLiveViewData, _lock);

                            LstBoxLiveViewData.CollectionChanged += LstBoxLiveViewData_CollectionChanged;
                            // For ListBox dont Show Previous Values
                            ShowPreviousValues = false;
                            ShowCartesianChart = false;
                            ShowListBox = true;

                            // Get Variable Datatype Name
                            VariableType = TwincatHelper.VariableDatatype;

                            ReadListBox(variableConfig);
                            break;
                        default:
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log(Logger.logLevel.Error, string.Concat("Exception when parsing received request for Live View ", ex.ToString()), Logger.logEvents.Blank);
            }
        }

        private void ReadListBox(VariableConfig variableConfig)
        {
            if (IsReadingListBox) return;

            IsReadingListBox = true;

            Action<VariableConfig> readFromTread = (_varConfig) =>
            {
                try
                {
                    while (IsReadingListBox)
                    {
                        string _value = string.Empty;

                        // Here find declared PLC Variable and read it according to provided Configuration
                        if (!string.IsNullOrEmpty(_varConfig.variableAddress))
                        {
                            switch (_varConfig.loggingType)
                            {
                                case LoggingType.Polling:

                                    if (!ShowPauseRestart) ShowPauseRestart = true;

                                    if (!Pause)
                                    {
                                        _value = TwincatHelper.ReadPLCValues(_varConfig.variableAddress).ToString();
                                        LstBoxLiveViewData.Insert(new LiveViewListBoxDataModel { TimeStamp = DateTime.Now, Value = string.IsNullOrEmpty(_value) ? "string.Empty" : _value });

                                        Thread.Sleep(_varConfig.pollingRefreshTime);
                                    }
                                    else
                                        NewItemAdded = false;

                                    break;

                                case LoggingType.OnChange:

                                    if (ShowPauseRestart) ShowPauseRestart = false;

                                    _value = TwincatHelper.ReadPLCValues(_varConfig.variableAddress).ToString();

                                    if (LstBoxLiveViewData.Count > 0)
                                    {
                                        if (_value != LstBoxLiveViewData.FirstOrDefault().Value && !string.IsNullOrEmpty(_value) && !string.IsNullOrEmpty(LstBoxLiveViewData.FirstOrDefault().Value))
                                            LstBoxLiveViewData.Insert(new LiveViewListBoxDataModel { TimeStamp = DateTime.Now, Value = string.IsNullOrEmpty(_value) ? "string.Empty" : _value });
                                        else if (string.IsNullOrEmpty(_value) || string.IsNullOrEmpty(LstBoxLiveViewData.FirstOrDefault().Value))
                                        {
                                            // If String empty insert it only once
                                            LstBoxLiveViewData.InsertOnce(new LiveViewListBoxDataModel { TimeStamp = DateTime.Now, Value = "string.Empty" });
                                        }
                                    }
                                    else
                                        LstBoxLiveViewData.InsertOnce(new LiveViewListBoxDataModel { TimeStamp = DateTime.Now, Value = string.IsNullOrEmpty(_value) ? "string.Empty" : _value });

                                    break;

                                default:
                                    break;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log(Logger.logLevel.Warning, string.Concat("Exception Live View ListBox Reading ", ex.ToString()), Logger.logEvents.Blank);
                }
            };

            //add as many tasks as you want to test this feature
            Task.Factory.StartNew(() => readFromTread(variableConfig));
        }

        private void ReadCartesianChart(VariableConfig variableConfig)
        {
            if (IsReadingCartesianChart) return;

            //lets keep in memory only the last 1000 records,
            //to keep everything running faster, and that's also exact length of a whole X axis
            const int keepRecords = 1000;
            IsReadingCartesianChart = true;

            Action<VariableConfig> readFromTread = (_varConfig) =>
            {
                try
                {
                    bool _boolean = false;
                    bool _parsed = false;
                    double _trendOld = 0;

                    while (IsReadingCartesianChart)
                    {
                        DateTime now = DateTime.Now;

                        // Here find declared PLC Variable and read it according to provided Configuration
                        if (!string.IsNullOrEmpty(_varConfig.variableAddress))
                        {
                            // Parsing the PLC Variable Type, actually jusdt checking is it Boolean or other Numeric Type
                            _parsed = double.TryParse(TwincatHelper.ReadPLCValues(_varConfig.variableAddress), out double _result1);

                            if (_parsed)
                            {
                                _trend = _result1;

                                _boolean = false;
                            }
                            else
                            {
                                TwincatHelper.G_ET_TagType _symbolType = TwincatHelper.GetSymbolType(_varConfig.variableAddress);

                                switch (_symbolType)
                                {
                                    case TwincatHelper.G_ET_TagType.PLCBooleanAndVBBoolean:
                                        _trend = bool.Parse(TwincatHelper.ReadPLCValues(_varConfig.variableAddress)) ? 1 : 0;
                                        _boolean = true;
                                        break;
                                    default:
                                        break;
                                }
                            }

                            switch (_varConfig.loggingType)
                            {
                                case LoggingType.Polling:
                                    // For Polling dont Show Previous Values
                                    ShowPreviousValues = false;

                                    now = DateTime.Now;

                                    ChartValues.Add(new LiveViewDataModel
                                    {
                                        DateTime = now,
                                        Value = _trend
                                    });

                                    SetAxisXLimits(now);
                                    if (_trend < 0)
                                        AxisYMin = _trend;
                                    else
                                        AxisYMin = 0;

                                    if (ChartValues.Count > keepRecords - 1) ChartValues.RemoveAt(0);
                                    Count = ChartValues.Count;
                                    CurrentValue = _boolean ? (_trend == 1 ? "True" : "False") : _trend.ToString("F2");
                                    CurrentTime = now.ToString("HH:mm:ss.fff");

                                    Thread.Sleep(_varConfig.pollingRefreshTime);
                                    break;

                                case LoggingType.OnChange:
                                    if (_trend != _trendOld)
                                    {
                                        now = DateTime.Now;

                                        if (ChartValues.Count > 0)
                                        {
                                            ShowPreviousValues = true;
                                            PreviousValue = _boolean ? (ChartValues.Last().Value == 1 ? "True" : "False") : ChartValues.Last().Value.ToString("F2");
                                            RegisteredAt = ChartValues.Last().DateTime.ToString("HH:mm:ss.fff");
                                        }
                                        else
                                            ShowPreviousValues = false;

                                        ChartValues.Add(new LiveViewDataModel
                                        {
                                            DateTime = now,
                                            Value = _trend
                                        });

                                        SetAxisXLimits(now);
                                        if (_trend < 0)
                                            AxisYMin = _trend;
                                        else
                                            AxisYMin = 0;

                                        if (ChartValues.Count > keepRecords - 1) ChartValues.RemoveAt(0);
                                        Count = ChartValues.Count;
                                        CurrentValue = _boolean ? (_trend == 1 ? "True" : "False") : _trend.ToString("F2");
                                        CurrentTime = now.ToString("HH:mm:ss.fff");

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

            CurrentValue = string.Empty;
            CurrentTime = string.Empty;
            ShowPreviousValues = false;

            LstBoxLiveViewData.Clear();
            LstBoxLiveViewData.ClearLock();
        }

        private void lstBoxValues_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            ScrollVerticalOffset = e.VerticalOffset;
        }

        private void lstBoxTimestamps_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            ScrollVerticalOffset = e.VerticalOffset;
        }

        private void LstBoxLiveViewData_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add) 
                NewItemAdded = true;
        }

        private void pauseRestart_Click(object sender, RoutedEventArgs e)
        {
            // Toggle
            Pause = !Pause;
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

    public class LiveViewListBoxDataModel : INotifyPropertyChanged
    {
        private DateTime _timeStamp = DateTime.Now;
        public DateTime TimeStamp
        {
            get { return _timeStamp; }
            set
            {
                if (value != this._timeStamp)
                    _timeStamp = value;
                OnPropertyChanged("TimeStamp");
            }
        }

        private string _value = string.Empty;
        public string Value
        {
            get { return _value; }
            set
            {
                if (value != this._value)
                    _value = value;
                OnPropertyChanged("Value");
            }
        }

        public override string ToString()
        {
            return string.Concat(_timeStamp.ToString("HH:mm:ss.fff"), _value);
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

    public class LimitedSizeObservableCollection<T> : ObservableCollection<T>
    {
        public int Capacity { get; }

        private bool _lock = false;

        public LimitedSizeObservableCollection(int capacity)
        {
            Capacity = capacity;
        }

        public void Insert(T item)
        {
            if (Count >= Capacity)
            {
                this.Remove(this.Last());
            }
            base.Insert(0, item);

            if (_lock)
                _lock = false;
        }

        public void InsertOnce(T item)
        {
            if (Count >= Capacity)
            {
                this.Remove(this.Last());
            }

            if(!_lock)
            {
                base.Insert(0, item);
                _lock = true;
            }
        }

        public void ClearLock()
        {
            _lock = false;
        }
    }
}
