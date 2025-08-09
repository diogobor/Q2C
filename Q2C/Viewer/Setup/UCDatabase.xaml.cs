using Microsoft.VisualBasic.ApplicationServices;
using Q2C.Control;
using Q2C.Control.Database;
using Q2C.Control.FileManagement;
using Q2C.Model;
using Q2C.Properties;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Reflection.PortableExecutable;
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

namespace Q2C.Viewer.Setup
{
    /// <summary>
    /// Interaction logic for UCDatabase.xaml
    /// </summary>
    public partial class UCDatabase : System.Windows.Controls.UserControl
    {
        private System.Windows.Window _window;
        public Database _database { get; private set; }
        public UCDatabase()
        {
            InitializeComponent();
        }

        public void Load(System.Windows.Window window)
        {
            _window = window;
            TextGoogleClientID.Focus();

            if (!String.IsNullOrEmpty(Settings.Default.Database))
                LoadDatabase();
            else
                CreateDatabase();
        }


        private void CreateDatabase()
        {
            List<FastaFile> defaut_fastaFiles = new();
            defaut_fastaFiles.Add(new FastaFile(0, "Human", @"FastaFiles\human.T-R", true));
            defaut_fastaFiles.Add(new FastaFile(1, "BSA", @"FastaFiles\bovin_serum_albumin.T-R", false));
            defaut_fastaFiles.Add(new FastaFile(2, "E. coli", @"FastaFiles\escherichia_coli.T-R", false));
            defaut_fastaFiles.Add(new FastaFile(3, "Yeast", @"FastaFiles\yeast.T-R", false));
            bool isOnline = CheckBoxOfflineMode.IsChecked == false;

            _database = new Database("", "", "", defaut_fastaFiles, isOnline);
            LoadComboFasta();
        }
        private void LoadDatabase()
        {
            Database _current_db = Management.GetDatabase();
            if (_current_db == null) return;
            _database = _current_db;

            TextGoogleClientID.Text = _current_db.GoogleClientID;
            TextGoogleClientSecret.Text = _current_db.GoogleClientSecret;
            TextSpreadSheetID.Text = _current_db.SpreadsheetID;
            CheckBoxOfflineMode.IsChecked = !_current_db.IsOnline;
            if (!String.IsNullOrEmpty(_current_db.SpreadsheetID))
                CheckBoxNewSpreadsheet.IsChecked = false;
            CheckBoxOfflineMode_Click(null, null);
            LoadComboFasta();
        }
        public void UpdateFastaFiles(Database database)
        {
            _database = database;
            LoadComboFasta();
        }
        private void LoadComboFasta()
        {
            List<string> _fastas = new(_database.FastaFiles.Count);
            int selected_index = 0;
            for (int i = 0; i < _database.FastaFiles.Count; i++)
            {
                FastaFile _fasta = _database.FastaFiles[i];
                _fastas.Add(_fasta.Name);
                if (_fasta.IsSelected)
                    selected_index = i;
            }
            ComboFasta.ItemsSource = _fastas;
            ComboFasta.SelectedIndex = selected_index;
        }
        private bool CheckFields()
        {
            if (CheckBoxOfflineMode.IsChecked == true) return false;

            if (String.IsNullOrEmpty(TextGoogleClientID.Text))
            {
                System.Windows.MessageBox.Show(
                            "'Google Client ID' field is empty!",
                            "Q2C :: Warning",
                            (System.Windows.MessageBoxButton)MessageBoxButtons.OK,
                            (System.Windows.MessageBoxImage)MessageBoxIcon.Warning);

                TextGoogleClientID.Focus();
                return true;
            }
            else if (!TextGoogleClientID.Text.EndsWith(".apps.googleusercontent.com"))
            {
                System.Windows.MessageBox.Show(
                            "'Google Client ID' is not valid!",
                            "Q2C :: Warning",
                            (System.Windows.MessageBoxButton)MessageBoxButtons.OK,
                            (System.Windows.MessageBoxImage)MessageBoxIcon.Warning);

                TextGoogleClientID.Focus();
                return true;
            }

            if (String.IsNullOrEmpty(TextGoogleClientSecret.Text))
            {
                System.Windows.MessageBox.Show(
                            "'Google Client Secret' field is empty!",
                            "Q2C :: Warning",
                            (System.Windows.MessageBoxButton)MessageBoxButtons.OK,
                            (System.Windows.MessageBoxImage)MessageBoxIcon.Warning);

                TextGoogleClientSecret.Focus();
                return true;
            }

            if (CheckBoxNewSpreadsheet.IsChecked == false &&
                String.IsNullOrEmpty(TextSpreadSheetID.Text))
            {
                System.Windows.MessageBox.Show(
                            "'Spreadsheet ID' field is empty!",
                            "Q2C :: Warning",
                            (System.Windows.MessageBoxButton)MessageBoxButtons.OK,
                            (System.Windows.MessageBoxImage)MessageBoxIcon.Warning);

                TextSpreadSheetID.Focus();
                return true;
            }
            return false;
        }
        private Database GetDatabase()
        {
            string googleClientID = TextGoogleClientID.Text;
            string googleClientSecret = TextGoogleClientSecret.Text;
            string spreadsheetID = TextSpreadSheetID.Text;
            List<FastaFile> fastaFiles = GetNewFastaFiles(ComboFasta.Text);
            bool isOnline = CheckBoxOfflineMode.IsChecked == false;
            if (!isOnline)
                googleClientID = googleClientSecret = spreadsheetID = string.Empty;

            return new Database(googleClientID, googleClientSecret, spreadsheetID, fastaFiles, isOnline);
        }
        private List<FastaFile> GetNewFastaFiles(string fasta_name)
        {
            List<FastaFile> fastaFiles = (from fasta in _database.FastaFiles.AsParallel()
                                          select new FastaFile(fasta.Id, fasta.Name, fasta.Path, false)).ToList();
            var selected_fasta = fastaFiles.FindIndex(a => a.Name.Equals(fasta_name));
            if (selected_fasta == -1) return null;
            fastaFiles[selected_fasta].IsSelected = true;

            return fastaFiles;
        }
        private async void ButtonConfirm_Click(object sender, RoutedEventArgs e)
        {
            if (CheckFields()) return;

            Database new_db = GetDatabase();
            if (new_db == null) return;

            UCWaitScreen waitScreen = new UCWaitScreen("Please Wait...", "Setting database parameters...");
            Grid.SetRow(waitScreen, 0);
            Grid.SetRowSpan(waitScreen, 2);
            waitScreen.Margin = new Thickness(0, 0, 0, -10);
            MainUCDatabase.Children.Add(waitScreen);

            Database current_db = Management.GetDatabase();

            bool transferOnlineToOffline = false;
            if (current_db != null && current_db.IsOnline && !new_db.IsOnline)
            {
                var r1 = System.Windows.Forms.MessageBox.Show(
                        "Would you like to transfer the Online to Offline (local) DB?\nIf not, a new local DB will be created!",
                        "Q2C :: Warning",
                        System.Windows.Forms.MessageBoxButtons.YesNo,
                        System.Windows.Forms.MessageBoxIcon.Warning);
                if (r1 == System.Windows.Forms.DialogResult.Yes) transferOnlineToOffline = true;
            }

            bool isNewSpreadsheet = CheckBoxNewSpreadsheet.IsChecked == true;
            byte _response = 0;
            await Task.Run(() => _response = ProcessDatabase(new_db, isNewSpreadsheet, transferOnlineToOffline));

            MainUCDatabase.Children.Remove(waitScreen);

            if (_response == 1)
            {
                System.Windows.Forms.Application.Restart();
                System.Windows.Application.Current.Shutdown();
            }
        }

