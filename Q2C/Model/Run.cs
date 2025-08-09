using LiteDB;
using Q2C.Control;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Q2C.Model
{
    public class Run
    {
        [BsonId]
        public int Id { get; set; } = -1;
        public SubRun OT { get; set; }
        public SubRun IT { get; set; }
        public string Operator { get; set; }
        public string Comments { get; set; }
        public string ColumnLotNumber { get; set; }
        public string AddedBy { get; set; }
        //Active or Deleted (if user removes it)
        public Management.InfoStatus InfoStatus { get; set; }
        public string _infoStatus { get => Management.GetInfoStatus(InfoStatus); }

        public Run() { }

        public Run(int id, SubRun oT, SubRun iT, string @operator, string comments, string columnLotNumber, string addedBy, Management.InfoStatus infoStatus)
        {
            Id = id;
            OT = oT;
            IT = iT;
            Operator = @operator;
            Comments = comments;
            ColumnLotNumber = columnLotNumber;
            AddedBy = addedBy;
            InfoStatus = infoStatus;
        }
    }
}
