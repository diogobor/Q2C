using Q2C.Control;
using Q2C.Control.Database;
using Q2C.Viewer.Machine;
using System;
using System.Collections.Generic;
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
using System.Windows.Shapes;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;

namespace Q2C.Viewer.Setup
{
    /// <summary>
    /// Interaction logic for WindowMachines.xaml
    /// </summary>
    public partial class WindowMachines : Window
    {
        public WindowMachines()
        {
            InitializeComponent();
            UpdateMachinesDataGrid();
        }

        public void UpdateMachinesDataGrid()
        {
            if (Management.Machines != null)
            {
                DataGridMachines.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(delegate ()
                {
                    DataGridMachines.ItemsSource = null;
                    List<Q2C.Model.Machine> _machines = Management.GetMachines();
                    DataGridMachines.ItemsSource = _machines;
                }));
            }
        }

        private void Button_AddMachine(object sender, RoutedEventArgs e)
        {
            var window = new WindowMachine();
            window.Load(this);
            window.ShowDialog();
        }

        private void DataGridMachines_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            e.Row.Header = (e.Row.GetIndex() + 1).ToString();
        }

        private void DataGridMachines_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Delete)
                return;

            string machine = Util.Util.GetSelectedValue(DataGridMachines, TagProperty, 1);
            var r = System.Windows.Forms.MessageBox.Show("Do you want to remove '" + machine + "'?", "Q2C :: Warning", System.Windows.Forms.MessageBoxButtons.YesNo, System.Windows.Forms.MessageBoxIcon.Warning);
            if (r != System.Windows.Forms.DialogResult.Yes)
            {
                e.Handled = true;
                return;
            }
            RemoveMachine();
        }

        private void RemoveMachine()
        {
            Q2C.Model.Machine _machine = (Q2C.Model.Machine)DataGridMachines.SelectedItem;
            if (Connection.RemoveMachine(_machine))
            {
                System.Windows.MessageBox.Show(
                                            "Machine has been removed successfully!\nQ2C will be restarted!",
                                            "Q2C :: Information",
                                            (System.Windows.MessageBoxButton)MessageBoxButtons.OK,
                                            (System.Windows.MessageBoxImage)MessageBoxIcon.Information);

                System.Windows.Forms.Application.Restart();
                System.Windows.Application.Current.Shutdown();
            }
        }

        private void DataGridMachines_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            string machineName = Util.Util.GetSelectedValue(DataGridMachines, TagProperty, 1);
            bool hasEvaluation = Convert.ToBoolean(Util.Util.GetSelectedValue(DataGridMachines, TagProperty, 2));
            bool hasFaims = Convert.ToBoolean(Util.Util.GetSelectedValue(DataGridMachines, TagProperty, 3));
            bool hasOT = Convert.ToBoolean(Util.Util.GetSelectedValue(DataGridMachines, TagProperty, 4));
            bool hasIT = Convert.ToBoolean(Util.Util.GetSelectedValue(DataGridMachines, TagProperty, 5));

            Q2C.Model.Machine? _machine = Management.GetMachines().Where(a => a.Name.Equals(machineName) &&
                a.HasEvaluation == hasEvaluation &&
                a.HasFAIMS == hasFaims &&
                a.HasOT == hasOT &&
                a.HasIT == hasIT).FirstOrDefault();
            if (_machine == null)
            {
                System.Windows.MessageBox.Show(
                                            "Machine has not been found!",
                                            "Q2C :: Warning",
                                            (System.Windows.MessageBoxButton)MessageBoxButtons.OK,
                                            (System.Windows.MessageBoxImage)MessageBoxIcon.Warning);
                return;
            }

            var w = new WindowMachine();
            w.Load(this, true, _machine);
            w.ShowDialog();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Connection.Refresh_time = 0;
        }
    }
}
