using Q2C.Control;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Q2C.Model
{
    public class Machine
    {
        public int Id { get; set; }
        public string RegistrationDateStr { get; set; }
        public DateTime RegistrationDate { get => Util.Util.ConvertStrToDate(RegistrationDateStr); }
        public string Name { get; set; }
        public bool HasEvaluation { get; set; }
        public bool HasFAIMS { get; set; }
        public bool HasOT { get; set; }
        public bool HasIT { get; set; }
        public string CalibrationTime { get; set; }
        public string FullCalibrationTime { get; set; }
        public Management.InfoStatus InfoStatus { get; set; }
        public string _infoStatus { get => Management.GetInfoStatus(InfoStatus); }
        public int IntervalTime { get; set; }

        public Machine(int id, string registrationDateStr, string name, bool hasEvaluation, bool hasFAIMS, bool hasOT, bool hasIT, string calibrationTime, string fullCalibrationTime, Management.InfoStatus infoStatus, int intervalTime)
        {
            Id = id;
            RegistrationDateStr = registrationDateStr;
            Name = name;
            HasEvaluation = hasEvaluation;
            HasFAIMS = hasFAIMS;
            HasOT = hasOT;
            HasIT = hasIT;
            CalibrationTime = calibrationTime;
            FullCalibrationTime = fullCalibrationTime;
            InfoStatus = infoStatus;
            IntervalTime = intervalTime;
        }
    }
}
