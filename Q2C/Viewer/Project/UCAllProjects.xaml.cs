using Q2C.Control;
using Q2C.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using UserControl = System.Windows.Controls.UserControl;
using ComboBoxItem = Q2C.Model.ComboBoxItem;
using Q2C.Control.Database;
using System.Text.RegularExpressions;
using System.Security.RightsManagement;
using System.Windows.Media.Media3D;
using System.Reflection.PortableExecutable;
using Q2C.Viewer.Machine;

namespace Q2C.Viewer.Project
{
    /// <summary>
    /// Interaction logic for UCAllProjects.xaml
    /// </summary>
    public partial class UCAllProjects : UserControl
    {
        private User CurrentUser { get; set; }
        private MainWindow MainWindow { get; set; }
        private ObservableCollection<ComboBoxItem> Filter_Users { get; set; }
        private ObservableCollection<ComboBoxItem> Filter_Status { get; set; }
        private List<Q2C.Model.Project> Filter_Projects { get; set; }
        public UCAllProjects()
        {
            InitializeComponent();
        }

        public void Load(User _user, MainWindow _window)
        {
            CurrentUser = _user;
            MainWindow = _window;
        }

        public void Filter()
        {
            if (Filter_Status == null || Filter_Users == null) return;

            Filter_Projects = new List<Q2C.Model.Project>(Management.Projects);

            var _status = Filter_Status.Where(b => b.IsChecked == true).Select(b => b.Name).ToList();
            var _users = Filter_Users.Where(b => b.IsChecked == true).Select(b => b.Name).ToList();

            if (_users.Contains("All users"))
                Filter_Projects = Filter_Projects.Where(a => _status.Contains(a._status)).ToList();
            else
                Filter_Projects = Filter_Projects.Where(a => _status.Contains(a._status) &&
                _users.Contains(a.AddedBy)).ToList();
        }

        public void UpdateProjectDataGrid()
        {
            if (Filter_Projects == null) return;
            DataGridProjects.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(delegate ()
            {
                DataGridProjects.ItemsSource = null;
                List<Q2C.Model.Project> projects = new List<Model.Project>(Filter_Projects.Where(a => a.InfoStatus == Management.InfoStatus.Active).ToList());
                projects.Sort((a, b) => b.RegistrationDate.CompareTo(a.RegistrationDate));
                foreach (var a in projects)
                //Parallel.ForEach(projects, a =>
                {
                    a.EstimatedWaitingTime = GetEstimatedWaitingTime(projects, a);
                    a._receiveNotification = a.ReceiveNotification ? "✓" : "";
                }
                //);
                DataGridProjects.ItemsSource = projects;
            }));
        }

        private string GetEstimatedWaitingTime(List<Q2C.Model.Project> projects, Q2C.Model.Project _current_project)
        {
            if (_current_project == null || _current_project.Status == ProjectStatus.Measured) return "";

            List<string> estimated_times = new List<string>(_current_project.GetMachines.Length);

            foreach (string machine in _current_project.GetMachines)
            {
                double total_minutes = 0;

                Model.Machine? _current_machine = Management.Machines.Where(a => a.Name.Equals(machine)).FirstOrDefault();
                if (_current_machine == null) continue;

                double interval_in_hours = _current_machine.IntervalTime / 60.0;

                (List<MachineQueue> queue, List<Model.Run> evaluation, List<MachineLog> log) _current_properties;
                Management.Machines_Properties.TryGetValue(machine, out _current_properties);
                if (_current_properties == (null, null, null)) continue;

                List<MachineQueue> _current_queue = null;

                if (_current_project.Status == ProjectStatus.WaitForAcquisition)
                {
                    _current_queue = _current_properties.queue.Where(
                        a => a.ProjectID != _current_project.Id &&
                        a.InfoStatus == Management.InfoStatus.Active)
                        .ToList();
                    total_minutes = _current_queue.Select(a => Convert.ToDouble(a.AmountMS)).ToList().Sum() + (_current_queue.Count() * interval_in_hours);

                    List<double> totalAmountMS = projects.Where(a => a.Machine.Contains(machine) &&
                        a.Status == ProjectStatus.WaitForAcquisition &&
                        a.RegistrationDate < _current_project.RegistrationDate)
                        .Select(a => Convert.ToDouble(a.AmountMS))
                        .ToList();
                    total_minutes += totalAmountMS.Sum() + (totalAmountMS.Count() * interval_in_hours);
                }
                else
                {
                    _current_queue = _current_properties.queue.Where(
                        a => a.ProjectID != _current_project.Id &&
                        a.RegistrationDate < _current_project.RegistrationDate &&
                        a.InfoStatus == Management.InfoStatus.Active)
                        .ToList();
                    total_minutes = _current_queue.Select(a => Convert.ToDouble(a.AmountMS)).ToList().Sum() + (_current_queue.Count() * interval_in_hours);
                }

                total_minutes = Math.Round(total_minutes, 2);
                estimated_times.Add(total_minutes.ToString());
            }
            return String.Join("/", estimated_times.ToList());
        }

