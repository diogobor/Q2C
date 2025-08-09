using Q2C.Control.Statistics;
using Q2C.Control;
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
using Q2C.Model;
using Q2C.Control.Database;
using Google.Apis.Logging;
using Xceed.Wpf.Toolkit.PropertyGrid.Editors;
using System.Reflection.PortableExecutable;
using System.Collections;
using System.Windows.Controls.Primitives;
using System.Text.RegularExpressions;
using System.ComponentModel;
using Newtonsoft.Json.Linq;
using System.Reflection;
using Q2C.Viewer.Project;
using System.IO;
using System.Globalization;
using ThermoFisher.CommonCore.Data;
using System.Net.WebSockets;
using System.Security.Policy;

namespace Q2C.Viewer.Machine
{
    /// <summary>
    /// Interaction logic for UCAllMachines.xaml
    /// </summary>
    public partial class UCAllMachines : UserControl
    {
        private readonly string[] Features = new string[] { "Queue", "Evaluation", "Log" };
        private User CurrentUser { get; set; }

        public static bool HasFaimsEvaluation { get; set; } = false;
        public static bool HasOTEvaluation { get; set; } = false;
        public static bool HasITEvaluation { get; set; } = false;
        private Window MainWindow { get; set; }
        private Grid MainGrid { get; set; }
        public UCAllMachines()
        {
            InitializeComponent();
        }

        #region All datagrids
        public void Load(User current_user, Window _window, Grid mainGrid)
        {
            CurrentUser = current_user;
            MainWindow = _window;
            MainGrid = mainGrid;
            CreateTabMachines();
        }
        private void CreateTabMachines()
        {
            List<Model.Machine> machines = Management.GetMachines().ToList();
            if (machines.Count == 0) return;
            foreach (var machine in machines)
            {
                TabItem tabItem = new TabItem();
                tabItem.Header = machine.Name;

                TabControl tb = new TabControl();
                tabItem.Content = tb;

                TabItem tiQueue = new TabItem();
                tiQueue.Header = "Queue";
                tiQueue.Content = CreateDataGridQueue(machine.Name);
                tb.Items.Add(tiQueue);

                if (machine.HasEvaluation)
                {
                    #region Evaluation
                    TabItem tiEvaluation = new TabItem();
                    tiEvaluation.Header = "Evaluation";

                    Grid grid_evaluation = new Grid();
                    grid_evaluation.Name = "grid_evaluation_" + machine.Name;
                    grid_evaluation.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                    grid_evaluation.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                    grid_evaluation.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                    grid_evaluation.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                    Grid grid_control_eval = new Grid();
                    grid_control_eval.Name = "grid_control_eval_" + machine.Name;
                    grid_control_eval.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
                    grid_control_eval.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                    GroupBox groupBoxAddRun = CreateGroupBoxAddRun(machine.Name);
                    Grid.SetColumn(groupBoxAddRun, 0);
                    grid_control_eval.Children.Add(groupBoxAddRun);

                    GroupBox groupBoxControlRun = CreateGroupBoxControlsRun(machine.Name, machine.HasFAIMS, machine.HasOT, machine.HasIT);
                    Grid.SetColumn(groupBoxControlRun, 1);
                    grid_control_eval.Children.Add(groupBoxControlRun);

                    Grid.SetRow(grid_control_eval, 0);
                    grid_evaluation.Children.Add(grid_control_eval);

                    GroupBox groupBoxAvg = CreateGroupBoxAvg(machine.Name);
                    Grid.SetRow(groupBoxAvg, 1);
                    grid_evaluation.Children.Add(groupBoxAvg);

                    ScrollViewer sv_run = CreateScrollViewerRun(machine.Name, machine.HasOT, machine.HasIT);
                    Grid.SetRow(sv_run, 2);
                    grid_evaluation.Children.Add(sv_run);

                    tiEvaluation.Content = grid_evaluation;
                    tb.Items.Add(tiEvaluation);
                    #endregion

                    #region Log
                    TabItem tiLog = new TabItem();
                    tiLog.Header = "Log";

                    Grid grid_log = new Grid();
                    grid_log.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                    grid_log.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                    grid_log.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                    GroupBox groupBoxLog = CreateGroupBoxLog(machine.Name);
                    Grid.SetRow(groupBoxLog, 1);
                    grid_log.Children.Add(groupBoxLog);

                    DataGrid data_grid = CreateDataGridLog(machine.Name);
                    Grid.SetRow(data_grid, 2);
                    grid_log.Children.Add(data_grid);

                    tiLog.Content = grid_log;
                    tb.Items.Add(tiLog);
                    #endregion
                }
                all_machines_tab.Items.Add(tabItem);
            }
        }
        private void UpdateMachineDataGrid(string machine, string feature, DataGrid[] dataGrid, bool isFAIMS = true, bool isOT = true, bool isIT = true)
        {
            if (dataGrid == null) return;

            (List<MachineQueue> queue, List<Model.Run> evaluation, List<MachineLog> log) prop;
            Management.Machines_Properties.TryGetValue(machine, out prop);
            if (prop == (null, null, null))
                return;

            switch (feature)
            {
                case "Queue":
                    if (dataGrid[0] == null) return;

                    if (prop.queue != null)
                        dataGrid[0].Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(delegate ()
                        {
                            var _qeue = prop.queue.Where(a => a.InfoStatus == Management.InfoStatus.Active).ToList();
                            _qeue.Sort((a, b) => b.RegistrationDate.CompareTo(a.RegistrationDate));
                            dataGrid[0].ItemsSource = null;
                            dataGrid[0].ItemsSource = _qeue;
                        }));
                    break;
                case "Evaluation":

                    if (prop.evaluation != null)
                    {
                        List<Model.Run> _runOT = null;
                        List<Model.Run> _runIT = null;
                        if (isOT && dataGrid[0] != null)
                        {
                            if (isFAIMS)
                                _runOT = prop.evaluation.Where(a => a.OT.FAIMS == true &&
                                a.OT.ProteinGroup != 0 && a.OT.PeptideGroup != 0 && a.OT.PSM != 0 &&
                                a.InfoStatus == Management.InfoStatus.Active).ToList();
                            else
                                _runOT = prop.evaluation.Where(a => a.OT.FAIMS == false &&
                                a.OT.ProteinGroup != 0 && a.OT.PeptideGroup != 0 && a.OT.PSM != 0 &&
                                a.InfoStatus == Management.InfoStatus.Active).ToList();
                            _runOT.Sort((a, b) => b.OT.RegistrationDate.CompareTo(a.OT.RegistrationDate));

                            dataGrid[0].Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(delegate ()
                            {
                                dataGrid[0].ItemsSource = null;
                                dataGrid[0].ItemsSource = _runOT;
                            }));
                        }

                        if (isIT && dataGrid[1] != null)
                        {
                            if (isFAIMS)
                                _runIT = prop.evaluation.Where(a => a.IT.FAIMS == true &&
                                a.IT.ProteinGroup != 0 && a.IT.PeptideGroup != 0 && a.IT.PSM != 0 &&
                                a.InfoStatus == Management.InfoStatus.Active).ToList();
                            else
                                _runIT = prop.evaluation.Where(a => a.IT.FAIMS == false &&
                                a.IT.ProteinGroup != 0 && a.IT.PeptideGroup != 0 && a.IT.PSM != 0 &&
                                a.InfoStatus == Management.InfoStatus.Active).ToList();

                            _runIT.Sort((a, b) => b.IT.RegistrationDate.CompareTo(a.IT.RegistrationDate));

                            dataGrid[1].Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(delegate ()
                            {
                                dataGrid[1].ItemsSource = null;
                                dataGrid[1].ItemsSource = _runIT;
                            }));
                        }

                        ComputeAverages(machine, _runOT, _runIT, isOT, isIT);
                    }
                    break;
                case "Log":
                    if (dataGrid[0] == null) return;

