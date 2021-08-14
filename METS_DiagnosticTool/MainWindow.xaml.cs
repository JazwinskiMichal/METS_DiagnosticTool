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

namespace METS_DiagnosticTool_UI
{
    /// <summary>
    /// Logika interakcji dla klasy MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static int _rowNumber = 0;
        private UserControls.UserInputWithIndicator _row;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Grid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            //label1.TakeFocusAway();
            //label2.TakeFocusAway();
        }

        private void label1_MouseDown(object sender, MouseButtonEventArgs e)
        {
            _row = new UserControls.UserInputWithIndicator();
            Grid.SetRow(_row, ++_rowNumber);
            RowDefinition rowDefinition = new RowDefinition();
            rowDefinition.Height = new GridLength(0, GridUnitType.Auto);
            mainGrid.RowDefinitions.Add(rowDefinition);
            mainGrid.Children.Add(_row);
        }
    }
}
