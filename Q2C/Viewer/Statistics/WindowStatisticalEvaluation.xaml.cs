using Newtonsoft.Json.Linq;
using Q2C.Control.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Shapes;

namespace Q2C.Viewer.Statistics
{
    /// <summary>
    /// Interaction logic for WindowStatisticsEvaluation.xaml
    /// </summary>
    public partial class WindowStatisticalEvaluation : Window
    {
        public WindowStatisticalEvaluation()
        {
            InitializeComponent();
            Connection.Refresh_time = int.MinValue;
            this.SizeChanged += WindowStatisticalEvaluation_SizeChanged;
            this.DataContext = new WindowMainCommandContext(this);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Connection.Refresh_time = 0;
        }

        private void WindowStatisticalEvaluation_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateWindowSize();
        }

        private void UpdateWindowSize()
        {
            double height = this.ActualHeight - 230;
            double width = this.ActualWidth / 2 - 20;
            double offset = 40;
            _MachinesEvaluation.ResizeGrids(width, height, offset);
        }

        private void MenuItemClose_Click(object sender, RoutedEventArgs e)
        {
            new CloseWindowKey(this).Execute(sender);
        }

        private void MenuItem_Report(object sender, RoutedEventArgs e)
        {
            new ReportKey(this).Execute(sender);
        }

        private void MenuItem_ReadMe(object sender, RoutedEventArgs e)
        {
            new ReadMeKey().Execute(sender);
        }

        private void MenuItem_About(object sender, RoutedEventArgs e)
        {
            new AboutKey().Execute(sender);
        }

    }

    internal class CloseWindowKey : ICommand
    {
        private WindowStatisticalEvaluation _window;
        public event EventHandler CanExecuteChanged;

        public CloseWindowKey(WindowStatisticalEvaluation window)
        {
            _window = window;
        }

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            if (_window != null)
                _window.Close();
        }
    }

    internal class ReportKey : ICommand
    {
        private WindowStatisticalEvaluation _window;

        public ReportKey(WindowStatisticalEvaluation window)
        {
            _window = window;
        }
        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            (DateTime minDate, DateTime maxDate) = GetMinMaxDates();

            var _windowReport = new WindowReport();
            _windowReport.Load(_window, minDate, maxDate);
            _windowReport.ShowDialog();
        }

        private (DateTime minDate, DateTime maxDate) GetMinMaxDates()
        {
            return _window._MachinesEvaluation.GetMinMaxDates();
        }
    }

    public class WindowMainCommandContext
    {
        private WindowStatisticalEvaluation _window;

        public WindowMainCommandContext(WindowStatisticalEvaluation window)
        {
            _window = window;
        }

        public WindowMainCommandContext() { }
        public ICommand CloseWindowCommand
        {
            get
            {
                return new CloseWindowKey(_window);
            }
        }

        public ICommand ReportCommand
        {
            get
            {
                return new ReportKey(_window);
            }
        }

        public ICommand ReadMeCommand
        {
            get
            {
                return new ReadMeKey();
            }
        }
    }
}
