using CommunityToolkit.Mvvm.ComponentModel;
using Q2C.Control;
using Q2C.Control.Database;
using Q2C.Model;
using Q2C.Properties;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using ComboBoxItem = Q2C.Model.ComboBoxItem;

namespace Q2C.Viewer.Project
{
    /// <summary>
    /// Interaction logic for UCSample.xaml
    /// </summary>
    public partial class UCProject : System.Windows.Controls.UserControl
    {
        private System.Windows.Window _window;
        private Q2C.Model.Project _project;
        private List<string> _methods;
        private double OriginalWindowHeight;
        private double OriginalUCHeight;

        public ObservableCollection<ComboBoxItem> Machines { get; set; }
        public ObservableCollection<ComboBoxItem> FAIMS { get; set; }

        public UCProject()
        {
            InitializeComponent();
            UpdateMethods();
            FillComboMachines();
        }

        private void FillComboMachines()
        {
            Machines = new ObservableCollection<ComboBoxItem>();
            foreach (var machine in Management.GetMachines())
                Machines.Add(new ComboBoxItem(machine.Name));

            ComboMachines.ItemsSource = Machines;
            ComboMachines.Text = "Select at least one machine";
        }

        public void Load(System.Windows.Window window, Q2C.Model.Project project)
        {
            _window = window;
            _project = project;

            OriginalWindowHeight = _window.Height;
            OriginalUCHeight = this.Height;

            TextUserInitials.Focus();

            if (!Connection.IsOnline)
            {
                ReceiveNotification.IsEnabled = false;
                ReceiveNotification.IsChecked = false;
            }
            if (_project != null)
                LoadProject();
        }

        private void LoadProject()
        {
            TextUserInitials.Text = _project.ProjectName;
            TextAmountMSTime.Text = _project.AmountMS;
            TextNumberOfSamples.Text = _project.NumberOfSamples;
            TextComments.Text = _project.Comments;
            if (!Connection.IsOnline)
            {
                ReceiveNotification.IsEnabled = false;
                ReceiveNotification.IsChecked = false;
            }
            else
                ReceiveNotification.IsChecked = _project.ReceiveNotification;

            int method_index = -1;
            method_index = _methods.FindIndex(a => a.Equals(_project.Method));
            ComboMethod.SelectedIndex = method_index;

            int status_index = -1;
            if (_project.Status == ProjectStatus.WaitForAcquisition)
                status_index = 0;
            else if (_project.Status == ProjectStatus.Measured)
                status_index = 2;
            else
                status_index = 1;
            ComboStatus.SelectedIndex = status_index;

            foreach (string machine in _project.GetMachines)
            {
                var currentMachine = Machines.Where(a => a.Name.Equals(machine)).FirstOrDefault();
                if (currentMachine != null)
                    currentMachine.IsChecked = true;
            }
            ComboMachines.Text = GetSelectedMachines();
            CreateFAIMS(Machines.Where(a => a.IsChecked).Select(b => b.Name).ToList(), true);
        }

        private void ButtonConfirm_Click(object sender, RoutedEventArgs e)
        {
            if (_window == null) return;

            if (CreateOrUpdateProject())
            {
                Connection.ReadInfo();
                ((WindowProject)_window).UpdateProjectDataGrid();
                _window.Close();
            }
        }

