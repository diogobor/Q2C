using Q2C.Control;
using Q2C.Control.Database;
using Q2C.Model;
using Q2C.Viewer.Machine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ThermoFisher.CommonCore.Data;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using UserControl = System.Windows.Controls.UserControl;

namespace Q2C.Viewer.Setup
{
    /// <summary>
    /// Interaction logic for UCMachine.xaml
    /// </summary>
    public partial class UCMachine : UserControl
    {
        private System.Windows.Window _window;
        private Q2C.Model.Machine _machine;

        public UCMachine()
        {
            InitializeComponent();
        }

        public void Load(System.Windows.Window window, Q2C.Model.Machine machine)
        {
            _window = window;
            _machine = machine;

            TextMachineName.Focus();

            if (_machine != null)
                LoadMachine();
        }

        private void LoadMachine()
        {
            TextMachineName.Text = _machine.Name; TextMachineName.IsReadOnly = true;
            HasFAIMS.IsChecked = _machine.HasFAIMS;
            HasOT.IsChecked = _machine.HasOT;
            HasIT.IsChecked = _machine.HasIT;
            HasEvaluation.IsChecked = _machine.HasEvaluation;
            UpDownIntervalTime.Value = _machine.IntervalTime;

            string[] colsCalibration = Regex.Split(_machine.CalibrationTime, "_");
            UpDownCalibrationTime.Value = Convert.ToInt32(colsCalibration[0]);
            ComboCalibrationTime.SelectedIndex = SelectComboboxItem(colsCalibration[1]);

            string[] colsFullCalibration = Regex.Split(_machine.FullCalibrationTime, "_");
            UpDownFullCalibrationTime.Value = Convert.ToInt32(colsFullCalibration[0]);
            ComboFullCalibrationTime.SelectedIndex = SelectComboboxItem(colsFullCalibration[1]);

        }

        private int SelectComboboxItem(string selected_item)
        {
            switch (selected_item)
            {
                case "Day(s)":
                    return 0;
                case "Week(s)":
                    return 1;
                case "Month(s)":
                    return 2;
                case "Year(s)":
                    return 3;
            }
            return 0;
        }

        private async void ButtonConfirm_Click(object sender, RoutedEventArgs e)
        {
            if (_window == null) return;

            if (CheckFields()) return;

            UCWaitScreen waitScreen = new UCWaitScreen("Please Wait...", "Adding/editing Machine...");
            Grid.SetRow(waitScreen, 0);
            Grid.SetRowSpan(waitScreen, 2);
            waitScreen.Margin = new Thickness(0, 0, 0, -8);
            MainUCMachine.Children.Add(waitScreen);

            bool isValid = false;
            if (_machine == null)
                _machine = GetMachine();
            else
            {
                var new_machine = GetMachine();
                _machine.HasFAIMS = new_machine.HasFAIMS;
                _machine.HasOT = new_machine.HasOT;
                _machine.HasIT = new_machine.HasIT;
                _machine.HasEvaluation = new_machine.HasEvaluation;
                _machine.CalibrationTime = new_machine.CalibrationTime;
                _machine.FullCalibrationTime = new_machine.FullCalibrationTime;
                _machine.IntervalTime = new_machine.IntervalTime;
            }

            await Task.Run(() => isValid = CreateOrUpdateMachine());

            if (isValid)
            {
                await Task.Run(() => isValid = UpdateData());

                if (isValid)
                {
                    if (_window.Title.Contains("Add"))
                        System.Windows.MessageBox.Show(
                                    "Machine has been added successfully!",
                                    "Q2C :: Information",
                                    (System.Windows.MessageBoxButton)MessageBoxButtons.OK,
                                    (System.Windows.MessageBoxImage)MessageBoxIcon.Information);
                    else
                        System.Windows.MessageBox.Show(
                                    "Machine has been updated successfully!",
                                    "Q2C :: Information",
                                    (System.Windows.MessageBoxButton)MessageBoxButtons.OK,
                                    (System.Windows.MessageBoxImage)MessageBoxIcon.Information);

                    _window.Close();
                }
                else
                    System.Windows.MessageBox.Show(
                               "Failed to add/modify machine",
                               "Q2C :: Warning",
                               (System.Windows.MessageBoxButton)MessageBoxButtons.OK,
                               (System.Windows.MessageBoxImage)MessageBoxIcon.Warning);
            }
            else
                System.Windows.MessageBox.Show(
                               "Failed to add/modify machine",
                               "Q2C :: Warning",
                               (System.Windows.MessageBoxButton)MessageBoxButtons.OK,
                               (System.Windows.MessageBoxImage)MessageBoxIcon.Warning);

            MainUCMachine.Children.Remove(waitScreen);
        }

        private bool UpdateData()
        {
            try
            {
                Connection.ReadInfo(true);
                ((WindowMachine)_window).UpdateMachinesDataGrid();
                return true;
            }
            catch (Exception)
            {
                return false;
            }

        }

        private bool CheckFields()
        {
            if (String.IsNullOrEmpty(TextMachineName.Text))
            {
                System.Windows.MessageBox.Show(
                            "'Username' field is empty!",
                            "Q2C :: Warning",
                            (System.Windows.MessageBoxButton)MessageBoxButtons.OK,
                            (System.Windows.MessageBoxImage)MessageBoxIcon.Warning);

                TextMachineName.Focus();
                return true;
            }

            return false;
        }

        private Q2C.Model.Machine GetMachine()
        {
            DateTime registrationDate = DateTime.Now;
            string taskDate_str = registrationDate.ToString("dd") + "/" + registrationDate.ToString("MM") + "/" + registrationDate.ToString("yyyy") + " " + registrationDate.ToString("HH:mm:ss");

            string machineName = TextMachineName.Text;
            bool hasFaims = HasFAIMS.IsChecked == true;
            bool hasOT = HasOT.IsChecked == true;
            bool hasIT = HasIT.IsChecked == true;
            bool hasEvaluation = HasEvaluation.IsChecked == true;
            string calibrationTime = "" + UpDownCalibrationTime.Value + "_" + ComboCalibrationTime.SelectionBoxItem.ToString();
            string fullCalibrationTime = "" + UpDownFullCalibrationTime.Value + "_" + ComboFullCalibrationTime.SelectionBoxItem.ToString();
            int intervalTime = Convert.ToInt32(UpDownIntervalTime.Value);

            return new Q2C.Model.Machine(
                -1,
                taskDate_str,
                machineName,
                hasEvaluation,
                hasFaims,
                hasOT,
                hasIT,
                calibrationTime,
                fullCalibrationTime,
                Management.InfoStatus.Active,
                intervalTime);
        }

        private bool CreateOrUpdateMachine()
        {
            return Connection.AddOrUpdateMachine(_machine);
        }

        private void ButtonCancel_Click(object sender, RoutedEventArgs e)
        {
            if (_window != null)
                _window.Close();
        }

        private void TextMachineName_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.OemMinus ||
                e.Key == Key.Space)
                e.Handled = true;
        }
    }
}
