using Q2C.Properties;
using PatternTools;
using SEPro;
using SeproPckg2;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThermoFisher.CommonCore.Data.Business;
using System.Text.RegularExpressions;
using System.Security.Policy;
using System.Xml.Linq;
using System.Runtime.InteropServices;

namespace Q2C.Control.QualityControl
{
    public class Identify
    {
        private bool IsOT { get; set; }
        public Identify(bool isOT)
        {
            IsOT = isOT;
        }
        public ResultPackage Identification(string rawFile)
        {
            string database = "";
            string cometParamsPath = "";

            try
            {
                //Clean older files
                CleanTmpFiles(cometParamsPath, rawFile);
                rawFile = Check_path(rawFile);

                string dir = Path.GetDirectoryName(rawFile).Replace("\\", "/");
                if (!IsValidDirectoryPath(dir))
                    throw new Exception("ERROR: Raw directory contains invalid character(s).");

                database = GetDBPath();
                Console.WriteLine($"INFO: Selected database: {database}");

                cometSearchParams.SequenceDatabase = database;
                SEProParams.AlternativeProteinDB = database;

                if (IsOT == false)
                {
                    cometSearchParams.FragmentBinOffset = 0.4;
                    cometSearchParams.FragmentBinTolerance = 1.0005;
                    cometSearchParams.TheoreticalFragIons = 1;
                    Console.WriteLine("INFO: IT has been detected.");
                }

                string cometParams = SearchEngineGUI.SearchUtils.GenerateSearchParamsText(cometSearchParams);
                cometParamsPath = dir + "/comet.params";
                Console.WriteLine($"INFO: Comet params will be saved to: {cometParamsPath}");

                cometSearchParams.SearchDirectory = dir;
                SEProParams.SeachResultDirectoy = dir;

                File.WriteAllText(cometParamsPath, cometParams);
                Console.WriteLine("INFO: Comet params have been saved.");

                //Call comet
                string cometPath = SearchEngineGUI.SearchUtils.GetCometPath();
                cometPath = Check_path(cometPath);
                Console.WriteLine($"INFO: Comet has been retrieved from {cometPath}.");

                SearchEngineGUI.SearchUtils.CallComet(new FileInfo(rawFile), cometPath);
                Console.WriteLine("INFO: Comet has been processed.");

                SEProFilter seproFilter = new SEProFilter(SEProParams);
                ResultPackage rp = seproFilter.Filter(dir, false);
                Console.WriteLine("INFO: Sepro has been processed.");

                if (rp != null)
                {
                    Console.WriteLine("INFO: Sepro has been processed results.");
                    rp.SearchParameters = cometSearchParams;
                }
                else
                    Console.WriteLine("INFO: Sepro has no result.");

                CleanTmpFiles(cometParamsPath, rawFile);
                Console.WriteLine("INFO: Temp files have been deleted.");

                return rp;

            }
            catch (Exception)
            {
                CleanTmpFiles(cometParamsPath, rawFile);
                throw;
            }
        }

        private static void CleanTmpFiles(string cometParamsPath, string rawFile)
        {
            #region remove tmp files
            if (File.Exists(cometParamsPath))
                File.Delete(cometParamsPath);
            var _ctxt = Path.ChangeExtension(rawFile, ".ctxt");
            if (File.Exists(_ctxt))
                File.Delete(_ctxt);

            string q2c_tmp_dir = @"C:\tmp\.q2c";
            if (Directory.Exists(q2c_tmp_dir))
            {
                // Get all files in the directory
                string[] files = Directory.GetFiles(q2c_tmp_dir);

                // Loop through each file and delete it
                foreach (string file in files)
                {
                    try
                    {
                        File.Delete(file);
                    }
                    catch (Exception)
                    {
                    }
                }
            }
            #endregion
        }

        public static string GetDBPath()
        {
            Q2C.Model.Database _current_db = Management.GetDatabase();
            if (_current_db == null) return "";

            var selected_falsta = _current_db.FastaFiles.Where(a => a.IsSelected).FirstOrDefault();
            if (selected_falsta == null) return "";

            //Check default path: \\FastaFiles
            var _defaut_path = AppDomain.CurrentDomain.BaseDirectory + selected_falsta.Path;
            if (!File.Exists(_defaut_path))
            {
                //Check users path: e.g. C:\Users\current_user\.q2c\FastaFiles
                _defaut_path = selected_falsta.Path;
                if (!File.Exists(_defaut_path))
                    throw new Exception("Database file not found!");
            }

            _defaut_path = Check_path(_defaut_path);

            return _defaut_path;
        }

