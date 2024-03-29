﻿using METS_DiagnosticTool_Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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
        #region Public Fields
        public RpcClient rpcClient;

        public string ADSIp = string.Empty;
        public string ADSPort =string.Empty;
        public string corePath = string.Empty;
        #endregion

        #region Private Fields
        private string lastPLCVariableAddress = string.Empty;

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
        private const string unsavedChanges_Show = "unsavedChanges_Show";
        private const string unsavedChanges_Hide = "unsavedChanges_Hide";
        private const string changeLabelSaveEdit_ShowEdit = "changeLabelSaveEdit_ShowEdit";
        private const string changeLabelSaveEdit_ShowSave = "changeLabelSaveEdit_ShowSave";
        private const string changeLabelSaveEdit_ShowEdit_Instant = "changeLabelSaveEdit_ShowEdit_Instant";
        private const string changeLabelSaveEdit_ShowSave_Instant = "changeLabelSaveEdit_ShowSave_Instant";

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
        private bool bSaved = false;

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

        // Variable Address input
        private static bool variableAddressInputGotFocus = false;

        private LiveViewPlot.LiveViewPlot liveViewPlot;
        #endregion

        #region Events
        public event EventHandler AddNewVariableClicked;
        public event EventHandler<UserInputWithIndicator> DeleteVariableClicked;
        #endregion

        #region Default Constructor
        public UserInputWithIndicator(VariableConfigurationHelper.VariableConfig variableConfig)
        {
            InitializeComponent();

            // Initialize List of Buttons
            configurationButtons.Add(configurationDisabled);
            configurationButtons.Add(configurationEnabled);
            configurationButtons.Add(configurationActive);
            liveViewButtons.Add(liveViewDisabled);
            liveViewButtons.Add(liveViewEnabled);
            liveViewButtons.Add(liveViewActive);
            pollingButtons.Add(pollingON);
            pollingButtons.Add(pollingOFF);
            pollingButtons.Add(pollingDisabled);
            onChangeButtons.Add(onChangeON);
            onChangeButtons.Add(onChangeOFF);
            onChangeButtons.Add(onChangeDisabled);
            recordingButtons.Add(recordingOFF);
            recordingButtons.Add(recordingON);
            recordingButtons.Add(recordingDisabledOFF);
            recordingButtons.Add(recordingDisabledON);

            // Attach Completed Event Handlers to every Storyboard
            // Variable Input
            ((Storyboard)Resources[indicatorOK_Pop]).Completed += new EventHandler(indicatorOK_Pop_Completed);
            ((Storyboard)Resources[indicatorNOK_Shake]).Completed += new EventHandler(indicatorNOK_Shake_Completed);

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

            // Apply read Configuration
            if (variableConfig.variableAddress != null)
            {
                bPollingActive = variableConfig.loggingType == VariableConfigurationHelper.LoggingType.Polling;
                bOnChangeActive = variableConfig.loggingType == VariableConfigurationHelper.LoggingType.OnChange;
                bRecordingActive = variableConfig.recording;
                input.Text = variableConfig.variableAddress;
                refreshTimeInput.Text = variableConfig.pollingRefreshTime.ToString();

                bSaved = true;

                // Change Value of the addNewVariable_Hide Translate Transform X Storyboard
                Storyboard _addNewRowHide = (Storyboard)Resources[addNewVariable_Hide];
                DoubleAnimationUsingKeyFrames _addNewRowHide_Anim = (DoubleAnimationUsingKeyFrames)_addNewRowHide.Children[0];
                _addNewRowHide_Anim.KeyFrames[0].Value = ActualWidth;

                // And Start AddNewRow Storyboard
                ((Storyboard)Resources[addNewVariable_Hide]).Begin();

                // Make Vairable Input Field Disabled
                input.IsEnabled = false;
                variableConfigurationControls.IsEnabled = false;

                // Change label to edit after save
                ((Storyboard)Resources[changeLabelSaveEdit_ShowEdit]).Begin();

                BringToFrontAndSendOtherBack(liveViewButtons, liveViewEnabled);

                if (!bOKPopCompleted)
                {
                    ((Storyboard)Resources[indicatorOK_Pop]).Begin();
                    bOKPopCompleted = true;

                    // Show Configuration Enabled Button
                    BringToFrontAndSendOtherBack(configurationButtons, configurationEnabled);
                }

                if (bOnChangeActive)
                    BringToFrontAndSendOtherBack(onChangeButtons, onChangeON);
                else
                    BringToFrontAndSendOtherBack(onChangeButtons, onChangeOFF);

                if (bPollingActive)
                {
                    refreshTimeInput.IsEnabled = true;
                    BringToFrontAndSendOtherBack(pollingButtons, pollingON);
                }
                else
                {
                    refreshTimeInput.IsEnabled = false;
                    BringToFrontAndSendOtherBack(pollingButtons, pollingOFF);
                }

                if (bRecordingActive)
                {
                    gridRecordingOFF.Visibility = Visibility.Hidden;
                    gridRecordingON.Visibility = Visibility.Visible;
                    BringToFrontAndSendOtherBack(recordingButtons, recordingON);
                }
                else
                {
                    gridRecordingOFF.Visibility = Visibility.Visible;
                    gridRecordingON.Visibility = Visibility.Hidden;
                    BringToFrontAndSendOtherBack(recordingButtons, recordingOFF);
                }
            }
        }
        #endregion

        #region Public Methods
        public void TakeFocusAway()
        {
            // When lost Focus and Input Field has been left empty, then put placeholder Text again
            if (string.IsNullOrEmpty(input.Text) || input.Text == VariableConfigurationHelper.inputPlaceHolderText)
            {
                input.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(defaultGrayColor));
                input.Text = VariableConfigurationHelper.inputPlaceHolderText;
            }

            Keyboard.ClearFocus();
        }

        public async void StartLogging()
        {
            // Send Trigger to Core about PLC Variable that has been saved
            if(!string.IsNullOrEmpty(input.Text) && input.Text != VariableConfigurationHelper.inputPlaceHolderText && bRecordingActive)
            {
                Task<string> _triggerPLCVaribaleConfiguration = RabbitMQHelper.SendToServer_TriggerPLCVarConfig(RabbitMQHelper.RoutingKeys[(int)RabbitMQHelper.RoutingKeysDictionary.triggerPLCVarConfig],
                                                                                                            input.Text,
                                                                                                            true,
                                                                                                            bOnChangeActive ? VariableConfigurationHelper.LoggingType.OnChange : VariableConfigurationHelper.LoggingType.Polling,
                                                                                                            string.IsNullOrEmpty(refreshTimeInput.Text) ? 0 : int.Parse(refreshTimeInput.Text),
                                                                                                            true);
                await _triggerPLCVaribaleConfiguration;
            }
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
                BringToFrontAndSendOtherBack(recordingButtons, recordingDisabledOFF);
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

        private async void labelYES_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // Delete Variable Config from XML FIle
            if(input.Text != VariableConfigurationHelper.inputPlaceHolderText && !string.IsNullOrEmpty(input.Text))
            {
                Task<string> _deletePLCVariableConfig = RabbitMQHelper.SendToServer_DeletePLCVarConfig(RabbitMQHelper.RoutingKeys[(int)RabbitMQHelper.RoutingKeysDictionary.deleteVarConfig],
                                                                                                   string.Concat("XMLFileFullPath$", string.Concat(corePath, @"\XML\VariablesConfiguration.xml"),
                                                                                                                 ";VariableAddress$", input.Text));
                await _deletePLCVariableConfig;

                if (_deletePLCVariableConfig.Result == true.ToString())
                {
                    Storyboard _deleteRow_Hide = (Storyboard)Resources[deleteRow_Hide];
                    DoubleAnimationUsingKeyFrames _deleteRow_Hide_Anim = (DoubleAnimationUsingKeyFrames)_deleteRow_Hide.Children[0];
                    _deleteRow_Hide_Anim.KeyFrames[0].Value = -ActualWidth;

                    ((Storyboard)Resources[deleteRow_Hide]).Begin();

                    variableConfigurationRow.IsEnabled = true;
                    liveViewRow.IsEnabled = true;

                    DeleteVariableClicked?.Invoke(this, (UserInputWithIndicator)userControl);
                }
                else
                {
                    if(!bSaved)
                    {
                        Storyboard _deleteRow_Hide = (Storyboard)Resources[deleteRow_Hide];
                        DoubleAnimationUsingKeyFrames _deleteRow_Hide_Anim = (DoubleAnimationUsingKeyFrames)_deleteRow_Hide.Children[0];
                        _deleteRow_Hide_Anim.KeyFrames[0].Value = -ActualWidth;

                        ((Storyboard)Resources[deleteRow_Hide]).Begin();

                        variableConfigurationRow.IsEnabled = true;
                        liveViewRow.IsEnabled = true;

                        DeleteVariableClicked?.Invoke(this, (UserInputWithIndicator)userControl);
                    }
                }
            }
            else
            {
                Storyboard _deleteRow_Hide = (Storyboard)Resources[deleteRow_Hide];
                DoubleAnimationUsingKeyFrames _deleteRow_Hide_Anim = (DoubleAnimationUsingKeyFrames)_deleteRow_Hide.Children[0];
                _deleteRow_Hide_Anim.KeyFrames[0].Value = -ActualWidth;

                ((Storyboard)Resources[deleteRow_Hide]).Begin();

                variableConfigurationRow.IsEnabled = true;
                liveViewRow.IsEnabled = true;

                DeleteVariableClicked?.Invoke(this, (UserInputWithIndicator)userControl);
            }
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
            if (input.Text == VariableConfigurationHelper.inputPlaceHolderText)
            {
                input.Text = string.Empty;
                input.Foreground = Brushes.White;
            }

            variableAddressInputGotFocus = true;
        }

        private void input_LostFocus(object sender, RoutedEventArgs e)
        {
            // When lost Focus and Input Field has been left empty, then put placeholder Text again
            if (string.IsNullOrEmpty(input.Text) || input.Text == VariableConfigurationHelper.inputPlaceHolderText)
            {
                input.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(defaultGrayColor));
                input.Text = VariableConfigurationHelper.inputPlaceHolderText;
            }

            variableAddressInputGotFocus = false;
        }

        // This event gets fired up when Variable Adress gets data from XML Read
        private async void input_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Check provided PLC Variable address if it can be found among all PLC variables show OK,
            // if it cant be find show NOK
            if (!string.IsNullOrEmpty(input.Text))
            {
                // Check Existance of the PLC Variable
                Task<string> _checkGivenPLCAddress = RabbitMQHelper.SendToServer_CheckPLCVarExistance(RabbitMQHelper.RoutingKeys[(int)RabbitMQHelper.RoutingKeysDictionary.checkPLCVarExistance], input.Text);
                await _checkGivenPLCAddress;

                if (_checkGivenPLCAddress.Result == true.ToString())
                {
                    // First check has the variable been already declared or not
                    Task<string> _checkDoesPLCVarConfigUsed = RabbitMQHelper.SendToServer_CheckDoesPLCVarConfigUsed(RabbitMQHelper.RoutingKeys[(int)RabbitMQHelper.RoutingKeysDictionary.checkDoesPLCVarConfigUsed], input.Text);
                    await _checkDoesPLCVarConfigUsed;

                    if (!bOKPopCompleted && _checkDoesPLCVarConfigUsed.Result != true.ToString())
                    {
                        ((Storyboard)Resources[indicatorOK_Pop]).Begin();
                        bOKPopCompleted = true;

                        // Show Configuration Enabled Button
                        BringToFrontAndSendOtherBack(configurationButtons, configurationEnabled);

                        lastPLCVariableAddress = input.Text;
                    }
                }
                else if (input.Text != VariableConfigurationHelper.inputPlaceHolderText)
                {
                    if (variableAddressInputGotFocus)
                        InputVariableNotFound();

                    if (!string.IsNullOrEmpty(lastPLCVariableAddress))
                    {
                        Task<string> _removeNotSavedButCorrectPLCVariable = RabbitMQHelper.SendToServer_DeletePLCVarConfig(RabbitMQHelper.RoutingKeys[(int)RabbitMQHelper.RoutingKeysDictionary.removeNotSavedButCorrectPLCVariable], lastPLCVariableAddress);
                        await _removeNotSavedButCorrectPLCVariable;
                        lastPLCVariableAddress = string.Empty;
                    }
                }
            }
            else
            {
                if (variableAddressInputGotFocus)
                    InputVariableNotFound();

                if (!string.IsNullOrEmpty(lastPLCVariableAddress))
                {
                    Task<string> _removeNotSavedButCorrectPLCVariable = RabbitMQHelper.SendToServer_DeletePLCVarConfig(RabbitMQHelper.RoutingKeys[(int)RabbitMQHelper.RoutingKeysDictionary.removeNotSavedButCorrectPLCVariable], lastPLCVariableAddress);
                    await _removeNotSavedButCorrectPLCVariable;
                    lastPLCVariableAddress = string.Empty;
                }
            }
        }
        #endregion

        #region Variable Configuration
        private void configuration_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // Dont Request Live View
            List<LiveViewPlot.LiveViewPlot> _livePlotsToBeRemoved = new List<LiveViewPlot.LiveViewPlot>();
            for (int i = 0; i < liveViewRow.Children.Count; i++)
            {
                if (liveViewRow.Children[i].GetType() == typeof(LiveViewPlot.LiveViewPlot))
                    _livePlotsToBeRemoved.Add((LiveViewPlot.LiveViewPlot)liveViewRow.Children[i]);
            }

            // Actuall remove Live Plots from grid
            for (int j = 0; j < _livePlotsToBeRemoved.Count; j++)
            {
                _livePlotsToBeRemoved[j].Dispose();

                liveViewRow.Children.Remove(_livePlotsToBeRemoved[j]);
            }

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
                if ((!bExtensionRow_VarConfig_Completed && !bExtensionRow_VarConfig_AnimationCompleted &&
                    !bExtensionRow_VarConfigDelayed_Completed && !bExtensionRow_VarConfigDelayed_AnimationCompleted) ||
                    (bExtensionRow_VarConfig_Completed && !bExtensionRow_VarConfig_AnimationCompleted &&
                    !bExtensionRow_VarConfigDelayed_Completed && !bExtensionRow_VarConfigDelayed_AnimationCompleted))
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

                    // Change Configuration buttons to inactive
                    if (bPollingActive)
                        BringToFrontAndSendOtherBack(onChangeButtons, onChangeDisabled);
                    else
                        BringToFrontAndSendOtherBack(pollingButtons, pollingDisabled);

                    if (bRecordingActive)
                    {
                        BringToFrontAndSendOtherBack(recordingButtons, recordingDisabledON);

                        gridRecordingOFF.Visibility = Visibility.Hidden;
                        gridRecordingON.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        BringToFrontAndSendOtherBack(recordingButtons, recordingDisabledOFF);

                        gridRecordingOFF.Visibility = Visibility.Visible;
                        gridRecordingON.Visibility = Visibility.Hidden;
                    }

                    if(!bPollingActive && !bOnChangeActive)
                    {
                        BringToFrontAndSendOtherBack(onChangeButtons, onChangeOFF);
                        BringToFrontAndSendOtherBack(pollingButtons, pollingOFF);
                        BringToFrontAndSendOtherBack(recordingButtons, recordingOFF);
                    }
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
            if (bSaved)
            {
                // Hide Extension Variable Configuration Row Animation
                ((Storyboard)Resources[extensionRow_VarConfig_ShowRollUp]).Begin();
                ((Storyboard)Resources[extensionRow_VarConfig_DecreaseHeight]).Begin();

                // Hide Variable Configuration Data
                ((Storyboard)Resources[extensionRow_VarConfig_HideData]).Begin();

                BringToFrontAndSendOtherBack(configurationButtons, configurationEnabled);

                Keyboard.ClearFocus();
            }
            else
            {
                // Warning about unsaved changes show
                Storyboard _unsavedChanges_Show = (Storyboard)Resources[unsavedChanges_Show];
                DoubleAnimationUsingKeyFrames _unsavedChanges_Anim = (DoubleAnimationUsingKeyFrames)_unsavedChanges_Show.Children[0];
                _unsavedChanges_Anim.KeyFrames[0].Value = 0;

                ((Storyboard)Resources[unsavedChanges_Show]).Begin();
            }
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
                    if (!bRecordingActive)
                        BringToFrontAndSendOtherBack(recordingButtons, recordingOFF);
                }

                refreshTimeInput.IsEnabled = true;
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

                if (!bRecordingActive)
                    BringToFrontAndSendOtherBack(recordingButtons, recordingOFF);
            }

            Keyboard.ClearFocus();
        }

        private void refreshTimeInput_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void recordingON_MouseDown(object sender, MouseButtonEventArgs e)
        {
            BringToFrontAndSendOtherBack(recordingButtons, recordingOFF);

            //gridRecordingOFF.Visibility = Visibility.Visible;
            //gridRecordingON.Visibility = Visibility.Hidden;
            //((Storyboard)Resources[recordingDot_ON_Pulse]).Stop();

            bRecordingActive = false;

            Keyboard.ClearFocus();
        }

        private void recordingOFF_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // If there is A configuration given
            if (!string.IsNullOrEmpty(refreshTimeInput.Text) && bPollingActive || bOnChangeActive)
            {
                BringToFrontAndSendOtherBack(recordingButtons, recordingON);

                //gridRecordingOFF.Visibility = Visibility.Hidden;
                //gridRecordingON.Visibility = Visibility.Visible;
                //((Storyboard)Resources[recordingDot_ON_Pulse]).Begin();

                bRecordingActive = true;
            }

            Keyboard.ClearFocus();
        }

        private async void saveConfigurationRectangle_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (bOnChangeActive || (bPollingActive && !string.IsNullOrEmpty(refreshTimeInput.Text)))
            {
                if (!bSaved)
                {
                    // Confirm Variable Configuration
                    Task<string> _saveGivenVariableConfiguration = RabbitMQHelper.SendToServer_SavePLCVarConfigs(RabbitMQHelper.RoutingKeys[(int)RabbitMQHelper.RoutingKeysDictionary.plcVarConfigsSave],
                                                                                                                string.Concat(corePath, @"\XML\VariablesConfiguration.xml"),
                                                                                                                input.Text,
                                                                                                                bOnChangeActive ? VariableConfigurationHelper.LoggingType.OnChange : VariableConfigurationHelper.LoggingType.Polling,
                                                                                                                string.IsNullOrEmpty(refreshTimeInput.Text) ? 0 : int.Parse(refreshTimeInput.Text),
                                                                                                                bRecordingActive);
                    await _saveGivenVariableConfiguration;

                    // Hide Extension Variable Configuration Row Animation
                    ((Storyboard)Resources[extensionRow_VarConfig_ShowRollUp]).Begin();
                    ((Storyboard)Resources[extensionRow_VarConfig_DecreaseHeight]).Begin();

                    // Hide Variable Configuration Data
                    ((Storyboard)Resources[extensionRow_VarConfig_HideData]).Begin();

                    BringToFrontAndSendOtherBack(configurationButtons, configurationEnabled);

                    // Make Vairable Input Field Disabled
                    input.IsEnabled = false;
                    variableConfigurationControls.IsEnabled = false;

                    // Change label to edit after save
                    ((Storyboard)Resources[changeLabelSaveEdit_ShowEdit_Instant]).Begin();

                    BringToFrontAndSendOtherBack(liveViewButtons, liveViewEnabled);

                    // Change Configuration buttons to inactive
                    if (bPollingActive)
                        BringToFrontAndSendOtherBack(onChangeButtons, onChangeDisabled);
                    else
                        BringToFrontAndSendOtherBack(pollingButtons, pollingDisabled);

                    if (bRecordingActive)
                    {
                        BringToFrontAndSendOtherBack(recordingButtons, recordingDisabledON);

                        gridRecordingOFF.Visibility = Visibility.Hidden;
                        gridRecordingON.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        BringToFrontAndSendOtherBack(recordingButtons, recordingDisabledOFF);

                        gridRecordingOFF.Visibility = Visibility.Visible;
                        gridRecordingON.Visibility = Visibility.Hidden;
                    }

                    bSaved = true;

                    Keyboard.ClearFocus();

                    // Send Trigger to Core about PLC Variable that has been saved
                    Task<string> _triggerPLCVaribaleConfiguration = RabbitMQHelper.SendToServer_TriggerPLCVarConfig(RabbitMQHelper.RoutingKeys[(int)RabbitMQHelper.RoutingKeysDictionary.triggerPLCVarConfig],
                                                                                                                    input.Text,
                                                                                                                    bSaved,
                                                                                                                    bOnChangeActive ? VariableConfigurationHelper.LoggingType.OnChange : VariableConfigurationHelper.LoggingType.Polling,
                                                                                                                    string.IsNullOrEmpty(refreshTimeInput.Text) ? 0 : int.Parse(refreshTimeInput.Text),
                                                                                                                    bRecordingActive);
                    await _triggerPLCVaribaleConfiguration;
                }
                else
                {
                    variableConfigurationControls.IsEnabled = true;

                    // Change label to edit after save
                    ((Storyboard)Resources[changeLabelSaveEdit_ShowSave_Instant]).Begin();

                    BringToFrontAndSendOtherBack(liveViewButtons, liveViewDisabled);

                    // Change Configuration buttons to inactive
                    if (bPollingActive)
                    {
                        BringToFrontAndSendOtherBack(onChangeButtons, onChangeOFF);

                        pollingConfiguration.IsEnabled = true;
                    }
                    else
                        BringToFrontAndSendOtherBack(pollingButtons, pollingOFF);

                    if (bRecordingActive)
                    {
                        BringToFrontAndSendOtherBack(recordingButtons, recordingON);

                        gridRecordingOFF.Visibility = Visibility.Hidden;
                        gridRecordingON.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        BringToFrontAndSendOtherBack(recordingButtons, recordingOFF);

                        gridRecordingOFF.Visibility = Visibility.Visible;
                        gridRecordingON.Visibility = Visibility.Hidden;
                    }

                    bSaved = false;

                    Keyboard.ClearFocus();
                }
            }
            
            if(!bPollingActive && !bOnChangeActive)
            {
                // Show information that some condfiguration has to be done
                WarningText.Content = "Empty Configuration!";
                Storyboard _unsavedChanges_Show = (Storyboard)Resources[unsavedChanges_Show];
                DoubleAnimationUsingKeyFrames _unsavedChanges_Anim = (DoubleAnimationUsingKeyFrames)_unsavedChanges_Show.Children[0];
                _unsavedChanges_Anim.KeyFrames[0].Value = 0;

                ((Storyboard)Resources[unsavedChanges_Show]).Begin();
            }
        }

        private void variableConfigurationRow_SaveChangesWarning_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // Warning about unsaved changes show
            Storyboard _unsavedChanges_Hide = (Storyboard)Resources[unsavedChanges_Hide];
            DoubleAnimationUsingKeyFrames _unsavedChanges_Anim = (DoubleAnimationUsingKeyFrames)_unsavedChanges_Hide.Children[0];
            _unsavedChanges_Anim.KeyFrames[0].Value = -ActualWidth;

            ((Storyboard)Resources[unsavedChanges_Hide]).Begin();
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

                // Request Live View
                // Initialize Live View Plot
                liveViewPlot = new LiveViewPlot.LiveViewPlot();
                liveViewPlot.Name = "liveViewPlot";
                liveViewPlot.Width = 740;
                liveViewPlot.HorizontalAlignment = HorizontalAlignment.Center;
                liveViewPlot.VerticalAlignment = VerticalAlignment.Stretch;
                liveViewPlot.Margin = new Thickness(10);
                Grid.SetRow(liveViewPlot, 1);
                liveViewRow.Children.Add(liveViewPlot);

                // Inject RPC Client
                if (liveViewPlot.rpcClient == null)
                    liveViewPlot.rpcClient = rpcClient;
                var variableConfig = new VariableConfigurationHelper.VariableConfig();
                variableConfig.variableAddress = input.Text;
                variableConfig.recording = bRecordingActive;
                variableConfig.loggingType = bOnChangeActive ? VariableConfigurationHelper.LoggingType.OnChange : VariableConfigurationHelper.LoggingType.Polling;
                variableConfig.pollingRefreshTime = string.IsNullOrEmpty(refreshTimeInput.Text) ? 0 : int.Parse(refreshTimeInput.Text);
                rpcClient.LiveViewRequested(ADSIp, ADSPort, true, variableConfig);
            }
            else
            {
                if (((!bExtensionRow_LiveView_Completed && !bExtensionRow_LiveView_AnimationCompleted &&
                    !bExtensionRow_LiveViewDelayed_Completed && !bExtensionRow_LiveViewDelayed_AnimationCompleted)) || bSaved)
                {
                    // Show extension Row Animation
                    ((Storyboard)Resources[extensionRow_LiveView_IncreaseHeight]).Begin();
                    ((Storyboard)Resources[extensionRow_LiveView_ShowBounceDown]).Begin();

                    // Change Visibility of the content
                    variableConfigurationRow.Visibility = Visibility.Hidden;
                    liveViewRow.Visibility = Visibility.Visible;

                    ((Storyboard)Resources[extensionRow_LiveView_ShowData]).Begin();

                    BringToFrontAndSendOtherBack(liveViewButtons, liveViewActive);

                    // Request Live View
                    // Initialize Live View Plot
                    liveViewPlot = new LiveViewPlot.LiveViewPlot();
                    liveViewPlot.Name = "liveViewPlot";
                    liveViewPlot.Width = 740;
                    liveViewPlot.HorizontalAlignment = HorizontalAlignment.Center;
                    liveViewPlot.VerticalAlignment = VerticalAlignment.Stretch;
                    liveViewPlot.Margin = new Thickness(10);
                    Grid.SetRow(liveViewPlot, 1);
                    liveViewRow.Children.Add(liveViewPlot);

                    // Inject RPC Client
                    if (liveViewPlot.rpcClient == null)
                        liveViewPlot.rpcClient = rpcClient;
                    var variableConfig = new VariableConfigurationHelper.VariableConfig();
                    variableConfig.variableAddress = input.Text;
                    variableConfig.recording = bRecordingActive;
                    variableConfig.loggingType = bOnChangeActive ? VariableConfigurationHelper.LoggingType.OnChange : VariableConfigurationHelper.LoggingType.Polling;
                    variableConfig.pollingRefreshTime = string.IsNullOrEmpty(refreshTimeInput.Text) ? 0 : int.Parse(refreshTimeInput.Text);
                    rpcClient.LiveViewRequested(ADSIp, ADSPort, true, variableConfig);
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

                    // Dont Request Live View
                    List<LiveViewPlot.LiveViewPlot> _livePlotsToBeRemoved = new List<LiveViewPlot.LiveViewPlot>();
                    for (int i = 0; i < liveViewRow.Children.Count; i++)
                    {
                        if (liveViewRow.Children[i].GetType() == typeof(LiveViewPlot.LiveViewPlot))
                            _livePlotsToBeRemoved.Add((LiveViewPlot.LiveViewPlot)liveViewRow.Children[i]);
                    }

                    // Actuall remove Live Plots from grid
                    for (int j = 0; j < _livePlotsToBeRemoved.Count; j++)
                    {
                        _livePlotsToBeRemoved[j].Dispose();

                        liveViewRow.Children.Remove(_livePlotsToBeRemoved[j]);
                    }
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

            // Dont Request Live View
            List<LiveViewPlot.LiveViewPlot> _livePlotsToBeRemoved = new List<LiveViewPlot.LiveViewPlot>();
            for (int i = 0; i < liveViewRow.Children.Count; i++)
            {
                if (liveViewRow.Children[i].GetType() == typeof(LiveViewPlot.LiveViewPlot))
                    _livePlotsToBeRemoved.Add((LiveViewPlot.LiveViewPlot)liveViewRow.Children[i]);
            }

            // Actuall remove Live Plots from grid
            for (int j = 0; j < _livePlotsToBeRemoved.Count; j++)
            {
                _livePlotsToBeRemoved[j].Dispose();
                
                liveViewRow.Children.Remove(_livePlotsToBeRemoved[j]);
            }
        }

        private void userControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (liveViewPlot != null)
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
