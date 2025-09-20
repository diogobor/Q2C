using CSMSL.IO;
using Q2C.Control;
using Q2C.Control.Database;
using Q2C.Model;
using Q2C.Properties;
using Q2C.Util;
using Q2C.Viewer.Setup;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
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

namespace Q2C.Viewer.WindowAbout
{
    /// <summary>
    /// Interaction logic for WindowAbout.xaml
    /// </summary>
    public partial class WindowAbout : Window
    {
        public WindowAbout()
        {
            InitializeComponent();
            load_info();
        }

        private void load_info()
        {
            string citation = $"All rights reserved®{DateTime.Now.Year}.";

            AddHyperlink(CitationText, citation);

            string _diogo_name = "Diogo Borges Lima";
            string _diogo_mail = "diogobor@gmail.com";
            AddHyperlink(diogo_mail, _diogo_name, _diogo_mail, true);

            string platform = System.Environment.OSVersion.Platform.ToString();
            if (platform.Contains("Win"))
            {
                osNameLabel.Text = Util.Util.GetWindowsVersion();
            }
            else
            {
                osNameLabel.Text = "Unix";
            }

            processorNameLabel.Text = Util.Util.GetProcessorName();
            RAMmemoryLabel.Text = Util.Util.GetRAMMemory();

            string version = System.Environment.OSVersion.ServicePack.ToString();
            if (!version.Equals(""))
            {
                if (Util.Util.Is64Bits())
                    versionOSLabel.Text = version + " (64 bits)";
                else
                    versionOSLabel.Text = version + " (32 bits)";
            }
            else
            {
                if (Util.Util.Is64Bits())
                    versionOSLabel.Text = "64 bits";
                else
                    versionOSLabel.Text = "32 bits";
            }
            usrLabel.Text = System.Environment.UserName.ToString();
            machineNameLabel.Text = System.Environment.MachineName.ToString();

            Q2CVersion.Text = "Version: " + Util.Util.GetAppVersion();

        }

        private void AddHyperlink(TextBlock textBlock,
                                  string initial_text,
                                  string url = "https://doi.org/10.1016/j.jprot.2025.105511",
                                  bool isEmail = false,
                                  string post_text = "Please")
        {
            textBlock.Inlines.Clear();

            // Create a new Hyperlink
            Hyperlink hyperlink = new Hyperlink();
            if (!isEmail)
                hyperlink.Inlines.Add("cite us");
            else
            {
                hyperlink.Inlines.Add(url);
                url = "mailto:" + url;
            }
            hyperlink.NavigateUri = new System.Uri(url);
            hyperlink.RequestNavigate += Hyperlink_RequestNavigate;
            // Add the Hyperlink to the TextBlock
            if (isEmail)
                initial_text += " (";
            textBlock.Inlines.Add(initial_text);
            if (!isEmail)
                textBlock.Inlines.Add(" " + post_text + " ");
            textBlock.Inlines.Add(hyperlink);
            if (!isEmail)
                textBlock.Inlines.Add(".");
            else
                textBlock.Inlines.Add(")");
        }
        // Event handler for when the hyperlink is clicked
        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            // Open the link in the default web browser
            try
            {
                System.Diagnostics.Process.Start(new ProcessStartInfo
                {
                    FileName = e.Uri.ToString(),
                    UseShellExecute = true,
                });
            }
            catch (Exception)
            {
                System.Windows.MessageBox.Show(
                        "Visit the Q2C website for more usability information.",
                        "Q2C :: Manuscript",
                        (System.Windows.MessageBoxButton)MessageBoxButtons.OK,
                        (System.Windows.MessageBoxImage)MessageBoxIcon.Information);
                throw;
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Connection.Refresh_time = 0;
        }

        private void ButtonResetSettings_Click(object sender, RoutedEventArgs e)
        {
            #region Reset fasta list
            Database _database = Management.GetDatabase();
            if (_database == null) return;
            _database.FastaFiles[0].IsSelected = true;

            if (_database.FastaFiles.Count > 4)
            {
                List<Q2C.Model.FastaFile> current_fasta_list = new List<FastaFile>(_database.FastaFiles);
                for (int i = 4; i < current_fasta_list.Count; i++)
                {
                    Q2C.Model.FastaFile current_fasta = current_fasta_list[i];
                    Util.Util.RemoveFasta(current_fasta, _database);
                }
            }
            Management.SetDatabase(_database);
            #endregion

            #region Reset Methods
            Settings.Default.Methods = Settings.Default.Default_methods;
            Settings.Default.Save();
            #endregion

            System.Windows.MessageBox.Show(
                    "All settings have been reset sucessfully!\nQ2C must be restarted!",
                    "Q2C :: Information",
                    (System.Windows.MessageBoxButton)MessageBoxButtons.OK,
                    (System.Windows.MessageBoxImage)MessageBoxIcon.Warning);

            System.Windows.Forms.Application.Restart();
            System.Windows.Application.Current.Shutdown();
        }
    }
}
