using Q2C.Control;
using Q2C.Control.Database;
using Q2C.Control.FileManagement;
using Q2C.Model;
using Q2C.Viewer.Setup;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.TextFormatting;
using System.Windows.Navigation;
using System.Windows.Shapes;
using static System.Net.Mime.MediaTypeNames;

namespace Q2C.Viewer.Machine
{
    /// <summary>
    /// Interaction logic for UCLog.xaml
    /// </summary>
    public partial class UCLog : System.Windows.Controls.UserControl
    {
        private Window _window { get; set; }
        private MachineLog _log { get; set; }
        private string _machine { get; set; }
        public UCLog()
        {
            InitializeComponent();
        }

        public void Load(Window window, MachineLog log, string machine)
        {
            _window = window;
            _log = log;
            _machine = machine;

            if (_log != null)
                LoadLog();
        }

        private void LoadLog()
        {
            TextOperator.Text = _log.Operator; TextOperator.IsReadOnly = true;
            TextRemarks.Text = _log.Remarks;
            TextColumnLotNumber.Text = _log.ColumnLotNumber;
            TextLC.Text = _log.LC;
            CheckBoxCalibrate.IsChecked = _log.IsCalibrated;
            CheckBoxFullCalibrate.IsChecked = _log.IsFullyCalibrated;
            TextFile.Text = _log._TechnicalReportFileName;
            ButtonReportFile.IsEnabled = false;
        }

        private bool CheckFields()
        {
            if (String.IsNullOrEmpty(TextOperator.Text))
            {
                System.Windows.MessageBox.Show(
                            "'Operator' field is empty!",
                            "Q2C :: Warning",
                            (System.Windows.MessageBoxButton)MessageBoxButtons.OK,
                            (System.Windows.MessageBoxImage)MessageBoxIcon.Warning);

                TextOperator.Focus();
                return true;
            }

            if (String.IsNullOrEmpty(TextRemarks.Text))
            {
                System.Windows.MessageBox.Show(
                            "'Remarks' field is empty!",
                            "Q2C :: Warning",
                            (System.Windows.MessageBoxButton)MessageBoxButtons.OK,
                            (System.Windows.MessageBoxImage)MessageBoxIcon.Warning);

                TextRemarks.Focus();
                return true;
            }

            return false;
        }

        private async void ButtonConfirm_Click(object sender, RoutedEventArgs e)
        {
            if (CheckFields()) return;

            UCWaitScreen waitScreen = new UCWaitScreen("Please Wait...", "Adding/editing log...");
            Grid.SetRow(waitScreen, 0);
            Grid.SetRowSpan(waitScreen, 2);
            waitScreen.Margin = new Thickness(0, 0, 0, -8);
            MainUCRun.Children.Add(waitScreen);

            bool isValid = false;
            if (_log == null)
                _log = GetLog();
            else
            {
                var new_log = GetLog();
                _log.Remarks = new_log.Remarks;
                _log.ColumnLotNumber = new_log.ColumnLotNumber;
                _log.LC = new_log.LC;
                _log._isCalibrated = new_log.IsCalibrated.ToString();
                _log._isFullyCalibrated = new_log.IsFullyCalibrated.ToString();
            }

            await Task.Run(() => isValid = CreateOrUpdateLog());

            if (isValid)
            {
                if (_window.Title.Contains("Add"))
                {
                    System.Windows.MessageBox.Show(
                                "Log has been added successfully!",
                                "Q2C :: Information",
                                (System.Windows.MessageBoxButton)MessageBoxButtons.OK,
                                (System.Windows.MessageBoxImage)MessageBoxIcon.Information);
                }
                else
                    System.Windows.MessageBox.Show(
                                "Log has been updated successfully!",
                                "Q2C :: Information",
                                (System.Windows.MessageBoxButton)MessageBoxButtons.OK,
                                (System.Windows.MessageBoxImage)MessageBoxIcon.Information);

                Connection.ReadInfo();
                ((WindowLog)_window).UpdateLogDataGrid();
                _window.Close();
            }
            else
                System.Windows.MessageBox.Show(
                               "Failed to add/modify log",
                               "Q2C :: Warning",
                               (System.Windows.MessageBoxButton)MessageBoxButtons.OK,
                               (System.Windows.MessageBoxImage)MessageBoxIcon.Warning);

            MainUCRun.Children.Remove(waitScreen);
        }

        private bool CreateOrUpdateLog()
        {
            return Connection.AddOrUpdateMachineLog(_log, _machine);
        }

        private MachineLog GetLog()
        {
            DateTime registrationDate = DateTime.Now;
            string taskDate_str = registrationDate.ToString("dd") + "/" + registrationDate.ToString("MM") + "/" + registrationDate.ToString("yyyy") + " " + registrationDate.ToString("HH:mm:ss");

            string @operator = TextOperator.Text;
            string remarks = TextRemarks.Text;
            string columnLotNumber = TextColumnLotNumber.Text;
            string lc = TextLC.Text;
            bool isCalibrated = CheckBoxCalibrate.IsChecked == true;
            bool isFullCalibrated = CheckBoxFullCalibrate.IsChecked == true;
            string technical_report_file = TextFile.Text;

            return new MachineLog(-1,
                taskDate_str,
                @operator,
                remarks,
                columnLotNumber,
                lc,
                isCalibrated.ToString(),
                isFullCalibrated.ToString(),
                technical_report_file,
                Management.GetComputerUser(),
                Management.InfoStatus.Active);
        }

        private void ButtonCancel_Click(object sender, RoutedEventArgs e)
        {
            if (_window != null)
                _window.Close();
        }

        private void CheckBoxCalibrate_Checked(object sender, RoutedEventArgs e)
        {
            System.Windows.Controls.CheckBox currentCheckBox = (System.Windows.Controls.CheckBox)sender;
            if (currentCheckBox.Name.Equals("CheckBoxCalibrate"))
            {
                if (CheckBoxCalibrate.IsChecked == true)
                {
                    if (CheckBoxFullCalibrate.IsChecked == true)
                        CheckBoxFullCalibrate.IsChecked = false;
                }
            }
            else
            {
                if (CheckBoxFullCalibrate.IsChecked == true)
                {
                    if (CheckBoxCalibrate.IsChecked == true)
                        CheckBoxCalibrate.IsChecked = false;
                }
            }
        }

        private void ButtonReport_Click(object sender, RoutedEventArgs e)
        {
            var ofd = new OpenFileDialog();
            ofd.Filter = "Technical report (*.pdf)|*.pdf";
            ofd.Multiselect = false;

            // Show open file dialog box
            var result = ofd.ShowDialog();

            if (result == DialogResult.OK)
            {
                TextFile.Text = ofd.FileName;
            }
            else
            {
                TextFile.Text = "";
                return;
            }
        }
    }
}
