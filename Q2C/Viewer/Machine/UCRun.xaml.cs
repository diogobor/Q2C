using Google.Apis.Logging;
using Q2C.Control;
using Q2C.Control.Database;
using Q2C.Control.FileManagement;
using Q2C.Control.QualityControl;
using Q2C.Model;
using System;
using System.Collections.Generic;
using System.Formats.Tar;
using System.IO;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ThermoFisher.CommonCore.Data;
using ThermoFisher.CommonCore.Data.Business;
using static System.Net.Mime.MediaTypeNames;
using Run = Q2C.Model.Run;

namespace Q2C.Viewer.Machine
{
    /// <summary>
    /// Interaction logic for UCRun.xaml
    /// </summary>
    public partial class UCRun : System.Windows.Controls.UserControl
    {
        private Window _window { get; set; }
        private Run _run { get; set; }
        private string _machine { get; set; }

        private string[] _selected_files { get; set; }


        public UCRun()
        {
            InitializeComponent();
        }

        public void Load(Window window, Run run, string machine)
        {
            _window = window;
            _run = run;
            _machine = machine;

            if (_run != null)
                LoadRun();
            else
                LoadColumnLotNumber();
        }

        private void LoadColumnLotNumber()
        {
            (List<MachineQueue> queue, List<Model.Run> evaluation, List<MachineLog> log) prop;
            Management.Machines_Properties.TryGetValue(_machine, out prop);
            if (prop == (null, null, null))
                return;

            List<MachineLog> current_log = prop.log.Where(a => a.InfoStatus != Management.InfoStatus.Deleted).ToList();
            if (current_log == null || current_log.Count == 0) return;

            current_log.Sort((a, b) => b.RegistrationDate.CompareTo(a.RegistrationDate));
            MachineLog? last_log = current_log.Where(a => !String.IsNullOrEmpty(a.ColumnLotNumber)).FirstOrDefault();
            if (last_log == null) return;

            TextColumnLotNumber.Text = last_log.ColumnLotNumber;
        }

        private void LoadRun()
        {
            TextOperator.Text = _run.Operator; TextOperator.IsReadOnly = true;
            TextColumnLotNumber.Text = _run.ColumnLotNumber; TextColumnLotNumber.IsReadOnly = true;
            TextComments.Text = _run.Comments;

            string file_names = string.Empty;
            if (!String.IsNullOrEmpty(_run.OT.RawFile))
                file_names = _run.OT.RawFile;
            if (!String.IsNullOrEmpty(_run.IT.RawFile))
                file_names += ";" + _run.IT.RawFile;

            TextFile.Text = file_names; TextFile.IsReadOnly = true;
            ButtonFile.IsEnabled = false;
        }

        private void ButtonBrowseFile_Click(object sender, RoutedEventArgs e)
        {
            var ofd = new OpenFileDialog();
            ofd.Filter = "Spectra RAW files (*.raw, *.mzml)|*.raw;*.mzml;*.mzML";
            ofd.Multiselect = true;
            ofd.ShowDialog();
            if (ofd.FileNames == null || ofd.FileNames.Length == 0)
            {
                TextFile.Text = "";
                return;
            }

            if (ofd.FileNames.Length > 2)
            {
                System.Windows.MessageBox.Show(
                               "Please select up to 2 files.",
                               "Q2C :: Warning",
                               (System.Windows.MessageBoxButton)MessageBoxButtons.OK,
                               (System.Windows.MessageBoxImage)MessageBoxIcon.Warning);
                return;
            }

            TextFile.Text = String.Join(";", ofd.FileNames);
            _selected_files = ofd.FileNames;
        }

