using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ThermoFisher.CommonCore.RawFileReader;
using ThermoFisher.CommonCore.Data.Interfaces;
using ThermoFisher.CommonCore.Data.Business;
using ThermoFisher.CommonCore.Data.FilterEnums;
using Q2C.Model;
using Q2C.Control.QualityControl;
using Newtonsoft.Json.Linq;
using PatternTools.MSParserLight;
using System.Formats.Tar;
using System.Reflection;
using CSMSL.IO.MzML;
using CSMSL.Spectral;

namespace Q2C.Control.FileManagement
{
    public class SpectrumParser
    {
        public SpectrumParser() { }

        /// <summary>
        /// Method responsible for parsing RAW file
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="MsnLevel"></param>
        /// <returns></returns>
        public static SpectraInfo ParseFile(string fileName)
        {
            string ext_file = Path.GetExtension(fileName).ToLower();
            if (ext_file.Equals(".raw"))
            {
                IRawDataPlus rawFile = ThermoLoad(fileName);
                return ParseRaw(rawFile);
            }
            else
                return ParseMzML(fileName);
        }

        private static SpectraInfo ParseMzML(string rawFile)
        {
            SpectraInfo specInfo = null;
            List<XreaEntry> Xreas = new();
            List<double> intensitiesMS1 = new();
            List<double> intensitiesMS2 = new();
            int ms1Count = 0;
            int ms2Count = 0;

            try
            {
                //Call CSMSL module for reading mzML file
                Mzml mm = new Mzml(rawFile);
                mm.Open();
                DateTime createDate = File.GetCreationTime(rawFile);

                IEnumerable<MSDataScan<MZSpectrum>> scans = mm.GetMsScans();
                foreach (MSDataScan<MZSpectrum> scan in scans)
                {
                    try
                    {
                        if (scan.MsnOrder == 1) ms1Count++;
                        else if (scan.MsnOrder == 2) ms2Count++;

                        MZSpectrum ms = scan.MassSpectrum;
                        Spectrum<MZPeak, MZSpectrum> spec = ms;
                        double[] masses = spec.GetMasses();
                        double[] intensities = spec.GetIntensities();
                        List<(double, double)> ions = new List<(double, double)>();
                        for (int i = 0; i < masses.Count(); i++)
                            ions.Add((masses[i], intensities[i]));

                        if (scan.MsnOrder == 1)
                            intensitiesMS1.AddRange(ions.Select(a => a.Item2).ToList());
                        else if (scan.MsnOrder == 2)
                        {
                            intensitiesMS2.AddRange(ions.Select(a => a.Item2).ToList());
                            double xrea = Xrea.GetXrea(ions.Select(a => a.Item2).ToList());
                            xrea = xrea > 0 ? xrea : 0;
                            double dRT = mm.GetRetentionTime(scan.SpectrumNumber);
                            Xreas.Add(new XreaEntry(dRT, xrea));
                        }
                    }
                    catch (Exception) { }
                }

                #region select Xreas from q1 to q3
                if (Xreas.Count > 0)
                {
                    List<double> ret_times = Xreas.Select(a => a.rt).ToList();
                    double q1 = Util.Util.Quartile(ret_times, 0.25);
                    double q3 = Util.Util.Quartile(ret_times, 0.75);
                    Xreas = Xreas.Where(a => a.rt >= q1 && a.rt <= q3).ToList();
                }
                #endregion
                double ms1Intensity = intensitiesMS1.Count > 0 ? intensitiesMS1.Max() : 0;
                double ms2Intensity = intensitiesMS2.Count > 0 ? intensitiesMS2.Max() : 0;
                specInfo = new SpectraInfo(ms1Intensity, ms2Intensity, ms1Count, ms2Count, false, false, createDate, Xreas);

            }
            catch (Exception ex)
            {
                Console.WriteLine(" ERROR: It's not possible to read mzML file.");
            }
            return specInfo;
        }
        private static SpectraInfo ParseRaw(IRawDataPlus rawFile)
        {
            int ms1Count = 0;
            int ms2Count = 0;
            double ms1Intensity = 0;
            double ms2Intensity = 0;
            object progress_lock = new object();
            int spectra_processed = 0;
            int old_progress = 0;
            //[0] -> MS1 => 0: FTMS / 1: ITMS
            //[1] -> MS2 => 0: FTMS / 1: ITMS
            int[] resolution = new int[2];
            resolution[0] = -1;
            resolution[1] = -1;
            DateTime createDate = DateTime.Now;
            int iNumPeaks = -1;
            double[] pdInten;

            SpectraInfo specInfo = null;
            List<XreaEntry> Xreas = new();
            try
            {

                // Get the first and last scan from the RAW file
                int iFirstScan = rawFile.RunHeaderEx.FirstSpectrum;
                int iLastScan = rawFile.RunHeaderEx.LastSpectrum;
                createDate = rawFile.CreationDate;

                IChromatogramSettings Settings = new ChromatogramTraceSettings()
                {
                    Trace = TraceType.BasePeak,
                    Filter = "ms"
                };

                IChromatogramData data = rawFile.GetChromatogramData(new IChromatogramSettings[] { Settings }, rawFile.RunHeaderEx.FirstSpectrum, rawFile.RunHeaderEx.LastSpectrum);
                ms1Intensity = data.IntensitiesArray[0].Max();

                Settings = new ChromatogramTraceSettings()
                {
                    Trace = TraceType.BasePeak,
                    Filter = "ms2"
                };

                data = rawFile.GetChromatogramData(new IChromatogramSettings[] { Settings }, rawFile.RunHeaderEx.FirstSpectrum, rawFile.RunHeaderEx.LastSpectrum);
                ms2Intensity = data.IntensitiesArray[0].Max();

                IScanFilter scanFilterFAIMS = rawFile.GetFilterForScanNumber(iFirstScan);
                bool hasFAIMS = scanFilterFAIMS != null && scanFilterFAIMS.ToString().Contains("cv=-") ? true : false;

                foreach (int iScanNumber in Enumerable.Range(iFirstScan, iLastScan))
                {
                    //ignore null spectra
                    try
                    {

                        // Get the scan filter for this scan number
                        IScanFilter scanFilter = rawFile.GetFilterForScanNumber(iScanNumber);

                        if (!string.IsNullOrEmpty(scanFilter.ToString()))
                        {
                            if (scanFilter.MSOrder == MSOrderType.Ms)
                            {
                                ms1Count++;
                                if (scanFilter != null && scanFilter.ToString().Contains("FTMS"))
                                    resolution[0] = 0;
                                else if (scanFilter != null && scanFilter.ToString().Contains("ITMS"))
                                    resolution[0] = 1;

                            }
                            else if (scanFilter.MSOrder == MSOrderType.Ms2)
                            {
                                ms2Count++;
                                if (scanFilter != null && scanFilter.ToString().Contains("FTMS"))
                                    resolution[1] = 0;
                                else if (scanFilter != null && scanFilter.ToString().Contains("ITMS"))
                                    resolution[1] = 1;

                                #region take intensities

                                // Check to see if the scan has centroid data or profile data.  Depending upon the
                                // type of data, different methods will be used to read the data.
                                CentroidStream centroidStream = rawFile.GetCentroidStream(iScanNumber, false);
                                ScanStatistics scanStatistics = rawFile.GetScanStatsForScanNumber(iScanNumber);

                                if (centroidStream.Length > 0)
                                {
                                    // Get the centroid (label) data from the RAW file for this scan

                                    iNumPeaks = centroidStream.Length;
                                    pdInten = new double[iNumPeaks];  // stores inten of spectral peaks
                                    pdInten = centroidStream.Intensities;

                                }
                                else
                                {
                                    // Get the segmented (low res and profile) scan data
                                    SegmentedScan segmentedScan = rawFile.GetSegmentedScanFromScanNumber(iScanNumber, scanStatistics);
                                    iNumPeaks = segmentedScan.Positions.Length;
                                    pdInten = new double[iNumPeaks];  // stores inten of spectral peaks
                                    pdInten = segmentedScan.Intensities;
                                }
                                if (pdInten.Length > 900)
                                    pdInten = FilterPeaks(pdInten);
                                double xrea = Xrea.GetXrea(pdInten.ToList());
                                xrea = xrea > 0 ? xrea : 0;
                                double dRT = rawFile.RetentionTimeFromScanNumber(iScanNumber);
                                Xreas.Add(new XreaEntry(dRT, xrea));
                                #endregion
                            }
                        }

                    }
                    catch (Exception)
                    {
                        Console.WriteLine($" ERROR: Unable to read spectrum {iScanNumber}");
                    }

                    lock (progress_lock)
                    {
                        spectra_processed++;
                        int new_progress = (int)((double)spectra_processed / (iLastScan - iFirstScan + 1) * 100);
                        if (new_progress > old_progress)
                        {
                            old_progress = new_progress;
                            Console.Write(" Reading RAW File: " + old_progress + "%");
                        }
                    }
                }
                bool isOT = false;
                if (resolution[0] == 0 && resolution[1] == 0)
                    isOT = true;

                #region select Xreas from q1 to q3
                List<double> ret_times = Xreas.Select(a => a.rt).ToList();
                double q1 = Util.Util.Quartile(ret_times, 0.25);
                double q3 = Util.Util.Quartile(ret_times, 0.75);
                Xreas = Xreas.Where(a => a.rt >= q1 && a.rt <= q3).ToList();
                #endregion

                specInfo = new SpectraInfo(ms1Intensity, ms2Intensity, ms1Count, ms2Count, hasFAIMS, isOT, createDate, Xreas);

                rawFile.Dispose();
            }
            catch (Exception rawSearchEx)
            {
                Console.WriteLine(" Error: " + rawSearchEx.Message);
            }

            Console.WriteLine("Reading RAW File: " + "100" + "%");

            return specInfo;
        }

