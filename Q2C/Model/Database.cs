using Q2C.Control;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Q2C.Model
{
    public class Database
    {
        public string GoogleClientID { get; set; }
        public string GoogleClientSecret { get; set; }
        public string SpreadsheetID { get; set; }
        public List<FastaFile> FastaFiles { get; set; }
        public bool IsOnline { get; set; } = true;

        public Database(string googleClientID,
                        string googleClientSecret,
                        string spreadsheetID,
                        List<FastaFile> fastaFiles,
                        bool isOnline)
        {
            GoogleClientID = googleClientID;
            GoogleClientSecret = googleClientSecret;
            SpreadsheetID = spreadsheetID;
            FastaFiles = fastaFiles;
            IsOnline = isOnline;
        }
    }
}