        private void RunInBatch(string rawsDirectory)
        {
            List<string> upadteDate = new();


            #region files Lumos / Fusion / Exploris
            List<(string IT_file, string OT_file)> raw_files = new();

            /*
            raw_files.Add(("L1_20220103_HEK_IT50FAIMS.raw", "L1_20220103_HEK_OT50FAIMS.raw"));
            raw_files.Add(("L1_20220104_HEK_IT50FAIMS.raw", "L1_20220104_HEK_OT50FAIMS.raw"));
            raw_files.Add(("L1_20220117_HEK_IT50FAIMS.raw", "L1_20220117_HEK_OT50FAIMS.raw"));
            raw_files.Add(("L1_20220122_HEK_IT50FAIMS.raw", "L1_20220122_HEK_OT50FAIMS.raw"));
            raw_files.Add(("L1_20220126_HEK_IT50FAIMS.raw", "L1_20220126_HEK_OT50FAIMS.raw"));
            raw_files.Add(("L1_20220206_HEK_IT50FAIMS.raw", "L1_20220206_HEK_OT50FAIMS.raw"));
            raw_files.Add(("L1_20220211_HEK_IT50FAIMS.raw", "L1_20220211_HEK_OT50FAIMS.raw"));
            raw_files.Add(("L1_20220214_HEK_IT50FAIMS.raw", "L1_20220214_HEK_OT50FAIMS.raw"));
            raw_files.Add(("L1_20220218_HEK_IT50FAIMS.raw", "L1_20220218_HEK_OT50FAIMS.raw"));
            raw_files.Add(("L1_20220222_HEK_IT50FAIMS.raw", "L1_20220222_HEK_OT50FAIMS.raw"));
            raw_files.Add(("L1_20220225_HEK_IT50FAIMS.raw", "L1_20220225_HEK_OT50FAIMS.raw"));
            raw_files.Add(("L1_20220225_HEK_IT50FAIMS_20220225183056.raw", "L1_20220225_HEK_OT50FAIMS.raw"));
            raw_files.Add(("L1_20220311_HEK_IT50FAIMS.raw", "L1_20220311_HEK_OT50FAIMS.raw"));
            raw_files.Add(("L1_20220314_HEK_IT50FAIMS.raw", "L1_20220314_HEK_OT50FAIMS.raw"));
            raw_files.Add(("L1_20220316_HEK_IT50FAIMS.raw", "L1_20220316_HEK_OT50FAIMS.raw"));
            raw_files.Add(("L1_20220318_HEK_IT50FAIMS.raw", "L1_20220318_HEK_OT50FAIMS.raw"));
            raw_files.Add(("L1_20220325_HEK_IT50FAIMS.raw", "L1_20220325_HEK_OT50FAIMS.raw"));
            raw_files.Add(("L1_20220405_HEK_IT50FAIMS.raw", "L1_20220405_HEK_OT50FAIMS_2.raw"));
            raw_files.Add(("L1_20220414_HEK_IT50FAIMS.raw", "L1_20220414_HEK_OT50FAIMS.raw"));
            raw_files.Add(("L1_20220420_HEK_IT50FAIMS.raw", "L1_20220420_HEK_OT50FAIMS.raw"));
            raw_files.Add(("L1_20220422_HEK_IT50FAIMS.raw", "L1_20220422_HEK_OT50FAIMS.raw"));
            raw_files.Add(("L1_20220502_HEK_IT50.raw", "L1_20220502_HEK_OT50.raw"));
            raw_files.Add(("L1_20220509_HEK_IT50.raw", "L1_20220509_HEK_OT50.raw"));
            raw_files.Add(("L1_20220509_HEK_IT50.raw", "L1_20220509_HEK_OT50_20220509154218.raw"));
            raw_files.Add(("L1_20220513_HEK_IT50.raw", "L1_20220513_HEK_OT50.raw"));
            raw_files.Add(("L1_20220518_HEK_IT50FAIMS.raw", "L1_20220518_HEK_OT50FAIMS.raw"));
            raw_files.Add(("L1_20220518_HEK_IT50FAIMS.raw", "L1_20220519_HEK_OT50FAIMS.raw"));
            raw_files.Add(("L1_20220518_HEK_IT50FAIMS.raw", "L1_20220519_HEK_OT50FAIMS_2.raw"));
            raw_files.Add(("L1_20220518_HEK_IT50FAIMS.raw", "L1_20220519_HEK_OT50FAIMS_20220519135446.raw"));
            raw_files.Add(("L1_20220518_HEK_IT50FAIMS.raw", "L1_20220519_HEK_OT50FAIMS_3.raw"));
            raw_files.Add(("L1_20220520_HEK_IT50FAIMS.raw", "L1_20220520_HEK_OT50FAIMS.raw"));
            raw_files.Add(("L1_20220520_HEK_IT50FAIMS.raw", "L1_20220520_HEK_OT50FAIMS_20220520140254.raw"));
            raw_files.Add(("L1_20220520_HEK_IT50FAIMS.raw", "L1_20220523_HEK_OT50FAIMS.raw"));
            raw_files.Add(("L1_20220528_HEK_IT50.raw", "L1_20220528_HEK_OT50.raw"));
            raw_files.Add(("L1_20220530_HEK_IT50FAIMS.raw", "L1_20220530_HEK_OT50FAIMS.raw"));
            raw_files.Add(("L1_20220530_HEK_IT50FAIMS_20220531112132.raw", "L1_20220530_HEK_OT50FAIMS_2.raw"));
            raw_files.Add(("L1_20220530_HEK_IT50FAIMS_20220531112132.raw", "L1_20220530_HEK_OT50FAIMS_3.raw"));
            raw_files.Add(("L1_20220531_HEK_IT50FAIMS.raw", "L1_20220531_HEK_OT50FAIMS.raw"));
            raw_files.Add(("L1_20220605_HEK_IT50FAIMS.raw", "L1_20220605_HEK_OT50FAIMS.raw"));
            raw_files.Add(("L1_20220607_HEK_IT50FAIMS.raw", "L1_20220607_HEK_OT50FAIMS.raw"));
            raw_files.Add(("L1_20220608_HEK_IT50FAIMS.raw", "L1_20220608_HEK_OT50FAIMS.raw"));
            raw_files.Add(("L1_20220613_HEK_IT50FAIMS.raw", "L1_20220613_HEK_OT50FAIMS.raw"));
            raw_files.Add(("L1_20220614_HEK_IT50FAIMS.raw", "L1_20220614_HEK_OT50FAIMS.raw"));
            raw_files.Add(("L1_20220614_HEK_IT50FAIMS.raw", "L1_20220616_HEK_OT50FAIMS.raw"));
            raw_files.Add(("L1_20220620_HEK_IT50FAIMS.raw", "L1_20220620_HEK_OT50FAIMS.raw"));
            raw_files.Add(("L1_20220624_HEK_IT50FAIMS.raw", "L1_20220624_HEK_OT50FAIMS.raw"));
            raw_files.Add(("L1_20220630_HEK_IT50FAIMS.raw", "L1_20220630_HEK_OT50FAIMS.raw"));
            raw_files.Add(("L1_20220708_HEK_IT50FAIMS.raw", "L1_20220708_HEK_OT50FAIMS.raw"));
            raw_files.Add(("L1_20220711_HEK_IT50FAIMS.raw", "L1_20220711_HEK_OT50FAIMS.raw"));
            raw_files.Add(("L1_20220712_HEK_IT50FAIMS.raw", "L1_20220712_HEK_OT50FAIMS.raw"));
            raw_files.Add(("L1_20220719_HEK_IT50FAIMS.raw", "L1_20220719_HEK_OT50FAIMS.raw"));
            raw_files.Add(("L1_20220723_HEK_IT50FAIMS.raw", "L1_20220723_HEK_OT50FAIMS.raw"));
            raw_files.Add(("L1_20220728_HEK_IT50FAIMS.raw", "L1_20220728_HEK_OT50FAIMS.raw"));
            raw_files.Add(("L1_20220805_HEK_IT50FAIMS.raw", "L1_20220805_HEK_OT50FAIMS.raw"));
            raw_files.Add(("L1_20220810_HEK_IT50FAIMS.raw", "L1_20220810_HEK_OT50FAIMS.raw"));
            raw_files.Add(("L1_20220811_HEK_IT50FAIMS.raw", "L1_20220811_HEK_OT50FAIMS.raw"));
            raw_files.Add(("L1_20220812_HEK_IT50.raw", "L1_20220812_HEK_OT50.raw"));
            raw_files.Add(("L1_20220818_HEK_IT50.raw", "L1_20220818_HEK_OT50.raw"));
            raw_files.Add(("L1_20220818_HEK_IT50_afterrun.raw", "L1_20220818_HEK_OT50_afterrun.raw"));
            raw_files.Add(("L1_20220823_HEK_IT50FAIMS.raw", "L1_20220823_HEK_OT50FAIMS.raw"));
            raw_files.Add(("L1_20220823_HEK_IT50FAIMS_2.raw", "L1_20220823_HEK_OT50FAIMS_2.raw"));
            raw_files.Add(("L1_20220824_HEK_IT50FAIMS.raw", "L1_20220824_HEK_OT50FAIMS.raw"));
            raw_files.Add(("L1_20220826_HEK_IT50FAIMS.raw", "L1_20220826_HEK_OT50FAIMS.raw"));
            raw_files.Add(("L1_20220826_HEK_IT50FAIMS.raw", "L1_20220826_HEK_OT50FAIMS_20220827003445.raw"));
            raw_files.Add(("L1_20220826_HEK_IT50FAIMS.raw", "L1_20220826_HEK_OT50FAIMS_20220828160147.raw"));
            raw_files.Add(("L1_20220830_HEK_IT50FAIMS.raw", "L1_20220830_HEK_OT50FAIMS.raw"));
            raw_files.Add(("L1_20220901_HEK_IT50.raw", "L1_20220901_HEK_OT50.raw"));
            raw_files.Add(("L1_20220901_HEK_IT50_3.raw", "L1_20220901_HEK_OT50_3.raw"));
            raw_files.Add(("L1_20220912_HEK_IT50_20220918220918.raw", "L1_20220912_HEK_OT50_20220918204944.raw"));
            raw_files.Add(("L1_20220921_HEK_IT50.raw", "L1_20220921_HEK_OT50.raw"));
            raw_files.Add(("L1_20220922_HEK_IT50FAIMS.raw", "L1_20220922_HEK_OT50FAIMS.raw"));
            raw_files.Add(("L1_20220922_HEK_IT50FAIMS_2.raw", "L1_20220922_HEK_OT50FAIMS_2.raw"));
            raw_files.Add(("L1_20220923_HEK_IT50.raw", "L1_20220923_HEK_OT50.raw"));
            raw_files.Add(("L1_20220923_HEK_IT50_20220923210405.raw", "L1_20220923_HEK_OT50_20220923194429.raw"));
            raw_files.Add(("L1_20220928_HEK_IT50.raw", "L1_20220928_HEK_OT50.raw"));
            raw_files.Add(("L1_20220928_HEK_IT50FAIMS.raw", "L1_20220928_HEK_OT50FAIMS.raw"));
            raw_files.Add(("L1_20220928_HEK_IT50FAIMS_2.raw", "L1_20220928_HEK_OT50FAIMS_2.raw"));
            raw_files.Add(("L1_20221001_HEK_IT50FAIMS.raw", "L1_20221001_HEK_OT50FAIMS.raw"));
            raw_files.Add(("L1_20221001_HEK_IT50FAIMS_2.raw", "L1_20221001_HEK_OT50FAIMS_2.raw"));
            raw_files.Add(("L1_20221006_HEK_IT50_FusionHPLC.raw", "L1_20221006_HEK_OT50_FusionHPLC.raw"));
            raw_files.Add(("L1_20221007_HEK_IT50_FusionHPLC_FAIMS.raw", "L1_20221007_HEK_OT50_FusionHPLC_FAIMS.raw"));
            raw_files.Add(("L1_20221010_HEK_IT50_FusionHPLC.raw", "L1_20221010_HEK_OT50_FusionHPLC.raw"));
            raw_files.Add(("L1_20221010_HEK_IT50_FusionHPLC_remeasure.raw", "L1_20221010_HEK_OT50_FusionHPLC.raw"));
            raw_files.Add(("L1_20221011_HEK_IT50FAIMS_1.raw", "L1_20221011_HEK_OT50FAIMS_1.raw"));
            raw_files.Add(("L1_20221011_HEK_IT50FAIMS_2.raw", "L1_20221011_HEK_OT50FAIMS_2_Fusionmethod.raw"));
            raw_files.Add(("L1_20221011_HEK_IT50FAIMS_3.raw", "L1_20221011_HEK_OT50FAIMS_3_Fusionmethod.raw"));
            raw_files.Add(("L1_20221022_HEK_IT50_1.raw", "L1_20221022_HEK_OT50_1.raw"));
            raw_files.Add(("L1_20221022_HEK_IT50_2.raw", "L1_20221022_HEK_OT50_2.raw"));
            raw_files.Add(("L1_20221130_HEK_IT50.raw", "L1_20221130_HEK_OT50.raw"));
            raw_files.Add(("L1_20230627_HEK_IT50_EliteHPLC_NCE30.raw", "L1_20230627_HEK_OT50_EliteHPLC_NCE30.raw"));
            raw_files.Add(("L1_20230627_HEK_IT50_EliteHPLC_NCE32.raw", "L1_20230627_HEK_OT50_EliteHPLC_NCE32.raw"));
            raw_files.Add(("L1_20230630_HEK_IT50_EliteHPLC_NCE30.raw", "L1_20230630_HEK_OT50_EliteHPLC_NCE30.raw"));
            raw_files.Add(("L1_20230703_HEK_IT50.raw", "L1_20230703_HEK_OT50.raw"));
            
            
            raw_files.Add(("L1_20230710_HEK_IT50.raw", "L1_20230710_HEK_OT50.raw"));
            raw_files.Add(("L1_20230711_HEK_IT50.raw", "L1_20230711_HEK_OT50.raw"));
            raw_files.Add(("L1_20230717_HEK_IT50.raw", "L1_20230717_HEK_OT50.raw"));
            raw_files.Add(("L1_20230717_HEK_IT50_20230719070632.raw", "L1_20230717_HEK_OT50_20230719082616.raw"));
            raw_files.Add(("L1_20230717_HEK_IT50_20230719070632.raw", "L1_20230717_HEK_OT50_20230719220408.raw"));
            raw_files.Add(("L1_20230709_HEK_IT50.raw", "L1_20230709_HEK_OT50.raw"));
            raw_files.Add(("L1_20230726_HEK_IT50.raw", "L1_20230726_HEK_OT50.raw"));
            
            raw_files.Add(("L1_20230726_HEK_IT50.raw", "L1_20230726_HEK_OT50_HCD32.raw"));
            raw_files.Add(("L1_20230727_HEK_IT50.raw", "L1_20230727_HEK_OT50.raw"));
            raw_files.Add(("L1_20230727_HEK_IT50_20230727193211.raw", "L1_20230728_HEK_OT50.raw"));
            raw_files.Add(("L1_20230728_HEK_IT50_noFAIMS.raw", "L1_20230728_HEK_OT50_noFAIMS.raw"));
            raw_files.Add(("L1_20230731_HEK_IT50.raw", "L1_20230731_HEK_OT50.raw"));
            raw_files.Add(("L1_20230806_HEK_IT50.raw", "L1_20230806_HEK_OT50.raw"));
            raw_files.Add(("L1_20230813_HEK_IT50.raw", "L1_20230813_HEK_OT50.raw"));
            raw_files.Add(("L1_20230817_HEK_IT50.raw", "L1_20230817_HEK_OT50.raw"));
            raw_files.Add(("L1_20230822_HEK_IT50.raw", "L1_20230822_HEK_OT50.raw"));
            raw_files.Add(("L1_20230823_HEK_IT50.raw", "L1_20230823_HEK_OT50.raw"));
            raw_files.Add(("L1_20230831_HEK_IT50.raw", "L1_20230831_HEK_OT50.raw"));
            raw_files.Add(("L1_20230908_HEK_IT50.raw", "L1_20230908_HEK_OT50.raw"));

            

            raw_files.Add(("F1_20220103_HEK_IT50.raw", "F1_20220103_HEK_OT50.raw"));
            raw_files.Add(("F1_20220104_HEK_IT50.raw", "F1_20220104_HEK_OT50.raw"));
            raw_files.Add(("F1_20220105_HEK_IT50.raw", "F1_20220105_HEK_OT50.raw"));
            raw_files.Add(("F1_20220106_HEK_IT50.raw", "F1_20220106_HEK_OT50.raw"));
            raw_files.Add(("F1_20220107_HEK_IT50.raw", "F1_20220107_HEK_OT50.raw"));
            raw_files.Add(("F1_20220110_HEK_IT50_column_56.raw", "F1_20220110_HEK_OT50_column56.raw"));
            raw_files.Add(("F1_20220112_HEK_IT50_column_50a.raw", "F1_20220112_HEK_OT50_column50a.raw"));
            raw_files.Add(("F1_20220113_HEK_IT50_column_50a.raw", "F1_20220113_HEK_OT50_column50a.raw"));
            raw_files.Add(("F1_20220114_HEK_IT50_column.raw", "F1_20220114_HEK_OT50_column.raw"));
            raw_files.Add(("F1_20220117_HEK_IT50_column.raw", "F1_20220117_HEK_OT50_column.raw"));
            raw_files.Add(("F1_20220117_HEK_IT50_column_20220203180840.raw", "F1_20220117_HEK_OT50_column_20220203164835.raw"));
            raw_files.Add(("F1_20220121_HEK_IT50.raw", "F1_20220121_HEK_OT50.raw"));
            raw_files.Add(("F1_20220125_HEK_IT50_column_55.raw", "F1_20220125_HEK_OT50_column55.raw"));
            raw_files.Add(("F1_20220128_HEK_IT50.raw", "F1_20220128_HEK_OT50.raw"));
            raw_files.Add(("F1_20220210_HEK_IT50.raw", "F1_20220210_HEK_OT50.raw"));
            raw_files.Add(("F1_20220211_HEK_IT50.raw", "F1_20220211_HEK_OT50.raw"));
            raw_files.Add(("F1_20220211_HEK_IT50_20220211190329.raw", "F1_20220211_HEK_OT50_20220211174325.raw"));
            raw_files.Add(("F1_20220218_HEK_IT50.raw", "F1_20220218_HEK_OT50.raw"));
            raw_files.Add(("F1_20220225_HEK_IT50.raw", "F1_20220225_HEK_OT50.raw"));
            raw_files.Add(("F1_20220301_HEK_IT50_column.raw", "F1_20220301_HEK_OT50_column.raw"));
            raw_files.Add(("F1_20220311_HEK_IT50_column.raw", "F1_20220311_HEK_OT50_column.raw"));
            raw_files.Add(("F1_20220314_HEK_IT50.raw", "F1_20220314_HEK_OT50.raw"));
            raw_files.Add(("F1_20220315_HEK_IT50.raw", "F1_20220315_HEK_OT50.raw"));
            raw_files.Add(("F1_20220315_HEK_IT50.raw", "F1_20220315_HEK_OT50_20220315160046.raw"));
            raw_files.Add(("F1_20220315_HEK_IT50.raw", "F1_20220315_HEK_OT50_20220315230052.raw"));
            raw_files.Add(("F1_20220318_HEK_IT50.raw", "F1_20220318_HEK_OT50.raw"));
            raw_files.Add(("F1_20220325_HEK_IT50.raw", "F1_20220325_HEK_OT50.raw"));
            raw_files.Add(("F1_20220406_HEK_IT50.raw", "F1_20220406_HEK_OT50.raw"));
            raw_files.Add(("F1_20220407_HEK_IT50.raw", "F1_20220407_HEK_OT50.raw"));
            raw_files.Add(("F1_20220416_HEK_IT50.raw", "F1_20220416_HEK_OT50.raw"));
            raw_files.Add(("F1_20220418_HEK_IT50.raw", "F1_20220418_HEK_OT50.raw"));
            raw_files.Add(("F1_20220422_HEK_IT50.raw", "F1_20220422_HEK_OT50.raw"));
            raw_files.Add(("F1_20220426_HEK_IT50.raw", "F1_20220426_HEK_OT50.raw"));
            raw_files.Add(("F1_20220429_HEK_IT50.raw", "F1_20220429_HEK_OT50.raw"));
            raw_files.Add(("F1_20220506_HEK_IT50.raw", "F1_20220506_HEK_OT50.raw"));
            raw_files.Add(("F1_20220516_HEK_IT50.raw", "F1_20220516_HEK_OT50.raw"));
            raw_files.Add(("F1_20220516_HEK_IT50_20220517000240.raw", "F1_20220516_HEK_OT50_20220516224301.raw"));
            raw_files.Add(("F1_20220517_HEK_IT50.raw", "F1_20220517_HEK_OT50.raw"));
            raw_files.Add(("F1_20220517_HEK_IT50_2.raw", "F1_20220517_HEK_OT50_2.raw"));
            raw_files.Add(("F1_20220517_HEK_IT50_2_20220517185301.raw", "F1_20220517_HEK_OT50_2.raw"));
            raw_files.Add(("F1_20220519_HEK_IT50.raw", "F1_20220519_HEK_OT50.raw"));
            raw_files.Add(("F1_20220519_HEK_IT50_2.raw", "F1_20220519_HEK_OT50_2.raw"));
            raw_files.Add(("F1_20220519_HEK_IT50_20220519134956.raw", "F1_20220519_HEK_OT50_2.raw"));
            raw_files.Add(("F1_20220519_HEK_IT50_3.raw", "F1_20220519_HEK_OT50_3.raw"));
            raw_files.Add(("F1_20220520_HEK_IT50.raw", "F1_20220520_HEK_OT50.raw"));
            raw_files.Add(("F1_20220527_HEK_IT50.raw", "F1_20220527_HEK_OT50.raw"));
            raw_files.Add(("F1_20220606_HEK_IT50.raw", "F1_20220606_HEK_OT50.raw"));
            raw_files.Add(("F1_20220607_HEK_IT50_cal.raw", "F1_20220607_HEK_OT50_cal.raw"));
            raw_files.Add(("F1_20220617_HEK_IT50_cal.raw", "F1_20220617_HEK_OT50_cal.raw"));
            raw_files.Add(("F1_20220621_HEK_IT50.raw", "F1_20220630_HEK_OT50.raw"));
            raw_files.Add(("F1_20220708_HEK_IT50.raw", "F1_20220708_HEK_OT50.raw"));
            raw_files.Add(("F1_20220711_HEK_IT50.raw", "F1_20220711_HEK_OT50.raw"));
            raw_files.Add(("F1_20220711_HEK_IT50_20220712152718.raw", "F1_20220711_HEK_OT50_20220712140734.raw"));
            raw_files.Add(("F1_20220711_HEK_IT50_20220716064229.raw", "F1_20220711_HEK_OT50_20220716052248.raw"));
            raw_files.Add(("F1_20220711_HEK_IT50_20220716064229.raw", "F1_20220711_HEK_OT50_20220718135926.raw"));
            raw_files.Add(("F1_20220714_HEK_IT50.raw", "F1_20220714_HEK_OT50.raw"));
            raw_files.Add(("F1_20220715_HEK_IT50.raw", "F1_20220715_HEK_OT50.raw"));
            raw_files.Add(("F1_20220715_HEK_IT50_20220718174720.raw", "F1_20220715_HEK_OT50_20220718162030.raw"));
            raw_files.Add(("F1_20220718_HEK_IT50_20220718093051.raw", "F1_20220718_HEK_OT50_20220718081107.raw"));
            raw_files.Add(("F1_20220720_HEK_IT50.raw", "F1_20220720_HEK_OT50.raw"));
            raw_files.Add(("F1_20220729_HEK_IT50.raw", "F1_20220729_HEK_OT50.raw"));
            raw_files.Add(("F1_20220801_HEK_IT50.raw", "F1_20220801_HEK_OT50.raw"));
            raw_files.Add(("F1_20220801_HEK_IT50_20220802194023.raw", "F1_20220801_HEK_OT50_20220802182038.raw"));
            raw_files.Add(("F1_20221006_HEK_IT50.raw", "F1_20221006_HEK_OT50.raw"));
            raw_files.Add(("F1_20221006_HEK_IT50_20221018015703.raw", "F1_20221006_HEK_OT50_20221018003724.raw"));
            raw_files.Add(("F1_20221012_HEK_IT50_20221012201947.raw", "F1_20221012_HEK_OT50_20221012185952.raw"));
            raw_files.Add(("F1_20221021_HEK_IT50.raw", "F1_20221021_HEK_OT50.raw"));
            raw_files.Add(("F1_20221024_HEK_IT50.raw", "F1_20221024_HEK_OT50.raw"));
            raw_files.Add(("F1_20221107_HEK_IT50.raw", "F1_20221107_HEK_OT50.raw"));
            raw_files.Add(("F1_20221111_HEK_IT50.raw", "F1_20221111_HEK_OT50.raw"));
            raw_files.Add(("F1_20221111_HEK_IT50_20221113155918.raw", "F1_20221111_HEK_OT50_20221113143942.raw"));
            raw_files.Add(("F1_20221117_HEK_IT50.raw", "F1_20221117_HEK_OT50.raw"));
            raw_files.Add(("F1_20221125_HEK_IT50.raw", "F1_20221125_HEK_OT50.raw"));
            raw_files.Add(("F1_20221125_HEK_IT50.raw", "F1_20221125_HEK_OT50_20221125111544.raw"));
            raw_files.Add(("F1_20221130_HEK_IT50.raw", "F1_20221130_HEK_OT50.raw"));
            raw_files.Add(("F1_20221201_HEK_IT50.raw", "F1_20221201_HEK_OT50_1.raw"));
            raw_files.Add(("F1_20221201_HEK_IT50.raw", "F1_20221201_HEK_OT50_2.raw"));
            raw_files.Add(("F1_20221201_HEK_IT50.raw", "F1_20221201_HEK_OT50_3.raw"));
            raw_files.Add(("F1_20221202_HEK_IT50_1.raw", "F1_20221202_HEK_OT50_3.raw"));
            raw_files.Add(("F1_20221202_HEK_IT50_1.raw", "F1_20221202_HEK_OT50_4.raw"));
            raw_files.Add(("F1_20221208_HEK_IT50_1.raw", "F1_20221208_HEK_OT50_1.raw"));
            raw_files.Add(("F1_20221208_HEK_IT50_1.raw", "F1_20221208_HEK_OT50_2.raw"));
            raw_files.Add(("F1_20221208_HEK_IT50_1.raw", "F1_20221208_HEK_OT50_2_20221208155918.raw"));
            raw_files.Add(("F1_20221208_HEK_IT50_1.raw", "F1_20221208_HEK_OT50_2_20221208160246.raw"));
            raw_files.Add(("F1_20221209_HEK_IT50_1.raw", "F1_20221209_HEK_OT50_1.raw"));
            raw_files.Add(("F1_20221209_HEK_IT50_1.raw", "F1_20221209_HEK_OT50_2.raw"));
            raw_files.Add(("F1_20221220_HEK_IT50_1.raw", "F1_20221220_HEK_OT50_1.raw"));
            raw_files.Add(("F1_20221220_HEK_IT50_1_20221227104424.raw", "F1_20221220_HEK_OT50_1_20221227092423.raw"));
            raw_files.Add(("F1_20221229_HEK_IT50_1.raw", "F1_20221229_HEK_OT50_1.raw"));
            raw_files.Add(("F1_20230102_HEK_IT50_1.raw", "F1_20230102_HEK_OT50_1.raw"));
            raw_files.Add(("F1_20230102_HEK_IT50_1_20230109153823.raw", "F1_20230102_HEK_OT50_1_20230109141812.raw"));
            raw_files.Add(("F1_20230117_HEK_IT50.raw", "F1_20230117_HEK_OT50.raw"));
            raw_files.Add(("F1_20230119_HEK_IT50.raw", "F1_20230119_HEK_OT50.raw"));
            raw_files.Add(("F1_20230125_HEK_IT50.raw", "F1_20230125_HEK_OT50.raw"));
            raw_files.Add(("F1_20230125_HEK_IT50_1.raw", "F1_20230125_HEK_OT50_1.raw"));
            raw_files.Add(("F1_20230127_HEK_IT50.raw", "F1_20230127_HEK_OT50.raw"));
            raw_files.Add(("F1_20230127_HEK_IT50_20230127180020.raw", "F1_20230127_HEK_OT50_20230127164027.raw"));
            raw_files.Add(("F1_20230202_HEK_IT50_1.raw", "F1_20230202_HEK_OT50_1.raw"));
            raw_files.Add(("F1_20230202_HEK_IT50_1_20230213000101.raw", "F1_20230202_HEK_OT50_1_20230212224112.raw"));
            raw_files.Add(("F1_20230206_HEK_IT50_1.raw", "F1_20230206_HEK_OT50_1.raw"));
            raw_files.Add(("F1_20230209_HEK_IT50.raw", "F1_20230209_HEK_OT50.raw"));
            raw_files.Add(("F1_20230209_HEK_IT50_1.raw", "F1_20230209_HEK_OT50_1.raw"));
            raw_files.Add(("F1_20230216_HEK_IT50.raw", "F1_20230216_HEK_OT50.raw"));
            raw_files.Add(("F1_20230217_HEK_IT50.raw", "F1_20230217_HEK_OT50.raw"));
            raw_files.Add(("F1_20230222_HEK_IT50.raw", "F1_20230222_HEK_OT50.raw"));
            raw_files.Add(("F1_20230224_HEK_IT50.raw", "F1_20230224_HEK_OT50.raw"));
            raw_files.Add(("F1_20230227_HEK_IT50.raw", "F1_20230227_HEK_OT50.raw"));
            raw_files.Add(("F1_20230227_HEK_IT50_20230303070814.raw", "F1_20230227_HEK_OT50_20230303054825.raw"));
            raw_files.Add(("F1_20230307_HEK_IT50_1.raw", "F1_20230307_HEK_OT50_1.raw"));
            raw_files.Add(("F1_20230307_HEK_IT50_2.raw", "F1_20230307_HEK_OT50_2.raw"));
            raw_files.Add(("F1_20230308_HEK_IT50_RF60_1.raw", "F1_20230308_HEK_OT50_RF60_1.raw"));
            raw_files.Add(("F1_20230310_HEK_IT50.raw", "F1_20230310_HEK_OT50.raw"));
            raw_files.Add(("F1_20230310_HEK_IT50_old.raw", "F1_20230310_HEK_OT50_old.raw"));
            raw_files.Add(("F1_20230313_HEK_IT50_new.raw", "F1_20230313_HEK_OT50_new.raw"));
            raw_files.Add(("F1_20230316_HEK_IT50_new.raw", "F1_20230316_HEK_OT50_new.raw"));
            raw_files.Add(("F1_20230317_HEK_IT50_new.raw", "F1_20230317_HEK_OT50_new.raw"));
            raw_files.Add(("F1_20230320_HEK_IT50_new_1.raw", "F1_20230320_HEK_OT50_new.raw"));
            raw_files.Add(("F1_20230320_HEK_IT50_new_2.raw", "F1_20230320_HEK_OT50_new_1.raw"));
            raw_files.Add(("F1_20230320_HEK_IT50_new_3.raw", "F1_20230320_HEK_OT50_new_2.raw"));
            raw_files.Add(("F1_20230320_HEK_IT50_new_4.raw", "F1_20230320_HEK_OT50_new_3.raw"));
            raw_files.Add(("F1_20230320_HEK_IT50_new_5.raw", "F1_20230320_HEK_OT50_new_4.raw"));
            raw_files.Add(("F1_20230406_HEK_IT50.raw", "F1_20230406_HEK_OT50.raw"));
            raw_files.Add(("F1_20230406_HEK_IT50_1.raw", "F1_20230406_HEK_OT50_1.raw"));
            raw_files.Add(("F1_20230406_HEK_IT50_1_20230410033214.raw", "F1_20230406_HEK_OT50_1_20230410021221.raw"));
            raw_files.Add(("F1_20230411_HEK_IT50.raw", "F1_20230411_HEK_OT50.raw"));
            raw_files.Add(("F1_20230417_HEK_IT50.raw", "F1_20230417_HEK_OT50.raw"));
            raw_files.Add(("F1_20230417_HEK_IT50_20230419164818.raw", "F1_20230417_HEK_OT50_2.raw"));
            raw_files.Add(("F1_20230417_HEK_IT50_20230419164818.raw", "F1_20230417_HEK_OT50_2_20230419180813.raw"));
            raw_files.Add(("F1_20230417_HEK_IT50_20230419164818.raw", "F1_20230417_HEK_OT50_20230419152824.raw"));
            raw_files.Add(("F1_20230424_HEK_IT50.raw", "F1_20230424_HEK_OT50.raw"));
            raw_files.Add(("F1_20230425_HEK_IT50.raw", "F1_20230425_HEK_OT50.raw"));
            raw_files.Add(("F1_20230426_HEK_IT50.raw", "F1_20230426_HEK_OT50.raw"));
            raw_files.Add(("F1_20230426_HEK_IT50_2.raw", "F1_20230426_HEK_OT50_2.raw"));
            raw_files.Add(("F1_20230426_HEK_IT50_3.raw", "F1_20230426_HEK_OT50_3.raw"));
            raw_files.Add(("F1_20230426_HEK_IT50_4.raw", "F1_20230426_HEK_OT50_4.raw"));
            raw_files.Add(("F1_20230503_HEK_IT50.raw", "F1_20230503_HEK_OT50.raw"));
            raw_files.Add(("F1_20230504_HEK_IT50.raw", "F1_20230504_HEK_OT50.raw"));
            raw_files.Add(("F1_20230504_HEK_IT50_20230508160738.raw", "F1_20230504_HEK_OT50_20230508144742.raw"));
            raw_files.Add(("F1_20230513_HEK_IT50.raw", "F1_20230513_HEK_OT50.raw"));
            raw_files.Add(("F1_20230515_HEK_IT50.raw", "F1_20230515_HEK_OT50.raw"));
            raw_files.Add(("F1_20230515_HEK_IT50_20230519134213.raw", "F1_20230515_HEK_OT50_20230519122229.raw"));
            raw_files.Add(("F1_20230515_HEK_IT50_20230521145330.raw", "F1_20230515_HEK_OT50_20230521133333.raw"));
            raw_files.Add(("F1_20230522_HEK_IT50.raw", "F1_20230522_HEK_OT50.raw"));
            raw_files.Add(("F1_20230523_HEK_IT50.raw", "F1_20230523_HEK_OT50.raw"));
            raw_files.Add(("F1_20230523_HEK_IT50.raw", "F1_20230523_HEK_OT50_20230524140509.raw"));
            raw_files.Add(("F1_20230530_HEK_IT50.raw", "F1_20230530_HEK_OT50.raw"));
            raw_files.Add(("F1_20230606_HEK_IT50.raw", "F1_20230606_HEK_OT50.raw"));
            raw_files.Add(("F1_20230608_HEK_IT50.raw", "F1_20230608_HEK_OT50.raw"));
            raw_files.Add(("F1_20230609_HEK_IT50.raw", "F1_20230609_HEK_OT50.raw"));
            raw_files.Add(("F1_20230609_HEK_IT50_20230609230054.raw", "F1_20230609_HEK_OT50_20230609214101.raw"));
            raw_files.Add(("F1_20230616_HEK_IT50.raw", "F1_20230616_HEK_OT50.raw"));
            raw_files.Add(("F1_20230617_HEK_IT50.raw", "F1_20230617_HEK_OT50.raw"));
            raw_files.Add(("F1_20230619_HEK_IT50.raw", "F1_20230619_HEK_OT50.raw"));
            raw_files.Add(("F1_20230619_HEK_IT50_2.raw", "F1_20230619_HEK_OT50_2.raw"));
            raw_files.Add(("F1_20230620_HEK_IT50.raw", "F1_20230620_HEK_OT50.raw"));
            raw_files.Add(("F1_20230621_HEK_IT50.raw", "F1_20230621_HEK_OT50.raw"));
            raw_files.Add(("F1_20230623_HEK_IT50.raw", "F1_20230623_HEK_OT50.raw"));
            raw_files.Add(("F1_20230627_HEK_IT50.raw", "F1_20230627_HEK_OT50.raw"));
            raw_files.Add(("F1_20230630_HEK_IT50.raw", "F1_20230630_HEK_OT50.raw"));
            raw_files.Add(("F1_20230704_HEK_IT50.raw", "F1_20230704_HEK_OT50.raw"));
            raw_files.Add(("F1_20230704_HEK_IT50_20230706204738.raw", "F1_20230704_HEK_OT50_20230706192803.raw"));
            raw_files.Add(("F1_20230704_HEK_IT50_20230712171901.raw", "F1_20230704_HEK_OT50_20230712155907.raw"));
            raw_files.Add(("F1_20230710_HEK_IT50.raw", "F1_20230710_HEK_OT50.raw"));
            raw_files.Add(("F1_20230714_HEK_IT50.raw", "F1_20230714_HEK_OT50.raw"));
            raw_files.Add(("F1_20230717_HEK_IT50.raw", "F1_20230717_HEK_OT50.raw"));
            raw_files.Add(("F1_20230718_HEK_IT50.raw", "F1_20230718_HEK_OT50.raw"));
            raw_files.Add(("F1_20230719_HEK_IT50.raw", "F1_20230719_HEK_OT50.raw"));
            raw_files.Add(("F1_20230802_HEK_IT50_1.raw", "F1_20230802_HEK_OT50_1.raw"));
            raw_files.Add(("F1_20230802_HEK_IT50_2.raw", "F1_20230802_HEK_OT50_2.raw"));
            raw_files.Add(("F1_20230811_HEK_IT50.raw", "F1_20230811_HEK_OT50.raw"));
            raw_files.Add(("F1_20230814_HEK_IT50.raw", "F1_20230814_HEK_OT50.raw"));
            raw_files.Add(("F1_20230815_HEK_IT50.raw", "F1_20230815_HEK_OT50.raw"));
            raw_files.Add(("F1_20230817_HEK_IT50.raw", "F1_20230817_HEK_OT50.raw"));
            raw_files.Add(("F1_20230822_HEK_IT50.raw", "F1_20230822_HEK_OT50.raw"));
            raw_files.Add(("F1_20230824_HEK_IT50_LumosLC.raw", "F1_20230824_HEK_OT50_LumosLC.raw"));
            raw_files.Add(("F1_20230830_HEK_IT50.raw", "F1_20230830_HEK_OT50.raw"));
            raw_files.Add(("F1_20230901_HEK_IT50.raw", "F1_20230901_HEK_OT50.raw"));
            raw_files.Add(("F1_20230906_HEK_IT50.raw", "F1_20230906_HEK_OT50.raw"));

            

            raw_files.Add(("", "Ex1_20230424_HEK_OT50_1.raw"));
            raw_files.Add(("", "Ex1_20230425_HEK_OT50_1.raw"));
            raw_files.Add(("", "Ex1_20230426_HEK_OT50_1.raw"));
            raw_files.Add(("", "Ex1_20230426_HEK_OT50_2.raw"));
            raw_files.Add(("", "Ex1_20230428_HEK_OT50.raw"));
            raw_files.Add(("", "Ex1_20230504_HEK_OT50.raw"));
            raw_files.Add(("", "Ex1_20230504_HEK_OT50_2.raw"));
            raw_files.Add(("", "Ex1_20230504_HEK_OT50_2_20230507133611.raw"));
            raw_files.Add(("", "Ex1_20230508_HEK_OT50.raw"));
            raw_files.Add(("", "Ex1_20230508_HEK_OT50_2.raw"));
            raw_files.Add(("", "Ex1_20230509_HEK_FAIMS_OT50.raw"));
            raw_files.Add(("", "Ex1_20230509_HEK_FAIMS_OT50_1.raw"));
            raw_files.Add(("", "Ex1_20230509_HEK_FAIMS_OT50_2.raw"));
            raw_files.Add(("", "Ex1_20230509_HEK_FAIMS_OT50_20230509201614.raw"));
            raw_files.Add(("", "Ex1_20230509_HEK_OT50.raw"));
            raw_files.Add(("", "Ex1_20230510_HEK_FAIMS_OT50.raw"));
            raw_files.Add(("", "Ex1_20230510_HEK_noFAIMS_OT50.raw"));
            raw_files.Add(("", "Ex1_20230511_HEK_FAIMS_OT50.raw"));
            raw_files.Add(("", "Ex1_20230511_HEK_noFAIMS_OT50.raw"));
            raw_files.Add(("", "Ex1_20230512_HEK_noFAIMS_OT50.raw"));
            raw_files.Add(("", "Ex1_20230512_HEK_noFAIMS_OT50_1.raw"));
            raw_files.Add(("", "Ex1_20230516_HEK_noFAIMS_OT50.raw"));
            raw_files.Add(("", "Ex1_20230516_HEK_noFAIMS_OT50_1.raw"));
            raw_files.Add(("", "Ex1_20230519_HEK_noFAIMS_OT50.raw"));
            raw_files.Add(("", "Ex1_20230519_HEK_noFAIMS_OT50_2.raw"));
            raw_files.Add(("", "Ex1_20230520_HEK_noFAIMS_OT50_1.raw"));
            raw_files.Add(("", "Ex1_20230520_HEK_noFAIMS_OT50_2a.raw"));
            raw_files.Add(("", "Ex1_20230523_HEK_noFAIMS_OT50.raw"));
            raw_files.Add(("", "Ex1_20230523_HEK_noFAIMS_OT50a.raw"));
            raw_files.Add(("", "Ex1_20230529_HEK_noFAIMS_OT50_1.raw"));
            raw_files.Add(("", "Ex1_20230529_HEK_noFAIMS_OT50_2.raw"));
            raw_files.Add(("", "Ex1_20230615_HEK_noFAIMS_OT50_1.raw"));
            raw_files.Add(("", "Ex1_20230615_HEK_noFAIMS_OT50_2.raw"));
            raw_files.Add(("", "Ex1_20230622_HEK_FAIMS_OT50_1.raw"));
            raw_files.Add(("", "Ex1_20230622_HEK_FAIMS_OT50_2.raw"));
            raw_files.Add(("", "Ex1_20230627_HEK_FAIMS_OT50_1.raw"));
            raw_files.Add(("", "Ex1_20230627_HEK_FAIMS_OT50_2.raw"));
            raw_files.Add(("", "Ex1_20230630_HEK_noFAIMS_OT50_1.raw"));
            raw_files.Add(("", "Ex1_20230630_HEK_noFAIMS_OT50_2.raw"));
            raw_files.Add(("", "Ex1_20230701_HEK_noFAIMS_OT50_1.raw"));
            raw_files.Add(("", "Ex1_20230703_HEK_noFAIMS_OT50_1.raw"));
            raw_files.Add(("", "Ex1_20230707_HEK_noFAIMS_OT50_1.raw"));
            raw_files.Add(("", "Ex1_20230719_HEK_noFAIMS_OT50_1.raw"));
            raw_files.Add(("", "Ex1_20230719_HEK_noFAIMS_OT50_2.raw"));
            raw_files.Add(("", "Ex1_20230727_HEK_noFAIMS_OT50_1.raw"));
            raw_files.Add(("", "Ex1_20230727_HEK_noFAIMS_OT50_1_20230730184232.raw"));
            raw_files.Add(("", "Ex1_20230727_HEK_noFAIMS_OT50_2.raw"));
            raw_files.Add(("", "Ex1_20230728_HEK_noFAIMS_OT50.raw"));
            raw_files.Add(("", "Ex1_20230731_HEK_noFAIMS_OT50.raw"));
            raw_files.Add(("", "Ex1_20230804_HEK_noFAIMS_OT50.raw"));
            raw_files.Add(("", "Ex1_20230804_HEK_noFAIMS_OT50_20230807180638.raw"));
            raw_files.Add(("", "Ex1_20230809_HEK_noFAIMS_OT1ug.raw"));
            raw_files.Add(("", "Ex1_20230809_HEK_noFAIMS_OT1ug2.raw"));
            raw_files.Add(("", "Ex1_20230809_HEK_noFAIMS_OT50_1.raw"));
            raw_files.Add(("", "Ex1_20230809_HEK_noFAIMS_OT50_2.raw"));
            raw_files.Add(("", "Ex1_20230821_HEK_noFAIMS_OT50.raw"));
            raw_files.Add(("", "Ex1_20230828_HEK_noFAIMS_OT50.raw"));
            */

            #endregion

            Connection.Refresh_time = int.MinValue;
            List<string> allRaws = Directory.GetFiles(
                                rawsDirectory,
                                "*.raw",
                                SearchOption.AllDirectories).ToList();

            StringBuilder sb_error = new();
            foreach (var item in raw_files)
            {
                int it_index = -1;
                int ot_index = -1;

                if (!String.IsNullOrEmpty(item.IT_file))
                    it_index = allRaws.FindIndex(a => a.Contains(item.IT_file));

                if (!String.IsNullOrEmpty(item.OT_file))
                    ot_index = allRaws.FindIndex(a => a.Contains(item.OT_file));

                if (it_index > -1 || ot_index > -1)
                {
                    try
                    {
                        string rawFile = "";
                        ProcessRun it = null;
                        ProcessRun ot = null;
                        if (it_index != -1)
                        {
                            rawFile = allRaws[it_index];
                            it = ProcessRaw.RunRawFile(rawFile);
                        }
                        if (ot_index != -1)
                        {
                            rawFile = allRaws[ot_index];
                            ot = ProcessRaw.RunRawFile(rawFile);
                        }
                        string error = string.Empty;
                        CreateOrUpdateRun(ot, it, out error, null, false);
                        Connection.ReadInfo();
                    }
                    catch (Exception e)
                    {
                        sb_error.AppendLine("File: " + item + "\n" + e.Message);
                    }
                }
            }

            if (!String.IsNullOrEmpty(sb_error.ToString()))
                System.Windows.MessageBox.Show(
                                sb_error.ToString(),
                                "Q2C :: Warning",
                                (System.Windows.MessageBoxButton)MessageBoxButtons.OK,
                                (System.Windows.MessageBoxImage)MessageBoxIcon.Warning);
            else
                System.Windows.MessageBox.Show(
                                "Process has been done!",
                                "Q2C :: Information",
                                (System.Windows.MessageBoxButton)MessageBoxButtons.OK,
                                (System.Windows.MessageBoxImage)MessageBoxIcon.Information);

            Connection.Refresh_time = 0;

        }