        /// <summary>
        /// Load Thermo file
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public static IRawDataPlus ThermoLoad(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                throw new Exception("No RAW file specified!");
            }

            // Check to see if the specified RAW file exists
            if (!File.Exists(fileName))
            {
                throw new Exception(@"The file doesn't exist in the specified location - " + fileName);
            }

            // Create the IRawDataPlus object for accessing the RAW file
            IRawDataPlus rawFile = null;
            try
            {
                rawFile = RawFileReaderAdapter.FileFactory(fileName);

            }
            catch (System.ApplicationException e)
            {
                throw new Exception(e.Message);
            }

            if (!rawFile.IsOpen || rawFile.IsError)
            {
                throw new Exception("Unable to access the RAW file using the RawFileReader class!");
            }

            // Check for any errors in the RAW file
            if (rawFile.IsError)
            {
                throw new Exception($"Error opening ({rawFile.FileError}) - {fileName}");
            }

            // Check if the RAW file is being acquired
            if (rawFile.InAcquisition)
            {
                throw new Exception("RAW file still being acquired - " + fileName);
            }

            // Get the number of instruments (controllers) present in the RAW file and set the selected instrument to the MS instrument, first instance of it
            rawFile.SelectInstrument(Device.MS, 1);

            return rawFile;
        }
        private static double[] FilterPeaks(double[] intensities, double relativeThresholdPercent = 0.01, int maximumNumberOfPeaks = 900)
        {
            double relative_threshold = intensities.Max() * (relativeThresholdPercent / 100.0);
            intensities = intensities.OrderByDescending(a => a).Take(maximumNumberOfPeaks).Where(a => a > relative_threshold).ToArray();
            return intensities;
        }
        public static bool IsOT(string fileName)
        {
            IRawDataPlus rawFile = ThermoLoad(fileName);
            return ParseOTIT(rawFile);
        }

