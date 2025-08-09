using Q2C.Control.FileManagement;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Q2C.Model
{
    public class ProcessRun
    {
        public double MS1Intensity { get; set; }
        public double MS2Intensity { get; set; }
        public double MSMSCount { get; set; }
        public bool IsOT { get; set; }
        public bool IsFAIMS { get; set; }
        public double ProteinGroups { get; set; }
        public double PeptideGroups { get; set; }
        public double PSMs { get; set; }
        public double IDRatio { get; set; }
        public string MassErrorPPM { get; set; }
        public double MassErrorMedian_PPM { get; set; }
        public double XreaMean { get; set; }
        public List<XreaEntry> Xreas { get; set; }
        public string RawFile { get; set; }
        public DateTime CreateDate { get; set; } = DateTime.Now;
        public Dictionary<string, int> MostAbundantPepts { get; set; }

        public ProcessRun() { }

        public void ProcessData(string fileName)
        {
            SpectraInfo specInfo = SpectrumParser.ParseFile(fileName);
            if (specInfo == null) return;

            MS1Intensity = specInfo.MS1Intensity;
            MS2Intensity = specInfo.MS2Intensity;
            MSMSCount = specInfo.MSMSCount;
            IsOT = specInfo.IsOT;
            IsFAIMS = specInfo.IsFAIMS;
            RawFile = fileName;
            CreateDate = specInfo.CreateDate;
            XreaMean = specInfo.XreaMean;
            Xreas = specInfo.Xreas;
        }

        public ProcessRun ToClone()
        {
            ProcessRun new_process = new ProcessRun();
            new_process.MS1Intensity = this.MS1Intensity;
            new_process.MS2Intensity = this.MS2Intensity;
            new_process.MSMSCount = this.MSMSCount;
            new_process.IsOT = this.IsOT;
            new_process.IsFAIMS = this.IsFAIMS;
            new_process.ProteinGroups = this.ProteinGroups;
            new_process.PeptideGroups = this.PeptideGroups;
            new_process.PSMs = this.PSMs;
            new_process.IDRatio = this.IDRatio;
            new_process.MassErrorPPM = this.MassErrorPPM;
            new_process.MassErrorMedian_PPM = this.MassErrorMedian_PPM;
            new_process.RawFile = this.RawFile;
            new_process.CreateDate = this.CreateDate;
            new_process.CreateDate = this.CreateDate;
            new_process.XreaMean = this.XreaMean;
            new_process.Xreas = this.Xreas;
            new_process.MostAbundantPepts = this.MostAbundantPepts;

            return new_process;
        }
    }
    public class XreaEntry
    {
        public double rt { get; set; }
        public double xrea { get; set; }

        public XreaEntry(double rt, double xrea)
        {
            this.rt = rt;
            this.xrea = xrea;
        }
        public XreaEntry() { }
    }
}
