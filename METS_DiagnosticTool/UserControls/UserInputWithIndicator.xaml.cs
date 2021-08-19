using METS_DiagnosticTool_Utilities;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace METS_DiagnosticTool_UI.UserControls
{
    /// <summary>
    /// Logika interakcji dla klasy UserInputWithIndicator.xaml
    /// </summary>
    public partial class UserInputWithIndicator : UserControl
    {
        #region Private Fields
        // PlaceHolder Text
        private const string inputPlaceHolderText = "empty";

        // Storyboards Names
        private const string indicatorOK_Pop = "indicatorOK_Pop";
        private const string indicatorNOK_Shake = "indicatorNOK_Shake";
        private const string recordingDot_ON_Pulse = "recordingDot_ON_Pulse";
        private const string extensionRow_VarConfig_IncreaseHeight = "extensionRow_IncreaseHeight";
        private const string extensionRow_VarConfig_IncreaseHeightDelayed = "extensionRow_IncreaseHeightDelayed";
        private const string extensionRow_VarConfig_DecreaseHeight = "extensionRow_DecreaseHeight";
        private const string extensionRow_VarConfig_DecreaseHeightDelayed = "extensionRow_DecreaseHeightDelayed";
        private const string extensionRow_VarConfig_ShowBounceDown = "extensionRow_ShowBounceDown";
        private const string extensionRow_VarConfig_ShowBounceDownDelayed = "extensionRow_ShowBounceDownDelayed";
        private const string extensionRow_VarConfig_ShowRollUp = "extensionRow_ShowRollUp";
        private const string extensionRow_VarConfig_ShowRollUpDelayed = "extensionRow_ShowRollUpDelayed";
        private const string extensionRow_VarConfig_ShowData = "extensionRowDataShow";
        private const string extensionRow_VarConfig_ShowDataDelayed = "extensionRowVarConfigDataShowDelayed";
        private const string extensionRow_VarConfig_HideData = "extensionRowDataHide";
        private const string extensionRow_LiveView_IncreaseHeight = "extensionRowLiveView_IncreaseHeight";
        private const string extensionRow_LiveView_IncreaseHeightDelayed = "extensionRowLiveView_IncreaseHeightDelayed";
        private const string extensionRow_LiveView_DecreaseHeight = "extensionRowLiveView_DecreaseHeight";
        private const string extensionRow_LiveView_DecreaseHeightDelayed = "extensionRowLiveView_DecreaseHeightDelayed";
        private const string extensionRow_LiveView_ShowBounceDown = "extensionRowLiveView_ShowBounceDown";
        private const string extensionRow_LiveView_ShowBounceDownDelayed = "extensionRowLiveView_ShowBounceDownDelayed";
        private const string extensionRow_LiveView_ShowRollUp = "extensionRowLiveView_ShowRollUp";
        private const string extensionRow_LiveView_ShowRollUpDelayed = "extensionRowLiveView_ShowRollUpDelayed";
        private const string extensionRow_LiveView_ShowData = "extensionRowLiveViewDataShow";
        private const string extensionRow_LiveView_ShowDataDelayed = "extensionRowLiveViewDataShowDelayed";
        private const string extensionRow_LiveView_HideData = "extensionRowLiveViewDataHide";
        private const string addNewVariable_Hide = "addNewRow_Hide";
        private const string addNewVariable_Show = "addNewRow_Show";
        private const string deleteRow_Hide = "deleteRow_Hide";
        private const string deleteRow_Show = "deleteRow_Show";

        // Flag to indicate that Storyboard has completed
        private bool bOKPopCompleted = false;
        private bool bNOKShakeCompleted = false;

        private bool bExtensionRow_VarConfig_Completed = false;
        private bool bExtensionRow_VarConfigDelayed_Completed = false;
        private bool bExtensionRow_VarConfig_AnimationCompleted = false;
        private bool bExtensionRow_VarConfigDelayed_AnimationCompleted = false;

        private bool bExtensionRow_LiveView_Completed = false;
        private bool bExtensionRow_LiveViewDelayed_Completed = false;
        private bool bExtensionRow_LiveView_AnimationCompleted = false;
        private bool bExtensionRow_LiveViewDelayed_AnimationCompleted = false;

        private bool bDeleteRow_Show_Completed = false;

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

        #region Events
        public event EventHandler AddNewVariableClicked;
        public event EventHandler<UserInputWithIndicator> DeleteVariableClicked;
        #endregion

        #region Default Constructor
        public UserInputWithIndicator()
        {
            InitializeComponent();

            // Initialize Rabbit MQ Client
            RabbitMQHelper.InitializeClient();

            // Initialize List of Buttons
            configurationButtons.Add(configurationDisabled);
            configurationButtons.Add(configurationEnabled);
            configurationButtons.Add(configurationActive);
            liveViewButtons.Add(liveViewDisabled);
            liveViewButtons.Add(liveViewEnabled);
            liveViewButtons.Add(liveViewActive);
            pollingButtons.Add(pollingON);
            pollingButtons.Add(pollingOFF);
            onChangeButtons.Add(onChangeON);
            onChangeButtons.Add(onChangeOFF);
            recordingButtons.Add(recordingOFF);
            recordingButtons.Add(recordingON);
            recordingButtons.Add(recordingDisabled);

            // Attach Completed Event Handlers to every Storyboard
            // Variable Input
            ((Storyboard)Resources[indicatorOK_Pop]).Completed += new EventHandler(indicatorOK_Pop_Completed);
            ((Storyboard)Resources[indicatorNOK_Shake]).Completed += new EventHandler(indicatorNOK_Shake_Completed);
            //((Storyboard)Resources[recordingDot_ON_Pulse]).Completed += new EventHandler(recordingDot_ON_Pulse_Completed);

            // Extension Row Variable Configuration
            ((Storyboard)Resources[extensionRow_VarConfig_IncreaseHeight]).Completed += new EventHandler(extensionRow_VarConfig_IncreaseHeight_Completed);
            ((Storyboard)Resources[extensionRow_VarConfig_IncreaseHeightDelayed]).Completed += new EventHandler(extensionRow_VarConfig_IncreaseHeightDelayed_Completed);
            ((Storyboard)Resources[extensionRow_VarConfig_DecreaseHeight]).Completed += new EventHandler(extensionRow_VarConfig_DecreaseHeight_Completed);
            ((Storyboard)Resources[extensionRow_VarConfig_ShowBounceDown]).Completed += new EventHandler(extensionRow_VarConfig_ShowBounceDown_Completed);
            ((Storyboard)Resources[extensionRow_VarConfig_ShowBounceDownDelayed]).Completed += new EventHandler(extensionRow_VarConfig_ShowBounceDownDelayed_Completed);
            ((Storyboard)Resources[extensionRow_VarConfig_ShowRollUp]).Completed += new EventHandler(extensionRow_VarConfig_ShowRollUp_Completed);

            // Extension Row Live View
            ((Storyboard)Resources[extensionRow_LiveView_IncreaseHeight]).Completed += new EventHandler(extensionRow_LiveView_IncreaseHeight_Completed);
            ((Storyboard)Resources[extensionRow_LiveView_IncreaseHeightDelayed]).Completed += new EventHandler(extensionRow_LiveView_IncreaseHeightDelayed_Completed);
            ((Storyboard)Resources[extensionRow_LiveView_DecreaseHeight]).Completed += new EventHandler(extensionRow_LiveView_DecreaseHeight_Completed);
            ((Storyboard)Resources[extensionRow_LiveView_ShowBounceDown]).Completed += new EventHandler(extensionRow_LiveView_ShowBounceDown_Completed);
            ((Storyboard)Resources[extensionRow_LiveView_ShowBounceDownDelayed]).Completed += new EventHandler(extensionRow_LiveView_ShowBounceDownDelayed_Completed);
            ((Storyboard)Resources[extensionRow_LiveView_ShowRollUp]).Completed += new EventHandler(extensionRow_LiveView_ShowRollUp_Completed);

            // Delete Row
            ((Storyboard)Resources[deleteRow_Show]).Completed += new EventHandler(deleteRow_Show_Completed);
            ((Storyboard)Resources[deleteRow_Hide]).Completed += new EventHandler(deleteRow_Hide_Completed);

            // By default Set Configuration Buttons to Diable
            BringToFrontAndSendOtherBack(pollingButtons, pollingOFF);
            lblPolling.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(defaultGrayColor));

            pollingConfiguration.IsEnabled = false;
            lblRefreshTime.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(defaultGrayColor));
            lblRefreshTimeMs.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(defaultGrayColor));

            BringToFrontAndSendOtherBack(onChangeButtons, onChangeOFF);
            lblOnChange.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(defaultGrayColor));

            BringToFrontAndSendOtherBack(recordingButtons, recordingDisabled);

            // initialize ScottPlot
            double[] dataX = new double[] { 1, 2, 3, 4, 5 };
            double[] dataY = new double[] { 1, 4, 9, 16, 25 };
            liveViewPlot.Plot.AddScatter(dataX, dataY);
            liveViewPlot.Plot.Style(ScottPlot.Style.Gray2);
            liveViewPlot.Plot.XAxis.TickLabelStyle(rotation: 45, fontSize: 14, fontName: "Segoe UI", color: System.Drawing.Color.White);
            liveViewPlot.Plot.YAxis.TickLabelStyle(fontSize: 14, fontName: "Segoe UI", color: System.Drawing.Color.White);
            liveViewPlot.Plot.XAxis.Label("Time", color: System.Drawing.Color.White, size: 14, fontName: "Segoe UI");
            liveViewPlot.Plot.YAxis.Label("Value", color: System.Drawing.Color.White, size: 14, fontName: "Segoe UI");
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

        private void InputVariableNotFound()
        {
            if (!bNOKShakeCompleted)
            {
                ((Storyboard)Resources[indicatorNOK_Shake]).Begin();
                bNOKShakeCompleted = true;
            }

            // Show Configuration Disabled Button and Live View Disabled Button
            BringToFrontAndSendOtherBack(configurationButtons, configurationDisabled);
            BringToFrontAndSendOtherBack(liveViewButtons, liveViewDisabled);

            // If Extension Variable Configuration Row is Visible Hide it
            if ((bExtensionRow_VarConfig_Completed && bExtensionRow_VarConfig_AnimationCompleted) ||
                (bExtensionRow_VarConfigDelayed_Completed && bExtensionRow_VarConfigDelayed_AnimationCompleted))
            {
                ((Storyboard)Resources[extensionRow_VarConfig_ShowRollUp]).Begin();
                ((Storyboard)Resources[extensionRow_VarConfig_DecreaseHeight]).Begin();

                ((Storyboard)Resources[extensionRow_VarConfig_HideData]).Begin();
            }

            // If Extension Live View Row is Visible Hide it
            if ((bExtensionRow_LiveView_Completed && bExtensionRow_LiveView_AnimationCompleted) ||
                (bExtensionRow_LiveViewDelayed_Completed && bExtensionRow_LiveViewDelayed_AnimationCompleted))
            {
                ((Storyboard)Resources[extensionRow_LiveView_ShowRollUp]).Begin();
                ((Storyboard)Resources[extensionRow_LiveView_DecreaseHeight]).Begin();

                ((Storyboard)Resources[extensionRow_LiveView_HideData]).Begin();
            }

            // Also Disable Recording
            DisableRecording();

            // Also Reset Variable Configuration
            BringToFrontAndSendOtherBack(pollingButtons, pollingOFF);
            lblPolling.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(defaultGrayColor));

            pollingConfiguration.IsEnabled = false;
            lblRefreshTime.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(defaultGrayColor));
            lblRefreshTimeMs.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(defaultGrayColor));

            BringToFrontAndSendOtherBack(onChangeButtons, onChangeOFF);
            lblOnChange.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(defaultGrayColor));

            bPollingActive = false;
            bOnChangeActive = false;
        }

        private void DisableRecording()
        {
            if (bRecordingActive)
            {
                gridRecordingOFF.Visibility = Visibility.Visible;
                gridRecordingON.Visibility = Visibility.Hidden;
                BringToFrontAndSendOtherBack(recordingButtons, recordingOFF);
                //((Storyboard)Resources[recordingDot_ON_Pulse]).Stop();
                bRecordingActive = false;
            }
            else
                BringToFrontAndSendOtherBack(recordingButtons, recordingDisabled);
        }
        #endregion

        #region User Input
        #region Delete Row
        private void DeleteRowInactive_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Storyboard _deleteRow_Show = (Storyboard)Resources[deleteRow_Show];
            DoubleAnimationUsingKeyFrames _deleteRow_Show_Anim = (DoubleAnimationUsingKeyFrames)_deleteRow_Show.Children[0];
            _deleteRow_Show_Anim.KeyFrames[0].Value = 0;

            ((Storyboard)Resources[deleteRow_Show]).Begin();

            variableConfigurationRow.IsEnabled = false;
            liveViewRow.IsEnabled = false;
        }

        private void DeleteRowActive_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Storyboard _deleteRow_Hide = (Storyboard)Resources[deleteRow_Hide];
            DoubleAnimationUsingKeyFrames _deleteRow_Hide_Anim = (DoubleAnimationUsingKeyFrames)_deleteRow_Hide.Children[0];
            _deleteRow_Hide_Anim.KeyFrames[0].Value = -ActualWidth;

            ((Storyboard)Resources[deleteRow_Hide]).Begin();

            variableConfigurationRow.IsEnabled = true;
            liveViewRow.IsEnabled = true;
        }

        private void labelYES_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Storyboard _deleteRow_Hide = (Storyboard)Resources[deleteRow_Hide];
            DoubleAnimationUsingKeyFrames _deleteRow_Hide_Anim = (DoubleAnimationUsingKeyFrames)_deleteRow_Hide.Children[0];
            _deleteRow_Hide_Anim.KeyFrames[0].Value = -ActualWidth;

            ((Storyboard)Resources[deleteRow_Hide]).Begin();

            variableConfigurationRow.IsEnabled = true;
            liveViewRow.IsEnabled = true;

            DeleteVariableClicked?.Invoke(this, (UserInputWithIndicator)userControl);
        }

        private void labelNO_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Storyboard _deleteRow_Hide = (Storyboard)Resources[deleteRow_Hide];
            DoubleAnimationUsingKeyFrames _deleteRow_Hide_Anim = (DoubleAnimationUsingKeyFrames)_deleteRow_Hide.Children[0];
            _deleteRow_Hide_Anim.KeyFrames[0].Value = -ActualWidth;

            ((Storyboard)Resources[deleteRow_Hide]).Begin();

            variableConfigurationRow.IsEnabled = true;
            liveViewRow.IsEnabled = true;
        }
        #endregion

        #region Add New Row
        private void addNewRowBorder_MouseDown(object sender, MouseButtonEventArgs e)
        {
            AddNewVariableClicked?.Invoke(this, EventArgs.Empty);

            // Change Value of the addNewVariable_Hide Translate Transform X Storyboard
            Storyboard _addNewRowHide = (Storyboard)Resources[addNewVariable_Hide];
            DoubleAnimationUsingKeyFrames _addNewRowHide_Anim = (DoubleAnimationUsingKeyFrames)_addNewRowHide.Children[0];
            _addNewRowHide_Anim.KeyFrames[0].Value = ActualWidth;

            // And Start AddNewRow Storyboard
            ((Storyboard)Resources[addNewVariable_Hide]).Begin();
        }
        #endregion

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

        private async void input_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Check provided PLC Variable address if it can be found among all PLC variables show OK,
            // if it cant be find show NOK

            if (!string.IsNullOrEmpty(input.Text))
            {
                // Check Existance of the PLC Variable
                Task<string> _checkGivenPLCAddress =  RabbitMQHelper.CallPLCVariableExistanceCheck(RabbitMQHelper.RoutingKeys[(int)RabbitMQHelper.RoutingKeysDictionary.checkPLCVarExistance], input.Text);
                await _checkGivenPLCAddress;

                if (_checkGivenPLCAddress.Result == true.ToString())
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
                    InputVariableNotFound();
                }
            }
            else
            {
                InputVariableNotFound();
            }
        }
        #endregion

        #region Variable Configuration
        private void configuration_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // If Live View Row is is visible show Delayed Animations
            if ((bExtensionRow_LiveView_Completed && bExtensionRow_LiveView_AnimationCompleted) ||
                (bExtensionRow_LiveViewDelayed_Completed && bExtensionRow_LiveViewDelayed_AnimationCompleted))
            {
                // Hide Extension Variable Configuration Row Animation
                ((Storyboard)Resources[extensionRow_LiveView_ShowRollUp]).Begin();
                ((Storyboard)Resources[extensionRow_LiveView_DecreaseHeight]).Begin();

                // Hide Variable Configuration Data
                ((Storyboard)Resources[extensionRow_LiveView_HideData]).Begin();

                // Show extension Row Animation
                ((Storyboard)Resources[extensionRow_VarConfig_IncreaseHeightDelayed]).Begin();
                ((Storyboard)Resources[extensionRow_VarConfig_ShowBounceDownDelayed]).Begin();

                // Change Visibility of the content
                variableConfigurationRow.Visibility = Visibility.Visible;
                liveViewRow.Visibility = Visibility.Hidden;

                ((Storyboard)Resources[extensionRow_VarConfig_ShowDataDelayed]).Begin();

                BringToFrontAndSendOtherBack(configurationButtons, configurationActive);
                BringToFrontAndSendOtherBack(liveViewButtons, liveViewEnabled);
            }
            else
            {
                if (!bExtensionRow_VarConfig_Completed && !bExtensionRow_VarConfig_AnimationCompleted &&
                    !bExtensionRow_VarConfigDelayed_Completed && !bExtensionRow_VarConfigDelayed_AnimationCompleted)
                {
                    // Show Extension Variable Configuration Row Animation
                    ((Storyboard)Resources[extensionRow_VarConfig_IncreaseHeight]).Begin();
                    ((Storyboard)Resources[extensionRow_VarConfig_ShowBounceDown]).Begin();

                    // Show Variable Configuration Data
                    ((Storyboard)Resources[extensionRow_VarConfig_ShowData]).Begin();

                    // Change Visibility of the content
                    variableConfigurationRow.Visibility = Visibility.Visible;
                    liveViewRow.Visibility = Visibility.Hidden;

                    BringToFrontAndSendOtherBack(configurationButtons, configurationActive);
                }
                else
                {
                    // Hide Extension Variable Configuration Row Animation
                    ((Storyboard)Resources[extensionRow_VarConfig_ShowRollUp]).Begin();
                    ((Storyboard)Resources[extensionRow_VarConfig_DecreaseHeight]).Begin();

                    // Hide Variable Configuration Data
                    ((Storyboard)Resources[extensionRow_VarConfig_HideData]).Begin();

                    BringToFrontAndSendOtherBack(configurationButtons, configurationEnabled);
                }
            }

            Keyboard.ClearFocus();
        }

        private void configurationActive_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // Hide Extension Variable Configuration Row Animation
            ((Storyboard)Resources[extensionRow_VarConfig_ShowRollUp]).Begin();
            ((Storyboard)Resources[extensionRow_VarConfig_DecreaseHeight]).Begin();

            // Hide Variable Configuration Data
            ((Storyboard)Resources[extensionRow_VarConfig_HideData]).Begin();

            BringToFrontAndSendOtherBack(configurationButtons, configurationEnabled);

            Keyboard.ClearFocus();
        }

        private void logginPolling_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (string.IsNullOrEmpty(refreshTimeInput.Text))
                DisableRecording();

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

                if (!string.IsNullOrEmpty(refreshTimeInput.Text))
                {
                    BringToFrontAndSendOtherBack(liveViewButtons, liveViewEnabled);
                    if (!bRecordingActive)
                        BringToFrontAndSendOtherBack(recordingButtons, recordingOFF);
                }
                else
                    BringToFrontAndSendOtherBack(liveViewButtons, liveViewDisabled);

                //if (!input.IsEnabled)
                //    input.IsEnabled = true;
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

                BringToFrontAndSendOtherBack(liveViewButtons, liveViewEnabled);

                if (!bRecordingActive)
                    BringToFrontAndSendOtherBack(recordingButtons, recordingOFF);
            }

            Keyboard.ClearFocus();
        }

        private void refreshTimeInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (string.IsNullOrEmpty(refreshTimeInput.Text))
            {
                BringToFrontAndSendOtherBack(recordingButtons, recordingDisabled);

                BringToFrontAndSendOtherBack(liveViewButtons, liveViewDisabled);

                DisableRecording();

                if (!input.IsEnabled)
                    input.IsEnabled = true;
            }
            else
            {
                if (!bOnChangeActive && bPollingActive)
                {
                    BringToFrontAndSendOtherBack(liveViewButtons, liveViewEnabled);
                    BringToFrontAndSendOtherBack(recordingButtons, recordingOFF);
                }

            }
        }

        private void refreshTimeInput_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void recordingON_MouseDown(object sender, MouseButtonEventArgs e)
        {
            BringToFrontAndSendOtherBack(recordingButtons, recordingOFF);

            gridRecordingOFF.Visibility = Visibility.Visible;
            gridRecordingON.Visibility = Visibility.Hidden;
            //((Storyboard)Resources[recordingDot_ON_Pulse]).Stop();

            bRecordingActive = false;

            // Make Vairable Input Field Disabled
            input.IsEnabled = true;

            Keyboard.ClearFocus();
        }

        private void recordingOFF_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // If there is A configuration given
            if (!string.IsNullOrEmpty(refreshTimeInput.Text) && bPollingActive || bOnChangeActive)
            {
                BringToFrontAndSendOtherBack(recordingButtons, recordingON);

                gridRecordingOFF.Visibility = Visibility.Hidden;
                gridRecordingON.Visibility = Visibility.Visible;
                //((Storyboard)Resources[recordingDot_ON_Pulse]).Begin();

                bRecordingActive = true;

                // Make Vairable Input Field Disabled
                input.IsEnabled = false;
            }

            Keyboard.ClearFocus();
        }
        #endregion

        #region Live View
        private void liveViewEnabled_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // If Variable Configuration is visible show Delayed Animations
            if ((bExtensionRow_VarConfig_Completed && bExtensionRow_VarConfig_AnimationCompleted) ||
                (bExtensionRow_VarConfigDelayed_Completed && bExtensionRow_VarConfigDelayed_AnimationCompleted))
            {
                // Hide Extension Variable Configuration Row Animation
                ((Storyboard)Resources[extensionRow_VarConfig_ShowRollUp]).Begin();
                ((Storyboard)Resources[extensionRow_VarConfig_DecreaseHeight]).Begin();

                // Hide Variable Configuration Data
                ((Storyboard)Resources[extensionRow_VarConfig_HideData]).Begin();

                // Show extension Row Animation
                ((Storyboard)Resources[extensionRow_LiveView_IncreaseHeightDelayed]).Begin();
                ((Storyboard)Resources[extensionRow_LiveView_ShowBounceDownDelayed]).Begin();

                // Change Visibility of the content
                variableConfigurationRow.Visibility = Visibility.Hidden;
                liveViewRow.Visibility = Visibility.Visible;

                ((Storyboard)Resources[extensionRow_LiveView_ShowDataDelayed]).Begin();

                BringToFrontAndSendOtherBack(liveViewButtons, liveViewActive);
                BringToFrontAndSendOtherBack(configurationButtons, configurationEnabled);
            }
            else
            {
                if ((!bExtensionRow_LiveView_Completed && !bExtensionRow_LiveView_AnimationCompleted &&
                    !bExtensionRow_LiveViewDelayed_Completed && !bExtensionRow_LiveViewDelayed_AnimationCompleted))
                {
                    // Show extension Row Animation
                    ((Storyboard)Resources[extensionRow_LiveView_IncreaseHeight]).Begin();
                    ((Storyboard)Resources[extensionRow_LiveView_ShowBounceDown]).Begin();

                    // Change Visibility of the content
                    variableConfigurationRow.Visibility = Visibility.Hidden;
                    liveViewRow.Visibility = Visibility.Visible;

                    ((Storyboard)Resources[extensionRow_LiveView_ShowData]).Begin();

                    BringToFrontAndSendOtherBack(liveViewButtons, liveViewActive);
                }
                else
                {
                    // Show extension Row Animation
                    ((Storyboard)Resources[extensionRow_LiveView_ShowRollUp]).Begin();
                    ((Storyboard)Resources[extensionRow_LiveView_DecreaseHeight]).Begin();

                    // Change Visibility of the content
                    variableConfigurationRow.Visibility = Visibility.Hidden;
                    liveViewRow.Visibility = Visibility.Visible;

                    ((Storyboard)Resources[extensionRow_LiveView_HideData]).Begin();

                    BringToFrontAndSendOtherBack(liveViewButtons, liveViewEnabled);
                }
            }
        }

        private void liveViewActive_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // Show extension Row Animation
            ((Storyboard)Resources[extensionRow_LiveView_ShowRollUp]).Begin();
            ((Storyboard)Resources[extensionRow_LiveView_DecreaseHeight]).Begin();

            // Change Visibility of the content
            variableConfigurationRow.Visibility = Visibility.Hidden;
            liveViewRow.Visibility = Visibility.Visible;

            ((Storyboard)Resources[extensionRow_LiveView_HideData]).Begin();

            BringToFrontAndSendOtherBack(liveViewButtons, liveViewEnabled);
        }

        private void userControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            liveViewPlot.Width = ActualWidth - 50;

            if (!bDeleteRow_Show_Completed)
                deleteRowTransform.X = -ActualWidth;   
        }
        #endregion
        #endregion

        #region Storyboard Events
        #region Input Inidicators and Buttons
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
            //((Storyboard)Resources[recordingDot_ON_Pulse]).Begin();
        }
        #endregion

        #region Variable Configuration
        private void extensionRow_VarConfig_ShowRollUp_Completed(object sender, EventArgs e)
        {
            bExtensionRow_VarConfig_AnimationCompleted = false;

            if (bExtensionRow_VarConfigDelayed_AnimationCompleted)
                bExtensionRow_VarConfigDelayed_AnimationCompleted = false;
        }

        private void extensionRow_VarConfig_ShowBounceDown_Completed(object sender, EventArgs e)
        {
            bExtensionRow_VarConfig_AnimationCompleted = true;
        }

        private void extensionRow_VarConfig_ShowBounceDownDelayed_Completed(object sender, EventArgs e)
        {
            bExtensionRow_VarConfigDelayed_AnimationCompleted = true;
        }

        private void extensionRow_VarConfig_DecreaseHeight_Completed(object sender, EventArgs e)
        {
            bExtensionRow_VarConfig_Completed = false;

            if (bExtensionRow_VarConfigDelayed_Completed)
                bExtensionRow_VarConfigDelayed_Completed = false;
        }

        private void extensionRow_VarConfig_IncreaseHeight_Completed(object sender, EventArgs e)
        {
            bExtensionRow_VarConfig_Completed = true;
        }

        private void extensionRow_VarConfig_IncreaseHeightDelayed_Completed(object sender, EventArgs e)
        {
            bExtensionRow_VarConfigDelayed_Completed = true;
        }
        #endregion

        #region Live View
        private void extensionRow_LiveView_ShowRollUp_Completed(object sender, EventArgs e)
        {
            bExtensionRow_LiveView_AnimationCompleted = false;

            if (bExtensionRow_LiveViewDelayed_AnimationCompleted)
                bExtensionRow_LiveViewDelayed_AnimationCompleted = false;
        }

        private void extensionRow_LiveView_ShowBounceDown_Completed(object sender, EventArgs e)
        {
            bExtensionRow_LiveView_AnimationCompleted = true;
        }

        private void extensionRow_LiveView_ShowBounceDownDelayed_Completed(object sender, EventArgs e)
        {
            bExtensionRow_LiveViewDelayed_AnimationCompleted = true;
        }

        private void extensionRow_LiveView_DecreaseHeight_Completed(object sender, EventArgs e)
        {
            bExtensionRow_LiveView_Completed = false;

            if (bExtensionRow_LiveViewDelayed_Completed)
                bExtensionRow_LiveViewDelayed_Completed = false;
        }

        private void extensionRow_LiveView_IncreaseHeight_Completed(object sender, EventArgs e)
        {
            bExtensionRow_LiveView_Completed = true;
        }

        private void extensionRow_LiveView_IncreaseHeightDelayed_Completed(object sender, EventArgs e)
        {
            bExtensionRow_LiveViewDelayed_Completed = true;
        }






        #endregion

        #region Delete Row
        private void deleteRow_Hide_Completed(object sender, EventArgs e)
        {
            bDeleteRow_Show_Completed = false;
        }

        private void deleteRow_Show_Completed(object sender, EventArgs e)
        {
            bDeleteRow_Show_Completed = true;
        }
        #endregion
        #endregion
    }
}