        private static string Check_path(string original_path)
        {
            if (IsValidDirectoryPath(original_path))
                return original_path;

            #region Copy file to a tmp dir

            Console.WriteLine("INFO: Detecting tmp directory...");
            string new_dir = @"C:\tmp\.q2c";
            //check permission to create a folder
            if (!HasCreateFolderPermission(new_dir))
                throw new Exception("ERROR: Unable to create a tmp folder.");

            Console.WriteLine("INFO: Creating tmp directory...");
            FileAttributes attributes = File.GetAttributes(new_dir);

            //Check whether C:\.q2c\FastaFiles exists
            if (!Directory.Exists(new_dir))
            {
                Directory.CreateDirectory(new_dir);
                // Add the Hidden attribute
                attributes |= FileAttributes.Hidden;

                // Set the attributes to the folder
                File.SetAttributes(new_dir, attributes);
            }
            else if ((attributes & FileAttributes.Hidden) != FileAttributes.Hidden) // Check if the folder is already hidden
            {
                // Add the Hidden attribute
                attributes |= FileAttributes.Hidden;
                // Set the attributes to the folder
                File.SetAttributes(new_dir, attributes);
            }

            string new_file_path = Path.Combine(new_dir, System.IO.Path.GetFileName(original_path));
            Console.WriteLine($"INFO: Copying {original_path} to {new_file_path}...");
            if (!File.Exists(new_file_path))
                File.Copy(original_path, new_file_path);
            Console.WriteLine("INFO: Copy done.");
            #endregion

            return new_file_path;
        }
        private static bool IsValidDirectoryPath(string path)
        {
            // Define a regex pattern to match only valid characters (alphanumeric and backslashes)
            string pattern = @"^[a-zA-Z0-9\\//._-|-:]+$";
            return Regex.IsMatch(path, pattern);
        }
        private static bool HasCreateFolderPermission(string directoryPath)
        {
            try
            {
                // Create a temporary directory to test permissions
                string tempFolderPath = Path.Combine(directoryPath, "TempFolderForPermissionCheck");
                Directory.CreateDirectory(tempFolderPath);
                Directory.Delete(tempFolderPath); // Clean up
                return true;
            }
            catch (UnauthorizedAccessException)
            {
                // Catch permission-related exceptions
                return false;
            }
            catch (Exception ex)
            {
                // Handle other potential exceptions if necessary
                Console.WriteLine("An unexpected error occurred: " + ex.Message);
                return false;
            }
        }

        static PSMSearchParams cometSearchParams = new PSMSearchParams()
        {
            Enzyme = 2, //Trypsin
            EnzymeSpecificity = 1, //semi specific
            IonsA = false,
            IonsB = true,
            IonsC = false,
            IonsX = false,
            IonsY = true,
            IonsZ = false,
            IonsNL = true,
            FragmentBinOffset = 0,
            FragmentBinTolerance = 0.02,
            ClearMZRangeMax = 0,
            ClearMZRangeMin = 100,
            MaxVariableModsPerPeptide = 1,
            IsPeff = false,
            MissedCleavages = 2,
            MyModificationItems = new List<PatternTools.PTMMods.Modification>()
                {
                    new PatternTools.PTMMods.Modification("MetOx", 15.9949, "M", 35, true ),
                    new PatternTools.PTMMods.Modification("CarbC", 57.02146, "C", 4, false),
                },
            PrecursorMassTolerance = 35,
            SearchMassRangeMax = 5500,
            SearchMassRangeMin = 550,
            TheoreticalFragIons = 0,

            SearchDirectory = "?",
            SequenceDatabase = "?",
        };

        static Parameters SEProParams = new Parameters()
        {
            CompositeScoreDeltaCN = true,
            CompositeScoreDeltaMassPPM = true,
            CompositeScorePeaksMatched = true,
            CompositeScorePrimaryScore = true,
            CompositeScorePresence = true,
            CompositeScoreSecondaryRank = true,

            GroupByNoEnzymaticTermini = true,

            QFilterDeltaCN = true,
            QFilterDeltaCNMin = 0.001,

            QFilterDeltaMassPPM = true,
            QFilterDeltaMassPPMValue = 30,
            QFilterDiscardChargeOneMS = false,
            QFilterPrimaryScore = false,
            QFilterProteinsMinNoPeptides = 1,
            QFilterProteinsMinNoUniquePeptides = 0,
            QFilterMinSequenceLength = 6,
            QFilterMinSpecCount = 1,
            QFilterPrimaryScoreValue = 1,

            MinProteinScore = 2,

            UnlabeledDecoyTag = "N/A",
            GroupPtnsWithXCommonPeptides = 1,

            EliminateInPtns = true,
            GroupByChargeState = false,
            LabeledDecoyTag = "Reverse",
            MS2PPM = 20,
            MyEnzime = Enzyme.Trypsin,

            SpectraFDR = 0.05,
            PeptideFDR = 0.04,
            ProteinFDR = 0.03,

            ProteinLogic = true,
            PTMModifications = cometSearchParams.MyModificationItems,

            DeltaMassPPMPostProcessing = 10,

            SeachResultDirectoy = "?",
            AlternativeProteinDB = "?"
        };
    }
}
