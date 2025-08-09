using Json;
using LiteDB;
using Q2C.Control.QualityControl;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Q2C.Model
{
    public class SubRun
    {
        [BsonId]
        public int Id { get; set; }
        public string RegistrationDateStr { get; set; }
        public DateTime RegistrationDate { get => Util.Util.ConvertStrToDate(RegistrationDateStr); }
        public bool? FAIMS { get; set; }
        public double MS1Intensity { get; set; }
        public double MS2Intensity { get; set; }
        public double ProteinGroup { get; set; }
        public double PeptideGroup { get; set; }
        public double PSM { get; set; }
        public double MSMS { get; set; }
        public double IDRatio { get; set; }
        public string MassError { get; set; }
        public double MassErrorMedian { get; set; }
        public double XreaMean { get; set; }
        public List<XreaEntry> Xreas { get; set; }
        public bool? Exclude { get; set; }
        public string RawFile { get; set; }
        public int RunID { get; set; }
        public Dictionary<string, int> MostAbundantPepts { get; set; }
        public bool IncludeAnalyticalMetrics { get; set; } = true;

        public SubRun(
            string registrationDateStr,
            bool? fAIMS,
            double mS1Intensity,
            double mS2Intensity,
            double proteinGroup,
            double peptideGroup,
            double pSM,
            double mSMS,
            double iDRatio,
            string massError,
            double massErrorMedian,
            double xreaMean,
            List<XreaEntry> xreas,
            bool? exclude,
            string rawFile,
            int runID,
            Dictionary<string, int> mostAbundantPepts)
        {
            RegistrationDateStr = registrationDateStr;
            FAIMS = fAIMS;
            MS1Intensity = mS1Intensity;
            MS2Intensity = mS2Intensity;
            ProteinGroup = proteinGroup;
            PeptideGroup = peptideGroup;
            PSM = pSM;
            MSMS = mSMS;
            IDRatio = iDRatio;
            MassError = massError;
            MassErrorMedian = massErrorMedian;
            XreaMean = xreaMean;
            Xreas = xreas;
            Exclude = exclude;
            RawFile = rawFile;
            RunID = runID;
            MostAbundantPepts = mostAbundantPepts;
        }

        public SubRun(
            string registrationDateStr,
            bool? fAIMS,
            double mS1Intensity,
            double mS2Intensity,
            double proteinGroup,
            double peptideGroup,
            double pSM,
            double mSMS,
            double iDRatio,
            string massError,
            double massErrorMedian,
            double xreaMean,
            string xreas,
            bool? exclude,
            string rawFile,
            int runID,
            Dictionary<string, int> mostAbundantPepts)
        {
            RegistrationDateStr = registrationDateStr;
            FAIMS = fAIMS;
            MS1Intensity = mS1Intensity;
            MS2Intensity = mS2Intensity;
            ProteinGroup = proteinGroup;
            PeptideGroup = peptideGroup;
            PSM = pSM;
            MSMS = mSMS;
            IDRatio = iDRatio;
            MassError = massError;
            MassErrorMedian = massErrorMedian;
            XreaMean = xreaMean;
            Xreas = Xrea.ConvertStrToXrea(xreas);
            Exclude = exclude;
            RawFile = rawFile;
            RunID = runID;
            MostAbundantPepts = mostAbundantPepts;
        }

        public SubRun() { }
    }
}