        private static bool ParseOTIT(IRawDataPlus rawFile)
        {
            //[0] -> MS1 => 0: FTMS / 1: ITMS
            //[1] -> MS2 => 0: FTMS / 1: ITMS
            int[] resolution = new int[2];
            resolution[0] = -1;
            resolution[1] = -1;
            int ms1Count = 0;
            int ms2Count = 0;
            bool isOT = false;

            try
            {
                // Get the first and last scan from the RAW file
                int iFirstScan = rawFile.RunHeaderEx.FirstSpectrum;
                int iLastScan = rawFile.RunHeaderEx.LastSpectrum;

                foreach (int iScanNumber in Enumerable.Range(iFirstScan, iLastScan))
                {
                    // Get the scan filter for this scan number
                    IScanFilter scanFilter = rawFile.GetFilterForScanNumber(iScanNumber);

                    if (!string.IsNullOrEmpty(scanFilter.ToString()))
                    {
                        if (ms1Count == 0 && scanFilter.MSOrder == MSOrderType.Ms)
                        {
                            ms1Count++;
                            if (scanFilter != null && scanFilter.ToString().Contains("FTMS"))
                                resolution[0] = 0;
                            else if (scanFilter != null && scanFilter.ToString().Contains("ITMS"))
                                resolution[0] = 1;

                        }
                        else if (ms2Count == 0 && scanFilter.MSOrder == MSOrderType.Ms2)
                        {
                            ms2Count++;
                            if (scanFilter != null && scanFilter.ToString().Contains("FTMS"))
                                resolution[1] = 0;
                            else if (scanFilter != null && scanFilter.ToString().Contains("ITMS"))
                                resolution[1] = 1;
                        }
                        else if (ms1Count > 0 && ms2Count == 0)
                            continue;
                        else
                            break;
                    }
                }


                if (resolution[0] == 0 && resolution[1] == 0)
                    isOT = true;

                rawFile.Dispose();

            }
            catch (Exception)
            {
                return false;
            }

            return isOT;
        }
    }
}