        public void AddOrUpdateProject()
        {
            if (CurrentUser == null ||
                CurrentUser.Category == UserCategory.User ||
                CurrentUser.Category == UserCategory.SuperUsrMachine)
            {

                System.Windows.MessageBox.Show(
                                        "The current user is not allowed to operate this feature.\nPlease contact the administrator.",
                                        "Q2C :: Warning",
                                        (System.Windows.MessageBoxButton)MessageBoxButtons.OK,
                                        (System.Windows.MessageBoxImage)MessageBoxIcon.Warning);
                return;
            }

            var w = new WindowProject();
            w.Load(this);
            w.ShowDialog();
        }

        private void Button_AddUpdateProject(object sender, RoutedEventArgs e)
        {
            AddOrUpdateProject();
        }

        private void DataGridProjects_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            e.Row.Header = (e.Row.GetIndex() + 1).ToString();

            Q2C.Model.Project project = e.Row.Item as Q2C.Model.Project;

            if (project != null)
            {
                if (project.Status == ProjectStatus.WaitForAcquisition)
                    e.Row.Background = Brushes.White;
                else if (project.Status == ProjectStatus.Measured)
                    e.Row.Background = Brushes.LightGray;
                else
                    e.Row.Background = Brushes.LightBlue;
            }
            else
                e.Row.IsEnabled = false;
        }

        private void DataGridProjects_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key != Key.Delete)
                return;

            if (CurrentUser == null ||
              CurrentUser.Category == UserCategory.User ||
              CurrentUser.Category == UserCategory.SuperUsrMachine)
            {

                System.Windows.MessageBox.Show(
                                        "The current user is not allowed to operate this feature.\nPlease contact the administrator.",
                                        "Q2C :: Warning",
                                        (System.Windows.MessageBoxButton)MessageBoxButtons.OK,
                                        (System.Windows.MessageBoxImage)MessageBoxIcon.Warning);
                e.Handled = true;
                return;
            }

            string mod = Util.Util.GetSelectedValue(DataGridProjects, TagProperty, 1);
            var r = System.Windows.Forms.MessageBox.Show("Do you want to remove '" + mod + "'?", "Q2C :: Warning", System.Windows.Forms.MessageBoxButtons.YesNo, System.Windows.Forms.MessageBoxIcon.Warning);
            if (r != System.Windows.Forms.DialogResult.Yes)
            {
                e.Handled = true;
                return;
            }
            RemoveSample();
        }

        private void RemoveSample()
        {
            Q2C.Model.Project _project = (Q2C.Model.Project)DataGridProjects.SelectedItem;
            if (Connection.RemoveProject(_project))
            {
                System.Windows.MessageBox.Show(
                                            "Project has been removed successfully!",
                                            "Q2C :: Information",
                                            (System.Windows.MessageBoxButton)MessageBoxButtons.OK,
                                            (System.Windows.MessageBoxImage)MessageBoxIcon.Information);

                Connection.ReadInfo();
                Filter();
                UpdateProjectDataGrid();
            }
        }

        private void DataGridProjects_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (CurrentUser == null ||
               CurrentUser.Category == UserCategory.User ||
               CurrentUser.Category == UserCategory.SuperUsrMachine)
            {

                System.Windows.MessageBox.Show(
                                        "The current user is not allowed to operate this feature.\nPlease contact the administrator.",
                                        "Q2C :: Warning",
                                        (System.Windows.MessageBoxButton)MessageBoxButtons.OK,
                                        (System.Windows.MessageBoxImage)MessageBoxIcon.Warning);
                return;
            }

            Q2C.Model.Project _project = (Q2C.Model.Project)DataGridProjects.SelectedItem;

            if (_project == null)
            {
                System.Windows.MessageBox.Show(
                                            "Project has not been found!",
                                            "Q2C :: Warning",
                                            (System.Windows.MessageBoxButton)MessageBoxButtons.OK,
                                            (System.Windows.MessageBoxImage)MessageBoxIcon.Warning);
                return;
            }

            var w = new WindowProject();
            w.Load(this, true, _project);
            w.ShowDialog();
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentUser == null ||
                CurrentUser.Category == UserCategory.User ||
                CurrentUser.Category == UserCategory.SuperUsrMachine ||
                CurrentUser.Category == UserCategory.SuperUsrSampleMachine)
            {

                System.Windows.MessageBox.Show(
                                        "The current user is not allowed to operate this feature.\nPlease contact the administrator.",
                                        "Q2C :: Warning",
                                        (System.Windows.MessageBoxButton)MessageBoxButtons.OK,
                                        (System.Windows.MessageBoxImage)MessageBoxIcon.Warning);
                return;
            }

            if (DataGridProjects.SelectedItem != null)
            {
                Q2C.Model.Project selectedProject = (Q2C.Model.Project)DataGridProjects.SelectedItem;

                if (selectedProject.Status == ProjectStatus.Measured)
                {
                    System.Windows.MessageBox.Show(
                                            "Sample has already been measured!",
                                            "Q2C :: Warning",
                                            (System.Windows.MessageBoxButton)MessageBoxButtons.OK,
                                            (System.Windows.MessageBoxImage)MessageBoxIcon.Warning);
                    return;
                }
                else if (selectedProject.Status == ProjectStatus.InProgress)
                {
                    System.Windows.MessageBox.Show(
                                            "Sample has already been queued!",
                                            "Q2C :: Warning",
                                            (System.Windows.MessageBoxButton)MessageBoxButtons.OK,
                                            (System.Windows.MessageBoxImage)MessageBoxIcon.Warning);
                    return;
                }

                if (selectedProject.GetMachines.Length > 1)
                {
                    //_Machines
                    DependencyObject _parent = VisualTreeHelper.GetParent(VisualTreeHelper.GetParent(VisualTreeHelper.GetParent(VisualTreeHelper.GetParent(VisualTreeHelper.GetParent(VisualTreeHelper.GetParent(VisualTreeHelper.GetParent(all_projects_grid)))))));
                    if (_parent is System.Windows.Controls.TabControl _tabControl)
                    {
                        TabItem _ti = (TabItem)_tabControl.Items[1];
                        UserControl _userControl = (UserControl)_ti.Content;

                        var w = new WindowQueue();
                        w.Load(this, ((UCAllMachines)_userControl), selectedProject.GetMachines, selectedProject);
                        w.ShowDialog();
                    }
                }
                else
                {
                    if (selectedProject.FAIMS[0] == ProjectFAIMS.IdontMind)
                    {
                        var r = System.Windows.Forms.MessageBox.Show("Will the sample(s) be measured by using FAIMS?", "Q2C :: Warning", System.Windows.Forms.MessageBoxButtons.YesNo, System.Windows.Forms.MessageBoxIcon.Warning);
                        if (r == System.Windows.Forms.DialogResult.Yes)
                            selectedProject.FAIMS[0] = ProjectFAIMS.Yes;
                        else
                            selectedProject.FAIMS[0] = ProjectFAIMS.No;
                    }
                    if (Connection.AddProjectToQueue(selectedProject.Machine, selectedProject))
                    {
                        System.Windows.MessageBox.Show(
                                            "Project has been added to " + selectedProject.Machine + " queue successfully!",
                                            "Q2C :: Information",
                                            (System.Windows.MessageBoxButton)MessageBoxButtons.OK,
                                            (System.Windows.MessageBoxImage)MessageBoxIcon.Information);
                        Connection.ReadInfo();
                        Filter();
                        UpdateProjectDataGrid();

                        //_Machines
                        DependencyObject _parent = VisualTreeHelper.GetParent(VisualTreeHelper.GetParent(VisualTreeHelper.GetParent(VisualTreeHelper.GetParent(VisualTreeHelper.GetParent(VisualTreeHelper.GetParent(VisualTreeHelper.GetParent(all_projects_grid)))))));
                        if (_parent is System.Windows.Controls.TabControl _tabControl)
                        {
                            TabItem _ti = (TabItem)_tabControl.Items[1];
                            UserControl _userControl = (UserControl)_ti.Content;
                            ((UCAllMachines)_userControl).UpdateMachineDataGrid(selectedProject.Machine, "Queue");
                        }
                    }
                }
            }
        }

        private void Button_FilterProject(object sender, RoutedEventArgs e)
        {
            Filter();
            UpdateProjectDataGrid();
        }

        private void CheckBoxUsers_Click(object sender, RoutedEventArgs e)
        {
            ComboUsers.Text = GetSelectedUsers();
        }
        private string GetSelectedUsers()
        {
            var currentCheckedUsers = Filter_Users.Where(a => a.IsChecked).Select(b => b.Name).ToList();
            if (currentCheckedUsers.Count == 0)
                return "All users";
            else
                return String.Join("/", currentCheckedUsers.ToList());
        }

        private void CheckBoxStatus_Click(object sender, RoutedEventArgs e)
        {
            ComboStatus.Text = GetSelectedStatus();
        }

        private string GetSelectedStatus()
        {
            var currentCheckedStatus = Filter_Status.Where(a => a.IsChecked).Select(b => b.Name).ToList();
            if (currentCheckedStatus.Count == 0)
                return "Select at least one status";
            else
                return String.Join("/", currentCheckedStatus.ToList());
        }

        public void EnableContextMenu(bool isVisible = true)
        {
            if (isVisible)
                ContextMenuQueue.Visibility = Visibility.Visible;
            else
                ContextMenuQueue.Visibility = Visibility.Hidden;
        }

        public void UpdateDatagridHeight(double height)
        {
            DataGridProjects.Height = height - 5 > 0 ? height - 5 : 0;
        }

        public void LoadComboFilter()
        {
            Filter_Status = new ObservableCollection<ComboBoxItem>()
            {
                new ComboBoxItem("Wait for acquisition",true),
                new ComboBoxItem("In Progress",true),
                new ComboBoxItem("Measured"),
            };
            ComboStatus.ItemsSource = Filter_Status;
            ComboStatus.Text = "Wait for acquisition/In Progress";

            Filter_Users = new ObservableCollection<ComboBoxItem>();
            Filter_Users.Add(new ComboBoxItem("All users", true));

            var all_projects = Management.Projects;
            if (!(all_projects == null || all_projects.Count == 0))
            {
                List<string> all_users = all_projects.Select(a => a.AddedBy).Distinct().OrderBy(b => b).ToList();
                foreach (var user in all_users)
                    Filter_Users.Add(new ComboBoxItem(user));
            }
            ComboUsers.ItemsSource = Filter_Users;
            ComboUsers.Text = "All users";

        }
    }
}