        private bool CheckFields()
        {
            if (String.IsNullOrEmpty(TextUserInitials.Text))
            {
                System.Windows.MessageBox.Show(
                            "'Project Name' field is empty!",
                            "Q2C :: Warning",
                            (System.Windows.MessageBoxButton)MessageBoxButtons.OK,
                            (System.Windows.MessageBoxImage)MessageBoxIcon.Warning);

                TextUserInitials.Focus();
                return true;
            }

            if (ComboMachines.Text.StartsWith("Select at least"))
            {
                System.Windows.MessageBox.Show(
                            "No machine has been selected!",
                            "Q2C :: Warning",
                            (System.Windows.MessageBoxButton)MessageBoxButtons.OK,
                            (System.Windows.MessageBoxImage)MessageBoxIcon.Warning);

                ComboMachines.Focus();
                return true;
            }

            if (String.IsNullOrEmpty(TextAmountMSTime.Text))
            {
                System.Windows.MessageBox.Show(
                            "'Amount of MS time (hours)' field is empty!",
                            "Q2C :: Warning",
                            (System.Windows.MessageBoxButton)MessageBoxButtons.OK,
                            (System.Windows.MessageBoxImage)MessageBoxIcon.Warning);

                TextAmountMSTime.Focus();
                return true;
            }
            //else
            //{
            //    int numberOfMachines = Machines.Where(a => a.IsChecked).Count();
            //    int numberOfSlashes = TextAmountMSTime.Text.Select(a => a).Count(b => b == '/');
            //    if ((numberOfMachines - 1) != numberOfSlashes)
            //    {
            //        System.Windows.MessageBox.Show(
            //                "Number of values is different from the number of selected machines!",
            //                "Q2C :: Warning",
            //                (System.Windows.MessageBoxButton)MessageBoxButtons.OK,
            //                (System.Windows.MessageBoxImage)MessageBoxIcon.Warning);

            //        TextAmountMSTime.Focus();
            //        return true;
            //    }
            //}

            if (String.IsNullOrEmpty(TextNumberOfSamples.Text))
            {
                System.Windows.MessageBox.Show(
                            "'Number of Samples' field is empty!",
                            "Q2C :: Warning",
                            (System.Windows.MessageBoxButton)MessageBoxButtons.OK,
                            (System.Windows.MessageBoxImage)MessageBoxIcon.Warning);

                TextNumberOfSamples.Focus();
                return true;
            }
            //else
            //{
            //    int numberOfMachines = Machines.Where(a => a.IsChecked).Count();
            //    int numberOfSlashes = TextNumberOfSamples.Text.Select(a => a).Count(b => b == '/');
            //    if ((numberOfMachines - 1) != numberOfSlashes)
            //    {
            //        System.Windows.MessageBox.Show(
            //                "Number of samples is different from the number of selected machines!",
            //                "Q2C :: Warning",
            //                (System.Windows.MessageBoxButton)MessageBoxButtons.OK,
            //                (System.Windows.MessageBoxImage)MessageBoxIcon.Warning);

            //        TextNumberOfSamples.Focus();
            //        return true;
            //    }
            //}

            int method_index = ComboMethod.SelectedIndex;
            if (method_index == -1)
            {
                System.Windows.MessageBox.Show(
                            "No method has been selected!",
                            "Q2C :: Warning",
                            (System.Windows.MessageBoxButton)MessageBoxButtons.OK,
                            (System.Windows.MessageBoxImage)MessageBoxIcon.Warning);

                ComboMethod.Focus();
                return true;
            }


            return false;
        }

        private bool CreateOrUpdateProject()
        {
            if (CheckFields()) return false;

            Q2C.Model.Project project = GetProject();
            //0: sucess; 1: new project must has 'wait for acquisition' as status; 2: project has not found in the machine queue; 3: project has found in the machine queue, delete it from there; 4: User not authorized to include data; 5: failed
            byte operation_status = Connection.AddOrUpdateProject(project);

            if (operation_status == 0)
            {
                if (_window.Title.Contains("Add"))
                    System.Windows.MessageBox.Show(
                                "Project has been added successfully!",
                                "Q2C :: Information",
                                (System.Windows.MessageBoxButton)MessageBoxButtons.OK,
                                (System.Windows.MessageBoxImage)MessageBoxIcon.Information);
                else
                    System.Windows.MessageBox.Show(
                                "Project has been updated successfully!",
                                "Q2C :: Information",
                                (System.Windows.MessageBoxButton)MessageBoxButtons.OK,
                                (System.Windows.MessageBoxImage)MessageBoxIcon.Information);
            }
            else if (operation_status == 1)
            {
                System.Windows.MessageBox.Show(
                               "New projects must have 'Wait for acquisition' as status!",
                               "Q2C :: Warning",
                               (System.Windows.MessageBoxButton)MessageBoxButtons.OK,
                               (System.Windows.MessageBoxImage)MessageBoxIcon.Warning);
                return false;
            }
            else if (operation_status == 2)
            {
                System.Windows.MessageBox.Show(
                               "Project has not found in the machine queue!\nContact the administrator to put it in the respective queue.",
                               "Q2C :: Warning",
                               (System.Windows.MessageBoxButton)MessageBoxButtons.OK,
                               (System.Windows.MessageBoxImage)MessageBoxIcon.Warning);
                return false;
            }
            else if (operation_status == 3)
            {
                System.Windows.MessageBox.Show(
                               "Project has found in the machine queue. Delete it from there.",
                               "Q2C :: Warning",
                               (System.Windows.MessageBoxButton)MessageBoxButtons.OK,
                               (System.Windows.MessageBoxImage)MessageBoxIcon.Warning);
                return false;
            }
            else if (operation_status == 4)
            {
                System.Windows.MessageBox.Show(
                               "User not authorized to include/change data. Please use a valid account.",
                               "Q2C :: Warning",
                               (System.Windows.MessageBoxButton)MessageBoxButtons.OK,
                               (System.Windows.MessageBoxImage)MessageBoxIcon.Warning);
                return false;
            }

            return true;
        }

