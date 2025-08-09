using Q2C.Control;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Q2C.Model
{
    public class Project
    {
        public int Id { get; set; } = -1;
        public string RegistrationDateStr { get; set; }
        public DateTime RegistrationDate { get => Util.Util.ConvertStrToDate(RegistrationDateStr); }
        public string ProjectName { get; set; }
        public string AmountMS { get; set; }
        public string NumberOfSamples { get; set; }
        public string Method { get; set; }
        public string Machine { get; set; }
        public string[] GetMachines
        {
            get => Regex.Split(Machine, "/");
        }
        public List<ProjectFAIMS> FAIMS { get; set; }
        public string _faims { get => GetFAIMSstr(FAIMS); }
        private string GetFAIMSstr(List<ProjectFAIMS> _faimsList)
        {
            StringBuilder sb_faims = new StringBuilder();
            if (_faimsList == null || _faimsList.Count == 0) return sb_faims.ToString();

            foreach (var faims in _faimsList)
            {
                switch (faims)
                {
                    case ProjectFAIMS.Yes:
                        sb_faims.Append("Yes/");
                        break;
                    case ProjectFAIMS.No:
                        sb_faims.Append("No/");
                        break;
                    case ProjectFAIMS.IdontMind:
                        sb_faims.Append("I don't mind/");
                        break;
                    default:
                        sb_faims.Append("/");
                        break;
                }
            }


            return sb_faims.ToString().Substring(0, sb_faims.ToString().Length - 1);
        }
        public static List<ProjectFAIMS> GetFAIMS(string _faims)
        {
            if (String.IsNullOrEmpty(_faims))
                return new List<ProjectFAIMS>() { ProjectFAIMS.Undefined };

            List<ProjectFAIMS> _faimsList = new();

            string[] _cols = Regex.Split(_faims, "/");
            foreach (string _col in _cols)
            {
                switch (_col)
                {
                    case "Yes":
                        _faimsList.Add(ProjectFAIMS.Yes);
                        break;
                    case "No":
                        _faimsList.Add(ProjectFAIMS.No);
                        break;
                    case "I don't mind":
                        _faimsList.Add(ProjectFAIMS.IdontMind);
                        break;
                    default:
                        _faimsList.Add(ProjectFAIMS.Undefined);
                        break;
                }
            }
            return _faimsList;

        }
        public string _receiveNotification { get; set; }
        public bool ReceiveNotification
        {
            get => GetReceiveNotification(_receiveNotification);
        }
        private bool GetReceiveNotification(string _receiveNotification)
        {
            if (String.IsNullOrEmpty(_receiveNotification)) return false;
            switch (_receiveNotification.ToLower())
            {
                case "true": return true;
                case "✓": return true;
                case "false": return false;
                default: return false;
            }
        }
        public string EstimatedWaitingTime { get; set; }
        public string Comments { get; set; }
        public string AddedBy { get; set; }
        //gray: measured; blue: measure soon (to be acquired); white: wait for acquisition
        public ProjectStatus Status { get; set; }
        public string _status
        {
            get => GetStatusStr(Status);
        }
        private string GetStatusStr(ProjectStatus Status)
        {
            string _status = "";
            switch (Status)
            {
                case ProjectStatus.WaitForAcquisition:
                    _status = "Wait for acquisition";
                    break;
                case ProjectStatus.InProgress:
                    _status = "In Progress";
                    break;
                case ProjectStatus.Measured:
                    _status = "Measured";
                    break;
                default:
                    _status = "";
                    break;
            }
            return _status;
        }
        public static ProjectStatus GetStatus(string _status)
        {
            if (String.IsNullOrEmpty(_status))
                return ProjectStatus.Undefined;

            switch (_status)
            {
                case "Wait for acquisition":
                    return ProjectStatus.WaitForAcquisition;
                case "Measure soon":
                    return ProjectStatus.InProgress;
                case "In Progress":
                    return ProjectStatus.InProgress;
                case "Measured":
                    return ProjectStatus.Measured;
                default:
                    return ProjectStatus.Undefined;
            }
        }
        //Active or Deleted (if user removes it)
        public Management.InfoStatus InfoStatus { get; set; }
        public string _infoStatus { get => Management.GetInfoStatus(InfoStatus); }

        public Project() { }

        public Project(int iD, string registrationDateStr, string projectName, string amountMS, string numberOfSamples, string method, string machine, List<ProjectFAIMS> fAIMS, string receiveNotification, string comments, string addedBy, ProjectStatus status, Management.InfoStatus infoStatus)
        {
            Id = iD;
            RegistrationDateStr = registrationDateStr;
            ProjectName = projectName;
            AmountMS = amountMS;
            NumberOfSamples = numberOfSamples;
            Method = method;
            Machine = machine;
            FAIMS = fAIMS;
            _receiveNotification = receiveNotification;
            Comments = comments;
            AddedBy = addedBy;
            Status = status;
            InfoStatus = infoStatus;
        }

    }
    public enum ProjectStatus
    {
        WaitForAcquisition,
        InProgress,
        Measured,
        Undefined
    }

    public enum ProjectFAIMS
    {
        Yes,
        No,
        IdontMind,
        Undefined
    }
}
