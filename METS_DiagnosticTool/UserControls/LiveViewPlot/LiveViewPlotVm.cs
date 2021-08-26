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
                {
                    _rpcClient.PLCVariableLiveViewTriggered += RpcClient_PLCVariableLiveViewTriggered;

                    Logger.Log(Logger.logLevel.Error, "PLCVariableLiveViewTriggered Event attached", Logger.logEvents.Blank);
                }
                    
                else
                    Logger.Log(Logger.logLevel.Error, "Rabbit MQ Client is null :(", Logger.logEvents.Blank);
            }
        }

        private double _trend;
        private double _count;
        private double _currentvalue;

        public LiveViewPlotVm()
        {
            Values = new GearedValues<double>().WithQuality(Quality.Highest);
            ReadCommand = new RelayCommand(Read);
            StopCommand = new RelayCommand(Stop);
            CleaCommand = new RelayCommand(Clear);

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
                    Logger.Log(Logger.logLevel.Warning, "Live View Requested", Logger.logEvents.Blank);
                }
                else
                {
                    // End Live View Mode Here
                }
            }
            catch (Exception ex )
            {
                Logger.Log(Logger.logLevel.Error, string.Concat("Exception when parsing received request for Live View ", ex.ToString()), Logger.logEvents.Blank);
            }
        }

        public bool IsReading { get; set; }
        public RelayCommand ReadCommand { get; set; }
        public RelayCommand StopCommand { get; set; }
        public RelayCommand CleaCommand { get; set; }
        public GearedValues<double> Values { get; set; }

        public Func<double, string> YFormatter { get; set; }

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

        private void Stop()
        {
            IsReading = false;
        }

        private void Clear()
        {
            Values.Clear();
        }

        private void Read()
        {
            if (IsReading) return;

            //lets keep in memory only the last 20000 records,
            //to keep everything running faster
            const int keepRecords = 20000;
            IsReading = true;

            Action readFromTread = () =>
            {
                while (IsReading)
                {
                    Thread.Sleep(1);
                    var r = new Random();
                    _trend += (r.NextDouble() < 0.5 ? 1 : -1) * r.Next(0, 10) * .001;
                    //when multi threading avoid indexed calls like -> Values[0] 
                    //instead enumerate the collection
                    //ChartValues/GearedValues returns a thread safe copy once you enumerate it.
                    //TIPS: use foreach instead of for
                    //LINQ methods also enumerate the collections
                    var first = Values.DefaultIfEmpty(0).FirstOrDefault();
                    if (Values.Count > keepRecords - 1) Values.Remove(first);
                    if (Values.Count < keepRecords) Values.Add(_trend);
                    Count = Values.Count;
                    CurrentValue = _trend;
                }
            };

            //add as many tasks as you want to test this feature
            Task.Factory.StartNew(readFromTread);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
