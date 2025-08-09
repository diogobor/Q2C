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

namespace Q2C.Viewer.Setup
{
    /// <summary>
    /// Interaction logic for WindowFastaFiles.xaml
    /// </summary>
    public partial class WindowFastaFiles : Window
    {
        private Window _window;
        private Database _database;
        public WindowFastaFiles(Window window, Database database)
        {
            InitializeComponent();
            _window = window;
            _database = database;
            _UCFastaFiles.Load(this, _database);
        }

        public void UpdateDatabase(Database database)
        {
            _database = database;
        }
        private void Window_Closed(object sender, EventArgs e)
        {
            ((WindowDatabase)_window).UpdateFastaFiles(_database);
        }
    }
}
