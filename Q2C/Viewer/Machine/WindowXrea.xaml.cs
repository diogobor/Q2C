using OxyPlot.Axes;
using OxyPlot.Legends;
using OxyPlot;
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
using OxyPlot.Series;
using System.Security.Policy;
using Newtonsoft.Json.Linq;
using Q2C.Control.QualityControl;
using Q2C.Control.Database;
using Q2C.Model;

namespace Q2C.Viewer.Machine
{
    /// <summary>
    /// Interaction logic for WindowXrea.xaml
    /// </summary>
    public partial class WindowXrea : Window
    {
        public WindowXrea()
        {
            InitializeComponent();
            Setup();
        }

        private void Setup()
        {
            Connection.Refresh_time = int.MinValue;

            PlotModel plotModel = new();

            plotModel.Legends.Add(new Legend()
            {
                LegendPlacement = LegendPlacement.Inside,
                LegendPosition = LegendPosition.LeftTop,
                ShowInvisibleSeries = false
            });

            var linearAxis1 = new LinearAxis
            {
                Title = "Xrea",
                Minimum = 0,
                IsZoomEnabled = false,
                Position = AxisPosition.Left,
                TitleFontSize = 15,
                FontSize = 15
            };
            plotModel.Axes.Add(linearAxis1);

            var linearAxis2 = new LinearAxis
            {
                Title = "Retention time (min)",
                Position = AxisPosition.Bottom,
                Minimum = 0,
                TitleFontSize = 15,
                FontSize = 15
            };
            plotModel.Axes.Add(linearAxis2);

            PlotViewXrea.Model = plotModel;
        }

        public void Load(List<XreaEntry> xreas)
        {
            if (xreas == null) return;
            Plot(xreas, OxyColor.FromAColor(25, OxyColor.FromArgb(150, 0, 90, 106)));
            Plot(Xrea.SmoothAreas(xreas), OxyColor.FromArgb(150, 0, 90, 106));
            ResetXAreaAxes();
        }
        private bool Plot(List<XreaEntry> points, OxyColor colour, string toolTip = null, string tag = null, bool isVisible = true)
        {
            if (points == null || points.Count == 0)
                return false;
            LineSeries lineSeries = new LineSeries()
            {
                Color = colour,
                IsVisible = isVisible
            };
            lineSeries.Points.AddRange(points.Select(a => new DataPoint(a.rt, a.xrea)));

            PlotViewXrea.Model.Series.Add(lineSeries);
            PlotViewXrea.Model.InvalidatePlot(true);

            return true;
        }
        private void ResetXAreaAxes()
        {
            double minXrea = 0d;
            double minTime = 0d;

            double maxXrea = 0d;
            double maxTime = 0d;

            var series = PlotViewXrea.Model.Series.Where(a => a.IsVisible == true);

            minXrea = series.Min(b => (b as LineSeries).MinY);
            minTime = series.Min(b => (b as LineSeries).MinX);

            maxXrea = series.Max(b => (b as LineSeries).MaxY);
            maxTime = series.Max(b => (b as LineSeries).MaxX);

            PlotViewXrea.Model.ResetAllAxes();
            PlotViewXrea.Model.Axes[0].Maximum = maxXrea;
            PlotViewXrea.Model.Axes[0].Minimum = minXrea;

            PlotViewXrea.Model.Axes[1].Maximum = maxTime;
            PlotViewXrea.Model.Axes[1].Minimum = minTime;

            PlotViewXrea.Model.ResetAllAxes();

            PlotViewXrea.Model.InvalidatePlot(true);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Connection.Refresh_time = 0;
        }
    }
}
