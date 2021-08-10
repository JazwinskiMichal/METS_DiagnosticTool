﻿using System;
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
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace METS_DiagnosticTool_UI.UserControls
{
    /// <summary>
    /// Logika interakcji dla klasy UserInputWithIndicator.xaml
    /// </summary>
    public partial class UserInputWithIndicator : UserControl
    {
        #region Private Fields
        // PlaceHolder Text
        private const string inputPlaceHolderText = "Enter Variable address here...";

        // Example Variable Address
        private const string exampleOKVariableAddress = ".sBridge.nWatchdog";

        // Flag to indicate that Storyboard has completed
        private static bool bOKPopCompleted = false;
        private static bool bNOKShakeCompleted = false;
        #endregion

        #region Default Constructor
        public UserInputWithIndicator()
        {
            InitializeComponent();

            // Attach Completed Event Handlers to every Storyboard
            ((Storyboard)Resources["indicatorOK_Pop"]).Completed += new EventHandler(indicatorOK_Pop_Completed);
            ((Storyboard)Resources["indicatorNOK_Shake"]).Completed += new EventHandler(indicatorNOK_Shake_Completed);
            ((Storyboard)Resources["recordingDot_ON_Pulse"]).Completed += new EventHandler(recordingDot_ON_Pulse_Completed);
        }
        #endregion

        #region Public Methods
        public void TakeFocusAway()
        {
            // When lost Focus and Input Field has been left empty, then put placeholder Text again
            if (string.IsNullOrEmpty(input.Text) || input.Text == inputPlaceHolderText)
            {
                input.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFB4B4B4"));
                input.Text = inputPlaceHolderText;
            }

            Keyboard.ClearFocus();
        }
        #endregion

        #region User Input


        private void input_GotFocus(object sender, RoutedEventArgs e)
        {
            // When got Focus clear Placeholder text and change Font Color
            if(input.Text == inputPlaceHolderText)
            {
                input.Text = string.Empty;
                input.Foreground = Brushes.White;
            }
        }

        private void input_LostFocus(object sender, RoutedEventArgs e)
        {
            // When lost Focus and Input Field has been left empty, then put placeholder Text again
            if (string.IsNullOrEmpty(input.Text) || input.Text == inputPlaceHolderText)
            {
                input.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFB4B4B4"));
                input.Text = inputPlaceHolderText;
            }
        }

        private void input_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Check provided PLC Variable address if it can be found among all PLC variables show OK,
            // if its the address of the table or structure show WARN,
            // if it cant be find show NOK

            if (!string.IsNullOrEmpty(input.Text))
            {
                if (input.Text == exampleOKVariableAddress)
                {
                    if (!bOKPopCompleted)
                    {
                        ((Storyboard)Resources["indicatorOK_Pop"]).Begin();
                        bOKPopCompleted = true;
                    }
                        
                }
                else
                {
                    if (input.Text != inputPlaceHolderText)
                    {
                        if (!bNOKShakeCompleted)
                        {
                            ((Storyboard)Resources["indicatorNOK_Shake"]).Begin();
                            bNOKShakeCompleted = true;
                        }
                    }
                }
            }
        }
        #endregion

        #region Storyboard Events
        private void indicatorNOK_Shake_Completed(object sender, EventArgs e)
        {
            bNOKShakeCompleted = false;
        }

        private void indicatorOK_Pop_Completed(object sender, EventArgs e)
        {
            bOKPopCompleted = false;
        }

        private void recordingDot_ON_Pulse_Completed(object sender, EventArgs e)
        {
            ((Storyboard)Resources["recordingDot_ON_Pulse"]).Begin();
        }
        #endregion

        private void configuration_MouseDown(object sender, MouseButtonEventArgs e)
        {
            ((Storyboard)Resources["recordingClicked"]).Begin();
        }
    }
}