        private async void ButtonConfirm_Click(object sender, RoutedEventArgs e)
        {
            if (CheckFields()) return;

            ////### TEMP ###
            //RunInBatch(System.IO.Path.GetDirectoryName(TextFileOT.Text));
            //return;
            //// ##########

            //Set a -infinity number to stop the refreshing of datagrids
            Connection.Refresh_time = int.MinValue;
            string error_msg = string.Empty;
            if (_run != null)
            {
                //Update run
                _run.Comments = TextComments.Text;
                if (CreateOrUpdateRun(null, null, out error_msg, _run))
                {
                    Connection.ReadInfo();
                    ((WindowRun)_window).UpdateRunDataGrid();
                    _window.Close();
                }
            }
            else
            {
                //Add run
                ProcessRun ot = null;
                ProcessRun it = null;
                ProcessRun process = null;

                if (!String.IsNullOrEmpty(TextFile.Text))
                {
                    UCWaitScreen waitScreen = new UCWaitScreen("Please Wait...", "Processing Raw file(s)...");
                    Grid.SetRow(waitScreen, 0);
                    Grid.SetRowSpan(waitScreen, 2);
                    waitScreen.Margin = new Thickness(0, 0, 0, 0);
                    MainUCRun.Children.Add(waitScreen);

                    foreach (string file in _selected_files)
                    {
                        try
                        {
                            await Task.Run(() => process = ProcessRaw.RunRawFile(file));

                            if (process != null)
                            {
                                if (process.IsOT)
                                {
                                    if (ot == null)
                                    {
                                        ot = process.ToClone();
                                        process = null;
                                    }
                                    else
                                        throw new Exception("Two OT raw files have been selected.");
                                }
                                else
                                {
                                    if (it == null)
                                    {
                                        it = process.ToClone();
                                        process = null;
                                    }
                                    else
                                        throw new Exception("Two IT raw files have been selected.");
                                }
                            }
                            else
                                throw new Exception("Failed to process raw file.");
                        }
                        catch (Exception e2)
                        {
                            error_msg += e2.Message;
                        }
                    }

                    if (String.IsNullOrEmpty(error_msg) &&
                            CreateOrUpdateRun(ot, it, out error_msg))
                    {
                        Connection.ReadInfo();
                        ((WindowRun)_window).UpdateRunDataGrid();
                        _window.Close();
                    }

                    MainUCRun.Children.Remove(waitScreen);
                }
            }
            if (!String.IsNullOrEmpty(error_msg))
            {
                System.Windows.MessageBox.Show(
                        "Failed to add/update the run!\n" + error_msg,
                        "Q2C :: Error",
                        (System.Windows.MessageBoxButton)MessageBoxButtons.OK,
                        (System.Windows.MessageBoxImage)MessageBoxIcon.Error);
            }
            //Restart the count to refresh datagrids
            Connection.Refresh_time = 0;
        }

