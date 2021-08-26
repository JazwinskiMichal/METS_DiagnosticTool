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

namespace METS_DiagnosticTool_UI.UserControls.LiveViewPlot
{
    /// <summary>
    /// Logika interakcji dla klasy LiveViewPlot.xaml
    /// </summary>
    public partial class LiveViewPlot : UserControl, IDisposable
    {
        //public METS_DiagnosticTool_Utilities.RpcClient rpcClient;

        private METS_DiagnosticTool_Utilities.RpcClient _rpcClient;
        public METS_DiagnosticTool_Utilities.RpcClient rpcClient 
        { 
            get
            {
                return _rpcClient;
            }
            set
            {
                _rpcClient = value;

                // Inject RPC Server Instance
                LiveViewPlotVm vm = (LiveViewPlotVm)DataContext;
                vm.rpcClient = _rpcClient;
            }
        }

        public LiveViewPlot()
        {
            InitializeComponent();
        }

        public void Dispose()
        {
            LiveViewPlotVm vm = (LiveViewPlotVm)DataContext;
            vm.Values.Dispose();
        }
    }
}
