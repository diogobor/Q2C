using Q2C.Control.Database;
using Q2C.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Q2C.Viewer.Machine
{
    /// <summary>
    /// Interaction logic for WindowRun.xaml
    /// </summary>
    public partial class WindowRun : Window
    {
        private UserControl _userControl;
        private DataGrid _grid;
        private string _machine { get; set; }
        public WindowRun()
        {
            InitializeComponent();
        }

        public void Load(UserControl userControl, string machine, bool isUpdate = false, Model.Run run = null, DataGrid grid = null)
        {
            Connection.Refresh_time = int.MinValue;
            _userControl = userControl;
            _grid = grid;
            _machine = machine;

            if (isUpdate)
                this.Title = "Q2C :: Edit Run";
            else
                this.Title = "Q2C :: Add Run";

            _Run.Load(this, run, machine);
        }

        public void UpdateRunDataGrid()
        {
            if (_grid == null)
            {
                ((UCAllMachines)_userControl).UpdateMachineDataGrid(_machine, "Evaluation", UCAllMachines.HasFaimsEvaluation, UCAllMachines.HasOTEvaluation, UCAllMachines.HasITEvaluation);
                return;
            }

            ((UCAllMachines)_userControl).UpdateMachineDataGridEvaluation(_machine, _grid);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Connection.Refresh_time = 0;
        }
    }
}