        private Q2C.Model.Project GetProject()
        {
            DateTime registrationDate = DateTime.Now;
            string taskDate_str = registrationDate.ToString("dd") + "/" + registrationDate.ToString("MM") + "/" + registrationDate.ToString("yyyy") + " " + registrationDate.ToString("HH:mm:ss");

            #region get selected item from comboboxes
            int method_index = ComboMethod.SelectedIndex;
            string method = _methods[method_index];

            System.Windows.Controls.ComboBoxItem _selectedStatus = (System.Windows.Controls.ComboBoxItem)ComboStatus.SelectedItem;
            string status = _selectedStatus.Content.ToString();

            string machine = GetSelectedMachines();

            string faims = GetFaimsInfo();
            #endregion

            bool? receive_notification = ReceiveNotification.IsChecked;
            if (!Connection.IsOnline)
                receive_notification = false;

            return new Q2C.Model.Project(
                _project != null ? _project.Id : -1,
                taskDate_str,
                TextUserInitials.Text,
                TextAmountMSTime.Text,
                TextNumberOfSamples.Text,
                method,
                machine,
                Q2C.Model.Project.GetFAIMS(faims),
                receive_notification.ToString(),
                TextComments.Text,
                Management.GetComputerUser(),
                Q2C.Model.Project.GetStatus(status),
                Management.InfoStatus.Active);
        }
        private string GetFaimsInfo()
        {
            var machines = Machines.Where(a => a.IsChecked).Select(b => b.Name).ToList();
            if (machines.Count == 0) return "";

            StringBuilder sb_faims = new();
            foreach (var machine in machines)
            {
                if (MachinesFAIMS.Children[0] is Grid innerGrid)
                {
                    foreach (UIElement child in innerGrid.Children)
                    {
                        if (child is System.Windows.Controls.ComboBox combobox)
                        {
                            string name = combobox.Name.Replace("cbFaims_", "");
                            if (name.Equals(machine))
                            {
                                int selectedIndex = combobox.SelectedIndex;

                                bool hasFaims = Management.GetMachines().Where(a => a.Name.Equals(machine)).FirstOrDefault().HasFAIMS;

                                if (hasFaims)
                                {
                                    if (selectedIndex == 0)
                                        sb_faims.Append("Yes/");
                                    else if (selectedIndex == 1)
                                        sb_faims.Append("No/");
                                    else
                                        sb_faims.Append("I don't mind/");
                                }
                                else
                                    sb_faims.Append("No/");
                                break;
                            }
                        }
                    }
                }
            }

            return sb_faims.ToString().Substring(0, sb_faims.ToString().Length - 1);

        }

        private void ButtonCancel_Click(object sender, RoutedEventArgs e)
        {
            if (_window != null)
                _window.Close();
        }

        private void CheckBoxMachine_Click(object sender, RoutedEventArgs e)
        {
            ComboMachines.Text = GetSelectedMachines();
            CreateFAIMS(Machines.Where(a => a.IsChecked).Select(b => b.Name).ToList());
        }

