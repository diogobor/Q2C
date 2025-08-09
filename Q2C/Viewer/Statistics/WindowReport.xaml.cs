using Accord.Math.Distances;
using Newtonsoft.Json.Linq;
using OxyPlot.Wpf;
using PatternTools.RBFClassifier;
using PdfSharp.Charting;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using Q2C.Control;
using Q2C.Control.Database;
using Q2C.Control.Statistics;
using Q2C.Model;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Security.AccessControl;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.DataVisualization.Charting;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Xml.Linq;
using ThermoFisher.CommonCore.Data;
using static alglib;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using Chart = System.Windows.Controls.DataVisualization.Charting.Chart;
using DataPoint = Q2C.Control.Statistics.DataPoint;
using Machine = Q2C.Model.Machine;
using Window = System.Windows.Window;

namespace Q2C.Viewer.Statistics
{
    /// <summary>
    /// Interaction logic for WindowReport.xaml
    /// </summary>
    public partial class WindowReport : Window
    {
        private DateTime dt_start = DateTime.Now;
        private DateTime dt_end = DateTime.Now;
        private bool exactPeriodBool = true;
        private bool monthPeriodBool = false;
        private bool yearPeriodBool = false;
        private bool autoScale_bool = false;
        private WindowStatisticalEvaluation _statisticalEvaluation_window;
        private Dictionary<string, ((List<DataPoint>, (List<DataPoint>, List<DataPoint>, List<DataPoint>), List<DataPoint>, (List<DataPoint>, List<DataPoint>, List<DataPoint>)), double, double)> dict_points = new();
        public WindowReport()
        {
            InitializeComponent();
        }

        private void CreateMachines()
        {
            if (!Management.HasMachine()) return;

            var selected_machines = Management.GetMachines().Where(a => a.HasEvaluation).ToList();

            Grid grid = new Grid();
            for (int i = 0; i < selected_machines.Count; i++)
                grid.RowDefinitions.Add(new RowDefinition());

            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(75) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(40) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(110) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(40) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            for (int i = 0; i < selected_machines.Count; i++)
            {
                Q2C.Model.Machine machine = selected_machines[i];
                System.Windows.Controls.CheckBox cb_machine = new System.Windows.Controls.CheckBox();
                cb_machine.IsChecked = true;
                cb_machine.Click += Cb_machine_Click;
                cb_machine.Checked += Cb_machine_Checked;
                cb_machine.Unchecked += Cb_machine_Unchecked;
                cb_machine.Name = "cb_machine_" + machine.Name;
                cb_machine.Content = machine.Name;
                grid.Children.Add(cb_machine);
                Grid.SetRow(cb_machine, i);
                Grid.SetColumn(cb_machine, 0);

                TextBlock tb_faims = new();
                tb_faims.Text = "FAIMS:";
                grid.Children.Add(tb_faims);
                Grid.SetRow(tb_faims, i);
                Grid.SetColumn(tb_faims, 1);

                System.Windows.Controls.ComboBox cbFaims = new System.Windows.Controls.ComboBox();
                cbFaims.Name = "cbFaims_" + machine.Name;
                if (machine.HasFAIMS)
                {
                    cbFaims.Items.Add("Both");
                    cbFaims.Items.Add("Only with");
                    cbFaims.Items.Add("Only without");
                }
                else
                    cbFaims.Items.Add("Only without");

                cbFaims.SelectedIndex = 0;
                cbFaims.Width = 100;
                cbFaims.Margin = new Thickness(5, -2, 5, 5);
                grid.Children.Add(cbFaims);
                Grid.SetRow(cbFaims, i);
                Grid.SetColumn(cbFaims, 2);

                TextBlock tb_otit = new();
                tb_otit.Text = "OT/IT:";
                grid.Children.Add(tb_otit);
                Grid.SetRow(tb_otit, i);
                Grid.SetColumn(tb_otit, 3);

                System.Windows.Controls.ComboBox cbOTIT = new System.Windows.Controls.ComboBox();
                cbOTIT.Name = "cbOTIT_" + machine.Name;
                if (machine.HasOT && machine.HasIT)
                    cbOTIT.Items.Add("Both");
                if (machine.HasOT)
                    cbOTIT.Items.Add("Only OT");
                if (machine.HasIT)
                    cbOTIT.Items.Add("Only IT");


                cbOTIT.SelectedIndex = 0;
                cbOTIT.Width = 80;
                cbOTIT.Margin = new Thickness(0, -2, 5, 5);
                grid.Children.Add(cbOTIT);
                Grid.SetRow(cbOTIT, i);
                Grid.SetColumn(cbOTIT, 4);

            }

            All_machines_grid.Children.Add(grid);

            this.Height += (selected_machines.Count * 30);
        }

