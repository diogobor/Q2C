using Q2C.Control;
using Q2C.Control.Database;
using Q2C.Model;
using Q2C.Util;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using ComboBoxItem = System.Windows.Controls.ComboBoxItem;
using UserControl = System.Windows.Controls.UserControl;

namespace Q2C.Viewer.Setup
{
    /// <summary>
    /// Interaction logic for UCUser.xaml
    /// </summary>
    public partial class UCUser : UserControl
    {
        private System.Windows.Window _window;
        private User _user;
        public UCUser()
        {
            InitializeComponent();
        }

        public void Load(System.Windows.Window window, User user)
        {
            _window = window;
            _user = user;

            TextUsername.Focus();

            if (_user != null)
                LoadUser();
        }

        private void LoadUser()
        {
            TextUsername.Text = _user.Name;
            TextUsername.IsReadOnly = true;
            UserHyperLink.IsEnabled = false;
            TextEmail.Text = _user.Email;

            int category_index = -1;
            if (_user.Category == UserCategory.User)
                category_index = 0;
            else if (_user.Category == UserCategory.UserSample)
                category_index = 1;
            else if (_user.Category == UserCategory.SuperUsrSample)
                category_index = 2;
            else if (_user.Category == UserCategory.SuperUsrMachine)
                category_index = 3;
            else if (_user.Category == UserCategory.SuperUsrSampleMachine)
                category_index = 4;
            else if (_user.Category == UserCategory.MasterUsrSample)
                category_index = 5;
            else if (_user.Category == UserCategory.MasterUsrSampleMachine)
                category_index = 6;
            else if (_user.Category == UserCategory.Administrator)
                category_index = 7;

            ComboCategory.SelectedIndex = category_index;
        }

        private void ButtonCancel_Click(object sender, RoutedEventArgs e)
        {
            if (_window != null)
                _window.Close();
        }

        private void ButtonConfirm_Click(object sender, RoutedEventArgs e)
        {
            if (_window == null) return;

            if (CreateOrUpdateUser())
            {
                Connection.ReadInfo(true);
                ((WindowUser)_window).UpdateUsersDataGrid();
                _window.Close();
            }
        }
        private bool CheckFields()
        {
            if (String.IsNullOrEmpty(TextUsername.Text))
            {
                System.Windows.MessageBox.Show(
                            "'Username' field is empty!",
                            "Q2C :: Warning",
                            (System.Windows.MessageBoxButton)MessageBoxButtons.OK,
                            (System.Windows.MessageBoxImage)MessageBoxIcon.Warning);

                TextUsername.Focus();
                return true;
            }

            if (String.IsNullOrEmpty(TextEmail.Text))
            {
                System.Windows.MessageBox.Show(
                            "'Email' field is empty!",
                            "Q2C :: Warning",
                            (System.Windows.MessageBoxButton)MessageBoxButtons.OK,
                            (System.Windows.MessageBoxImage)MessageBoxIcon.Warning);

                TextEmail.Focus();
                return true;
            }

            if (!Util.Util.IsValidEmail(TextEmail.Text))
            {
                System.Windows.MessageBox.Show(
                            "Email is not valid!",
                            "Q2C :: Warning",
                            (System.Windows.MessageBoxButton)MessageBoxButtons.OK,
                            (System.Windows.MessageBoxImage)MessageBoxIcon.Warning);

                TextEmail.Focus();
                return true;
            }

            return false;
        }
        private bool CreateOrUpdateUser()
        {
            if (CheckFields()) return false;

            User user = GetUser();
            //0: sucess; 1: failed
            byte operation_status = Connection.AddOrUpdateUser(user);
            if (operation_status == 0)
            {
                if (_window.Title.Contains("Add"))
                    System.Windows.MessageBox.Show(
                                "User has been added successfully!",
                                "Q2C :: Information",
                                (System.Windows.MessageBoxButton)MessageBoxButtons.OK,
                                (System.Windows.MessageBoxImage)MessageBoxIcon.Information);
                else
                    System.Windows.MessageBox.Show(
                                "User has been updated successfully!",
                                "Q2C :: Information",
                                (System.Windows.MessageBoxButton)MessageBoxButtons.OK,
                                (System.Windows.MessageBoxImage)MessageBoxIcon.Information);
            }
            else if (operation_status == 1)
            {
                System.Windows.MessageBox.Show(
                               "Failed to add/modify user",
                               "Q2C :: Warning",
                               (System.Windows.MessageBoxButton)MessageBoxButtons.OK,
                               (System.Windows.MessageBoxImage)MessageBoxIcon.Warning);
                return false;
            }
            return true;
        }

        private User GetUser()
        {
            DateTime registrationDate = DateTime.Now;
            string taskDate_str = registrationDate.ToString("dd") + "/" + registrationDate.ToString("MM") + "/" + registrationDate.ToString("yyyy") + " " + registrationDate.ToString("HH:mm:ss");

            ComboBoxItem cb = (ComboBoxItem)ComboCategory.SelectedItem;
            if (cb == null) return null;

            string category_combo = cb.Content.ToString();
            UserCategory category = UserCategory.Undefined;
            if (category_combo != null)
                category = User.GetCategory(category_combo);

            string username = TextUsername.Text;
            string originalEmail = TextEmail.Text;
            string email = Util.Util.EncryptString(originalEmail);

            return new User(-1,
                taskDate_str,
                username,
                category,
                email,
                Management.InfoStatus.Active);
        }

        private void HyperlinkAdvanced_Click(object sender, RoutedEventArgs e)
        {
            TextUsername.Text = System.Environment.UserName.ToString();
        }
    }
}
