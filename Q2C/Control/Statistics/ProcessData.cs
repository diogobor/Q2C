using Q2C.Control.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Controls.DataVisualization.Charting;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows;
using System.Windows.Input;
using System.Windows.Data;
using PdfSharp.Charting;
using OxyPlot.Annotations;
using OxyPlot.Axes;
using OxyPlot.Series;
using OxyPlot;
using System.Windows.Media.Imaging;
using OxyPlot.Wpf;
using System.Reflection;
using System.DirectoryServices.ActiveDirectory;
using PatternTools;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using TabControl = System.Windows.Controls.TabControl;
using Newtonsoft.Json.Linq;
using System.Windows.Documents;
using System.Security.Policy;
using Accord.MachineLearning;
using System.Windows.Media.Media3D;
using Q2C.Model;
using System.Windows.Controls.Primitives;
using System.Reflection.PortableExecutable;
using Q2C.Viewer.Machine;
using Google.Apis.Logging;
using DocumentFormat.OpenXml.Drawing.Charts;
using DocumentFormat.OpenXml.Office2016.Drawing.ChartDrawing;

namespace Q2C.Control.Statistics
{
    public static class ProcessData
    {
        public static Dictionary<string, (List<DataPoint>, List<DataPoint>, List<DataPoint>, List<DataPoint>)> Dict_Queue_points = new();
        public static Dictionary<string, ((List<DataPoint>, (List<DataPoint>, List<DataPoint>, List<DataPoint>), List<DataPoint>, (List<DataPoint>, List<DataPoint>, List<DataPoint>)), double, double)> Dict_Evaluation_points = new();
        private static List<string> BoxPlot_xAxis_DateOTFAIMS = new();
        private static List<string> BoxPlot_xAxis_DateITFAIMS = new();
        private static List<string> BoxPlot_xAxis_DateOTNoFAIMS = new();
        private static List<string> BoxPlot_xAxis_DateITNoFAIMS = new();

        private static DateTime dt_start = DateTime.Now;
        private static DateTime dt_end = DateTime.Now;
        private static bool exactPeriod_bool = true;
        private static bool monthPeriod_bool = false;
        private static bool yearPeriod_bool = false;
        private static bool autoScale_bool = false;

        #region Generate Data

        #region Machine Evaluation
        [STAThread]
        internal static void CallEvaluationTaskRun(string machine, Grid mainGrid)
        {
            Connection.Refresh_time = int.MinValue;
            ComputeAllEvaluationDataPoints(machine, dt_start, dt_end, exactPeriod_bool, monthPeriod_bool, yearPeriod_bool);

            Application.Current.Dispatcher.Invoke(() =>
            {
                CreateAllEvaluationPlots(machine, mainGrid);
                SetScrollViewerOffset(machine, mainGrid);
                Dict_Evaluation_points.Clear();
                LoadAllDatagridEvaluation(machine, mainGrid, dt_start, dt_end, exactPeriod_bool, monthPeriod_bool, yearPeriod_bool);

                RunAnalyticalMetrics(machine, mainGrid);
            });

        }
        public static void RunAnalyticalMetrics(string machine, Grid mainGrid)
        {
            //FAIMS
            DataGrid dg_faims_OT = (DataGrid)RetrieveElement($"DataGridAnalyticalMetrics_Evaluation_{machine}_OT_FAIMS", "", "FAIMS", machine, true, mainGrid, 8);
            if (dg_faims_OT != null)
                dg_faims_OT.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(delegate ()
                {
                    ComputeAnalyticalMetricsOTorIT(machine, mainGrid, dg_faims_OT, true, true);
                }));

            DataGrid dg_faims_IT = (DataGrid)RetrieveElement($"DataGridAnalyticalMetrics_Evaluation_{machine}_IT_FAIMS", "", "FAIMS", machine, false, mainGrid, 8);
            if (dg_faims_IT != null)
                dg_faims_IT.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(delegate ()
                {
                    ComputeAnalyticalMetricsOTorIT(machine, mainGrid, dg_faims_IT, false, true);
                }));