        public void Load(WindowStatisticalEvaluation statisticalEvaluation_window, DateTime st_date, DateTime end_date)
        {
            stPeriod.SelectedDate = st_date;
            endPeriod.SelectedDate = end_date;
            _statisticalEvaluation_window = statisticalEvaluation_window;
            CreateMachines();
        }

        private void CreateBoxPlot(Canvas canvas,
            ((List<DataPoint> pointsOT,
            (List<DataPoint> ransac, List<DataPoint> ransacMinusSigma, List<DataPoint> ransacPlusSigma) ransacPointsOT,
            List<DataPoint> pointsIT,
            (List<DataPoint> ransac, List<DataPoint> ransacMinusSigma, List<DataPoint> ransacPlusSigma) ransacPointsIT) points,
            double avgOT, double avgIT) data,
            bool isFAIMS,
            bool hasOT = true,
            bool hasIT = true,
            bool hasYlimit = true)
        {
            var plotModels = ProcessData.CreateBoxPlots(
                     data.points.pointsOT,
                     data.points.pointsIT,
                     isFAIMS,
                     hasYlimit);

            RotateTransform rotate = new RotateTransform(-90);

            if (hasOT)
            {
                #region OT
                PlotView _plotViewOT = new PlotView();
                _plotViewOT.Model = plotModels.plotModelOT;
                if (hasIT == false)
                    _plotViewOT.Width = 1382;
                else
                    _plotViewOT.Width = 691;
                _plotViewOT.Height = 440;
                Canvas.SetLeft(_plotViewOT, 10);
                Canvas.SetTop(_plotViewOT, -10);
                canvas.Children.Add(_plotViewOT);

                TextBlock legendOT = new TextBlock();
                legendOT.Text = "Mass error (ppm)";
                legendOT.RenderTransform = rotate;
                Canvas.SetLeft(legendOT, 0);
                Canvas.SetTop(legendOT, 255);
                canvas.Children.Add(legendOT);

                #endregion
            }

            if (hasIT)
            {
                #region IT
                PlotView _plotViewIT = new PlotView();
                _plotViewIT.Model = plotModels.plotModelIT;
                if (hasOT == false)
                {
                    _plotViewIT.Width = 1382;
                    Canvas.SetLeft(_plotViewIT, 10);
                }
                else
                {
                    _plotViewIT.Width = 691;
                    Canvas.SetLeft(_plotViewIT, 710);
                }
                _plotViewIT.Height = 440;
                Canvas.SetTop(_plotViewIT, -10);
                canvas.Children.Add(_plotViewIT);

                TextBlock legendIT = new TextBlock();
                legendIT.Text = "Mass error (ppm)";
                legendIT.RenderTransform = rotate;
                canvas.Children.Add(legendIT);
                if (hasOT == false)
                    Canvas.SetLeft(legendIT, 0);
                else
                    Canvas.SetLeft(legendIT, 700);
                Canvas.SetTop(legendIT, 255);
                #endregion
            }
        }