        private void CreateFAIMS(List<string> machines, bool loadInfo = false)
        {
            MachinesFAIMS.Children.Clear();

            if (machines.Count == 0) return;

            this.Height = OriginalUCHeight;
            _window.Height = OriginalWindowHeight;

            Grid newGrid = new Grid();
            ColumnDefinition column1 = new();
            ColumnDefinition column2 = new();

            newGrid.ColumnDefinitions.Add(column1);
            newGrid.ColumnDefinitions.Add(column2);

            for (int i = 0; i < machines.Count; i++)
            {
                string item = machines[i];
                var current_machine = Management.GetMachines().Where(a => a.Name.Equals(item)).FirstOrDefault();
                if (current_machine == null) continue;
                int selectedIndex = 1;
                if (loadInfo)
                {
                    string[] cols_faims = Regex.Split(_project._faims, "/");
                    string faims_info = cols_faims[i];

                    if (current_machine.HasFAIMS)
                    {
                        if (faims_info.Equals("Yes"))
                            selectedIndex = 0;
                        else if (faims_info.Equals("No"))
                            selectedIndex = 1;
                        else
                            selectedIndex = 2;
                    }
                    else
                        selectedIndex = 0;
                }
                else
                {
                    selectedIndex = 0;
                    //if (current_machine.HasFAIMS)
                    //    selectedIndex = 0;
                    //else /*if (item.Equals("Fusion") || item.Equals("Elite"))*/
                    //    selectedIndex = 1;
                    //else
                    //    selectedIndex = 2;
                }

                TextBlock tbFaims = new TextBlock();
                tbFaims.Text = "FAIMS - " + item + ":";
                tbFaims.Margin = new Thickness(5);

                System.Windows.Controls.ComboBox cbFaims = new System.Windows.Controls.ComboBox();
                cbFaims.Name = "cbFaims_" + item;
                if (current_machine.HasFAIMS)
                {
                    cbFaims.Items.Add("Yes");
                    cbFaims.Items.Add("No");
                    cbFaims.Items.Add("I don't mind");
                }
                else
                    cbFaims.Items.Add("No");
                cbFaims.SelectedIndex = selectedIndex;
                cbFaims.Margin = new Thickness(-50, 5, 5, 5);

                RowDefinition rowDef = new();
                newGrid.RowDefinitions.Add(rowDef);

                Grid.SetRow(tbFaims, i);
                Grid.SetRow(cbFaims, i);

                Grid.SetColumn(tbFaims, 0);
                Grid.SetColumn(cbFaims, 1);

                newGrid.Children.Add(tbFaims);
                newGrid.Children.Add(cbFaims);

            }
            MachinesFAIMS.Children.Add(newGrid);

            this.Height += (machines.Count * 30);
            _window.Height += (machines.Count * 30);
        }
        private void ComboFaims_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            CheckBoxMachine_Click(sender, null);
        }

        private void ComboMachines_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboMachines.Text = GetSelectedMachines();
        }

        private string GetSelectedMachines()
        {
            var currentCheckedMachines = Machines.Where(a => a.IsChecked).Select(b => b.Name).ToList();
            if (currentCheckedMachines.Count == 0)
                return "Select at least one machine";
            else
                return String.Join("/", currentCheckedMachines.ToList());
        }

        private void ButtonMethod_Click(object sender, RoutedEventArgs e)
        {
            var w = new WindowMethod(_window);
            w.ShowDialog();
        }

        internal void UpdateMethods()
        {
            _methods = Regex.Split(Settings.Default.Methods, "\r\n").ToList();
            ComboMethod.ItemsSource = null;
            ComboMethod.ItemsSource = _methods;
        }

        private void TextNumberOfSamples_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (!System.Text.RegularExpressions.Regex.IsMatch(e.Text, "^[0-9./]"))
                e.Handled = true;
        }

        private void TextNumberOfSamples_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextAmountMSTime.Text = "???";
            int method_index = ComboMethod.SelectedIndex;
            if (method_index > -1)
            {
                string[] cols_method = Regex.Split(ComboMethod.SelectedItem.ToString(), "_");

                int _gradient_index = 0;
                for (_gradient_index = 0; _gradient_index < cols_method.Length; _gradient_index++)
                    if (cols_method[_gradient_index].Any(char.IsDigit) && cols_method[_gradient_index] != "BS3")
                        break;

                try
                {
                    double gradient = Convert.ToDouble(cols_method[_gradient_index]) / 60.0;
                    int samples = Convert.ToInt32(TextNumberOfSamples.Text);
                    TextAmountMSTime.Text = (samples * gradient).ToString("0.0");
                }
                catch (Exception)
                {
                    //It's not a number
                }
            }
        }

        private void ComboMethod_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            TextAmountMSTime.Text = "???";
            int method_index = ComboMethod.SelectedIndex;
            if (method_index > -1)
            {
                string[] cols_method = Regex.Split(ComboMethod.SelectedItem.ToString(), "_");

                int _gradient_index = 0;
                for (_gradient_index = 0; _gradient_index < cols_method.Length; _gradient_index++)
                    if (cols_method[_gradient_index].Any(char.IsDigit) && cols_method[_gradient_index] != "BS3")
                        break;

                try
                {
                    double gradient = Convert.ToDouble(cols_method[_gradient_index]) / 60.0;
                    int samples = Convert.ToInt32(TextNumberOfSamples.Text);
                    TextAmountMSTime.Text = (samples * gradient).ToString("0.0");
                }
                catch (Exception)
                {
                    //It's not a number
                }
            }
        }
    }
}
