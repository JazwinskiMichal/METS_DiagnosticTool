using ScottPlot;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace METS_DiagnosticTool_UI.UserControls
{
    /// <summary>
    /// Logika interakcji dla klasy LiveViewPlot.xaml
    /// </summary>
    public partial class LiveViewPlot : UserControl
    {
        Random rand = new Random();
        double[] liveData = new double[400];
        DataGen.Electrocardiogram ecg = new DataGen.Electrocardiogram();
        Stopwatch sw = Stopwatch.StartNew();

        private Timer _updateDataTimer;
        private DispatcherTimer _renderTimer;

        public LiveViewPlot()
        {
            InitializeComponent();

            // initialize ScottPlot
            liveViewPlot.Plot.Style(ScottPlot.Style.Gray2);
            liveViewPlot.Plot.XAxis.TickLabelStyle(rotation: 45, fontSize: 14, fontName: "Segoe UI", color: System.Drawing.Color.White);
            liveViewPlot.Plot.YAxis.TickLabelStyle(fontSize: 14, fontName: "Segoe UI", color: System.Drawing.Color.White);
            liveViewPlot.Plot.XAxis.Label("Time", color: System.Drawing.Color.White, size: 14, fontName: "Segoe UI");
            liveViewPlot.Plot.YAxis.Label("Value", color: System.Drawing.Color.White, size: 14, fontName: "Segoe UI");

            liveViewPlot.Configuration.MiddleClickAutoAxisMarginX = 0;

            // plot the data array only once
            liveViewPlot.Plot.AddSignal(liveData);
            liveViewPlot.Plot.AxisAutoX(margin: 0);
            liveViewPlot.Plot.SetAxisLimits(yMin: -1, yMax: 2.5);

            // create a traditional timer to update the data
            _updateDataTimer = new Timer(_ => UpdateData(), null, 0, 5);

            // create a separate timer to update the GUI
            _renderTimer = new DispatcherTimer();
            _renderTimer.Interval = TimeSpan.FromMilliseconds(10);
            _renderTimer.Tick += Render;
            _renderTimer.Start();

            //Closed += (sender, args) =>
            //{
            //    _updateDataTimer?.Dispose();
            //    _renderTimer?.Stop();
            //};
        }

        void UpdateData()
        {
            // "scroll" the whole chart to the left
            Array.Copy(liveData, 1, liveData, 0, liveData.Length - 1);

            // place the newest data point at the end
            double nextValue = ecg.GetVoltage(sw.Elapsed.TotalSeconds);
            liveData[liveData.Length - 1] = nextValue;
        }

        void Render(object sender, EventArgs e)
        {
            liveViewPlot.Render();
        }
    }
}