        private bool CheckFields()
        {
            if (String.IsNullOrEmpty(TextOperator.Text))
            {
                System.Windows.MessageBox.Show(
                            "'Operator' field is empty!",
                            "Q2C :: Warning",
                            (System.Windows.MessageBoxButton)MessageBoxButtons.OK,
                            (System.Windows.MessageBoxImage)MessageBoxIcon.Warning);

                TextOperator.Focus();
                return true;
            }

            if (String.IsNullOrEmpty(TextFile.Text))
            {
                System.Windows.MessageBox.Show(
                            "'OT/IT Raw File(s)' field is empty!",
                            "Q2C :: Warning",
                            (System.Windows.MessageBoxButton)MessageBoxButtons.OK,
                            (System.Windows.MessageBoxImage)MessageBoxIcon.Warning);

                TextFile.Focus();
                return true;
            }

            return false;
        }

        private bool CreateOrUpdateRun(ProcessRun ot, ProcessRun it, out string errorMsg, Run _current_run = null, bool showInfo = true)
        {
            errorMsg = string.Empty;
            if (_current_run == null && ot == null && it == null)
            {
                errorMsg = "Unable to read OT or/and IT raw file(s).";
                return false;
            }

            bool isOT_correct = true;
            bool isIT_correct = true;
            if (_current_run == null)
                _current_run = GetRunFields(ot, it, out isOT_correct, out isIT_correct);

            string warningMsg = string.Empty;
            bool isWarningMsg = false;

            if (isOT_correct == true && isIT_correct == true)
            {
                isWarningMsg = !Management.CheckIDRatio(_current_run, _machine, out warningMsg);
                Connection.AddOrUpdateRun(_current_run, _machine);
            }
            else
            {
                if (isOT_correct == false)
                {
                    _current_run = GetRunFields(null, ot, out isOT_correct, out isIT_correct);
                    isWarningMsg = !Management.CheckIDRatio(_current_run, _machine, out warningMsg);
                    Connection.AddOrUpdateRun(_current_run, _machine);
                }

                if (isIT_correct == false)
                {
                    _current_run = GetRunFields(it, null, out isOT_correct, out isIT_correct);
                    isWarningMsg = !Management.CheckIDRatio(_current_run, _machine, out warningMsg);
                    Connection.AddOrUpdateRun(_current_run, _machine);
                }
            }

            if (showInfo)
            {
                if (_window.Title.Contains("Add"))
                {
                    if (isWarningMsg)
                        System.Windows.MessageBox.Show(
                            "Run has been added successfully!\n\nWARN: " +
                                    warningMsg,
                                    "Q2C :: Warning",
                                    (System.Windows.MessageBoxButton)MessageBoxButtons.OK,
                                    (System.Windows.MessageBoxImage)MessageBoxIcon.Warning);
                    else
                        System.Windows.MessageBox.Show(
                                    "Run has been added successfully!",
                                    "Q2C :: Information",
                                    (System.Windows.MessageBoxButton)MessageBoxButtons.OK,
                                    (System.Windows.MessageBoxImage)MessageBoxIcon.Information);
                }
                else
                {
                    System.Windows.MessageBox.Show(
                                "Run has been updated successfully!",
                                "Q2C :: Information",
                                (System.Windows.MessageBoxButton)MessageBoxButtons.OK,
                                (System.Windows.MessageBoxImage)MessageBoxIcon.Information);
                }
            }
            return true;
        }

