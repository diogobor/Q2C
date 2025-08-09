using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
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

namespace Q2C.Viewer.Setup
{
    /// <summary>
    /// Interaction logic for WindowMachine.xaml
    /// </summary>
    public partial class WindowMachine : Window
    {
        private Window _window;
        private bool _isChanged { get; set; } = false;
        public WindowMachine()
        {
            InitializeComponent();
            this.Closing += WindowMachine_Closing;
        }

        private void WindowMachine_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            if (_isChanged)
            {
                System.Windows.MessageBox.Show(
                               "Q2C must be restarted due to the changes.",
                               "Q2C :: Warning",
                               (System.Windows.MessageBoxButton)MessageBoxButtons.OK,
                               (System.Windows.MessageBoxImage)MessageBoxIcon.Warning);

                System.Windows.Forms.Application.Restart();
                System.Windows.Application.Current.Shutdown();
            }
        }

        public void Load(Window window, bool isUpdate = false, Q2C.Model.Machine machine = null)
        {
            _isChanged = false;
            _window = window;
            if (isUpdate)
                this.Title = "Q2C :: Edit Machine";
            else
                this.Title = "Q2C :: Add Machine";
            _Machine.Load(this, machine);
        }

        public void UpdateMachinesDataGrid()
        {
            _isChanged = true;
            ((WindowMachines)_window).UpdateMachinesDataGrid();
        }
    }
}