                    if (prop.log != null)
                        dataGrid[0].Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(delegate ()
                        {
                            prop.log.ForEach(a =>
                            {
                                a._isFullyCalibrated = a.IsFullyCalibrated ? "✓" : "";
                                a._isCalibrated = a.IsCalibrated ? "✓" : "";
                            });

                            var _log = prop.log.Where(a => a.InfoStatus == Management.InfoStatus.Active).ToList();
                            _log.Sort((a, b) => b.RegistrationDate.CompareTo(a.RegistrationDate));
                            dataGrid[0].ItemsSource = null;
                            dataGrid[0].ItemsSource = _log;
                        }));
                    break;
                default: break;
            }
        }
        private void ComputeAverages(string machine,
            List<Model.Run> _runOT,
            List<Model.Run> _runIT,
            bool isOT,
            bool isIT)
        {
            string[] properties = new string[8] { "MS1Intensity", "MS2Intensity", "ProteinGroup", "PeptideGroup", "PSM", "MSMS", "IDRatio", "XreaMean" };
            if (isOT)
            {
                if (_runOT != null)
                {
                    _runOT = _runOT.Where(a => a.OT.Exclude == false).ToList();

                    for (int i = 0; i < properties.Length; i++)
                    {
                        string property = properties[i];
                        string element_name = $"Avg{property}_OT_{machine}";
                        TextBlock tb_element = (TextBlock)RetrieveElement("Evaluation", element_name, machine, all_machines_grid);
                        string _value = "";
                        if (i < 2)
                            _value = _runOT.Count > 0 ? _runOT.Average(a => (double)(a.OT.GetType().GetProperty(property).GetValue(a.OT))).ToString("E2") : "????";
                        else if (property == "IDRatio")
                            _value = _runOT.Count > 0 ? Convert.ToDouble(_runOT.Average(a => (double)(a.OT.GetType().GetProperty(property).GetValue(a.OT)))).ToString("N2") : "????";
                        else if (property == "XreaMean")
                            _value = _runOT.Count > 0 ? _runOT.Where(a => a.OT.XreaMean > 0).Count() > 0 ? _runOT.Where(a => a.OT.XreaMean > 0).Average(a => a.OT.XreaMean).ToString("N2") : "????" : "????";
                        else
                            _value = _runOT.Count > 0 ? Convert.ToInt64(_runOT.Average(a => (double)(a.OT.GetType().GetProperty(property).GetValue(a.OT)))).ToString() : "????";

                        if (i == 6)
                            tb_element.Text = _value + "%";
                        else
                            tb_element.Text = _value;
                    }
                }
            }

            if (isIT)
            {
                if (_runIT != null)
                {
                    _runIT = _runIT.Where(a => a.IT.Exclude == false).ToList();

                    for (int i = 0; i < properties.Length; i++)
                    {
                        string property = properties[i];
                        string element_name = $"Avg{property}_IT_{machine}";
                        TextBlock tb_element = (TextBlock)RetrieveElement("Evaluation", element_name, machine, all_machines_grid);
                        string _value = "";
                        if (i < 2)
                            _value = _runIT.Count > 0 ? _runIT.Average(a => (double)(a.IT.GetType().GetProperty(property).GetValue(a.IT))).ToString("E2") : "????";
                        else if (property == "IDRatio")
                            _value = _runIT.Count > 0 ? Convert.ToDouble(_runIT.Average(a => (double)(a.IT.GetType().GetProperty(property).GetValue(a.IT)))).ToString("N2") : "????";
                        else if (property == "XreaMean")
                            _value = _runIT.Count > 0 ? _runIT.Where(a => a.IT.XreaMean > 0).Count() > 0 ? _runIT.Where(a => a.IT.XreaMean > 0).Average(a => a.IT.XreaMean).ToString("N2") : "????" : "????";
                        else
                            _value = _runIT.Count > 0 ? Convert.ToInt64(_runIT.Average(a => (double)(a.IT.GetType().GetProperty(property).GetValue(a.IT)))).ToString() : "????";

                        if (i == 6)
                            tb_element.Text = _value + "%";
                        else
                            tb_element.Text = _value;
                    }
                }
            }
        }
        public void UpdateMachineDataGrid(string machine, string feature, bool isFAIMS = true, bool isOT = true, bool isIT = true)
        {
            string datagrid_name = $"DataGridMachines_{feature}_{machine}";
            switch (feature)
            {
                case "Evaluation":
                    datagrid_name = $"DataGridMachines_{feature}_{machine}_OT";
                    DataGrid dataGridOT = (DataGrid)RetrieveElement(feature, datagrid_name, machine, all_machines_grid);
                    datagrid_name = $"DataGridMachines_{feature}_{machine}_IT";
                    DataGrid dataGridIT = (DataGrid)RetrieveElement(feature, datagrid_name, machine, all_machines_grid);
                    UpdateMachineDataGrid(machine, feature, new DataGrid[2] { dataGridOT, dataGridIT }, isFAIMS, isOT, isIT);
                    break;
                default:
                    DataGrid dataGrid_default = (DataGrid)RetrieveElement(feature, datagrid_name, machine, all_machines_grid);
                    UpdateMachineDataGrid(machine, feature, new DataGrid[1] { dataGrid_default });
                    break;
            }
        }
        public void UpdateMachineDataGrids()
        {
            foreach (var machine_property in Management.Machines_Properties)
            {
                foreach (string feature in Features)
                {
                    Model.Machine? current_machine = Management.GetMachine(machine_property.Key);
                    if (current_machine == null) continue;
                    if (feature == "Evaluation")
                        ApplyFilterFromCheckboxes(machine_property.Key);
                    else
                        UpdateMachineDataGrid(machine_property.Key, feature, current_machine.HasFAIMS, current_machine.HasOT, current_machine.HasIT);
                }
            }
        }
        public void SetTab(int index = 0)
        {
            all_machines_tab.SelectedIndex = index;
        }
        public void EnableTabMachines(string machine, bool enable)
        {
            TabItem tiMachine = (TabItem)RetrieveElement("", "", machine, all_machines_grid, 1);
            if (tiMachine == null) return;

            if (enable)
                tiMachine.Visibility = Visibility.Visible;
            else
                tiMachine.Visibility = Visibility.Collapsed;
        }
        public void EnableTabs(bool enable)
        {
            foreach (var machine_property in Management.Machines_Properties)
                EnableTabMachines(machine_property.Key, enable);
        }
        public void EnableSubTabMachines(string machine, string feature, bool enable)
        {
            string datagrid_name = $"DataGridMachines_{feature}_{machine}";
            switch (feature)
            {
                case "Queue":
                    TabItem tiQueue = (TabItem)RetrieveElement(feature, "", machine, all_machines_grid, 2);
                    if (tiQueue == null) return;

                    DataGrid dataGrid_queue = (DataGrid)RetrieveElement(feature, datagrid_name, machine, all_machines_grid);
                    if (dataGrid_queue == null) return;

                    if (enable)
                    {
                        tiQueue.Visibility = Visibility.Visible;
                        dataGrid_queue.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        tiQueue.Visibility = Visibility.Collapsed;
                        dataGrid_queue.Visibility = Visibility.Collapsed;
                    }
                    break;
                case "Evaluation":
                    TabItem tiEval = (TabItem)RetrieveElement(feature, "", machine, all_machines_grid, 2);
                    if (tiEval == null) return;

                    datagrid_name = $"DataGridMachines_Evaluation_{machine}_OT";
                    DataGrid dataGridOT = (DataGrid)RetrieveElement(feature, datagrid_name, machine, all_machines_grid);
                    datagrid_name = $"DataGridMachines_Evaluation_{machine}_IT";
                    DataGrid dataGridIT = (DataGrid)RetrieveElement(feature, datagrid_name, machine, all_machines_grid);

                    if (enable)
                    {
                        tiEval.Visibility = Visibility.Visible;
                        if (dataGridOT != null)
                            dataGridOT.Visibility = Visibility.Visible;
                        if (dataGridIT != null)
                            dataGridIT.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        tiEval.Visibility = Visibility.Collapsed;
                        if (dataGridOT != null)
                            dataGridOT.Visibility = Visibility.Collapsed;
                        if (dataGridIT != null)
                            dataGridIT.Visibility = Visibility.Collapsed;
                    }
                    break;
                case "Log":
                    TabItem tiLog = (TabItem)RetrieveElement(feature, "", machine, all_machines_grid, 2);
                    if (tiLog == null) return;

                    DataGrid dataGrid_log = (DataGrid)RetrieveElement(feature, datagrid_name, machine, all_machines_grid);
                    if (dataGrid_log == null) return;

                    if (enable)
                    {
                        tiLog.Visibility = Visibility.Visible;
                        dataGrid_log.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        tiLog.Visibility = Visibility.Collapsed;
                        dataGrid_log.Visibility = Visibility.Collapsed;
                    }
                    break;
                default:
                    break;
            }
        }
        public void EnableSubTabs(bool enable, string[] features)
        {
            foreach (var machine_property in Management.Machines_Properties)
            {
                foreach (string feature in features)
                    EnableSubTabMachines(machine_property.Key, feature, enable);
            }
        }

        public void LoadDataGrids()
        {
            UpdateMachineDataGrids();
        }
        public void UpdateDatagridHeight(double height)
        {
            foreach (var machine_property in Management.Machines_Properties)
            {
                foreach (string feature in Features)
                {
                    string datagrid_name = $"DataGridMachines_{feature}_{machine_property.Key}";
                    DataGrid dataGrid = (DataGrid)RetrieveElement(feature, datagrid_name, machine_property.Key, all_machines_grid);
                    switch (feature)
                    {
                        case "Queue":
                            if (dataGrid == null) continue;
                            dataGrid.Height = height > 0 ? height + 7 : 0;
                            break;
                        case "Evaluation":
                            datagrid_name = $"DataGridMachines_{feature}_{machine_property.Key}_OT";
                            DataGrid dataGridOT = (DataGrid)RetrieveElement(feature, datagrid_name, machine_property.Key, all_machines_grid);
                            datagrid_name = $"DataGridMachines_{feature}_{machine_property.Key}_IT";
                            DataGrid dataGridIT = (DataGrid)RetrieveElement(feature, datagrid_name, machine_property.Key, all_machines_grid);
                            if (dataGridOT != null)
                                dataGridOT.Height = height > 0 ? height - 140 : 0;
                            if (dataGridIT != null)
                                dataGridIT.Height = height > 0 ? height - 140 : 0;
                            break;
                        case "Log":
                            if (dataGrid == null) continue;
                            dataGrid.Height = height > 0 ? height - 45 : 0;
                            break;
                        default:
                            break;
                    }
                }
            }
        }
        public UIElement RetrieveElement(string feature, string name, string machine, Grid mainGrid, byte element = 0)
        {
            if (mainGrid == null) return null;

            foreach (TabItem tabItem in ((TabControl)mainGrid.Children[0]).Items)
            {
                if (!tabItem.Header.ToString().Equals(machine)) continue;
                if (element == 1)
                    return tabItem;

                if (element == 3)
                    return (TabControl)tabItem.Content;

                foreach (TabItem tabItem_feature in ((TabControl)tabItem.Content).Items)
                {
                    if (!tabItem_feature.Header.ToString().Equals(feature)) continue;
                    if (element == 2)
                        return tabItem_feature;

                    switch (feature)
                    {
                        case "Queue":
                            DataGrid datagrid_queue = (DataGrid)tabItem_feature.Content;
                            if (datagrid_queue != null && datagrid_queue.Name.Equals(name))
                                return datagrid_queue;
                            break;
                        case "Evaluation":
                            if (element == 4 || element == 5)
                            {
                                StackPanel sp_controls = ((StackPanel)((GroupBox)((Grid)((Grid)((Grid)tabItem_feature.Content)).Children[0]).Children[1]).Content);
                                if (element == 5)
                                    return sp_controls;

                                foreach (var sp_item in sp_controls.Children)
                                {
                                    if (sp_item is TextBlock tb_calibration)
                                    {
                                        if (tb_calibration.Name.Equals(name))
                                            return tb_calibration;
                                    }
                                }
                            }

                            if (element == 6)
                                return ((Grid)((ScrollViewer)((Grid)tabItem_feature.Content).Children[2]).Content);

                            bool isOT = name.Contains("_OT");
                            foreach (var child in ((Grid)((ScrollViewer)((Grid)tabItem_feature.Content).Children[2]).Content).Children)
                            {
                                if (child is GroupBox gb_ot_it)
                                {
                                    string gb_name_ot_it = Regex.Split(gb_ot_it.Name.Replace("GroupBox_", ""), "_")[0];
                                    if (gb_name_ot_it.Equals("OT"))
                                    {
                                        if (isOT)
                                        {
                                            DataGrid dg_OT = (DataGrid)gb_ot_it.Content;
                                            if (dg_OT.Name.Equals(name))
                                                return dg_OT;
                                        }
                                    }
                                    else if (gb_name_ot_it.Equals("IT"))
                                    {
                                        if (!isOT)
                                        {
                                            DataGrid dg_IT = (DataGrid)gb_ot_it.Content;
                                            if (dg_IT.Name.Equals(name))
                                                return dg_IT;
                                        }
                                    }
                                }
                                else if (child is TextBlock tb_ot_it)
                                {
                                    if (tb_ot_it.Name.Equals(name))
                                        return tb_ot_it;
                                }
                            }
                            break;
                        case "Log":
                            DataGrid datagrid = (DataGrid)((Grid)tabItem_feature.Content).Children[1];
                            if (datagrid != null && datagrid.Name.Equals(name))
                                return datagrid;
                            break;
                        default: break;

                    }
                }
            }
            return null;
        }

        #endregion

        #region DataGrid Queue
        private DataGrid CreateDataGridQueue(string machine)
        {
            // Create the DataGrid
            DataGrid dataGrid = new DataGrid
            {
                Name = $"DataGridMachines_Queue_{machine}",
                AutoGenerateColumns = false,
                AlternatingRowBackground = System.Windows.Media.Brushes.WhiteSmoke,
                VerticalScrollBarVisibility = ScrollBarVisibility.Visible,
                SelectionMode = DataGridSelectionMode.Single,
                MinHeight = 310
            };

            // Create a column for "Registration Date"
            DataGridTextColumn registrationDateColumn = CreateTextColumn("Registration Date", 155, 150, "RegistrationDateStr");
            DataGridTextColumn projectNameColumn = CreateTextColumn("Project name", 700, 250, "ProjectName");
            DataGridTextColumn amountMSColumn = CreateTextColumn("Amount of MS time (hours)", 175, 155, "AmountMS");
            DataGridTextColumn numberOfSamplesColumn = CreateTextColumn("Number of Samples", 155, 155, "NumberOfSamples");
            DataGridTextColumn fAIMSColumn = CreateTextColumn("FAIMS", 155, 155, "FAIMS");
            DataGridTextColumn methodColumn = CreateTextColumn("Method", 355, 255, "Method");

            // Add the column to the DataGrid
            dataGrid.Columns.Add(registrationDateColumn);
            dataGrid.Columns.Add(projectNameColumn);
            dataGrid.Columns.Add(amountMSColumn);
            dataGrid.Columns.Add(numberOfSamplesColumn);
            dataGrid.Columns.Add(fAIMSColumn);
            dataGrid.Columns.Add(methodColumn);
            dataGrid.LoadingRow += DataGridMachines_LoadingRow;
            dataGrid.PreviewKeyDown += DataGridMachines_Queue_PreviewKeyDown;

            // Create a style for column headers
            Style columnHeaderStyle = new Style(typeof(DataGridColumnHeader));
            columnHeaderStyle.Setters.Add(new Setter(HorizontalContentAlignmentProperty, HorizontalAlignment.Center));

            // Set the DataGrid's ColumnHeaderStyle
            dataGrid.ColumnHeaderStyle = columnHeaderStyle;

            return dataGrid;
        }
        private DataGridTextColumn CreateTextColumn(string header, double maxWidth, double minWidth, string binding, byte hasStringFormat = 0, string stringFormat = "", bool has_max_width = true)
        {
            DataGridTextColumn datagrid_textColumn = new DataGridTextColumn
            {
                Header = header,
                MinWidth = minWidth,
                IsReadOnly = true,
                CanUserSort = false
            };
            datagrid_textColumn.Width = minWidth;

            if (has_max_width == true)
                datagrid_textColumn.MaxWidth = maxWidth;

            if (hasStringFormat == 0)
            {
                Binding _binding = new Binding(binding);
                _binding.StringFormat = stringFormat;
                datagrid_textColumn.Binding = _binding;
            }
            else if (hasStringFormat == 1)
            {
                Binding _binding = new Binding(binding);
                _binding.Converter = new FileNameConverter();
                datagrid_textColumn.Binding = _binding;
            }
            else
                datagrid_textColumn.Binding = new System.Windows.Data.Binding(binding);

            // Set the element style for the column
            datagrid_textColumn.ElementStyle = new Style(typeof(TextBlock));
            datagrid_textColumn.ElementStyle.Setters.Add(new Setter(HorizontalAlignmentProperty, System.Windows.HorizontalAlignment.Center));

            return datagrid_textColumn;
        }
        private void DataGridColumn_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (sender is DataGridColumn)
            {
                double newWidth = e.NewSize.Width;
                double oldWidth = e.PreviousSize.Width;

                if (newWidth != oldWidth)
                {

                }
            }
        }

        private void DataGridMachines_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            e.Row.Header = (e.Row.GetIndex() + 1).ToString();
        }
        private void DataGridMachines_Queue_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key != Key.Delete)
                return;

            if (CurrentUser == null ||
              CurrentUser.Category == UserCategory.User ||
              CurrentUser.Category == UserCategory.UserSample)
            {

                System.Windows.MessageBox.Show(
                                        "The current user is not allowed to operate this feature.\nPlease contact the administrator.",
                                        "Q2C :: Warning",
                                        (System.Windows.MessageBoxButton)System.Windows.Forms.MessageBoxButtons.OK,
                                        (System.Windows.MessageBoxImage)System.Windows.Forms.MessageBoxIcon.Warning);
                e.Handled = true;
                return;
            }

            DataGrid current_datagrid = (DataGrid)sender;
            if (current_datagrid == null) return;
            if (current_datagrid.SelectedItem == CollectionView.NewItemPlaceholder) return;

            string machine = current_datagrid.Name.Replace("DataGridMachines_Queue_", "");

            string mod = Util.Util.GetSelectedValue(current_datagrid, TagProperty, 1);
            var r = System.Windows.Forms.MessageBox.Show("Has '" + mod + "' sample been measured?", "Q2C :: Warning", System.Windows.Forms.MessageBoxButtons.YesNoCancel, System.Windows.Forms.MessageBoxIcon.Warning);
            if (r == System.Windows.Forms.DialogResult.Yes)
                RemoveProjectFromQueue(current_datagrid, machine, true);
            else if (r == System.Windows.Forms.DialogResult.No)
                RemoveProjectFromQueue(current_datagrid, machine, false);
            else
                e.Handled = true;
        }
        private void RemoveProjectFromQueue(DataGrid dataGrid, string machine, bool hasMeasured)
        {
            MachineQueue mq = (MachineQueue)dataGrid.SelectedItem;
            if (Connection.RemoveProjectFromQueue(machine, mq, hasMeasured))
            {
                System.Windows.MessageBox.Show(
                                            "Project has been removed from " + machine + " queue successfully!",
                                            "Q2C :: Information",
                                            (System.Windows.MessageBoxButton)System.Windows.Forms.MessageBoxButtons.OK,
                                            (System.Windows.MessageBoxImage)System.Windows.Forms.MessageBoxIcon.Information);

                Connection.ReadInfo();

                //_Projects
                DependencyObject _parent = VisualTreeHelper.GetParent(VisualTreeHelper.GetParent(VisualTreeHelper.GetParent(VisualTreeHelper.GetParent(VisualTreeHelper.GetParent(VisualTreeHelper.GetParent(VisualTreeHelper.GetParent(all_machines_grid)))))));
                if (_parent is TabControl _tabControl)
                {
                    TabItem _ti = (TabItem)_tabControl.Items[0];
                    UserControl _userControl = (UserControl)_ti.Content;
                    ((UCAllProjects)_userControl).Filter();
                    ((UCAllProjects)_userControl).UpdateProjectDataGrid();
                }
            }
        }
        #endregion

        #region DataGrid Evaluation
        private GroupBox CreateGroupBoxAvg(string machine)
        {
            return new System.Windows.Controls.GroupBox
            {
                Header = new TextBlock
                {
                    FontWeight = System.Windows.FontWeights.Bold,
                    Text = "Average"
                },
                Name = "GroupBox_Avg_" + machine,
                Margin = new Thickness(5, 5, 5, 0),
                Height = 50
            };
        }

        private void ApplyFilterFromCheckboxes(string machine, UIElement _element = null)
        {
            Model.Machine? _current_machine = Management.GetMachine(machine);
            if (_current_machine == null) return;

            HasFaimsEvaluation = _current_machine.HasFAIMS;
            HasOTEvaluation = _current_machine.HasOT;
            HasITEvaluation = _current_machine.HasIT;

            DependencyObject cb_parent = null;
            if (_element != null)
                cb_parent = VisualTreeHelper.GetParent(_element);
            else
            {
                cb_parent = (StackPanel)RetrieveElement("Evaluation", $"CheckBoxFAIMSAverage_{machine}", machine, all_machines_grid, 5);
                if (cb_parent == null)
                {
                    cb_parent = (StackPanel)RetrieveElement("Evaluation", $"CheckBoxOTAverage_{machine}", machine, all_machines_grid, 5);
                    if (cb_parent == null)
                        cb_parent = (StackPanel)RetrieveElement("Evaluation", $"CheckBoxITAverage_{machine}", machine, all_machines_grid, 5);
                }
            }
            if (cb_parent == null)
                return;

            System.Windows.Controls.CheckBox checkbox_faims = null;
            System.Windows.Controls.CheckBox checkbox_OT = null;
            System.Windows.Controls.CheckBox checkbox_IT = null;
            if (cb_parent is StackPanel stack)
            {
                foreach (UIElement element in stack.Children)
                {
                    if (element is CheckBox cb)
                    {
                        if (cb.Name.StartsWith("CheckBoxFAIMSAverage_"))
                        {
                            checkbox_faims = cb;
                        }
                        else if (cb.Name.StartsWith("CheckBoxOTAverage_"))
                        {
                            checkbox_OT = cb;
                        }
                        else if (cb.Name.StartsWith("CheckBoxITAverage_"))
                        {
                            checkbox_IT = cb;
                        }
                    }
                }
            }

            if (checkbox_faims != null)
                HasFaimsEvaluation = checkbox_faims.IsChecked == true;
            if (checkbox_OT != null)
                HasOTEvaluation = checkbox_OT.IsChecked == true;
            if (checkbox_IT != null)
                HasITEvaluation = checkbox_IT.IsChecked == true;

            if (!HasOTEvaluation && !HasITEvaluation)
            {
                System.Windows.MessageBox.Show(
                                       $"Netheir OT nor IT was checked.\nThe default result for {machine} is displayed.",
                                       "Q2C :: Warning",
                                       (System.Windows.MessageBoxButton)System.Windows.Forms.MessageBoxButtons.OK,
                                       (System.Windows.MessageBoxImage)System.Windows.Forms.MessageBoxIcon.Warning);

                if (checkbox_faims != null)
                    checkbox_faims.IsChecked = HasFaimsEvaluation = _current_machine.HasFAIMS;
                if (checkbox_OT != null)
                    checkbox_OT.IsChecked = HasOTEvaluation = _current_machine.HasOT;
                if (checkbox_IT != null)
                    checkbox_IT.IsChecked = HasITEvaluation = _current_machine.HasIT;
            }

            EnableOTITGroupBoxes(machine, HasOTEvaluation, HasITEvaluation);
            UpdateMachineDataGrid(machine, "Evaluation", HasFaimsEvaluation, HasOTEvaluation, HasITEvaluation);

            //UpdateWindow size
            if (MainWindow != null)
                UpdateDatagridHeight(MainWindow.ActualHeight - 240);
        }
        private void EnableOTITGroupBoxes(string machine, bool hasOT, bool hasIT)
        {
            foreach (TabItem tb_control_machine in all_machines_tab.Items)
            {
                if (!tb_control_machine.Header.ToString().Equals(machine)) continue;

                TabControl tb_machine_features = (TabControl)tb_control_machine.Content;
                foreach (TabItem tb_machine_feature in tb_machine_features.Items)
                {
                    if (!tb_machine_feature.Header.ToString().Equals("Evaluation")) continue;
                    Grid grid_evaluation = (Grid)tb_machine_feature.Content;
                    if (!grid_evaluation.Name.Replace("grid_evaluation_", "").Equals(machine))
                        return;
                    grid_evaluation.Children.RemoveAt(2);
                    ScrollViewer sv_run = CreateScrollViewerRun(machine, hasOT, hasIT);
                    Grid.SetRow(sv_run, 2);
                    grid_evaluation.Children.Add(sv_run);

                    return;
                }
            }
        }
        private void CheckBoxFAIMSAverage_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Controls.CheckBox checkbox_faims = (System.Windows.Controls.CheckBox)(sender);
            string machine = checkbox_faims.Name.Replace("CheckBoxFAIMSAverage_", "");

            ApplyFilterFromCheckboxes(machine, checkbox_faims);
        }
        private void CheckBoxOTAverage_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Controls.CheckBox checkbox_faims = (System.Windows.Controls.CheckBox)(sender);
            string machine = checkbox_faims.Name.Replace("CheckBoxOTAverage_", "");

            ApplyFilterFromCheckboxes(machine, checkbox_faims);
        }
        private void CheckBoxITAverage_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Controls.CheckBox checkbox_faims = (System.Windows.Controls.CheckBox)(sender);
            string machine = checkbox_faims.Name.Replace("CheckBoxITAverage_", "");

            ApplyFilterFromCheckboxes(machine, checkbox_faims);
        }

        private ScrollViewer CreateScrollViewerRun(string machine, bool hasOT, bool hasIT)
        {
            ScrollViewer sv = new ScrollViewer
            {
                Margin = new Thickness(0, -25, 5, 0),
                HorizontalScrollBarVisibility = ScrollBarVisibility.Visible,
                VerticalScrollBarVisibility = ScrollBarVisibility.Hidden
            };

            Grid grid = new Grid();
            grid.Name = "grid_both_ot_it_datagrids_" + machine;
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(650) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(95) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(195) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(65) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(85) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(112) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(97) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(97) });
            if ((hasOT && !hasIT) ||
                (!hasOT && hasIT))
            {
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(569) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            }
            else//Both OT & IT
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(569) });

            if (hasOT && hasIT)
            {
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(5) });//Separator
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(650) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(95) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(195) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(65) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(85) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(112) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(97) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(97) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(569) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            }
            int[] positions = null;
            if (hasOT && hasIT)
                positions = new int[16] { 1, 2, 3, 4, 5, 6, 7, 8, 12, 13, 14, 15, 16, 17, 18, 19 };
            else
                positions = new int[8] { 1, 2, 3, 4, 5, 6, 7, 8 };

            if (hasOT)
            {
                CreateTextBlock($"AvgMS1Intensity_OT_{machine}", positions[0], "????", grid, new Thickness(0));
                CreateTextBlock($"AvgMS2Intensity_OT_{machine}", positions[1], "????", grid, new Thickness(0));
                CreateTextBlock($"AvgProteinGroup_OT_{machine}", positions[2], "????", grid, new Thickness(0));
                CreateTextBlock($"AvgPeptideGroup_OT_{machine}", positions[3], "????", grid, new Thickness(0));
                CreateTextBlock($"AvgPSM_OT_{machine}", positions[4], "????", grid, new Thickness(0));
                CreateTextBlock($"AvgMSMS_OT_{machine}", positions[5], "????", grid, new Thickness(0));
                CreateTextBlock($"AvgIDRatio_OT_{machine}", positions[6], "????", grid, new Thickness(0));
                CreateTextBlock($"AvgXreaMean_OT_{machine}", positions[7], "????", grid, new Thickness(0));

                GroupBox groupBoxOT = CreateGroupBoxOTIT(machine, "OT");
                Grid.SetColumn(groupBoxOT, 0);
                if (hasIT)
                    Grid.SetColumnSpan(groupBoxOT, 10);
                else
                    Grid.SetColumnSpan(groupBoxOT, 11);
                grid.Children.Add(groupBoxOT);
            }
            if (hasIT)
            {
                CreateTextBlock($"AvgMS1Intensity_IT_{machine}", hasOT && hasIT ? positions[8] : positions[0], "????", grid, new Thickness(0));
                CreateTextBlock($"AvgMS2Intensity_IT_{machine}", hasOT && hasIT ? positions[9] : positions[1], "????", grid, new Thickness(0));
                CreateTextBlock($"AvgProteinGroup_IT_{machine}", hasOT && hasIT ? positions[10] : positions[2], "????", grid, new Thickness(0));
                CreateTextBlock($"AvgPeptideGroup_IT_{machine}", hasOT && hasIT ? positions[11] : positions[3], "????", grid, new Thickness(0));
                CreateTextBlock($"AvgPSM_IT_{machine}", hasOT && hasIT ? positions[12] : positions[4], "????", grid, new Thickness(0));
                CreateTextBlock($"AvgMSMS_IT_{machine}", hasOT && hasIT ? positions[13] : positions[5], "????", grid, new Thickness(0));
                CreateTextBlock($"AvgIDRatio_IT_{machine}", hasOT && hasIT ? positions[14] : positions[6], "????", grid, new Thickness(0));
                CreateTextBlock($"AvgXreaMean_IT_{machine}", hasOT && hasIT ? positions[15] : positions[7], "????", grid, new Thickness(0));

                GroupBox groupBoxIT = CreateGroupBoxOTIT(machine, "IT");
                Grid.SetColumn(groupBoxIT, hasOT ? 11 : 0);
                if (hasOT)
                    Grid.SetColumnSpan(groupBoxIT, 10);
                else
                    Grid.SetColumnSpan(groupBoxIT, 11);
                grid.Children.Add(groupBoxIT);
            }

            sv.Content = grid;
            return sv;
        }
        private void CreateTextBlock(string name, int gridColumn, string text, UIElement grid, Thickness margin, bool isBold = true, bool isVisible = true, HorizontalAlignment horizontalAlignment = HorizontalAlignment.Center)
        {
            TextBlock textBlock = new TextBlock();
            textBlock.Name = name;
            textBlock.Text = text;
            textBlock.HorizontalAlignment = horizontalAlignment;
            textBlock.Margin = margin;
            if (isBold)
                textBlock.FontWeight = FontWeights.Bold;
            else
                textBlock.FontWeight = FontWeights.Regular;

            if (isVisible)
                textBlock.Visibility = Visibility.Visible;
            else
                textBlock.Visibility = Visibility.Collapsed;

            if (grid is Grid)
            {
                Grid.SetColumn(textBlock, gridColumn);
                ((Grid)grid).Children.Add(textBlock);
            }
            else
                ((StackPanel)grid).Children.Add(textBlock);
        }
        private GroupBox CreateGroupBoxOTIT(string machine, string text)
        {
            GroupBox groupBox = new System.Windows.Controls.GroupBox
            {
                Name = $"GroupBox_{text}_{machine}",
                Header = new TextBlock
                {
                    FontWeight = System.Windows.FontWeights.Bold,
                    Text = text
                },
                Margin = new Thickness(0, 30, 0, 0)
            };

            DataGrid datagrid = CreateDataGridEvaluation(machine, text);
            groupBox.Content = datagrid;

            return groupBox;
        }

        private DataGrid CreateDataGridEvaluation(string machine, string ot_it)
        {
            // Create the DataGrid
            DataGrid dataGrid = new DataGrid
            {
                Name = $"DataGridMachines_Evaluation_{machine}_{ot_it}",
                IsReadOnly = false,
                CanUserAddRows = false,
                AutoGenerateColumns = false,
                AlternatingRowBackground = System.Windows.Media.Brushes.WhiteSmoke,
                VerticalScrollBarVisibility = ScrollBarVisibility.Visible,
                SelectionMode = DataGridSelectionMode.Single,
                SelectionUnit = DataGridSelectionUnit.FullRow,
                MinHeight = 210
            };
            // Create a style for column headers
            Style columnHeaderStyle = new Style(typeof(DataGridColumnHeader));
            columnHeaderStyle.Setters.Add(new Setter(HorizontalContentAlignmentProperty, HorizontalAlignment.Center));

            // Set the DataGrid's ColumnHeaderStyle
            dataGrid.ColumnHeaderStyle = columnHeaderStyle;

            DataGridTextColumn operatorColumn = CreateTextColumn("Operator", 155, 150, "Operator");
            DataGridTextColumn regitrationDateColumn = CreateTextColumn("Created", 155, 150, $"{ot_it}.RegistrationDateStr");
            DataGridTextColumn commentsColumn = CreateTextColumn("Comments", 0, 150, "Comments", 0, "", false);
            DataGridTextColumn lcColumn = CreateTextColumn("Column lot number", 155, 150, "ColumnLotNumber");
            DataGridTextColumn ms1IntensityColumn = CreateTextColumn("MS1 Intensity", 155, 150, $"{ot_it}.MS1Intensity", 0, "E2");
            DataGridTextColumn ms2IntensityColumn = CreateTextColumn("MS2 Intensity", 155, 150, $"{ot_it}.MS2Intensity", 0, "E2");
            DataGridTextColumn proteinGroupsColumn = CreateTextColumn("Protein Groups", 150, 100, $"{ot_it}.ProteinGroup");
            DataGridTextColumn peptideGroupsColumn = CreateTextColumn("Peptide Groups", 150, 100, $"{ot_it}.PeptideGroup");
            DataGridTextColumn psmsColumn = CreateTextColumn("PSMs", 150, 100, $"{ot_it}.PSM");
            DataGridTextColumn msmsColumn = CreateTextColumn("MS/MS", 150, 100, $"{ot_it}.MSMS");
            DataGridTextColumn iDRatioColumn = CreateTextColumn("ID Ratio (%)", 150, 100, $"{ot_it}.IDRatio", 0, "N2");
            DataGridTextColumn xReaColumn = CreateTextColumn("Xrea", 150, 100, $"{ot_it}.XreaMean", 0, "N2");
            DataGridTextColumn massErrorColumn = CreateTextColumn("Mass Error (ppm)", 150, 100, $"{ot_it}.MassError");
            DataGridTextColumn massErrorMedianColumn = CreateTextColumn("Median ppm", 150, 100, $"{ot_it}.MassErrorMedian");
            DataGridCheckBoxColumn excludeColumn = CreateCheckBoxColumn("Exclude", $"{ot_it}.Exclude");
            DataGridTextColumn RawFileColumn = CreateTextColumn("Raw File", 300, 274, $"{ot_it}.RawFile", 1);

            // Add the column to the DataGrid
            dataGrid.Columns.Add(operatorColumn);
            dataGrid.Columns.Add(regitrationDateColumn);
            dataGrid.Columns.Add(commentsColumn);
            dataGrid.Columns.Add(lcColumn);
            dataGrid.Columns.Add(ms1IntensityColumn);
            dataGrid.Columns.Add(ms2IntensityColumn);
            dataGrid.Columns.Add(proteinGroupsColumn);
            dataGrid.Columns.Add(peptideGroupsColumn);
            dataGrid.Columns.Add(psmsColumn);
            dataGrid.Columns.Add(msmsColumn);
            dataGrid.Columns.Add(iDRatioColumn);
            dataGrid.Columns.Add(xReaColumn);
            dataGrid.Columns.Add(massErrorColumn);
            dataGrid.Columns.Add(massErrorMedianColumn);
            dataGrid.Columns.Add(excludeColumn);
            dataGrid.Columns.Add(RawFileColumn);

            dataGrid.LoadingRow += DataGridMachines_LoadingRow;
            dataGrid.PreviewKeyDown += DataGridMachines_Evaluation_PreviewKeyDown;
            dataGrid.MouseDoubleClick += DataGridMachines_Evaluation_MouseDoubleClick;
            dataGrid.Loaded += DataGrid_Loaded;

            return dataGrid;
        }
        private void DataGrid_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is DataGrid dataGrid)
            {
                // Iterate through each row and set the EventSetter for SizeChanged
                foreach (var item in dataGrid.Items)
                {
                    var row = dataGrid.ItemContainerGenerator.ContainerFromItem(item) as DataGridRow;
                    if (row != null)
                    {
                        foreach (DataGridColumn column in dataGrid.Columns)
                        {
                            if (!(column.Header != null && column.Header.Equals("Comments"))) continue;

                            var cellContent = column.GetCellContent(item);
                            if (cellContent != null)
                            {
                                var cell = (DataGridCell)cellContent.Parent;
                                cell.Tag = dataGrid.Name;
                                SetSizeChangedEventHandler(cell);
                            }
                        }
                    }
                }
            }
        }
        private void SetSizeChangedEventHandler(DataGridCell cell)
        {
            var style = new Style(typeof(DataGridCell));
            style.Setters.Add(new EventSetter(FrameworkElement.SizeChangedEvent, new SizeChangedEventHandler(DataGridCell_SizeChanged)));
            cell.Style = style;
        }
        private double newWidth = -1;
        private double oldWidth = -1;
        private void DataGridCell_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (newWidth == e.NewSize.Width &&
                oldWidth == e.PreviousSize.Width)
                return;

            newWidth = e.NewSize.Width;
            oldWidth = e.PreviousSize.Width;

            if (newWidth != oldWidth)
            {
                string[] colsName = Regex.Split(((DataGridCell)sender).Tag.ToString(), "_");
                string machine_name = colsName[2];

                bool isOT = colsName[3].Equals("OT");
                DependencyObject cb_parent = (Grid)RetrieveElement("Evaluation", "", machine_name, all_machines_grid, 6);
                if (cb_parent is Grid grid)
                {
                    ColumnDefinition? current_cd = null;
                    if (isOT)
                        current_cd = grid.ColumnDefinitions[0];
                    else
                    {
                        int total_columns = grid.ColumnDefinitions.Count;
                        if (total_columns == 20)//There are two datagrids: OT & IT
                            current_cd = grid.ColumnDefinitions[10];
                        else //There is only IT
                            current_cd = grid.ColumnDefinitions[0];
                    }
                    double delta = current_cd.Width.Value + (newWidth - oldWidth);
                    delta = delta > 0 ? delta : current_cd.Width.Value;
                    current_cd.Width = new GridLength(delta);
                }
            }
        }
        private GroupBox CreateGroupBoxControlsRun(string machine, bool hasFaims, bool hasOT, bool hasIT)
        {
            GroupBox groupBox = new GroupBox();
            groupBox.Name = "groupbox_controls_run_" + machine;
            groupBox.Margin = new Thickness(5, 5, 5, 0);

            StackPanel stackPanel = new StackPanel();
            stackPanel.Orientation = Orientation.Horizontal;

            if (hasFaims)
            {
                // Create the CheckBox FAIMS
                CheckBox checkBox_faims = new CheckBox();
                checkBox_faims.Name = "CheckBoxFAIMSAverage_" + machine;
                checkBox_faims.Content = "FAIMS";
                checkBox_faims.FontWeight = FontWeights.Bold;
                checkBox_faims.Click += CheckBoxFAIMSAverage_Click;
                checkBox_faims.IsChecked = true;
                checkBox_faims.Margin = new Thickness(15, 12, 15, 0);

                stackPanel.Children.Add(checkBox_faims);
            }

            if (hasOT && hasIT)
            {
                // Create the CheckBox OT
                CheckBox checkBox_OT = new CheckBox();
                checkBox_OT.Name = "CheckBoxOTAverage_" + machine;
                checkBox_OT.Content = "OT";
                checkBox_OT.FontWeight = FontWeights.Bold;
                checkBox_OT.Click += CheckBoxOTAverage_Click;
                checkBox_OT.IsChecked = true;
                checkBox_OT.Margin = new Thickness(15, 12, 15, 0);

                stackPanel.Children.Add(checkBox_OT);

                // Create the CheckBox IT
                CheckBox checkBox_IT = new CheckBox();
                checkBox_IT.Name = "CheckBoxITAverage_" + machine;
                checkBox_IT.Content = "IT";
                checkBox_IT.FontWeight = FontWeights.Bold;
                checkBox_IT.Click += CheckBoxITAverage_Click;
                checkBox_IT.IsChecked = true;
                checkBox_IT.Margin = new Thickness(15, 12, 15, 0);

                stackPanel.Children.Add(checkBox_IT);
            }

            CreateTextBlock($"status_machineLabel{machine}", 0, "Status: ", stackPanel, new Thickness(10, 11, 5, 5), false, false, HorizontalAlignment.Left);
            CreateTextBlock($"status_machineValue{machine}", 0, "???", stackPanel, new Thickness(5, 11, 5, 5), true, false, HorizontalAlignment.Left);

            groupBox.Content = stackPanel;

            return groupBox;
        }
        private GroupBox CreateGroupBoxAddRun(string machine)
        {
            GroupBox groupBox = new GroupBox();
            groupBox.Margin = new Thickness(5, 5, 5, 0);

            Button button = new Button();
            button.Name = "ButtonEval_" + machine;
            button.Click += Button_AddUpdateRun;
            button.Padding = new Thickness(5, 1, 5, 1);
            button.Margin = new Thickness(5, 10, 5, 5);
            button.Height = 20;
            button.HorizontalAlignment = HorizontalAlignment.Left;
            button.Cursor = Cursors.Hand;

            // Create the StackPanel
            StackPanel stackPanel = new StackPanel();
            stackPanel.Orientation = Orientation.Horizontal;

            // Create the DockPanel
            DockPanel dockPanel = new DockPanel();

            // Create the Image
            Image image = new Image();
            image.Source = new BitmapImage(new Uri("/icons/add_icon.png", UriKind.Relative)); // Replace with the actual image path.

            // Create the TextBlock
            TextBlock textBlock = new TextBlock();
            textBlock.Margin = new Thickness(5, 0, 0, 0);
            textBlock.Width = 50;
            textBlock.Text = "Add Run";

            // Add the Image to the DockPanel
            DockPanel.SetDock(image, Dock.Left);
            dockPanel.Children.Add(image);

            // Add the DockPanel and TextBlock to the StackPanel
            stackPanel.Children.Add(dockPanel);
            stackPanel.Children.Add(textBlock);

            // Add the StackPanel to your parent container
            button.Content = stackPanel;

            groupBox.Content = button;

            return groupBox;
        }
        private void Button_AddUpdateRun(object sender, RoutedEventArgs e)
        {
            System.Windows.Controls.Button buttonConfirm = (System.Windows.Controls.Button)(sender);
            string machine = buttonConfirm.Name.Replace("ButtonEval_", "");
            AddUpdateRun(machine);
        }
        public void AddUpdateRun(string machine)
        {
            if (CurrentUser == null ||
              CurrentUser.Category == UserCategory.User ||
              CurrentUser.Category == UserCategory.SuperUsrSample ||
              CurrentUser.Category == UserCategory.MasterUsrSample)
            {

                System.Windows.MessageBox.Show(
                                        "The current user is not allowed to operate this feature.\nPlease contact the administrator.",
                                        "Q2C :: Warning",
                                        (System.Windows.MessageBoxButton)System.Windows.Forms.MessageBoxButtons.OK,
                                        (System.Windows.MessageBoxImage)System.Windows.Forms.MessageBoxIcon.Warning);
                return;
            }
            var w = new WindowRun();
            w.Load(this, machine);
            w.ShowDialog();
        }
        private void DataGridMachines_Evaluation_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key != Key.Delete)
                return;

            if (CurrentUser == null ||
              CurrentUser.Category == UserCategory.User ||
              CurrentUser.Category == UserCategory.UserSample ||
              CurrentUser.Category == UserCategory.SuperUsrSample ||
              CurrentUser.Category == UserCategory.MasterUsrSample)
            {

                System.Windows.MessageBox.Show(
                                        "The current user is not allowed to operate this feature.\nPlease contact the administrator.",
                                        "Q2C :: Warning",
                                        (System.Windows.MessageBoxButton)System.Windows.Forms.MessageBoxButtons.OK,
                                        (System.Windows.MessageBoxImage)System.Windows.Forms.MessageBoxIcon.Warning);
                e.Handled = true;
                return;
            }

            DataGrid current_datagrid = (DataGrid)sender;
            if (current_datagrid == null) return;
            if (current_datagrid.SelectedItem == CollectionView.NewItemPlaceholder) return;

            string machine = Regex.Split(current_datagrid.Name.Replace("DataGridMachines_Evaluation_", ""), "_")[0];

            string run = Util.Util.GetSelectedValue(current_datagrid, TagProperty, 1);
            var r = System.Windows.Forms.MessageBox.Show("Do you want to remove '" + run + "'?", "Q2C :: Warning", System.Windows.Forms.MessageBoxButtons.YesNo, System.Windows.Forms.MessageBoxIcon.Warning);
            if (r != System.Windows.Forms.DialogResult.Yes)
            {
                e.Handled = true;
                return;
            }
            RemoveRun(machine, current_datagrid);
        }

        private void RemoveRun(string machine, DataGrid datagrid)
        {
            Model.Run _run = (Q2C.Model.Run)datagrid.SelectedItem;
            if (Connection.RemoveRun(_run, machine))
            {
                System.Windows.MessageBox.Show(
                                            "Run has been removed successfully!",
                                            "Q2C :: Information",
                                            (System.Windows.MessageBoxButton)System.Windows.Forms.MessageBoxButtons.OK,
                                            (System.Windows.MessageBoxImage)System.Windows.Forms.MessageBoxIcon.Information);

                Connection.ReadInfo();
                UpdateMachineDataGridEvaluation(machine, datagrid);
            }
        }
        public void UpdateMachineDataGridEvaluation(string machine, DataGrid datagrid)
        {
            if (string.IsNullOrEmpty(machine) || datagrid == null) return;

            Model.Machine? _current_machine = Management.GetMachine(machine);
            if (_current_machine == null) return;

            HasFaimsEvaluation = _current_machine.HasFAIMS;
            HasOTEvaluation = _current_machine.HasOT;
            HasITEvaluation = _current_machine.HasIT;

            DataGrid _ot = null;
            DataGrid _it = null;
            if (datagrid.Name.Equals($"DataGridMachines_Evaluation_{machine}_OT"))
                _ot = datagrid;
            else if (datagrid.Name.Equals($"DataGridMachines_Evaluation_{machine}_IT"))
                _it = datagrid;

            if (HasOTEvaluation && HasITEvaluation)
            {
                if (_ot == null)
                {
                    //retrieve OT
                    DependencyObject datagrid_parent = VisualTreeHelper.GetParent(VisualTreeHelper.GetParent(VisualTreeHelper.GetParent(VisualTreeHelper.GetParent(datagrid))));
                    if (datagrid_parent is Grid grid_both_ot_it)
                    {
                        foreach (var item in grid_both_ot_it.Children)
                        {
                            if (item is GroupBox gb_ot_it)
                            {
                                string gb_name = Regex.Split(gb_ot_it.Name.Replace("GroupBox_", ""), "_")[0];
                                if (gb_name.Equals("OT"))
                                {
                                    _ot = (DataGrid)gb_ot_it.Content;
                                    break;
                                }
                            }
                        }
                    }
                }
                if (_it == null)
                {
                    //retrieve IT
                    DependencyObject datagrid_parent = VisualTreeHelper.GetParent(VisualTreeHelper.GetParent(VisualTreeHelper.GetParent(VisualTreeHelper.GetParent(datagrid))));
                    if (datagrid_parent is Grid grid_both_ot_it)
                    {
                        foreach (var item in grid_both_ot_it.Children)
                        {
                            if (item is GroupBox gb_ot_it)
                            {
                                string gb_name = Regex.Split(gb_ot_it.Name.Replace("GroupBox_", ""), "_")[0];
                                if (gb_name.Equals("IT"))
                                {
                                    _it = (DataGrid)gb_ot_it.Content;
                                    break;
                                }
                            }
                        }
                    }
                }
            }

            System.Windows.Controls.CheckBox checkbox_faims = null;
            System.Windows.Controls.CheckBox checkbox_OT = null;
            System.Windows.Controls.CheckBox checkbox_IT = null;

            DependencyObject cb_parent = VisualTreeHelper.GetParent(VisualTreeHelper.GetParent(VisualTreeHelper.GetParent(VisualTreeHelper.GetParent(VisualTreeHelper.GetParent(VisualTreeHelper.GetParent(VisualTreeHelper.GetParent(VisualTreeHelper.GetParent(datagrid))))))));
            if (cb_parent is Grid mainGrid)
            {
                StackPanel stack = (StackPanel)((GroupBox)((Grid)mainGrid.Children[0]).Children[1]).Content;
                foreach (UIElement element in stack.Children)
                {
                    if (element is CheckBox cb)
                    {
                        if (cb.Name.StartsWith("CheckBoxFAIMSAverage_"))
                        {
                            checkbox_faims = cb;
                        }
                        else if (cb.Name.StartsWith("CheckBoxOTAverage_"))
                        {
                            checkbox_OT = cb;
                        }
                        else if (cb.Name.StartsWith("CheckBoxITAverage_"))
                        {
                            checkbox_IT = cb;
                        }
                    }
                }
            }

            if (checkbox_faims != null)
                HasFaimsEvaluation = checkbox_faims.IsChecked == true;
            if (checkbox_OT != null)
                HasOTEvaluation = checkbox_OT.IsChecked == true;
            if (checkbox_IT != null)
                HasITEvaluation = checkbox_IT.IsChecked == true;

            UpdateMachineDataGrid(machine, "Evaluation", new DataGrid[2] { _ot, _it }, HasFaimsEvaluation, HasOTEvaluation, HasITEvaluation);
        }
        private DataGridCheckBoxColumn CreateCheckBoxColumn(string header, string binding)
        {
            DataGridCheckBoxColumn datagrid_checkboxColumn = new DataGridCheckBoxColumn
            {
                Header = header,
                Binding = new System.Windows.Data.Binding(binding) { Mode = BindingMode.TwoWay, UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged }
            };

            Style checkBoxStyle = new Style(typeof(DataGridCell));
            checkBoxStyle.BasedOn = (Style)Application.Current.Resources["DataGridCell"];
            checkBoxStyle.Setters.Add(new EventSetter(CheckBox.MouseLeaveEvent, new MouseEventHandler(Exclude_Run_Evaluation_MouseLeave)));
            checkBoxStyle.Setters.Add(new EventSetter(CheckBox.MouseEnterEvent, new MouseEventHandler(Exclude_Run_Evaluation_MouseEnter)));
            checkBoxStyle.Setters.Add(new EventSetter(CheckBox.CheckedEvent, new RoutedEventHandler(Exclude_Run_Evaluation_OnChecked)));
            checkBoxStyle.Setters.Add(new EventSetter(CheckBox.UncheckedEvent, new RoutedEventHandler(Exclude_Run_Evaluation_OnChecked)));
            datagrid_checkboxColumn.CellStyle = checkBoxStyle;

            return datagrid_checkboxColumn;
        }
        private DataGrid GetDataGridFromCell(System.Windows.Controls.DataGridCell datagrid_cell)
        {
            DependencyObject dep = datagrid_cell;
            while (dep != null && !(dep is DataGrid))
            {
                dep = VisualTreeHelper.GetParent(dep);
            }
            return dep as DataGrid;
        }
        private void Exclude_Run_Evaluation_MouseLeave(object sender, MouseEventArgs e)
        {
            ((CheckBox)((DataGridCell)e.OriginalSource).Content).Tag = "";
        }
        private void Exclude_Run_Evaluation_MouseEnter(object sender, MouseEventArgs e)
        {
            ((CheckBox)((DataGridCell)e.OriginalSource).Content).Tag = "clicked";
        }
        private void Exclude_Run_Evaluation_OnChecked(object sender, RoutedEventArgs e)
        {
            if (((CheckBox)e.OriginalSource).Tag == null ||
                String.IsNullOrEmpty(((CheckBox)e.OriginalSource).Tag.ToString())) return;

            System.Windows.Controls.DataGridCell datagrid_cell = (System.Windows.Controls.DataGridCell)(sender);
            if (datagrid_cell.IsSelected == true)
            {
                DataGrid dg = GetDataGridFromCell(datagrid_cell);
                if (dg == null) return;

                string[] _data = Regex.Split(dg.Name.Replace("DataGridMachines_Evaluation_", ""), "_");
                string machine = _data[0];//machine name
                string property = _data[1];//OT or IT

                bool _is_checked = ((CheckBox)e.OriginalSource).IsChecked == true;
                ((CheckBox)e.OriginalSource).Tag = "";

                ExcludeRunOnChecked(machine, property, dg, _is_checked);
            }
        }
        private void ExcludeRunOnChecked(string machine, string property, DataGrid datagrid, bool _is_checked)
        {
            Model.Run current_run = (Q2C.Model.Run)datagrid.SelectedItem;
            if (current_run == null) return;

            if (current_run != null)
            {
                if (property == "OT")
                    current_run.OT.Exclude = _is_checked;
                else if (property == "IT")
                    current_run.IT.Exclude = _is_checked;

                Connection.AddOrUpdateRun(current_run, machine);

                System.Windows.MessageBox.Show(
                            "Run has been updated successfully!",
                            "Q2C :: Information",
                            (System.Windows.MessageBoxButton)System.Windows.Forms.MessageBoxButtons.OK,
                            (System.Windows.MessageBoxImage)System.Windows.Forms.MessageBoxIcon.Information);

                Connection.ReadInfo();
                UpdateMachineDataGridEvaluation(machine, datagrid);
            }
        }
        private void DataGridMachines_Evaluation_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (CurrentUser == null ||
              CurrentUser.Category == UserCategory.User ||
              CurrentUser.Category == UserCategory.SuperUsrSample ||
              CurrentUser.Category == UserCategory.MasterUsrSample)
            {

                System.Windows.MessageBox.Show(
                                        "The current user is not allowed to operate this feature.\nPlease contact the administrator.",
                                        "Q2C :: Warning",
                                        (System.Windows.MessageBoxButton)System.Windows.Forms.MessageBoxButtons.OK,
                                        (System.Windows.MessageBoxImage)System.Windows.Forms.MessageBoxIcon.Warning);
                return;
            }

            if (((DataGrid)sender).CurrentColumn == null) return;

            //'Exclude' column (13) does not do anything
            if (((DataGrid)sender).CurrentColumn.DisplayIndex == 14) return;

            DataGrid current_datagrid = (DataGrid)sender;
            if (current_datagrid == null) return;
            if (current_datagrid.SelectedItem == CollectionView.NewItemPlaceholder) return;

            string[] data_grid_name = Regex.Split(current_datagrid.Name.Replace("DataGridMachines_Evaluation_", ""), "_");
            string machine = data_grid_name[0];
            bool isOT = data_grid_name[data_grid_name.Length - 1] == "OT";

            Model.Run current_run = (Q2C.Model.Run)current_datagrid.SelectedItem;
            if (current_run == null) return;

            List<XreaEntry> xreas = isOT ? current_run.OT.Xreas : current_run.IT.Xreas;
            if (((DataGrid)sender).CurrentColumn.DisplayIndex == 11 && xreas != null && xreas.Count > 0)//Show Xrea
            {
                var w = new WindowXrea();
                w.Load(xreas);
                w.ShowDialog();
            }
            else
            {
                var w = new WindowRun();
                w.Load(this, machine, true, current_run, current_datagrid);
                w.ShowDialog();
            }
        }

        public void EnableorDisableCalibration(Dictionary<string, (bool calibration, bool full_calibration)> machines_to_calibrate)
        {
            if (machines_to_calibrate.Count == 0) return;

            foreach (var machine in machines_to_calibrate)
            {
                TextBlock calibrationText = (TextBlock)RetrieveElement("Evaluation", $"status_machineLabel{machine.Key}", machine.Key, all_machines_grid, 4);
                TextBlock calibrationStatusText = (TextBlock)RetrieveElement("Evaluation", $"status_machineValue{machine.Key}", machine.Key, all_machines_grid, 4);
                if (calibrationText == null ||
                    calibrationStatusText == null)
                    continue;

                if (machine.Value.full_calibration == true)
                {
                    calibrationText.Visibility = Visibility.Visible;
                    calibrationStatusText.Visibility = Visibility.Visible;
                    calibrationStatusText.Text = "Machine may need to be fully calibrated. Check 'Log' tab.";
                }
                else if (machine.Value.calibration == true)
                {
                    calibrationText.Visibility = Visibility.Visible;
                    calibrationStatusText.Visibility = Visibility.Visible;
                    calibrationStatusText.Text = "Machine may need to be calibrated. Check 'Log' tab.";
                }
                else
                {
                    calibrationText.Visibility = Visibility.Collapsed;
                    calibrationStatusText.Visibility = Visibility.Collapsed;
                    calibrationStatusText.Text = "";
                }
            }
        }
        #endregion

        #region DataGrid Log
        private GroupBox CreateGroupBoxLog(string machine)
        {
            GroupBox groupBox = new GroupBox();
            groupBox.Margin = new Thickness(0, 0, 0, 10);

            Button button = new Button();
            button.Name = "ButtonLog_" + machine;
            button.Click += Button_AddUpdateLog;
            button.Padding = new Thickness(5, 1, 5, 1);
            button.Margin = new Thickness(5, 10, 5, 5);
            button.Height = 20;
            button.HorizontalAlignment = HorizontalAlignment.Left;
            button.Cursor = Cursors.Hand;

            // Create the StackPanel
            StackPanel stackPanel = new StackPanel();
            stackPanel.Orientation = Orientation.Horizontal;

            // Create the DockPanel
            DockPanel dockPanel = new DockPanel();

            // Create the Image
            Image image = new Image();
            image.Source = new BitmapImage(new Uri("/icons/add_icon.png", UriKind.Relative)); // Replace with the actual image path.

            // Create the TextBlock
            TextBlock textBlock = new TextBlock();
            textBlock.Margin = new Thickness(5, 0, 0, 0);
            textBlock.Width = 50;
            textBlock.Text = "Add Log";

            // Add the Image to the DockPanel
            DockPanel.SetDock(image, Dock.Left);
            dockPanel.Children.Add(image);

            // Add the DockPanel and TextBlock to the StackPanel
            stackPanel.Children.Add(dockPanel);
            stackPanel.Children.Add(textBlock);

            // Add the StackPanel to your parent container
            button.Content = stackPanel;

            groupBox.Content = button;

            return groupBox;
        }
        private DataGrid CreateDataGridLog(string machine)
        {
            // Create the DataGrid
            DataGrid dataGrid = new DataGrid
            {
                Name = $"DataGridMachines_Log_{machine}",
                AutoGenerateColumns = false,
                AlternatingRowBackground = System.Windows.Media.Brushes.WhiteSmoke,
                VerticalScrollBarVisibility = ScrollBarVisibility.Visible,
                SelectionMode = DataGridSelectionMode.Single,
                MinHeight = 310
            };

            // Create a column for "Registration Date"
            DataGridTextColumn registrationDateColumn = CreateTextColumn("Registration Date", 155, 150, "RegistrationDateStr");
            DataGridTextColumn operatorColumn = CreateTextColumn("Operator", 150, 100, "Operator");
            DataGridTextColumn isCalibratedColumn = CreateTextColumn("Mass Calibration", 150, 100, "_isCalibrated");
            DataGridTextColumn isFullCalibratedColumn = CreateTextColumn("Full Calibration", 150, 100, "_isFullyCalibrated");
            DataGridTextColumn lotNumberColumn = CreateTextColumn("Column lot number", 150, 120, "ColumnLotNumber");
            DataGridTextColumn lcColumn = CreateTextColumn("LC pressure (B 4%, 50 °C, 0.25 µL/min)", 250, 215, "LC");
            DataGridTextColumn technicalReportColumn = CreateTextColumn("Technical Report", 400, 200, "_TechnicalReportFileName");
            DataGridTextColumn remarksColumn = CreateTextColumn("Remarks", 2560, 405, "Remarks");

            // Add the column to the DataGrid
            dataGrid.Columns.Add(registrationDateColumn);
            dataGrid.Columns.Add(operatorColumn);
            dataGrid.Columns.Add(isCalibratedColumn);
            dataGrid.Columns.Add(isFullCalibratedColumn);
            dataGrid.Columns.Add(lotNumberColumn);
            dataGrid.Columns.Add(lcColumn);
            dataGrid.Columns.Add(technicalReportColumn);
            dataGrid.Columns.Add(remarksColumn);
            dataGrid.LoadingRow += DataGridMachines_LoadingRow;
            dataGrid.PreviewKeyDown += DataGridMachines_Log_PreviewKeyDown;
            dataGrid.MouseDoubleClick += DataGridMachines_Log_MouseDoubleClick;

            // Create a style for column headers
            Style columnHeaderStyle = new Style(typeof(DataGridColumnHeader));
            columnHeaderStyle.Setters.Add(new Setter(HorizontalContentAlignmentProperty, HorizontalAlignment.Center));

            // Set the DataGrid's ColumnHeaderStyle
            dataGrid.ColumnHeaderStyle = columnHeaderStyle;

            return dataGrid;
        }
        private void DataGridMachines_Log_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key != Key.Delete)
                return;

            if (CurrentUser == null ||
              CurrentUser.Category == UserCategory.User ||
              CurrentUser.Category == UserCategory.UserSample ||
              CurrentUser.Category == UserCategory.SuperUsrSample ||
              CurrentUser.Category == UserCategory.MasterUsrSample)
            {

                System.Windows.MessageBox.Show(
                                        "The current user is not allowed to operate this feature.\nPlease contact the administrator.",
                                        "Q2C :: Warning",
                                        (System.Windows.MessageBoxButton)System.Windows.Forms.MessageBoxButtons.OK,
                                        (System.Windows.MessageBoxImage)System.Windows.Forms.MessageBoxIcon.Warning);
                e.Handled = true;
                return;
            }

            DataGrid current_datagrid = (DataGrid)sender;
            if (current_datagrid == null) return;
            if (current_datagrid.SelectedItem == CollectionView.NewItemPlaceholder) return;

            string machine = current_datagrid.Name.Replace("DataGridMachines_Log_", "");

            string log = Util.Util.GetSelectedValue(current_datagrid, TagProperty);
            var r = System.Windows.Forms.MessageBox.Show("Do you want to remove '" + log + "' register?", "Q2C :: Warning", System.Windows.Forms.MessageBoxButtons.YesNo, System.Windows.Forms.MessageBoxIcon.Warning);
            if (r != System.Windows.Forms.DialogResult.Yes)
            {
                e.Handled = true;
                return;
            }
            RemoveLog(machine, current_datagrid);
        }
        private void RemoveLog(string machine, DataGrid datagrid)
        {
            Q2C.Model.MachineLog _log = (Q2C.Model.MachineLog)datagrid.SelectedItem;
            if (Connection.RemoveLog(_log, machine))
            {
                System.Windows.MessageBox.Show(
                                            "Log has been removed successfully!",
                                            "Q2C :: Information",
                                            (System.Windows.MessageBoxButton)System.Windows.Forms.MessageBoxButtons.OK,
                                            (System.Windows.MessageBoxImage)System.Windows.Forms.MessageBoxIcon.Information);

                Connection.ReadInfo();
            }
        }
        private async void DataGridMachines_Log_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            DataGrid current_datagrid = (DataGrid)sender;
            if (current_datagrid == null) return;
            if (current_datagrid.SelectedItem == CollectionView.NewItemPlaceholder) return;

            string machine = current_datagrid.Name.Replace("DataGridMachines_Log_", "");

            var element = e.OriginalSource as FrameworkElement;
            if (element == null) return;
            var cell = element.Parent as DataGridCell;
            if (cell == null)
            {
                cell = element.TemplatedParent as DataGridCell;
                if (cell == null) return;
            }
            int columnIndex = cell.Column.DisplayIndex;

            Q2C.Model.MachineLog _log = (Q2C.Model.MachineLog)current_datagrid.SelectedItem;
            if (_log == null)
            {
                System.Windows.MessageBox.Show(
                                            "Log has not been found!",
                                            "Q2C :: Warning",
                                            (System.Windows.MessageBoxButton)System.Windows.Forms.MessageBoxButtons.OK,
                                            (System.Windows.MessageBoxImage)System.Windows.Forms.MessageBoxIcon.Warning);
                return;
            }

            bool openLog = true;
            if (columnIndex == 6)//Open technical report file
            {
                openLog = false;
                if (!String.IsNullOrEmpty(_log.TechnicalReportFile_GoogleID))
                {
                    UCWaitScreen waitScreen = new UCWaitScreen("Please Wait...", "Downloading technical report file...");
                    Grid.SetRow(waitScreen, 0);
                    Grid.SetRowSpan(waitScreen, 4);
                    waitScreen.Margin = new Thickness(0, 0, 0, -8);
                    MainGrid.Children.Add(waitScreen);

                    await Task.Run(() => Management.OpenTechnicalReportFile(_log._TechnicalReportGoogleId, _log._TechnicalReportFileName));

                    MainGrid.Children.Remove(waitScreen);
                }
                else
                    openLog = true;
            }

            if (openLog == true)
                AddUpdateLog(machine, _log);
        }
        public void AddUpdateLog(string machine, MachineLog _log = null)
        {
            if (CurrentUser == null ||
              CurrentUser.Category == UserCategory.User ||
              CurrentUser.Category == UserCategory.SuperUsrSample ||
              CurrentUser.Category == UserCategory.MasterUsrSample)
            {

                System.Windows.MessageBox.Show(
                                        "The current user is not allowed to operate this feature.\nPlease contact the administrator.",
                                        "Q2C :: Warning",
                                        (System.Windows.MessageBoxButton)System.Windows.Forms.MessageBoxButtons.OK,
                                        (System.Windows.MessageBoxImage)System.Windows.Forms.MessageBoxIcon.Warning);
                return;
            }
            var w = new WindowLog();
            if (_log == null)
                w.Load(this, machine);
            else
                w.Load(this, machine, true, _log);
            w.ShowDialog();
        }
        private void Button_AddUpdateLog(object sender, RoutedEventArgs e)
        {
            System.Windows.Controls.Button buttonConfirm = (System.Windows.Controls.Button)(sender);
            string machine = buttonConfirm.Name.Replace("ButtonLog_", "");

            AddUpdateLog(machine);
        }
        #endregion
    }

    public class FileNameConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string filePath)
            {
                return System.IO.Path.GetFileName(filePath);
            }

            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
