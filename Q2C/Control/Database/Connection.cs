using Google.Apis.Auth.OAuth2;
using Google.Apis.Gmail.v1;
using Google.Apis.Logging;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Q2C.Model;
using Q2C.Viewer;
using Microsoft.VisualBasic.ApplicationServices;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Reflection.PortableExecutable;
using System.Windows.Documents;
using Run = Q2C.Model.Run;
using static alglib;
using User = Q2C.Model.User;
using Machine = Q2C.Model.Machine;
using Q2C.Properties;
using System.Security;
using Google.Apis.Drive.v3;
using Google.Apis.Drive.v3.Data;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ListView;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;
using System.Windows.Forms;
using Google.Apis.Download;
using Accord.Collections;
using System.IO;
using File = Google.Apis.Drive.v3.Data.File;
using Google.Apis.Util.Store;
using Json;
using Newtonsoft.Json.Linq;
using Q2C.Control.QualityControl;
using System.Diagnostics;
using Accord.Statistics.Distributions.Univariate;
using LiteDB;
using ClosedXML.Excel;
using Q2C.Util;
using Q2C.Viewer.Setup;
using static System.Net.WebRequestMethods;
using System.Data;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.Wordprocessing;
using Accord.Math.Distances;
using Newtonsoft.Json;
using Google.Apis.Auth.OAuth2.Responses;

namespace Q2C.Control.Database
{
    public class Connection
    {
        private static readonly string ApplicationName = "Google Sheet API .NET Quickstart";
        public static string SpreadsheetId;

        private static UserCredential UserCredential;

        public static bool IsOnline { get; set; }
        public static DriveService service_drive;
        public static SheetsService service_sheets;
        public static GmailService service_email;
        private CultureInfo cultureInfo = new CultureInfo("en-US");

        public static double Refresh_time = 0;

        private const int OFFSET_TIME_REFRESH_MILLISECONDS = 1000;

        public Connection()
        {
        }

        public static LiteDatabase CreateDatabase()
        {
            if (!Directory.Exists(Util.Util.DB_Folder))
                Directory.CreateDirectory(Util.Util.DB_Folder);
            string databasePath = Path.Combine(Util.Util.DB_Folder, "q2c_db.db");

            LiteDatabase db = new LiteDatabase(databasePath);
            return db;
        }

        public static bool Init(Q2C.Model.Database db)
        {
            if (db == null) return false;
            SpreadsheetId = db.SpreadsheetID;

            string[] Scopes = {
                DriveService.Scope.Drive,
                DriveService.Scope.DriveFile,
                GmailService.Scope.GmailSend,
                SheetsService.Scope.Spreadsheets };

            try
            {
                UserCredential = Login(db.GoogleClientID, db.GoogleClientSecret, Scopes);

                service_drive = new DriveService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = UserCredential,
                    ApplicationName = ApplicationName
                });