        private Run GetRunFields(ProcessRun ot, ProcessRun it, out bool IsOT_correct, out bool IsIT_correct)
        {
            IsOT_correct = true;
            IsIT_correct = true;

            if (ot != null && ot.IsOT == false)
                IsOT_correct = false;

            if (it != null && it.IsOT == true)
                IsIT_correct = false;

            if (IsOT_correct == false || IsIT_correct == false)
                return new();

            if (ot == null) ot = new();
            if (it == null) it = new();

            string taskDateOT_str = ot.CreateDate.ToString("dd") + "/" + ot.CreateDate.ToString("MM") + "/" + ot.CreateDate.ToString("yyyy") + " " + ot.CreateDate.ToString("HH:mm:ss");
            string taskDateIT_str = it.CreateDate.ToString("dd") + "/" + it.CreateDate.ToString("MM") + "/" + it.CreateDate.ToString("yyyy") + " " + it.CreateDate.ToString("HH:mm:ss");
            string rawFileOT = ot != null && ot.RawFile != null ? ot.RawFile : "";
            string rawFileIT = it != null && it.RawFile != null ? it.RawFile : "";

            var runOT = new SubRun(taskDateOT_str,
                ot != null ? ot.IsFAIMS : false,
                ot != null ? ot.MS1Intensity : 0,
                ot != null ? ot.MS2Intensity : 0,
                ot != null ? ot.ProteinGroups : 0,
                ot != null ? ot.PeptideGroups : 0,
                ot != null ? ot.PSMs : 0,
                ot != null ? ot.MSMSCount : 0,
                ot != null ? ot.IDRatio * 100 : 0,
                ot != null ? ot.MassErrorPPM : "0 to 0",
                ot != null ? Math.Round(ot.MassErrorMedian_PPM, 2) : 0,
                ot != null ? ot.XreaMean : 0,
                ot != null ? ot.Xreas : new(),
                String.IsNullOrWhiteSpace(rawFileOT),
                rawFileOT,
                -1,
                ot != null ? ot.MostAbundantPepts : new());

            var runIT = new SubRun(taskDateIT_str,
                it != null ? it.IsFAIMS : false,
                it != null ? it.MS1Intensity : 0,
                it != null ? it.MS2Intensity : 0,
                it != null ? it.ProteinGroups : 0,
                it != null ? it.PeptideGroups : 0,
                it != null ? it.PSMs : 0,
                it != null ? it.MSMSCount : 0,
                it != null ? it.IDRatio * 100 : 0,
                it != null ? it.MassErrorPPM : "0 to 0",
                it != null ? Math.Round(it.MassErrorMedian_PPM, 2) : 0,
                it != null ? it.XreaMean : 0,
                it != null ? it.Xreas : new(),
                String.IsNullOrWhiteSpace(rawFileIT),
                rawFileIT,
                -1,
                it != null ? it.MostAbundantPepts : new());

            return new Run(-1,
                runOT,
                runIT,
                TextOperator.Text,
                TextComments.Text,
                TextColumnLotNumber.Text,
                Management.GetComputerUser(),
                Management.InfoStatus.Active);
        }

        private void ButtonCancel_Click(object sender, RoutedEventArgs e)
        {
            if (_window != null)
                _window.Close();
        }
    }
}
