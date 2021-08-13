using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
        // Example Variable Address
        private const string exampleOKVariableAddress = ".sBridge.nWatchdog";
        // PlaceHolder Text
        private const string inputPlaceHolderText = "Enter Variable address here...";

        // Storyboards Names
        private const string indicatorOK_Pop = "indicatorOK_Pop";
        private const string indicatorNOK_Shake = "indicatorNOK_Shake";
        private const string recordingDot_ON_Pulse = "recordingDot_ON_Pulse";
        private const string extensionRow_IncreaseHeight = "extensionRow_IncreaseHeight";
        private const string extensionRow_DecreaseHeight = "extensionRow_DecreaseHeight";
        private const string extensionRow_ShowBounceDown = "extensionRow_ShowBounceDown";
        private const string extensionRow_ShowRollUp = "extensionRow_ShowRollUp";
        private const string extensionRow_ShowData = "extensionRowDataShow";
        private const string extensionRow_HideData = "extensionRowDataHide";
        private const string extensionRow_LiveView_IncreaseHeight = "extensionRowLiveView_IncreaseHeight";
        private const string extensionRow_LiveView_DecreaseHeight = "extensionRowLiveView_DecreaseHeight";
        private const string extensionRow_LiveView_ShowBounceDown = "extensionRowLiveView_ShowBounceDown";
        private const string extensionRow_LiveView_ShowRollUp = "extensionRowLiveView_ShowRollUp";
        // Flag to indicate that Storyboard has completed
        private bool bOKPopCompleted = false;
        private bool bNOKShakeCompleted = false;
        private bool bExtensionRowCompleted = false;
        private bool bExtensionRowAnimationCompleted = false;

        // Variable Configuration
        private bool bPollingActive = false;
        private bool bOnChangeActive = false;
        private bool bRecordingActive = false;

        // Colors
        private const string defaultGrayColor = "#FFB4B4B4";
        private const string defaultWhiteColor = "#FFFFFF";
        private const string defaultBlackColor = "#000000";

        // Buttons Lists
        private List<UserInputWithIndicator_Image> configurationButtons = new List<UserInputWithIndicator_Image>();
        private List<UserInputWithIndicator_Image> liveViewButtons = new List<UserInputWithIndicator_Image>();
        private List<UserInputWithIndicator_Image> pollingButtons = new List<UserInputWithIndicator_Image>();
        private List<UserInputWithIndicator_Image> onChangeButtons = new List<UserInputWithIndicator_Image>();
        private List<UserInputWithIndicator_Image> recordingButtons = new List<UserInputWithIndicator_Image>();
        #endregion

        #region Default Constructor
        public UserInputWithIndicator()
        {
            InitializeComponent();

            // Initialize List of Buttons
            configurationButtons.Add(configurationDisabled);
            configurationButtons.Add(configurationEnabled);
            liveViewButtons.Add(liveViewDisabled);
            liveViewButtons.Add(liveViewEnabled);
            pollingButtons.Add(pollingON);
            pollingButtons.Add(pollingOFF);
            onChangeButtons.Add(onChangeON);
            onChangeButtons.Add(onChangeOFF);
            recordingButtons.Add(recordingOFF);
            recordingButtons.Add(recordingON);

            // Attach Completed Event Handlers to every Storyboard
            ((Storyboard)Resources[indicatorOK_Pop]).Completed += new EventHandler(indicatorOK_Pop_Completed);
            ((Storyboard)Resources[indicatorNOK_Shake]).Completed += new EventHandler(indicatorNOK_Shake_Completed);
            ((Storyboard)Resources[recordingDot_ON_Pulse]).Completed += new EventHandler(recordingDot_ON_Pulse_Completed);
            ((Storyboard)Resources[extensionRow_IncreaseHeight]).Completed += new EventHandler(extensionRow_IncreaseHeight_Completed);
            ((Storyboard)Resources[extensionRow_DecreaseHeight]).Completed += new EventHandler(extensionRow_DecreaseHeight_Completed);
            ((Storyboard)Resources[extensionRow_ShowBounceDown]).Completed += new EventHandler(extensionRow_ShowBounceDown_Completed);
            ((Storyboard)Resources[extensionRow_ShowRollUp]).Completed += new EventHandler(extensionRow_ShowRollUp_Completed);
            ((Storyboard)Resources[extensionRow_ShowData]).Completed += new EventHandler(extensionRow_ShowData_Completed);
            ((Storyboard)Resources[extensionRow_HideData]).Completed += new EventHandler(extensionRow_HideData_Completed);

            // By default Set Configuration Buttons to Diable
            BringToFrontAndSendOtherBack(pollingButtons, pollingOFF);
            lblPolling.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(defaultGrayColor));

            pollingConfiguration.IsEnabled = false;
            lblRefreshTime.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(defaultGrayColor));
            lblRefreshTimeMs.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(defaultGrayColor));

            BringToFrontAndSendOtherBack(onChangeButtons, onChangeOFF);
            lblOnChange.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(defaultGrayColor));

            // initialize ScottPlot
            double[] dataX = new double[] { 1, 2, 3, 4, 5 };
            double[] dataY = new double[] { 1, 4, 9, 16, 25 };
            liveViewPlot.Plot.AddScatter(dataX, dataY);
            liveViewPlot.Plot.Style(ScottPlot.Style.Gray2);
        }
        #endregion

        #region Public Methods
        public void TakeFocusAway()
        {
            // When lost Focus and Input Field has been left empty, then put placeholder Text again
            if (string.IsNullOrEmpty(input.Text) || input.Text == inputPlaceHolderText)
            {
                input.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(defaultGrayColor));
                input.Text = inputPlaceHolderText;
            }

            Keyboard.ClearFocus();
        }
        #endregion

        #region Private Methods
        private void BringToFrontAndSendOtherBack(List<UserInputWithIndicator_Image> buttons, UserInputWithIndicator_Image givenImage)
        {
            foreach (UserInputWithIndicator_Image button in buttons)
            {
                if (givenImage.Name == button.Name)
                    button.Visibility = Visibility.Visible;
                else
                    button.Visibility = Visibility.Hidden;
            }
        }
        #endregion

        #region User Input
        #region Variable Address Input
        private void input_GotFocus(object sender, RoutedEventArgs e)
        {
            // When got Focus clear Placeholder text and change Font Color
            if (input.Text == inputPlaceHolderText)
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
                input.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(defaultGrayColor));
                input.Text = inputPlaceHolderText;
            }
        }

        private void input_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Check provided PLC Variable address if it can be found among all PLC variables show OK,
            // if it cant be find show NOK

            if (!string.IsNullOrEmpty(input.Text))
            {
                if (input.Text == exampleOKVariableAddress)
                {
                    if (!bOKPopCompleted)
                    {
                        ((Storyboard)Resources[indicatorOK_Pop]).Begin();
                        bOKPopCompleted = true;

                        // Show Configuration Enabled Button
                        BringToFrontAndSendOtherBack(configurationButtons, configurationEnabled);
                    }
                }
                else if (input.Text != inputPlaceHolderText)
                {
                    if (!bNOKShakeCompleted)
                    {
                        ((Storyboard)Resources[indicatorNOK_Shake]).Begin();
                        bNOKShakeCompleted = true;
                    }

                    // Show Configuration Disabled Button
                    BringToFrontAndSendOtherBack(configurationButtons, configurationDisabled);

                    // If Extension row is Visible Hide it
                    if (bExtensionRowCompleted && bExtensionRowAnimationCompleted)
                    {
                        ((Storyboard)Resources[extensionRow_ShowRollUp]).Begin();
                        ((Storyboard)Resources[extensionRow_DecreaseHeight]).Begin();

                        ((Storyboard)Resources[extensionRow_HideData]).Begin();
                    }

                    // Also Disable Recording
                    if (bRecordingActive)
                    {
                        gridRecordingOFF.Visibility = Visibility.Visible;
                        gridRecordingON.Visibility = Visibility.Hidden;
                        BringToFrontAndSendOtherBack(recordingButtons, recordingOFF);
                        ((Storyboard)Resources[recordingDot_ON_Pulse]).Stop();
                        bRecordingActive = false;
                    }
                }
            }
            else
            {
                // Show Configuration Disabled Button
                BringToFrontAndSendOtherBack(configurationButtons, configurationDisabled);

                // If Extension row is Visible Hide it
                if (bExtensionRowCompleted && bExtensionRowAnimationCompleted)
                {
                    ((Storyboard)Resources[extensionRow_ShowRollUp]).Begin();
                    ((Storyboard)Resources[extensionRow_DecreaseHeight]).Begin();

                    ((Storyboard)Resources[extensionRow_HideData]).Begin();
                }

                // Also Disable Recording
                if (bRecordingActive)
                {
                    gridRecordingOFF.Visibility = Visibility.Visible;
                    gridRecordingON.Visibility = Visibility.Hidden;
                    BringToFrontAndSendOtherBack(recordingButtons, recordingOFF);
                    ((Storyboard)Resources[recordingDot_ON_Pulse]).Stop();
                    bRecordingActive = false;
                }
            }
        }
        #endregion

        #region Variable Configuration
        private void configuration_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (!bExtensionRowCompleted && !bExtensionRowAnimationCompleted)
            {
                // Show extension Row Animation
                ((Storyboard)Resources[extensionRow_IncreaseHeight]).Begin();
                ((Storyboard)Resources[extensionRow_ShowBounceDown]).Begin();

                // Show Data
                ((Storyboard)Resources[extensionRow_ShowData]).Begin();
                variableConfigurationRow.Visibility = Visibility;
                liveViewRow.Visibility = Visibility.Hidden;
            }
            else
            {
                ((Storyboard)Resources[extensionRow_ShowRollUp]).Begin();
                ((Storyboard)Resources[extensionRow_DecreaseHeight]).Begin();

                ((Storyboard)Resources[extensionRow_HideData]).Begin();

                if (bRecordingActive)
                {
                    gridRecordingOFF.Visibility = Visibility.Hidden;
                    gridRecordingON.Visibility = Visibility.Visible;
                    ((Storyboard)Resources[recordingDot_ON_Pulse]).Begin();
                }
                else
                {
                    gridRecordingOFF.Visibility = Visibility.Visible;
                    gridRecordingON.Visibility = Visibility.Hidden;
                    ((Storyboard)Resources[recordingDot_ON_Pulse]).Stop();
                }

            }

            Keyboard.ClearFocus();
        }

        private void logginPolling_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (string.IsNullOrEmpty(refreshTimeInput.Text))
                BringToFrontAndSendOtherBack(recordingButtons, recordingOFF);

            if ((!bPollingActive && bOnChangeActive) || (!bPollingActive && !bOnChangeActive))
            {
                BringToFrontAndSendOtherBack(pollingButtons, pollingON);
                lblPolling.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(defaultWhiteColor));

                pollingConfiguration.IsEnabled = true;
                lblRefreshTime.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(defaultWhiteColor));
                lblRefreshTimeMs.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(defaultWhiteColor));

                BringToFrontAndSendOtherBack(onChangeButtons, onChangeOFF);
                lblOnChange.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(defaultGrayColor));

                bPollingActive = true;
                bOnChangeActive = false;
            }

            Keyboard.ClearFocus();
        }

        private void loggingOnChange_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if ((!bOnChangeActive && bPollingActive) || (!bPollingActive && !bOnChangeActive))
            {
                BringToFrontAndSendOtherBack(pollingButtons, pollingOFF);
                lblPolling.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(defaultGrayColor));

                pollingConfiguration.IsEnabled = false;
                lblRefreshTime.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(defaultGrayColor));
                lblRefreshTimeMs.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(defaultGrayColor));

                BringToFrontAndSendOtherBack(onChangeButtons, onChangeON);
                lblOnChange.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(defaultWhiteColor));

                bPollingActive = false;
                bOnChangeActive = true;
            }

            Keyboard.ClearFocus();
        }

        private void refreshTimeInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (string.IsNullOrEmpty(refreshTimeInput.Text))
                BringToFrontAndSendOtherBack(recordingButtons, recordingOFF);
        }

        private void refreshTimeInput_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void recordingON_MouseDown(object sender, MouseButtonEventArgs e)
        {
            BringToFrontAndSendOtherBack(recordingButtons, recordingOFF);

            bRecordingActive = false;

            Keyboard.ClearFocus();
        }

        private void recordingOFF_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (!string.IsNullOrEmpty(refreshTimeInput.Text) && bPollingActive || bOnChangeActive)
            {
                BringToFrontAndSendOtherBack(recordingButtons, recordingON);

                bRecordingActive = true;
            }

            Keyboard.ClearFocus();
        }
        #endregion

        #region Live View
        private void liveViewDisabled_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // Show extension Row Animation
            ((Storyboard)Resources[extensionRow_LiveView_IncreaseHeight]).Begin();
            ((Storyboard)Resources[extensionRow_LiveView_ShowBounceDown]).Begin();
        }

        private void userControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            liveViewPlot.Width = this.ActualWidth - 50;
        }
        #endregion
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
            ((Storyboard)Resources[recordingDot_ON_Pulse]).Begin();
        }

        private void extensionRow_ShowRollUp_Completed(object sender, EventArgs e)
        {
            bExtensionRowAnimationCompleted = false;
        }

        private void extensionRow_ShowBounceDown_Completed(object sender, EventArgs e)
        {
            bExtensionRowAnimationCompleted = true;
        }

        private void extensionRow_DecreaseHeight_Completed(object sender, EventArgs e)
        {
            bExtensionRowCompleted = false;
        }

        private void extensionRow_IncreaseHeight_Completed(object sender, EventArgs e)
        {
            bExtensionRowCompleted = true;
        }

        private void extensionRow_HideData_Completed(object sender, EventArgs e)
        {
        }

        private void extensionRow_ShowData_Completed(object sender, EventArgs e)
        {
        }


        #endregion
    }
}
