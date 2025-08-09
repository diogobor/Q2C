using PatternTools;
using PatternTools.FastaParser;
using Q2C.Properties;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Q2C.Control.Database
{
    public static class ProcessDatabase
    {
        public static FastaFileParser AssembleFasta(
            string fastaFile,
            bool FastaDistinctByLocus = true,
            bool AddContaminants = true,
            bool AddDecoys = true)
        {
            var fp = new FastaFileParser();
            fp.ParseFile(new StreamReader(fastaFile), false);

            //Removing tabs
            fp.MyItems.ForEach(a =>
            {
                a.Sequence = Regex.Replace(a.Sequence, "[\t|\n|\r]", "", RegexOptions.IgnoreCase);
                a.Description = Regex.Replace(a.Description, "-", "_", RegexOptions.IgnoreCase);
                a.SequenceIdentifier = Regex.Replace(a.SequenceIdentifier, "[\\||\\.]", "_", RegexOptions.IgnoreCase);
            });

            if (FastaDistinctByLocus == true)
            {
                int count = fp.MyItems.Count;
                fp.MyItems = fp.MyItems.DistinctBy(a => a.SequenceIdentifier).ToList();
            }

            List<FastaItem> decoys = null;

            if (AddContaminants)
            {
                var contaminants = ProcessDatabase.AddContaminants();

                if (contaminants != null)
                {
                    fp.MyItems.AddRange(contaminants);
                }
            }

            if (AddDecoys)
                decoys = GetDecoys(fp.MyItems);
            if (decoys != null) 
                fp.MyItems.AddRange(decoys);

            return fp;
        }
        private static List<FastaItem> AddContaminants()
        {
            try
            {
                FastaFileParser fp = new FastaFileParser();
                fp.ParseString(System.Text.RegularExpressions.Regex.Split(Settings.Default.UserContaminants, "\r\n").ToList());

                if (fp.MyItems.Count == 0)
                    return null;
                return fp.MyItems;
            }
            catch
            {
                return null;
            }
        }

        private static List<FastaItem> GetDecoys(List<FastaItem> fastaItems, string decoyTag = "Reverse")
        {
            var newProts = fastaItems
                    .Select(a =>
                    new FastaItem($"{decoyTag}_{GetAccessionNumber(a.SequenceIdentifier)}",
                    GetReverseProtein(a.Sequence), a.Description))
                    .ToList();

            return newProts;
        }
        private static string GetAccessionNumber(string original_an)
        {
            string[] cols = Regex.Split(original_an, "\\|");
            if (cols.Length > 1)
                return cols[1];
            else 
                return cols[0];

        }
        private static string GetReverseProtein(string sequence)
        {
            return new string(sequence.Reverse().ToArray());

        }
    }
}