        private void CreateScatterPlot(Canvas canvas,
            ((List<DataPoint> pointsOT,
            (List<DataPoint> ransac, List<DataPoint> ransacMinusSigma, List<DataPoint> ransacPlusSigma) ransacPointsOT,
            List<DataPoint> pointsIT,
            (List<DataPoint> ransac, List<DataPoint> ransacMinusSigma, List<DataPoint> ransacPlusSigma) ransacPointsIT) points,
            double avgOT, double avgIT) data,
            string yText,
            bool hasOT = true,
            bool hasIT = true,
            bool hasYlimit = true,
            double minimumYAxis = 0,
            double maximumYAxis = 100)
        {

            if (hasOT)
            {
                #region OT

                System.Windows.Controls.TextBlock avgTbOT = new System.Windows.Controls.TextBlock();
                avgTbOT.Text = "Average: " + data.avgOT;
                avgTbOT.TextAlignment = TextAlignment.Right;
                canvas.Children.Add(avgTbOT);
                if (hasIT == false)
                    Canvas.SetLeft(avgTbOT, 1285);
                else
                    Canvas.SetLeft(avgTbOT, 605);
                Canvas.SetTop(avgTbOT, 0);

                Chart chartOT = ProcessData.CreateScatterChart("chart_OT", data.points.pointsOT, data.points.ransacPointsOT.ransac, data.points.ransacPointsOT.ransacMinusSigma, data.points.ransacPointsOT.ransacPlusSigma, yText, hasYlimit, minimumYAxis, maximumYAxis);
                chartOT.Height = 442;
                if (hasIT == false)
                    chartOT.Width = 1382;
                else
                    chartOT.Width = 691;

                Canvas.SetTop(chartOT, -20);
                canvas.Children.Add(chartOT);

                StackPanel created_legendOT = ProcessData.CreateLegend(chartOT, "panel_OT");
                created_legendOT.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
                created_legendOT.Orientation = System.Windows.Controls.Orientation.Horizontal;
                if (hasIT == false)
                    Canvas.SetLeft(created_legendOT, 600);
                else
                    Canvas.SetLeft(created_legendOT, 290);
                Canvas.SetTop(created_legendOT, 405);
                canvas.Children.Add(created_legendOT);
                #endregion
            }
            if (hasIT)
            {
                #region IT

                System.Windows.Controls.TextBlock avgTbIT = new System.Windows.Controls.TextBlock();
                avgTbIT.Text = "Average: " + data.avgIT;
                avgTbIT.TextAlignment = TextAlignment.Right;
                canvas.Children.Add(avgTbIT);
                if (hasOT == false)
                    Canvas.SetLeft(avgTbIT, 1285);
                else
                    Canvas.SetLeft(avgTbIT, 1305);
                Canvas.SetTop(avgTbIT, 0);

                Chart chartIT = ProcessData.CreateScatterChart("chart_IT", data.points.pointsIT, data.points.ransacPointsIT.ransac, data.points.ransacPointsIT.ransacMinusSigma, data.points.ransacPointsIT.ransacPlusSigma, yText, hasYlimit, minimumYAxis, maximumYAxis);
                chartIT.Height = 442;
                if (hasOT == false)
                    chartIT.Width = 1382;
                else
                {
                    chartIT.Width = 691;
                    Canvas.SetLeft(chartIT, 700);
                }
                Canvas.SetTop(chartIT, -20);
                canvas.Children.Add(chartIT);

                StackPanel created_legendIT = ProcessData.CreateLegend(chartIT, "panel_IT");
                created_legendIT.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
                created_legendIT.Orientation = System.Windows.Controls.Orientation.Horizontal;
                if (hasOT == false)
                    Canvas.SetLeft(created_legendIT, 600);
                else
                    Canvas.SetLeft(created_legendIT, 990);
                Canvas.SetTop(created_legendIT, 405);
                canvas.Children.Add(created_legendIT);
                #endregion
            }
        }

