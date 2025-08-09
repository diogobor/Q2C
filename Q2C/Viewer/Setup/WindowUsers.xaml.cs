using Q2C.Control;
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
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;

namespace Q2C.Viewer.Setup
{
    /// <summary>
    /// Interaction logic for WindowUsers.xaml
    /// </summary>
    public partial class WindowUsers : Window
    {
        public WindowUsers()
        {
            InitializeComponent();
            UpdateUserDataGrid();
        }

        public void UpdateUserDataGrid()
        {
            if (Management.Users != null)
            {
                DataGridUsers.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(delegate ()
                {
                    DataGridUsers.ItemsSource = null;
                    List<User> _users = Management.GetUsers();
                    DataGridUsers.ItemsSource = _users;
                }));
            }
        }

        private void Button_AddUser(object sender, RoutedEventArgs e)
        {
            var window = new WindowUser();
            window.Load(this);
            window.ShowDialog();
        }

        private void DataGridUsers_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            e.Row.Header = (e.Row.GetIndex() + 1).ToString();
        }

        private void DataGridUsers_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Delete)
                return;

            string user = Util.Util.GetSelectedValue(DataGridUsers, TagProperty, 1);
            var r = System.Windows.Forms.MessageBox.Show("Do you want to remove '" + user + "'?", "Q2C :: Warning", System.Windows.Forms.MessageBoxButtons.YesNo, System.Windows.Forms.MessageBoxIcon.Warning);
            if (r != System.Windows.Forms.DialogResult.Yes)
            {
                e.Handled = true;
                return;
            }
            RemoveUser();
        }

        private void RemoveUser()
        {
            User _user = (User)DataGridUsers.SelectedItem;
            if (Connection.RemoveUser(_user))
            {
                System.Windows.MessageBox.Show(
                                            "User has been removed successfully!",
                                            "Q2C :: Information",
                                            (System.Windows.MessageBoxButton)MessageBoxButtons.OK,
                                            (System.Windows.MessageBoxImage)MessageBoxIcon.Information);

                Connection.ReadInfo(true);
                UpdateUserDataGrid();
            }
        }

        private void DataGridUsers_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            string userName = Util.Util.GetSelectedValue(DataGridUsers, TagProperty, 1);
            string category = Util.Util.GetSelectedValue(DataGridUsers, TagProperty, 2);
            string email = Util.Util.GetSelectedValue(DataGridUsers, TagProperty, 3);

            User? _user = Management.GetUsers().Where(a => a.Name.Equals(userName) &&
                a.Email.Equals(email) &&
                a.Category == User.GetCategory(category)).FirstOrDefault();
            if (_user == null)
            {
                System.Windows.MessageBox.Show(
                                            "User has not been found!",
                                            "Q2C :: Warning",
                                            (System.Windows.MessageBoxButton)MessageBoxButtons.OK,
                                            (System.Windows.MessageBoxImage)MessageBoxIcon.Warning);
                return;
            }

            var w = new WindowUser();
            w.Load(this, true, _user);
            w.ShowDialog();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Connection.Refresh_time = 0;
        }
    }
}
