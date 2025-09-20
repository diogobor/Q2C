using Q2C.Control;
using Q2C.Model;
using Q2C.Properties;
using System;
using System.Collections.Generic;
using System.DirectoryServices.ActiveDirectory;
using System.IO;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace Q2C.Viewer.Project
{
    /// <summary>
    /// Interaction logic for UCMethod.xaml
    /// </summary>
    public partial class UCMethod : System.Windows.Controls.UserControl
    {
        private List<DataGridItem> _allMethods { get; set; }
        public UCMethod()
        {
            InitializeComponent();
        }

        public void Load()
        {
            if (!String.IsNullOrEmpty(Settings.Default.Methods))
                _allMethods = Regex.Split(Settings.Default.Methods, "\r\n").Select(a => new DataGridItem(a)).ToList();
            else
                _allMethods = new();

            UpdateDataGrid();
            TextComment.Focus();
        }

        private void DataGridMethods_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            e.Row.Header = (e.Row.GetIndex() + 1).ToString();
        }

        private void DataGridMethods_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Delete)
            {
                string method = Util.Util.GetSelectedValue(DataGridMethods, TagProperty);
                var r = System.Windows.Forms.MessageBox.Show("Do you want to remove '" + method + "' method?", "Q2C :: Warning", System.Windows.Forms.MessageBoxButtons.YesNo, System.Windows.Forms.MessageBoxIcon.Warning);
                if (r == System.Windows.Forms.DialogResult.Yes)
                {
                    if (RemoveMethod(method))
                        System.Windows.MessageBox.Show(
                                    "Method has been removed successfully!",
                                    "Q2C :: Information",
                                    (System.Windows.MessageBoxButton)MessageBoxButtons.OK,
                                    (System.Windows.MessageBoxImage)MessageBoxIcon.Information);
                    CleanFields();
                }
                else
                {
                    e.Handled = true;
                }
            }
            else if (e.Key == Key.I && (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)))
            {
                UpdateDataGrid();
                CleanFields();
                TextComment.Focus();
            }
            else
                return;
        }

        private void ButtonConfirm_Click(object sender, RoutedEventArgs e)
        {
            if (CreateOrUpdateMethod())
            {
                UpdateDataGrid();
            }
        }

        private void CleanFields()
        {
            ComboQuantification.SelectedIndex = 0;
            ComboPurpose.SelectedIndex = 0;
            ComboGradient.SelectedIndex = 0;
            TextComment.Text = "";
        }

        private bool CreateOrUpdateMethod()
        {
            string method = GetMethodFields();
            if (AddOrUpdateMethod(method))
            {
                System.Windows.MessageBox.Show(
                            "Method has been saved successfully!",
                            "Q2C :: Information",
                            (System.Windows.MessageBoxButton)MessageBoxButtons.OK,
                            (System.Windows.MessageBoxImage)MessageBoxIcon.Information);
                CleanFields();
            }
            return true;
        }

        private void UpdateDataGrid()
        {
            DataGridMethods.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(delegate ()
            {
                DataGridMethods.ItemsSource = null;
                DataGridMethods.ItemsSource = _allMethods;
            }));
        }

        private string GetMethodFields()
        {
            StringBuilder sb = new StringBuilder();

            int _method_index = ComboQuantification.SelectedIndex;
            if (_method_index > 0)
            {
                sb.Append(((System.Windows.Controls.ComboBoxItem)(ComboQuantification.SelectedValue)).Content.ToString());
                sb.Append('_');
            }

            int _purpose_index = ComboPurpose.SelectedIndex;
            if (_purpose_index > 0)
            {
                sb.Append(((System.Windows.Controls.ComboBoxItem)(ComboPurpose.SelectedValue)).Content.ToString());
                sb.Append('_');
            }

            int _gradient_index = ComboGradient.SelectedIndex;
            if (_gradient_index > -1)
            {
                string gradient = ((System.Windows.Controls.ComboBoxItem)(ComboGradient.SelectedValue)).Content.ToString();
                if (gradient.Equals("Custom"))
                    gradient = Convert.ToString(UpDownCustomGradient.Value);
                sb.Append(gradient);
                sb.Append('_');
            }

            if (!String.IsNullOrEmpty(TextComment.Text))
            {
                sb.Append(TextComment.Text);
                sb.Append('_');
            }

            return sb.ToString().Substring(0, sb.ToString().Length - 1);
        }

        private bool RemoveMethod(string method)
        {
            if (!String.IsNullOrEmpty(Settings.Default.Methods))
                _allMethods = Regex.Split(Settings.Default.Methods, "\r\n").Select(a => new DataGridItem(a)).ToList();
            else
            {
                System.Windows.MessageBox.Show(
                       "Method has not been found in the list!",
                       "Q2C :: Warning",
                       (System.Windows.MessageBoxButton)MessageBoxButtons.OK,
                       (System.Windows.MessageBoxImage)MessageBoxIcon.Warning);
                return false;
            }

            string _selectedMethod = Util.Util.GetSelectedValue(DataGridMethods, TagProperty);
            if (!String.IsNullOrEmpty(_selectedMethod))
            {
                _allMethods = _allMethods.Where(a => !a.Name.Equals(_selectedMethod)).ToList();
                _allMethods.Sort((a, b) => a.Name.CompareTo(b.Name));
                Settings.Default.Methods = String.Join("\r\n", _allMethods.Select(a => a.Name).ToList());
                Settings.Default.Save();
            }
            else
            {
                System.Windows.MessageBox.Show(
                       "No method has been selected!",
                       "Q2C :: Warning",
                       (System.Windows.MessageBoxButton)MessageBoxButtons.OK,
                       (System.Windows.MessageBoxImage)MessageBoxIcon.Warning);
                return false;
            }

            return true;
        }
        private bool AddOrUpdateMethod(string method)
        {
            if (!String.IsNullOrEmpty(Settings.Default.Methods))
                _allMethods = Regex.Split(Settings.Default.Methods, "\r\n").Select(a => new DataGridItem(a)).ToList();
            else
                _allMethods = new();

            string _selectedMethod = Util.Util.GetSelectedValue(DataGridMethods, TagProperty);
            if (!String.IsNullOrEmpty(_selectedMethod))
            {
                //Update
                _allMethods = _allMethods.Where(a => !a.Name.Equals(_selectedMethod)).ToList();
            }
            else
            {
                //Add
                if (_allMethods.Where(a => a.Name.Equals(method)).Count() > 0)
                {
                    System.Windows.MessageBox.Show(
                       "Method already existis in the list!",
                       "Q2C :: Warning",
                       (System.Windows.MessageBoxButton)MessageBoxButtons.OK,
                       (System.Windows.MessageBoxImage)MessageBoxIcon.Warning);
                    return false;
                }
            }

            _allMethods.Add(new DataGridItem(method));
            _allMethods.Sort((a, b) => a.Name.CompareTo(b.Name));
            Settings.Default.Methods = String.Join("\r\n", _allMethods.Select(a => a.Name).ToList());
            Settings.Default.Save();

            return true;
        }

        private void DataGridMethods_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            CleanFields();
            FillMethodFields();
        }

        private void FillMethodFields()
        {
            string _selectedMethod = Util.Util.GetSelectedValue(DataGridMethods, TagProperty);
            if (String.IsNullOrEmpty(_selectedMethod)) return;

            string[] cols = Regex.Split(_selectedMethod, "_");
            int _gradient_index = 0;
            for (_gradient_index = 0; _gradient_index < cols.Length; _gradient_index++)
                if (cols[_gradient_index].Any(char.IsDigit) && cols[_gradient_index] != "BS3")
                    break;

            switch (cols[_gradient_index])
            {
                case "0":
                    ComboGradient.SelectedIndex = 0;
                    break;
                case "60":
                    ComboGradient.SelectedIndex = 1;
                    break;
                case "80":
                    ComboGradient.SelectedIndex = 2;
                    break;
                case "120":
                    ComboGradient.SelectedIndex = 3;
                    break;
                case "180":
                    ComboGradient.SelectedIndex = 4;
                    break;
                default:
                    ComboGradient.SelectedIndex = 5;
                    UpDownCustomGradient.Value = Convert.ToInt32(cols[_gradient_index]);
                    break;
            }

            //Quantification/purpose/gradient/comment

            if (_gradient_index == 0)//There is no quant nor purpose
            {
                //gradient/comment
                if (cols.Length > 1)
                    TextComment.Text = cols[_gradient_index + 1];
            }
            else if (_gradient_index == 1)
            {
                if (cols.Length == 2)//Quantification or purpose / gradient
                {
                    switch (cols[0])
                    {
                        case "LFQ":
                            ComboQuantification.SelectedIndex = 1;
                            break;
                        case "TMT":
                            ComboQuantification.SelectedIndex = 2;
                            break;
                        case "iTRAQ":
                            ComboQuantification.SelectedIndex = 3;
                            break;
                        case "SILAC":
                            ComboQuantification.SelectedIndex = 4;
                            break;
                        case "Customized":
                            ComboQuantification.SelectedIndex = 5;
                            break;
                        default:
                            //It means cols[0] is a purpose
                            switch (cols[0])
                            {
                                case "BS3":
                                    ComboPurpose.SelectedIndex = 1;
                                    break;
                                case "DSS":
                                    ComboPurpose.SelectedIndex = 2;
                                    break;
                                case "DSSO":
                                    ComboPurpose.SelectedIndex = 3;
                                    break;
                                case "SDA":
                                    ComboPurpose.SelectedIndex = 4;
                                    break;
                                case "PhoX":
                                    ComboPurpose.SelectedIndex = 5;
                                    break;
                                case "Phospho":
                                    ComboPurpose.SelectedIndex = 6;
                                    break;
                                case "DSBSO":
                                    ComboPurpose.SelectedIndex = 7;
                                    break;
                                case "DSBU":
                                    ComboPurpose.SelectedIndex = 8;
                                    break;
                                case "ProteinID":
                                    ComboPurpose.SelectedIndex = 9;
                                    break;
                                default:
                                    ComboPurpose.SelectedIndex = 0;
                                    break;
                            }
                            break;
                    }
                }
                else
                {
                    switch (cols[0])
                    {
                        case "LFQ":
                            ComboQuantification.SelectedIndex = 1;
                            break;
                        case "TMT":
                            ComboQuantification.SelectedIndex = 2;
                            break;
                        case "iTRAQ":
                            ComboQuantification.SelectedIndex = 3;
                            break;
                        case "SILAC":
                            ComboQuantification.SelectedIndex = 4;
                            break;
                        case "Customized":
                            ComboQuantification.SelectedIndex = 5;
                            break;
                        default:
                            //It means cols[0] is a purpose
                            switch (cols[0])
                            {
                                case "BS3":
                                    ComboPurpose.SelectedIndex = 1;
                                    break;
                                case "DSS":
                                    ComboPurpose.SelectedIndex = 2;
                                    break;
                                case "DSSO":
                                    ComboPurpose.SelectedIndex = 3;
                                    break;
                                case "SDA":
                                    ComboPurpose.SelectedIndex = 4;
                                    break;
                                case "PhoX":
                                    ComboPurpose.SelectedIndex = 5;
                                    break;
                                case "Phospho":
                                    ComboPurpose.SelectedIndex = 6;
                                    break;
                                case "DSBSO":
                                    ComboPurpose.SelectedIndex = 7;
                                    break;
                                case "DSBU":
                                    ComboPurpose.SelectedIndex = 8;
                                    break;
                                case "ProteinID":
                                    ComboPurpose.SelectedIndex = 9;
                                    break;
                                default:
                                    ComboPurpose.SelectedIndex = 0;
                                    break;
                            }
                            break;
                    }
                    TextComment.Text = cols[2];
                }
            }
            else if (_gradient_index == 2)
            {
                switch (cols[0])
                {
                    case "LFQ":
                        ComboQuantification.SelectedIndex = 1;
                        break;
                    case "TMT":
                        ComboQuantification.SelectedIndex = 2;
                        break;
                    case "iTRAQ":
                        ComboQuantification.SelectedIndex = 3;
                        break;
                    case "SILAC":
                        ComboQuantification.SelectedIndex = 4;
                        break;
                    case "Customized":
                        ComboQuantification.SelectedIndex = 5;
                        break;
                    default:
                        break;
                }
                switch (cols[1])
                {
                    case "BS3":
                        ComboPurpose.SelectedIndex = 1;
                        break;
                    case "DSS":
                        ComboPurpose.SelectedIndex = 2;
                        break;
                    case "DSSO":
                        ComboPurpose.SelectedIndex = 3;
                        break;
                    case "SDA":
                        ComboPurpose.SelectedIndex = 4;
                        break;
                    case "PhoX":
                        ComboPurpose.SelectedIndex = 5;
                        break;
                    case "Phospho":
                        ComboPurpose.SelectedIndex = 6;
                        break;
                    case "DSBSO":
                        ComboPurpose.SelectedIndex = 7;
                        break;
                    case "DSBU":
                        ComboPurpose.SelectedIndex = 8;
                        break;
                    case "ProteinID":
                        ComboPurpose.SelectedIndex = 9;
                        break;
                    default:
                        ComboPurpose.SelectedIndex = 0;
                        break;
                }

                if (cols.Length > 3)
                    TextComment.Text = cols[3];
            }
        }

        private void ButtonLoad_Click(object sender, RoutedEventArgs e)
        {
            var ofd = new Microsoft.Win32.OpenFileDialog()
            {
                Title = "Load Q2C methods",
                Filter = "Q2C methods|*.json",
                AddExtension = true
            };

            if (ofd.ShowDialog() != true) return;

            try
            {

                string content = File.ReadAllText(ofd.FileName);
                Settings.Default.Methods = Control.FileManagement.Serializer.FromJson<string>(content, false);
                Settings.Default.Save();
                Load();

                System.Windows.MessageBox.Show(
                                    "Methods have been loaded successfully!",
                                    "Q2C :: Information",
                                    (System.Windows.MessageBoxButton)MessageBoxButtons.OK,
                                    (System.Windows.MessageBoxImage)MessageBoxIcon.Information);
            }
            catch (Exception)
            {
                System.Windows.MessageBox.Show(
                                "Failed to load methods!",
                                "Q2C :: Error",
                                (System.Windows.MessageBoxButton)MessageBoxButtons.OK,
                                (System.Windows.MessageBoxImage)MessageBoxIcon.Error);
                return;
            }
        }

        private void ButtonSave_Click(object sender, RoutedEventArgs e)
        {
            var sfd = new Microsoft.Win32.SaveFileDialog()
            {
                Title = "Save Q2C methods",
                Filter = "Q2C methos|*.json",
                FileName = "q2c_methods.json",
                AddExtension = true
            };

            if (sfd.ShowDialog() != true) return;

            try
            {
                string methodsToJson = String.Join("\r\n", _allMethods.Select(a => a.Name).ToList());
                string json = Control.FileManagement.Serializer.ToJSON(methodsToJson, false);
                File.Delete(sfd.FileName);
                File.WriteAllText(sfd.FileName, json);
                System.Windows.MessageBox.Show(
                                        "Methods have been exported successfully!",
                                        "Q2C :: Information",
                                        (System.Windows.MessageBoxButton)MessageBoxButtons.OK,
                                        (System.Windows.MessageBoxImage)MessageBoxIcon.Information);
            }
            catch (Exception)
            {
                System.Windows.MessageBox.Show(
                                "Failed to export methods!",
                                "Q2C :: Error",
                                (System.Windows.MessageBoxButton)MessageBoxButtons.OK,
                                (System.Windows.MessageBoxImage)MessageBoxIcon.Error);
                return;
            }
        }

        private void ComboGradient_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ComboGradient.SelectedValue != null &&
                ((System.Windows.Controls.ComboBoxItem)(ComboGradient.SelectedValue)).Content != null)
            {
                string gradient = ((System.Windows.Controls.ComboBoxItem)(ComboGradient.SelectedValue)).Content.ToString();
                if (gradient.Equals("Custom"))
                    UpDownCustomGradient.Visibility = Visibility.Visible;
                else
                    UpDownCustomGradient.Visibility = Visibility.Collapsed;
            }
        }
    }

    internal class DataGridItem
    {
        public string Name { get; set; }

        public DataGridItem(string name)
        {
            Name = name;
        }
    }
}