        [STAThread]
        private void CallTaskRun(string fileName, List<(string machine, byte faims, byte ot_it)> selected_machines)
        {
            Connection.Refresh_time = int.MinValue;
            dict_points.Clear();
            foreach (string machine in selected_machines.Select(a => a.Item1).ToList())
            {
                ProcessData.ComputeAllEvaluationDataPoints(machine, dt_start, dt_end, exactPeriodBool, monthPeriodBool, yearPeriodBool);
                foreach (var item in ProcessData.Dict_Evaluation_points)
                    dict_points.Add(item.Key, item.Value);
            }

            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                ProcessData.Dict_Evaluation_points.Clear();
                //this.Hide();
                PdfDocument document = null;
                XGraphics gfx = null;
                byte returnAnswer = Control.PDFExporter.CreatePDF(out document, out gfx);

                if (returnAnswer == 0)
                    returnAnswer = CreatePDFFile(fileName, selected_machines, document, gfx, returnAnswer);

                if (returnAnswer == 0)
                {
                    System.Windows.Forms.MessageBox.Show("Plots have been exported successfully!", "Q2C :: Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    Control.PDFExporter.OpenFile(fileName);
                    this.Close();
                }
                else if (returnAnswer == 1)
                    System.Windows.Forms.MessageBox.Show("Export data failed!", "Q2C :: Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                ProcessData.Dict_Evaluation_points.Clear();
                dict_points.Clear();
                Connection.Refresh_time = 0;
            });

        }

