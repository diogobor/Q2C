using Accord.MachineLearning;
using Accord.Statistics.Visualizations;
using Google.Apis.Logging;
using Q2C.Model;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.DataVisualization;
using System.Windows.Controls.DataVisualization.Charting;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Converters;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Controls.DataVisualization.Charting.Primitives;
using OxyPlot.Wpf;
using OxyPlot.Series;
using OxyPlot;
using Accord.Math.Distances;
using Newtonsoft.Json.Linq;
using OxyPlot.Annotations;
using OxyPlot.Axes;
using System.Collections.ObjectModel;
using Q2C.Control.FileManagement;
using ThermoFisher.CommonCore.Data.Business;
using System.Reflection;
using System.Threading;
using System.Security.AccessControl;
using Google.Apis.Gmail.v1.Data;
using SeproPckg2;
using static alglib;
using Q2C.Control.Database;
using Q2C.Control.Statistics;
using DataPoint = Q2C.Control.Statistics.DataPoint;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using Q2C.Control;
using System.Security.RightsManagement;

namespace Q2C.Viewer.Statistics
{
    /// <summary>
    /// Interaction logic for UCStatisticalEvaluation.xaml
    /// </summary>
    public partial class UCStatisticalEvaluation : UserControl
    {
        private DateTime dt_start = DateTime.Now;
        private DateTime dt_end = DateTime.Now;
        private bool exactPeriod = true;
        private bool monthPeriod = false;
        private bool yearPeriod = false;

        public UCStatisticalEvaluation()
        {
            InitializeComponent();
            CreateTabs();
        }

        private void CreateTabs()
        {
            List<Model.Machine> machines = Management.GetMachines().Where(a => a.HasEvaluation).ToList();
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

                var period_groupbox = ProcessData.CreatePeriodDataPicker(machine.Name, 2);
                Grid.SetRow(period_groupbox, 0);
                grid.Children.Add(period_groupbox);

                var faims_no_faims = ProcessData.Create_Faims_NoFaims(machine.Name, machine.HasFAIMS, machine.HasOT, machine.HasIT);
                Grid.SetRow(faims_no_faims, 1);
                grid.Children.Add(faims_no_faims);

                tabItem.Content = grid;
                all_machines_tab.Items.Add(tabItem);
            }
        }

        public (DateTime minDate, DateTime maxDate) GetMinMaxDates()
        {
            List<DateTime> st_dates = new();
            DateTime minDate = DateTime.Now;

            List<DateTime> end_dates = new();
            DateTime maxDate = DateTime.Now;

            List<Model.Machine> machines = Management.GetMachines().Where(a => a.HasEvaluation).ToList();
            if (machines.Count == 0) return (minDate, maxDate);

            foreach (Model.Machine machine in machines)
            {
                DatePicker st_date = (DatePicker)ProcessData.RetrieveElement("stPeriod" + machine.Name, "", "", machine.Name, true, MainUCStatisticalEvaluation, 6);
                if (st_date != null && st_date.SelectedDate != null)
                    st_dates.Add(st_date.SelectedDate.Value);

                DatePicker end_date = (DatePicker)ProcessData.RetrieveElement("endPeriod" + machine.Name, "", "", machine.Name, true, MainUCStatisticalEvaluation, 6);
                if (end_date != null && end_date.SelectedDate != null)
                    end_dates.Add(end_date.SelectedDate.Value);
            }
            minDate = st_dates.Min();
            maxDate = end_dates.Max();
            return (minDate, maxDate);


        }

