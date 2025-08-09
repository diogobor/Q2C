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

namespace Q2C.Viewer.Project
{
    /// <summary>
    /// Interaction logic for WindowMethod.xaml
    /// </summary>
    public partial class WindowMethod : Window
    {
        private Window _window;
        public WindowMethod(Window window)
        {
            InitializeComponent();
            _window = window;
            _UCMethod.Load();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            ((WindowProject)_window).UpdateMethods();
        }
    }
}
