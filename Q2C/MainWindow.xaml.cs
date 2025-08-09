using Accord.IO;
using Accord.MachineLearning;
using Accord.Math.Optimization;
using Google.Apis.Gmail.v1;
using Microsoft.VisualBasic.ApplicationServices;
using Q2C.Control;
using Q2C.Control.Database;
using Q2C.Model;
using Q2C.Properties;
using Q2C.Viewer;
using Q2C.Viewer.Machine;
using Q2C.Viewer.Project;
using Q2C.Viewer.Setup;
using Q2C.Viewer.Statistics;
using Q2C.Viewer.Updates;
using Q2C.Viewer.WindowAbout;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
using System.Security.Policy;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Xps;
using static Google.Apis.Auth.GoogleJsonWebSignature;
using static System.Net.WebRequestMethods;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using ComboBoxItem = Q2C.Model.ComboBoxItem;
using User = Q2C.Model.User;

namespace Q2C
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : System.Windows.Window
    {
        private const int REFRESH_THRESHOLD_TIME_IN_SECONDS = 60;
        private System.Windows.Threading.DispatcherTimer dispatcherTimer;
        private User CurrentUser { get; set; }

        [STAThread]
        private void ConnectDatabase()
        {
            Q2C.Model.Database db = Management.GetDatabase();
            if (db == null) throw new Exception("reset_database");
            Connection.IsOnline = db.IsOnline;

            if (Connection.IsOnline)
            {
                bool isValid = Connection.Init(db);
                if (isValid)
                    Connection.ReadSheets(true);
                else
                    throw new Exception("reset_database");
            }
            else
                Connection.RetrieveAllInfoFromDB();
        }

        private void EnableDisableStatus()
        {
            if (Connection.IsOnline)
            {
                LabelLastUpdate.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(delegate () { LabelLastUpdate.Text = "Last updated:"; }));
                LastUpdate.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(delegate () { LastUpdate.Text = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"); }));
                ProgressBarRefresh.Visibility = Visibility.Visible;
            }
            else
            {
                LabelLastUpdate.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(delegate () { LabelLastUpdate.Text = "Mode:"; }));
                LastUpdate.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(delegate () { LastUpdate.Text = "Local"; }));
                ProgressBarRefresh.Visibility = Visibility.Collapsed;
            }
        }
        private void EnableUserFeatures()
        {
            if (CurrentUser == null)
            {
                System.Windows.MessageBox.Show(
                                            "The current user is not allowed to operate this software.\nPlease contact the administrator.",
                                            "Q2C :: Error",
                                            (System.Windows.MessageBoxButton)MessageBoxButtons.OK,
                                            (System.Windows.MessageBoxImage)MessageBoxIcon.Error);

                Connection.RevokeConnection();
                this.Close();
                return;
            }

            TabItem tiProjects = (TabItem)control_tab.Items[0];
            TabItem tiMachine = (TabItem)control_tab.Items[1];
            if (CurrentUser.Category == UserCategory.Administrator)
            {
                Setup_separator.Visibility = Visibility.Visible;
                SetupMenu.Visibility = Visibility.Visible;
                _Projects.EnableContextMenu();
                StatisticsMenu.Visibility = Visibility.Visible;
                tiMachine.Visibility = Visibility.Visible;
                tiProjects.Visibility = Visibility.Visible;
                _Machines.Visibility = Visibility.Visible;
                _Projects.Visibility = Visibility.Visible;
            }
            else
            {
                tiMachine.Visibility = Visibility.Collapsed;
                tiProjects.Visibility = Visibility.Collapsed;
                _Machines.Visibility = Visibility.Collapsed;
                _Projects.Visibility = Visibility.Collapsed;

                if (CurrentUser.Category == UserCategory.User ||
                    CurrentUser.Category == UserCategory.UserSample ||
                    CurrentUser.Category == UserCategory.SuperUsrSample ||
                    CurrentUser.Category == UserCategory.MasterUsrSample ||
                    CurrentUser.Category == UserCategory.SuperUsrSampleMachine ||
                    CurrentUser.Category == UserCategory.MasterUsrSampleMachine)
                {
                    tiProjects.Visibility = Visibility.Visible;
                    _Projects.Visibility = Visibility.Visible;
                }

                if (CurrentUser.Category == UserCategory.UserSample)
                {
                    tiMachine.Visibility = Visibility.Visible;
                    _Machines.Visibility = Visibility.Visible;

                    //Enable queue tabs
                    _Machines.EnableTabs(true);
                    _Machines.EnableSubTabs(true, new string[] { "Queue" });
                    //Disable other tables
                    _Machines.EnableSubTabs(false, new string[] { "Log", "Evaluation" });
                }
                else if (CurrentUser.Category == UserCategory.SuperUsrSample ||
                   CurrentUser.Category == UserCategory.MasterUsrSample ||
                   CurrentUser.Category == UserCategory.SuperUsrMachine ||
                   CurrentUser.Category == UserCategory.SuperUsrSampleMachine ||
                   CurrentUser.Category == UserCategory.MasterUsrSampleMachine)
                {
                    tiMachine.Visibility = Visibility.Visible;
                    _Machines.Visibility = Visibility.Visible;

                    //Enable all tabs
                    _Machines.EnableTabs(true);
                    _Machines.EnableSubTabs(true, new string[] { "Queue", "Log", "Evaluation" });

                    if (CurrentUser.Category == UserCategory.SuperUsrMachine)
                        control_tab.SelectedIndex = 1;
                }

                Setup_separator.Visibility = Visibility.Collapsed;
                SetupMenu.Visibility = Visibility.Collapsed;
                _Projects.EnableContextMenu(false);

                if (CurrentUser.Category == UserCategory.MasterUsrSample ||
                CurrentUser.Category == UserCategory.MasterUsrSampleMachine)
                {
                    _Projects.EnableContextMenu();
                }

                if (CurrentUser.Category == UserCategory.MasterUsrSample ||
                    CurrentUser.Category == UserCategory.SuperUsrSample ||
                    CurrentUser.Category == UserCategory.UserSample ||
                    CurrentUser.Category == UserCategory.User)
                    ToolsMenu.Visibility = Visibility.Collapsed;
                else
                    ToolsMenu.Visibility = Visibility.Visible;
            }
        }

        private void CheckForUpdates(string current_version)
        {
            Update update = new();
            var info = update.Connect(current_version);

            if (info.HasNewRelease())
            {
                var r = System.Windows.Forms.MessageBox.Show(
                    "Q2C current version is not the latest release.\nDo you want to update it ?",
                    "Q2C :: Check for updates",
                    System.Windows.Forms.MessageBoxButtons.YesNo,
                    System.Windows.Forms.MessageBoxIcon.Warning);

                if (r != System.Windows.Forms.DialogResult.Yes) { return; }

                update.Load();
                update.ShowDialog();

            }
        }

        private async void StartApp()
        {
            string citation = $"All rights reserved®{DateTime.Now.Year}.";

            AddHyperlink(InfoLabLabel, citation);

            AppDomain.CurrentDomain.UnhandledException += AppException;

            Management.CheckSystemLanguage();

            String? version = Util.Util.GetAppVersion();

            CheckForUpdates(version);

            #region Check Database
            bool hasDatabase = false;
            int connect_count = 0;
            do
            {
                try
                {
                    hasDatabase = Management.HasDatabase();
                    if (!hasDatabase)
                    {
                        var window = new WindowDatabase();
                        window.ShowDialog();
                    }
                    connect_count++;
                }
                catch (Exception)
                {
                    throw;
                }
            } while (!hasDatabase && connect_count < 2);
            if (!hasDatabase)
            {
                System.Windows.Forms.MessageBox.Show(
                        "Failed to connect to database.\nQ2C has been finished!",
                        "Q2C :: Error",
                        System.Windows.Forms.MessageBoxButtons.OK,
                        System.Windows.Forms.MessageBoxIcon.Error);
                System.Windows.Application.Current.Shutdown();
            }
            #endregion

            var wait_screen = Util.Util.CallWaitWindow("Welcome to Q2C", "Please wait, we are loading data...");
            MainGrid.Children.Add(wait_screen);
            Grid.SetRowSpan(wait_screen, 4);
            Grid.SetRow(wait_screen, 0);
            var rows = MainGrid.RowDefinitions;
            rows[2].Height = new GridLength(2, GridUnitType.Star);

            #region Connect Database
            string msg_db = "";
            do
            {
                msg_db = "";
                try
                {
                    await Task.Run(() => ConnectDatabase());
                }
                catch (Exception e)
                {
                    msg_db = e.Message;
                    if (e.Message.Equals("reset_database"))
                    {
                        //Error to connect to google
                        if (e.InnerException != null)
                        {
                            System.Windows.Forms.MessageBox.Show(
                        "Failed to connect to database.\nPlease check the parameters.\n\nReason: " + e.InnerException.Message,
                        "Q2C :: Warning",
                        System.Windows.Forms.MessageBoxButtons.OK,
                        System.Windows.Forms.MessageBoxIcon.Warning);
                        }
                        else
                        {
                            System.Windows.Forms.MessageBox.Show(
                        "Failed to connect to database.\nPlease check the parameters.",
                        "Q2C :: Warning",
                        System.Windows.Forms.MessageBoxButtons.OK,
                        System.Windows.Forms.MessageBoxIcon.Warning);
                        }

                        Connection.RevokeConnection();

                        new DatabaseKey().Execute(null);
                    }
                    else
                        throw;
                }
            } while (msg_db.Equals("reset_database"));
            #endregion

            #region Check database and app version
            if (Connection.IsOnline)
            {
                int processed = Management.CheckAppDBVersion();
                if (processed == 1)
                {
                    System.Windows.MessageBox.Show(
                                            "The current Q2C version is not compatible with the database.\nPlease contact the administrator.",
                                            "Q2C :: Error",
                                            (System.Windows.MessageBoxButton)MessageBoxButtons.OK,
                                            (System.Windows.MessageBoxImage)MessageBoxIcon.Error);
                    this.Close();
                    return;
                }
            }

            #endregion

            #region Check user
            try
            {
                if (!Management.HasUser())
                    new UsersKey().Execute(null);

                if (!Management.IsValidUser(Management.GetComputerUser()))
                {
                    System.Windows.MessageBox.Show(
                                            "The current user is not allowed to operate this software.\nPlease contact the administrator.",
                                            "Q2C :: Error",
                                            (System.Windows.MessageBoxButton)MessageBoxButtons.OK,
                                            (System.Windows.MessageBoxImage)MessageBoxIcon.Error);

                    //the credentials are wrong
                    //reset database
                    Management.ResetDatabase();
                    Connection.RevokeConnection();
                    System.Windows.Forms.Application.Restart();
                    System.Windows.Application.Current.Shutdown();
                    return;
                }
                else
                {
                    CurrentUser = Connection.GetUser();
                }

                if (CurrentUser == null)
                    throw new Exception("reset_database");

                if (!Management.HasMachine())
                    new MachinesKey().Execute(null);
            }
            catch (Exception e)
            {
                if (e.Message.Equals("reset_database"))
                {
                    Connection.RevokeConnection();

                    new DatabaseKey().Execute(null);
                }
                else
                    throw;
            }
            #endregion

            MainGrid.Children.Remove(wait_screen);
            rows[2].Height = GridLength.Auto;

            #region Set initial features
            try
            {
                EnableDisableStatus();
                _Machines.Load(CurrentUser, this, MainGrid);
                TBUser.Text = Management.GetComputerUser();
                _Projects.Load(CurrentUser, this);
                _Projects.LoadComboFilter();
                _Projects.Filter();
                EnableUserFeatures();
                LoadDatagrid();
                UpdateWindowSize();

                this.SizeChanged += MainWindow_SizeChanged;
                this.DataContext = new WindowMainCommandContext();

                dispatcherTimer = new System.Windows.Threading.DispatcherTimer();
                dispatcherTimer.Tick += new EventHandler(dispatcherTimer_Tick);
                dispatcherTimer.Interval = new TimeSpan(0, 0, 1);
                dispatcherTimer.Start();
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            }
            catch (Exception)
            {
                throw;
            }
            #endregion
        }

        public MainWindow()
        {
            #region debug
            #endregion
            InitializeComponent();
            this.Closing += MainWindow_Closing;
            StartApp();
        }

        private void MainWindow_Closing(object? sender, CancelEventArgs e)
        {
            new CloseWindowKey().Execute(null);
        }

        private void AppException(object sender, System.UnhandledExceptionEventArgs e)
        {
            Exception exception;

            try
            {
                exception = e.ExceptionObject as Exception;
            }
            catch (Exception)
            {
                System.Windows.MessageBox.Show("Unexpected exception.");
                return;
            }

            OpenExceptionWindow(exception);
        }

        private void OpenExceptionWindow(Exception exception)
        {

            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();

                WindowAppException window = new WindowAppException(exception)
                {
                    Title = "Q2C :: Exception (beta version)",
                    Height = 400,
                    Width = 450
                };

                window.ShowDialog();
            });
        }

        private void UpdateData()
        {
            Connection.ReadSheets();
            _Projects.Filter();
            LoadDatagrid();
        }

        private void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            if (Connection.IsOnline)
            {
                if (Connection.Refresh_time > REFRESH_THRESHOLD_TIME_IN_SECONDS)
                {
                    UpdateData();
                    Connection.Refresh_time = 0;
                    LastUpdate.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(delegate () { LastUpdate.Text = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"); }));
                }
                else
                {
                    Connection.Refresh_time++;
                    ProgressBarRefresh.Value = Connection.Refresh_time * (100.0 / REFRESH_THRESHOLD_TIME_IN_SECONDS);
                }
            }

            Dictionary<string, (bool, bool)> machines_to_calibrate = Management.CheckMachinesCalibration();
            if (machines_to_calibrate != null)
                _Machines.EnableorDisableCalibration(machines_to_calibrate);
        }

        private void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateWindowSize();
        }

        private void UpdateWindowSize()
        {
            double height = this.ActualHeight - 240;
            _Projects.UpdateDatagridHeight(height);
            _Machines.UpdateDatagridHeight(height);
        }

        private void LoadDatagrid()
        {
            _Projects.UpdateProjectDataGrid();
            _Machines.LoadDataGrids();
        }

        private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.N && Keyboard.IsKeyDown(Key.LeftCtrl))
            {
                string? selected_tab = ((TabItem)(control_tab.SelectedItem)).Header.ToString();
                if (String.IsNullOrEmpty(selected_tab)) return;
                if (selected_tab == "Projects")
                    _Projects.AddOrUpdateProject();
                else if (selected_tab == "Machines")
                {
                    bool isSelectedTab = false;
                    var selected_machine = ((TabItem)_Machines.all_machines_tab.SelectedItem).Header.ToString();

                    if (!String.IsNullOrEmpty(selected_machine))
                    {
                        foreach (var sub_tab_machine in _Machines.all_machines_tab.Items)
                        {
                            var machine_name = ((TabItem)(sub_tab_machine)).Header.ToString();
                            if (String.IsNullOrEmpty(machine_name)) continue;

                            if (selected_machine != machine_name) continue;

                            System.Windows.Controls.TabControl machine_sub_tab_control = (System.Windows.Controls.TabControl)((TabItem)sub_tab_machine).Content;
                            if (machine_sub_tab_control.SelectedItem == null) continue;

                            selected_tab = ((TabItem)(machine_sub_tab_control.SelectedItem)).Header.ToString();
                            if (String.IsNullOrEmpty(selected_tab)) return;

                            switch (selected_tab)
                            {
                                case "Evaluation":
                                    _Machines.AddUpdateRun(machine_name);
                                    isSelectedTab = true;
                                    break;
                                case "Log":
                                    _Machines.AddUpdateLog(machine_name);
                                    isSelectedTab = true;
                                    break;
                                default: break;
                            }
                            if (isSelectedTab) break;
                        }
                    }
                }
            }
        }
        #region menu
        private void MenuItemExit_Click(object sender, RoutedEventArgs e)
        {
            new CloseWindowKey().Execute(sender);
        }
        private void MenuItemStatisticsQueue_Click(object sender, RoutedEventArgs e)
        {
            new CheckStatisticsQueueKey().Execute(sender);
        }
        private void MenuItemStatisticsEvaluation_Click(object sender, RoutedEventArgs e)
        {
            new CheckStatisticsEvaluationKey().Execute(sender);
        }
        private void MenuItem_Users(object sender, RoutedEventArgs e)
        {
            Connection.Refresh_time = int.MinValue;
            new UsersKey().Execute(sender);
        }
        private void MenuItem_Machines(object sender, RoutedEventArgs e)
        {
            Connection.Refresh_time = int.MinValue;
            new MachinesKey().Execute(sender);
        }
        private void MenuItem_ReadMe(object sender, RoutedEventArgs e)
        {
            new ReadMeKey().Execute(sender);
        }
        private void MenuItem_About(object sender, RoutedEventArgs e)
        {
            Connection.Refresh_time = int.MinValue;
            new AboutKey().Execute(sender);
        }
        private void MenuItem_Database(object sender, RoutedEventArgs e)
        {
            Connection.Refresh_time = int.MinValue;
            new DatabaseKey().Execute(sender);
        }

        #endregion

        private void MenuItem_CheckForUpdates(object sender, RoutedEventArgs e)
        {
            Connection.Refresh_time = int.MinValue;
            Update winUpdate = new();
            winUpdate.Load();
            winUpdate.ShowDialog();
        }

        private void MenuItem_DiscussionForum(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start(new ProcessStartInfo
                {
                    FileName = "https://github.com/diogobor/Q2C/issues",
                    UseShellExecute = true,
                });
            }
            catch (Exception)
            {
                System.Windows.MessageBox.Show(
                        "Visit the Q2C website for more usability information.",
                        "Q2C :: Discussion Forum",
                        (System.Windows.MessageBoxButton)MessageBoxButtons.OK,
                        (System.Windows.MessageBoxImage)MessageBoxIcon.Information);
                throw;
            }
        }

        private void AddHyperlink(TextBlock textBlock, string processing_time)
        {
            textBlock.Inlines.Clear();

            // Create a new Hyperlink
            Hyperlink hyperlink = new Hyperlink();
            hyperlink.Inlines.Add("cite us");
            hyperlink.NavigateUri = new System.Uri("https://doi.org/10.1016/j.jprot.2025.105511");
            hyperlink.RequestNavigate += Hyperlink_RequestNavigate;
            // Add the Hyperlink to the TextBlock
            textBlock.Inlines.Add(processing_time);
            textBlock.Inlines.Add(" Please ");
            textBlock.Inlines.Add(hyperlink);
            textBlock.Inlines.Add(".");
        }
        // Event handler for when the hyperlink is clicked
        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            // Open the link in the default web browser
            try
            {
                System.Diagnostics.Process.Start(new ProcessStartInfo
                {
                    FileName = e.Uri.ToString(),
                    UseShellExecute = true,
                });
            }
            catch (Exception)
            {
                System.Windows.MessageBox.Show(
                        "Visit the Q2C website for more usability information.",
                        "Q2C :: Manuscript",
                        (System.Windows.MessageBoxButton)MessageBoxButtons.OK,
                        (System.Windows.MessageBoxImage)MessageBoxIcon.Information);
                throw;
            }
        }

    }

    internal class CloseWindowKey : ICommand
    {
        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            System.Environment.Exit(1);
        }
    }

    internal class CheckStatisticsQueueKey : ICommand
    {
        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            User user = Connection.GetUser();
            if (user != null && (user.Category == UserCategory.Administrator ||
                user.Category == UserCategory.SuperUsrMachine ||
                user.Category == UserCategory.SuperUsrSampleMachine ||
                user.Category == UserCategory.MasterUsrSampleMachine))
            {
                var w = new WindowStatisticalQueue();
                w.ShowDialog();
            }
        }
    }

    internal class CheckStatisticsEvaluationKey : ICommand
    {
        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            User user = Connection.GetUser();
            if (user != null && (user.Category == UserCategory.Administrator ||
                user.Category == UserCategory.SuperUsrMachine ||
                user.Category == UserCategory.SuperUsrSampleMachine ||
                user.Category == UserCategory.MasterUsrSampleMachine))
            {
                var w = new WindowStatisticalEvaluation();
                w.ShowDialog();
            }
        }
    }

    internal class UsersKey : ICommand
    {
        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            bool canOpen = false;
            if (!Management.HasUser())
                canOpen = true;
            else
            {
                User user = Connection.GetUser();
                if (user != null && user.Category == UserCategory.Administrator)
                    canOpen = true;
            }

            if (canOpen)
            {
                WindowUsers info = new WindowUsers();
                info.ShowDialog();
            }
        }
    }

    internal class MachinesKey : ICommand
    {
        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            bool canOpen = false;
            if (!Management.HasMachine())
                canOpen = true;
            else
            {
                User user = Connection.GetUser();
                if (user != null && user.Category == UserCategory.Administrator)
                    canOpen = true;
            }

            if (canOpen)
            {
                var window = new WindowMachines();
                window.ShowDialog();
            }
        }
    }

    internal class ReadMeKey : ICommand
    {
        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            try
            {
                System.Diagnostics.Process.Start(new ProcessStartInfo
                {
                    FileName = "https://github.com/diogobor/Q2C#readme",
                    UseShellExecute = true,
                });
            }
            catch (Exception)
            {
                System.Windows.MessageBox.Show(
                        "Visit the Q2C website for more usability information.",
                        "Q2C :: Read Me",
                        (System.Windows.MessageBoxButton)MessageBoxButtons.OK,
                        (System.Windows.MessageBoxImage)MessageBoxIcon.Information);
                throw;
            }
        }
    }

    internal class AboutKey : ICommand
    {
        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            WindowAbout info = new WindowAbout();
            info.ShowDialog();
        }
    }

    internal class DatabaseKey : ICommand
    {
        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            User user = Connection.GetUser();
            if (user != null)
            {
                if (user.Category == UserCategory.Administrator)
                {
                    var window = new WindowDatabase();
                    window.ShowDialog();
                }
            }
            else
            {
                //there is no user and the credentials are wrong
                //reset database
                Management.ResetDatabase();
                Connection.RevokeConnection();
                System.Windows.Forms.Application.Restart();
                System.Windows.Application.Current.Shutdown();
            }
        }
    }

    public class WindowMainCommandContext
    {
        public WindowMainCommandContext() { }
        public ICommand CloseWindowCommand
        {
            get
            {
                return new CloseWindowKey();
            }
        }

        public ICommand StatisticsQueueCommand
        {
            get
            {
                return new CheckStatisticsQueueKey();
            }
        }

        public ICommand StatisticsEvaluationCommand
        {
            get
            {
                return new CheckStatisticsEvaluationKey();
            }
        }

        public ICommand UsersCommand
        {
            get
            {
                return new UsersKey();
            }
        }

        public ICommand MachinesCommand
        {
            get
            {
                return new MachinesKey();
            }
        }
        public ICommand ReadMeCommand
        {
            get
            {
                return new ReadMeKey();
            }
        }

        public ICommand DatabaseCommand
        {
            get
            {
                return new DatabaseKey();
            }
        }
    }
}
