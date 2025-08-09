using Q2C.Control.Database;
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

namespace Q2C.Viewer.Statistics
{
    /// <summary>
    /// Interaction logic for WindowStatisticalQueue.xaml
    /// </summary>
    public partial class WindowStatisticalQueue : Window
    {
        public WindowStatisticalQueue()
        {
            InitializeComponent();
            Connection.Refresh_time = int.MinValue;
            this.SizeChanged += WindowStatisticalQueue_SizeChanged;
        }
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Connection.Refresh_time = 0;
        }
        private void WindowStatisticalQueue_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateWindowSize();
        }
        private void UpdateWindowSize()
        {
            double height = this.ActualHeight - 180;
            double width = this.ActualWidth;
            _StatiscalQueue.ResizeGrids(width, height);
        }
    }
}