        private byte CreatePDFFile(string fileName,
            List<(string machine, byte faims, byte ot_it)> selected_machines,
            PdfDocument document,
            XGraphics gfx,
            byte returnAnswer)
        {
            var _new_window = new Window();
            _new_window.WindowState = WindowState.Maximized;

            ((List<DataPoint> pointsOT,
            (List<DataPoint> ransac, List<DataPoint> ransacMinusSigma, List<DataPoint> ransacPlusSigma) ransacPointsOT,
            List<DataPoint> pointsIT,
            (List<DataPoint> ransac, List<DataPoint> ransacMinusSigma, List<DataPoint> ransacPlusSigma) ransacPointsIT) points,
            double avgOT, double avgIT) data;
            Canvas canvas = new Canvas();
            List<string> properties = new() { "Protein", "Peptide", "PSM", "MSMS", "IDRatio", "PPM" };
            string[] yTexts = new string[] { "# Protein Groups", "# Peptide Groups", "# PSMs", "# MS/MS", "ID Ratio (PSM / MSMS)" };
            int y_offset = 110;

            for (int i = 0; i < selected_machines.Count; i++)
            {
                int total_faims = 2;
                int k = 0;
                if (selected_machines[i].faims == 0)//both: FAIMS and NoFAIMS
                {
                    k = 0;
                    total_faims = 2;
                }
                else if (selected_machines[i].faims == 1)//FAIMS
                {
                    k = 0;
                    total_faims = 1;
                }
                else if (selected_machines[i].faims == 2)//NoFAIMS
                {
                    k = 1;
                    total_faims = 2;
                }

                for (; k < total_faims; k++)
                {
                    string faims = k == 0 ? "FAIMS" : "NoFAIMS";
                    string machine = selected_machines[i].machine;
                    bool hasYlimit = (autoScale_bool == false);

                    // Create an empty page
                    returnAnswer = Control.PDFExporter.AddNewPage(document, out gfx);
                    y_offset = 110;
                    if (selected_machines[i].ot_it == 0)//OT and IT
                        Control.PDFExporter.AddOTITTitle(gfx);
                    else if (selected_machines[i].ot_it == 1)//Only OT
                        Control.PDFExporter.AddOTITTitle(gfx, true, false);
                    else if (selected_machines[i].ot_it == 2)//Only IT
                        Control.PDFExporter.AddOTITTitle(gfx, false, true);

                    Control.PDFExporter.AddMachineTitle(machine, (k == 0 ? "FAIMS" : "No FAIMS"), gfx);

                    for (int j = 0; j < properties.Count; j++)
                    {
                        string property = properties[j];

                        if (property == "MSMS")
                        {
                            // Create an empty page
                            returnAnswer = Control.PDFExporter.AddNewPage(document, out gfx);
                            Control.PDFExporter.AddMachineTitle(machine, (k == 0 ? "FAIMS" : "No FAIMS"), gfx);
                            if (selected_machines[i].ot_it == 0)//OT and IT
                                Control.PDFExporter.AddOTITTitle(gfx);
                            else if (selected_machines[i].ot_it == 1)//Only OT
                                Control.PDFExporter.AddOTITTitle(gfx, true, false);
                            else if (selected_machines[i].ot_it == 2)//Only IT
                                Control.PDFExporter.AddOTITTitle(gfx, false, true);

                            y_offset = 110;
                        }

                        string _key = property + "#" + faims + "#" + machine;
                        if (dict_points.TryGetValue(_key, out data))
                        {
                            double minimumYAxis = 0;
                            double maximumYAxis = 0;

                            if (autoScale_bool == false)
                            {
                                if (hasYlimit == true)
                                {
                                    if (property == "IDRatio")
                                    {
                                        minimumYAxis = 0;
                                        maximumYAxis = 100;
                                    }
                                    else
                                    {
                                        if ((data.points.pointsOT != null && data.points.pointsOT.Count > 0 &&
                                        data.points.pointsIT != null && data.points.pointsIT.Count > 0))
                                        {
                                            minimumYAxis = Math.Min(data.points.ransacPointsOT.ransacMinusSigma.Min(a => a.Y), data.points.ransacPointsIT.ransacMinusSigma.Min(a => a.Y));
                                            var minPoints = Math.Min(data.points.pointsOT.Min(a => a.Y), data.points.pointsIT.Min(a => a.Y));
                                            minimumYAxis = Math.Min(minimumYAxis, minPoints);

                                            maximumYAxis = Math.Max(data.points.ransacPointsOT.ransacPlusSigma.Max(a => a.Y), data.points.ransacPointsIT.ransacPlusSigma.Max(a => a.Y));
                                            var maxPoints = Math.Max(data.points.pointsOT.Max(a => a.Y), data.points.pointsIT.Max(a => a.Y));
                                            maximumYAxis = Math.Max(maximumYAxis, maxPoints);

                                            minimumYAxis = minimumYAxis > 0 ? minimumYAxis - 10 > 0 ? minimumYAxis - 10 : minimumYAxis : 0;
                                            maximumYAxis += 10;
                                        }
                                        else hasYlimit = false;
                                    }
                                }
                            }
                            canvas.Children.Clear();

                            if (property == "PPM")
                            {
                                if (selected_machines[i].ot_it == 0)//OT and IT
                                    CreateBoxPlot(canvas, data, k == 0,true,true, hasYlimit);
                                else if (selected_machines[i].ot_it == 1)//Only OT
                                    CreateBoxPlot(canvas, data, (k == 0), true, false, hasYlimit);
                                else if (selected_machines[i].ot_it == 2)//Only IT
                                    CreateBoxPlot(canvas, data, (k == 0), false, true, hasYlimit);
                            }
                            else
                            {
                                if (selected_machines[i].ot_it == 0)//OT and IT
                                    CreateScatterPlot(canvas, data, yTexts[j], true, true, hasYlimit, minimumYAxis, maximumYAxis);
                                else if (selected_machines[i].ot_it == 1)//Only OT
                                    CreateScatterPlot(canvas, data, yTexts[j], true, false, hasYlimit, minimumYAxis, maximumYAxis);
                                else if (selected_machines[i].ot_it == 2)//Only IT
                                    CreateScatterPlot(canvas, data, yTexts[j], false, true, hasYlimit, minimumYAxis, maximumYAxis);
                            }

                            _new_window.Content = canvas;
                            _new_window.InvalidateVisual();
                            _new_window.UpdateLayout();
                            _new_window.Show();

                            var renderTargetBitMapOT = new RenderTargetBitmap((int)4400, (int)1340, 300d, 300d, System.Windows.Media.PixelFormats.Default);
                            renderTargetBitMapOT.Render(_new_window);
                            BitmapEncoder pngEnconderOT = new PngBitmapEncoder();
                            pngEnconderOT.Frames.Add(BitmapFrame.Create(renderTargetBitMapOT));
                            using (var msPlot = new MemoryStream())
                            {
                                pngEnconderOT.Save(msPlot);
                                XImage imgPlot = XImage.FromStream(msPlot);
                                if (property == "PPM")
                                    gfx.DrawImage(imgPlot, 20, y_offset, 560, 190);
                                else
                                    gfx.DrawImage(imgPlot, 15, y_offset, 560, 190);

                            }
                            _new_window.Hide();
                            y_offset += 210;
                        }
                    }
                }
            }

            Control.PDFExporter.ClosePDF(fileName, document);
            _new_window.Close();

            return returnAnswer;
        }

