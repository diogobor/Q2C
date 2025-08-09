using Microsoft.Win32;
using Q2C.Control.QualityControl;
using Q2C.Model;
using SeproPckg2;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.RightsManagement;
using System.Text;
using System.Threading.Tasks;
using ThermoFisher.CommonCore.Data.Business;

namespace Q2C.Control.FileManagement
{
    public static class ProcessRaw
    {
        private const int OFFSET_TIME_TO_FILTER_RESULTS = 1000;

        public static bool IsOTFile(string rawFile)
        {
            bool isOTFile = SpectrumParser.IsOT(rawFile);
            System.Threading.Thread.Sleep(OFFSET_TIME_TO_FILTER_RESULTS);
            return isOTFile;
        }

        public static ProcessRun RunRawFile(string rawFile)
        {
            CleanTmpFiles(System.IO.Path.GetDirectoryName(rawFile));
            
            string ext_raw_file = Path.GetExtension(rawFile).ToLower();
            if (ext_raw_file.Equals(".raw") && CheckMSFileReaderInstalled() == false)
                throw new Exception("MSFileReader has not been detected!");

            ProcessRun processRun = new ProcessRun();
            processRun.ProcessData(rawFile);
            if (processRun.MSMSCount == 0 ||
                (ext_raw_file.Equals(".raw") && processRun.MS1Intensity == 0) ||
                (ext_raw_file.Equals(".raw") && processRun.MS2Intensity == 0) ||
                String.IsNullOrEmpty(processRun.RawFile))
                throw new Exception("Unable to read " + rawFile + ".");

            Identify quality_control = new Identify(processRun.IsOT);
            System.Threading.Thread.Sleep(OFFSET_TIME_TO_FILTER_RESULTS);
            SeproPckg2.ResultPackage result = quality_control.Identification(rawFile);

            if (result != null)
            {
                string _fileName = Path.GetFileNameWithoutExtension(rawFile);

                var protein_groups = result.MyProteins.MyProteinList.Where(a => a.Scans.Any(b => b.FileName.Equals(_fileName))).ToList();
                processRun.ProteinGroups = protein_groups.Count;
                processRun.PeptideGroups = result.MyProteins.MyPeptideList.Where(a => a.MyScans.Any(b => b.FileName.Equals(_fileName))).Count();
                processRun.PSMs = result.MyProteins.AllPSMs.Where(a => a.FileName.Equals(_fileName)).Count();

                if (processRun.ProteinGroups == 0 &&
                    processRun.PeptideGroups == 0 &&
                    processRun.PSMs == 0)
                    throw new Exception("No proteins/peptides/psms have been found in " + rawFile);

                processRun.IDRatio = processRun.MSMSCount > 0 ? processRun.PSMs / processRun.MSMSCount : 0;

                if (processRun.IDRatio > 100)
                    throw new Exception(rawFile + " contains inconsistencies. The number of MS/MS is less than the number of PSMs.");

                List<double> ppms = new List<double>(result.MyProteins.AllPSMs.Select(a => Math.Round(a.PPM_Orbitrap, 3)).ToList());
                double median = Math.Round(Util.Util.Median(ppms), 3);

                double q1 = Util.Util.Quartile(ppms, 0.25);
                double q3 = Util.Util.Quartile(ppms, 0.75);
                string minPPM = q1.ToString("0.0");
                string maxPPM = q3.ToString("0.0");
                processRun.MassErrorPPM = minPPM + " to " + maxPPM;
                processRun.MassErrorMedian_PPM = median;

                List<SeproPckg2.MyProtein> valid_proteins = protein_groups.Where(a => !a.Locus.Contains("contaminant")).ToList();
                valid_proteins.Sort((a, b) => b.ProteinScore.CompareTo(a.ProteinScore));
                List<PeptideResult> mostAbundantPepts = valid_proteins[0].PeptideResults
                    .Where(a => a.NoMyMapableProteins == 1).ToList()
                    .OrderByDescending(p => p.NoMyScans)
                    .Take(5) // Select top 5
                    .ToList();

                Dictionary<string, int> run = mostAbundantPepts
                                              .ToDictionary(pept => pept.PeptideSequence, pept => pept.NoMyScans);

                processRun.MostAbundantPepts = run;
            }
            else
                throw new Exception("No results have been found in " + rawFile);

            return processRun;

        }

        private static void CleanTmpFiles(string tmpFilesDir)
        {
            if (String.IsNullOrEmpty(tmpFilesDir)) return;
            List<string> tmpFiles = Directory.GetFiles(tmpFilesDir, "*.*",
                                SearchOption.AllDirectories).Where(a => a.EndsWith(".ctxt", StringComparison.OrdinalIgnoreCase) || a.EndsWith("comet.params", StringComparison.OrdinalIgnoreCase)).ToList();

            foreach (var file in tmpFiles)
            {
                if (File.Exists(file))
                    File.Delete(file);
            }
        }

        /// <summary>
        /// Check whether MSFileReader (Thermo Program) is installed in pc, because the ParserRAW needs a msfilereader DLL
        /// </summary>
        /// <returns></returns>
        public static bool CheckMSFileReaderInstalled()
        {
            #region Windows 7 or later
            //Windows RegistryKey
            RegistryKey regKey = Registry.LocalMachine;
            regKey = regKey.OpenSubKey("SOFTWARE\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\Uninstall");
            if (regKey == null)
            {
                return false;
            }
            //Get key vector for each entry
            string[] keys = regKey.GetSubKeyNames();
            if (keys != null && keys.Length > 0)
            {
                //Interates key vector to try to get DisplayName
                for (int i = 0; i < keys.Length; i++)
                {
                    //Open current key
                    RegistryKey k = regKey.OpenSubKey(keys[i]);
                    try
                    {
                        //Get DisplayName
                        String appName = k?.GetValue("DisplayName")?.ToString();
                        if (appName != null && appName.Length > 0 && appName.Contains("Thermo MSFileReader"))
                        {
                            return true;
                        }
                    }
                    catch (Exception) { }
                }
            }
            #endregion

            #region Windows Vista
            //Windows RegistryKey
            regKey = regKey.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall");
            if (regKey == null)
            {
                return false;
            }
            //Get key vector for each entry
            keys = regKey.GetSubKeyNames();
            if (keys != null && keys.Length > 0)
            {
                //Interates key vector to try to get DisplayName
                for (int i = 0; i < keys.Length; i++)
                {
                    //Open current key
                    RegistryKey k = regKey.OpenSubKey(keys[i]);
                    try
                    {
                        //Get DisplayName
                        String appName = k?.GetValue("DisplayName")?.ToString();
                        if (appName != null && appName.Length > 0 && appName.Contains("Thermo MSFileReader"))
                        {
                            return true;
                        }
                    }
                    catch (Exception) { }
                }
            }
            #endregion
            return false;
        }
    }
}