        private byte ProcessDatabase(Database db, bool isNewSpreadsheet, bool transferOnlineToOffline)
        {
            bool createNewSpreadsheet = false;
            if (db.IsOnline)
            {
                if (isNewSpreadsheet &&
                    !String.IsNullOrEmpty(db.SpreadsheetID))
                {
                    var r1 = System.Windows.Forms.MessageBox.Show(
                        "A 'Spreadsheet ID' has been identified!\nDo you want to proceed? If so, the new spreadsheet will replace the old one.",
                        "Q2C :: Warning",
                        System.Windows.Forms.MessageBoxButtons.YesNo,
                        System.Windows.Forms.MessageBoxIcon.Warning);
                    if (r1 != System.Windows.Forms.DialogResult.Yes) return 0;
                    createNewSpreadsheet = true;
                }
                else if (String.IsNullOrEmpty(db.SpreadsheetID))
                    createNewSpreadsheet = true;
            }

            var r = System.Windows.Forms.MessageBox.Show("Are you sure you want to modify the database settings?", "Q2C :: Warning", System.Windows.Forms.MessageBoxButtons.YesNo, System.Windows.Forms.MessageBoxIcon.Warning);
            if (r != System.Windows.Forms.DialogResult.Yes) return 0;

            try
            {
                if (transferOnlineToOffline)
                    Management.TransferOnlineToOffline();

                //Move new fasta files to user folder => C:\Users\{user}\.q2c\FastaFiles
                Management.CreateFastaFiles(db.FastaFiles.Where(a => a.Id > 3).ToList());
                Management.SetDatabase(db);

                if (createNewSpreadsheet)
                    Management.CreateNewSpreadsheet(db);

                System.Windows.MessageBox.Show(
                               "Database settings have been saved successfully!\nQ2C must be restarted!",
                               "Q2C :: Warning",
                               (System.Windows.MessageBoxButton)MessageBoxButtons.OK,
                               (System.Windows.MessageBoxImage)MessageBoxIcon.Warning);

                if (db.IsOnline)
                    Connection.RevokeConnection();
                return 1;

            }
            catch (Exception)
            {
                throw;
            }
        }

