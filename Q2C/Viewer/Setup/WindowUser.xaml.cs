using Microsoft.VisualBasic.ApplicationServices;
using Q2C.Model;
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
using User = Q2C.Model.User;

namespace Q2C.Viewer.Setup
{
    /// <summary>
    /// Interaction logic for WindowUser.xaml
    /// </summary>
    public partial class WindowUser : Window
    {
        private Window _window;
        private bool _isChanged { get; set; } = false;
        public WindowUser()
        {
            InitializeComponent();
            this.Closing += WindowUser_Closing;
        }

        private void WindowUser_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
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

        public void Load(Window window, bool isUpdate = false, User user = null)
        {
            _window = window;
            if (isUpdate)
                this.Title = "Q2C :: Edit User";
            else
                this.Title = "Q2C :: Add User";

            _User.Load(this, user);
        }

        public void UpdateUsersDataGrid()
        {
            _isChanged = true;
            ((WindowUsers)_window).UpdateUserDataGrid();
        }
    }
}
