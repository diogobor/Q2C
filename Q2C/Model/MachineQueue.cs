using LiteDB;
using Q2C.Control;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.RightsManagement;
using System.Text;
using System.Threading.Tasks;

namespace Q2C.Model
{
    public class MachineQueue
    {
        [BsonId]
        public int Id { get; set; }
        public string RegistrationDateStr { get; set; }
        public DateTime RegistrationDate { get => Util.Util.ConvertStrToDate(RegistrationDateStr); }
        public string ProjectName { get; set; }
        public string AmountMS { get; set; }
        public string NumberOfSamples { get; set; }
        public string FAIMS { get; set; }
        public string Method { get; set; }
        public int ProjectID { get; set; }
        public string AddedBy { get; set; }
        public Management.InfoStatus InfoStatus { get; set; }
        public string _infoStatus { get => Management.GetInfoStatus(InfoStatus); }

        public MachineQueue(string registrationDateStr, string projectName, string amountMS, string numberOfSamples, string fAIMS, string method, int projectID, string addedBy, Management.InfoStatus infoStatus)
        {
            RegistrationDateStr = registrationDateStr;
            ProjectName = projectName;
            AmountMS = amountMS;
            NumberOfSamples = numberOfSamples;
            FAIMS = fAIMS;
            Method = method;
            ProjectID = projectID;
            AddedBy = addedBy;
            InfoStatus = infoStatus;
        }
        public MachineQueue() { }
    }
}
