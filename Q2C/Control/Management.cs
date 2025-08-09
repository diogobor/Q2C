using Accord.Math.Distances;
using Accord.Statistics.Running;
using Google.Apis.Logging;
using Microsoft.Win32;
using Q2C.Control.Database;
using Q2C.Model;
using Q2C.Properties;
using Q2C.Viewer.Setup;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
using System.Security.Policy;
using System.Security.RightsManagement;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Windows.Forms;

namespace Q2C.Control
{
    public static class Management
    {
        public static List<Project> Projects = new();
        public static (string creation_date, string app_version) InfoApp = new();
        public static Dictionary<string, (List<MachineQueue> queue, List<Run> evaluation, List<MachineLog> log)> Machines_Properties = new();
        public static List<Model.Machine> Machines = new();
        public static List<User> Users = new();

        public enum InfoStatus
        {
            Active,
            Deleted,
            Undefined
        }
        public static string GetInfoStatus(InfoStatus status)
        {
            switch (status)
            {
                case InfoStatus.Active:
                    return "Active";
                case InfoStatus.Deleted:
                    return "Deleted";
                default: return "Deleted";
            }
        }

        public static InfoStatus GetInfoStatusStr(string status)
        {
            switch (status)
            {
                case "Active":
                    return InfoStatus.Active;
                case "Deleted":
                    return InfoStatus.Deleted;
                default: return InfoStatus.Deleted;
            }
        }

