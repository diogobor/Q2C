using LiteDB;
using Q2C.Control;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Q2C.Model
{
    public class MachineLog
    {
        [BsonId]
        public int Id { get; set; } = -1;
        public string RegistrationDateStr { get; set; }
        public DateTime RegistrationDate { get => Util.Util.ConvertStrToDate(RegistrationDateStr); }
        public string Operator { get; set; }
        public string Remarks { get; set; }
        public string ColumnLotNumber { get; set; }
        public string LC { get; set; }
        public string _isCalibrated { get; set; }
        public bool IsCalibrated
        {
            get => GetCalibratedStr(_isCalibrated);
        }
        public string _isFullyCalibrated { get; set; }
        public bool IsFullyCalibrated
        {
            get => GetCalibratedStr(_isFullyCalibrated);
        }
        public string AddedBy { get; set; }
        //Active or Deleted (if user removes it)
        public Management.InfoStatus InfoStatus { get; set; }
        public string _infoStatus { get => Management.GetInfoStatus(InfoStatus); }
        public string TechnicalReportFile_GoogleID { get; set; }
        public string _TechnicalReportFileName
        {
            get => GetTechReportFileName(TechnicalReportFile_GoogleID);
        }
        public string _TechnicalReportGoogleId
        {
            get => GetTechReportFileGoogleID(TechnicalReportFile_GoogleID);
        }

        private string GetTechReportFileName(string techreport_googleID)
        {
            if (String.IsNullOrEmpty(techreport_googleID)) return "";
            string[] cols = Regex.Split(techreport_googleID, "###");
            return cols[0];
        }
        private string GetTechReportFileGoogleID(string techreport_googleID)
        {
            if (String.IsNullOrEmpty(techreport_googleID)) return "";
            string[] cols = Regex.Split(techreport_googleID, "###");
            return cols[1];
        }

        private bool GetCalibratedStr(string calibration)
        {
            if (String.IsNullOrEmpty(calibration)) return false;
            switch (calibration.ToLower())
            {
                case "true": return true;
                case "✓": return true;
                case "false": return false;
                default: return false;
            }
        }
        public MachineLog(
            int id,
            string registrationDateStr,
            string @operator,
            string remarks,
            string columnLotNumber,
            string lc,
            string isCalibrated,
            string isFullyCalibrated,
            string technicalReportFile,
            string addedBy,
            Management.InfoStatus infoStatus)
        {
            Id = id;
            RegistrationDateStr = registrationDateStr;
            Operator = @operator;
            Remarks = remarks;
            ColumnLotNumber = columnLotNumber;
            LC = lc;
            _isCalibrated = isCalibrated;
            _isFullyCalibrated = isFullyCalibrated;
            TechnicalReportFile_GoogleID = technicalReportFile;
            AddedBy = addedBy;
            InfoStatus = infoStatus;
        }
        public MachineLog() { }
    }
}