            //NoFAIMS
            DataGrid dg_no_faims_OT = (DataGrid)RetrieveElement($"DataGridAnalyticalMetrics_Evaluation_{machine}_OT_NoFAIMS", "", "NoFAIMS", machine, true, mainGrid, 8);
            if (dg_no_faims_OT != null)
                dg_no_faims_OT.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(delegate ()
                {
                    ComputeAnalyticalMetricsOTorIT(machine, mainGrid, dg_no_faims_OT, true, false);
                }));

            DataGrid dg_no_faims_IT = (DataGrid)RetrieveElement($"DataGridAnalyticalMetrics_Evaluation_{machine}_IT_NoFAIMS", "", "NoFAIMS", machine, false, mainGrid, 8);
            if (dg_no_faims_IT != null)
                dg_no_faims_IT.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(delegate ()
                {
                    ComputeAnalyticalMetricsOTorIT(machine, mainGrid, dg_no_faims_IT, false, false);
                }));
        }

        private static void ComputeAnalyticalMetricsOTorIT(string machine, Grid mainGrid, DataGrid dg_faims_nofaims, bool isOT, bool hasFAIMS)
        {
            string _hasFAIMS = hasFAIMS ? "FAIMS" : "NoFAIMS";
            string _isOT = isOT ? "OT" : "IT";

            List<Model.Run> evaluation_runs = new();
            foreach (var item in dg_faims_nofaims.Items)
                evaluation_runs.Add((Model.Run)item);

            if (isOT)
                evaluation_runs = evaluation_runs.Where(a => a.OT.IncludeAnalyticalMetrics).ToList();
            else
                evaluation_runs = evaluation_runs.Where(a => a.IT.IncludeAnalyticalMetrics).ToList();

            Grid result_grid = (Grid)RetrieveElement($"Grid_Analytical_Metrics_Evaluation_Result_{_isOT}_{machine}_{_hasFAIMS}", "", _hasFAIMS, machine, isOT, mainGrid, 9);
            if (result_grid != null)
                ComputeAnalyticalMetrics(machine, isOT, _hasFAIMS, evaluation_runs, result_grid);
        }

        public static void ComputeAnalyticalMetrics(string machine, bool isOT, string _hasFaims, List<Model.Run> runs, Grid current_grid)
        {
            if (runs.Count == 0) return;

            current_grid.Children.Clear();

            string OTIT = isOT ? "OT" : "IT";
            Dictionary<string, List<int>> peptScans = ConvertRunsToPeptideScans(runs, isOT);
            double overlap = CalculateOverlap(peptScans);
            (List<double> all_zscores, List<int> outlier_runs) poisson = ComputePoisson(peptScans);

            TextBlock textBlock = new TextBlock();
            textBlock.Inlines.Add(new System.Windows.Documents.Run("Average peptide overlap: ") { FontWeight = System.Windows.FontWeights.Bold });
            textBlock.Inlines.Add(new System.Windows.Documents.Run($"{overlap:F2}%"));
            if (poisson.outlier_runs.Count > 0)
                textBlock.Inlines.Add(new System.Windows.Documents.Run("\n\nOutlier Runs: ") { FontWeight = System.Windows.FontWeights.Bold });
            textBlock.Margin = new System.Windows.Thickness(10, 0, 0, 10);
            Grid.SetRow(textBlock, 0);
            current_grid.Children.Add(textBlock);

            System.Windows.Controls.DataVisualization.Charting.Chart _histogram = null;

            if (poisson.all_zscores.Count > 0)
                _histogram = CreateHistogramChart("chart_histogram" + OTIT, poisson.all_zscores, "Z-Score Histogram");

            List<Model.Run> outlier_runs = null;
            if (poisson.outlier_runs.Count > 0)
            {
                outlier_runs = poisson.outlier_runs.Select(index => runs.ElementAt(index)).ToList();

                //create datagrid
                var outlier_dg = CreateDataGridEvaluation(machine, OTIT, _hasFaims, false, "DataGridAnalyticalMetrics_Evaluation_Outliers");
                outlier_dg.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(delegate ()
                {
                    outlier_dg.ItemsSource = null;
                    outlier_dg.ItemsSource = outlier_runs;
                }));
                Grid.SetRow(outlier_dg, 1);
                current_grid.Children.Add(outlier_dg);

                if (_histogram != null)
                    Grid.SetRow(_histogram, 2);
            }
            else if (_histogram != null)
                Grid.SetRow(_histogram, 1);

            if (_histogram != null)
                current_grid.Children.Add(_histogram);
        }

        public static (List<double> all_zscores, List<int> outlier_runs) ComputePoisson(Dictionary<string, List<int>> peptScans)
        {
            List<double> allZScores = new();
            Dictionary<string, List<double>> peptideZScores = new();

            //Runs that contain outliers
            List<int> outliers = new();

            foreach (var peptide in peptScans)
            {
                string peptSeq = peptide.Key;
                List<int> specCounts = peptide.Value;

                var nonZeroValues = specCounts.Where(v => v > 0).ToList();

                if (nonZeroValues.Count > 1)
                {
                    double lambda = nonZeroValues.Average();//expected mean
                    double sigma = Math.Sqrt(lambda);// Poisson's std

                    if (sigma == 0) sigma = 1;

                    List<double> zScores = specCounts.Select(specCount => specCount > 0 ? Math.Abs(specCount - lambda) / sigma : double.NaN).ToList();

                    //Save the runs that contain outliers (|Z| > 2)
                    outliers.AddRange(zScores.Select((value, index) => new { value, index })
                                            .Where(item => item.value > 2)
                                            .Select(item => item.index)
                                            .ToList());


                    peptideZScores[peptSeq] = zScores;
                    allZScores.AddRange(zScores.Where(z => !double.IsNaN(z)));
                }
            }
            outliers = outliers.Distinct().ToList();

            return (allZScores, outliers);
        }

        public static Dictionary<string, List<int>> ConvertRunsToPeptideScans(List<Model.Run> runs, bool isOT)
        {
            int numRuns = runs.Count;
            Dictionary<string, List<int>> peptScans = new();

            // Identify all unique peptides
            IEnumerable<string> allPeptides = null;
            if (isOT)
                allPeptides = runs.SelectMany(r => r.OT.MostAbundantPepts.Keys).Distinct();
            else
                allPeptides = runs.SelectMany(r => r.IT.MostAbundantPepts.Keys).Distinct();

            // Initialize dictionary with empty lists of correct size
            foreach (string peptide in allPeptides)
            {
                peptScans[peptide] = Enumerable.Repeat(0, numRuns).ToList();
            }

            // Fill in the spectrum counts
            for (int i = 0; i < numRuns; i++)
            {
                if (isOT)
                    foreach (var kvp in runs[i].OT.MostAbundantPepts)
                        peptScans[kvp.Key][i] = kvp.Value;
                else
                    foreach (var kvp in runs[i].IT.MostAbundantPepts)
                        peptScans[kvp.Key][i] = kvp.Value;
            }

            return peptScans;
        }
        public static double CalculateOverlap(Dictionary<string, List<int>> peptScans)
        {
            if (peptScans == null || peptScans.Count == 0) return 0;

            // Calculate the number of peptides identified in multiple runs
            int totalPeptides = 0;
            int overlappingPeptides = 0;

            foreach (var peptide in peptScans)
            {
                var scanCounts = peptide.Value;
                totalPeptides++;

                // Check if the peptide is identified in more than one run
                int identifiedRuns = 0;
                foreach (var count in scanCounts)
                {
                    if (count > 0)
                    {
                        identifiedRuns++;
                    }
                }

                if (identifiedRuns > 1) // If identified in more than one run
                {
                    overlappingPeptides++;
                }
            }

            // Calculate the overlap percentage
            return (double)overlappingPeptides / totalPeptides * 100;

        }

        public static void LoadAllDatagridEvaluation(string machine,
            Grid mainGrid,
           DateTime dt_start,
           DateTime dt_end,
           bool exactPeriod,
           bool monthPeriod,
           bool yearPeriod)
        {

            (List<MachineQueue> queue, List<Model.Run> evaluation, List<MachineLog> log) prop;
            Management.Machines_Properties.TryGetValue(machine, out prop);
            if (prop == (null, null, null))
                return;

            //FAIMS
            List<Model.Run> valid_runs_faims_OT = GetValidRunsEvaluation(machine,
                prop.evaluation,
                dt_start,
                dt_end,
                true,
                true,
                exactPeriod,
                monthPeriod,
                yearPeriod);
            List<Model.Run> valid_runs_faims_IT = GetValidRunsEvaluation(machine,
                prop.evaluation,
                dt_start,
                dt_end,
                true,
                false,
                exactPeriod,
                monthPeriod,
                yearPeriod);

            //NoFAIMS
            List<Model.Run> valid_runs_no_faims_OT = GetValidRunsEvaluation(machine,
                prop.evaluation,
                dt_start,
                dt_end,
                false,
                true,
                exactPeriod,
                monthPeriod,
                yearPeriod);
            List<Model.Run> valid_runs_no_faims_IT = GetValidRunsEvaluation(machine,
                prop.evaluation,
                dt_start,
                dt_end,
                false,
                false,
                exactPeriod,
                monthPeriod,
                yearPeriod);

            LoadDataGridEvaluation(machine,
                mainGrid,
                valid_runs_faims_OT,
                valid_runs_faims_IT,
                valid_runs_no_faims_OT,
                valid_runs_no_faims_IT
                );

        }

        public static void LoadDataGridEvaluation(string machine,
            Grid mainGrid,
            List<Model.Run> valid_runs_faims_OT,
            List<Model.Run> valid_runs_faims_IT,
            List<Model.Run> valid_runs_no_faims_OT,
            List<Model.Run> valid_runs_no_faims_IT
            )
        {
            //FAIMS
            DataGrid dg_faims_OT = (DataGrid)RetrieveElement($"DataGridAnalyticalMetrics_Evaluation_{machine}_OT_FAIMS", "", "FAIMS", machine, true, mainGrid, 8);
            if (dg_faims_OT != null)
                dg_faims_OT.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(delegate ()
                {
                    var _runs = valid_runs_faims_OT.Where(a => a.InfoStatus == Management.InfoStatus.Active).ToList();
                    _runs.Sort((a, b) => b.OT.RegistrationDate.CompareTo(a.OT.RegistrationDate));
                    dg_faims_OT.ItemsSource = null;
                    dg_faims_OT.ItemsSource = _runs;
                }));

            DataGrid dg_faims_IT = (DataGrid)RetrieveElement($"DataGridAnalyticalMetrics_Evaluation_{machine}_IT_FAIMS", "", "FAIMS", machine, false, mainGrid, 8);
            if (dg_faims_IT != null)
                dg_faims_IT.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(delegate ()
                {
                    var _runs = valid_runs_faims_IT.Where(a => a.InfoStatus == Management.InfoStatus.Active).ToList();
                    _runs.Sort((a, b) => b.IT.RegistrationDate.CompareTo(a.IT.RegistrationDate));
                    dg_faims_IT.ItemsSource = null;
                    dg_faims_IT.ItemsSource = _runs;
                }));

            //NoFAIMS
            DataGrid dg_no_faims_OT = (DataGrid)RetrieveElement($"DataGridAnalyticalMetrics_Evaluation_{machine}_OT_NoFAIMS", "", "NoFAIMS", machine, true, mainGrid, 8);
            if (dg_no_faims_OT != null)
                dg_no_faims_OT.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(delegate ()
                {
                    var _runs = valid_runs_no_faims_OT.Where(a => a.InfoStatus == Management.InfoStatus.Active).ToList();
                    _runs.Sort((a, b) => b.OT.RegistrationDate.CompareTo(a.OT.RegistrationDate));
                    dg_no_faims_OT.ItemsSource = null;
                    dg_no_faims_OT.ItemsSource = _runs;
                }));

            DataGrid dg_no_faims_IT = (DataGrid)RetrieveElement($"DataGridAnalyticalMetrics_Evaluation_{machine}_IT_NoFAIMS", "", "NoFAIMS", machine, false, mainGrid, 8);
            if (dg_no_faims_IT != null)
                dg_no_faims_IT.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(delegate ()
                {
                    var _runs = valid_runs_no_faims_IT.Where(a => a.InfoStatus == Management.InfoStatus.Active).ToList();
                    _runs.Sort((a, b) => b.IT.RegistrationDate.CompareTo(a.IT.RegistrationDate));
                    dg_no_faims_IT.ItemsSource = null;
                    dg_no_faims_IT.ItemsSource = _runs;
                }));
        }

        public static List<Model.Run> GetValidRunsEvaluation(string machine,
           List<Model.Run> evaluation,
           DateTime dt_start,
           DateTime dt_end,
           bool isFAIMS,
           bool isOT,
           bool exactPeriod,
           bool monthPeriod,
           bool yearPeriod)
        {
            List<Model.Run> valid_runs = null;

            if (isFAIMS)
            {
                if (isOT) // FAIMS && OT
                {
                    valid_runs = (from run in evaluation.AsParallel()
                                  where run.InfoStatus == Management.InfoStatus.Active &&
                                  run.OT.FAIMS == true && run.OT.Exclude == false &&
                                  run.OT.MostAbundantPepts != null && run.OT.MostAbundantPepts.Count > 0 &&
                                  run.OT.RegistrationDate >= dt_start && run.OT.RegistrationDate <= dt_end
                                  select run).ToList();
                    if (monthPeriod)
                        valid_runs = valid_runs.Where(a => a.OT.RegistrationDate.Month >= dt_start.Month && a.OT.RegistrationDate.Month <= dt_end.Month).ToList();
                    else if (yearPeriod)
                        valid_runs = valid_runs.Where(a => a.OT.RegistrationDate.Year >= dt_start.Year && a.OT.RegistrationDate.Year <= dt_end.Year).ToList();

                    return valid_runs;
                }
                else // IT
                {
                    valid_runs = (from run in evaluation.AsParallel()
                                  where run.InfoStatus == Management.InfoStatus.Active &&
                                  run.IT.FAIMS == true && run.IT.Exclude == false &&
                                  run.IT.MostAbundantPepts != null && run.IT.MostAbundantPepts.Count > 0 &&
                                  run.IT.RegistrationDate >= dt_start && run.IT.RegistrationDate <= dt_end
                                  select run).ToList();
                    if (monthPeriod)
                        valid_runs = valid_runs.Where(a => a.IT.RegistrationDate.Month >= dt_start.Month && a.IT.RegistrationDate.Month <= dt_end.Month).ToList();
                    else if (yearPeriod)
                        valid_runs = valid_runs.Where(a => a.IT.RegistrationDate.Year >= dt_start.Year && a.IT.RegistrationDate.Year <= dt_end.Year).ToList();

                    return valid_runs;
                }
            }
            else // NoFaims
            {
                if (isOT) // FAIMS && OT
                {
                    valid_runs = (from run in evaluation.AsParallel()
                                  where run.InfoStatus == Management.InfoStatus.Active &&
                                  run.OT.FAIMS == false && run.OT.Exclude == false &&
                                  run.OT.MostAbundantPepts != null && run.OT.MostAbundantPepts.Count > 0 &&
                                  run.OT.RegistrationDate >= dt_start && run.OT.RegistrationDate <= dt_end
                                  select run).ToList();
                    if (monthPeriod)
                        valid_runs = valid_runs.Where(a => a.OT.RegistrationDate.Month >= dt_start.Month && a.OT.RegistrationDate.Month <= dt_end.Month).ToList();
                    else if (yearPeriod)
                        valid_runs = valid_runs.Where(a => a.OT.RegistrationDate.Year >= dt_start.Year && a.OT.RegistrationDate.Year <= dt_end.Year).ToList();

                    return valid_runs;
                }
                else // IT
                {
                    valid_runs = (from run in evaluation.AsParallel()
                                  where run.InfoStatus == Management.InfoStatus.Active &&
                                  run.IT.FAIMS == false && run.IT.Exclude == false &&
                                  run.IT.MostAbundantPepts != null && run.IT.MostAbundantPepts.Count > 0 &&
                                  run.IT.RegistrationDate >= dt_start && run.IT.RegistrationDate <= dt_end
                                  select run).ToList();
                    if (monthPeriod)
                        valid_runs = valid_runs.Where(a => a.IT.RegistrationDate.Month >= dt_start.Month && a.IT.RegistrationDate.Month <= dt_end.Month).ToList();
                    else if (yearPeriod)
                        valid_runs = valid_runs.Where(a => a.IT.RegistrationDate.Year >= dt_start.Year && a.IT.RegistrationDate.Year <= dt_end.Year).ToList();

                    return valid_runs;
                }
            }
        }

        public static void ComputeAllEvaluationDataPoints(string machine,
            DateTime dt_start,
            DateTime dt_end,
            bool exactPeriod,
            bool monthPeriod,
            bool yearPeriod)
        {
            Dict_Evaluation_points.Clear();

            //FAIMS
            ComputeOT_and_IT_DataPoints(machine,
                dt_start,
                dt_end,
                exactPeriod,
                monthPeriod,
                yearPeriod,
                 true);
            //No FAIMS
            ComputeOT_and_IT_DataPoints(machine,
                dt_start,
                dt_end,
                exactPeriod,
                monthPeriod,
                yearPeriod,
                 false);
        }

        private static void ComputeOT_and_IT_DataPoints(string machine,
           DateTime dt_start,
           DateTime dt_end,
           bool exactPeriod,
           bool monthPeriod,
           bool yearPeriod,
           bool isFAIMS)
        {
            string[] properties = new string[] { "Protein", "Peptide", "PSM", "MSMS", "IDRatio", "XreaMean", "PPM" };
            bool[] categories = new bool[properties.Length];

            for (int i = 0; i < properties.Length; i++)
            {
                string _property = properties[i];

                if (i == 0)
                    categories[i] = true;
                else
                {
                    categories[i - 1] = false;
                    categories[i] = true;
                }

                //Add to Dict_Points all DataPoints
                ComputeDataPointsBlock(machine,
                    dt_start,
                    dt_end,
                    exactPeriod,
                    monthPeriod,
                    yearPeriod,
                    categories,
                    _property,
                    isFAIMS);
            }
        }

        private static void ComputeDataPointsBlock(
           string machine,
           DateTime dt_start,
           DateTime dt_end,
           bool exactPeriod,
           bool monthPeriod,
           bool yearPeriod,
           bool[] categories,
           string _property,
           bool isFAIMS)
        {
            string faims = isFAIMS ? "FAIMS" : "NoFAIMS";
            var _data_points = ComputeDataPointsForEachChart(machine, categories, isFAIMS, dt_start, dt_end, exactPeriod, monthPeriod, yearPeriod);
            double avgOT = ComputeAverage(machine, isFAIMS, true, _property);
            double avgIT = ComputeAverage(machine, isFAIMS, false, _property);
            string _key = _property + "#" + faims + "#" + machine;

            Dict_Evaluation_points.Add(_key, (_data_points, avgOT, avgIT));
        }

        private static (List<DataPoint> pointsOT,
           (List<DataPoint> ransac, List<DataPoint> ransacMinusSigma, List<DataPoint> ransacPlusSigma) ransacPointsOT,
           List<DataPoint> pointsIT,
           (List<DataPoint> ransac, List<DataPoint> ransacMinusSigma, List<DataPoint> ransacPlusSigma) ransacPointsIT)
           ComputeDataPointsForEachChart(
           string machine,
           bool[] categories,
           bool isFAIMS,
           DateTime dt_start,
           DateTime dt_end,
           bool exactPeriod,
           bool monthPeriod,
           bool yearPeriod
           )
        {
            var pointsOT = GetRunsPoints(machine,
                                             isFAIMS,
                                             true,
                                             categories,
                                             dt_start,
                                             dt_end,
                                             exactPeriod,
                                             monthPeriod,
                                             yearPeriod);
            var ransacPointsOT = GetRANSACPoints(pointsOT);

            var pointsIT = GetRunsPoints(machine,
                                             isFAIMS,
                                             false,
                                             categories,
                                             dt_start,
                                             dt_end,
                                             exactPeriod,
                                             monthPeriod,
                                             yearPeriod);
            var ransacPointsIT = GetRANSACPoints(pointsIT);

            return (pointsOT, ransacPointsOT, pointsIT, ransacPointsIT);
        }

        private static List<DataPoint> GetRunsPoints(string machine,
                                              bool isFAIMS,
                                              bool isOT,
                                              bool[] categories,
                                              DateTime dt_start,
                                              DateTime dt_end,
                                              bool exactPeriod,
                                              bool monthPeriod,
                                              bool yearPeriod)
        {

            List<DataPoint> valid_runs = new();
            (List<MachineQueue> queue, List<Model.Run> evaluation, List<MachineLog> log) prop;
            Management.Machines_Properties.TryGetValue(machine, out prop);
            if (prop == (null, null, null))
                return new();

            if (isFAIMS)
            {
                if (isOT) // FAIMS && OT
                {
                    if (categories[0])//Protein
                        valid_runs = (from run in prop.evaluation.AsParallel()
                                      where run.InfoStatus == Management.InfoStatus.Active &&
                                      run.OT.FAIMS == true && run.OT.Exclude == false
                                      select new DataPoint((run.OT.RegistrationDate.Day + "/" + run.OT.RegistrationDate.Month + "/" + run.OT.RegistrationDate.Year), run.OT.ProteinGroup)).ToList();
                    else if (categories[1])//Peptide
                        valid_runs = (from run in prop.evaluation.AsParallel()
                                      where run.InfoStatus == Management.InfoStatus.Active &&
                                      run.OT.FAIMS == true && run.OT.Exclude == false
                                      select new DataPoint((run.OT.RegistrationDate.Day + "/" + run.OT.RegistrationDate.Month + "/" + run.OT.RegistrationDate.Year), run.OT.PeptideGroup)).ToList();
                    else if (categories[2])//PSM
                        valid_runs = (from run in prop.evaluation.AsParallel()
                                      where run.InfoStatus == Management.InfoStatus.Active &&
                                      run.OT.FAIMS == true && run.OT.Exclude == false
                                      select new DataPoint((run.OT.RegistrationDate.Day + "/" + run.OT.RegistrationDate.Month + "/" + run.OT.RegistrationDate.Year), run.OT.PSM)).ToList();
                    else if (categories[3])//MSMS
                        valid_runs = (from run in prop.evaluation.AsParallel()
                                      where run.InfoStatus == Management.InfoStatus.Active &&
                                      run.OT.FAIMS == true && run.OT.Exclude == false
                                      select new DataPoint((run.OT.RegistrationDate.Day + "/" + run.OT.RegistrationDate.Month + "/" + run.OT.RegistrationDate.Year), run.OT.MSMS)).ToList();
                    else if (categories[4])//IDRatio
                        valid_runs = (from run in prop.evaluation.AsParallel()
                                      where run.InfoStatus == Management.InfoStatus.Active &&
                                      run.OT.FAIMS == true && run.OT.Exclude == false
                                      select new DataPoint((run.OT.RegistrationDate.Day + "/" + run.OT.RegistrationDate.Month + "/" + run.OT.RegistrationDate.Year), run.OT.IDRatio)).ToList();
                    else if (categories[5])//Xrea
                        valid_runs = (from run in prop.evaluation.AsParallel()
                                      where run.InfoStatus == Management.InfoStatus.Active &&
                                      run.OT.FAIMS == true && run.OT.Exclude == false
                                      select new DataPoint((run.OT.RegistrationDate.Day + "/" + run.OT.RegistrationDate.Month + "/" + run.OT.RegistrationDate.Year), run.OT.XreaMean)).ToList();
                    else if (categories[6])//PPM
                        valid_runs = (from run in prop.evaluation.AsParallel()
                                      where run.InfoStatus == Management.InfoStatus.Active &&
                                      run.OT.FAIMS == true && run.OT.Exclude == false
                                      select new DataPoint((run.OT.RegistrationDate.Day + "/" + run.OT.RegistrationDate.Month + "/" + run.OT.RegistrationDate.Year), 0, GetMinValue(run.OT.MassError), GetMaxValue(run.OT.MassError), run.OT.MassErrorMedian)).ToList();
                }
                else // FAIMS && IT
                {
                    if (categories[0])//Protein
                        valid_runs = (from run in prop.evaluation.AsParallel()
                                      where run.InfoStatus == Management.InfoStatus.Active &&
                                      run.IT.FAIMS == true && run.IT.Exclude == false
                                      select new DataPoint((run.IT.RegistrationDate.Day + "/" + run.IT.RegistrationDate.Month + "/" + run.IT.RegistrationDate.Year), run.IT.ProteinGroup)).ToList();
                    else if (categories[1])//Peptide
                        valid_runs = (from run in prop.evaluation.AsParallel()
                                      where run.InfoStatus == Management.InfoStatus.Active &&
                                      run.IT.FAIMS == true && run.IT.Exclude == false
                                      select new DataPoint((run.IT.RegistrationDate.Day + "/" + run.IT.RegistrationDate.Month + "/" + run.IT.RegistrationDate.Year), run.IT.PeptideGroup)).ToList();
                    else if (categories[2])//PSM
                        valid_runs = (from run in prop.evaluation.AsParallel()
                                      where run.InfoStatus == Management.InfoStatus.Active &&
                                      run.IT.FAIMS == true && run.IT.Exclude == false
                                      select new DataPoint((run.IT.RegistrationDate.Day + "/" + run.IT.RegistrationDate.Month + "/" + run.IT.RegistrationDate.Year), run.IT.PSM)).ToList();
                    else if (categories[3])//MSMS
                        valid_runs = (from run in prop.evaluation.AsParallel()
                                      where run.InfoStatus == Management.InfoStatus.Active &&
                                      run.IT.FAIMS == true && run.IT.Exclude == false
                                      select new DataPoint((run.IT.RegistrationDate.Day + "/" + run.IT.RegistrationDate.Month + "/" + run.IT.RegistrationDate.Year), run.IT.MSMS)).ToList();
                    else if (categories[4])//IDRatio
                        valid_runs = (from run in prop.evaluation.AsParallel()
                                      where run.InfoStatus == Management.InfoStatus.Active &&
                                      run.IT.FAIMS == true && run.IT.Exclude == false
                                      select new DataPoint((run.IT.RegistrationDate.Day + "/" + run.IT.RegistrationDate.Month + "/" + run.IT.RegistrationDate.Year), run.IT.IDRatio)).ToList();
                    else if (categories[5])//Xrea
                        valid_runs = (from run in prop.evaluation.AsParallel()
                                      where run.InfoStatus == Management.InfoStatus.Active &&
                                      run.IT.FAIMS == true && run.IT.Exclude == false
                                      select new DataPoint((run.IT.RegistrationDate.Day + "/" + run.IT.RegistrationDate.Month + "/" + run.IT.RegistrationDate.Year), run.IT.XreaMean)).ToList();
                    else if (categories[6])//PPM
                        valid_runs = (from run in prop.evaluation.AsParallel()
                                      where run.InfoStatus == Management.InfoStatus.Active &&
                                      run.IT.FAIMS == true && run.IT.Exclude == false
                                      select new DataPoint((run.IT.RegistrationDate.Day + "/" + run.IT.RegistrationDate.Month + "/" + run.IT.RegistrationDate.Year), 0, GetMinValue(run.IT.MassError), GetMaxValue(run.IT.MassError), run.IT.MassErrorMedian)).ToList();
                }
            }
            else
            {
                if (isOT) // noFAIMS && OT
                {
                    if (categories[0])//Protein
                        valid_runs = (from run in prop.evaluation.AsParallel()
                                      where run.InfoStatus == Management.InfoStatus.Active &&
                                      run.OT.FAIMS == false && run.OT.Exclude == false
                                      select new DataPoint((run.OT.RegistrationDate.Day + "/" + run.OT.RegistrationDate.Month + "/" + run.OT.RegistrationDate.Year), run.OT.ProteinGroup)).ToList();
                    else if (categories[1])//Peptide
                        valid_runs = (from run in prop.evaluation.AsParallel()
                                      where run.InfoStatus == Management.InfoStatus.Active &&
                                      run.OT.FAIMS == false && run.OT.Exclude == false
                                      select new DataPoint((run.OT.RegistrationDate.Day + "/" + run.OT.RegistrationDate.Month + "/" + run.OT.RegistrationDate.Year), run.OT.PeptideGroup)).ToList();
                    else if (categories[2])//PSM
                        valid_runs = (from run in prop.evaluation.AsParallel()
                                      where run.InfoStatus == Management.InfoStatus.Active &&
                                      run.OT.FAIMS == false && run.OT.Exclude == false
                                      select new DataPoint((run.OT.RegistrationDate.Day + "/" + run.OT.RegistrationDate.Month + "/" + run.OT.RegistrationDate.Year), run.OT.PSM)).ToList();
                    else if (categories[3])//MSMS
                        valid_runs = (from run in prop.evaluation.AsParallel()
                                      where run.InfoStatus == Management.InfoStatus.Active &&
                                      run.OT.FAIMS == false && run.OT.Exclude == false
                                      select new DataPoint((run.OT.RegistrationDate.Day + "/" + run.OT.RegistrationDate.Month + "/" + run.OT.RegistrationDate.Year), run.OT.MSMS)).ToList();
                    else if (categories[4])//IDRatio
                        valid_runs = (from run in prop.evaluation.AsParallel()
                                      where run.InfoStatus == Management.InfoStatus.Active &&
                                      run.OT.FAIMS == false && run.OT.Exclude == false
                                      select new DataPoint((run.OT.RegistrationDate.Day + "/" + run.OT.RegistrationDate.Month + "/" + run.OT.RegistrationDate.Year), run.OT.IDRatio)).ToList();
                    else if (categories[5])//Xrea
                        valid_runs = (from run in prop.evaluation.AsParallel()
                                      where run.InfoStatus == Management.InfoStatus.Active &&
                                      run.OT.FAIMS == false && run.OT.Exclude == false
                                      select new DataPoint((run.OT.RegistrationDate.Day + "/" + run.OT.RegistrationDate.Month + "/" + run.OT.RegistrationDate.Year), run.OT.XreaMean)).ToList();
                    else if (categories[6])//PPM
                        valid_runs = (from run in prop.evaluation.AsParallel()
                                      where run.InfoStatus == Management.InfoStatus.Active &&
                                      run.OT.FAIMS == false && run.OT.Exclude == false
                                      select new DataPoint((run.OT.RegistrationDate.Day + "/" + run.OT.RegistrationDate.Month + "/" + run.OT.RegistrationDate.Year), 0, GetMinValue(run.OT.MassError), GetMaxValue(run.OT.MassError), run.OT.MassErrorMedian)).ToList();
                }
                else // noFAIMS && IT
                {
                    if (categories[0])//Protein
                        valid_runs = (from run in prop.evaluation.AsParallel()
                                      where run.InfoStatus == Management.InfoStatus.Active &&
                                      run.IT.FAIMS == false && run.IT.Exclude == false
                                      select new DataPoint((run.IT.RegistrationDate.Day + "/" + run.IT.RegistrationDate.Month + "/" + run.IT.RegistrationDate.Year), run.IT.ProteinGroup)).ToList();
                    else if (categories[1])//Peptide
                        valid_runs = (from run in prop.evaluation.AsParallel()
                                      where run.InfoStatus == Management.InfoStatus.Active &&
                                      run.IT.FAIMS == false && run.IT.Exclude == false
                                      select new DataPoint((run.IT.RegistrationDate.Day + "/" + run.IT.RegistrationDate.Month + "/" + run.IT.RegistrationDate.Year), run.IT.PeptideGroup)).ToList();
                    else if (categories[2])//PSM
                        valid_runs = (from run in prop.evaluation.AsParallel()
                                      where run.InfoStatus == Management.InfoStatus.Active &&
                                      run.IT.FAIMS == false && run.IT.Exclude == false
                                      select new DataPoint((run.IT.RegistrationDate.Day + "/" + run.IT.RegistrationDate.Month + "/" + run.IT.RegistrationDate.Year), run.IT.PSM)).ToList();
                    else if (categories[3])//MSMS
                        valid_runs = (from run in prop.evaluation.AsParallel()
                                      where run.InfoStatus == Management.InfoStatus.Active &&
                                      run.IT.FAIMS == false && run.IT.Exclude == false
                                      select new DataPoint((run.IT.RegistrationDate.Day + "/" + run.IT.RegistrationDate.Month + "/" + run.IT.RegistrationDate.Year), run.IT.MSMS)).ToList();
                    else if (categories[4])//IDRatio
                        valid_runs = (from run in prop.evaluation.AsParallel()
                                      where run.InfoStatus == Management.InfoStatus.Active &&
                                      run.IT.FAIMS == false && run.IT.Exclude == false
                                      select new DataPoint((run.IT.RegistrationDate.Day + "/" + run.IT.RegistrationDate.Month + "/" + run.IT.RegistrationDate.Year), run.IT.IDRatio)).ToList();
                    else if (categories[5])//Xrea
                        valid_runs = (from run in prop.evaluation.AsParallel()
                                      where run.InfoStatus == Management.InfoStatus.Active &&
                                      run.IT.FAIMS == false && run.IT.Exclude == false
                                      select new DataPoint((run.IT.RegistrationDate.Day + "/" + run.IT.RegistrationDate.Month + "/" + run.IT.RegistrationDate.Year), run.IT.XreaMean)).ToList();
                    else if (categories[6])//PPM
                        valid_runs = (from run in prop.evaluation.AsParallel()
                                      where run.InfoStatus == Management.InfoStatus.Active &&
                                      run.IT.FAIMS == false && run.IT.Exclude == false
                                      select new DataPoint((run.IT.RegistrationDate.Day + "/" + run.IT.RegistrationDate.Month + "/" + run.IT.RegistrationDate.Year), 0, GetMinValue(run.IT.MassError), GetMaxValue(run.IT.MassError), run.IT.MassErrorMedian)).ToList();
                }
            }

            if (exactPeriod)
                valid_runs = GetGroupedPointsExactPeriod(valid_runs.Where(a => Util.Util.ConvertStrToDate(a.X) >= dt_start && Util.Util.ConvertStrToDate(a.X) <= dt_end).ToList());
            else if (monthPeriod)
                valid_runs = GetGroupedPointsMonthPeriod(valid_runs.Where(a => DateTime.Compare(new DateTime(dt_start.Year, dt_start.Month, 1), new DateTime(Util.Util.ConvertStrToDate(a.X).Year, Util.Util.ConvertStrToDate(a.X).Month, 1)) < 0 && DateTime.Compare(Util.Util.ConvertStrToDate(a.X), new DateTime(dt_end.Year, dt_end.Month + 1, 1)) < 0).ToList());
            else if (yearPeriod)
                valid_runs = GetGroupedPointsYearPeriod(valid_runs.Where(a => Util.Util.ConvertStrToDate(a.X).Year >= dt_start.Year && Util.Util.ConvertStrToDate(a.X).Year <= dt_end.Year).ToList());

            if (valid_runs != null && valid_runs.Count > 0)
                valid_runs[valid_runs.Count - 1].Highlight = true;

            return valid_runs != null ? valid_runs : new();
        }

        private static double GetMinValue(string massErrorPPM)
        {
            if (String.IsNullOrEmpty(massErrorPPM)) return 0;
            string[] cols = Regex.Split(massErrorPPM, " to ");
            if (cols.Length == 1) return 0;
            return Convert.ToDouble(cols[0].Trim());
        }

        private static double GetMaxValue(string massErrorPPM)
        {
            if (String.IsNullOrEmpty(massErrorPPM)) return 0;
            string[] cols = Regex.Split(massErrorPPM, " to ");
            if (cols.Length == 1) return 0;
            return Convert.ToDouble(cols[1].Trim());
        }

        private static List<DataPoint> GetGroupedPointsExactPeriod(List<DataPoint> _set)
        {
            List<DataPoint> list = new();
            var grouped = _set.GroupBy(dt => new
            {
                Year = Util.Util.ConvertStrToDate(dt.X).Year,
                Month = Util.Util.ConvertStrToDate(dt.X).Month,
                Day = Util.Util.ConvertStrToDate(dt.X).Day,
            });
            foreach (var value in grouped)
                list.Add(new DataPoint((value.Key.Day + "/" + value.Key.Month + "/" + value.Key.Year), Util.Util.Median(value.Select(a => a.Y).ToList()), Util.Util.Median(value.Select(a => a.MinValue).ToList()), Util.Util.Median(value.Select(a => a.MaxValue).ToList()), Util.Util.Median(value.Select(a => a.MedianValue).ToList())));
            return list;
        }
        private static List<DataPoint> GetGroupedPointsMonthPeriod(List<DataPoint> _set)
        {
            List<DataPoint> list = new();
            var grouped = _set.GroupBy(dt => new
            {
                Year = Util.Util.ConvertStrToDate(dt.X).Year,
                Month = Util.Util.ConvertStrToDate(dt.X).Month,
            });
            foreach (var value in grouped)
                list.Add(new DataPoint((value.Key.Month + "/" + value.Key.Year), Util.Util.Median(value.Select(a => a.Y).ToList()), Util.Util.Median(value.Select(a => a.MinValue).ToList()), Util.Util.Median(value.Select(a => a.MaxValue).ToList()), Util.Util.Median(value.Select(a => a.MedianValue).ToList())));

            return list;
        }
        private static List<DataPoint> GetGroupedPointsYearPeriod(List<DataPoint> _set)
        {
            List<DataPoint> list = new();
            var grouped = _set.GroupBy(dt => new
            {
                Year = Util.Util.ConvertStrToDate(dt.X).Year,
            });
            foreach (var value in grouped)
                list.Add(new DataPoint("" + value.Key.Year, Util.Util.Median(value.Select(a => a.Y).ToList()), Util.Util.Median(value.Select(a => a.MinValue).ToList()), Util.Util.Median(value.Select(a => a.MaxValue).ToList()), Util.Util.Median(value.Select(a => a.MedianValue).ToList())));

            return list;
        }

        private static (List<DataPoint> ransac, List<DataPoint> ransacMinusSigma, List<DataPoint> ransacPlusSigma) GetRANSACPoints(List<DataPoint> points)
        {
            if (points == null || points.Count == 0) return (new(), new(), new());

            var ransac = Util.Util.ComputeRANSACLinearRegression(points.Select(a => Util.Util.ConvertStrToDate(a.X)).Select(b => b.ToOADate()).ToArray(), points.Select(a => a.Y).ToArray());

            List<DataPoint> ransacPoints = new(points.Count);
            for (int i = 0; i < points.Count; i++)
            {
                double rans_point = ransac[i];
                rans_point = Double.IsNaN(rans_point) ? 0 : rans_point;
                DataPoint point = points[i];
                ransacPoints.Add(new DataPoint(point.X, Math.Round(ransac[i])));
            }

            double ransac_std = Util.Util.Stdev(ransac.ToList(), true);
            ransac_std = Double.IsNaN(ransac_std) ? 0 : ransac_std;

            List<DataPoint> ransacPointsMinusStd = new(points.Count);
            for (int i = 0; i < points.Count; i++)
            {
                DataPoint point = points[i];
                ransacPointsMinusStd.Add(new DataPoint(point.X, Math.Round(ransac[i] - 3 * ransac_std)));
            }

            List<DataPoint> ransacPointsPlusStd = new(points.Count);
            for (int i = 0; i < points.Count; i++)
            {
                DataPoint point = points[i];
                ransacPointsPlusStd.Add(new DataPoint(point.X, Math.Round(ransac[i] + 3 * ransac_std)));
            }
            return (ransacPoints, ransacPointsMinusStd, ransacPointsPlusStd);
        }

        private static double ComputeAverage(string machine, bool isFAIMS, bool isOT, string _property)
        {
            List<Model.Run> _runs = GetRun(machine, isFAIMS, isOT);
            if (_runs == null || _runs.Count == 0)
                return 0;

            switch (_property)
            {
                case "Protein":
                    if (isOT)
                        return Convert.ToInt32(_runs.Average(a => a.OT.ProteinGroup));
                    else
                        return Convert.ToInt32(_runs.Average(a => a.IT.ProteinGroup));
                case "Peptide":
                    if (isOT)
                        return Convert.ToInt32(_runs.Average(a => a.OT.PeptideGroup));
                    else
                        return Convert.ToInt32(_runs.Average(a => a.IT.PeptideGroup));
                case "PSM":
                    if (isOT)
                        return Convert.ToInt32(_runs.Average(a => a.OT.PSM));
                    else
                        return Convert.ToInt32(_runs.Average(a => a.IT.PSM));
                case "MSMS":
                    if (isOT)
                        return Convert.ToInt32(_runs.Average(a => a.OT.MSMS));
                    else
                        return Convert.ToInt32(_runs.Average(a => a.IT.MSMS));
                case "IDRatio":
                    if (isOT)
                        return Convert.ToDouble(_runs.Average(a => a.OT.IDRatio));
                    else
                        return Convert.ToDouble(_runs.Average(a => a.IT.IDRatio));
                case "XreaMean":
                    if (isOT)
                        return Convert.ToDouble(_runs.Average(a => a.OT.XreaMean));
                    else
                        return Convert.ToDouble(_runs.Average(a => a.IT.XreaMean));
                default: return 0;
            }

        }

        private static List<Model.Run> GetRun(string machine, bool isFAIMS, bool isOT)
        {
            List<Model.Run> _runs = null;
            (List<MachineQueue> queue, List<Model.Run> evaluation, List<MachineLog> log) prop;
            Management.Machines_Properties.TryGetValue(machine, out prop);
            if (prop == (null, null, null))
                return null;

            if (isOT)
            {
                _runs = prop.evaluation.AsParallel().Where(a => a.OT.Exclude == false && a.InfoStatus == Management.InfoStatus.Active).ToList();
                if (isFAIMS)
                    _runs = _runs.AsParallel().Where(a => a.OT.FAIMS == true).ToList();
                else
                    _runs = _runs.AsParallel().Where(a => a.OT.FAIMS == false).ToList();
            }
            else
            {
                _runs = prop.evaluation.AsParallel().Where(a => a.IT.Exclude == false && a.InfoStatus == Management.InfoStatus.Active).ToList();
                if (isFAIMS)
                    _runs = _runs.AsParallel().Where(a => a.IT.FAIMS == true).ToList();
                else
                    _runs = _runs.AsParallel().Where(a => a.IT.FAIMS == false).ToList();
            }

            return _runs;
        }

        public static (double min, double max) GetThresholds(List<DataPoint> points)
        {
            if (points == null || points.Count == 0) return (0, 0);

            double minimum = points.Select(a => a.MinValue).Min();
            double maximum = points.Select(a => a.MaxValue).Max();

            double absMax = Math.Max(Math.Abs(minimum), Math.Abs(maximum));

            return (-absMax, absMax);
        }
        #endregion

        #region Machine Queue
        [STAThread]
        internal static void CallQueueTaskRun(string machine, Grid mainGrid)
        {
            Connection.Refresh_time = int.MinValue;
            ComputeAllQueueDataPoints(machine, dt_start, dt_end, exactPeriod_bool, monthPeriod_bool, yearPeriod_bool);
            Application.Current.Dispatcher.Invoke(() =>
            {
                CreateAllQueuePlots(machine, mainGrid);
                Dict_Queue_points.Clear();
            });

        }
        public static void ComputeAllQueueDataPoints(string machine,
          DateTime dt_start,
          DateTime dt_end,
          bool exactPeriod,
          bool monthPeriod,
          bool yearPeriod)
        {
            Dict_Queue_points.Clear();
            var data_points_measured_samples_period = GetQueuePoints(machine, dt_start, dt_end, exactPeriod, monthPeriod, yearPeriod, false, true);
            var data_points_all_samples_period = GetQueuePoints(machine, dt_start, dt_end, exactPeriod, monthPeriod, yearPeriod, false, false);

            var data_points_measured_samples_user = GetQueuePoints(machine, dt_start, dt_end, exactPeriod, monthPeriod, yearPeriod, true, true);
            var data_points_all_samples_user = GetQueuePoints(machine, dt_start, dt_end, exactPeriod, monthPeriod, yearPeriod, true, false);
            Dict_Queue_points.Add(machine, (data_points_measured_samples_period, data_points_all_samples_period, data_points_measured_samples_user, data_points_all_samples_user));
        }
        private static List<DataPoint> GetQueuePoints(string machine,
                                              DateTime dt_start,
                                              DateTime dt_end,
                                              bool exactPeriod,
                                              bool monthPeriod,
                                              bool yearPeriod,
                                              bool isPerUser,
                                              bool isMeasured)
        {
            List<(Q2C.Model.Project sample, DateTime RegistrationDate)> _set = null;
            (List<MachineQueue> queue, List<Model.Run> evaluation, List<MachineLog> log) prop;
            Management.Machines_Properties.TryGetValue(machine, out prop);
            if (prop == (null, null, null))
                return new();

            if (isMeasured)
                _set = (from queue in prop.queue.AsParallel()
                        from project in Management.Projects.AsParallel()
                        where queue.InfoStatus == Management.InfoStatus.Deleted &&
                                project.InfoStatus == Management.InfoStatus.Active &&
                                project.Status == ProjectStatus.Measured &&
                                queue.ProjectID == project.Id &&
                                project.Machine == machine
                        select (project, queue.RegistrationDate))
                        .GroupBy(x => x.project.Id)
                        .Select(g => g.First())
                        .ToList();
            else // All samples
                _set = (from queue in prop.queue.AsParallel()
                        from project in Management.Projects.AsParallel()
                        where project.InfoStatus == Management.InfoStatus.Active &&
                              project.Status != ProjectStatus.WaitForAcquisition &&
                              queue.ProjectID == project.Id &&
                              project.Machine == machine
                        select (project, queue.RegistrationDate))
                        .GroupBy(x => x.project.Id)
                        .Select(g => g.First())
                        .ToList();

            if (isPerUser == true)
                return GetGroupedSamplesByUser(_set.Where(a => a.RegistrationDate >= dt_start && a.RegistrationDate <= dt_end).ToList());
            else if (exactPeriod == true)
                return GetGroupedSamplesByExactPeriod(_set.Where(a => a.RegistrationDate >= dt_start && a.RegistrationDate <= dt_end).ToList());
            else if (monthPeriod == true)
                return GetGroupedSamplesByMonthPeriod(_set.Where(a => DateTime.Compare(new DateTime(dt_start.Year, dt_start.Month, 1), new DateTime(a.RegistrationDate.Year, a.RegistrationDate.Month, 1)) < 0 && DateTime.Compare(a.RegistrationDate, new DateTime(dt_end.Year, dt_end.Month + 1, 1)) < 0).ToList());
            else if (yearPeriod == true)
                return GetGroupedSamplesByYearPeriod(_set.Where(a => a.RegistrationDate.Year >= dt_start.Year && a.RegistrationDate.Year <= dt_end.Year).ToList());

            return new();
        }
        private static List<DataPoint> GetGroupedSamplesByUser(List<(Q2C.Model.Project project, DateTime RegistrationDate)> _set)
        {
            List<DataPoint> list = new();
            var grouped = _set.GroupBy(a => a.project.AddedBy).Select(group => new
            {
                User = group.Key,
                Events = group.OrderBy(item => item.RegistrationDate)
            });
            foreach (var value in grouped)
                list.Add(new DataPoint(value.User, value.Events.Select(a => Convert.ToInt32(a.project.NumberOfSamples)).Sum()));
            return list;
        }
        private static List<DataPoint> GetGroupedSamplesByExactPeriod(List<(Q2C.Model.Project project, DateTime RegistrationDate)> _set)
        {
            List<DataPoint> list = new();
            var grouped = _set.GroupBy(dt => new
            {
                Year = dt.RegistrationDate.Year,
                Month = dt.RegistrationDate.Month,
                Day = dt.RegistrationDate.Day,
            }).Select(group => new
            {
                Date = group.Key,
                Events = group.OrderBy(item => item.RegistrationDate)
            });
            foreach (var value in grouped)
                list.Add(new DataPoint((value.Date.Day + "/" + value.Date.Month + "/" + value.Date.Year), value.Events.Select(a => Convert.ToInt32(a.project.NumberOfSamples)).Sum()));
            return list;
        }
        private static List<DataPoint> GetGroupedSamplesByMonthPeriod(List<(Q2C.Model.Project project, DateTime RegistrationDate)> _set)
        {
            List<DataPoint> list = new();
            var grouped = _set.GroupBy(dt => new
            {
                Year = dt.RegistrationDate.Year,
                Month = dt.RegistrationDate.Month,
            });
            foreach (var value in grouped)
                list.Add(new DataPoint((value.Key.Month + "/" + value.Key.Year), value.Select(a => Convert.ToInt32(a.project.NumberOfSamples)).Sum()));

            return list;
        }
        private static List<DataPoint> GetGroupedSamplesByYearPeriod(List<(Q2C.Model.Project project, DateTime RegistrationDate)> _set)
        {
            List<DataPoint> list = new();
            var grouped = _set.GroupBy(dt => new
            {
                Year = dt.RegistrationDate.Year,
            });
            foreach (var value in grouped)
                list.Add(new DataPoint("" + value.Key.Year, value.Select(a => Convert.ToInt32(a.project.NumberOfSamples)).Sum()));

            return list;
        }

        #endregion

        #endregion

        #region Generate interface
        public static System.Windows.Controls.DataVisualization.Charting.Chart CreateHistogramChart(string chartName,
            List<double> allZScores,
            string title,
            int binCount = 6)
        {
            if (allZScores == null || allZScores.Count == 0)
                throw new ArgumentException("Z-score list is empty.");

            // Calculate min, max, and bin width
            double min = allZScores.Min();
            double max = allZScores.Max();
            double binWidth = (max - min) / binCount;

            // Generate histogram bins
            Dictionary<string, int> histogram = new Dictionary<string, int>();
            for (int i = 0; i < binCount; i++)
            {
                double binStart = min + (i * binWidth);
                double binEnd = binStart + binWidth;
                string binLabel = $"{binStart:F2} - {binEnd:F2}";
                histogram[binLabel] = 0;
            }

            binWidth = binWidth == 0 ? 1 : binWidth;//avoid division by zero

            // Assign values to bins
            foreach (var z in allZScores)
            {
                int binIndex = (int)((z - min) / binWidth);
                binIndex = Math.Min(binIndex, binCount - 1); // Ensure the last value fits
                string binKey = histogram.Keys.ElementAt(binIndex);
                histogram[binKey]++;
            }

            // Create Chart
            System.Windows.Controls.DataVisualization.Charting.Chart chart = new System.Windows.Controls.DataVisualization.Charting.Chart
            {
                Name = chartName,
                Title = title,
                Margin = new System.Windows.Thickness(0, 30, 0, 0),
                LegendStyle = null,
            };

            ColumnSeries columnSeries = new ColumnSeries
            {
                IndependentValuePath = "Key",
                DependentValuePath = "Value",
                ItemsSource = histogram
            };

            chart.Series.Add(columnSeries);

            System.Windows.Style legendStyle = new System.Windows.Style(typeof(System.Windows.Controls.Control));
            legendStyle.Setters.Add(new Setter(System.Windows.Controls.Control.WidthProperty, 0.0));
            legendStyle.Setters.Add(new Setter(System.Windows.Controls.Control.HeightProperty, 0.0));
            legendStyle.Setters.Add(new Setter(System.Windows.Controls.Control.BorderBrushProperty, Brushes.White));
            chart.LegendStyle = legendStyle;

            chart.PlotAreaStyle = new System.Windows.Style(typeof(Grid));
            chart.PlotAreaStyle.Setters.Add(new Setter(Grid.BackgroundProperty, Brushes.White));


            return chart;
        }

        public static System.Windows.Controls.DataVisualization.Charting.Chart CreateScatterChart(string chartName,
           List<DataPoint> allRuns,
           List<DataPoint> ransac,
           List<DataPoint> ransacMinusStd,
           List<DataPoint> ransacPlusStd,
           string title,
           bool hasYlimit,
           double minimum = 0,
           double maximum = 100)
        {
            #region evaluation points series
            System.Windows.Controls.DataVisualization.Charting.ScatterSeries scatterSeriesEvaluationPoints = new System.Windows.Controls.DataVisualization.Charting.ScatterSeries();
            scatterSeriesEvaluationPoints.Title = "Measure";
            scatterSeriesEvaluationPoints.IndependentValuePath = "X";
            scatterSeriesEvaluationPoints.DependentValuePath = "Y";

            System.Windows.Style dataPointEvaluationStyle = new System.Windows.Style(typeof(ScatterDataPoint));
            dataPointEvaluationStyle.Setters.Add(new EventSetter(ScatterDataPoint.MouseEnterEvent, new MouseEventHandler(DataPoint_MouseEnter)));
            dataPointEvaluationStyle.Setters.Add(new Setter(ScatterDataPoint.BackgroundProperty, new SolidColorBrush(Color.FromRgb(71, 130, 136))));
            dataPointEvaluationStyle.Setters.Add(new Setter(ScatterDataPoint.IsTabStopProperty, false));
            dataPointEvaluationStyle.Setters.Add(new Setter(ScatterDataPoint.BorderBrushProperty, new SolidColorBrush(Color.FromRgb(104, 104, 104))));
            dataPointEvaluationStyle.Setters.Add(new Setter(ScatterDataPoint.WidthProperty, 8.0));
            dataPointEvaluationStyle.Setters.Add(new Setter(ScatterDataPoint.HeightProperty, 8.0));

            DataTrigger dataTriggerHighlight = new DataTrigger();
            dataTriggerHighlight.Binding = new Binding("Highlight");
            dataTriggerHighlight.Value = true;

            ControlTemplate controlHighlightTemplate = new ControlTemplate(typeof(ScatterDataPoint));
            FrameworkElementFactory rectangleHighlightFactory = new FrameworkElementFactory(typeof(Rectangle));
            rectangleHighlightFactory.SetValue(Rectangle.FillProperty, Brushes.Red);
            rectangleHighlightFactory.SetValue(Rectangle.StrokeProperty, new SolidColorBrush(Color.FromRgb(104, 104, 104)));

            controlHighlightTemplate.VisualTree = rectangleHighlightFactory;

            dataTriggerHighlight.Setters.Add(new Setter(ScatterDataPoint.TemplateProperty, controlHighlightTemplate));
            dataPointEvaluationStyle.Triggers.Add(dataTriggerHighlight);

            DataTrigger dataTrigger = new DataTrigger();
            dataTrigger.Binding = new Binding("Highlight");
            dataTrigger.Value = false;

            ControlTemplate controlTemplate = new ControlTemplate(typeof(ScatterDataPoint));
            FrameworkElementFactory rectangleFactory = new FrameworkElementFactory(typeof(Ellipse));
            rectangleFactory.SetValue(Ellipse.FillProperty, new SolidColorBrush(Color.FromRgb(71, 130, 136)));
            rectangleFactory.SetValue(Ellipse.StrokeProperty, new SolidColorBrush(Color.FromRgb(104, 104, 104)));

            controlTemplate.VisualTree = rectangleFactory;

            dataTrigger.Setters.Add(new Setter(ScatterDataPoint.TemplateProperty, controlTemplate));
            dataPointEvaluationStyle.Triggers.Add(dataTrigger);

            scatterSeriesEvaluationPoints.DataPointStyle = dataPointEvaluationStyle;
            scatterSeriesEvaluationPoints.ItemsSource = allRuns;
            #endregion

            #region ransac series
            System.Windows.Style dataPointRansacStyle = new System.Windows.Style(typeof(LineDataPoint));
            dataPointRansacStyle.Setters.Add(new Setter(LineDataPoint.BackgroundProperty, Brushes.LightBlue));
            dataPointRansacStyle.Setters.Add(new Setter(LineDataPoint.IsTabStopProperty, false));
            dataPointRansacStyle.Setters.Add(new Setter(LineDataPoint.BorderBrushProperty, Brushes.LightBlue));
            dataPointRansacStyle.Setters.Add(new Setter(LineDataPoint.WidthProperty, 1.0));
            dataPointRansacStyle.Setters.Add(new Setter(LineDataPoint.HeightProperty, 1.0));

            System.Windows.Controls.DataVisualization.Charting.LineSeries scatterSeriesRansac = new System.Windows.Controls.DataVisualization.Charting.LineSeries();
            scatterSeriesRansac.Title = "Ransac";
            scatterSeriesRansac.IndependentValuePath = "X";
            scatterSeriesRansac.DependentValuePath = "Y";
            scatterSeriesRansac.ItemsSource = ransac;
            scatterSeriesRansac.DataPointStyle = dataPointRansacStyle;

            // Create a Style for the legend item
            System.Windows.Style linearLegendItemStyle = new System.Windows.Style(typeof(LegendItem));
            // Create a ControlTemplate for the legend item
            ControlTemplate legendItemTemplate = new ControlTemplate(typeof(LegendItem));
            // Create a StackPanel to hold the line and text
            FrameworkElementFactory stackPanelFactory = new FrameworkElementFactory(typeof(StackPanel));
            stackPanelFactory.SetValue(StackPanel.OrientationProperty, System.Windows.Controls.Orientation.Horizontal);
            // Create a Rectangle element (line symbol)
            FrameworkElementFactory rectangleFactoryLegend = new FrameworkElementFactory(typeof(Rectangle));
            rectangleFactoryLegend.SetValue(Rectangle.WidthProperty, 8.0);
            rectangleFactoryLegend.SetValue(Rectangle.HeightProperty, 2.0);
            rectangleFactoryLegend.SetBinding(Rectangle.FillProperty, new Binding("Background"));
            // Create a TextBlock element
            FrameworkElementFactory textBlockFactory = new FrameworkElementFactory(typeof(TextBlock));
            textBlockFactory.SetValue(TextBlock.TextProperty, "Ransac");
            textBlockFactory.SetValue(TextBlock.MarginProperty, new System.Windows.Thickness(4, 0, 0, 0));
            // Add the Rectangle and TextBlock to the StackPanel
            stackPanelFactory.AppendChild(rectangleFactoryLegend);
            stackPanelFactory.AppendChild(textBlockFactory);
            // Set the StackPanel as the visual tree of the LegendItem
            legendItemTemplate.VisualTree = stackPanelFactory; // Set the ControlTemplate for the LegendItem Style
            linearLegendItemStyle.Setters.Add(new Setter(LegendItem.TemplateProperty, legendItemTemplate)); // Apply the style to a specific LinearSeries

            scatterSeriesRansac.LegendItemStyle = linearLegendItemStyle;

            System.Windows.Style dataPointRansacSigmaStyle = new System.Windows.Style(typeof(LineDataPoint));
            dataPointRansacSigmaStyle.Setters.Add(new Setter(LineDataPoint.BackgroundProperty, Brushes.LightBlue));
            dataPointRansacSigmaStyle.Setters.Add(new Setter(LineDataPoint.IsTabStopProperty, false));
            dataPointRansacSigmaStyle.Setters.Add(new Setter(LineDataPoint.BorderBrushProperty, Brushes.LightBlue));
            dataPointRansacSigmaStyle.Setters.Add(new Setter(LineDataPoint.WidthProperty, 1.0));
            dataPointRansacSigmaStyle.Setters.Add(new Setter(LineDataPoint.HeightProperty, 1.0));

            System.Windows.Style dashedLineStyle = new System.Windows.Style(typeof(Polyline));
            dashedLineStyle.Setters.Add(new Setter(System.Windows.Shapes.Shape.StrokeDashArrayProperty, new DoubleCollection(new[] { 5.0 })));

            System.Windows.Controls.DataVisualization.Charting.LineSeries scatterSeriesRansacMinusStd = new System.Windows.Controls.DataVisualization.Charting.LineSeries();
            //Just to show in the legend
            scatterSeriesRansacMinusStd.Title = "Last measure";
            scatterSeriesRansacMinusStd.IndependentValuePath = "X";
            scatterSeriesRansacMinusStd.DependentValuePath = "Y";
            scatterSeriesRansacMinusStd.ItemsSource = ransacMinusStd;
            scatterSeriesRansacMinusStd.DataPointStyle = dataPointRansacSigmaStyle;
            scatterSeriesRansacMinusStd.PolylineStyle = dashedLineStyle;

            #region legend last measurement
            System.Windows.Style linearLegendLastItemStyle = new System.Windows.Style(typeof(LegendItem));
            // Create a ControlTemplate for the LegendLast item
            ControlTemplate LegendLastItemTemplate = new ControlTemplate(typeof(LegendItem));
            // Create a StackPanel to hold the line and text
            FrameworkElementFactory stackPanelFactoryLast = new FrameworkElementFactory(typeof(StackPanel));
            stackPanelFactoryLast.SetValue(StackPanel.OrientationProperty, System.Windows.Controls.Orientation.Horizontal);
            // Create a Rectangle element (line symbol)
            FrameworkElementFactory rectangleFactoryLegendLast = new FrameworkElementFactory(typeof(Rectangle));
            rectangleFactoryLegendLast.SetValue(Rectangle.WidthProperty, 8.0);
            rectangleFactoryLegendLast.SetValue(Rectangle.HeightProperty, 8.0);
            rectangleFactoryLegendLast.SetValue(Rectangle.FillProperty, Brushes.Red);
            rectangleFactoryLegendLast.SetValue(Rectangle.StrokeProperty, new SolidColorBrush(Color.FromRgb(104, 104, 104)));
            // Create a TextBlock element
            FrameworkElementFactory textBlockFactoryLast = new FrameworkElementFactory(typeof(TextBlock));
            textBlockFactoryLast.SetValue(TextBlock.TextProperty, "Last measure");
            textBlockFactoryLast.SetValue(TextBlock.MarginProperty, new System.Windows.Thickness(4, 0, 0, 0));
            // Add the Rectangle and TextBlock to the StackPanel
            stackPanelFactoryLast.AppendChild(rectangleFactoryLegendLast);
            stackPanelFactoryLast.AppendChild(textBlockFactoryLast);
            // Set the StackPanel as the visual tree of the LegendLastItem
            LegendLastItemTemplate.VisualTree = stackPanelFactoryLast; // Set the ControlTemplate for the LegendLastItem Style
            linearLegendLastItemStyle.Setters.Add(new Setter(LegendItem.TemplateProperty, LegendLastItemTemplate)); // Apply the style to a specific LinearSeries
            scatterSeriesRansacMinusStd.LegendItemStyle = linearLegendLastItemStyle;
            #endregion

            System.Windows.Controls.DataVisualization.Charting.LineSeries scatterSeriesRansacPlusStd = new System.Windows.Controls.DataVisualization.Charting.LineSeries();
            scatterSeriesRansacPlusStd.Title = "";
            scatterSeriesRansacPlusStd.IndependentValuePath = "X";
            scatterSeriesRansacPlusStd.DependentValuePath = "Y";
            scatterSeriesRansacPlusStd.ItemsSource = ransacPlusStd;
            scatterSeriesRansacPlusStd.DataPointStyle = dataPointRansacSigmaStyle;
            scatterSeriesRansacPlusStd.PolylineStyle = dashedLineStyle;

            System.Windows.Style hideLegendStyle = new System.Windows.Style(typeof(LegendItem));
            hideLegendStyle.Setters.Add(new Setter(LegendItem.WidthProperty, 0.0));
            hideLegendStyle.Setters.Add(new Setter(LegendItem.HeightProperty, 0.0));
            scatterSeriesRansacPlusStd.LegendItemStyle = hideLegendStyle;

            #endregion

            #region axis

            // Y Axis
            System.Windows.Controls.DataVisualization.Charting.LinearAxis yAxis = new System.Windows.Controls.DataVisualization.Charting.LinearAxis();
            yAxis.Orientation = AxisOrientation.Y;
            yAxis.Title = title;
            System.Windows.Style titleStyleY = new System.Windows.Style(typeof(System.Windows.Controls.Control));
            titleStyleY.Setters.Add(new Setter(System.Windows.Controls.Control.MarginProperty, new System.Windows.Thickness(0, 0, 0, 20)));
            yAxis.TitleStyle = titleStyleY;
            yAxis.Margin = new System.Windows.Thickness(0, 0, 0, 0);

            if (hasYlimit == true)
            {
                yAxis.Minimum = minimum;
                yAxis.Maximum = maximum;
            }

            // X Axis
            System.Windows.Controls.DataVisualization.Charting.CategoryAxis xAxis = new System.Windows.Controls.DataVisualization.Charting.CategoryAxis { Orientation = AxisOrientation.X };
            System.Windows.Style axisLabelStyle = new System.Windows.Style(typeof(AxisLabel));
            axisLabelStyle.Setters.Add(new Setter(AxisLabel.LayoutTransformProperty, new RotateTransform(45)));
            xAxis.AxisLabelStyle = axisLabelStyle;

            #endregion

            System.Windows.Controls.DataVisualization.Charting.Chart chart = new System.Windows.Controls.DataVisualization.Charting.Chart();
            chart.Name = chartName;
            chart.BorderThickness = new System.Windows.Thickness(0);

            // Create a LegendStyle
            System.Windows.Style legendStyle = new System.Windows.Style(typeof(System.Windows.Controls.Control));
            legendStyle.Setters.Add(new Setter(System.Windows.Controls.Control.WidthProperty, 0.0));
            legendStyle.Setters.Add(new Setter(System.Windows.Controls.Control.HeightProperty, 0.0));
            legendStyle.Setters.Add(new Setter(System.Windows.Controls.Control.BorderBrushProperty, Brushes.White));
            chart.LegendStyle = legendStyle;

            chart.Axes.Add(xAxis);
            chart.Axes.Add(yAxis);
            chart.PlotAreaStyle = new System.Windows.Style(typeof(Grid));
            chart.PlotAreaStyle.Setters.Add(new Setter(Grid.BackgroundProperty, Brushes.White));
            chart.PlotAreaStyle.Setters.Add(new Setter(Grid.MarginProperty, new System.Windows.Thickness(200, 200, 200, 200)));

            // Add the ScatterSeries to the chart
            chart.Series.Add(scatterSeriesEvaluationPoints);
            chart.Series.Add(scatterSeriesRansac);
            chart.Series.Add(scatterSeriesRansacMinusStd);
            chart.Series.Add(scatterSeriesRansacPlusStd);

            return chart;
        }

        private static void DataPoint_MouseEnter(object sender, MouseEventArgs e)
        {
            try
            {
                ((ScatterDataPoint)sender).ToolTip = ((DataPoint)((ScatterDataPoint)sender).DataContext).ToolTip;
            }
            catch (Exception)
            {
            }
        }

        public static StackPanel CreateLegend(System.Windows.Controls.DataVisualization.Charting.Chart chart,
            string panelName)
        {
            StackPanel stackPanel = new();
            stackPanel.Name = panelName;

            if (chart == null) return stackPanel;

            foreach (LegendItem legendItem in chart.LegendItems)
            {
                string legendContent = legendItem != null && legendItem.Content != null ? legendItem.Content.ToString() : "";
                if (String.IsNullOrEmpty(legendContent)) continue;

                Rectangle symbol = new Rectangle();
                symbol.Width = 8;
                symbol.Margin = new System.Windows.Thickness(0, 0, 5, 0);

                if (legendContent.Equals("Measure") || legendContent.Equals("Last measure"))
                {
                    if (legendContent.Equals("Last measure"))
                    {
                        symbol.Height = 8;
                        symbol.Fill = new SolidColorBrush(Colors.Red);
                        symbol.Stroke = new SolidColorBrush(Color.FromRgb(104, 104, 104));
                        stackPanel.Children.Add(symbol);
                    }
                    else
                    {
                        Ellipse symbol_measure = new Ellipse();
                        symbol_measure.Width = 8;
                        symbol_measure.Height = 8;
                        symbol_measure.Margin = new System.Windows.Thickness(0, 0, 5, 0);
                        symbol_measure.Fill = new SolidColorBrush(Color.FromRgb(71, 130, 136));
                        symbol_measure.Stroke = new SolidColorBrush(Color.FromRgb(104, 104, 104));
                        stackPanel.Children.Add(symbol_measure);
                    }
                }
                else
                {
                    symbol.Height = 2;
                    symbol.Fill = new SolidColorBrush(Colors.LightBlue);
                    symbol.Stroke = new SolidColorBrush(Colors.LightBlue);
                    stackPanel.Children.Add(symbol);
                }

                TextBlock textBlock = new TextBlock();
                textBlock.Text = legendContent;
                textBlock.Margin = new System.Windows.Thickness(0, 0, 8, 0);
                stackPanel.Children.Add(textBlock);
            }

            return stackPanel;
        }

        public static (PlotModel plotModelOT, PlotModel plotModelIT) CreateBoxPlots(
            List<DataPoint> pointsOT,
            List<DataPoint> pointsIT,
            bool isFAIMS,
            bool hasYlimit)
        {
            (double min, double max) max_min_points = (0, 0);
            if (hasYlimit)
            {
                var max_min_pointsOT = GetThresholds(pointsOT);
                var max_min_pointsIT = GetThresholds(pointsIT);
                max_min_points = (Math.Min(max_min_pointsOT.min, max_min_pointsIT.min), Math.Max(max_min_pointsOT.max, max_min_pointsIT.max));
            }

            #region OT
            if (isFAIMS)
                Fill_boxPlot_xAxis_Date_CollectionFAIMS(pointsOT, true);
            else
                Fill_boxPlot_xAxis_Date_CollectionNoFAIMS(pointsOT, true);

            if (hasYlimit == false)
                max_min_points = GetThresholds(pointsOT);

            // Create the plot model
            var plotModelOT = new PlotModel();

            // Create the box plot series
            var boxPlotSeriesOT = new BoxPlotSeries
            {
                BoxWidth = 0.6, // Adjust the width of the boxes
            };
            boxPlotSeriesOT.Fill = OxyColor.FromArgb(90, 71, 130, 136);

            //X axis -> the dates
            var _x_axis = new OxyPlot.Axes.CategoryAxis
            {
                Position = OxyPlot.Axes.AxisPosition.Bottom,
                LabelField = "Label",
                IsTickCentered = true,
                TickStyle = TickStyle.None,
                Minimum = 0,
                Angle = 45
            };
            if (isFAIMS)
            {
                _x_axis.Maximum = BoxPlot_xAxis_DateOTFAIMS.Count + 1;
                _x_axis.LabelFormatter = LabelFormatOTFAIMS;
            }
            else
            {
                _x_axis.Maximum = BoxPlot_xAxis_DateOTNoFAIMS.Count + 1;
                _x_axis.LabelFormatter = LabelFormatOTNoFAIMS;
            }

            plotModelOT.Axes.Add(_x_axis);

            plotModelOT.Axes.Add(new OxyPlot.Axes.LinearAxis
            {
                Position = OxyPlot.Axes.AxisPosition.Left,
                MajorStep = 1,
                MinorStep = 0.25,
                TickStyle = TickStyle.Crossing
            });

            var lineAnnotationOT = new LineAnnotation
            {
                Type = LineAnnotationType.Horizontal,
                Y = (max_min_points.min + max_min_points.max) / 2,
                LineStyle = OxyPlot.LineStyle.Dash,
                StrokeThickness = 1,
                Color = OxyColor.FromArgb(50, 0, 0, 0)
            };
            plotModelOT.Annotations.Add(lineAnnotationOT);

            lineAnnotationOT = new LineAnnotation
            {
                Type = LineAnnotationType.Horizontal,
                Y = max_min_points.min,
                LineStyle = OxyPlot.LineStyle.Dash,
                StrokeThickness = 1,
                Color = OxyColor.FromArgb(50, 0, 0, 0)
            };
            plotModelOT.Annotations.Add(lineAnnotationOT);

            lineAnnotationOT = new LineAnnotation
            {
                Type = LineAnnotationType.Horizontal,
                Y = max_min_points.max,
                LineStyle = OxyPlot.LineStyle.Dash,
                StrokeThickness = 1,
                Color = OxyColor.FromArgb(50, 0, 0, 0)
            };
            plotModelOT.Annotations.Add(lineAnnotationOT);

            for (int i = 0; i < pointsOT.Count; i++)
            {
                DataPoint dp = pointsOT[i];
                var boxItem = new BoxPlotItem(i + 1, dp.MinValue, dp.MinValue, dp.MedianValue, dp.MaxValue, dp.MaxValue);
                boxPlotSeriesOT.Items.Add(boxItem);
            }

            // Add the box plot series to the plot model
            plotModelOT.Series.Add(boxPlotSeriesOT);

            plotModelOT.Axes[1].Minimum = max_min_points.min * 1.1;
            plotModelOT.Axes[1].Maximum = max_min_points.max * 1.1;

            #endregion

            #region IT

            if (isFAIMS)
                Fill_boxPlot_xAxis_Date_CollectionFAIMS(pointsIT, false);
            else
                Fill_boxPlot_xAxis_Date_CollectionNoFAIMS(pointsIT, false);

            if (hasYlimit == false)
                max_min_points = GetThresholds(pointsIT);

            // Create the plot model
            var plotModelIT = new PlotModel();

            // Create the box plot series
            var boxPlotSeriesIT = new BoxPlotSeries
            {
                BoxWidth = 0.6, // Adjust the width of the boxes
            };
            boxPlotSeriesIT.Fill = OxyColor.FromArgb(90, 71, 130, 136);

            //X axis -> the dates
            var _x_axisIT = new OxyPlot.Axes.CategoryAxis
            {
                Position = OxyPlot.Axes.AxisPosition.Bottom,
                LabelField = "Label",
                IsTickCentered = true,
                TickStyle = TickStyle.None,
                Minimum = 0,
                Angle = 45
            };
            if (isFAIMS)
            {
                _x_axisIT.Maximum = BoxPlot_xAxis_DateITFAIMS.Count + 1;
                _x_axisIT.LabelFormatter = LabelFormatITFAIMS;
            }
            else
            {
                _x_axisIT.Maximum = BoxPlot_xAxis_DateITNoFAIMS.Count + 1;
                _x_axisIT.LabelFormatter = LabelFormatITNoFAIMS;
            }
            plotModelIT.Axes.Add(_x_axisIT);

            plotModelIT.Axes.Add(new OxyPlot.Axes.LinearAxis
            {
                Position = OxyPlot.Axes.AxisPosition.Left,
                MajorStep = 1,
                MinorStep = 0.25,
                TickStyle = TickStyle.Crossing
            });

            var lineAnnotationIT = new LineAnnotation
            {
                Type = LineAnnotationType.Horizontal,
                Y = (max_min_points.min + max_min_points.max) / 2,
                LineStyle = OxyPlot.LineStyle.Dash,
                StrokeThickness = 1,
                Color = OxyColor.FromArgb(50, 0, 0, 0)
            };
            plotModelIT.Annotations.Add(lineAnnotationIT);

            lineAnnotationIT = new LineAnnotation
            {
                Type = LineAnnotationType.Horizontal,
                Y = max_min_points.min,
                LineStyle = OxyPlot.LineStyle.Dash,
                StrokeThickness = 1,
                Color = OxyColor.FromArgb(50, 0, 0, 0)
            };
            plotModelIT.Annotations.Add(lineAnnotationIT);

            lineAnnotationIT = new LineAnnotation
            {
                Type = LineAnnotationType.Horizontal,
                Y = max_min_points.max,
                LineStyle = OxyPlot.LineStyle.Dash,
                StrokeThickness = 1,
                Color = OxyColor.FromArgb(50, 0, 0, 0)
            };
            plotModelIT.Annotations.Add(lineAnnotationIT);

            for (int i = 0; i < pointsIT.Count; i++)
            {
                DataPoint dp = pointsIT[i];
                var boxItem = new BoxPlotItem(i + 1, dp.MinValue, dp.MinValue, dp.MedianValue, dp.MaxValue, dp.MaxValue);
                boxPlotSeriesIT.Items.Add(boxItem);
            }

            // Add the box plot series to the plot model
            plotModelIT.Series.Add(boxPlotSeriesIT);

            plotModelIT.Axes[1].Minimum = max_min_points.min * 1.1;
            plotModelIT.Axes[1].Maximum = max_min_points.max * 1.1;

            #endregion

            return (plotModelOT, plotModelIT);
        }

        private static string LabelFormatOTFAIMS(double arg)
        {
            if (arg > 0 && arg <= BoxPlot_xAxis_DateOTFAIMS.Count)
                return BoxPlot_xAxis_DateOTFAIMS[(int)arg - 1];
            else
                return "";
        }
        private static string LabelFormatITFAIMS(double arg)
        {
            if (arg > 0 && arg <= BoxPlot_xAxis_DateITFAIMS.Count)
                return BoxPlot_xAxis_DateITFAIMS[(int)arg - 1];
            else
                return "";
        }
        private static string LabelFormatOTNoFAIMS(double arg)
        {
            if (arg > 0 && arg <= BoxPlot_xAxis_DateOTNoFAIMS.Count)
                return BoxPlot_xAxis_DateOTNoFAIMS[(int)arg - 1];
            else
                return "";
        }
        private static string LabelFormatITNoFAIMS(double arg)
        {
            if (arg > 0 && arg <= BoxPlot_xAxis_DateITNoFAIMS.Count)
                return BoxPlot_xAxis_DateITNoFAIMS[(int)arg - 1];
            else
                return "";
        }
        private static void Fill_boxPlot_xAxis_Date_CollectionFAIMS(List<DataPoint> points, bool isOT)
        {
            if (isOT)
            {
                if (BoxPlot_xAxis_DateOTFAIMS == null)
                    BoxPlot_xAxis_DateOTFAIMS = new();
                else
                    BoxPlot_xAxis_DateOTFAIMS.Clear();

                foreach (var point in points)
                    BoxPlot_xAxis_DateOTFAIMS.Add(point.X);
            }
            else
            {
                if (BoxPlot_xAxis_DateITFAIMS == null)
                    BoxPlot_xAxis_DateITFAIMS = new();
                else
                    BoxPlot_xAxis_DateITFAIMS.Clear();

                foreach (var point in points)
                    BoxPlot_xAxis_DateITFAIMS.Add(point.X);
            }
        }
        private static void Fill_boxPlot_xAxis_Date_CollectionNoFAIMS(List<DataPoint> points, bool isOT)
        {
            if (isOT)
            {
                if (BoxPlot_xAxis_DateOTNoFAIMS == null)
                    BoxPlot_xAxis_DateOTNoFAIMS = new();
                else
                    BoxPlot_xAxis_DateOTNoFAIMS.Clear();

                foreach (var point in points)
                    BoxPlot_xAxis_DateOTNoFAIMS.Add(point.X);
            }
            else
            {
                if (BoxPlot_xAxis_DateITNoFAIMS == null)
                    BoxPlot_xAxis_DateITNoFAIMS = new();
                else
                    BoxPlot_xAxis_DateITNoFAIMS.Clear();

                foreach (var point in points)
                    BoxPlot_xAxis_DateITNoFAIMS.Add(point.X);
            }
        }

        #region create interface for plotting statistics of machines

        #region Machines Evaluation
        public static GroupBox CreatePeriodDataPicker(string machine, int main_property)
        {
            var groupBox = CreateGroupBox("Period");
            groupBox.Margin = new System.Windows.Thickness(0, 0, 0, 10);

            // Create a Grid
            Grid grid = new Grid();

            // Create RowDefinitions
            RowDefinition row1 = new RowDefinition();
            RowDefinition row2 = new RowDefinition();
            row1.Height = GridLength.Auto;
            row2.Height = GridLength.Auto;
            grid.RowDefinitions.Add(row1);
            grid.RowDefinitions.Add(row2);

            // Create the first StackPanel (containing DatePickers and Button)
            StackPanel stackPanel1 = new StackPanel();
            stackPanel1.Orientation = System.Windows.Controls.Orientation.Horizontal;
            Grid.SetRow(stackPanel1, 0);

            DatePicker stPeriod = new DatePicker();
            stPeriod.Margin = new System.Windows.Thickness(5);
            stPeriod.Name = "stPeriod" + machine;
            stPeriod.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
            stPeriod.VerticalAlignment = System.Windows.VerticalAlignment.Top;
            stPeriod.Width = 200;
            stPeriod.SelectedDate = DateTime.Now.AddYears(-1);

            TextBlock textBlock = new TextBlock();
            textBlock.Text = "to";
            textBlock.Margin = new System.Windows.Thickness(5);

            DatePicker endPeriod = new DatePicker();
            endPeriod.Margin = new System.Windows.Thickness(5);
            endPeriod.Name = "endPeriod" + machine;
            endPeriod.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
            endPeriod.VerticalAlignment = System.Windows.VerticalAlignment.Top;
            endPeriod.Width = 200;
            endPeriod.SelectedDate = DateTime.Now;

            System.Windows.Controls.Button buttonConfirm = new System.Windows.Controls.Button();
            if (main_property == 1)
                buttonConfirm.Name = "ButtonConfirm_" + machine + "_Queue";
            else
                buttonConfirm.Name = "ButtonConfirm_" + machine + "_Evaluation";
            buttonConfirm.Click += ButtonPlotConfirm_Click;
            buttonConfirm.Padding = new System.Windows.Thickness(10, 1, 10, 1);
            buttonConfirm.Height = 20;
            buttonConfirm.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
            buttonConfirm.Cursor = Cursors.Hand;

            StackPanel stackPanelButton = new StackPanel();
            stackPanelButton.Orientation = System.Windows.Controls.Orientation.Horizontal;

            DockPanel dockPanel = new DockPanel();
            Image image = new Image();
            image.Source = new BitmapImage(new Uri("/icons/ok.png", UriKind.Relative));
            dockPanel.Children.Add(image);

            TextBlock buttonTextBlock = new TextBlock();
            buttonTextBlock.Margin = new System.Windows.Thickness(5, 0, 0, 0);
            buttonTextBlock.Width = 45;
            buttonTextBlock.Text = "Confirm";

            stackPanelButton.Children.Add(dockPanel);
            stackPanelButton.Children.Add(buttonTextBlock);

            buttonConfirm.Content = stackPanelButton;

            stackPanel1.Children.Add(stPeriod);
            stackPanel1.Children.Add(textBlock);
            stackPanel1.Children.Add(endPeriod);
            stackPanel1.Children.Add(buttonConfirm);

            // Create the second StackPanel (containing RadioButtons & Checkbox)
            StackPanel stackPanel2 = new StackPanel();
            stackPanel2.Orientation = System.Windows.Controls.Orientation.Horizontal;
            Grid.SetRow(stackPanel2, 1);

            RadioButton exactPeriod = new RadioButton();
            exactPeriod.Margin = new System.Windows.Thickness(5);
            exactPeriod.Content = "Exact period";
            exactPeriod.IsChecked = true;
            exactPeriod.Name = "exactPeriod" + machine;

            RadioButton monthPeriod = new RadioButton();
            monthPeriod.Margin = new System.Windows.Thickness(5);
            monthPeriod.Content = "Month";
            monthPeriod.Name = "monthPeriod" + machine;

            RadioButton yearPeriod = new RadioButton();
            yearPeriod.Margin = new System.Windows.Thickness(5);
            yearPeriod.Content = "Year";
            yearPeriod.Name = "yearPeriod" + machine;

            stackPanel2.Children.Add(exactPeriod);
            stackPanel2.Children.Add(monthPeriod);
            stackPanel2.Children.Add(yearPeriod);

            if (main_property != 1)
            {
                CheckBox autoScale = new CheckBox();
                autoScale.Margin = new System.Windows.Thickness(25, 5, 5, 5);
                autoScale.Content = "Auto scale";
                autoScale.Name = "autoScale" + machine;
                autoScale.IsChecked = false;
                stackPanel2.Children.Add(autoScale);
            }

            // Add the StackPanels to the Grid
            grid.Children.Add(stackPanel1);
            grid.Children.Add(stackPanel2);

            // Add the Grid to the GroupBox
            groupBox.Content = grid;

            return groupBox;

        }

        internal static async void ButtonPlotConfirm_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Controls.Button buttonConfirm = (System.Windows.Controls.Button)(sender);
            string[] cols = Regex.Split(buttonConfirm.Name.Replace("ButtonConfirm_", ""), "_");
            string machine = cols[0];
            string property = cols[1];

            UIElement button = buttonConfirm;
            DependencyObject button_parent = VisualTreeHelper.GetParent(button);

            if (button_parent is StackPanel stack)
            {
                DatePicker stPeriod = GetDatePicker("stPeriod" + machine, stack);
                DatePicker endPeriod = GetDatePicker("endPeriod" + machine, stack);
                if (stPeriod == null || stPeriod.SelectedDate == null || stPeriod.SelectedDate.Value == null ||
                    endPeriod == null || endPeriod.SelectedDate == null || endPeriod.SelectedDate.Value == null) return;
                dt_start = stPeriod.SelectedDate.Value;
                dt_end = endPeriod.SelectedDate.Value;

                DependencyObject stack_parent = VisualTreeHelper.GetParent(stack);
                if (stack_parent is Grid grid)
                {
                    StackPanel stack2 = (StackPanel)grid.Children[1];
                    if (stack2 == null) return;
                    RadioButton exactPeriod = GetRadioButton("exactPeriod" + machine, stack2);
                    RadioButton monthPeriod = GetRadioButton("monthPeriod" + machine, stack2);
                    RadioButton yearPeriod = GetRadioButton("yearPeriod" + machine, stack2);
                    CheckBox autoScale = GetCheckBox("autoScale" + machine, stack2);
                    exactPeriod_bool = exactPeriod.IsChecked == true;
                    monthPeriod_bool = monthPeriod.IsChecked == true;
                    yearPeriod_bool = yearPeriod.IsChecked == true;
                    if (autoScale != null)
                        autoScale_bool = autoScale.IsChecked == true;

                    DependencyObject main_grid = VisualTreeHelper.GetParent(VisualTreeHelper.GetParent(VisualTreeHelper.GetParent(VisualTreeHelper.GetParent(VisualTreeHelper.GetParent(VisualTreeHelper.GetParent(VisualTreeHelper.GetParent(VisualTreeHelper.GetParent(VisualTreeHelper.GetParent(grid)))))))));
                    if (main_grid is Grid mainGrid)
                    {
                        var wait_screen = Util.Util.CallWaitWindow("Please wait...", "Generating plots...");
                        mainGrid.Children.Add(wait_screen);

                        if (property == "Queue")
                            await Task.Run(() => CallQueueTaskRun(machine, mainGrid));
                        else
                            await Task.Run(() => CallEvaluationTaskRun(machine, mainGrid));

                        mainGrid.Children.Remove(wait_screen);

                    }
                }
            }
        }

        internal static DatePicker GetDatePicker(string _name, StackPanel stackPanel)
        {
            if (stackPanel == null) return null;
            foreach (var child in stackPanel.Children)
            {
                if (child is DatePicker datePicker)
                {
                    if (datePicker.Name.Equals(_name))
                    {
                        return datePicker;
                    }
                }
            }
            return null;
        }
        internal static RadioButton GetRadioButton(string _name, StackPanel stackPanel)
        {
            if (stackPanel == null) return null;
            foreach (var child in stackPanel.Children)
            {
                if (child is RadioButton radioButton)
                {
                    if (radioButton.Name.Equals(_name))
                    {
                        return radioButton;
                    }
                }
            }
            return null;
        }
        internal static CheckBox GetCheckBox(string _name, StackPanel stackPanel)
        {
            if (stackPanel == null) return null;
            foreach (var child in stackPanel.Children)
            {
                if (child is CheckBox checkBox)
                {
                    if (checkBox.Name.Equals(_name))
                    {
                        return checkBox;
                    }
                }
            }
            return null;
        }
        internal static void SetScrollViewerOffset(string machine, Grid mainGrid)
        {
            ScrollViewer sv_faims = (ScrollViewer)RetrieveElement("sv_faims", "", "FAIMS", machine, true, mainGrid, 5);
            if (sv_faims != null)
                sv_faims.ScrollToVerticalOffset(0);
            ScrollViewer sv_no_faims = (ScrollViewer)RetrieveElement("sv_no_faims", "", "NoFAIMS", machine, true, mainGrid, 5);
            if (sv_no_faims != null)
                sv_no_faims.ScrollToVerticalOffset(0);

        }

        internal static void CreateAllEvaluationPlots(string machine, Grid mainGrid)
        {
            string objectName = machine.ToLower();

            CreateOT_and_ITPlots(mainGrid,
                machine,
                objectName,
                 true,
                 (autoScale_bool == false));
            CreateOT_and_ITPlots(mainGrid,
                machine,
                objectName,
                 false,
                 (autoScale_bool == false));
        }

        internal static void CreateOT_and_ITPlots(Grid mainGrid,
            string machine,
            string objectName,
            bool isFAIMS,
            bool hasYlimit)
        {
            string[] properties = new string[] { "Protein", "Peptide", "PSM", "MSMS", "IDRatio", "XreaMean" };
            string[] yTexts = new string[] { "# Protein Groups", "# Peptide Groups", "# PSMs", "# MS/MS", "ID Ratio (PSM / MSMS)", "Xrea" };

            for (int i = 0; i < properties.Length; i++)
            {
                //Create and plot all graphics
                CallBlockScatterPlots(mainGrid,
                    machine,
                    properties[i],
                    objectName,
                    yTexts[i],
                    isFAIMS,
                    hasYlimit);
            }

            CallBlockBoxPlot(mainGrid,
                machine,
                "PPM",
                isFAIMS,
                hasYlimit);

        }

        internal static void CallBlockScatterPlots(Grid mainGrid,
            string machine,
            string _property,
            string objectName,
            string yText,
            bool isFAIMS,
            bool hasYlimit)
        {
            string faims = isFAIMS ? "FAIMS" : "NoFAIMS";
            string _key = _property + "#" + faims + "#" + machine;
            double minimumYAxis = 0;
            double maximumYAxis = 100;

            ((List<DataPoint> pointsOT,
            (List<DataPoint> ransac, List<DataPoint> ransacMinusSigma, List<DataPoint> ransacPlusSigma) ransacPointsOT,
            List<DataPoint> pointsIT,
            (List<DataPoint> ransac, List<DataPoint> ransacMinusSigma, List<DataPoint> ransacPlusSigma) ransacPointsIT) points,
            double avgOT, double avgIT) data;

            bool isRound = !(_property == "IDRatio" || _property == "XreaMean");

            if (Dict_Evaluation_points.TryGetValue(_key, out data))
            {
                if (hasYlimit == true)
                {
                    if (_property == "IDRatio")
                    {
                        minimumYAxis = 0;
                        maximumYAxis = 100;
                    }
                    else if (_property == "XreaMean")
                    {
                        minimumYAxis = -0.5;
                        maximumYAxis = 1.2;
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

                PlotEachChart(
               (Grid)RetrieveElement("PlotMachineEvaluation", _property, faims, machine, true, mainGrid, 0),
               (StackPanel)RetrieveElement("LegendMachineEvaluation", _property, faims, machine, true, mainGrid, 1),
               (Grid)RetrieveElement("PlotMachineEvaluation", _property, faims, machine, false, mainGrid, 0),
               (StackPanel)RetrieveElement("LegendMachineEvaluation", _property, faims, machine, false, mainGrid, 1),
               objectName + _property,
               yText,
               data.points.pointsOT,
               data.points.pointsIT,
               data.points.ransacPointsOT,
               data.points.ransacPointsIT,
               hasYlimit,
               minimumYAxis,
               maximumYAxis);

                ShowYAxisLabelPlot(_property, faims, machine, true, mainGrid);
                SetYAxisLabelPlot(_property, faims, machine, true, data.avgOT, mainGrid, isRound);
                ShowYAxisLabelPlot(_property, faims, machine, false, mainGrid);
                SetYAxisLabelPlot(_property, faims, machine, false, data.avgIT, mainGrid, isRound);
            }
        }

        internal static void CallBlockBoxPlot(Grid mainGrid,
            string machine,
            string _property,
            bool isFAIMS,
            bool hasYlimit)
        {
            string faims = isFAIMS ? "FAIMS" : "NoFAIMS";
            string _key = _property + "#" + faims + "#" + machine;

            ((List<DataPoint> pointsOT,
            (List<DataPoint> ransac, List<DataPoint> ransacMinusSigma, List<DataPoint> ransacPlusSigma) ransacPointsOT,
            List<DataPoint> pointsIT,
            (List<DataPoint> ransac, List<DataPoint> ransacMinusSigma, List<DataPoint> ransacPlusSigma) ransacPointsIT) points,
            double avgOT, double avgIT) data;

            if (Dict_Evaluation_points.TryGetValue(_key, out data))
            {
                var plotModels = CreateBoxPlots(
                     data.points.pointsOT,
                     data.points.pointsIT,
                     isFAIMS,
                     hasYlimit);

                PlotBoxPlots(mainGrid,
                    machine,
                    (OxyPlot.Wpf.PlotView)RetrieveElement("PlotMachineEvaluation", _property, faims, machine, true, mainGrid, 3),
                    plotModels.plotModelOT,
                    (OxyPlot.Wpf.PlotView)RetrieveElement("PlotMachineEvaluation", _property, faims, machine, false, mainGrid, 3),
                    plotModels.plotModelIT,
                    isFAIMS);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="_name"></param>
        /// <param name="_property"></param>
        /// <param name="faims_or_queue_property"></param>
        /// <param name="machine"></param>
        /// <param name="isOT"></param>
        /// <param name="mainGrid"></param>
        /// <param name="element">0: Grid_Plot; 1:StackPanel; 2:TextBlock; 3: Oxyplot; 4: GridPlot (parent); 5: ScrollViewer; 6: DatePicker; 8: Datagrid</param>
        /// <returns></returns>
        public static UIElement RetrieveElement(string _name,
            string _property,
            string faims_or_queue_property,
            string machine,
            bool isOT,
            Grid mainGrid,
            byte element)
        {
            if (mainGrid == null) return null;

            foreach (TabItem tabItem in ((TabControl)mainGrid.Children[0]).Items)
            {
                if (!tabItem.Header.ToString().Equals(machine)) continue;
                Grid grid = (Grid)tabItem.Content;

                if (element == 6)
                {
                    GroupBox gb = (GroupBox)grid.Children[0];
                    Grid grid_gb = (Grid)gb.Content;
                    StackPanel sp_gb = (StackPanel)grid_gb.Children[0];
                    return GetDatePicker(_name, sp_gb);
                }
                else
                {
                    TabControl tb = (TabControl)grid.Children[1];
                    foreach (TabItem ti in tb.Items)
                    {
                        if (!ti.Header.ToString().Replace(" ", "").Equals(faims_or_queue_property)) continue;

                        if (element == 7)
                        {
                            Grid plot_grid = (Grid)ti.Content;
                            if (plot_grid.Name.Equals(_name))
                                return plot_grid;
                        }
                        TabControl tb_msoverview = (TabControl)ti.Content;//ms data overview
                        TabItem ti_faims = (TabItem)tb_msoverview.Items[0];
                        ScrollViewer sv = (ScrollViewer)ti_faims.Content;
                        if (element == 5)
                        {
                            if (sv.Name.Equals(_name))
                                return sv;
                        }
                        if (element == 8 || element == 9)
                        {
                            TabItem ti_analytical = (TabItem)tb_msoverview.Items[1];
                            ScrollViewer sv_analytical = (ScrollViewer)ti_analytical.Content;
                            Grid _maingrid = (Grid)sv_analytical.Content;

                            int count_analytical_gbs = _maingrid.Children.Count;
                            GroupBox gb_analytical = null;
                            Grid content_analytical = null;
                            if (count_analytical_gbs == 2)//There are OT and IT
                            {
                                if (isOT)
                                    gb_analytical = (GroupBox)_maingrid.Children[0];
                                else
                                    gb_analytical = (GroupBox)_maingrid.Children[1];

                                content_analytical = (Grid)gb_analytical.Content;
                            }
                            else // There is only 1: OT or IT
                            {
                                gb_analytical = (GroupBox)_maingrid.Children[0];
                                TextBlock header_gb = (TextBlock)gb_analytical.Header;
                                content_analytical = (Grid)gb_analytical.Content;
                            }

                            Grid grid_dg_result_evaluation = (Grid)content_analytical.Children[0];
                            GroupBox gb_dg_eval = null;
                            if (element == 8)
                            {
                                gb_dg_eval = (GroupBox)grid_dg_result_evaluation.Children[0];

                                DataGrid dg_eval = (DataGrid)gb_dg_eval.Content;
                                if (dg_eval.Name == _name)
                                    return dg_eval;
                            }
                            else
                            {
                                gb_dg_eval = (GroupBox)grid_dg_result_evaluation.Children[2];
                                Grid grid_reult_eval = (Grid)gb_dg_eval.Content;
                                if (grid_reult_eval.Name == _name)
                                    return grid_reult_eval;
                            }


                        }
                        else
                        {
                            Grid grid_sv = (Grid)sv.Content;
                            foreach (GroupBox groupBox in grid_sv.Children)
                            {
                                System.Windows.Controls.TextBlock header_title = (System.Windows.Controls.TextBlock)groupBox.Header;
                                if (isOT)
                                {
                                    if (!header_title.Text.Equals("OT")) continue;
                                }
                                else
                                {
                                    if (!header_title.Text.Equals("IT")) continue;
                                }
                                Grid grid_gb = (Grid)groupBox.Content;
                                foreach (Grid grid_plot in grid_gb.Children)
                                {
                                    string _grid_name = "Grid" + _property + (isOT ? "OT" : "IT") + faims_or_queue_property + machine;
                                    if (!grid_plot.Name.Equals(_grid_name)) continue;
                                    if (element == 4)
                                        return grid_plot;

                                    _name += _property + (isOT ? "OT" : "IT") + faims_or_queue_property + machine;

                                    if (element == 0)
                                    {
                                        Grid final_grid = ((Grid)grid_plot.Children[1]);
                                        if (final_grid.Name.Equals(_name))
                                            return final_grid;
                                    }
                                    else if (element == 1)
                                    {
                                        StackPanel final_sp = ((StackPanel)grid_plot.Children[2]);
                                        if (final_sp.Name.Equals(_name))
                                            return final_sp;
                                    }
                                    else if (element == 2)
                                    {
                                        TextBlock final_tb = ((TextBlock)grid_plot.Children[0]);
                                        if (final_tb.Name.Equals(_name))
                                            return final_tb;
                                    }
                                    else if (element == 3)
                                    {
                                        OxyPlot.Wpf.PlotView final_grid = ((OxyPlot.Wpf.PlotView)grid_plot.Children[1]);
                                        if (final_grid.Name.Equals(_name))
                                            return final_grid;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return null;
        }

        internal static TextBlock RetrieveTextBlock(string _name, Grid mainGrid)
        {
            if (mainGrid == null) return null;
            foreach (var child in mainGrid.Children)
            {
                if (child is TextBlock textBlock)
                {
                    if (textBlock.Name.Equals(_name))
                    {
                        return textBlock;
                    }
                }
            }
            return null;
        }
        internal static void ShowYAxisLabelPlot(string _property, string faims, string machine, bool isOT, Grid mainGrid)
        {
            TextBlock yAxis = (TextBlock)RetrieveElement("Avg", _property, faims, machine, isOT, mainGrid, 2);
            if (yAxis != null)
                yAxis.Visibility = Visibility.Visible;
        }

        internal static void SetYAxisLabelPlot(string _property, string faims, string machine, bool isOT, double avg, Grid mainGrid, bool isRound = true)
        {
            TextBlock yAxis = (TextBlock)RetrieveElement("Avg", _property, faims, machine, isOT, mainGrid, 2);
            if (yAxis != null)
                yAxis.Text = "Average: " + (isRound ? Math.Round(avg, 0) : avg.ToString("N2"));
        }

        internal static void PlotEachChart(
            Grid gridOT,
            StackPanel legendOT,
            Grid gridIT,
            StackPanel legendIT,
            string property,
            string yText,
            List<DataPoint> pointsOT,
            List<DataPoint> pointsIT,
            (List<DataPoint> ransac, List<DataPoint> ransacMinusSigma, List<DataPoint> ransacPlusSigma) ransacPointsOT,
            (List<DataPoint> ransac, List<DataPoint> ransacMinusSigma, List<DataPoint> ransacPlusSigma) ransacPointsIT,
            bool hasYlimit = false,
            double minimumYAxis = 0,
            double maximumYAxis = 100
            )
        {
            if (gridOT != null)
            {
                var chartProteinOT = CreateScatterChart("chart_" + property + "OT", pointsOT, ransacPointsOT.ransac, ransacPointsOT.ransacMinusSigma, ransacPointsOT.ransacPlusSigma, yText, hasYlimit, minimumYAxis, maximumYAxis);
                PlotChart(gridOT, chartProteinOT);
                StackPanel created_legend = CreateLegend(chartProteinOT, "panel_" + property + "OT");
                PlotLegend(created_legend, legendOT);
            }

            if (gridIT != null)
            {
                var chartProteinIT = CreateScatterChart("chart_" + property + "IT", pointsIT, ransacPointsIT.ransac, ransacPointsIT.ransacMinusSigma, ransacPointsIT.ransacPlusSigma, yText, hasYlimit, minimumYAxis, maximumYAxis);
                PlotChart(gridIT, chartProteinIT);
                StackPanel created_legend = CreateLegend(chartProteinIT, "panel_" + property + "IT");
                PlotLegend(created_legend, legendIT);
            }
        }

        internal static void PlotBoxPlots(Grid mainGrid,
            string machine,
            PlotView plotViewOT,
            PlotModel plotModelOT,
            PlotView plotViewIT,
            PlotModel plotModelIT,
            bool isFAIMS)
        {
            string faims = isFAIMS ? "FAIMS" : "NoFAIMS";
            #region OT
            if (plotViewOT != null)
            {
                TextBlock yAxis = (TextBlock)RetrieveElement("ylabel", "PPM", faims, machine, true, mainGrid, 2);
                if (yAxis != null)
                    yAxis.Visibility = Visibility.Visible;

                plotViewOT.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(delegate () { plotViewOT.Model = plotModelOT; }));
                plotModelOT.InvalidatePlot(true);
            }
            #endregion

            #region IT
            if (plotViewIT != null)
            {
                TextBlock yAxis = (TextBlock)RetrieveElement("ylabel", "PPM", faims, machine, false, mainGrid, 2);
                if (yAxis != null)
                    yAxis.Visibility = Visibility.Visible;

                plotViewIT.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(delegate () { plotViewIT.Model = plotModelIT; }));
                plotModelIT.InvalidatePlot(true);
            }
            #endregion
        }

        internal static void PlotChart(Grid dataGrid, System.Windows.Controls.DataVisualization.Charting.Chart chart)
        {
            dataGrid.Children.Clear();
            dataGrid.Children.Add(chart);
        }

        internal static void PlotLegend(StackPanel createdPanel, StackPanel stackPanel)
        {
            stackPanel.Children.Clear();
            List<UIElement> created_children = new();

            foreach (var child in createdPanel.Children)
                created_children.Add((UIElement)child);

            foreach (var child in created_children)
            {
                createdPanel.Children.Remove(child);
                stackPanel.Children.Add(child);
            }
        }

        internal static TabItem CreatePlotsIntoEvaluationTabItems(string machine, bool hasFaims, bool hasOT, bool hasIT)
        {
            // Create the "FAIMS" TabItem
            var faimsTab = new TabItem() { Header = hasFaims ? "FAIMS" : "No FAIMS" };

            var tabControl = new TabControl();
            var msoverviewTab = new TabItem() { Header = "MS Data Overview" };
            tabControl.Items.Add(msoverviewTab);
            faimsTab.Content = tabControl;

            // Create a ScrollViewer
            var scrollViewer = new ScrollViewer
            {
                Name = hasFaims ? "sv_faims" : "sv_no_faims",
                Margin = new System.Windows.Thickness(0),
                BorderThickness = new System.Windows.Thickness(0),
                HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden,
                VerticalScrollBarVisibility = ScrollBarVisibility.Visible
            };
            msoverviewTab.Content = scrollViewer;

            // Create the main Grid
            var mainGrid = new Grid();
            mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            scrollViewer.Content = mainGrid;
            string[] properties = new string[] { "Protein", "Peptide", "PSM", "MSMS", "IDRatio", "XreaMean" };
            string _hasFaims = hasFaims ? "FAIMS" : "NoFAIMS";

            if (hasOT)
            {
                #region OT
                // Create the GroupBox and its content (similar structure for other GroupBoxes)
                var groupBoxOT = CreateGroupBox("OT");
                groupBoxOT.Margin = new System.Windows.Thickness(0);
                Grid.SetColumn(groupBoxOT, 0);
                mainGrid.Children.Add(groupBoxOT);

                // Create the content for the GroupBox
                var contentOT = CreateContent(properties.Length + 1);
                groupBoxOT.Content = contentOT;

                for (int i = 0; i < properties.Length; i++)
                {
                    string _property = properties[i];
                    var grid_plot = CreateGridPlot(_property, "OT", _hasFaims, machine);
                    Grid.SetRow(grid_plot, i);
                    contentOT.Children.Add(grid_plot);
                }

                var oxyplot_grid = CreateOxyPlotGrid("PPM", "OT", _hasFaims, machine);
                Grid.SetRow(oxyplot_grid, properties.Length);
                contentOT.Children.Add(oxyplot_grid);
                #endregion
            }
            if (hasIT)
            {
                #region IT
                var groupBoxIT = CreateGroupBox("IT");
                groupBoxIT.Margin = new System.Windows.Thickness(5, 0, 0, 0);
                Grid.SetColumn(groupBoxIT, 1);
                mainGrid.Children.Add(groupBoxIT);

                // Create the content for the GroupBox
                var contentIT = CreateContent(properties.Length + 1);
                groupBoxIT.Content = contentIT;
                for (int i = 0; i < properties.Length; i++)
                {
                    string _property = properties[i];
                    var grid_plot = CreateGridPlot(_property, "IT", _hasFaims, machine);
                    Grid.SetRow(grid_plot, i);
                    contentIT.Children.Add(grid_plot);
                }

                var oxyplot_grid = CreateOxyPlotGrid("PPM", "IT", _hasFaims, machine);
                Grid.SetRow(oxyplot_grid, properties.Length);
                contentIT.Children.Add(oxyplot_grid);
                #endregion
            }

            tabControl.Items.Add(Create_Analytical_Metrics_tab(machine, _hasFaims, hasFaims, hasOT, hasIT));

            return faimsTab;
        }

        private static TabItem Create_Analytical_Metrics_tab(string machine, string _hasFaims, bool hasFaims, bool hasOT, bool hasIT)
        {
            var analyticalTab = new TabItem() { Header = "Analytical Metrics" };
            var scrollViewer = new ScrollViewer
            {
                Name = hasFaims ? "sv_analytical_faims" : "sv_analytical_no_faims",
                Margin = new System.Windows.Thickness(0),
                BorderThickness = new System.Windows.Thickness(0),
                HorizontalScrollBarVisibility = ScrollBarVisibility.Visible,
                VerticalScrollBarVisibility = ScrollBarVisibility.Visible
            };
            analyticalTab.Content = scrollViewer;

            // Create the main Grid
            var mainGrid = new Grid();
            mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            scrollViewer.Content = mainGrid;

            if (hasOT)
            {
                #region OT
                // Create the GroupBox and its content (similar structure for other GroupBoxes)
                var groupBoxOT = CreateGroupBox("OT");
                groupBoxOT.Margin = new System.Windows.Thickness(0);
                Grid.SetColumn(groupBoxOT, 0);
                mainGrid.Children.Add(groupBoxOT);

                // Create the content for the GroupBox
                var contentOT = CreateContent(2);
                groupBoxOT.Content = contentOT;

                var grid_dg_and_result_evaluation_OT = new Grid
                {
                    Name = $"Grid_Analytical_Metrics_Evaluation_OT_{machine}_{_hasFaims}",
                    MinHeight = 270
                };
                grid_dg_and_result_evaluation_OT.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                grid_dg_and_result_evaluation_OT.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                grid_dg_and_result_evaluation_OT.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                Grid.SetRow(grid_dg_and_result_evaluation_OT, 0);

                var groupBoxDatagridOT = CreateGroupBox("");
                groupBoxDatagridOT.Content = CreateDataGridEvaluation(machine, "OT", _hasFaims);

                Grid.SetRow(groupBoxDatagridOT, 0);
                grid_dg_and_result_evaluation_OT.Children.Add(groupBoxDatagridOT);

                //buttton
                System.Windows.Controls.Button buttonUpdateResultsOT = new System.Windows.Controls.Button();
                buttonUpdateResultsOT.Name = "ButtonUpdateAnalyticMetrics_OT_" + machine + "_" + _hasFaims;
                buttonUpdateResultsOT.Click += ButtonUpdateAnalyticMetrics_Click;
                buttonUpdateResultsOT.Padding = new System.Windows.Thickness(10, 1, 10, 1);
                buttonUpdateResultsOT.Margin = new System.Windows.Thickness(2, 8, 0, 0);
                buttonUpdateResultsOT.Height = 20;
                buttonUpdateResultsOT.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
                buttonUpdateResultsOT.Cursor = Cursors.Hand;

                StackPanel stackPanelButtonOT = new StackPanel();
                stackPanelButtonOT.Orientation = System.Windows.Controls.Orientation.Horizontal;

                DockPanel dockPanelOT = new DockPanel();
                Image image = new Image();
                image.Source = new BitmapImage(new Uri("/icons/update.png", UriKind.Relative));
                dockPanelOT.Children.Add(image);

                TextBlock buttonTextBlockOT = new TextBlock();
                buttonTextBlockOT.Margin = new System.Windows.Thickness(5, 0, 0, 0);
                buttonTextBlockOT.Width = 80;
                buttonTextBlockOT.Text = "Update results";

                stackPanelButtonOT.Children.Add(dockPanelOT);
                stackPanelButtonOT.Children.Add(buttonTextBlockOT);

                buttonUpdateResultsOT.Content = stackPanelButtonOT;

                Grid.SetRow(buttonUpdateResultsOT, 1);
                grid_dg_and_result_evaluation_OT.Children.Add(buttonUpdateResultsOT);

                var groupBoxResultOT = CreateGroupBox("");

                var grid_result_evaluation_OT = new Grid
                {
                    Name = $"Grid_Analytical_Metrics_Evaluation_Result_OT_{machine}_{_hasFaims}",
                    MinHeight = 270
                };
                grid_result_evaluation_OT.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                grid_result_evaluation_OT.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                grid_result_evaluation_OT.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                grid_result_evaluation_OT.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                groupBoxResultOT.Content = grid_result_evaluation_OT;

                Grid.SetRow(groupBoxResultOT, 2);
                grid_dg_and_result_evaluation_OT.Children.Add(groupBoxResultOT);


                contentOT.Children.Add(grid_dg_and_result_evaluation_OT);


                #endregion
            }
            if (hasIT)
            {
                #region IT
                var groupBoxIT = CreateGroupBox("IT");
                groupBoxIT.Margin = new System.Windows.Thickness(5, 0, 0, 0);
                Grid.SetColumn(groupBoxIT, 1);
                mainGrid.Children.Add(groupBoxIT);

                // Create the content for the GroupBox
                var contentIT = CreateContent(2);
                groupBoxIT.Content = contentIT;

                var grid_dg_and_result_evaluation_IT = new Grid
                {
                    Name = $"Grid_Analytical_Metrics_Evaluation_IT_{machine}_{_hasFaims}",
                    MinHeight = 270
                };
                grid_dg_and_result_evaluation_IT.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                grid_dg_and_result_evaluation_IT.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                grid_dg_and_result_evaluation_IT.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                Grid.SetRow(grid_dg_and_result_evaluation_IT, 0);

                var groupBoxDatagridIT = CreateGroupBox("");
                groupBoxDatagridIT.Content = CreateDataGridEvaluation(machine, "IT", _hasFaims);

                Grid.SetRow(groupBoxDatagridIT, 0);
                grid_dg_and_result_evaluation_IT.Children.Add(groupBoxDatagridIT);

                //buttton
                System.Windows.Controls.Button buttonUpdateResultsIT = new System.Windows.Controls.Button();
                buttonUpdateResultsIT.Name = "ButtonUpdateAnalyticMetrics_IT_" + machine + "_" + _hasFaims;
                buttonUpdateResultsIT.Click += ButtonUpdateAnalyticMetrics_Click;
                buttonUpdateResultsIT.Padding = new System.Windows.Thickness(10, 1, 10, 1);
                buttonUpdateResultsIT.Margin = new System.Windows.Thickness(2, 8, 0, 0);
                buttonUpdateResultsIT.Height = 20;
                buttonUpdateResultsIT.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
                buttonUpdateResultsIT.Cursor = Cursors.Hand;

                StackPanel stackPanelButtonIT = new StackPanel();
                stackPanelButtonIT.Orientation = System.Windows.Controls.Orientation.Horizontal;

                DockPanel dockPanelIT = new DockPanel();
                Image image = new Image();
                image.Source = new BitmapImage(new Uri("/icons/update.png", UriKind.Relative));
                dockPanelIT.Children.Add(image);

                TextBlock buttonTextBlockIT = new TextBlock();
                buttonTextBlockIT.Margin = new System.Windows.Thickness(5, 0, 0, 0);
                buttonTextBlockIT.Width = 80;
                buttonTextBlockIT.Text = "Update results";

                stackPanelButtonIT.Children.Add(dockPanelIT);
                stackPanelButtonIT.Children.Add(buttonTextBlockIT);

                buttonUpdateResultsIT.Content = stackPanelButtonIT;

                Grid.SetRow(buttonUpdateResultsIT, 1);
                grid_dg_and_result_evaluation_IT.Children.Add(buttonUpdateResultsIT);

                var groupBoxResultIT = CreateGroupBox("");

                var grid_result_evaluation_IT = new Grid
                {
                    Name = $"Grid_Analytical_Metrics_Evaluation_Result_IT_{machine}_{_hasFaims}",
                    MinHeight = 270
                };
                grid_result_evaluation_IT.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                grid_result_evaluation_IT.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                grid_result_evaluation_IT.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                grid_result_evaluation_IT.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                groupBoxResultIT.Content = grid_result_evaluation_IT;

                Grid.SetRow(groupBoxResultIT, 2);
                grid_dg_and_result_evaluation_IT.Children.Add(groupBoxResultIT);


                contentIT.Children.Add(grid_dg_and_result_evaluation_IT);
                #endregion
            }
            return analyticalTab;
        }

        internal static async void ButtonUpdateAnalyticMetrics_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Controls.Button buttonConfirm = (System.Windows.Controls.Button)(sender);
            string[] cols = Regex.Split(buttonConfirm.Name.Replace("ButtonUpdateAnalyticMetrics_", ""), "_");
            bool isOT = cols[0] == "OT";
            string machine = cols[1];
            bool hasFAIMS = cols[2] == "FAIMS";

            UIElement button = buttonConfirm;
            DependencyObject button_parent = VisualTreeHelper.GetParent(button);

            if (button_parent is Grid grid)
            {
                DependencyObject main_grid = VisualTreeHelper.GetParent(VisualTreeHelper.GetParent(VisualTreeHelper.GetParent(VisualTreeHelper.GetParent(VisualTreeHelper.GetParent(VisualTreeHelper.GetParent(VisualTreeHelper.GetParent(VisualTreeHelper.GetParent(VisualTreeHelper.GetParent(VisualTreeHelper.GetParent(VisualTreeHelper.GetParent(VisualTreeHelper.GetParent(VisualTreeHelper.GetParent(VisualTreeHelper.GetParent(VisualTreeHelper.GetParent(VisualTreeHelper.GetParent(VisualTreeHelper.GetParent(VisualTreeHelper.GetParent(VisualTreeHelper.GetParent(VisualTreeHelper.GetParent(VisualTreeHelper.GetParent(VisualTreeHelper.GetParent(grid))))))))))))))))))))));
                if (main_grid is Grid mainGrid)
                {
                    DataGrid dg_faims_no_faims = (DataGrid)RetrieveElement($"DataGridAnalyticalMetrics_Evaluation_{machine}_{cols[0]}_{cols[2]}", "", cols[2], machine, isOT, mainGrid, 8);
                    if (dg_faims_no_faims != null)
                        dg_faims_no_faims.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(delegate ()
                        {
                            var wait_screen = Util.Util.CallWaitWindow("Please wait...", "Updating results...");
                            mainGrid.Children.Add(wait_screen);

                            ComputeAnalyticalMetricsOTorIT(machine, mainGrid, dg_faims_no_faims, isOT, hasFAIMS);

                            mainGrid.Children.Remove(wait_screen);
                        }));
                }
            }
        }

        private static DataGridTextColumn CreateTextColumn(string header, double maxWidth, double minWidth, string binding, byte hasStringFormat = 0, string stringFormat = "", bool has_max_width = true)
        {
            DataGridTextColumn datagrid_textColumn = new DataGridTextColumn
            {
                Header = header,
                MinWidth = minWidth,
                IsReadOnly = true,
                CanUserSort = false
            };
            datagrid_textColumn.Width = minWidth;

            if (has_max_width == true)
                datagrid_textColumn.MaxWidth = maxWidth;

            if (hasStringFormat == 0)
            {
                Binding _binding = new Binding(binding);
                _binding.StringFormat = stringFormat;
                datagrid_textColumn.Binding = _binding;
            }
            else if (hasStringFormat == 1)
            {
                Binding _binding = new Binding(binding);
                _binding.Converter = new FileNameConverter();
                datagrid_textColumn.Binding = _binding;
            }
            else
                datagrid_textColumn.Binding = new System.Windows.Data.Binding(binding);

            // Set the element style for the column
            datagrid_textColumn.ElementStyle = new System.Windows.Style(typeof(TextBlock));
            datagrid_textColumn.ElementStyle.Setters.Add(new Setter(System.Windows.Controls.Control.HorizontalAlignmentProperty, System.Windows.HorizontalAlignment.Center));

            return datagrid_textColumn;
        }

        private static DataGrid CreateDataGridEvaluation(string machine, string ot_it, string _hasFaims, bool hasInclude = true, string main_name = "DataGridAnalyticalMetrics_Evaluation")
        {
            // Create the DataGrid
            DataGrid dataGrid = new DataGrid
            {
                Name = $"{main_name}_{machine}_{ot_it}_{_hasFaims}",
                IsReadOnly = false,
                CanUserAddRows = false,
                AutoGenerateColumns = false,
                AlternatingRowBackground = System.Windows.Media.Brushes.WhiteSmoke,
                VerticalScrollBarVisibility = ScrollBarVisibility.Visible,
                SelectionMode = DataGridSelectionMode.Single,
                SelectionUnit = DataGridSelectionUnit.FullRow,
                MinHeight = 210
            };
            // Create a style for column headers
            System.Windows.Style columnHeaderStyle = new System.Windows.Style(typeof(DataGridColumnHeader));
            columnHeaderStyle.Setters.Add(new Setter(System.Windows.Controls.Control.HorizontalContentAlignmentProperty, System.Windows.HorizontalAlignment.Center));

            // Set the DataGrid's ColumnHeaderStyle
            dataGrid.ColumnHeaderStyle = columnHeaderStyle;

            DataGridTextColumn regitrationDateColumn = CreateTextColumn("Created", 130, 130, $"{ot_it}.RegistrationDateStr");
            DataGridTextColumn proteinGroupsColumn = CreateTextColumn("Protein Groups", 100, 100, $"{ot_it}.ProteinGroup");
            DataGridTextColumn peptideGroupsColumn = CreateTextColumn("Peptide Groups", 100, 100, $"{ot_it}.PeptideGroup");
            DataGridTextColumn psmsColumn = CreateTextColumn("PSMs", 90, 90, $"{ot_it}.PSM");
            DataGridTextColumn msmsColumn = CreateTextColumn("MS/MS", 90, 90, $"{ot_it}.MSMS");
            DataGridTextColumn iDRatioColumn = CreateTextColumn("ID Ratio (%)", 100, 100, $"{ot_it}.IDRatio", 0, "N2");
            if (hasInclude)
            {
                DataGridCheckBoxColumn includeColumn = CreateCheckBoxColumn("Include", $"{ot_it}.IncludeAnalyticalMetrics");
                dataGrid.Columns.Add(includeColumn);
            }
            // Add the column to the DataGrid
            dataGrid.Columns.Add(regitrationDateColumn);
            dataGrid.Columns.Add(proteinGroupsColumn);
            dataGrid.Columns.Add(peptideGroupsColumn);
            dataGrid.Columns.Add(psmsColumn);
            dataGrid.Columns.Add(msmsColumn);
            dataGrid.Columns.Add(iDRatioColumn);

            return dataGrid;
        }

        private static DataGridCheckBoxColumn CreateCheckBoxColumn(string header, string binding)
        {
            DataGridCheckBoxColumn datagrid_checkboxColumn = new DataGridCheckBoxColumn
            {
                Header = header,
                Binding = new System.Windows.Data.Binding(binding) { Mode = BindingMode.TwoWay, UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged }
            };

            System.Windows.Style checkBoxStyle = new System.Windows.Style(typeof(DataGridCell));
            checkBoxStyle.BasedOn = (System.Windows.Style)Application.Current.Resources["DataGridCell"];
            checkBoxStyle.Setters.Add(new EventSetter(CheckBox.MouseLeaveEvent, new MouseEventHandler(Include_Run_Evaluation_MouseLeave)));
            checkBoxStyle.Setters.Add(new EventSetter(CheckBox.MouseEnterEvent, new MouseEventHandler(Include_Run_Evaluation_MouseEnter)));
            checkBoxStyle.Setters.Add(new EventSetter(CheckBox.CheckedEvent, new RoutedEventHandler(Include_Run_Evaluation_OnChecked)));
            checkBoxStyle.Setters.Add(new EventSetter(CheckBox.UncheckedEvent, new RoutedEventHandler(Include_Run_Evaluation_OnChecked)));
            datagrid_checkboxColumn.CellStyle = checkBoxStyle;

            return datagrid_checkboxColumn;
        }
        private static DataGrid GetDataGridFromCell(System.Windows.Controls.DataGridCell datagrid_cell)
        {
            DependencyObject dep = datagrid_cell;
            while (dep != null && !(dep is DataGrid))
            {
                dep = VisualTreeHelper.GetParent(dep);
            }
            return dep as DataGrid;
        }
        private static void Include_Run_Evaluation_MouseLeave(object sender, MouseEventArgs e)
        {
            ((CheckBox)((DataGridCell)e.OriginalSource).Content).Tag = "";
        }
        private static void Include_Run_Evaluation_MouseEnter(object sender, MouseEventArgs e)
        {
            ((CheckBox)((DataGridCell)e.OriginalSource).Content).Tag = "clicked";
        }
        private static void Include_Run_Evaluation_OnChecked(object sender, RoutedEventArgs e)
        {
            if (((CheckBox)e.OriginalSource).Tag == null ||
                String.IsNullOrEmpty(((CheckBox)e.OriginalSource).Tag.ToString())) return;

            System.Windows.Controls.DataGridCell datagrid_cell = (System.Windows.Controls.DataGridCell)(sender);
            if (datagrid_cell.IsSelected == true)
            {
                DataGrid dg = GetDataGridFromCell(datagrid_cell);
                if (dg == null) return;

                string[] _data = Regex.Split(dg.Name.Replace("DataGridAnalyticalMetrics_Evaluation_", ""), "_");

                // Get the CheckBox
                CheckBox checkBox = (CheckBox)e.OriginalSource;

                // Find the DataGridRow that contains this checkbox
                DataGridRow row = FindParent<DataGridRow>(checkBox);

                if (row != null)
                {
                    // Get the bound data item (which is a Run object)
                    if (row.DataContext is Model.Run runItem)
                    {
                        // Update the Include property based on the checkbox state
                        if (_data[1] == "OT")
                            runItem.OT.IncludeAnalyticalMetrics = checkBox.IsChecked ?? false;
                        else
                            runItem.IT.IncludeAnalyticalMetrics = checkBox.IsChecked ?? false;
                    }
                }
            }
        }
        private static T FindParent<T>(DependencyObject child) where T : DependencyObject
        {
            DependencyObject parent = VisualTreeHelper.GetParent(child);

            while (parent != null && parent is not T)
            {
                parent = VisualTreeHelper.GetParent(parent);
            }

            return parent as T;
        }

        public static TabControl Create_Faims_NoFaims(string machine, bool hasFaims, bool hasOT, bool hasIT)
        {
            // Create the TabControl
            var tabControl = new TabControl();
            if (hasFaims)
            {
                var faimsTab = CreatePlotsIntoEvaluationTabItems(machine, true, hasOT, hasIT);
                tabControl.Items.Add(faimsTab);
            }
            var noFaimsTab = CreatePlotsIntoEvaluationTabItems(machine, false, hasOT, hasIT);
            tabControl.Items.Add(noFaimsTab);
            return tabControl;
        }
        // Helper method to create a GroupBox
        internal static System.Windows.Controls.GroupBox CreateGroupBox(string headerText)
        {
            var groupBox = new System.Windows.Controls.GroupBox
            {
                Header = new TextBlock
                {
                    FontWeight = System.Windows.FontWeights.Bold,
                    Text = headerText
                }
            };
            return groupBox;
        }

        // Helper method to create content for a GroupBox
        internal static Grid CreateContent(int total_rows)
        {
            var contentGrid = new Grid();
            for (int i = 0; i < total_rows; i++)
            {
                contentGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            }
            return contentGrid;
        }

        // Helper method to create a TextBlock
        internal static TextBlock CreateTextBlock(string name, string text, string _property, string ot_it, string faims, string machine, bool isOxyPlot = false)
        {
            TextBlock textBlock = textBlock = new TextBlock
            {
                Name = name + _property + ot_it + faims + machine,
                Text = text,
                Visibility = Visibility.Hidden
            };
            if (isOxyPlot == false)
            {
                textBlock.TextAlignment = TextAlignment.Right;
                textBlock.Margin = new System.Windows.Thickness(0, 10, 11, 0);
            }
            else
            {
                RotateTransform rt = new();
                rt.Angle = -90;
                textBlock.RenderTransform = rt;
            }
            return textBlock;
        }

        // Helper method to create a Grid
        internal static Grid CreateGridPlot(string _property, string ot_it, string faims, string machine)
        {
            var grid = new Grid
            {
                Name = "Grid" + _property + ot_it + faims + machine,
                MinHeight = 270
            };
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var averageTextBox = CreateTextBlock("Avg", "Average: ???", _property, ot_it, faims, machine);
            Grid.SetRow(averageTextBox, 0);
            grid.Children.Add(averageTextBox);

            var grid_plot_machine = new Grid
            {
                Name = "PlotMachineEvaluation" + _property + ot_it + faims + machine,
                Width = 700,
                Margin = new System.Windows.Thickness(0, -10, 0, 0)
            };
            Grid.SetRow(grid_plot_machine, 0);
            grid.Children.Add(grid_plot_machine);

            var stackPanel = new StackPanel
            {
                Name = "LegendMachineEvaluation" + _property + ot_it + faims + machine,
                Margin = new System.Windows.Thickness(0, -20, 0, 0),
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                Orientation = System.Windows.Controls.Orientation.Horizontal
            };
            Grid.SetRow(stackPanel, 1);
            grid.Children.Add(stackPanel);

            return grid;
        }

        internal static Grid CreateOxyPlotGrid(string _property, string ot_it, string faims, string machine)
        {
            var grid = new Grid
            {
                Name = "Grid" + _property + ot_it + faims + machine,
                MinHeight = 270
            };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(20) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var yLabelTextBox = CreateTextBlock("ylabel", "Mass error (ppm)", _property, ot_it, faims, machine, true);
            Grid.SetColumn(yLabelTextBox, 0);
            grid.Children.Add(yLabelTextBox);

            var grid_plot_machine = new OxyPlot.Wpf.PlotView
            {
                Name = "PlotMachineEvaluation" + _property + ot_it + faims + machine,
            };
            Grid.SetColumn(grid_plot_machine, 1);
            grid.Children.Add(grid_plot_machine);

            return grid;
        }
        #endregion

        #region Machines Queue
        internal static void CreateAllQueuePlots(string machine, Grid mainGrid)
        {
            (List<DataPoint> measuredPeriod, List<DataPoint> allPeriod, List<DataPoint> measuredUser, List<DataPoint> allUser) data;

            if (Dict_Queue_points.TryGetValue(machine, out data))
            {
                string datagrid_name = "PlotQueuePerPeriod" + machine;
                Grid periodGrid = (Grid)RetrieveElement(datagrid_name, "", "PerPeriod", machine, true, mainGrid, 7);

                KeyValuePair<string, int>[] keyValuePairsMeasured = data.measuredPeriod.Select(dataPoint => new KeyValuePair<string, int>(dataPoint.X, (int)dataPoint.Y)).ToArray();
                KeyValuePair<string, int>[] keyValuePairsAll = data.allPeriod.Select(dataPoint => new KeyValuePair<string, int>(dataPoint.X, (int)dataPoint.Y)).ToArray();

                CreateChartPlot(keyValuePairsMeasured, keyValuePairsAll, periodGrid);

                datagrid_name = "PlotQueuePerUser" + machine;
                Grid userGrid = (Grid)RetrieveElement(datagrid_name, "", "PerUser", machine, true, mainGrid, 7);

                keyValuePairsMeasured = data.measuredUser.Select(dataPoint => new KeyValuePair<string, int>(dataPoint.X, (int)dataPoint.Y)).ToArray();
                keyValuePairsAll = data.allUser.Select(dataPoint => new KeyValuePair<string, int>(dataPoint.X, (int)dataPoint.Y)).ToArray();

                CreateChartPlot(keyValuePairsMeasured, keyValuePairsAll, userGrid);
            }
        }
        internal static void CreateChartPlot(
            KeyValuePair<string, int>[] measuredSamples,
            KeyValuePair<string, int>[] allSamples,
            Grid grid)
        {
            // Create a Chart
            System.Windows.Controls.DataVisualization.Charting.Chart myChart = new System.Windows.Controls.DataVisualization.Charting.Chart();
            myChart.BorderThickness = new System.Windows.Thickness(0);

            // Create a LegendStyle
            System.Windows.Style legendStyle = new System.Windows.Style(typeof(System.Windows.Controls.Control));
            legendStyle.Setters.Add(new Setter(System.Windows.Controls.Control.WidthProperty, 120.0));
            legendStyle.Setters.Add(new Setter(System.Windows.Controls.Control.HeightProperty, 70.0));
            legendStyle.Setters.Add(new Setter(System.Windows.Controls.Control.BorderBrushProperty, Brushes.White));
            myChart.LegendStyle = legendStyle;

            // Create a LinearAxis
            System.Windows.Controls.DataVisualization.Charting.LinearAxis yAxis = new System.Windows.Controls.DataVisualization.Charting.LinearAxis();
            yAxis.Orientation = AxisOrientation.Y;
            yAxis.Title = "# Samples";
            myChart.Axes.Add(yAxis); // Create a PlotAreaStyle
            System.Windows.Style plotAreaStyle = new System.Windows.Style(typeof(Grid));
            plotAreaStyle.Setters.Add(new Setter(Grid.BackgroundProperty, Brushes.White));
            myChart.PlotAreaStyle = plotAreaStyle;
            myChart.Width = grid.Width;
            myChart.Height = grid.Height;

            // Create column series
            CreateColumnSeries(myChart, measuredSamples, allSamples);

            // Add the Chart to your container (e.g., a Grid)
            grid.Children.Clear();
            grid.Children.Add(myChart);
        }
        internal static void CreateColumnSeries(System.Windows.Controls.DataVisualization.Charting.Chart myChart,
            KeyValuePair<string, int>[] measuredSamples,
            KeyValuePair<string, int>[] allSamples)
        {
            ColumnSeries series1 = new ColumnSeries();
            series1.Title = "Measured";
            series1.IndependentValueBinding = new System.Windows.Data.Binding("Key");
            series1.DependentValueBinding = new System.Windows.Data.Binding("Value");
            System.Windows.Style dataPointStyle1 = new System.Windows.Style(typeof(ColumnDataPoint));
            dataPointStyle1.Setters.Add(new Setter(ColumnDataPoint.BackgroundProperty, (Brush)(new BrushConverter().ConvertFrom("#2b8489"))));
            dataPointStyle1.Setters.Add(new Setter(ColumnDataPoint.TemplateProperty, GetColumnDataPointTemplate()));
            series1.DataPointStyle = dataPointStyle1;
            series1.ItemsSource = measuredSamples;

            ColumnSeries series2 = new ColumnSeries();
            series2.Title = "All samples";
            series2.IndependentValueBinding = new System.Windows.Data.Binding("Key");
            series2.DependentValueBinding = new System.Windows.Data.Binding("Value");
            System.Windows.Style dataPointStyle2 = new System.Windows.Style(typeof(ColumnDataPoint));
            dataPointStyle2.Setters.Add(new Setter(ColumnDataPoint.BackgroundProperty, (Brush)(new BrushConverter().ConvertFrom("#FFE67B20"))));
            dataPointStyle2.Setters.Add(new Setter(ColumnDataPoint.TemplateProperty, GetColumnDataPointTemplate()));
            series2.DataPointStyle = dataPointStyle2;
            series2.ItemsSource = allSamples;

            myChart.Series.Add(series1);
            myChart.Series.Add(series2);
        }
        internal static ControlTemplate GetColumnDataPointTemplate()
        {
            ControlTemplate template = new ControlTemplate(typeof(ColumnDataPoint));

            FrameworkElementFactory gridFactory = new FrameworkElementFactory(typeof(Grid));
            FrameworkElementFactory row1Factory = new FrameworkElementFactory(typeof(RowDefinition));
            FrameworkElementFactory row2Factory = new FrameworkElementFactory(typeof(RowDefinition));
            row1Factory.SetValue(RowDefinition.HeightProperty, new GridLength(2));
            row2Factory.SetValue(RowDefinition.HeightProperty, new GridLength(1, GridUnitType.Star));

            FrameworkElementFactory rectFactory = new FrameworkElementFactory(typeof(Rectangle));
            rectFactory.SetValue(Grid.RowProperty, 1);
            rectFactory.SetValue(Rectangle.FillProperty, new TemplateBindingExtension(ColumnDataPoint.BackgroundProperty));
            rectFactory.SetValue(Rectangle.StrokeProperty, Brushes.Black);

            FrameworkElementFactory grid2Factory = new FrameworkElementFactory(typeof(Grid));
            grid2Factory.SetValue(Grid.RowProperty, 0);
            grid2Factory.SetValue(Grid.BackgroundProperty, new SolidColorBrush(Color.FromArgb(15, 0, 0, 0)));
            grid2Factory.SetValue(Grid.HorizontalAlignmentProperty, System.Windows.HorizontalAlignment.Center);
            grid2Factory.SetValue(Grid.MarginProperty, new System.Windows.Thickness(0, -20, 0, 0));
            grid2Factory.SetValue(Grid.VerticalAlignmentProperty, System.Windows.VerticalAlignment.Center);

            FrameworkElementFactory textBlockFactory = new FrameworkElementFactory(typeof(TextBlock));
            textBlockFactory.SetValue(TextBlock.TextProperty, new TemplateBindingExtension(ColumnDataPoint.FormattedDependentValueProperty));
            textBlockFactory.SetValue(TextBlock.TextAlignmentProperty, TextAlignment.Center);
            textBlockFactory.SetValue(TextBlock.FontWeightProperty, System.Windows.FontWeights.Normal); textBlockFactory.SetValue(TextBlock.WidthProperty, double.NaN);
            textBlockFactory.SetValue(TextBlock.MinWidthProperty, 40.0);
            textBlockFactory.SetValue(TextBlock.MarginProperty, new System.Windows.Thickness(0));

            grid2Factory.AppendChild(textBlockFactory); gridFactory.AppendChild(row1Factory);
            gridFactory.AppendChild(row2Factory);
            gridFactory.AppendChild(rectFactory);
            gridFactory.AppendChild(grid2Factory);
            template.VisualTree = gridFactory;

            return template;
        }
        public static TabControl Create_QueuePlots(string machine)
        {
            // Create the TabControl
            var tabControl = new TabControl();
            var periodTab = CreatePlotsIntoQueueTabItems(machine, true);
            tabControl.Items.Add(periodTab);
            var userTab = CreatePlotsIntoQueueTabItems(machine, false);
            tabControl.Items.Add(userTab);
            return tabControl;
        }
        internal static TabItem CreatePlotsIntoQueueTabItems(string machine, bool isPeriod)
        {
            var queueTab = new TabItem() { Header = isPeriod ? "Per Period" : "Per User" };

            // Create the main Grid
            var mainGrid = new Grid();
            if (isPeriod)
                mainGrid.Name = $"PlotQueuePerPeriod{machine}";
            else
                mainGrid.Name = $"PlotQueuePerUser{machine}";
            mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            queueTab.Content = mainGrid;

            return queueTab;
        }

        #endregion

        #endregion

        #endregion

    }

    public class DataPoint
    {
        public string X { get; set; }
        public double Y { get; set; }
        public bool Highlight { get; set; } // Add a property to control highlighting
        public double MinValue { get; set; }
        public double MaxValue { get; set; }
        public double MedianValue { get; set; }
        public string ToolTip { get; set; }

        public DataPoint(string x, double y, double minValue, double maxValue, double medianValue)
        {
            X = x;
            Y = y;
            MinValue = minValue;
            MaxValue = maxValue;
            MedianValue = medianValue;
            this.ToolTip = "X: " + x + "\nY: " + y;
        }

        public DataPoint(string x, double y, double minValue, double maxValue, double medianValue, string toolTip)
        {
            X = x;
            Y = y;
            MinValue = minValue;
            MaxValue = maxValue;
            MedianValue = medianValue;
            ToolTip = toolTip;
        }

        public DataPoint(string x, double y, bool highlight)
        {
            X = x;
            Y = y;
            Highlight = highlight;
        }

        public DataPoint(string x, double y, string toolTip)
        {
            X = x;
            Y = y;
            this.ToolTip = toolTip;
        }

        public DataPoint(string x, double y)
        {
            X = x;
            Y = y;
            ToolTip = "X: " + x + "\nY: " + y;
        }

        public DataPoint()
        {
        }
    }

}