        #region Database
        public static bool HasDatabase()
        {
            string _database = Settings.Default.Database;
            try
            {
                if (String.IsNullOrWhiteSpace(_database))
                {
                    //Get info from regedit
                    _database = GetDatabaseFromRegEdit();
                    Settings.Default.Database = _database;
                    Settings.Default.Save();
                }
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
        public static Model.Database GetDatabase()
        {
            try
            {
                string _database = Settings.Default.Database;
                if (String.IsNullOrWhiteSpace(_database))
                {
                    //Get info from regedit
                    _database = GetDatabaseFromRegEdit();
                }

                Model.Database current_db = Q2C.Control.FileManagement.Serializer.FromJson<Model.Database>(_database);

                if (HasOnlineInfoInDBString(_database))
                    return current_db;
                else
                {
                    if (!String.IsNullOrWhiteSpace(current_db.SpreadsheetID) &&
                    !String.IsNullOrWhiteSpace(current_db.GoogleClientID) &&
                    !String.IsNullOrWhiteSpace(current_db.GoogleClientSecret))
                        current_db.IsOnline = true;
                    return current_db;
                }
            }
            catch (Exception)
            {
                return null;
            }
        }
        private static bool HasOnlineInfoInDBString(string regedit_info)
        {
            if (regedit_info.ToLower().Contains("isonline"))
                return true;
            return false;
        }

        public static void TransferOnlineToOffline()
        {
            string tmp_file_name = GetTmpDir() + Guid.NewGuid() + ".xlsx";

            Connection.TransferOnlineToOffline(tmp_file_name, GetDatabase());
        }

        public static void SetDatabase(Model.Database database)
        {
            Settings.Default.Database = Q2C.Control.FileManagement.Serializer.ToJSON(database);
            Settings.Default.Save();

            // Save database in RegEdit
            RegisterQ2C();
        }
        public static bool ExportDatabase(string fileName)
        {
            return Connection.ExportSpreadSheet(fileName);
        }
        private static string GetDatabaseFromRegEdit()
        {
            // Get the registry key for Q2C
            using (RegistryKey? command_key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Classes\\Applications\\Q2C.exe\\shell\\open\\command", false))
            {
                if (command_key == null) throw new Exception();
                return command_key.GetValue("Database") as string;
            }
        }
        private static void RegisterQ2C()
        {
            try
            {
                string OpenWith = System.Windows.Forms.Application.ExecutablePath;
                string ExecutableName = "Q2C.exe";
                using (RegistryKey? User_Classes = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Classes\\", true))
                using (RegistryKey? User_Classes_Applications = User_Classes?.CreateSubKey("Applications"))
                using (RegistryKey? User_Classes_Applications_Exe = User_Classes_Applications?.CreateSubKey(ExecutableName))
                using (RegistryKey? User_Application_Command = User_Classes_Applications_Exe?.CreateSubKey("shell").CreateSubKey("open").CreateSubKey("command"))
                {
                    string logo_path = System.AppDomain.CurrentDomain.BaseDirectory + "icons\\q2c_icon.ico";
                    User_Application_Command?.SetValue("", "\"" + OpenWith + "\"" + " \"%1\"");
                    User_Application_Command.SetValue("Database", Settings.Default.Database);
                    User_Classes_Applications_Exe?.CreateSubKey("DefaultIcon").SetValue("", logo_path);
                }
                SHChangeNotify(0x08000000, 0x0000, IntPtr.Zero, IntPtr.Zero);
            }
            catch (Exception) { }
        }
        [DllImport("Shell32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern void SHChangeNotify(uint wEventId, uint uFlags, IntPtr dwItem1, IntPtr dwItem2);

        public static void ResetDatabase()
        {
            Settings.Default.Database = "";
            Settings.Default.Save();

            RemoveRegisterQ2C();
        }
        private static void RemoveRegisterQ2C()
        {
            try
            {
                using (RegistryKey? command_key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Classes\\Applications\\Q2C.exe", true))
                using (RegistryKey? command_key_default_icon = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Classes\\Applications\\Q2C.exe\\DefaultIcon", true))
                using (RegistryKey? command_key_command = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Classes\\Applications\\Q2C.exe\\shell\\open\\command", true))
                {
                    if (command_key != null && command_key_default_icon != null && command_key_command != null)
                    {
                        using (RegistryKey? command_applications_key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Classes\\Applications", true))
                        {
                            if (command_applications_key != null)
                            {
                                command_key_default_icon.DeleteValue("");
                                command_key_command.DeleteValue("");
                                command_key_command.DeleteValue("Database");
                                command_key.DeleteSubKey("DefaultIcon");

                                command_key.DeleteSubKey("shell\\open\\command");
                                command_key.DeleteSubKey("shell\\open");
                                command_key.DeleteSubKey("shell");
                                command_applications_key.DeleteSubKey("Q2C.exe");
                            }
                        }
                    }
                }
                SHChangeNotify(0x08000000, 0x0000, IntPtr.Zero, IntPtr.Zero);
            }
            catch (Exception) { }
        }
        #endregion

        #region Machine
        public static Model.Machine? GetMachine(string name)
        {
            return GetMachines().Where(a => a.Name.Equals(name)).FirstOrDefault();
        }
        public static List<Model.Machine> GetMachines()
        {
            return Machines != null ? Machines.Where(a => a.InfoStatus == InfoStatus.Active).ToList() : new();
        }
        public static bool HasMachine()
        {
            if (Machines.Count == 0 ||
                Machines.Where(a => a.InfoStatus == InfoStatus.Active).Count() == 0)
                return false;
            return true;
        }
        public static Dictionary<string, (bool, bool)> CheckMachinesCalibration()
        {
            Dictionary<string, (bool, bool)> machines_to_calibrate = new();
            DateTime current_time = DateTime.Now;
            bool needCalibration = false;
            bool needFullCalibration = false;

            foreach (var machine in GetMachines())
            {
                needCalibration = false;
                needFullCalibration = false;

                (List<MachineQueue> queue, List<Run> evaluation, List<MachineLog> log) current_properties;
                Machines_Properties.TryGetValue(machine.Name, out current_properties);

                if (current_properties.queue == null && current_properties.evaluation == null && current_properties.log == null) return null;

                current_properties.log.Sort((a, b) => b.RegistrationDate.CompareTo(a.RegistrationDate));

                MachineLog? last_fully_calibrated_log = current_properties.log.Where(a => a.IsFullyCalibrated && a.InfoStatus == InfoStatus.Active).FirstOrDefault();
                DateTime last_full_calibrated_time = new DateTime();
                if (last_fully_calibrated_log != null)
                {
                    if (last_fully_calibrated_log.IsFullyCalibrated == true)
                    {
                        last_full_calibrated_time = GetNewDateTime(last_fully_calibrated_log.RegistrationDate, machine.FullCalibrationTime);
                        if (current_time > last_full_calibrated_time)
                            needFullCalibration = true;
                    }
                }
                else
                    needFullCalibration = true;

                bool checkPartialCalibration = false;
                MachineLog? last_calibrated_log = current_properties.log.Where(a => a.IsCalibrated && a.InfoStatus == InfoStatus.Active).FirstOrDefault();
                if (last_calibrated_log != null)
                {
                    if (last_calibrated_log.IsCalibrated == true)
                    {
                        DateTime last_calibrated_time = GetNewDateTime(last_calibrated_log.RegistrationDate, machine.CalibrationTime);
                        if (current_time > last_calibrated_time)
                            checkPartialCalibration = true;
                    }
                }
                else
                    checkPartialCalibration = true;

                if (checkPartialCalibration)
                {
                    if (last_fully_calibrated_log != null)
                    {
                        last_full_calibrated_time = GetNewDateTime(last_fully_calibrated_log.RegistrationDate, machine.CalibrationTime);
                        if (current_time > last_full_calibrated_time)
                            needCalibration = true;
                    }
                }

                machines_to_calibrate.Add(machine.Name, (needCalibration, needFullCalibration));
            }
            return machines_to_calibrate;
        }
        private static DateTime GetNewDateTime(DateTime log_time, string calibration)
        {
            string[] cols_calibration_time = Regex.Split(calibration, "_");
            DateTime new_log_time = log_time;

            switch (cols_calibration_time[1])
            {
                case "Day(s)":
                    new_log_time = new_log_time.AddDays(Convert.ToDouble(cols_calibration_time[0]));
                    break;
                case "Week(s)":
                    new_log_time = new_log_time.AddDays((Convert.ToDouble(cols_calibration_time[0]) * 7));
                    break;
                case "Month(s)":
                    new_log_time = new_log_time.AddMonths(Convert.ToInt32(cols_calibration_time[0]));
                    break;
                case "Year(s)":
                    new_log_time = new_log_time.AddYears(Convert.ToInt32(cols_calibration_time[0]));
                    break;
            }
            return new_log_time;
        }
        #endregion

        #region User
        public static bool IsValidUser(string user)
        {
            if (!String.IsNullOrEmpty(user))
            {
                if (GetUsers().Select(a => a.Name).Contains(user))
                    return true;
            }
            return false;
        }
        public static bool HasUser()
        {
            if (GetUsers().Count == 0)
                return false;
            //if (String.IsNullOrEmpty(Settings.Default.Users))
            //    return false;

            //Users = Q2C.Control.FileManagement.Serializer.FromJson<List<User>>(Settings.Default.Users, true);
            return true;
        }
        public static List<User> GetUsers()
        {
            return Users != null ? Users.Where(a => a.InfoStatus == Management.InfoStatus.Active).ToList() : new();
        }
        #endregion

        private static bool _checkIDRatio(Run run, List<Run> runs, out string errorMsg)
        {
            errorMsg = string.Empty;

            if (runs == null ||
                run == null)
                return false;

            if (runs.Count < 6) return true;

            double q1 = 0;
            double q3 = 0;
            (double lower_bound, double upper_bound) = (0, 0);

            // OT
            if (run.OT.Exclude == false)
            {
                List<double> selected_idratios = runs.Where(a => a.OT.Exclude == false).Select(a => a.OT.IDRatio).ToList();
                selected_idratios.Add(run.OT.IDRatio);

                q1 = Util.Util.Quartile(selected_idratios, 0.25);
                q3 = Util.Util.Quartile(selected_idratios, 0.75);

                (lower_bound, upper_bound) = Util.Util.OutlierBounds(q1, q3);
                if (run.OT.Exclude == false && run.OT.IDRatio < lower_bound)
                {
                    run.OT.Exclude = true;
                    errorMsg += "OT ID ratio is out of the lower bound.";
                    return false;
                }
                else if (run.OT.Exclude == false && run.OT.IDRatio > upper_bound)
                {
                    run.OT.Exclude = true;
                    errorMsg += "OT ID ratio is out of the upper bound.";
                    return false;
                }
            }

            // IT 
            if (run.IT.Exclude == false)
            {
                List<double> selected_idratios = runs.Where(a => a.IT.Exclude == false).Select(a => a.IT.IDRatio).ToList();
                selected_idratios.Add(run.IT.IDRatio);
                q1 = Util.Util.Quartile(selected_idratios, 0.25);
                q3 = Util.Util.Quartile(selected_idratios, 0.75);

                (lower_bound, upper_bound) = Util.Util.OutlierBounds(q1, q3);
                if (run.IT.Exclude == false && run.IT.IDRatio < lower_bound)
                {
                    run.IT.Exclude = true;
                    errorMsg += "IT ID ratio is out of the lower bound.";
                    return false;
                }
                else if (run.IT.Exclude == false && run.IT.IDRatio > upper_bound)
                {
                    run.IT.Exclude = true;
                    errorMsg += "IT ID ratio is out of the upper bound.";
                    return false;
                }
            }

            return true;
        }
        public static bool CheckIDRatio(Run run, string machine, out string errorMsg)
        {
            errorMsg = string.Empty;
            (List<MachineQueue> queue, List<Run> evaluation, List<MachineLog> log) prop;
            Machines_Properties.TryGetValue(machine, out prop);
            return _checkIDRatio(run, prop.evaluation.Where(a => a.InfoStatus == InfoStatus.Active).ToList(), out errorMsg);
        }
        public static void CreateNewSpreadsheet(Model.Database database)
        {
            bool isValid = Connection.Init(database);
            if (isValid == false) return;

            string fileID = Connection.CreateNewSpreadsheet();

            Connection.SpreadsheetId = fileID;
            Connection.CreateNewTabIntoSpreadsheet("Info", Connection.GetCategories("Info"));
            UpdateDBAppInfo();
            Connection.CreateNewTabIntoSpreadsheet("Users", Connection.GetCategories("Users"));
            Connection.RemoveTabInSpreadsheet(0);
            database.SpreadsheetID = fileID;
            Management.SetDatabase(database);
        }

        private static string[] GetInfoApp()
        {
            string[] values = new string[2];
            DateTime registrationDate = DateTime.Now;
            values[0] = registrationDate.ToString("dd") + "/" + registrationDate.ToString("MM") + "/" + registrationDate.ToString("yyyy") + " " + registrationDate.ToString("HH:mm:ss");
            values[1] = Util.Util.GetAppVersion();
            return values;
        }
        private static void UpdateDBAppInfo()
        {
            Connection.UpdateInfo(GetInfoApp());
        }
        public static int CheckAppDBVersion()
        {
            try
            {
                Version app_version = new Version(Util.Util.GetAppVersion());
                Version db_version = new Version(InfoApp.app_version);

                if (db_version < app_version)
                    UpdateDBAppInfo();
                else if (db_version > app_version)
                {
                    Connection.RevokeConnection();
                    return 1;
                }
                return 0;
            }
            catch (System.ArgumentNullException e)
            {
                if (e.Message.Equals("Value cannot be null. (Parameter 'input')"))
                {
                    //database version is null
                    UpdateDBAppInfo();
                    Connection.ReadInfo(true);
                    return CheckAppDBVersion();
                }
                return 1;
            }
            catch (Exception)
            {
                throw;
            }
        }
        public static void CreateFastaFiles(List<FastaFile> fastaFiles)
        {
            if (fastaFiles.Count == 0) return;

            //Check whether Users\{user}\.q2c\FastaFiles exists
            if (!Directory.Exists(Util.Util.FastaFile_Folder))
                Directory.CreateDirectory(Util.Util.FastaFile_Folder);

            foreach (var fasta in fastaFiles)
            {
                //Check whether the file exist in the source path
                if (File.Exists(fasta.Path))
                {
                    //If the file does not exist in Users\{user}\.q2c\FastaFiles, create it
                    string new_fasta_path = Util.Util.FastaFile_Folder + System.IO.Path.GetFileNameWithoutExtension(fasta.Path) + ".T-R";

                    #region create T-R file
                    if (!File.Exists(new_fasta_path))
                    {
                        var fp = ProcessDatabase.AssembleFasta(fasta.Path);
                        fp.SaveDB(new_fasta_path);
                        //File.Copy(fasta.Path, new_fasta_path);
                        fasta.Path = new_fasta_path;
                    }
                    #endregion
                }
            }
        }
        public static string GetComputerUser()
        {
            return Environment.UserName.ToLower().ToString();
        }
        public static void CheckSystemLanguage()
        {
            #region Setting Language
            Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                if (!Microsoft.Win32.Registry.GetValue(@"HKEY_CURRENT_USER\Control Panel\International", "LocaleName", null).ToString().ToLower().Equals("en-us"))
                {
                    DialogResult answer = System.Windows.Forms.MessageBox.Show("The system default language is not English. Do you want to change it to English ?\nThis tool works if only the system default language is English.", "Q2C :: Warning", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                    if (answer == System.Windows.Forms.DialogResult.Yes)
                    {
                        Microsoft.Win32.Registry.SetValue(@"HKEY_CURRENT_USER\Control Panel\International", "Locale", "00000409");
                        Microsoft.Win32.Registry.SetValue(@"HKEY_CURRENT_USER\Control Panel\International", "LocaleName", "en-US");
                        Microsoft.Win32.Registry.SetValue(@"HKEY_CURRENT_USER\Control Panel\International", "sCountry", "United States");
                        Microsoft.Win32.Registry.SetValue(@"HKEY_CURRENT_USER\Control Panel\International", "sCurrency", "$");
                        Microsoft.Win32.Registry.SetValue(@"HKEY_CURRENT_USER\Control Panel\International", "sDate", "/");
                        Microsoft.Win32.Registry.SetValue(@"HKEY_CURRENT_USER\Control Panel\International", "sDecimal", ".");
                        Microsoft.Win32.Registry.SetValue(@"HKEY_CURRENT_USER\Control Panel\International", "sGrouping", "3;0");
                        Microsoft.Win32.Registry.SetValue(@"HKEY_CURRENT_USER\Control Panel\International", "sLanguage", "ENU");
                        Microsoft.Win32.Registry.SetValue(@"HKEY_CURRENT_USER\Control Panel\International", "sList", ",");
                        Microsoft.Win32.Registry.SetValue(@"HKEY_CURRENT_USER\Control Panel\International", "sLongDate", "dddd, MMMM dd, yyyy");
                        Microsoft.Win32.Registry.SetValue(@"HKEY_CURRENT_USER\Control Panel\International", "sMonDecimalSep", ".");
                        Microsoft.Win32.Registry.SetValue(@"HKEY_CURRENT_USER\Control Panel\International", "sMonGrouping", "3;0");
                        Microsoft.Win32.Registry.SetValue(@"HKEY_CURRENT_USER\Control Panel\International", "sMonThousandSep", ",");
                        Microsoft.Win32.Registry.SetValue(@"HKEY_CURRENT_USER\Control Panel\International", "sNativeDigits", "0123456789");
                        Microsoft.Win32.Registry.SetValue(@"HKEY_CURRENT_USER\Control Panel\International", "sNegativeSign", "-");
                        Microsoft.Win32.Registry.SetValue(@"HKEY_CURRENT_USER\Control Panel\International", "sPositiveSign", "");
                        Microsoft.Win32.Registry.SetValue(@"HKEY_CURRENT_USER\Control Panel\International", "sShortDate", "M/d/yyyy");
                        Microsoft.Win32.Registry.SetValue(@"HKEY_CURRENT_USER\Control Panel\International", "sThousand", ",");
                        Microsoft.Win32.Registry.SetValue(@"HKEY_CURRENT_USER\Control Panel\International", "sTime", ":");
                        Microsoft.Win32.Registry.SetValue(@"HKEY_CURRENT_USER\Control Panel\International", "sTimeFormat", "h:mm:ss tt");
                        Microsoft.Win32.Registry.SetValue(@"HKEY_CURRENT_USER\Control Panel\International", "sShortTime", "h:mm tt");
                        Microsoft.Win32.Registry.SetValue(@"HKEY_CURRENT_USER\Control Panel\International", "sYearMonth", "MMMM, yyyy");
                        Microsoft.Win32.Registry.SetValue(@"HKEY_CURRENT_USER\Control Panel\International", "iCalendarType", "1");
                        Microsoft.Win32.Registry.SetValue(@"HKEY_CURRENT_USER\Control Panel\International", "iCountry", "1");
                        Microsoft.Win32.Registry.SetValue(@"HKEY_CURRENT_USER\Control Panel\International", "iCurrDigits", "2");
                        Microsoft.Win32.Registry.SetValue(@"HKEY_CURRENT_USER\Control Panel\International", "iCurrency", "0");
                        Microsoft.Win32.Registry.SetValue(@"HKEY_CURRENT_USER\Control Panel\International", "iDate", "0");
                        Microsoft.Win32.Registry.SetValue(@"HKEY_CURRENT_USER\Control Panel\International", "iDigits", "2");
                        Microsoft.Win32.Registry.SetValue(@"HKEY_CURRENT_USER\Control Panel\International", "NumShape", "1");
                        Microsoft.Win32.Registry.SetValue(@"HKEY_CURRENT_USER\Control Panel\International", "iFirstDayOfWeek", "6");
                        Microsoft.Win32.Registry.SetValue(@"HKEY_CURRENT_USER\Control Panel\International", "iFirstWeekOfYear", "0");
                        Microsoft.Win32.Registry.SetValue(@"HKEY_CURRENT_USER\Control Panel\International", "iLZero", "1");
                        Microsoft.Win32.Registry.SetValue(@"HKEY_CURRENT_USER\Control Panel\International", "iMeasure", "1");
                        Microsoft.Win32.Registry.SetValue(@"HKEY_CURRENT_USER\Control Panel\International", "iNegCurr", "0");
                        Microsoft.Win32.Registry.SetValue(@"HKEY_CURRENT_USER\Control Panel\International", "iNegNumber", "1");
                        Microsoft.Win32.Registry.SetValue(@"HKEY_CURRENT_USER\Control Panel\International", "iPaperSize", "1");
                        Microsoft.Win32.Registry.SetValue(@"HKEY_CURRENT_USER\Control Panel\International", "iTime", "0");
                        Microsoft.Win32.Registry.SetValue(@"HKEY_CURRENT_USER\Control Panel\International", "iTimePrefix", "0");
                        Microsoft.Win32.Registry.SetValue(@"HKEY_CURRENT_USER\Control Panel\International", "iTLZero", "0");
                        System.Windows.Forms.MessageBox.Show("Q2C will be restarted!", "Q2C :: Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        System.Windows.Forms.Application.Restart();
                        System.Windows.Application.Current.Shutdown();
                    }
                    else
                    {
                        System.Windows.Forms.MessageBox.Show("Q2C will be closed!", "Q2C :: ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        System.Environment.Exit(0);
                        System.Windows.Forms.Application.Exit();
                    }
                }
            }
            #endregion
        }

        private static bool CleanTmpFiles()
        {
            try
            {
                DirectoryInfo dr = new DirectoryInfo(Path.GetTempPath() + "\\Q2C_auxiliar_files");
                if (dr.Exists)
                {
                    string directoryPath = dr.FullName;

                    // Delete *.pdf files
                    DeleteFiles(directoryPath, "*.pdf");
                }
                return true;
            }
            catch (Exception)
            {
                Console.WriteLine("ERROR: Unable to delete files.");
                return false;
            }
        }
        private static void DeleteFiles(string directoryPath, string searchPattern)
        {
            try
            {
                // Get files matching the search pattern
                string[] files = Directory.GetFiles(directoryPath, searchPattern);
                // Delete each file
                foreach (string file in files)
                    File.Delete(file);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }
        }

        public static void OpenTechnicalReportFile(string file_googleID, string original_file_name)
        {
            bool isValid = true;
            string file_name = string.Empty;
            if (Connection.IsOnline)
            {
                CleanTmpFiles();

                file_name = GetTmpDir() + Path.GetFileNameWithoutExtension(original_file_name) + "_" + Guid.NewGuid() + ".pdf";
                isValid = Connection.DownloadFile(file_googleID, file_name);
            }
            else
                file_name = original_file_name;

            if (isValid)
                OpenTechnicalReportFile(file_name);

        }
        internal static string GetTmpDir()
        {
            DirectoryInfo dr = new DirectoryInfo(Path.GetTempPath() + "\\Q2C_auxiliar_files\\");
            if (!dr.Exists)
                dr.Create();
            return dr.FullName;
        }
        private static void OpenTechnicalReportFile(string fileName)
        {
            try
            {
                System.Diagnostics.Process.Start(new ProcessStartInfo
                {
                    FileName = fileName,
                    UseShellExecute = true,
                });
            }
            catch (Exception)
            {
                System.Windows.MessageBox.Show(
                        "Visit the Q2C website for more usability information.",
                        "Q2C :: Read Me",
                        (System.Windows.MessageBoxButton)MessageBoxButtons.OK,
                        (System.Windows.MessageBoxImage)MessageBoxIcon.Information);
                throw;
            }
        }
    }
}
