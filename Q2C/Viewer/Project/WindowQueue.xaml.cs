using Q2C.Control.Database;
using Q2C.Model;
using Q2C.Viewer.Machine;
using Q2C.Viewer.Project;
using SearchEngineGUI;
using System;
using System.Collections.Generic;
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
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using UserControl = System.Windows.Controls.UserControl;

namespace Q2C.Viewer.Project
{
    /// <summary>
    /// Interaction logic for WindowQueue.xaml
    /// </summary>
    public partial class WindowQueue : Window
    {
        private UserControl _userControl_projects { get; set; }
        private UserControl _userControl_machines { get; set; }
        private Q2C.Model.Project _project { get; set; }
        private string[] _machines { get; set; }
        public WindowQueue()
        {
            InitializeComponent();
        }

        public void Load(UserControl userControl_projects,
            UserControl userControl_machines,
            string[] machines,
            Q2C.Model.Project project)
        {
            _userControl_projects = userControl_projects;
            _userControl_machines = userControl_machines;
            _project = project;
            _machines = machines;

            Grid newGrid = new Grid();
            ColumnDefinition column1 = new();
            ColumnDefinition column2 = new();
            ColumnDefinition column3 = new();
            ColumnDefinition column4 = new();

            newGrid.ColumnDefinitions.Add(column1);
            newGrid.ColumnDefinitions.Add(column2);
            newGrid.ColumnDefinitions.Add(column3);
            newGrid.ColumnDefinitions.Add(column4);

            for (int i = 0; i < machines.Length; i++)
            {
                string item = machines[i];
                System.Windows.Controls.RadioButton rbMachines = new System.Windows.Controls.RadioButton();
                rbMachines.Name = "rbMachine_" + item;
                rbMachines.Content = item;
                rbMachines.Margin = new Thickness(5, 5, 5, 5);

                RowDefinition rowDef = new();
                newGrid.RowDefinitions.Add(rowDef);

                Grid.SetRow(rbMachines, 0);
                Grid.SetColumn(rbMachines, i);

                newGrid.Children.Add(rbMachines);
            }
            QueueGrid.Children.Add(newGrid);
        }

        private void ButtonConfirm_Click(object sender, RoutedEventArgs e)
        {
            if (_project == null) return;

            string machine = "";
            if (QueueGrid.Children[0] is Grid innerGrid)
            {
                foreach (UIElement child in innerGrid.Children)
                {
                    if (child is System.Windows.Controls.RadioButton radioButton)
                    {
                        string name = radioButton.Name.Replace("rbMachine_", "");
                        if (radioButton.IsChecked == true)
                        {
                            machine = name;
                            break;
                        }
                    }
                }
            }

            int machine_index = _project.GetMachines.ToList().FindIndex(a => a == machine);
            if (machine_index == -1)
            {
                System.Windows.MessageBox.Show(
                                    "Error to figure out " + machine + "!",
                                    "Q2C :: Error",
                                    (System.Windows.MessageBoxButton)MessageBoxButtons.OK,
                                    (System.Windows.MessageBoxImage)MessageBoxIcon.Error);
                return;
            }

            _project.Machine = machine;
            string[] cols_faims = Regex.Split(_project._faims, "/");
            _project.FAIMS = Q2C.Model.Project.GetFAIMS(cols_faims[machine_index]);

            if (_project.FAIMS[0] == ProjectFAIMS.IdontMind)
            {
                var r = System.Windows.Forms.MessageBox.Show("Will the sample(s) be measured by using FAIMS?", "Q2C :: Warning", System.Windows.Forms.MessageBoxButtons.YesNo, System.Windows.Forms.MessageBoxIcon.Warning);
                if (r == System.Windows.Forms.DialogResult.Yes)
                    _project.FAIMS[0] = ProjectFAIMS.Yes;
                else
                    _project.FAIMS[0] = ProjectFAIMS.No;
            }

            if (Connection.AddProjectToQueue(machine, _project))
            {
                System.Windows.MessageBox.Show(
                                    "Sample has been added to " + machine + " queue successfully!",
                                    "Q2C :: Information",
                                    (System.Windows.MessageBoxButton)MessageBoxButtons.OK,
                                    (System.Windows.MessageBoxImage)MessageBoxIcon.Information);
                Connection.ReadInfo();
                ((UCAllProjects)_userControl_projects).UpdateProjectDataGrid();
                ((UCAllMachines)_userControl_machines).UpdateMachineDataGrid(machine, "Queue");

                this.Close();
            }
        }
    }
}