        private void ButtonCancel_Click(object sender, RoutedEventArgs e)
        {
            if (_window != null)
                _window.Close();
        }
        private void CheckBoxNewSpreadSheet_Click(object sender, RoutedEventArgs e)
        {
            if (CheckBoxNewSpreadsheet.IsChecked == true)
            {
                TextSpreadSheetID.IsEnabled = false;
                ButtonExport.IsEnabled = false;
            }
            else
            {
                TextSpreadSheetID.IsEnabled = true;
                ButtonExport.IsEnabled = true;
            }
        }

        private void ComboFasta_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void ButtonAddFasta_Click(object sender, RoutedEventArgs e)
        {
            var _windowFastaFile = new WindowFastaFiles(_window, _database);
            _windowFastaFile.ShowDialog();
        }

        private async void ButtonExportSpreadsheet_Click(object sender, RoutedEventArgs e)
        {
            if (Connection.IsOnline && CheckBoxNewSpreadsheet.IsChecked == true)
            {
                System.Windows.MessageBox.Show(
                               "To export the current spreadsheet, uncheck the 'Create spreadsheet' option.",
                               "Q2C :: Warning",
                               (System.Windows.MessageBoxButton)MessageBoxButtons.OK,
                               (System.Windows.MessageBoxImage)MessageBoxIcon.Warning);
                return;
            }

            Database db = Management.GetDatabase();
            if (db == null)
            {
                System.Windows.MessageBox.Show(
                               "No database has been found.",
                               "Q2C :: Warning",
                               (System.Windows.MessageBoxButton)MessageBoxButtons.OK,
                               (System.Windows.MessageBoxImage)MessageBoxIcon.Warning);
                return;
            }

            if (Connection.IsOnline && String.IsNullOrEmpty(db.SpreadsheetID))
            {
                System.Windows.MessageBox.Show(
                               "No spreadsheet has been found.",
                               "Q2C :: Warning",
                               (System.Windows.MessageBoxButton)MessageBoxButtons.OK,
                               (System.Windows.MessageBoxImage)MessageBoxIcon.Warning);
                return;
            }

            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
            dlg.FileName = "q2c_database_report.xlsx"; // Default file name
            dlg.Filter = "Database file (*.xlsx)|*.xlsx"; // Filter files by extension
            dlg.Title = "Q2C :: Database :: Report";

            // Show open file dialog box
            Nullable<bool> result = dlg.ShowDialog();

            if (result == true)
            {
                UCWaitScreen waitScreen = new UCWaitScreen("Please Wait...", "Exporting database...");
                Grid.SetRow(waitScreen, 0);
                Grid.SetRowSpan(waitScreen, 2);
                waitScreen.Margin = new Thickness(0, 0, 0, -10);
                MainUCDatabase.Children.Add(waitScreen);

                bool isNewSpreadsheet = CheckBoxNewSpreadsheet.IsChecked == true;
                byte _response = 0;
                await Task.Run(() => _response = ExportSpreadsheet(dlg.FileName));

                MainUCDatabase.Children.Remove(waitScreen);

                if (_response == 1)
                    System.Windows.MessageBox.Show(
                               "Database has been exported successfully.",
                               "Q2C :: Information",
                               (System.Windows.MessageBoxButton)MessageBoxButtons.OK,
                               (System.Windows.MessageBoxImage)MessageBoxIcon.Information);
                else
                    System.Windows.MessageBox.Show(
                               "Unable to export the database.",
                               "Q2C :: Warning",
                               (System.Windows.MessageBoxButton)MessageBoxButtons.OK,
                               (System.Windows.MessageBoxImage)MessageBoxIcon.Warning);

            }
        }

        private byte ExportSpreadsheet(string fileName)
        {
            var isValid = Management.ExportDatabase(fileName);
            if (isValid == true)
                return 1;
            return 0;
        }

        private void CheckBoxOfflineMode_Click(object sender, RoutedEventArgs e)
        {
            if (CheckBoxOfflineMode.IsChecked == true)
            {
                TextGoogleClientID.IsEnabled = false;
                TextGoogleClientSecret.IsEnabled = false;
                TextSpreadSheetID.IsEnabled = false;
                CheckBoxNewSpreadsheet.IsEnabled = false;
                ButtonExport.IsEnabled = true;
            }
            else
            {
                TextGoogleClientID.IsEnabled = true;
                TextGoogleClientSecret.IsEnabled = true;
                TextSpreadSheetID.IsEnabled = true;
                CheckBoxNewSpreadsheet.IsEnabled = true;
                CheckBoxNewSpreadSheet_Click(null, null);
            }
        }
    }
}
