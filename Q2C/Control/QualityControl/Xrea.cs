using Google.Apis.Logging;
using Q2C.Model;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Q2C.Control.QualityControl
{
    public static class Xrea
    {
        public static List<XreaEntry> SmoothAreas(List<XreaEntry> xreas, int window = 101, int pol = 3)
        {
            PatternTools.XIC.SavitzkyGolay sg = new PatternTools.XIC.SavitzkyGolay(window, pol);
            var points = sg.SmoothData(xreas.Select(a => a.xrea).ToArray());

            List<XreaEntry> areas = new ();

            for (int i = 0; i < points.Length; i++)
            {
                areas.Add(new XreaEntry(xreas[i].rt, points[i]));
            }

            return areas;
        }

        /// <summary>
        /// Calculate Xrea score.
        /// </summary>
        /// <param name="intensities">Intensities of fragment ions.</param>
        /// <returns>Xrea.</returns>
        public static double GetXrea(List<double> intensities)
        {
            if (intensities == null || intensities.Count == 0) return 0;

            //Lets close the object
            List<double> intensityClone = intensities.OrderBy(a => a).ToList();

            double[] cumulativeIntensities = new double[intensityClone.Count];

            cumulativeIntensities[0] = intensityClone[0];
            for (int i = 1; i < cumulativeIntensities.Length; i++)
            {
                cumulativeIntensities[i] = cumulativeIntensities[i - 1] + intensityClone[i];
            }

            //And now compute the Xrea
            double triangleArea = (cumulativeIntensities.Length * cumulativeIntensities.Last()) / 2f;
            double cumulativeArea = cumulativeIntensities.Sum();

            return (triangleArea - cumulativeArea) / (triangleArea + intensityClone.Last());
        }

        public static string ConvertXreaToStr(List<XreaEntry> xreas)
        {
            if (xreas == null || xreas.Count == 0) return "";

            List<XreaEntry> new_xreas = (from x in xreas
                                         select new XreaEntry(Math.Round(x.rt, 3), Math.Round(x.xrea, 2))).ToList();

            var xrea_grouped = new_xreas.GroupBy(x => x.rt);
            StringBuilder sb = new StringBuilder();
            foreach (var xrea_g in xrea_grouped)
            {
                sb.Append(xrea_g.Key + "_" + xrea_g.Max(a => a.xrea) + "#");
            }
            return sb.ToString();
        }

        public static List<XreaEntry> ConvertStrToXrea(string xreas_str)
        {
            if (String.IsNullOrEmpty(xreas_str)) return new();

            string[] xreas = Regex.Split(xreas_str, "#");

            List<XreaEntry> new_xreas = new();
            for (int i = 0; i < xreas.Length - 1; i++)
            {
                string[] _tuple = Regex.Split(xreas[i], "_");
                new_xreas.Add(new XreaEntry(Convert.ToDouble(_tuple[0]), Convert.ToDouble(_tuple[1])));
            }
            new_xreas.RemoveAll(a => a.xrea == 0);
            return new_xreas;
        }
    }
}