        private List<(string, byte, byte)> GetSelectedMachine()
        {
            List<(string, byte, byte)> selected_machines = new();
            if (All_machines_grid.Children[0] is Grid innerGrid)
            {
                string checkBox_name = string.Empty;
                bool is_current_cb_checked = false;
                byte FAIMS = 0;
                byte OTIT = 0;
                foreach (UIElement child in innerGrid.Children)
                {
                    if (child is System.Windows.Controls.CheckBox checkbox)
                    {
                        if (!String.IsNullOrEmpty(checkBox_name) && is_current_cb_checked == true)
                            selected_machines.Add((checkBox_name, FAIMS, OTIT));

                        is_current_cb_checked = false;
                        FAIMS = 0;
                        OTIT = 0;

                        checkBox_name = checkbox.Name.Replace("cb_machine_", "");
                        is_current_cb_checked = checkbox.IsChecked == true;
                    }

                    if (!is_current_cb_checked) continue;

                    if (child is System.Windows.Controls.ComboBox combobox)
                    {
                        string name = combobox.Name.Replace("cbFaims_", "");
                        if (checkBox_name.Equals(name))
                        {
                            var selectedItem = combobox.SelectedItem;
                            switch (selectedItem)
                            {
                                case "Both":
                                    FAIMS = 0;
                                    break;
                                case "Only with":
                                    FAIMS = 1;
                                    break;
                                case "Only without":
                                    FAIMS = 2;
                                    break;
                                default: break;
                            }
                        }

                        name = combobox.Name.Replace("cbOTIT_", "");
                        if (checkBox_name.Equals(name))
                        {
                            var selectedItem = combobox.SelectedItem;
                            switch (selectedItem)
                            {
                                case "Both":
                                    OTIT = 0;
                                    break;
                                case "Only OT":
                                    OTIT = 1;
                                    break;
                                case "Only IT":
                                    OTIT = 2;
                                    break;
                                default: break;
                            }
                        }
                    }
                }

                //Add last machine
                if (!String.IsNullOrEmpty(checkBox_name) && is_current_cb_checked == true)
                    selected_machines.Add((checkBox_name, FAIMS, OTIT));
            }
            return selected_machines;
        }

