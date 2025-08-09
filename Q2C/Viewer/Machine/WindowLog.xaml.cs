using Q2C.Control.Database;
using Q2C.Model;
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
using System.Windows.Shapes;

namespace Q2C.Viewer.Machine
{
    /// <summary>
    /// Interaction logic for WindowLog.xaml
    /// </summary>
    public partial class WindowLog : Window
    {
        private UserControl _userControl;
        private string _machine { get; set; }
        public WindowLog()
        {
            InitializeComponent();
        }
        public void Load(UserControl userControl, string machine, bool isUpdate = false, Model.MachineLog log = null)
        {
            Connection.Refresh_time = int.MinValue;
            _userControl = userControl;
            _machine = machine;

            if (isUpdate)
                this.Title = "Q2C :: Edit Log";
            else
                this.Title = "Q2C :: Add Log";

            _Log.Load(this, log, machine);
        }

        public void UpdateLogDataGrid()
        {
            ((UCAllMachines)_userControl).UpdateMachineDataGrid(_machine, "Log");
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Connection.Refresh_time = 0;
        }
    }
}
