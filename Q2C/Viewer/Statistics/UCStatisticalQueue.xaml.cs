using Q2C.Control;
using Q2C.Control.Database;
using Q2C.Control.Statistics;
using Q2C.Model;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.DataVisualization;
using System.Windows.Controls.DataVisualization.Charting;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Xceed.Wpf.AvalonDock.Controls;

namespace Q2C.Viewer.Statistics
{
    /// <summary>
    /// Interaction logic for UCStatisticalQueue.xaml
    /// </summary>
    public partial class UCStatisticalQueue : System.Windows.Controls.UserControl
    {
        public UCStatisticalQueue()
        {
            InitializeComponent();
            CreateTabs();
        }
        private void CreateTabs()
        {
            List<Model.Machine> machines = Management.GetMachines().ToList();
            if (machines.Count == 0) return;
            foreach (var machine in machines)
            {
                TabItem tabItem = new TabItem();
                tabItem.Header = machine.Name;

                Grid grid = new Grid();
                grid.Name = "grid_machines_" + machine.Name;
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                var period_groupbox = ProcessData.CreatePeriodDataPicker(machine.Name, 1);
                Grid.SetRow(period_groupbox, 0);
                grid.Children.Add(period_groupbox);

                var plot_tabs = ProcessData.Create_QueuePlots(machine.Name);
                Grid.SetRow(plot_tabs, 1);
                grid.Children.Add(plot_tabs);

                tabItem.Content = grid;
                all_machines_tab.Items.Add(tabItem);
            }
        }
        public void ResizeGrids(double width, double height)
        {
            string[] queue_property = new string[] { "PerPeriod", "PerUser" };
            foreach (var machine in Management.GetMachines())
            {
                foreach (var property in queue_property)
                {
                    string datagrid_name = "PlotQueue" + property + machine.Name;
                    Grid grid = (Grid)ProcessData.RetrieveElement(datagrid_name, "", property, machine.Name, true, MainUCStatisticalQueue, 7);
                    if (grid != null)
                    {
                        ((Grid)grid).Height = height;
                        ((Grid)grid).Width = width;

                        if (grid.Children.Count > 0)
                        {
                            Chart plot = (Chart)grid.Children[0];
                            if (plot != null)
                            {
                                plot.Height = height;
                                plot.Width = width;
                            }
                        }
                    }

                }
            }
        }
    }
}
