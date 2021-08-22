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

            // Initialize First Row
            _row = new UserControls.UserInputWithIndicator();

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
            Grid.SetRow(_row, 0);

            // Add Control to the Grid
            mainGrid.Children.Add(_row);

            scrollViewer.ScrollToEnd();

            _rowCount++;
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
            // Create new Control
            _row = new UserControls.UserInputWithIndicator();

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
    }
}