                service_email = new GmailService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = UserCredential,
                    ApplicationName = ApplicationName
                });

                service_sheets = new SheetsService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = UserCredential,
                    ApplicationName = ApplicationName
                });
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static void TransferOnlineToOffline(string fileName, Model.Database database)
        {
            ExportSpreadSheetOnline(fileName);

            #region transfer db
            string databasePath = Path.Combine(Util.Util.DB_Folder, "q2c_db.db");
            if (System.IO.File.Exists(databasePath))
                System.IO.File.Delete(databasePath);

            using (var db = CreateDatabase())
            {
                using (var workbook = new XLWorkbook(fileName))
                {
                    foreach (var worksheet in workbook.Worksheets)
                    {
                        string collectionName = worksheet.Name;  // Use sheet name as collection name
                        if (collectionName.StartsWith("Info")) continue;

                        if (collectionName.Equals("Users")) collectionName = "User";
                        else if (collectionName.Equals("Machines")) collectionName = "Machine";
                        else if (collectionName.Equals("Projects")) collectionName = "Project";

                        var collection = db.GetCollection(collectionName);

                        // Read the worksheet into a DataTable
                        DataTable dt = new DataTable(collectionName);
                        DataTable dtOT = new DataTable(collectionName + "OT");
                        DataTable dtIT = new DataTable(collectionName + "IT");
                        bool firstRow = true;
                        int rowIndex = 1;
                        foreach (var row in worksheet.RowsUsed())
                        {
                            switch (collectionName)
                            {
                                case "User":
                                    {
                                        if (firstRow)
                                        {
                                            rowIndex = 0;
                                            dt.Columns.Add("_id");
                                            dt.Columns.Add("RegistrationDateStr");
                                            dt.Columns.Add("RegistrationDate");
                                            dt.Columns.Add("Name");
                                            dt.Columns.Add("Category");
                                            dt.Columns.Add("_category");
                                            dt.Columns.Add("Email");
                                            dt.Columns.Add("InfoStatus");
                                            dt.Columns.Add("_infoStatus");
                                            // Create columns from first row
                                            firstRow = false;
                                        }
                                        else
                                        {
                                            // Add data rows
                                            var dataRow = dt.NewRow();
                                            dataRow["_id"] = rowIndex++;
                                            var iter = row.CellsUsed().GetEnumerator();
                                            iter.MoveNext();//Id
                                            iter.MoveNext();//RegistrationDate
                                            var cell = iter.Current;
                                            dataRow["RegistrationDateStr"] = cell.Value.ToString();
                                            dataRow["RegistrationDate"] = Util.Util.ConvertStrToDate(cell.Value.ToString());
                                            iter.MoveNext();
                                            cell = iter.Current;
                                            dataRow["Name"] = cell.Value.ToString();
                                            iter.MoveNext();
                                            cell = iter.Current;
                                            dataRow["Category"] = User.GetCategory(cell.Value.ToString());
                                            dataRow["_category"] = cell.Value.ToString();
                                            iter.MoveNext();
                                            cell = iter.Current;
                                            dataRow["Email"] = cell.Value.ToString();
                                            iter.MoveNext();
                                            cell = iter.Current;
                                            dataRow["InfoStatus"] = Management.GetInfoStatusStr(cell.Value.ToString());
                                            dataRow["_infoStatus"] = cell.Value.ToString();
                                            dt.Rows.Add(dataRow);
                                        }
                                        break;
                                    }
                                case "Machine":
                                    {
                                        if (firstRow)
                                        {
                                            rowIndex = 0;
                                            dt.Columns.Add("_id");
                                            dt.Columns.Add("RegistrationDateStr");
                                            dt.Columns.Add("RegistrationDate");
                                            dt.Columns.Add("Name");
                                            dt.Columns.Add("HasEvaluation");
                                            dt.Columns.Add("HasFAIMS");
                                            dt.Columns.Add("HasOT");
                                            dt.Columns.Add("HasIT");
                                            dt.Columns.Add("CalibrationTime");
                                            dt.Columns.Add("FullCalibrationTime");
                                            dt.Columns.Add("InfoStatus");
                                            dt.Columns.Add("_infoStatus");
                                            dt.Columns.Add("IntervalTime");
                                            // Create columns from first row
                                            firstRow = false;
                                        }
                                        else
                                        {
                                            // Add data rows
                                            var dataRow = dt.NewRow();
                                            dataRow["_id"] = rowIndex++;
                                            var iter = row.CellsUsed().GetEnumerator();
                                            iter.MoveNext();//Id
                                            iter.MoveNext();//RegistrationDate
                                            var cell = iter.Current;
                                            dataRow["RegistrationDateStr"] = cell.Value.ToString();
                                            dataRow["RegistrationDate"] = Util.Util.ConvertStrToDate(cell.Value.ToString());
                                            iter.MoveNext();
                                            cell = iter.Current;
                                            dataRow["Name"] = cell.Value.ToString();
                                            iter.MoveNext();
                                            cell = iter.Current;
                                            dataRow["HasEvaluation"] = Convert.ToBoolean(cell.Value.ToString());
                                            iter.MoveNext();
                                            cell = iter.Current;
                                            dataRow["HasFAIMS"] = Convert.ToBoolean(cell.Value.ToString());
                                            iter.MoveNext();
                                            cell = iter.Current;
                                            dataRow["HasOT"] = Convert.ToBoolean(cell.Value.ToString());
                                            iter.MoveNext();
                                            cell = iter.Current;
                                            dataRow["HasIT"] = Convert.ToBoolean(cell.Value.ToString());
                                            iter.MoveNext();
                                            cell = iter.Current;
                                            dataRow["CalibrationTime"] = cell.Value.ToString();
                                            iter.MoveNext();
                                            cell = iter.Current;
                                            dataRow["FullCalibrationTime"] = cell.Value.ToString();
                                            iter.MoveNext();
                                            cell = iter.Current;
                                            dataRow["InfoStatus"] = Management.GetInfoStatusStr(cell.Value.ToString());
                                            dataRow["_infoStatus"] = cell.Value.ToString();
                                            iter.MoveNext();
                                            cell = iter.Current;
                                            dataRow["IntervalTime"] = Convert.ToInt32(cell.Value.ToString());
                                            dt.Rows.Add(dataRow);
                                        }
                                        break;
                                    }
                                case "Project":
                                    {
                                        if (firstRow)
                                        {
                                            rowIndex = 0;
                                            dt.Columns.Add("_id");
                                            dt.Columns.Add("RegistrationDateStr");
                                            dt.Columns.Add("RegistrationDate");
                                            dt.Columns.Add("ProjectName");
                                            dt.Columns.Add("AmountMS");
                                            dt.Columns.Add("NumberOfSamples");
                                            dt.Columns.Add("Method");
                                            dt.Columns.Add("Machine");
                                            dt.Columns.Add("GetMachines");
                                            dt.Columns.Add("FAIMS");
                                            dt.Columns.Add("_faims");
                                            dt.Columns.Add("_receiveNotification");
                                            dt.Columns.Add("ReceiveNotification");
                                            dt.Columns.Add("Comments");
                                            dt.Columns.Add("AddedBy");
                                            dt.Columns.Add("Status");
                                            dt.Columns.Add("_status");
                                            dt.Columns.Add("InfoStatus");
                                            dt.Columns.Add("_infoStatus");
                                            // Create columns from first row
                                            firstRow = false;
                                        }
                                        else
                                        {
                                            // Add data rows
                                            var dataRow = dt.NewRow();
                                            dataRow["_id"] = rowIndex++;
                                            var iter = row.CellsUsed().GetEnumerator();
                                            iter.MoveNext();//Id
                                            iter.MoveNext();//RegistrationDate
                                            var cell = iter.Current;
                                            dataRow["RegistrationDateStr"] = cell.Value.ToString();
                                            dataRow["RegistrationDate"] = Util.Util.ConvertStrToDate(cell.Value.ToString());
                                            iter.MoveNext();
                                            cell = iter.Current;
                                            dataRow["ProjectName"] = cell.Value.ToString();
                                            iter.MoveNext();
                                            cell = iter.Current;
                                            dataRow["AmountMS"] = cell.Value.ToString();
                                            iter.MoveNext();
                                            cell = iter.Current;
                                            dataRow["NumberOfSamples"] = cell.Value.ToString();
                                            iter.MoveNext();
                                            cell = iter.Current;
                                            dataRow["Method"] = cell.Value.ToString();
                                            iter.MoveNext();
                                            cell = iter.Current;
                                            dataRow["Machine"] = cell.Value.ToString();
                                            var _machines = cell.Value.ToString().Replace("/", "###");
                                            if (!_machines.Contains("###")) _machines += "###";
                                            dataRow["GetMachines"] = _machines;
                                            iter.MoveNext();
                                            cell = iter.Current;
                                            var _faims = cell.Value.ToString().Replace("/", "###");
                                            if (!_faims.Contains("###")) _faims += "###";
                                            dataRow["FAIMS"] = _faims;
                                            dataRow["_faims"] = cell.Value.ToString();
                                            iter.MoveNext();
                                            cell = iter.Current;
                                            dataRow["_receiveNotification"] = cell.Value.ToString();
                                            dataRow["ReceiveNotification"] = Convert.ToBoolean(cell.Value.ToString());
                                            iter.MoveNext();
                                            cell = iter.Current;
                                            if (cell.Address.ColumnLetter.Equals("J"))//Check whether Comments is filled 
                                            {
                                                dataRow["Comments"] = cell.Value.ToString();
                                                iter.MoveNext();
                                                cell = iter.Current;
                                            }
                                            dataRow["AddedBy"] = cell.Value.ToString();
                                            iter.MoveNext();
                                            cell = iter.Current;
                                            dataRow["Status"] = Project.GetStatus(Convert.ToString(cell.Value.ToString()));
                                            dataRow["_status"] = cell.Value.ToString();
                                            iter.MoveNext();
                                            cell = iter.Current;
                                            dataRow["InfoStatus"] = Management.GetInfoStatusStr(cell.Value.ToString());
                                            dataRow["_infoStatus"] = cell.Value.ToString();
                                            dt.Rows.Add(dataRow);
                                        }
                                        break;
                                    }
                                case var name when name.EndsWith("_Queue"):
                                    {
                                        if (firstRow)
                                        {
                                            rowIndex = 0;
                                            dt.Columns.Add("_id");
                                            dt.Columns.Add("RegistrationDateStr");
                                            dt.Columns.Add("RegistrationDate");
                                            dt.Columns.Add("ProjectName");
                                            dt.Columns.Add("AmountMS");
                                            dt.Columns.Add("NumberOfSamples");
                                            dt.Columns.Add("FAIMS");
                                            dt.Columns.Add("Method");
                                            dt.Columns.Add("ProjectID");
                                            dt.Columns.Add("AddedBy");
                                            dt.Columns.Add("InfoStatus");
                                            dt.Columns.Add("_infoStatus");
                                            // Create columns from first row
                                            firstRow = false;
                                        }
                                        else
                                        {
                                            // Add data rows
                                            var dataRow = dt.NewRow();
                                            dataRow["_id"] = rowIndex++;
                                            var iter = row.CellsUsed().GetEnumerator();
                                            iter.MoveNext();//RegistrationDate
                                            var cell = iter.Current;
                                            dataRow["RegistrationDateStr"] = cell.Value.ToString();
                                            dataRow["RegistrationDate"] = Util.Util.ConvertStrToDate(cell.Value.ToString());
                                            iter.MoveNext();
                                            cell = iter.Current;
                                            dataRow["ProjectName"] = cell.Value.ToString();
                                            iter.MoveNext();
                                            cell = iter.Current;
                                            dataRow["AmountMS"] = cell.Value.ToString();
                                            iter.MoveNext();
                                            cell = iter.Current;
                                            dataRow["NumberOfSamples"] = cell.Value.ToString();
                                            iter.MoveNext();
                                            cell = iter.Current;
                                            dataRow["FAIMS"] = cell.Value.ToString();
                                            iter.MoveNext();
                                            cell = iter.Current;
                                            dataRow["Method"] = cell.Value.ToString();
                                            iter.MoveNext();
                                            cell = iter.Current;
                                            dataRow["ProjectID"] = Convert.ToInt32(cell.Value.ToString());
                                            iter.MoveNext();
                                            cell = iter.Current;
                                            dataRow["AddedBy"] = cell.Value.ToString();
                                            iter.MoveNext();
                                            cell = iter.Current;
                                            dataRow["InfoStatus"] = Management.GetInfoStatusStr(cell.Value.ToString());
                                            dataRow["_infoStatus"] = cell.Value.ToString();
                                            dt.Rows.Add(dataRow);
                                        }
                                        break;
                                    }
                                case var name when name.EndsWith("_Log"):
                                    {
                                        if (firstRow)
                                        {
                                            rowIndex = 0;
                                            dt.Columns.Add("_id");
                                            dt.Columns.Add("RegistrationDateStr");
                                            dt.Columns.Add("RegistrationDate");
                                            dt.Columns.Add("Operator");
                                            dt.Columns.Add("Remarks");
                                            dt.Columns.Add("ColumnLotNumber");
                                            dt.Columns.Add("LC");
                                            dt.Columns.Add("_isCalibrated");
                                            dt.Columns.Add("IsCalibrated");
                                            dt.Columns.Add("_isFullyCalibrated");
                                            dt.Columns.Add("IsFullyCalibrated");
                                            dt.Columns.Add("AddedBy");
                                            dt.Columns.Add("InfoStatus");
                                            dt.Columns.Add("_infoStatus");
                                            dt.Columns.Add("TechnicalReportFile_GoogleID");
                                            dt.Columns.Add("_TechnicalReportFileName");
                                            dt.Columns.Add("_TechnicalReportGoogleId");
                                            // Create columns from first row
                                            firstRow = false;
                                        }
                                        else
                                        {
                                            // Add data rows
                                            var dataRow = dt.NewRow();
                                            dataRow["_id"] = rowIndex++;
                                            var iter = row.CellsUsed().GetEnumerator();
                                            iter.MoveNext();//Id
                                            iter.MoveNext();//RegistrationDate
                                            var cell = iter.Current;
                                            dataRow["RegistrationDateStr"] = cell.Value.ToString();
                                            dataRow["RegistrationDate"] = Util.Util.ConvertStrToDate(cell.Value.ToString());
                                            iter.MoveNext();
                                            cell = iter.Current;
                                            dataRow["Operator"] = cell.Value.ToString();
                                            iter.MoveNext();
                                            cell = iter.Current;
                                            dataRow["Remarks"] = cell.Value.ToString();
                                            iter.MoveNext();
                                            cell = iter.Current;
                                            if (cell.Address.ColumnLetter.Equals("E"))//Check whether Comments is filled 
                                            {
                                                dataRow["ColumnLotNumber"] = cell.Value.ToString();
                                                iter.MoveNext();
                                                cell = iter.Current;
                                            }
                                            if (cell.Address.ColumnLetter.Equals("F"))//Check whether Comments is filled 
                                            {
                                                dataRow["LC"] = cell.Value.ToString();
                                                iter.MoveNext();
                                                cell = iter.Current;
                                            }
                                            dataRow["_isCalibrated"] = cell.Value.ToString();
                                            dataRow["IsCalibrated"] = Convert.ToBoolean(cell.Value.ToString());
                                            iter.MoveNext();
                                            cell = iter.Current;
                                            dataRow["_isFullyCalibrated"] = cell.Value.ToString();
                                            dataRow["IsFullyCalibrated"] = Convert.ToBoolean(cell.Value.ToString());
                                            iter.MoveNext();
                                            cell = iter.Current;
                                            if (cell.Address.ColumnLetter.Equals("I"))//Check whether Comments is filled 
                                            {
                                                var google_file = Regex.Split(cell.Value.ToString(), "###");

                                                var file_name = Management.GetTmpDir() + Path.GetFileNameWithoutExtension(google_file[0]) + "_" + Guid.NewGuid() + ".pdf";
                                                Connection.DownloadFile(google_file[1], file_name);

                                                var machine = Regex.Split(name, "_")[0];
                                                string new_tech_file = string.Empty;
                                                UploadTechnicalReportOffline(file_name, machine, out new_tech_file);

                                                dataRow["TechnicalReportFile_GoogleID"] = new_tech_file + "###";
                                                dataRow["_TechnicalReportFileName"] = new_tech_file;
                                                dataRow["_TechnicalReportGoogleId"] = null;
                                                iter.MoveNext();
                                                cell = iter.Current;
                                            }
                                            dataRow["AddedBy"] = cell.Value.ToString();
                                            iter.MoveNext();
                                            cell = iter.Current;
                                            dataRow["InfoStatus"] = Management.GetInfoStatusStr(cell.Value.ToString());
                                            dataRow["_infoStatus"] = cell.Value.ToString();
                                            dt.Rows.Add(dataRow);
                                        }
                                        break;
                                    }
                                case var name when name.EndsWith("_Evaluation"):
                                    {
                                        if (firstRow)
                                        {
                                            rowIndex = 0;
                                            dt.Columns.Add("_id");

                                            dtOT.Columns.Add("_id");
                                            dtOT.Columns.Add("RegistrationDateStr");
                                            dtOT.Columns.Add("RegistrationDate");
                                            dtOT.Columns.Add("FAIMS");
                                            dtOT.Columns.Add("MS1Intensity");
                                            dtOT.Columns.Add("MS2Intensity");
                                            dtOT.Columns.Add("ProteinGroup");
                                            dtOT.Columns.Add("PeptideGroup");
                                            dtOT.Columns.Add("PSM");
                                            dtOT.Columns.Add("MSMS");
                                            dtOT.Columns.Add("IDRatio");
                                            dtOT.Columns.Add("MassError");
                                            dtOT.Columns.Add("MassErrorMedian");
                                            dtOT.Columns.Add("XreaMean");
                                            dtOT.Columns.Add("Xreas");
                                            dtOT.Columns.Add("Exclude");
                                            dtOT.Columns.Add("RawFile");
                                            dtOT.Columns.Add("RunID");
                                            dtOT.Columns.Add("MostAbundantPepts");

                                            dtIT.Columns.Add("_id");
                                            dtIT.Columns.Add("RegistrationDateStr");
                                            dtIT.Columns.Add("RegistrationDate");
                                            dtIT.Columns.Add("FAIMS");
                                            dtIT.Columns.Add("MS1Intensity");
                                            dtIT.Columns.Add("MS2Intensity");
                                            dtIT.Columns.Add("ProteinGroup");
                                            dtIT.Columns.Add("PeptideGroup");
                                            dtIT.Columns.Add("PSM");
                                            dtIT.Columns.Add("MSMS");
                                            dtIT.Columns.Add("IDRatio");
                                            dtIT.Columns.Add("MassError");
                                            dtIT.Columns.Add("MassErrorMedian");
                                            dtIT.Columns.Add("XreaMean");
                                            dtIT.Columns.Add("Xreas");
                                            dtIT.Columns.Add("Exclude");
                                            dtIT.Columns.Add("RawFile");
                                            dtIT.Columns.Add("RunID");
                                            dtIT.Columns.Add("MostAbundantPepts");

                                            dt.Columns.Add("Operator");
                                            dt.Columns.Add("Comments");
                                            dt.Columns.Add("ColumnLotNumber");
                                            dt.Columns.Add("AddedBy");
                                            dt.Columns.Add("InfoStatus");
                                            dt.Columns.Add("_infoStatus");
                                            // Create columns from first row
                                            firstRow = false;
                                        }
                                        else
                                        {
                                            if (row.RangeAddress.FirstAddress.RowNumber == 2) continue;
                                            // Add data rows
                                            var dataRow = dt.NewRow();
                                            dataRow["_id"] = rowIndex++;
                                            var iter = row.CellsUsed().GetEnumerator();
                                            iter.MoveNext();//Id
                                            iter.MoveNext();//Operator
                                            var cell = iter.Current;//OT
                                            dataRow["Operator"] = cell.Value.ToString();
                                            iter.MoveNext();
                                            cell = iter.Current;
                                            if (cell.Address.ColumnLetter.Equals("C"))//Check whether Comments is filled 
                                            {
                                                dataRow["Comments"] = cell.Value.ToString();
                                                iter.MoveNext();
                                                cell = iter.Current;
                                            }
                                            if (cell.Address.ColumnLetter.Equals("D"))//Check whether Column lot number is filled 
                                            {
                                                dataRow["ColumnLotNumber"] = cell.Value.ToString();
                                                iter.MoveNext();
                                                cell = iter.Current;
                                            }
                                            #region OT
                                            var dataRowOT = dtOT.NewRow();
                                            dataRowOT["_id"] = 0;
                                            dataRowOT["RegistrationDateStr"] = cell.Value.ToString();
                                            dataRowOT["RegistrationDate"] = Util.Util.ConvertStrToDate(cell.Value.ToString());
                                            iter.MoveNext();
                                            cell = iter.Current;
                                            dataRowOT["FAIMS"] = Convert.ToBoolean(cell.Value.ToString());
                                            iter.MoveNext();
                                            cell = iter.Current;
                                            dataRowOT["MS1Intensity"] = Convert.ToDouble(cell.Value.ToString(), CultureInfo.InvariantCulture) % 1 == 0 ? Convert.ToDouble(cell.Value.ToString(), CultureInfo.InvariantCulture).ToString("0.0", CultureInfo.InvariantCulture) : Convert.ToDouble(cell.Value.ToString(), CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture);
                                            iter.MoveNext();
                                            cell = iter.Current;
                                            dataRowOT["MS2Intensity"] = Convert.ToDouble(cell.Value.ToString(), CultureInfo.InvariantCulture) % 1 == 0 ? Convert.ToDouble(cell.Value.ToString(), CultureInfo.InvariantCulture).ToString("0.0", CultureInfo.InvariantCulture) : Convert.ToDouble(cell.Value.ToString(), CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture);
                                            iter.MoveNext();
                                            cell = iter.Current;
                                            dataRowOT["ProteinGroup"] = Convert.ToDouble(cell.Value.ToString(), CultureInfo.InvariantCulture) % 1 == 0 ? Convert.ToDouble(cell.Value.ToString(), CultureInfo.InvariantCulture).ToString("0.0", CultureInfo.InvariantCulture) : Convert.ToDouble(cell.Value.ToString(), CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture);
                                            iter.MoveNext();
                                            cell = iter.Current;
                                            dataRowOT["PeptideGroup"] = Convert.ToDouble(cell.Value.ToString(), CultureInfo.InvariantCulture) % 1 == 0 ? Convert.ToDouble(cell.Value.ToString(), CultureInfo.InvariantCulture).ToString("0.0", CultureInfo.InvariantCulture) : Convert.ToDouble(cell.Value.ToString(), CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture);
                                            iter.MoveNext();
                                            cell = iter.Current;
                                            dataRowOT["PSM"] = Convert.ToDouble(cell.Value.ToString(), CultureInfo.InvariantCulture) % 1 == 0 ? Convert.ToDouble(cell.Value.ToString(), CultureInfo.InvariantCulture).ToString("0.0", CultureInfo.InvariantCulture) : Convert.ToDouble(cell.Value.ToString(), CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture);
                                            iter.MoveNext();
                                            cell = iter.Current;
                                            dataRowOT["MSMS"] = Convert.ToDouble(cell.Value.ToString(), CultureInfo.InvariantCulture) % 1 == 0 ? Convert.ToDouble(cell.Value.ToString(), CultureInfo.InvariantCulture).ToString("0.0", CultureInfo.InvariantCulture) : Convert.ToDouble(cell.Value.ToString(), CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture);
                                            iter.MoveNext();
                                            cell = iter.Current;
                                            dataRowOT["IDRatio"] = Convert.ToDouble(cell.Value.ToString(), CultureInfo.InvariantCulture) % 1 == 0 ? Convert.ToDouble(cell.Value.ToString(), CultureInfo.InvariantCulture).ToString("0.0", CultureInfo.InvariantCulture) : Convert.ToDouble(cell.Value.ToString(), CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture);
                                            iter.MoveNext();
                                            cell = iter.Current;
                                            if (cell.Address.ColumnLetter.Equals("N"))//Check whether Mass error is filled 
                                            {
                                                dataRowOT["MassError"] = cell.Value.ToString();
                                                iter.MoveNext();
                                                cell = iter.Current;
                                            }
                                            dataRowOT["MassErrorMedian"] = Convert.ToDouble(cell.Value.ToString(), CultureInfo.InvariantCulture) % 1 == 0 ? Convert.ToDouble(cell.Value.ToString(), CultureInfo.InvariantCulture).ToString("0.0", CultureInfo.InvariantCulture) : Convert.ToDouble(cell.Value.ToString(), CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture);
                                            iter.MoveNext();
                                            cell = iter.Current;
                                            dataRowOT["XreaMean"] = Convert.ToDouble(cell.Value.ToString(), CultureInfo.InvariantCulture) % 1 == 0 ? Convert.ToDouble(cell.Value.ToString(), CultureInfo.InvariantCulture).ToString("0.0", CultureInfo.InvariantCulture) : Convert.ToDouble(cell.Value.ToString(), CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture);
                                            iter.MoveNext();
                                            cell = iter.Current;
                                            if (cell.Address.ColumnLetter.Equals("Q"))//Check whether Xreas is filled 
                                            {
                                                dataRowOT["Xreas"] = cell.Value.ToString();
                                                iter.MoveNext();
                                                cell = iter.Current;
                                            }
                                            dataRowOT["MostAbundantPepts"] = cell.Value.ToString();
                                            iter.MoveNext();
                                            cell = iter.Current;
                                            dataRowOT["Exclude"] = Convert.ToBoolean(cell.Value.ToString());
                                            iter.MoveNext();
                                            cell = iter.Current;
                                            if (cell.Address.ColumnLetter.Equals("T"))//Check whether Raw file is filled 
                                            {
                                                dataRowOT["RawFile"] = cell.Value.ToString();
                                                iter.MoveNext();
                                                cell = iter.Current;
                                            }
                                            dataRowOT["RunID"] = dataRow["_id"];
                                            dtOT.Rows.Add(dataRowOT);
                                            #endregion

                                            #region IT
                                            var dataRowIT = dtIT.NewRow();
                                            dataRowIT["_id"] = 0;
                                            dataRowIT["RegistrationDateStr"] = cell.Value.ToString();
                                            dataRowIT["RegistrationDate"] = Util.Util.ConvertStrToDate(cell.Value.ToString());
                                            iter.MoveNext();
                                            cell = iter.Current;
                                            dataRowIT["FAIMS"] = Convert.ToBoolean(cell.Value.ToString());
                                            iter.MoveNext();
                                            cell = iter.Current;
                                            dataRowIT["MS1Intensity"] = Convert.ToDouble(cell.Value.ToString(), CultureInfo.InvariantCulture) % 1 == 0 ? Convert.ToDouble(cell.Value.ToString(), CultureInfo.InvariantCulture).ToString("0.0", CultureInfo.InvariantCulture) : Convert.ToDouble(cell.Value.ToString(), CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture);
                                            iter.MoveNext();
                                            cell = iter.Current;
                                            dataRowIT["MS2Intensity"] = Convert.ToDouble(cell.Value.ToString(), CultureInfo.InvariantCulture) % 1 == 0 ? Convert.ToDouble(cell.Value.ToString(), CultureInfo.InvariantCulture).ToString("0.0", CultureInfo.InvariantCulture) : Convert.ToDouble(cell.Value.ToString(), CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture);
                                            iter.MoveNext();
                                            cell = iter.Current;
                                            dataRowIT["ProteinGroup"] = Convert.ToDouble(cell.Value.ToString(), CultureInfo.InvariantCulture) % 1 == 0 ? Convert.ToDouble(cell.Value.ToString(), CultureInfo.InvariantCulture).ToString("0.0", CultureInfo.InvariantCulture) : Convert.ToDouble(cell.Value.ToString(), CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture);
                                            iter.MoveNext();
                                            cell = iter.Current;
                                            dataRowIT["PeptideGroup"] = Convert.ToDouble(cell.Value.ToString(), CultureInfo.InvariantCulture) % 1 == 0 ? Convert.ToDouble(cell.Value.ToString(), CultureInfo.InvariantCulture).ToString("0.0", CultureInfo.InvariantCulture) : Convert.ToDouble(cell.Value.ToString(), CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture);
                                            iter.MoveNext();
                                            cell = iter.Current;
                                            dataRowIT["PSM"] = Convert.ToDouble(cell.Value.ToString(), CultureInfo.InvariantCulture) % 1 == 0 ? Convert.ToDouble(cell.Value.ToString(), CultureInfo.InvariantCulture).ToString("0.0", CultureInfo.InvariantCulture) : Convert.ToDouble(cell.Value.ToString(), CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture);
                                            iter.MoveNext();
                                            cell = iter.Current;
                                            dataRowIT["MSMS"] = Convert.ToDouble(cell.Value.ToString(), CultureInfo.InvariantCulture) % 1 == 0 ? Convert.ToDouble(cell.Value.ToString(), CultureInfo.InvariantCulture).ToString("0.0", CultureInfo.InvariantCulture) : Convert.ToDouble(cell.Value.ToString(), CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture);
                                            iter.MoveNext();
                                            cell = iter.Current;
                                            dataRowIT["IDRatio"] = Convert.ToDouble(cell.Value.ToString(), CultureInfo.InvariantCulture) % 1 == 0 ? Convert.ToDouble(cell.Value.ToString(), CultureInfo.InvariantCulture).ToString("0.0", CultureInfo.InvariantCulture) : Convert.ToDouble(cell.Value.ToString(), CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture);
                                            iter.MoveNext();
                                            cell = iter.Current;
                                            if (cell.Address.ColumnLetter.Equals("AD"))//Check whether Mass error is filled 
                                            {
                                                dataRowIT["MassError"] = cell.Value.ToString();
                                                iter.MoveNext();
                                                cell = iter.Current;
                                            }
                                            dataRowIT["MassErrorMedian"] = Convert.ToDouble(cell.Value.ToString(), CultureInfo.InvariantCulture) % 1 == 0 ? Convert.ToDouble(cell.Value.ToString(), CultureInfo.InvariantCulture).ToString("0.0", CultureInfo.InvariantCulture) : Convert.ToDouble(cell.Value.ToString(), CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture);
                                            iter.MoveNext();
                                            cell = iter.Current;
                                            dataRowIT["XreaMean"] = Convert.ToDouble(cell.Value.ToString(), CultureInfo.InvariantCulture) % 1 == 0 ? Convert.ToDouble(cell.Value.ToString(), CultureInfo.InvariantCulture).ToString("0.0", CultureInfo.InvariantCulture) : Convert.ToDouble(cell.Value.ToString(), CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture);
                                            iter.MoveNext();
                                            cell = iter.Current;
                                            if (cell.Address.ColumnLetter.Equals("AG"))//Check whether Xreas is filled 
                                            {
                                                dataRowIT["Xreas"] = cell.Value.ToString();
                                                iter.MoveNext();
                                                cell = iter.Current;
                                            }
                                            dataRowIT["MostAbundantPepts"] = cell.Value.ToString();
                                            iter.MoveNext();
                                            cell = iter.Current;
                                            dataRowIT["Exclude"] = Convert.ToBoolean(cell.Value.ToString());
                                            iter.MoveNext();
                                            cell = iter.Current;
                                            if (cell.Address.ColumnLetter.Equals("AJ"))//Check whether Raw file is filled 
                                            {
                                                dataRowIT["RawFile"] = cell.Value.ToString();
                                                iter.MoveNext();
                                                cell = iter.Current;
                                            }
                                            dataRowIT["RunID"] = dataRow["_id"];
                                            dtIT.Rows.Add(dataRowIT);
                                            #endregion

                                            dataRow["AddedBy"] = cell.Value.ToString();
                                            iter.MoveNext();
                                            cell = iter.Current;
                                            dataRow["InfoStatus"] = Management.GetInfoStatusStr(cell.Value.ToString());
                                            dataRow["_infoStatus"] = cell.Value.ToString();

                                            dt.Rows.Add(dataRow);
                                        }

                                        break;
                                    }
                            }
                        }

                        if (collectionName.EndsWith("_Evaluation"))
                            CreateCollectionEvaluation(collection, dt, dtOT, dtIT);
                        else
                            CreateCollection(collection, dt);
                    }
                }
            }
            #endregion

            System.IO.File.Delete(fileName);
        }

        private static void CreateCollection(ILiteCollection<BsonDocument> collection, DataTable dt)
        {
            // Convert DataTable to LiteDB documents
            foreach (DataRow row in dt.Rows)
            {
                var document = new BsonDocument();
                foreach (DataColumn column in dt.Columns)
                {
                    string columnValue = row[column]?.ToString()?.Trim();

                    if (string.IsNullOrEmpty(columnValue))
                    {
                        document[column.ColumnName] = BsonValue.Null;
                        continue;
                    }
                    if (KeepString(column.ColumnName))
                        document[column.ColumnName] = row[column].ToString();
                    else
                        document[column.ColumnName] = DetectType(columnValue);
                }
                collection.Insert(document);
            }
        }

        private static void CreateCollectionEvaluation(ILiteCollection<BsonDocument> collection, DataTable dt, DataTable dtOT, DataTable dtIT)
        {
            if (dt.Rows.Count == 0 ||
                (dt.Rows.Count != dtOT.Rows.Count &&
                dt.Rows.Count != dtIT.Rows.Count &&
                dtOT.Rows.Count != dtIT.Rows.Count
                )
            ) return;

            // Convert DataTable to LiteDB documents
            var documentsOT = new BsonArray();
            foreach (DataRow row in dtOT.Rows)
            {
                var documentOT = new BsonDocument();
                foreach (DataColumn column in dtOT.Columns)
                {
                    string columnValue = row[column]?.ToString()?.Trim();

                    if (string.IsNullOrEmpty(columnValue))
                    {
                        documentOT[column.ColumnName] = BsonValue.Null;
                        continue;
                    }
                    if (KeepString(column.ColumnName))
                        documentOT[column.ColumnName] = row[column].ToString();
                    else if (column.ColumnName == "Xreas")
                        documentOT[column.ColumnName] = ParseXreaArray(columnValue);
                    else if (column.ColumnName == "MostAbundantPepts")
                        documentOT[column.ColumnName] = ParseDictionary(columnValue);
                    else
                        documentOT[column.ColumnName] = DetectType(columnValue);
                }
                if (documentOT.Count != 0)
                    documentsOT.Add(documentOT);
            }

            var documentsIT = new BsonArray();
            foreach (DataRow row in dtIT.Rows)
            {
                var documentIT = new BsonDocument();
                foreach (DataColumn column in dtIT.Columns)
                {
                    string columnValue = row[column]?.ToString()?.Trim();

                    if (string.IsNullOrEmpty(columnValue))
                    {
                        documentIT[column.ColumnName] = BsonValue.Null;
                        continue;
                    }
                    if (KeepString(column.ColumnName))
                        documentIT[column.ColumnName] = row[column].ToString();
                    else if (column.ColumnName == "Xreas")
                        documentIT[column.ColumnName] = ParseXreaArray(columnValue);
                    else if (column.ColumnName == "MostAbundantPepts")
                        documentIT[column.ColumnName] = ParseDictionary(columnValue);
                    else
                        documentIT[column.ColumnName] = DetectType(columnValue);
                }
                if (documentIT.Count != 0)
                    documentsIT.Add(documentIT);
            }

            for (int i = 0; i < dt.Rows.Count; i++)
            {
                DataRow row = dt.Rows[i];

                var masterDocument = new BsonDocument();
                foreach (DataColumn column in dt.Columns)
                {
                    string columnValue = row[column]?.ToString()?.Trim();

                    if (string.IsNullOrEmpty(columnValue))
                    {
                        masterDocument[column.ColumnName] = BsonValue.Null;
                        continue;
                    }
                    if (KeepString(column.ColumnName))
                        masterDocument[column.ColumnName] = row[column].ToString();
                    else
                        masterDocument[column.ColumnName] = DetectType(columnValue);
                }

                masterDocument["OT"] = documentsOT[i];
                masterDocument["IT"] = documentsIT[i];

                collection.Insert(masterDocument);
            }
        }

        private static bool KeepString(string columnName)
        {
            if (columnName.EndsWith("Str")) return true;
            else if (columnName.EndsWith("Name")) return true;
            else if (columnName.Equals("AmountMS")) return true;
            else if (columnName.Equals("NumberOfSamples")) return true;
            else if (columnName.Equals("Method")) return true;
            else if (columnName.Equals("Machine")) return true;
            else if (columnName.Equals("AddedBy")) return true;
            else if (columnName.Equals("NumberOfSamples")) return true;
            else if (columnName.Equals("_receiveNotification")) return true;
            else if (columnName.Equals("_faims")) return true;
            else if (columnName.Equals("_isCalibrated")) return true;
            else if (columnName.Equals("_isFullyCalibrated")) return true;
            else if (columnName.Equals("ColumnLotNumber")) return true;
            else if (columnName.Equals("TechnicalReportFile_GoogleID")) return true;
            else if (columnName.Equals("Operator")) return true;
            else if (columnName.Equals("MassError")) return true;
            return false;

        }

        /// <summary>
        /// Detects the correct data type and converts the value.
        /// </summary>
        private static BsonValue DetectType(string value)
        {
            if (value.Contains("###"))
            {
                // If the value contains commas, treat it as an array
                var arrayValues = Regex.Split(value, "###")
                                .Where(v => !string.IsNullOrWhiteSpace(v))  // Remove empty values
                                .Select(v => new BsonValue(v.Trim()))      // Convert each string to BsonValue
                                .ToList();
                return new BsonArray(arrayValues);
            }

            if (int.TryParse(value, out int intValue))
                return intValue;

            if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double doubleValue))
                return doubleValue;

            if (bool.TryParse(value, out bool boolValue))
                return boolValue;

            if (DateTime.TryParse(value, out DateTime dateValue))
                return dateValue;

            return value; // Default to string
        }

        /// <summary>
        /// Parses a comma-separated list of Xrea data into an array of objects.
        /// </summary>
        static BsonArray ParseXreaArray(string value)
        {
            var array = new BsonArray();
            var entries = value.Split('#');

            foreach (var entry in entries)
            {
                var parts = entry.Split('_');
                if (parts.Length == 2 && double.TryParse(parts[0], out double rt) && double.TryParse(parts[1], out double xrea))
                {
                    array.Add(new BsonDocument { { "rt", rt }, { "xrea", xrea } });
                }
            }
            return array;
        }

        /// <summary>
        /// Parses a dictionary-like structure into a BsonDocument.
        /// Example input: "FKDLGEEHFK:6,DLGEEHFK:4,GEEHFK:1"
        /// </summary>
        static BsonDocument ParseDictionary(string value)
        {
            try
            {
                var jsonDict = Q2C.Control.FileManagement.Serializer.FromJson<Dictionary<string, object>>(value, false);
                if (jsonDict == null) return new BsonDocument();

                var bsonDoc = new BsonDocument();

                foreach (var kvp in jsonDict)
                {
                    // Ensure proper type conversion
                    if (kvp.Value is long || kvp.Value is int)
                    {
                        bsonDoc[kvp.Key] = Convert.ToInt32(kvp.Value); // Store as int
                    }
                    else if (kvp.Value is double)
                    {
                        bsonDoc[kvp.Key] = Convert.ToDouble(kvp.Value); // Store as double
                    }
                    else
                    {
                        bsonDoc[kvp.Key] = kvp.Value?.ToString(); // Store as string
                    }
                }

                return bsonDoc;
            }
            catch (Exception)
            {
                return new BsonDocument();
            }

        }

        public static bool ExportSpreadSheet(string fileName)
        {
            if (IsOnline) return ExportSpreadSheetOnline(fileName);
            else return ExportSpreadSheetOffline(fileName);
        }

        private static bool ExportSpreadSheetOnline(string fileName)
        {
            try
            {
                string mimeType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"; // Excel format
                                                                                                       // Export the spreadsheet
                var request = service_drive.Files.Export(SpreadsheetId, mimeType);
                var stream = new System.IO.MemoryStream();
                request.MediaDownloader.ProgressChanged += (IDownloadProgress progress) =>
                {
                    switch (progress.Status)
                    {
                        case DownloadStatus.Downloading:
                            {
                                Console.WriteLine(progress.BytesDownloaded);
                                break;
                            }
                        case DownloadStatus.Completed:
                            {
                                Console.WriteLine("Download complete.");
                                break;
                            }
                        case DownloadStatus.Failed:
                            {
                                Console.WriteLine("Download failed.");
                                break;
                            }
                    }
                };
                request.Download(stream);

                // Save the exported file
                using (var fileStream = new System.IO.FileStream(fileName, System.IO.FileMode.Create, System.IO.FileAccess.Write))
                {
                    stream.WriteTo(fileStream);
                }
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
        private static bool ExportSpreadSheetOffline(string fileName)
        {
            using (var db = CreateDatabase())
            {
                using (var workbook = new XLWorkbook())
                {
                    foreach (var collectionName in db.GetCollectionNames())
                    {
                        var collection = db.GetCollection(collectionName);
                        var records = collection.FindAll();

                        // Convert records to DataTable
                        DataTable dt = new DataTable(collectionName);

                        // Add columns dynamically
                        foreach (var record in records)
                        {
                            foreach (var key in record.Keys)
                            {
                                if (!dt.Columns.Contains(key))
                                    dt.Columns.Add(key);
                            }
                        }

                        // Add rows
                        foreach (var record in records)
                        {
                            var row = dt.NewRow();
                            foreach (var key in record.Keys)
                            {
                                row[key] = record[key]?.ToString() ?? "";
                            }
                            dt.Rows.Add(row);
                        }

                        // Add DataTable to Excel sheet
                        var worksheet = workbook.Worksheets.Add(dt, collectionName);
                    }

                    // Save to file
                    workbook.SaveAs(fileName);
                }
            }
            return true;
        }

        public static string CreateNewSpreadsheet()
        {
            string fileID = "unidentified";
            try
            {
                var filename = "machines_samples_information";
                var fileMetadata = new Google.Apis.Drive.v3.Data.File()
                {
                    Name = filename,
                    MimeType = "application/vnd.google-apps.spreadsheet",
                };
                FilesResource.CreateRequest request = service_drive.Files.Create(fileMetadata);
                request.SupportsTeamDrives = true;
                request.Fields = "id";
                System.Net.ServicePointManager.ServerCertificateValidationCallback = delegate (object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors) { return true; };
                var file = request.Execute();
                fileID = file.Id;
            }
            catch (Google.GoogleApiException e)
            {
                if (e.Error.Message.Contains("Request had insufficient authentication scopes"))
                {
                    RevokeConnection();
                    Thread.Sleep(1000);
                    bool isValid = Init(Management.GetDatabase());
                    if (isValid)
                        CreateNewSpreadsheet();
                }
                else
                    throw;
            }
            catch (Exception)
            {
                throw;
            }
            return fileID;
        }
        public async static void RevokeConnection()
        {
            try
            {
                if (UserCredential != null)
                {
                    await UserCredential.RevokeTokenAsync(CancellationToken.None);
                }
            }
            catch (Exception)
            {
            }
        }

        private static UserCredential Login(string googleClientId, string googleClientSecret, string[] scopes)
        {
            // Create a cancellation token source with a timeout
            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.CancelAfter(TimeSpan.FromSeconds(90)); // Set timeout to 90 seconds

            ClientSecrets secrets = new ClientSecrets()
            {
                ClientId = googleClientId,
                ClientSecret = googleClientSecret,
            };

            try
            {
                return GoogleWebAuthorizationBroker.AuthorizeAsync(secrets, scopes, "user", cancellationTokenSource.Token).Result;
            }
            catch (OperationCanceledException)
            {
                return null;
            }
        }

        public static User GetUser()
        {
            if (Management.Users == null || Management.Users.Count == 0) return null;

            return Management.Users.Where(a => a.Name.Equals(Management.GetComputerUser())).FirstOrDefault();
        }

        private static void CleanLists(bool isInitial = false, string tab_name = "", string machine_name = "")
        {
            if (isInitial)
            {
                if (String.IsNullOrEmpty(tab_name))
                {
                    Management.InfoApp = new();
                    Management.Users.Clear();
                    Management.Machines.Clear();
                }
                else
                {
                    switch (tab_name)
                    {
                        case "Info":
                            Management.InfoApp = new();
                            break;
                        case "Users":
                            Management.Users.Clear();
                            break;
                        case "Machines":
                            Management.Machines.Clear();
                            break;
                    }
                }
            }
            else
            {
                if (String.IsNullOrEmpty(tab_name))
                {
                    foreach (var machine_property in Management.Machines_Properties)
                    {
                        (List<MachineQueue> queue, List<Run> evaluation, List<MachineLog> log) _properties = machine_property.Value;
                        _properties.queue.Clear();
                        _properties.evaluation.Clear();
                        _properties.log.Clear();
                    }
                    Management.Projects.Clear();
                }
                else
                {
                    switch (tab_name)
                    {
                        case "Projects":
                            Management.Projects.Clear();
                            break;
                        case "Machines":
                            Dictionary<string, (List<MachineQueue> queue, List<Run> evaluation, List<MachineLog> log)> current_machines = Management.Machines_Properties;
                            if (String.IsNullOrEmpty(machine_name))
                            {
                                foreach (var machine_property in Management.Machines_Properties)
                                {
                                    (List<MachineQueue> queue, List<Run> evaluation, List<MachineLog> log) _properties = machine_property.Value;
                                    _properties.queue.Clear();
                                    _properties.evaluation.Clear();
                                    _properties.log.Clear();
                                }
                            }
                            else
                            {
                                (List<MachineQueue> queue, List<Run> evaluation, List<MachineLog> log) _properties;
                                Management.Machines_Properties.TryGetValue(machine_name, out _properties);
                                _properties.queue.Clear();
                                _properties.evaluation.Clear();
                                _properties.log.Clear();
                            }
                            break;
                    }
                }
            }
        }

        private static List<string> ReadTab(out List<string> machine_properties, bool isInitial = false, string tab_name = "", string machine_name = "")
        {
            List<string> ranges = null;
            machine_properties = new();

            if (isInitial)
            {
                if (String.IsNullOrEmpty(tab_name))
                {
                    ranges = new List<string> {
                        "Info!A:" + GetLastColumnCategory("Info"),
                        "Users!A:" + GetLastColumnCategory("Users"),
                        "Machines!A:" + GetLastColumnCategory("Machines"),
                    };
                }
                else
                {
                    ranges = new();
                    switch (tab_name)
                    {
                        case "Info":
                            ranges.Add("Info!A:" + GetLastColumnCategory("Info"));
                            break;
                        case "Users":
                            ranges.Add("Users!A:" + GetLastColumnCategory("Users"));
                            break;
                        case "Machines":
                            ranges.Add("Machines!A:" + GetLastColumnCategory("Machines"));
                            break;
                    }
                }
            }
            else
            {
                if (String.IsNullOrEmpty(tab_name))
                {
                    ranges = new List<string> {
                    "Projects!A:" + GetLastColumnCategory("Projects"),
                    };

                    foreach (var machine in Management.Machines)
                    {
                        if (machine.HasEvaluation)
                        {
                            ranges.Add($"{machine.Name}_Evaluation!A:" + GetLastColumnCategory("Evaluation"));
                            ranges.Add($"{machine.Name}_Log!A:" + GetLastColumnCategory("Log", machine.Name));
                        }
                        ranges.Add($"{machine.Name}_Queue!A:" + GetLastColumnCategory("Queue", machine.Name));
                        machine_properties.Add($"{machine.Name}Queue_Date");
                        machine_properties.Add($"{machine.Name}EvaluationID");
                        machine_properties.Add($"{machine.Name}LogID");
                    }
                }
                else
                {
                    ranges = new();
                    switch (tab_name)
                    {
                        case "Projects":
                            ranges.Add("Projects!A:" + GetLastColumnCategory("Projects"));
                            break;
                        case "Machines":
                            List<Machine> current_machines = new List<Machine>(Management.Machines);
                            if (!String.IsNullOrEmpty(machine_name))
                            {
                                current_machines.Clear();

                                var machine = Management.Machines.Where(a => a.Name.ToLower().Equals(machine_name.ToLower())).FirstOrDefault();
                                if (machine != null)
                                    current_machines.Add(machine);
                            }

                            foreach (var machine in current_machines)
                            {
                                if (machine.HasEvaluation)
                                {
                                    ranges.Add($"{machine.Name}_Evaluation!A:" + GetLastColumnCategory("Evaluation"));
                                    ranges.Add($"{machine.Name}_Log!A:" + GetLastColumnCategory("Log", machine.Name));
                                }
                                ranges.Add($"{machine.Name}_Queue!A:" + GetLastColumnCategory("Queue", machine.Name));
                                machine_properties.Add($"{machine.Name}Queue_Date");
                                machine_properties.Add($"{machine.Name}EvaluationID");
                                machine_properties.Add($"{machine.Name}LogID");
                            }

                            break;
                    }
                }
            }
            return ranges;
        }
        private static IList<ValueRange> GetSpreadSheetValues(List<string> ranges = null)
        {
            try
            {
                var request = new SpreadsheetsResource.ValuesResource.BatchGetRequest(service_sheets, SpreadsheetId);
                request.Ranges = ranges;
                System.Net.ServicePointManager.ServerCertificateValidationCallback = delegate (object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors) { return true; };
                // Executing Read Operation...
                var response = request.Execute();
                return response.ValueRanges;
            }
            catch (TokenResponseException ex) when (ex.Error.Error == "invalid_grant")
            {
                throw new Exception("reset_database", new Exception("Timeout! Try entering your credentials again!"));
            }
            catch (Google.GoogleApiException exc)
            {
                if (exc.HttpStatusCode == System.Net.HttpStatusCode.BadRequest)
                {
                    if (exc.Error.Message.StartsWith("Unable to parse range:"))
                    {
                        string new_sheet = exc.Error.Message.Replace("Unable to parse range:", "").Trim();

                        string sheetTitle = Regex.Split(new_sheet, "!")[0];
                        string machine = "";
                        if (sheetTitle.Contains("_"))
                            machine = Regex.Split(sheetTitle, "_")[0];
                        if (sheetTitle.EndsWith("_Evaluation"))
                            CreateEvaluationTabIntoSpreadSheet(sheetTitle, GetCategories(sheetTitle, machine));
                        else
                            CreateNewTabIntoSpreadsheet(sheetTitle, GetCategories(sheetTitle, machine));
                        return GetSpreadSheetValues(ranges);
                    }
                    else
                        throw new Exception("reset_database");
                }
                else if (exc.HttpStatusCode == System.Net.HttpStatusCode.Forbidden)
                    throw new Exception("reset_database", new Exception("Google account was not authorized to access the spreadsheet!"));
                else if (exc.HttpStatusCode == System.Net.HttpStatusCode.NotFound)
                    throw new Exception("reset_database", new Exception("Spreadsheet ID was not found!"));
                else
                {
                    throw new Exception("reset_database", exc);
                }
            }
            catch (System.FormatException e)
            {
                if (e.Message.Contains("was not recognized as a valid"))
                {
                    System.Windows.MessageBox.Show(
                                            "The database is not recognized.\nPlease contact the administrator.",
                                            "Q2C :: Error",
                                            (System.Windows.MessageBoxButton)MessageBoxButtons.OK,
                                            (System.Windows.MessageBoxImage)MessageBoxIcon.Error);

                    Connection.RevokeConnection();
                    Management.ResetDatabase();
                    System.Environment.Exit(1);
                }
            }
            catch (Exception e)
            {
                throw new Exception("reset_database", e);
            }
            return null;
        }

        private static bool CheckEachColumn(IList<object> values, string _property, string machine = "", bool isEvaluation = false)
        {
            object[] categories = GetCategories(_property, machine);
            if (!String.IsNullOrEmpty(machine))
                _property = machine + "_" + _property;

            for (int i = 0; i < values.Count; i++)
            {
                string existing_field = values[i].ToString();
                string mandatory_field = categories[i].ToString();
                if (existing_field == mandatory_field) continue;

                CreateNewColumn(i, mandatory_field, _property, isEvaluation);
                return true;
            }
            return false;
        }
        private static bool CheckColumns(out IList<ValueRange> valueRanges, List<string> machine_properties, List<string> ranges)
        {
            valueRanges = GetSpreadSheetValues(ranges);
            try
            {
                bool hasChanged = false;
                foreach (var valueRange in valueRanges)
                {
                    var values = valueRange.Values;
                    if (hasChanged) break;

                    if (values != null && values.Count > 0)
                    {
                        if (values[0][0] != null && values[0][0].ToString().StartsWith("Creation_Date"))
                            hasChanged = CheckEachColumn(values[0], "Info");
                        else if (values[0][0] != null && values[0][0].ToString().StartsWith("UserID"))
                            hasChanged = CheckEachColumn(values[0], "Users");
                        else if (values[0][0] != null && values[0][0].ToString().StartsWith("MachineID"))
                            hasChanged = CheckEachColumn(values[0], "Machines");
                        else if (values[0][0] != null && values[0][0].ToString().StartsWith("ProjectID"))
                            hasChanged = CheckEachColumn(values[0], "Projects");
                        else
                        {
                            byte _property_key = 0;
                            foreach (var machine_prop in machine_properties)
                            {
                                if (hasChanged) break;

                                string machine = "";
                                if (machine_prop.EndsWith("Queue_Date"))
                                {
                                    machine = machine_prop.Replace("Queue_Date", "");
                                    _property_key = 0;
                                }
                                else if (machine_prop.EndsWith("EvaluationID"))
                                {
                                    machine = machine_prop.Replace("EvaluationID", "");
                                    _property_key = 1;
                                }
                                else if (machine_prop.EndsWith("LogID"))
                                {
                                    machine = machine_prop.Replace("LogID", "");
                                    _property_key = 2;
                                }

                                if ((_property_key == 0 || _property_key == 2) &&
                                    values[0][0] != null && values[0][0].ToString().StartsWith(machine_prop))
                                {
                                    if (_property_key == 0)//Queue
                                        hasChanged = CheckEachColumn(values[0], "Queue", machine);
                                    else if (_property_key == 2)
                                        hasChanged = CheckEachColumn(values[0], "Log", machine);
                                }
                                else if (values.Count > 1 && values[1][0] != null && values[1][0].ToString().StartsWith(machine_prop))
                                {
                                    if (_property_key == 1)//Evaluation
                                        hasChanged = CheckEachColumn(values[1], "Evaluation", machine, true);
                                }
                            }
                        }
                    }
                }

                if (hasChanged)
                    return CheckColumns(out valueRanges, machine_properties, ranges);
                return true;

            }
            catch (Exception)
            {
                return false;
            }
        }

        public static void ReadInfo(bool isInitial = false, string tab_name = "", string machine_name = "")
        {
            if (IsOnline) ReadSheets(isInitial, tab_name, machine_name);
            else RetrieveAllInfoFromDB();
        }

        public static void RetrieveAllInfoFromDB()
        {
            using (var db = CreateDatabase())
            {
                if (db.CollectionExists("User"))
                {
                    var userList = db.GetCollection<User>("User").Query().ToList();
                    Management.Users = userList.Where(a => !string.IsNullOrWhiteSpace(a.Name))
                        .Select(user => { user.Email = Util.Util.DecryptString(user.Email); return user; })
                        .ToList();
                    Management.Users.Sort((a, b) => b.RegistrationDate.CompareTo(a.RegistrationDate));
                }

                if (db.CollectionExists("Machine"))
                {
                    var machinesList = db.GetCollection<Machine>("Machine").Query().ToList();
                    Management.Machines = machinesList;
                    Management.Machines.Sort((a, b) => b.RegistrationDate.CompareTo(a.RegistrationDate));
                }

                if (db.CollectionExists("Project"))
                {
                    var projectsList = db.GetCollection<Project>("Project").Query().ToList();
                    Management.Projects = projectsList;
                    Management.Projects.Sort((a, b) => b.RegistrationDate.CompareTo(a.RegistrationDate));
                }

                Management.Machines_Properties = new();
                foreach (var machine in Management.Machines)
                {
                    List<MachineQueue> queue = null;
                    List<Run> evaluation = null;
                    List<MachineLog> log = null;

                    if (db.CollectionExists($"{machine.Name}_Queue"))
                        queue = db.GetCollection<MachineQueue>($"{machine.Name}_Queue").Query().ToList();
                    if (db.CollectionExists($"{machine.Name}_Evaluation"))
                        evaluation = db.GetCollection<Run>($"{machine.Name}_Evaluation").Query().ToList();
                    if (db.CollectionExists($"{machine.Name}_Log"))
                        log = db.GetCollection<MachineLog>($"{machine.Name}_Log").Query().ToList();

                    (List<MachineQueue> queue, List<Run> evaluation, List<MachineLog> log) properties = new();
                    properties.queue = queue != null ? queue : new();
                    properties.evaluation = evaluation != null ? evaluation : new();
                    properties.log = log != null ? log : new();
                    Management.Machines_Properties.Add(machine.Name, properties);
                }
            }
        }
        public static void ReadSheets(bool isInitial = false, string tab_name = "", string machine_name = "")
        {
            try
            {
                CleanLists(isInitial, tab_name, machine_name);

                IList<ValueRange> valueRanges = null;
                List<string> machine_properties = new();
                List<string> ranges = ReadTab(out machine_properties, isInitial, tab_name, machine_name);
                bool isValid = CheckColumns(out valueRanges, machine_properties, ranges);

                if (isValid == false) throw new Exception("reset_database");

                foreach (var valueRange in valueRanges)
                {
                    var values = valueRange.Values;

                    if (values != null && values.Count > 0)
                    {
                        if (values[0][0] != null && values[0][0].ToString().StartsWith("Creation_Date"))
                        {
                            for (int i = 1; i < values.Count; i++)
                            {
                                var row = values[i];
                                if (row == null || row.Count == 0) continue;
                                //Creation_date, Q2C_Info
                                var row_data = (Convert.ToString(row[0]), Convert.ToString(row[1]));
                                Management.InfoApp = row_data;
                            }
                        }
                        else if (values[0][0] != null && values[0][0].ToString().StartsWith("UserID"))
                        {
                            for (int i = 1; i < values.Count; i++)
                            {
                                var row = values[i];
                                if (row == null || row.Count == 0) continue;
                                //UserID, Registration Date, Username, Category, Email, InfoStatus
                                var row_data = new User(Convert.ToInt32(row[0]), Convert.ToString(row[1]), Convert.ToString(row[2]), User.GetCategory(Convert.ToString(row[3])), Util.Util.DecryptString(Convert.ToString(row[4])), Management.GetInfoStatusStr(Convert.ToString(row[5])));
                                Management.Users.Add(row_data);
                            }
                            Management.Users.Sort((a, b) => b.RegistrationDate.CompareTo(a.RegistrationDate));
                        }
                        else if (values[0][0] != null && values[0][0].ToString().StartsWith("MachineID"))
                        {
                            for (int i = 1; i < values.Count; i++)
                            {
                                var row = values[i];
                                if (row == null || row.Count == 0) continue;
                                //MachineID, Registration Date, MachineName, HasEvaluation, HasFAIMS, HasOT, HasIT, CalibrationTime, FullCalibrationTime, InfoStatus, IntervalTime
                                var row_data = new Machine(Convert.ToInt32(row[0]), Convert.ToString(row[1]), Convert.ToString(row[2]), Convert.ToBoolean(row[3]), Convert.ToBoolean(row[4]), Convert.ToBoolean(row[5]), Convert.ToBoolean(row[6]), Convert.ToString(row[7]), Convert.ToString(row[8]), Management.GetInfoStatusStr(Convert.ToString(row[9])), Convert.ToInt32(row[10]));
                                Management.Machines.Add(row_data);
                            }
                            Management.Machines.Sort((a, b) => b.RegistrationDate.CompareTo(a.RegistrationDate));
                        }
                        else if (values[0][0] != null && values[0][0].ToString().StartsWith("ProjectID"))
                        {
                            for (int i = 1; i < values.Count; i++)
                            {
                                var row = values[i];
                                if (row == null || row.Count == 0) continue;
                                //ID, Date, Project name, Amount of MS time (hours), Number of Samples, Method, Injection volume, Machine(s), Comments
                                var row_data = new Project(Convert.ToInt32(row[0]), Convert.ToString(row[1]), Convert.ToString(row[2]), Convert.ToString(row[3]), Convert.ToString(row[4]), Convert.ToString(row[5]), Convert.ToString(row[6]), Project.GetFAIMS(Convert.ToString(row[7])), Convert.ToString(row[8]), Convert.ToString(row[9]), Convert.ToString(row[10]), Project.GetStatus(Convert.ToString(row[11])), Management.GetInfoStatusStr(Convert.ToString(row[12])));
                                Management.Projects.Add(row_data);
                            }
                            Management.Projects.Sort((a, b) => b.RegistrationDate.CompareTo(a.RegistrationDate));
                        }
                        else
                        {
                            byte _property_key = 0;
                            foreach (var machine_prop in machine_properties)
                            {
                                string machine = "";
                                if (machine_prop.EndsWith("Queue_Date"))
                                {
                                    machine = machine_prop.Replace("Queue_Date", "");
                                    _property_key = 0;
                                }
                                else if (machine_prop.EndsWith("EvaluationID"))
                                {
                                    machine = machine_prop.Replace("EvaluationID", "");
                                    _property_key = 1;
                                }
                                else if (machine_prop.EndsWith("LogID"))
                                {
                                    machine = machine_prop.Replace("LogID", "");
                                    _property_key = 2;
                                }

                                if ((_property_key == 0 || _property_key == 2) &&
                                    values[0][0] != null && values[0][0].ToString().StartsWith(machine_prop))
                                {
                                    //Queue or Log
                                    (List<MachineQueue> queue, List<Run> evaluation, List<MachineLog> log) properties;
                                    if (Management.Machines_Properties.ContainsKey(machine))
                                        Management.Machines_Properties.TryGetValue(machine, out properties);
                                    else
                                    {
                                        Management.Machines_Properties.Add(machine, (new(), new(), new()));
                                        properties = Management.Machines_Properties[machine];
                                    }

                                    if (_property_key == 0)//Queue
                                    {
                                        for (int i = 1; i < values.Count; i++)
                                        {
                                            var row = values[i];
                                            if (row == null || row.Count == 0) continue;
                                            //Date, Sample, Amount of MS time (hours)
                                            var row_data = new MachineQueue(Convert.ToString(row[0]), Convert.ToString(row[1]), Convert.ToString(row[2]), Convert.ToString(row[3]), Convert.ToString(row[4]), Convert.ToString(row[5]), Convert.ToInt32(row[6]), Convert.ToString(row[7]), Management.GetInfoStatusStr(Convert.ToString(row[8])));
                                            properties.queue.Add(row_data);
                                        }
                                        break;
                                    }
                                    else if (_property_key == 2)
                                    {
                                        string[] header = values[0].Select(a => a.ToString()).ToArray();
                                        for (int i = 1; i < values.Count; i++)
                                        {
                                            var row = values[i];
                                            if (row == null || row.Count == 0) continue;
                                            //ID, Date, Operator (s), Remarks, Column Lot number, LC, IsPartialCalibrated, IsFullyCalibrated, TechnicalReport, AddedBy, InfoStatus

                                            int _id = Convert.ToInt32(row[0]);
                                            string date_str = Convert.ToString(row[1]);

                                            int index = Array.IndexOf(header, "Operator");
                                            string _operator = "";
                                            if (index != -1)
                                                _operator = Convert.ToString(row[index]);

                                            index = Array.IndexOf(header, "Remarks");
                                            string remarks = "";
                                            if (index != -1)
                                                remarks = Convert.ToString(row[index]);

                                            index = Array.IndexOf(header, "ColumnLotNumber");
                                            string columnLotNumber = "";
                                            if (index != -1)
                                                columnLotNumber = Convert.ToString(row[index]);

                                            index = Array.IndexOf(header, "LC (4%B, 50 °C, 0.25µL/min)");
                                            string lc = "";
                                            if (index != -1)
                                                lc = Convert.ToString(row[index]);

                                            index = Array.IndexOf(header, "IsCalibrated");
                                            string isCalibrated = "";
                                            if (index != -1)
                                                isCalibrated = Convert.ToString(row[index]);

                                            index = Array.IndexOf(header, "IsFullyCalibrated");
                                            string isFullyCalibrated = "";
                                            if (index != -1)
                                                isFullyCalibrated = Convert.ToString(row[index]);

                                            index = Array.IndexOf(header, "TechnicalReport");
                                            string technicalReport = "";
                                            if (index != -1)
                                                technicalReport = Convert.ToString(row[index]);

                                            index = Array.IndexOf(header, "AddedBy");
                                            string addedBy = "";
                                            if (index != -1)
                                                addedBy = Convert.ToString(row[index]);

                                            index = Array.IndexOf(header, "InfoStatus");
                                            string infoStatus = "";
                                            if (index != -1)
                                                infoStatus = Convert.ToString(row[index]);

                                            //"LC (4%B, 50 °C, 0.25µL/min)", "IsCalibrated", "IsFullyCalibrated", "TechnicalReport", "AddedBy", "InfoStatus" };
                                            var row_data = new MachineLog(_id, date_str, _operator, remarks, columnLotNumber, lc, isCalibrated, isFullyCalibrated, technicalReport, addedBy, Management.GetInfoStatusStr(infoStatus));
                                            properties.log.Add(row_data);
                                        }
                                        properties.log.Sort((a, b) => b.RegistrationDate.CompareTo(a.RegistrationDate));
                                        break;
                                    }
                                }
                                else if (values.Count > 1 && values[1][0] != null && values[1][0].ToString().StartsWith(machine_prop))
                                {
                                    (List<MachineQueue> queue, List<Run> evaluation, List<MachineLog> log) properties;
                                    if (Management.Machines_Properties.ContainsKey(machine))
                                        Management.Machines_Properties.TryGetValue(machine, out properties);
                                    else
                                    {
                                        Management.Machines_Properties.Add(machine, (new(), new(), new()));
                                        properties = Management.Machines_Properties[machine];
                                    }
                                    if (_property_key == 1)//Evaluation
                                    {
                                        for (int i = 2; i < values.Count; i++)
                                        {
                                            var row = values[i];
                                            double xreaOT = 0;
                                            double xreaIT = 0;
                                            try
                                            {
                                                xreaOT = Convert.ToDouble(row[15]);
                                                xreaIT = Convert.ToDouble(row[31]);
                                            }
                                            catch (Exception) { }

                                            Dictionary<string, int> mostAbundantPeptsOT = new();
                                            Dictionary<string, int> mostAbundantPeptsIT = new();
                                            try
                                            {
                                                mostAbundantPeptsOT = Q2C.Control.FileManagement.Serializer.FromJson<Dictionary<string, int>>(Convert.ToString(row[17]));
                                                mostAbundantPeptsIT = Q2C.Control.FileManagement.Serializer.FromJson<Dictionary<string, int>>(Convert.ToString(row[33]));
                                            }
                                            catch (Exception) { }
                                            var sub_run_dataOT = new SubRun(Convert.ToString(row[4]), Convert.ToBoolean(row[5]), Convert.ToDouble(row[6]), Convert.ToDouble(row[7]), Convert.ToDouble(row[8]), Convert.ToDouble(row[9]), Convert.ToDouble(row[10]), Convert.ToDouble(row[11]), Convert.ToDouble(row[12]), Convert.ToString(row[13]), Convert.ToDouble(row[14]), xreaOT, Convert.ToString(row[16]), Convert.ToBoolean(row[18]), Convert.ToString(row[19]), Convert.ToInt32(row[0]), mostAbundantPeptsOT);
                                            var sub_run_dataIT = new SubRun(Convert.ToString(row[20]), Convert.ToBoolean(row[21]), Convert.ToDouble(row[22]), Convert.ToDouble(row[23]), Convert.ToDouble(row[24]), Convert.ToDouble(row[25]), Convert.ToDouble(row[26]), Convert.ToDouble(row[27]), Convert.ToDouble(row[28]), Convert.ToString(row[29]), Convert.ToDouble(row[30]), xreaIT, Convert.ToString(row[32]), Convert.ToBoolean(row[34]), Convert.ToString(row[35]), Convert.ToInt32(row[0]), mostAbundantPeptsIT);
                                            var row_data = new Run(Convert.ToInt32(row[0]), sub_run_dataOT, sub_run_dataIT, Convert.ToString(row[1]), Convert.ToString(row[2]), Convert.ToString(row[3]), Convert.ToString(row[36]), Management.GetInfoStatusStr(Convert.ToString(row[37])));
                                            properties.evaluation.Add(row_data);
                                        }
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }

                if (isInitial)
                    ReadSheets(false, tab_name);
            }
            catch (System.FormatException e)
            {
                if (e.Message.Contains("was not recognized as a valid"))
                {
                    System.Windows.MessageBox.Show(
                                            "The database is not recognized.\nPlease contact the administrator.",
                                            "Q2C :: Error",
                                            (System.Windows.MessageBoxButton)MessageBoxButtons.OK,
                                            (System.Windows.MessageBoxImage)MessageBoxIcon.Error);

                    Connection.RevokeConnection();
                    Management.ResetDatabase();
                    System.Environment.Exit(1);
                }
            }
            catch (Exception e)
            {
                throw;
            }
        }

        private static void CreateNewColumn(int column_index, string column_name, string tab_name, bool isEvaluation = false)
        {
            // Retrieve the spreadsheet properties to get the sheet ID
            SpreadsheetsResource.GetRequest getSheetRequest = service_sheets.Spreadsheets.Get(SpreadsheetId);
            Spreadsheet spreadsheet = getSheetRequest.Execute();
            Google.Apis.Sheets.v4.Data.Sheet? sheet = spreadsheet.Sheets.Where(a => a.Properties.Title == tab_name).FirstOrDefault();
            int sheetId = (int)sheet.Properties.SheetId;

            //Insert column
            var insertColumnRequest = new Request()
            {
                InsertDimension = new InsertDimensionRequest()
                {
                    Range = new DimensionRange()
                    {
                        SheetId = sheetId,
                        Dimension = "COLUMNS",
                        StartIndex = column_index,
                        EndIndex = column_index + 1
                    },
                    InheritFromBefore = true,
                }
            };

            var batchUpdateRequest = new BatchUpdateSpreadsheetRequest
            {
                Requests = new[] { insertColumnRequest }
            };
            // Performing Update Operation...
            try
            {
                service_sheets.Spreadsheets.BatchUpdate(batchUpdateRequest, SpreadsheetId).Execute();
            }
            catch (Exception)
            { }


            //Insert column_name
            string range = "";
            if (isEvaluation)
                range = $"{tab_name}!{Util.Util.GetLetter(column_index)}2";
            else
                range = $"{tab_name}!{Util.Util.GetLetter(column_index)}1";
            var valueRange = new ValueRange
            {
                Values = new List<IList<object>> { new List<object> { column_name } }
            };

            // Performing Update Operation...
            try
            {
                var updateRequest = service_sheets.Spreadsheets.Values.Update(valueRange, SpreadsheetId, range);
                updateRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;
                var appendReponse = updateRequest.Execute();
            }
            catch (Exception e)
            { }

        }

        internal static string[] ProcessHeaderCSV(string row)
        {
            row = Regex.Replace(row, "\"", "");
            string[] headerLine = Regex.Split(row, ",");
            if (headerLine.Length > 1)
            {
                for (int i = 0; i < headerLine.Length; i++)
                {
                    headerLine[i] = Regex.Replace(headerLine[i], "\"", "");
                }

            }
            else
            {
                headerLine = Regex.Split(row, ",");
            }
            return headerLine;
        }
        public static string[] GetCategories(string _property, string machine = "")
        {
            if (_property.Contains("Queue"))
                return new string[] { $"{machine}Queue_Date", "Sample name", "Amount of MS time (hours)", "Number of Samples", "FAIMS", "Method", "ProjectID", "AddedBy", "InfoStatus" };
            else if (_property.Contains("Log"))
                return new string[] { $"{machine}LogID", $"{machine}Log_Date", "Operator", "Remarks", "ColumnLotNumber", "LC (4%B, 50 °C, 0.25µL/min)", "IsCalibrated", "IsFullyCalibrated", "TechnicalReport", "AddedBy", "InfoStatus" };
            else if (_property.Contains("Evaluation"))
                return new string[] { $"{machine}EvaluationID", "Operator", "Comments", "Column lot number", "OT_Date", "FAIMS", "MS1 Intensity", "MS2 Intensity", "Protein Groups", "Peptide Groups", "PSMs", "MS/MS", "ID ratio", "Mass Error (ppm)", "Mass Error Median (ppm)", "XreaMean", "Xreas", "MostAbundantPepts", "Exclude", "RAW File", "IT_Date", "FAIMS", "MS1 Intensity", "MS2 Intensity", "Protein Groups", "Peptide Groups", "PSMs", "MS/MS", "ID ratio", "Mass Error (ppm)", "Mass Error Median (ppm)", "XreaMean", "Xreas", "MostAbundantPepts", "Exclude", "RAW File", "AddedBy", "InfoStatus" };
            else
            {
                switch (_property)
                {
                    case "Info":
                        return new string[] { "Creation_Date", "Q2C_Version" };
                    case "Users":
                        return new string[] { "UserID", "User_Date", "Username", "Category", "Email", "InfoStatus" };
                    case "Machines":
                        return new string[] { "MachineID", "Machine_Date", "Name", "HasEvaluation", "HasFAIMS", "HasOT", "HasIT", "CalibrationTime", "FullCalibrationTime", "InfoStatus", "IntervalTime" };
                    case "Projects":
                        return new string[] { "ProjectID", "Project_Date", "Sample name", "Amount of MS time (hours)", "Number of Samples", "Method", "Machine", "FAIMS", "Receive Notification", "Comments", "AddedBy", "Status", "InfoStatus" };
                }
            }
            return null;
        }
        private static string GetLastColumnCategory(string _property, string machine = "")
        {
            object[] categories = GetCategories(_property, machine);
            return Util.Util.GetLetter(categories.Length - 1);
        }
        public static void CreateNewTabIntoSpreadsheet(string sheetTitle, object[] categories)
        {
            string range = sheetTitle + "!A1:" + Util.Util.GetLetter(categories.Length - 1) + "1";
            var valueRange = new ValueRange();
            var oblist = categories.ToList();
            valueRange.Values = new List<IList<object>> { oblist };

            try
            {
                // Create a new sheet properties
                Google.Apis.Sheets.v4.Data.SheetProperties newSheetProperties = new Google.Apis.Sheets.v4.Data.SheetProperties
                {
                    Title = sheetTitle
                };

                // Add the new sheet to the spreadsheet
                Request addSheetRequest = new Request
                {
                    AddSheet = new AddSheetRequest
                    {
                        Properties = newSheetProperties
                    }
                };

                BatchUpdateSpreadsheetRequest batchUpdateRequest = new BatchUpdateSpreadsheetRequest
                {
                    Requests = new List<Request> { addSheetRequest }
                };

                //Create sheet
                SpreadsheetsResource.BatchUpdateRequest request = service_sheets.Spreadsheets.BatchUpdate(batchUpdateRequest, SpreadsheetId);
                BatchUpdateSpreadsheetResponse response = request.Execute();

                // Append the above record...
                var appendRequest = service_sheets.Spreadsheets.Values.Append(valueRange, SpreadsheetId, range);
                appendRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.AppendRequest.ValueInputOptionEnum.USERENTERED;
                var appendReponse = appendRequest.Execute();

                //Get newSheetID
                // Retrieve the spreadsheet properties to get the sheet ID
                SpreadsheetsResource.GetRequest getSheetRequest = service_sheets.Spreadsheets.Get(SpreadsheetId);
                Spreadsheet spreadsheet = getSheetRequest.Execute();
                Google.Apis.Sheets.v4.Data.Sheet? sheet = spreadsheet.Sheets.LastOrDefault();
                int sheetId = (int)sheet.Properties.SheetId;
                //Freeze 1st row
                // Create a request to update sheet properties for freezing the first row
                var sheetPropertiesRequest = new UpdateSheetPropertiesRequest
                {
                    Properties = new Google.Apis.Sheets.v4.Data.SheetProperties
                    {
                        SheetId = sheetId,
                        GridProperties = new GridProperties
                        {
                            FrozenRowCount = 1 // Set the number of rows to freeze (in this case, 1)
                        }
                    },
                    Fields = "GridProperties.FrozenRowCount"
                };

                // Create a batch update request
                batchUpdateRequest = new BatchUpdateSpreadsheetRequest
                {
                    Requests = new List<Request>
                    {
                        new Request
                        {
                            UpdateSheetProperties = sheetPropertiesRequest
                        }
                    }
                };

                // Execute the request to freeze the first row
                service_sheets.Spreadsheets.BatchUpdate(batchUpdateRequest, SpreadsheetId).Execute();

            }
            catch (Exception e)
            {
                Thread.Sleep(OFFSET_TIME_REFRESH_MILLISECONDS);
                CreateNewTabIntoSpreadsheet(sheetTitle, categories);
            }
        }
        public static void RemoveTabInSpreadsheet(int sheet_index)
        {
            // Create a request to delete the sheet
            var deleteSheetRequest = new DeleteSheetRequest
            {
                SheetId = sheet_index
            };

            // Create a batch update request
            var batchUpdateRequest = new BatchUpdateSpreadsheetRequest
            {
                Requests = new List<Request>
                {
                    new Request
                    {
                        DeleteSheet = deleteSheetRequest
                    }
                }
            };

            // Execute the request to remove the sheet
            service_sheets.Spreadsheets.BatchUpdate(batchUpdateRequest, SpreadsheetId).Execute();
        }
        private static void CreateEvaluationTabIntoSpreadSheet(string sheetTitle, object[] categories)
        {
            string range = sheetTitle + "!A1:" + Util.Util.GetLetter(categories.Length - 1) + "1";
            var valueRange = new ValueRange();
            var oblist1 = new List<object>();
            foreach (object category in categories)
            {
                if (category != null)
                {
                    if (category == "OT_Date")
                        oblist1.Add("OT");
                    else if (category == "IT_Date")
                        oblist1.Add("IT");
                    else
                        oblist1.Add("");
                }
            }
            var oblist2 = categories.ToList();
            valueRange.Values = new List<IList<object>> { oblist1, oblist2 };

            try
            {
                // Create a new sheet properties
                Google.Apis.Sheets.v4.Data.SheetProperties newSheetProperties = new Google.Apis.Sheets.v4.Data.SheetProperties
                {
                    Title = sheetTitle
                };

                // Add the new sheet to the spreadsheet
                Request addSheetRequest = new Request
                {
                    AddSheet = new AddSheetRequest
                    {
                        Properties = newSheetProperties
                    }
                };

                BatchUpdateSpreadsheetRequest batchUpdateRequest = new BatchUpdateSpreadsheetRequest
                {
                    Requests = new List<Request> { addSheetRequest }
                };

                SpreadsheetsResource.BatchUpdateRequest request = service_sheets.Spreadsheets.BatchUpdate(batchUpdateRequest, SpreadsheetId);
                BatchUpdateSpreadsheetResponse response = request.Execute();

                //Get newSheetID
                // Retrieve the spreadsheet properties to get the sheet ID
                SpreadsheetsResource.GetRequest getSheetRequest = service_sheets.Spreadsheets.Get(SpreadsheetId);
                Spreadsheet spreadsheet = getSheetRequest.Execute();
                Google.Apis.Sheets.v4.Data.Sheet? sheet = spreadsheet.Sheets.LastOrDefault();
                int sheetId = (int)sheet.Properties.SheetId;

                // Create the merge request to merge cells E1 to Q1
                MergeCellsRequest mergeRequest1 = new MergeCellsRequest
                {
                    Range = new GridRange
                    {
                        SheetId = sheetId, // Assuming the sheet you're working on has a SheetId of 0
                        StartRowIndex = 0,
                        EndRowIndex = 1, // Since you want to merge a single row
                        StartColumnIndex = 4,
                        EndColumnIndex = 19
                    },
                };

                Request mergeCellsRequest1 = new Request
                {
                    MergeCells = mergeRequest1
                };
                // Create the merge request to merge cells R1 to AD1
                MergeCellsRequest mergeRequest2 = new MergeCellsRequest
                {
                    Range = new GridRange
                    {
                        SheetId = sheetId, // Assuming the sheet you're working on has a SheetId of 0
                        StartRowIndex = 0,
                        EndRowIndex = 1, // Since you want to merge a single row
                        StartColumnIndex = 20,
                        EndColumnIndex = 35
                    },
                };

                Request mergeCellsRequest2 = new Request
                {
                    MergeCells = mergeRequest2
                };

                //Freeze 1st and 2nd rows
                // Create a request to update sheet properties for freezing the first and second rows
                var sheetPropertiesRequest = new UpdateSheetPropertiesRequest
                {
                    Properties = new Google.Apis.Sheets.v4.Data.SheetProperties
                    {
                        SheetId = sheetId,
                        GridProperties = new GridProperties
                        {
                            FrozenRowCount = 2 // Set the number of rows to freeze (in this case, 1)
                        }
                    },
                    Fields = "GridProperties.FrozenRowCount"
                };
                Request freezeRowsRequest = new Request
                {
                    UpdateSheetProperties = sheetPropertiesRequest
                };

                batchUpdateRequest = new BatchUpdateSpreadsheetRequest
                {
                    Requests = new List<Request> { mergeCellsRequest1, mergeCellsRequest2, freezeRowsRequest }
                };

                // Append the above record...
                var appendRequest = service_sheets.Spreadsheets.Values.Append(valueRange, SpreadsheetId, range);
                appendRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.AppendRequest.ValueInputOptionEnum.USERENTERED;
                var appendReponse = appendRequest.Execute();
                // Execute the request to merge cells
                request = service_sheets.Spreadsheets.BatchUpdate(batchUpdateRequest, SpreadsheetId);
                response = request.Execute();
            }
            catch (Exception e)
            {
                Thread.Sleep(OFFSET_TIME_REFRESH_MILLISECONDS);
                CreateEvaluationTabIntoSpreadSheet(sheetTitle, categories);
            }
        }

        #region Project methods

        /// <summary>
        /// Method responsible for adding or updating a project
        /// </summary>
        /// <param name="project"></param>
        /// <returns>0: sucess; 1: new project must has 'wait for acquisition' as status; 2: project has not found on the machine queue; 3: project has found on the machine queue, delete it from there; 4: failed</returns>
        public static byte AddOrUpdateProject(Project project)
        {
            if (project == null) return 4;

            #region read projects
            if (IsOnline)
                //Double check ProjectID
                ReadSheets(false, "Projects");
            #endregion

            Management.Projects.Sort((a, b) => a.Id.CompareTo(b.Id));

            bool isValid = true;
            if (project.Id == -1)// ADD
            {
                project.Id = getNewProjectID();
                if (project.Status != ProjectStatus.WaitForAcquisition)
                    return 1;
                isValid = AddProject(project);
            }
            else// UPDATE
            {
                if (CheckProjectOnTheQueue(project))
                    return 3;
                else
                {
                    if (project.Status != ProjectStatus.WaitForAcquisition)
                        return 2;
                    else
                        isValid = UpdateProject(project);
                }
            }

            Management.Projects.Sort((a, b) => b.RegistrationDate.CompareTo(a.RegistrationDate));

            if (isValid)
                return 0;
            else
                return 4;
        }

        private static bool CheckProjectOnTheQueue(Project project)
        {
            if (project.GetMachines.Length > 1) return false;

            (List<MachineQueue> queue, List<Model.Run> evaluation, List<MachineLog> log) prop;
            Management.Machines_Properties.TryGetValue(project.Machine, out prop);
            if (prop == (null, null, null))
                return false;

            if (prop.queue != null &&
                        prop.queue.Count > 0 &&
                        prop.queue.Where(a => a.ProjectID == project.Id && a.InfoStatus == Management.InfoStatus.Active).FirstOrDefault() != null)
                return true;

            return false;
        }

        private static int getNewProjectID()
        {
            if (Management.Projects != null)
            {
                if (Management.Projects.Count == 0) return 0;
                return Management.Projects[Management.Projects.Count - 1].Id + 1;
            }
            else
                return -1;
        }

        public static bool AddProject(Project project)
        {
            if (IsOnline) return AddProjectOnline(project);
            else return AddProjectOffline(project);
        }
        private static bool AddProjectOnline(Project project)
        {
            // Specifying Column Range for reading...
            var range = "Projects!A:" + GetLastColumnCategory("Projects");
            var valueRange = new ValueRange();
            var oblist = new List<object>() { project.Id, project.RegistrationDateStr, project.ProjectName, project.AmountMS, project.NumberOfSamples, project.Method, project.Machine, project._faims, project._receiveNotification, project.Comments, project.AddedBy, project._status, project._infoStatus };
            valueRange.Values = new List<IList<object>> { oblist };

            try
            {
                // Append the above record...
                var appendRequest = service_sheets.Spreadsheets.Values.Append(valueRange, SpreadsheetId, range);
                appendRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.AppendRequest.ValueInputOptionEnum.USERENTERED;
                var appendReponse = appendRequest.Execute();
            }
            catch (Exception e)
            {
                Thread.Sleep(OFFSET_TIME_REFRESH_MILLISECONDS);
                AddProjectOnline(project);
            }
            return true;
        }
        private static bool AddProjectOffline(Project project)
        {
            using (var db = CreateDatabase())
            {
                db.GetCollection<Project>().Insert(project);
                db.Commit();
            }
            return true;
        }

        public static bool UpdateProject(Project project)
        {
            if (IsOnline) return UpdateProjectOnline(project);
            else return UpdateProjectOffline(project);

        }
        private static bool UpdateProjectOnline(Project project)
        {
            // Specifying Column Range for reading...
            var range = "Projects!C" + (project.Id + 2) + ":" + GetLastColumnCategory("Projects") + (project.Id + 2);
            var valueRange = new ValueRange();
            var oblist = new List<object>() { project.ProjectName, project.AmountMS, project.NumberOfSamples, project.Method, project.Machine, project._faims, project.ReceiveNotification, project.Comments, project.AddedBy, project._status, project._infoStatus };
            valueRange.Values = new List<IList<object>> { oblist };

            // Performing Update Operation...
            try
            {
                var updateRequest = service_sheets.Spreadsheets.Values.Update(valueRange, SpreadsheetId, range);
                updateRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;
                var appendReponse = updateRequest.Execute();
            }
            catch (Exception e)
            {
                Thread.Sleep(OFFSET_TIME_REFRESH_MILLISECONDS);
                UpdateProjectOnline(project);
            }
            return true;
        }
        private static bool UpdateProjectOffline(Project project)
        {
            using (var db = CreateDatabase())
            {
                db.GetCollection<Project>().Update(project);
            }
            return true;
        }

        private static MachineQueue? GetMQ(string machine, MachineQueue mq)
        {
            (List<MachineQueue> queue, List<Model.Run> evaluation, List<MachineLog> log) prop;
            Management.Machines_Properties.TryGetValue(machine, out prop);
            if (prop == (null, null, null))
                return null;

            return prop.queue.Where(a => a.ProjectName == mq.ProjectName &&
                a.AmountMS == mq.AmountMS && a.NumberOfSamples.Equals(mq.NumberOfSamples) && a.InfoStatus == Management.InfoStatus.Active).FirstOrDefault();
        }

        public static bool RemoveProjectFromQueue(string sheet, MachineQueue mq, bool hasMeasured)
        {
            if (Management.Projects == null) return false;

            MachineQueue? current_mq = null;
            if (mq.ProjectID == -1)
                current_mq = GetMQ(sheet, mq);
            else
                current_mq = mq;
            if (current_mq == null) return false;

            if (!DeleteProjectFromQueue(sheet, current_mq)) return false;

            Project? current_project = Management.Projects.Where(a => a.Id == mq.ProjectID && a.InfoStatus == Management.InfoStatus.Active).FirstOrDefault();
            if (current_project != null)
            {
                if (hasMeasured)
                    current_project.Status = ProjectStatus.Measured;
                else
                    current_project.Status = ProjectStatus.WaitForAcquisition;

                UpdateProject(current_project);

                if (current_project.ReceiveNotification && hasMeasured)
                    SendEmailToUser(current_project, false);

                return true;
            }
            return false;
        }

        private static bool DeleteProjectFromQueue(string sheet, MachineQueue mq)
        {
            int machine_queue_index = -1;
            (List<MachineQueue> queue, List<Model.Run> evaluation, List<MachineLog> log) prop;
            Management.Machines_Properties.TryGetValue(sheet, out prop);
            if (prop == (null, null, null))
                return false;

            machine_queue_index = prop.queue.FindIndex(a => a.ProjectID == mq.ProjectID && a.InfoStatus == Management.InfoStatus.Active);
            if (machine_queue_index == -1) return false;

            if (IsOnline) return DeleteProjectFromQueueOnline(sheet, mq, machine_queue_index);
            else return DeleteProjectFromQueueOffline(sheet, mq);
        }
        private static bool DeleteProjectFromQueueOnline(string sheet, MachineQueue mq, int machine_queue_index)
        {
            // Specifying Column Range for reading...
            var range = $"{sheet}_Queue!" + GetLastColumnCategory("Queue", sheet) + (machine_queue_index + 2) + ":" + GetLastColumnCategory("Queue", sheet) + (machine_queue_index + 2);
            var valueRange = new ValueRange();
            var oblist = new List<object>() { "Deleted" };
            valueRange.Values = new List<IList<object>> { oblist };

            Console.WriteLine($"INFO: DeleteProjectFromQueue - {range}");
            // Performing Update Operation...
            try
            {
                var updateRequest = service_sheets.Spreadsheets.Values.Update(valueRange, SpreadsheetId, range);
                updateRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;
                var appendReponse = updateRequest.Execute();
            }
            catch (Exception e)
            {
                Console.WriteLine($"ERROR: DeleteProjectFromQueue - {range}\n{e.Message}");
                Thread.Sleep(OFFSET_TIME_REFRESH_MILLISECONDS);
                DeleteProjectFromQueueOnline(sheet, mq, machine_queue_index);
            }
            return true;
        }
        private static bool DeleteProjectFromQueueOffline(string sheet, MachineQueue mq)
        {
            using (var db = CreateDatabase())
            {
                mq.InfoStatus = Management.InfoStatus.Deleted;
                db.GetCollection<MachineQueue>($"{sheet}_Queue").Update(mq);
            }
            return true;
        }

        public static bool RemoveProject(Project project)
        {
            if (project == null) return false;

            if (IsOnline) return RemoveProjectOnline(project);
            else return RemoveProjectOffline(project);

        }
        private static bool RemoveProjectOnline(Project project)
        {
            #region read projects
            //Double check ProjectID
            ReadSheets(false, "Projects");
            #endregion

            // Specifying Column Range for reading...
            var range = "Projects!" + GetLastColumnCategory("Projects") + (project.Id + 2) + ":" + GetLastColumnCategory("Projects") + (project.Id + 2);
            var valueRange = new ValueRange();
            List<object> oblist = new List<object>() { "Deleted" };
            valueRange.Values = new List<IList<object>> { oblist };

            // Performing Update Operation...
            try
            {
                var updateRequest = service_sheets.Spreadsheets.Values.Update(valueRange, SpreadsheetId, range);
                updateRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;
                var appendReponse = updateRequest.Execute();
            }
            catch (Exception e)
            {
                Thread.Sleep(OFFSET_TIME_REFRESH_MILLISECONDS);
                RemoveProjectOnline(project);
            }
            return true;
        }
        private static bool RemoveProjectOffline(Project project)
        {
            using (var db = CreateDatabase())
            {
                project.InfoStatus = Management.InfoStatus.Deleted;
                db.GetCollection<Project>().Update(project);
            }
            return true;
        }

        public static bool AddProjectToQueue(string sheet, Project project)
        {
            #region read projects
            if (IsOnline)
            {
                //Double check ProjectID
                ReadSheets(false, "Projects");
                ReadSheets(false, "Machines");
            }
            #endregion

            SaveProjectToQueue(sheet, project);
            //Projects.Sort((a, b) => a.ID.CompareTo(b.ID));
            //int project_index = Projects.FindIndex(a => a.ProjectName.Equals(project.ProjectName) &&
            //    a.AmountMS.Equals(project.AmountMS) &&
            //    a.NumberOfSamples.Equals(project.NumberOfSamples));

            //Projects.Sort((a, b) => b.RegistrationDate.CompareTo(a.RegistrationDate));

            project.Status = ProjectStatus.InProgress;
            //if (project_index != -1)
            UpdateProject(project);

            if (IsOnline && project.ReceiveNotification)
                SendEmailToUser(project);

            return true;
        }

        private static bool SaveProjectToQueue(string sheet, Project project)
        {
            if (IsOnline) return SaveProjectToQueueOnline(sheet, project);
            else return SaveProjectToQueueOffline(sheet, project);
        }
        private static bool SaveProjectToQueueOnline(string sheet, Project project)
        {
            // Specifying Column Range for reading...
            var range = $"{sheet}_Queue!A:" + GetLastColumnCategory("Queue", sheet);
            var valueRange = new ValueRange();
            var oblist = new List<object>() { project.RegistrationDateStr, project.ProjectName, project.AmountMS, project.NumberOfSamples, project._faims, project.Method, project.Id, Management.GetComputerUser(), project._infoStatus };
            valueRange.Values = new List<IList<object>> { oblist };

            try
            {
                // Append the above record...
                var appendRequest = service_sheets.Spreadsheets.Values.Append(valueRange, SpreadsheetId, range);
                appendRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.AppendRequest.ValueInputOptionEnum.USERENTERED;
                var appendReponse = appendRequest.Execute();
            }
            catch (Exception e)
            {
                Thread.Sleep(OFFSET_TIME_REFRESH_MILLISECONDS);
                SaveProjectToQueueOnline(sheet, project);
            }
            return true;
        }
        private static bool SaveProjectToQueueOffline(string sheet, Project project)
        {
            using (var db = CreateDatabase())
            {
                var mq = new MachineQueue(project.RegistrationDateStr, project.ProjectName, project.AmountMS, project.NumberOfSamples, project._faims, project.Method, project.Id, Management.GetComputerUser(), project.InfoStatus);
                db.GetCollection<MachineQueue>($"{sheet}_Queue").Insert(mq);
                db.Commit();
            }
            return true;
        }
        #endregion

        #region Info
        public static bool UpdateInfo(string[] values)
        {
            // Specifying Column Range for reading...
            var range = $"Info!A2:B2";
            var valueRange = new ValueRange();
            //values[0]: Registration Date;
            //values[1]: Q2C_Version (format: 1.0.0)
            var oblist = new List<object>() { values[0], values[1] };
            valueRange.Values = new List<IList<object>> { oblist };

            // Performing Update Operation...
            try
            {
                var updateRequest = service_sheets.Spreadsheets.Values.Update(valueRange, SpreadsheetId, range);
                updateRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;
                var appendReponse = updateRequest.Execute();
            }
            catch (Exception e)
            {
                Thread.Sleep(OFFSET_TIME_REFRESH_MILLISECONDS);
                UpdateInfo(values);
            }
            return true;
        }
        #endregion

        #region Run methods

        private static int getNewEvaluationID(string machine)
        {
            (List<MachineQueue> queue, List<Model.Run> evaluation, List<MachineLog> log) prop;
            Management.Machines_Properties.TryGetValue(machine, out prop);
            if (prop == (null, null, null))
                return -1;

            if (prop.evaluation != null)
            {
                prop.evaluation.Sort((a, b) => a.Id.CompareTo(b.Id));
                if (prop.evaluation.Count == 0) return 0;
                return prop.evaluation[prop.evaluation.Count - 1].Id + 1;
            }
            else return -1;
        }
        public static bool AddOrUpdateRun(Run run, string machine)
        {
            if (run == null)
                return false;

            #region read machines
            //Double check EvaluationID
            if (IsOnline)
                ReadSheets(false, "Machines", machine);
            #endregion

            if (run.Id == -1)
            {
                run.Id = getNewEvaluationID(machine);
                if (run.OT != null) run.OT.RunID = run.Id;
                if (run.IT != null) run.IT.RunID = run.Id;
                return AddRun(run, machine);
            }
            else
                return UpdateRun(run, machine);
        }

        public static bool AddRun(Run run, string machine)
        {
            if (IsOnline) return AddRunOnline(run, machine);
            else return AddRunOffline(run, machine);

        }
        private static bool AddRunOnline(Run run, string machine)
        {
            // Specifying Column Range for reading...
            var range = $"{machine}_Evaluation!A:" + GetLastColumnCategory("Evaluation");
            var valueRange = new ValueRange();
            string mostAbundantPeptsOT = string.Empty;
            string mostAbundantPeptsIT = string.Empty;
            try
            {
                mostAbundantPeptsOT = Q2C.Control.FileManagement.Serializer.ToJSON(run.OT.MostAbundantPepts, false);
                mostAbundantPeptsIT = Q2C.Control.FileManagement.Serializer.ToJSON(run.IT.MostAbundantPepts, false);
            }
            catch (Exception) { }
            var oblist = new List<object>()
            {
                run.Id,
                run.Operator,
                run.Comments,
                run.ColumnLotNumber,

                run.OT != null && run.OT.RegistrationDateStr!= null ? run.OT.RegistrationDateStr: "",
                run.OT != null && run.OT.FAIMS!= null ? run.OT.FAIMS: "",
                run.OT != null ? run.OT.MS1Intensity.ToString(CultureInfo.InvariantCulture): "",
                run.OT != null ? run.OT.MS2Intensity.ToString(CultureInfo.InvariantCulture): "",
                run.OT != null ? run.OT.ProteinGroup: "",
                run.OT != null ? run.OT.PeptideGroup: "",
                run.OT != null ? run.OT.PSM:"",
                run.OT != null ? run.OT.MSMS: "",
                run.OT != null ? run.OT.IDRatio.ToString(CultureInfo.InvariantCulture): "",
                run.OT != null && run.OT.MassError != null ? run.OT.MassError.ToString(CultureInfo.InvariantCulture): "",
                run.OT != null ? run.OT.MassErrorMedian.ToString(CultureInfo.InvariantCulture) : "",
                run.OT != null ? run.OT.XreaMean.ToString(CultureInfo.InvariantCulture): "",
                run.OT != null ? Xrea.ConvertXreaToStr(run.OT.Xreas): "",
                mostAbundantPeptsOT,
                run.OT != null && run.OT.Exclude != null ? run.OT.Exclude: "FALSE",
                run.OT != null && run.OT.Exclude != null ? run.OT.RawFile: "",

                run.IT != null && run.IT.RegistrationDateStr!= null ? run.IT.RegistrationDateStr: "",
                run.IT != null && run.IT.FAIMS!= null ? run.IT.FAIMS: "",
                run.IT != null ? run.IT.MS1Intensity.ToString(CultureInfo.InvariantCulture): "",
                run.IT != null ? run.IT.MS2Intensity.ToString(CultureInfo.InvariantCulture): "",
                run.IT != null ? run.IT.ProteinGroup: "",
                run.IT != null ? run.IT.PeptideGroup: "",
                run.IT != null ? run.IT.PSM:"",
                run.IT != null ? run.IT.MSMS: "",
                run.IT != null ? run.IT.IDRatio.ToString(CultureInfo.InvariantCulture): "",
                run.IT != null && run.IT.MassError != null ? run.IT.MassError.ToString(CultureInfo.InvariantCulture): "",
                run.IT != null ? run.IT.MassErrorMedian.ToString(CultureInfo.InvariantCulture) : "",
                run.IT != null ? run.IT.XreaMean.ToString(CultureInfo.InvariantCulture): "",
                run.IT != null ? Xrea.ConvertXreaToStr(run.IT.Xreas): "",
                mostAbundantPeptsIT,
                run.IT != null && run.IT.Exclude != null ? run.IT.Exclude: "FALSE",
                run.IT != null && run.IT.Exclude != null ? run.IT.RawFile: "",


                run.AddedBy,
                run._infoStatus
            };
            valueRange.Values = new List<IList<object>> { oblist };

            try
            {
                // Append the above record...
                var appendRequest = service_sheets.Spreadsheets.Values.Append(valueRange, SpreadsheetId, range);
                appendRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.AppendRequest.ValueInputOptionEnum.RAW;

                var appendReponse = appendRequest.Execute();
            }
            catch (Exception e)
            {
                Thread.Sleep(OFFSET_TIME_REFRESH_MILLISECONDS);
                AddRunOnline(run, machine);
            }

            return true;
        }
        private static bool AddRunOffline(Run run, string machine)
        {
            using (var db = CreateDatabase())
            {
                db.GetCollection<Run>($"{machine}_Evaluation").Insert(run);
                db.Commit();
            }
            return true;
        }
        public static bool UpdateRun(Run run, string machine)
        {
            if (IsOnline) return AddRunOnline(run, machine);
            else return AddRunOffline(run, machine);
        }
        private static bool UpdateRunOnline(Run run, string machine)
        {
            // Specifying Column Range for reading...
            var range = $"{machine}_Evaluation!B" + (run.Id + 3) + ":" + GetLastColumnCategory("Evaluation") + (run.Id + 3);
            var valueRange = new ValueRange();
            var oblist = new List<object>() { run.Operator, run.Comments, run.ColumnLotNumber, run.OT.RegistrationDateStr, run.OT.FAIMS, run.OT.MS1Intensity, run.OT.MS2Intensity, run.OT.ProteinGroup, run.OT.PeptideGroup, run.OT.PSM, run.OT.MSMS, run.OT.IDRatio, run.OT.MassError, run.OT.MassErrorMedian, run.OT.XreaMean, Xrea.ConvertXreaToStr(run.OT.Xreas), run.OT.Exclude, run.OT.RawFile, run.IT.RegistrationDateStr, run.IT.FAIMS, run.IT.MS1Intensity, run.IT.MS2Intensity, run.IT.ProteinGroup, run.IT.PeptideGroup, run.IT.PSM, run.IT.MSMS, run.IT.IDRatio, run.IT.MassError, run.IT.MassErrorMedian, run.IT.XreaMean, Xrea.ConvertXreaToStr(run.IT.Xreas), run.IT.Exclude, run.IT.RawFile, run.AddedBy, run._infoStatus };
            valueRange.Values = new List<IList<object>> { oblist };

            // Performing Update Operation...
            try
            {
                var updateRequest = service_sheets.Spreadsheets.Values.Update(valueRange, SpreadsheetId, range);
                updateRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;
                var appendReponse = updateRequest.Execute();
            }
            catch (Exception e)
            {
                Thread.Sleep(OFFSET_TIME_REFRESH_MILLISECONDS);
                UpdateRunOnline(run, machine);
            }
            return true;
        }
        private static bool UpdateRunOffline(Run run, string machine)
        {
            using (var db = CreateDatabase())
            {
                db.GetCollection<Run>($"{machine}_Evaluation").Update(run);
                db.Commit();
            }
            return true;
        }
        public static bool RemoveRun(Run run, string machine)
        {
            if (IsOnline) return RemoveRunOnlilne(run, machine);
            else return RemoveRunOfflilne(run, machine);

        }
        private static bool RemoveRunOnlilne(Run run, string machine)
        {
            int _index = run.Id;
            (List<MachineQueue> queue, List<Model.Run> evaluation, List<MachineLog> log) prop;
            Management.Machines_Properties.TryGetValue(machine, out prop);
            if (prop == (null, null, null))
                return false;

            if (_index == -1)
            {
                prop.evaluation.Sort((a, b) => a.Id.CompareTo(b.Id));
                _index = prop.evaluation.FindIndex(a => a.Operator.Equals(run.Operator) &&
                a.OT.FAIMS == run.OT.FAIMS && a.IT.FAIMS == run.IT.FAIMS &&
                a.OT.MS1Intensity == run.OT.MS1Intensity && a.IT.MS1Intensity == run.IT.MS1Intensity &&
                a.OT.MS2Intensity == run.OT.MS2Intensity && a.IT.MS2Intensity == run.IT.MS2Intensity &&
                a.OT.MSMS == run.OT.MSMS && a.IT.MSMS == run.IT.MSMS &&
                a.OT.Exclude == run.OT.Exclude && a.IT.Exclude == run.IT.Exclude &&
                a.OT.RawFile == run.OT.RawFile && a.IT.RawFile == run.IT.RawFile &&
                a.InfoStatus == Management.InfoStatus.Active);
                if (_index == -1) return false;
            }

            // Specifying Column Range for reading...
            var range = $"{machine}_Evaluation!" + GetLastColumnCategory("Evaluation") + (_index + 3) + ":" + GetLastColumnCategory("Evaluation") + (_index + 3);
            var valueRange = new ValueRange();
            List<object> oblist = new List<object>() { "Deleted" };
            valueRange.Values = new List<IList<object>> { oblist };

            // Performing Update Operation...
            try
            {
                var updateRequest = service_sheets.Spreadsheets.Values.Update(valueRange, SpreadsheetId, range);
                updateRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;
                var appendReponse = updateRequest.Execute();
            }
            catch (Exception e)
            {
                Thread.Sleep(OFFSET_TIME_REFRESH_MILLISECONDS);
                RemoveRunOnlilne(run, machine);
            }
            return true;
        }
        private static bool RemoveRunOfflilne(Run run, string machine)
        {
            using (var db = CreateDatabase())
            {
                run.InfoStatus = Management.InfoStatus.Deleted;
                db.GetCollection<Run>($"{machine}_Evaluation").Update(run);
            }
            return true;
        }
        #region log
        private static int getNewLogID(string machine)
        {
            (List<MachineQueue> queue, List<Model.Run> evaluation, List<MachineLog> log) prop;
            Management.Machines_Properties.TryGetValue(machine, out prop);
            if (prop == (null, null, null))
                return -1;
            if (prop.log != null)
            {
                prop.log.Sort((a, b) => a.Id.CompareTo(b.Id));
                if (prop.log.Count == 0) return 0;
                return prop.log[prop.log.Count - 1].Id + 1;
            }
            else return -1;
        }
        public static bool AddOrUpdateMachineLog(MachineLog machine_log, string machine)
        {
            if (machine_log == null)
                return false;

            #region read machines
            //Double check LogID
            if (IsOnline)
                ReadSheets(false, "Machines");
            #endregion

            if (machine_log.Id == -1)
            {
                machine_log.Id = getNewLogID(machine);
                return AddLog(machine_log, machine);
            }
            else
                return UpdateLog(machine_log.Id, machine_log, machine);
        }

        public static bool AddLog(MachineLog log, string machine)
        {
            if (IsOnline) return AddLogOnline(log, machine);
            else return AddLogOffline(log, machine);
        }
        private static bool AddLogOnline(MachineLog log, string machine)
        {
            bool isValid = true;
            string coded_file_name = "";
            if (!String.IsNullOrEmpty(log.TechnicalReportFile_GoogleID))
                isValid = UploadTechnicalReportOnline(log.TechnicalReportFile_GoogleID, out coded_file_name);

            if (isValid == false)
                return false;

            // Specifying Column Range for reading...
            var range = $"{machine}_Log!A:" + GetLastColumnCategory("Log", machine);
            var valueRange = new ValueRange();
            var oblist = new List<object>() { log.Id, log.RegistrationDateStr, log.Operator, log.Remarks, log.ColumnLotNumber, log.LC, log.IsCalibrated, log.IsFullyCalibrated, coded_file_name, log.AddedBy, log._infoStatus };
            valueRange.Values = new List<IList<object>> { oblist };

            try
            {
                // Append the above record...
                var appendRequest = service_sheets.Spreadsheets.Values.Append(valueRange, SpreadsheetId, range);
                appendRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.AppendRequest.ValueInputOptionEnum.USERENTERED;
                var appendReponse = appendRequest.Execute();
            }
            catch (Exception e)
            {
                Thread.Sleep(OFFSET_TIME_REFRESH_MILLISECONDS);
                AddLogOnline(log, machine);
            }


            return isValid;
        }
        private static bool AddLogOffline(MachineLog log, string machine)
        {
            bool isValid = true;
            if (!String.IsNullOrEmpty(log._TechnicalReportFileName))
            {
                string new_tech_file = string.Empty;
                isValid = UploadTechnicalReportOffline(log._TechnicalReportFileName, machine, out new_tech_file);
                log.TechnicalReportFile_GoogleID = new_tech_file + "###";
            }

            using (var db = CreateDatabase())
            {
                db.GetCollection<MachineLog>($"{machine}_Log").Insert(log);
                db.Commit();
            }
            return isValid;
        }

        internal static bool UploadTechnicalReportOffline(string fileName, string machine, out string new_dir)
        {
            new_dir = string.Empty;
            if (String.IsNullOrEmpty(fileName)) return false;

            //Check whether Users\{user}\.q2c\ReportsMachines exists
            if (!Directory.Exists(Util.Util.ReportMachine_Folder))
                Directory.CreateDirectory(Util.Util.ReportMachine_Folder);

            //Check whether Users\{user}\.q2c\ReportsMachines\{machine} exists
            var subdir = Path.Combine(Util.Util.ReportMachine_Folder, machine);
            if (!Directory.Exists(subdir))
                Directory.CreateDirectory(subdir);

            //Check whether the file exist in the source path
            if (System.IO.File.Exists(fileName))
            {
                string uniqueFileName = $"{Path.GetFileNameWithoutExtension(fileName)}_{Guid.NewGuid()}{Path.GetExtension(fileName)}";
                new_dir = Path.Combine(subdir, Path.GetFileName(uniqueFileName));
                System.IO.File.Copy(fileName, new_dir, true);
            }
            return true;
        }
        internal static bool UploadTechnicalReportOnline(string fileName, out string coded_file_name)
        {
            coded_file_name = "";
            string fileID = "";
            // Upload a file to the specified folder
            bool isValid = UploadFile(fileName, "application/pdf", "Q2C_auxiliar_files", out fileID);

            if (isValid == true)
                coded_file_name = Path.GetFileName(fileName) + "###" + fileID;

            return true;
        }

        internal static bool UploadFile(string filePath, string contentType, string folderName, out string fileID)
        {
            fileID = "";

            try
            {
                // Find the folder by name
                var folder = FindFolderByName(folderName);
                if (folder == null)
                    folder = CreateFolder(folderName);

                var fileMetadata = new File()
                {
                    Name = Path.GetFileName(filePath),
                    Parents = new[] { folder.Id }
                };

                FilesResource.CreateMediaUpload request;
                using (var stream = new FileStream(filePath, FileMode.Open))
                {
                    request = service_drive.Files.Create(fileMetadata, stream, contentType);
                    request.Fields = "id";
                    request.Upload();
                }

                var file = request.ResponseBody;
                fileID = file.Id;

                return true;
            }
            catch (Exception)
            {
                return false;
            }

        }

        internal static File FindFolderByName(string folderName)
        {
            var request = service_drive.Files.List();
            request.Q = $"mimeType='application/vnd.google-apps.folder' and name='{folderName}'";
            var result = request.Execute();
            return result.Files.FirstOrDefault();
        }
        //internal static File FindFileInFolder(string folderId, string mimeType)
        //{
        //    var request = service_drive.Files.List();
        //    request.Q = $"'{folderId}' in parents and mimeType='{mimeType}'";
        //    var result = request.Execute();
        //    return result.Files.FirstOrDefault();
        //}
        public static bool DownloadFile(string fileId, string outputPath)
        {
            try
            {
                var request = service_drive.Files.Get(fileId);
                using (var stream = new MemoryStream())
                {
                    request.Download(stream);
                    using (var fileStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write))
                    {
                        stream.Position = 0;
                        stream.CopyTo(fileStream);
                    }
                }
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
        internal static File CreateFolder(string folderName)
        {
            var folderMetadata = new File()
            {
                Name = folderName,
                MimeType = "application/vnd.google-apps.folder"
            };

            var request = service_drive.Files.Create(folderMetadata);
            request.Fields = "id, name";

            var folder = request.Execute();
            return folder;
        }
        public static bool UpdateLog(int log_index, MachineLog log, string machine)
        {
            if (IsOnline) return UpdateLogOnline(log_index, log, machine);
            else return UpdateLogOffline(log, machine);
        }
        private static bool UpdateLogOnline(int log_index, MachineLog log, string machine)
        {
            // Specifying Column Range for reading...
            var range = $"{machine}_Log!C" + (log_index + 2) + ":" + GetLastColumnCategory("Log", machine) + (log_index + 2);
            var valueRange = new ValueRange();
            var oblist = new List<object>() { log.Operator, log.Remarks, log.ColumnLotNumber, log.LC, log.IsCalibrated, log.IsFullyCalibrated, log.TechnicalReportFile_GoogleID, log.AddedBy, log._infoStatus };
            valueRange.Values = new List<IList<object>> { oblist };

            // Performing Update Operation...
            try
            {
                var updateRequest = service_sheets.Spreadsheets.Values.Update(valueRange, SpreadsheetId, range);
                updateRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;
                var appendReponse = updateRequest.Execute();
            }
            catch (Exception e)
            {
                Thread.Sleep(OFFSET_TIME_REFRESH_MILLISECONDS);
                UpdateLogOnline(log_index, log, machine);
            }
            return true;
        }
        private static bool UpdateLogOffline(MachineLog log, string machine)
        {
            using (var db = CreateDatabase())
            {
                db.GetCollection<MachineLog>($"{machine}_Log").Update(log);
            }
            return true;
        }

        public static bool RemoveLog(MachineLog log, string machine)
        {
            if (log == null) return false;

            if (IsOnline) return RemoveLogOnline(log, machine);
            else return RemoveLogOffline(log, machine);
        }
        private static bool RemoveLogOnline(MachineLog log, string machine)
        {
            // Specifying Column Range for reading...
            var range = $"{machine}_Log!" + GetLastColumnCategory("Log", machine) + (log.Id + 2) + ":" + GetLastColumnCategory("Log", machine) + (log.Id + 2);
            var valueRange = new ValueRange();
            List<object> oblist = new List<object>() { "Deleted" };
            valueRange.Values = new List<IList<object>> { oblist };

            // Performing Update Operation...
            try
            {
                var updateRequest = service_sheets.Spreadsheets.Values.Update(valueRange, SpreadsheetId, range);
                updateRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;
                var appendReponse = updateRequest.Execute();
            }
            catch (Exception e)
            {
                Thread.Sleep(OFFSET_TIME_REFRESH_MILLISECONDS);
                RemoveLogOnline(log, machine);
            }
            return true;
        }
        private static bool RemoveLogOffline(MachineLog log, string machine)
        {
            using (var db = CreateDatabase())
            {
                log.InfoStatus = Management.InfoStatus.Deleted;
                db.GetCollection<MachineLog>($"{machine}_Log").Update(log);
            }
            return true;
        }
        #endregion

        #endregion

        #region User methods
        public static byte AddOrUpdateUser(User user)
        {
            Management.Users.Sort((a, b) => a.Id.CompareTo(b.Id));
            User? user_index = Management.Users.Where(a => a.Name.Equals(user.Name) &&
                a.InfoStatus == Management.InfoStatus.Active).FirstOrDefault();

            bool isValid = true;
            if (user_index == null)// ADD
            {
                user.Id = getNewUserID();
                if (user.Id == 0)
                    user.Category = UserCategory.Administrator;
                isValid = AddUser(user);
            }
            else// UPDATE
            {
                user.Id = user_index.Id;
                isValid = UpdateUser(user);
            }

            if (isValid)
                return 0;
            return 1;
        }

        public static bool AddUser(User user)
        {
            if (IsOnline) return AddUserOnline(user);
            else return AddUserOffline(user);
        }

        private static bool AddUserOnline(User user)
        {
            // Specifying Column Range for reading...
            var range = "Users!A:" + GetLastColumnCategory("Users");
            var valueRange = new ValueRange();
            var oblist = new List<object>() { user.Id, user.RegistrationDateStr, user.Name, user._category, user.Email, user._infoStatus };
            valueRange.Values = new List<IList<object>> { oblist };

            try
            {
                // Append the above record...
                var appendRequest = service_sheets.Spreadsheets.Values.Append(valueRange, SpreadsheetId, range);
                appendRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.AppendRequest.ValueInputOptionEnum.USERENTERED;
                var appendReponse = appendRequest.Execute();
            }
            catch (Exception e)
            {
                Thread.Sleep(OFFSET_TIME_REFRESH_MILLISECONDS);
                AddUserOnline(user);
            }
            return true;
        }

        private static bool AddUserOffline(User user)
        {
            using (var db = CreateDatabase())
            {
                db.GetCollection<User>().Insert(user);
                db.Commit();
            }
            return true;
        }

        public static bool UpdateUser(User user)
        {
            if (IsOnline) return UpdateUserOnline(user);
            else return UpdateUserOffline(user);
        }

        private static bool UpdateUserOnline(User user)
        {
            // Specifying Column Range for reading...
            var range = "Users!C" + (user.Id + 2) + ":" + GetLastColumnCategory("Users") + (user.Id + 2);
            var valueRange = new ValueRange();
            var oblist = new List<object>() { user.Name, user._category, user.Email, user._infoStatus };
            valueRange.Values = new List<IList<object>> { oblist };

            // Performing Update Operation...
            try
            {
                var updateRequest = service_sheets.Spreadsheets.Values.Update(valueRange, SpreadsheetId, range);
                updateRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;
                var appendReponse = updateRequest.Execute();
            }
            catch (Exception e)
            {
                Thread.Sleep(OFFSET_TIME_REFRESH_MILLISECONDS);
                UpdateUserOnline(user);
            }
            return true;
        }
        private static bool UpdateUserOffline(User user)
        {
            using (var db = CreateDatabase())
            {
                db.GetCollection<User>().Update(user);
            }
            return true;
        }
        public static bool RemoveUser(User user)
        {
            if (user == null) return false;

            if (IsOnline) return RemoveUserOnline(user);
            else return RemoveUserOffline(user);
        }
        private static bool RemoveUserOnline(User user)
        {
            // Specifying Column Range for reading...
            var range = "Users!" + GetLastColumnCategory("Users") + (user.Id + 2) + ":" + GetLastColumnCategory("Users") + (user.Id + 2);
            var valueRange = new ValueRange();
            List<object> oblist = new List<object>() { "Deleted" };
            valueRange.Values = new List<IList<object>> { oblist };

            // Performing Update Operation...
            try
            {
                var updateRequest = service_sheets.Spreadsheets.Values.Update(valueRange, SpreadsheetId, range);
                updateRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;
                var appendReponse = updateRequest.Execute();
            }
            catch (Exception e)
            {
                Thread.Sleep(OFFSET_TIME_REFRESH_MILLISECONDS);
                RemoveUserOnline(user);
            }
            return true;
        }
        private static bool RemoveUserOffline(User user)
        {
            using (var db = CreateDatabase())
            {
                user.Email = Util.Util.EncryptString(user.Email);
                user.InfoStatus = Management.InfoStatus.Deleted;
                db.GetCollection<User>().Update(user);
            }
            return true;
        }


        private static int getNewUserID()
        {
            if (Management.Users != null)
            {
                if (Management.Users.Count == 0) return 0;
                return Management.Users[Management.Users.Count - 1].Id + 1;
            }
            else
                return -1;
        }
        #endregion

        #region Machine methods
        public static bool AddOrUpdateMachine(Machine machine)
        {
            Management.Machines.Sort((a, b) => a.Id.CompareTo(b.Id));
            Machine? machine_index = Management.Machines.Where(a => a.Name.Equals(machine.Name) &&
                a.InfoStatus == Management.InfoStatus.Active).FirstOrDefault();
            bool isValid = true;
            if (machine_index == null)// ADD
            {
                machine.Id = getNewMachineID();
                isValid = AddMachine(machine);
            }
            else// UPDATE
            {
                machine.Id = machine_index.Id;
                isValid = UpdateMachine(machine);
            }

            return isValid;
        }

        public static bool AddMachine(Machine machine)
        {
            if (IsOnline) return AddMachineOnline(machine);
            else return AddMachineOffline(machine);
        }
        private static bool AddMachineOnline(Machine machine)
        {
            // Specifying Column Range for reading...
            var range = "Machines!A:" + GetLastColumnCategory("Machines");
            var valueRange = new ValueRange();
            var oblist = new List<object>() { machine.Id, machine.RegistrationDateStr, machine.Name, machine.HasEvaluation, machine.HasFAIMS, machine.HasOT, machine.HasIT, machine.CalibrationTime, machine.FullCalibrationTime, machine._infoStatus, machine.IntervalTime };
            valueRange.Values = new List<IList<object>> { oblist };

            try
            {
                // Append the above record...
                var appendRequest = service_sheets.Spreadsheets.Values.Append(valueRange, SpreadsheetId, range);
                appendRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.AppendRequest.ValueInputOptionEnum.USERENTERED;
                var appendReponse = appendRequest.Execute();
            }
            catch (Exception e)
            {
                Thread.Sleep(OFFSET_TIME_REFRESH_MILLISECONDS);
                AddMachineOnline(machine);
            }
            return true;
        }
        private static bool AddMachineOffline(Machine machine)
        {
            using (var db = CreateDatabase())
            {
                db.GetCollection<Machine>().Insert(machine);
                db.Commit();

                var mq_queue = db.GetCollection<MachineQueue>($"{machine.Name}_Queue");
                mq_queue.EnsureIndex(x => x.Id);
                var machine_log = db.GetCollection<MachineLog>($"{machine.Name}_Log");
                machine_log.EnsureIndex(x => x.Id);
                var machine_evaluation = db.GetCollection<Run>($"{machine.Name}_Evaluation");
                machine_evaluation.EnsureIndex(x => x.Id);
            }
            return true;
        }

        public static bool UpdateMachine(Machine machine)
        {
            if (IsOnline) return UpdateMachineOnline(machine);
            else return UpdateMachineOffline(machine);
        }
        private static bool UpdateMachineOnline(Machine machine)
        {
            // Specifying Column Range for reading...
            var range = "Machines!C" + (machine.Id + 2) + ":" + GetLastColumnCategory("Machines") + (machine.Id + 2);
            var valueRange = new ValueRange();
            var oblist = new List<object>() { machine.Name, machine.HasEvaluation, machine.HasFAIMS, machine.HasOT, machine.HasIT, machine.CalibrationTime, machine.FullCalibrationTime, machine._infoStatus, machine.IntervalTime };
            valueRange.Values = new List<IList<object>> { oblist };

            // Performing Update Operation...
            try
            {
                var updateRequest = service_sheets.Spreadsheets.Values.Update(valueRange, SpreadsheetId, range);
                updateRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;
                var appendReponse = updateRequest.Execute();
            }
            catch (Exception e)
            {
                Thread.Sleep(OFFSET_TIME_REFRESH_MILLISECONDS);
                UpdateMachineOnline(machine);
            }
            return true;
        }
        private static bool UpdateMachineOffline(Machine machine)
        {
            using (var db = CreateDatabase())
            {
                db.GetCollection<Machine>().Update(machine);
            }
            return true;
        }

        public static bool RemoveMachine(Machine machine)
        {
            if (machine == null) return false;

            if (IsOnline) return RemoveMachineOnline(machine);
            else return RemoveMachineOffline(machine);

        }
        private static bool RemoveMachineOnline(Machine machine)
        {
            // Specifying Column Range for reading...
            var range = "Machines!J" + (machine.Id + 2) + ":" + GetLastColumnCategory("Machines") + (machine.Id + 2);
            var valueRange = new ValueRange();
            List<object> oblist = new List<object>() { "Deleted" };
            valueRange.Values = new List<IList<object>> { oblist };

            // Performing Update Operation...
            try
            {
                var updateRequest = service_sheets.Spreadsheets.Values.Update(valueRange, SpreadsheetId, range);
                updateRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;
                var appendReponse = updateRequest.Execute();
            }
            catch (Exception e)
            {
                Thread.Sleep(OFFSET_TIME_REFRESH_MILLISECONDS);
                RemoveMachineOnline(machine);
            }
            return true;
        }
        private static bool RemoveMachineOffline(Machine machine)
        {
            using (var db = CreateDatabase())
            {
                machine.InfoStatus = Management.InfoStatus.Deleted;
                db.GetCollection<Machine>().Update(machine);
            }
            return true;
        }
        private static int getNewMachineID()
        {
            if (Management.Machines != null)
            {
                if (Management.Machines.Count == 0) return 0;
                return Management.Machines[Management.Machines.Count - 1].Id + 1;
            }
            else
                return -1;
        }

        #endregion
        private static bool SendEmailToUser(Project project, bool isMeasureSoon = true)
        {
            if (service_email == null || Management.Users == null || Management.Users.Count == 0) return false;

            User? user = Management.Users.Where(a => a.Name == project.AddedBy).FirstOrDefault();
            if (user == null) return false;

            string sender = user.Email;

            try
            {
                #region first char to capital letter
                string[] cols_user = Regex.Split(project.AddedBy, "\\.");
                string user_capital = char.ToUpper(cols_user[0][0]) + cols_user[0].Substring(1);
                if (cols_user.Length > 1)
                    user_capital = char.ToUpper(cols_user[1][0]) + cols_user[1].Substring(1) + " " + char.ToUpper(cols_user[0][0]) + cols_user[0].Substring(1);
                #endregion
                string message = "";

                if (isMeasureSoon)
                    message = $"To: {sender}\r\nSubject: INFO: Update about your sample\r\nContent-Type: text/html;charset=utf-8\r\n\r\n<p>Dear {user_capital},<br/><br/>Your sample '" + project.ProjectName + "' has been queued in " + project.Machine + ".<br/><br/>Best regards,<br/>Q2C Support Team</p>";
                else
                    message = $"To: {sender}\r\nSubject: INFO: Update about your sample\r\nContent-Type: text/html;charset=utf-8\r\n\r\n<p>Dear {user_capital},<br/><br/>Your sample '" + project.ProjectName + "' has been measured in " + project.Machine + ".<br/><br/>Best regards,<br/>Q2C Support Team</p>";
                var msg = new Google.Apis.Gmail.v1.Data.Message();
                msg.Raw = Base64UrlEncode(message.ToString());
                service_email.Users.Messages.Send(msg, "me").Execute();
            }
            catch (Exception)
            {
                return false;
            }
            return true;
        }

        private static string Base64UrlEncode(string input)
        {
            var data = Encoding.UTF8.GetBytes(input);
            return Convert.ToBase64String(data).Replace("+", "-").Replace("/", "_").Replace("=", "");
        }

    }
}