        private async void ButtonConfirm_Click(object sender, RoutedEventArgs e)
        {
            //List<(machine,
            //faims->0:both;1:with only; 2: without onlyt,
            //ot/it-> 0: both; 1: OT; 2: IT)>
            List<(string machine, byte faims, byte ot_it)> selected_machines = GetSelectedMachine();

            if (selected_machines.Count == 0)
            {
                System.Windows.Forms.MessageBox.Show("Please select at least one machine!", "Q2C :: Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (stPeriod.SelectedDate == null || stPeriod.SelectedDate.Value == null ||
                endPeriod.SelectedDate == null || endPeriod.SelectedDate.Value == null)
            {
                System.Windows.Forms.MessageBox.Show("Please select a date!", "Q2C :: Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
            dlg.FileName = "q2c_machines_evalutation.pdf"; // Default file name
            dlg.Filter = "PDF file (*.pdf)|*.pdf"; // Filter files by extension
            dlg.Title = "Q2C :: Machine evaluations :: Report";

            // Show open file dialog box
            Nullable<bool> result = dlg.ShowDialog();

            if (result == true)
            {
                dt_start = stPeriod.SelectedDate.Value;
                dt_end = endPeriod.SelectedDate.Value;
                exactPeriodBool = exactPeriod.IsChecked == true;
                monthPeriodBool = monthPeriod.IsChecked == true;
                yearPeriodBool = yearPeriod.IsChecked == true;
                autoScale_bool = autoScale.IsChecked == true;

                var wait_screen = Util.Util.CallWaitWindow("Please wait...", "Generating plots...");
                wait_screen.Height = 320;
                MainGrid.Children.Add(wait_screen);

                await Task.Run(() => CallTaskRun(dlg.FileName, selected_machines));

                MainGrid.Children.Remove(wait_screen);

                ProcessData.Dict_Evaluation_points.Clear();

            }
        }

        private void Window_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                ButtonConfirm_Click(null, null);
            }
        }

        private void All_machines_cb_Click(object sender, RoutedEventArgs e)
        {
            bool isAllChecked = All_machines_cb.IsChecked == true;
            if (All_machines_grid.Children[0] is Grid innerGrid)
            {
                foreach (UIElement child in innerGrid.Children)
                {
                    if (child is System.Windows.Controls.CheckBox checkbox)
                        checkbox.IsChecked = isAllChecked;
                }
            }
        }
        private void Cb_machine_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Controls.CheckBox current_cb = (System.Windows.Controls.CheckBox)(e.Source);
            if (current_cb.IsChecked == true)
                Cb_machine_Checked(sender, e);
            else
                Cb_machine_Unchecked(sender, e);

        }

        private void Cb_machine_Unchecked(object sender, RoutedEventArgs e)
        {
            System.Windows.Controls.CheckBox current_cb = (System.Windows.Controls.CheckBox)(e.Source);
            string current_machine = current_cb.Name.Replace("cb_machine_", "");
            if (All_machines_grid.Children[0] is Grid innerGrid)
            {
                foreach (UIElement child in innerGrid.Children)
                {
                    if (child is System.Windows.Controls.ComboBox combobox)
                    {
                        string name = combobox.Name.Replace("cbFaims_", "");
                        if (current_machine.Equals(name))
                            combobox.IsEnabled = false;

                        name = combobox.Name.Replace("cbOTIT_", "");
                        if (current_machine.Equals(name))
                            combobox.IsEnabled = false;
                    }
                }
            }
            All_machines_cb.IsChecked = false;
        }

        private void Cb_machine_Checked(object sender, RoutedEventArgs e)
        {
            System.Windows.Controls.CheckBox current_cb = (System.Windows.Controls.CheckBox)(e.Source);
            string current_machine = current_cb.Name.Replace("cb_machine_", "");
            if (All_machines_grid.Children[0] is Grid innerGrid)
            {
                foreach (UIElement child in innerGrid.Children)
                {
                    if (child is System.Windows.Controls.ComboBox combobox)
                    {
                        string name = combobox.Name.Replace("cbFaims_", "");
                        if (current_machine.Equals(name))
                            combobox.IsEnabled = true;

                        name = combobox.Name.Replace("cbOTIT_", "");
                        if (current_machine.Equals(name))
                            combobox.IsEnabled = true;
                    }
                }
            }
        }
    }
}
