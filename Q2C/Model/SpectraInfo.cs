using Q2C.Control.QualityControl;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThermoFisher.CommonCore.Data.Business;

namespace Q2C.Model
{
    public class SpectraInfo
    {
        internal const int MAX_STR_GOOGLE_ALLOWED = 32767;
        public double MS1Intensity { get; set; }
        public double MS2Intensity { get; set; }
        public int NumberOfMS { get; set; }
        public double MSMSCount { get; set; }
        public bool IsFAIMS { get; set; }
        public bool IsOT { get; set; }
        public DateTime CreateDate { get; set; }
        public double XreaMean { get; set; }
        public List<XreaEntry> Xreas { get; set; }

        public SpectraInfo(
            double mS1Intensity,
            double mS2Intensity,
            int numberOfMS,
            double mSMSCount,
            bool isFAIMS,
            bool isOT,
            DateTime createDate,
            List<XreaEntry> xreas)
        {
            MS1Intensity = mS1Intensity;
            MS2Intensity = mS2Intensity;
            NumberOfMS = numberOfMS;
            MSMSCount = mSMSCount;
            IsFAIMS = isFAIMS;
            IsOT = isOT;
            CreateDate = createDate;
            Xreas = ProcessXreas(xreas);
            if (Xreas != null && Xreas.Count > 0)
            {
                double sum = Xreas.Sum(a => a.xrea);
                XreaMean = sum / Xreas.Where(a => a.xrea > 0).Count();
            }
        }

        private List<XreaEntry> ProcessXreas(List<XreaEntry> xreas, int step = 2, int lowerbound = 4000, int upperbound = 8000)
        {
            xreas.RemoveAll(a => a.xrea == 0);

            List<XreaEntry> xrea_round = (from x in xreas
                                          select new XreaEntry(Math.Round(x.rt, 3), Math.Round(x.xrea, 2))).ToList();

            var xrea_grouped = xrea_round.GroupBy(x => x.rt);
            List<XreaEntry> new_xreas = new();
            foreach (var xrea_g in xrea_grouped)
            {
                new_xreas.Add(new XreaEntry(xrea_g.Key, xrea_g.Max(a => a.xrea)));
            }

            var xrea_str_length = Xrea.ConvertXreaToStr(new_xreas).Length;
            if (xrea_str_length < MAX_STR_GOOGLE_ALLOWED)//limit google spreadsheet
                return new_xreas;

            List<XreaEntry> result = new();
            int median_index = Util.Util.MedianIndex(new_xreas.Select(a => a.xrea).ToList());

            while (xrea_str_length > MAX_STR_GOOGLE_ALLOWED - 1)
            {
                result.Clear();
                lowerbound = Convert.ToInt32(lowerbound * 0.95);
                upperbound = Convert.ToInt32(upperbound * 0.95);


                for (int i = median_index - step; i >= 0 && result.Count < lowerbound; i -= step)
                {
                    result.Insert(0, new_xreas[i]);
                }

                result.Add(new_xreas[median_index]);

                for (int i = median_index + step; i < new_xreas.Count && result.Count < upperbound; i += step)
                {
                    result.Add(new_xreas[i]);
                }

                xrea_str_length = Xrea.ConvertXreaToStr(result).Length;

            }
            return result;
        }
    }
}
