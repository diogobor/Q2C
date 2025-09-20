using Q2C.Control;
using Q2C.Model;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
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

namespace Q2C.Viewer.Setup
{
    /// <summary>
    /// Interaction logic for UCFastaFiles.xaml
    /// </summary>
    public partial class UCFastaFiles : UserControl
    {
        private Window _window;
        private Database _database;
        public UCFastaFiles()
        {
            InitializeComponent();
        }

        public void Load(Window window, Database database)
        {
            _window = window;
            _database = database;
            if (_database == null) return;
            UpdateDataGrid();
        }

        private void UpdateDataGrid()
        {
            DataGridFastas.ItemsSource = null;
            DataGridFastas.ItemsSource = _database.FastaFiles;
        }

        private bool CheckFields()
        {
            if (String.IsNullOrEmpty(TextFastaName.Text))
            {
                System.Windows.MessageBox.Show(
                            "'Fasta name' field is empty!",
                            "Q2C :: Warning",
                            (System.Windows.MessageBoxButton)System.Windows.Forms.MessageBoxButtons.OK,
                            (System.Windows.MessageBoxImage)System.Windows.Forms.MessageBoxIcon.Warning);

                TextFastaName.Focus();
                return true;
            }

            if (String.IsNullOrEmpty(TextFastaDir.Text))
            {
                System.Windows.MessageBox.Show(
                            "'File' field is empty!",
                            "Q2C :: Warning",
                            (System.Windows.MessageBoxButton)System.Windows.Forms.MessageBoxButtons.OK,
                            (System.Windows.MessageBoxImage)System.Windows.Forms.MessageBoxIcon.Warning);

                TextFastaDir.Focus();
                return true;
            }

            return false;
        }

        private void ButtonConfirm_Click(object sender, RoutedEventArgs e)
        {
            if (CheckFields()) return;

            string fasta_name = TextFastaName.Text;
            string fasta_dir = TextFastaDir.Text;

            bool isUpdate = false;
            var selected_fasta = (FastaFile)DataGridFastas.SelectedItem;
            if (selected_fasta != null)
            {
                var current_fasta = _database.FastaFiles.Where(a => a.Id == selected_fasta.Id).FirstOrDefault();
                if (current_fasta != null && !current_fasta.Path.Equals(fasta_dir))
                {
                    current_fasta.Path = fasta_dir;
                    isUpdate = true;
                }
                else
                    return;
            }
            else
            {
                List<string> fastaNames = _database.FastaFiles.Select(a => a.Name.ToLower()).ToList();
                if (fastaNames.Contains(fasta_name.ToLower()))
                {
                    System.Windows.MessageBox.Show(
                                    "Fasta name exists in the list!\nPlease, insert another name.",
                                    "Q2C :: Warning",
                                    (System.Windows.MessageBoxButton)System.Windows.Forms.MessageBoxButtons.OK,
                                    (System.Windows.MessageBoxImage)System.Windows.Forms.MessageBoxIcon.Warning);
                    TextFastaName.Focus();
                    return;
                }
                _database.FastaFiles.Sort((a, b) => a.Id.CompareTo(b.Id));
                FastaFile _newFasta = new FastaFile((_database.FastaFiles[_database.FastaFiles.Count - 1].Id + 1), fasta_name, fasta_dir, false);
                _database.FastaFiles.Add(_newFasta);
            }

            UpdateDataGrid();
            ((WindowFastaFiles)_window).UpdateDatabase(_database);

            ButtonFile.IsEnabled = true;
            CleanFields();
            TextFastaName.IsReadOnly = false;
            TextFastaName.Focus();

            if (isUpdate)
                System.Windows.MessageBox.Show(
                                        "Fasta has been update successfully!",
                                        "Q2C :: Information",
                                        (System.Windows.MessageBoxButton)System.Windows.Forms.MessageBoxButtons.OK,
                                        (System.Windows.MessageBoxImage)System.Windows.Forms.MessageBoxIcon.Information);
            else
                System.Windows.MessageBox.Show(
                                    "Fasta has been added successfully!",
                                    "Q2C :: Information",
                                    (System.Windows.MessageBoxButton)System.Windows.Forms.MessageBoxButtons.OK,
                                    (System.Windows.MessageBoxImage)System.Windows.Forms.MessageBoxIcon.Information);
        }

        private void DataGridFastas_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            e.Row.Header = (e.Row.GetIndex() + 1).ToString();
        }

        private void DataGridFastas_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete)
            {
                if (((DataGrid)sender).SelectedIndex < 4)
                {
                    e.Handled = true;
                    return;
                }

                var fasta = (FastaFile)((DataGrid)sender).SelectedItem;
                var r = System.Windows.Forms.MessageBox.Show("Do you want to remove '" + fasta.Name + "' file?", "Q2C :: Warning", System.Windows.Forms.MessageBoxButtons.YesNo, System.Windows.Forms.MessageBoxIcon.Warning);
                if (r == System.Windows.Forms.DialogResult.Yes)
                {
                    if (Util.Util.RemoveFasta(fasta, _database))
                    {
                        System.Windows.MessageBox.Show(
                                    "Fasta has been removed successfully!",
                                    "Q2C :: Information",
                                    (System.Windows.MessageBoxButton)System.Windows.Forms.MessageBoxButtons.OK,
                                    (System.Windows.MessageBoxImage)System.Windows.Forms.MessageBoxIcon.Information);
                        CleanFields();
                        UpdateDataGrid();
                        TextFastaName.IsReadOnly = false;
                        ButtonFile.IsEnabled = true;
                    }
                }
                else
                {
                    e.Handled = true;
                }
            }
            else if (e.Key == Key.I && Keyboard.IsKeyDown(Key.LeftCtrl))
            {
                DataGridFastas.SelectedItem = null;
                DataGridFastas.SelectedIndex = -1;
                DataGridFastas.SelectedValue = null;
                CleanFields();
                TextFastaName.IsReadOnly = false;
                ButtonFile.IsEnabled = true;
                TextFastaName.Focus();
            }
            else
                return;
        }

        private void CleanFields()
        {
            TextFastaName.Text = "";
            TextFastaDir.Text = "";
        }

        private void DataGridFastas_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            CleanFields();
            TextFastaName.IsReadOnly = true;

            int selected_index = ((DataGrid)sender).SelectedIndex;
            if (selected_index < 4)
                ButtonFile.IsEnabled = false;
            else
                ButtonFile.IsEnabled = true;

            var fasta = (FastaFile)((DataGrid)sender).SelectedItem;
            if (fasta != null)
                FillFields(fasta);
        }

        private void FillFields(FastaFile fasta)
        {
            TextFastaName.Text = fasta.Name;
            TextFastaDir.Text = fasta.Path;
        }

        private void ButtonBrowseFile_Click(object sender, RoutedEventArgs e)
        {
            var ofd = new System.Windows.Forms.OpenFileDialog()
            {
                Title = "Open Fasta file",
                Filter = "Fasta file|*.fasta",
                FileName = "",
                Multiselect = false
            };

            if (ofd.ShowDialog() != System.Windows.Forms.DialogResult.OK) return;

            TextFastaDir.Text = ofd.FileName;

        }
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
