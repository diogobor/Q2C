using Q2C.Control.Database;
using Q2C.Control;
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

namespace Q2C.Viewer.Setup
{
    /// <summary>
    /// Interaction logic for WindowDatabase.xaml
    /// </summary>
    public partial class WindowDatabase : Window
    {
        public WindowDatabase()
        {
            InitializeComponent();
            _Database.Load(this);
        }

        public void UpdateFastaFiles(Database database)
        {
            _Database.UpdateFastaFiles(database);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            bool cancel = false;
            var saved_db = Management.GetDatabase();
            if (saved_db != null)
            {
                if (_Database._database != null)
                {
                    if (saved_db.FastaFiles.Count != _Database._database.FastaFiles.Count)
                        cancel = true;
                    else if (saved_db.FastaFiles.Count > 4 &&
                    !saved_db.FastaFiles[saved_db.FastaFiles.Count - 1].Name.Equals(_Database._database.FastaFiles[_Database._database.FastaFiles.Count - 1].Name))
                        cancel = true;
                }
                else
                    cancel = true;
            }
            else
            {
                if (_Database._database != null && _Database._database.FastaFiles.Count > 4)
                    cancel = true;
            }

            if (cancel)
            {
                var r = System.Windows.Forms.MessageBox.Show(
                    "Fasta files list is different. If you close the window, you will lose the new ones. Do you want to proceed?",
                    "Q2C :: Warning",
                    System.Windows.Forms.MessageBoxButtons.YesNo,
                    System.Windows.Forms.MessageBoxIcon.Warning);

                if (r != System.Windows.Forms.DialogResult.Yes)
                    e.Cancel = cancel;
            }
            Connection.Refresh_time = 0;
        }
    }
}