        public void ResizeGrids(double width, double height, double offset)
        {
            double yPPM = height / 1.7;
            string[] properties = new string[] { "Protein", "Peptide", "PSM", "MSMS", "IDRatio", "XreaMean", "PPM" };
            foreach (var machine in Management.GetMachines().Where(a => a.HasEvaluation))
            {
                int j = 0;
                if (!machine.HasFAIMS)
                    j = 1;
                string _hasFaims = "";
                for (; j < 2; j++)
                {
                    _hasFaims = j == 0 ? "FAIMS" : "NoFAIMS";
                    foreach (string property in properties)
                    {
                        UIElement grid = null;
                        UIElement plot_grid = null;
                        if (machine.HasOT)
                        {
                            //OT
                            grid = (Grid)ProcessData.RetrieveElement("Grid", property, _hasFaims, machine.Name, machine.HasOT, MainUCStatisticalEvaluation, 4);
                            if (grid != null)
                            {
                                ((Grid)grid).Height = height;

                                if (machine.HasIT)
                                    ((Grid)grid).Width = width;
                                else
                                    ((Grid)grid).Width = (width - 20) * 2;
                            }

                            if (property == "PPM")
                            {
                                plot_grid = (OxyPlot.Wpf.PlotView)ProcessData.RetrieveElement("PlotMachineEvaluation", property, _hasFaims, machine.Name, machine.HasOT, MainUCStatisticalEvaluation, 3);
                                if (plot_grid != null)
                                {
                                    if (machine.HasIT)
                                        ((OxyPlot.Wpf.PlotView)plot_grid).Width = width;
                                    else
                                        ((OxyPlot.Wpf.PlotView)plot_grid).Width = (width - 20) * 2;
                                }

                                TextBlock yLabel = (TextBlock)ProcessData.RetrieveElement("ylabel", property, _hasFaims, machine.Name, machine.HasOT, MainUCStatisticalEvaluation, 2);
                                if (yLabel != null)
                                    yLabel.Margin = new Thickness(0, yPPM, -70, 0);
                            }
                            else
                            {
                                plot_grid = (Grid)ProcessData.RetrieveElement("PlotMachineEvaluation", property, _hasFaims, machine.Name, machine.HasOT, MainUCStatisticalEvaluation, 0);
                                if (plot_grid != null)
                                {
                                    if (machine.HasIT)
                                        ((Grid)plot_grid).Width = width;
                                    else
                                        ((Grid)plot_grid).Width = (width - 20) * 2;
                                }
                            }
                        }
                        if (machine.HasIT)
                        {
                            //IT
                            grid = (Grid)ProcessData.RetrieveElement("Grid", property, _hasFaims, machine.Name, false, MainUCStatisticalEvaluation, 4);
                            if (grid != null)
                            {
                                ((Grid)grid).Height = height;

                                if (machine.HasOT)
                                    ((Grid)grid).Width = width - offset;
                                else
                                    ((Grid)grid).Width = (width - 20) * 2;
                            }
                            if (property == "PPM")
                            {
                                plot_grid = (OxyPlot.Wpf.PlotView)ProcessData.RetrieveElement("PlotMachineEvaluation", property, _hasFaims, machine.Name, false, MainUCStatisticalEvaluation, 3);
                                if (plot_grid != null)
                                {
                                    if (machine.HasOT)
                                        ((OxyPlot.Wpf.PlotView)plot_grid).Width = width - offset;
                                    else
                                        ((OxyPlot.Wpf.PlotView)plot_grid).Width = (width - 20) * 2;
                                }

                                TextBlock yLabel = (TextBlock)ProcessData.RetrieveElement("ylabel", property, _hasFaims, machine.Name, false, MainUCStatisticalEvaluation, 2);
                                if (yLabel != null)
                                    yLabel.Margin = new Thickness(0, yPPM, -70, 0);
                            }
                            else
                            {
                                plot_grid = (Grid)ProcessData.RetrieveElement("PlotMachineEvaluation", property, _hasFaims, machine.Name, false, MainUCStatisticalEvaluation, 0);
                                if (plot_grid != null)
                                {
                                    if (machine.HasOT)
                                        ((Grid)plot_grid).Width = width - offset;
                                    else
                                        ((Grid)plot_grid).Width = (width - 20) * 2;
                                }
                            }
                        }
                    }
                }
            }
        }

    }
}
