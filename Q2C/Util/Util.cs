using Accord.MachineLearning;
using Accord.Statistics.Models.Regression.Linear;
using Accord.Math.Optimization;
using Accord.Statistics.Models.Regression;
using Accord.Statistics.Models.Regression.Fitting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Accord.Math;
using System.Globalization;
using Q2C.Viewer;
using System.Windows.Controls;
using System.Windows;
using System.IO;
using System.Xml;
using System.Windows.Markup;
using System.Security.Cryptography;
using System.Windows.Data;
using System.Text.RegularExpressions;

namespace Q2C.Util
{
    public static class Util
    {
        public readonly static string ReportMachine_Folder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\.q2c\ReportsMachines\";
        public readonly static string FastaFile_Folder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\.q2c\FastaFiles\";
        public readonly static string DB_Folder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\.q2c\InfoDB\";
        private const string Key = "2e38eb05c0e6d971bccf1ca13e5dccfb";

        [DllImport("kernel32.dll", SetLastError = false)]
        static extern bool GetProductInfo(
             int dwOSMajorVersion,
             int dwOSMinorVersion,
             int dwSpMajorVersion,
             int dwSpMinorVersion,
             out int pdwReturnedProductType);

        public enum WindowsVersion
        {
            None = 0,
            Windows_1_01,
            Windows_2_03,
            Windows_2_10,
            Windows_2_11,
            Windows_3_0,
            Windows_for_Workgroups_3_1,
            Windows_for_Workgroups_3_11,
            Windows_3_2,
            Windows_NT_3_5,
            Windows_NT_3_51,
            Windows_95,
            Windows_NT_4_0,
            Windows_98,
            Windows_98_SE,
            Windows_2000,
            Windows_Me,
            Windows_XP,
            Windows_Server_2003,
            Windows_Vista,
            Windows_Home_Server,
            Windows_7,
            Windows_2008_R2,
            Windows_8,
        }

        public static bool Is64Bits()
        {
            return Environment.Is64BitOperatingSystem;
        }

        public static string GetWindowsVersion()
        {
            string platform = System.Environment.OSVersion.Platform.ToString();
            if (platform.ToLower().Contains("nt"))
            {
                platform = Microsoft.Win32.Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion", "ProductName", null).ToString();
            }
            else
            {
                platform = Microsoft.Win32.Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion", "ProductName", null).ToString();
            }
            return platform;
        }

        public static string GetProcessorName()
        {
            string processorName = "?";
            ManagementObjectSearcher searcher = new ManagementObjectSearcher("select * from Win32_Processor");
            try
            {
                foreach (ManagementObject share in searcher.Get())
                {
                    processorName = share["Name"].ToString();
                    break;
                }
            }
            catch (Exception) { }

            return processorName;
        }

        public static string GetRAMMemory()
        {
            string memory = "?";
            ManagementObjectSearcher searcher = new ManagementObjectSearcher("select * from Win32_OperatingSystem");
            try
            {
                foreach (ManagementObject share in searcher.Get())
                {
                    double memoryInBytes = Convert.ToDouble(share["TotalVisibleMemorySize"].ToString());
                    memoryInBytes /= (1024 * 1024);
                    memory = Math.Round(memoryInBytes, 2).ToString() + " GB";
                    break;
                }
            }
            catch (Exception) { }

            return memory;
        }

        public static string GetAppVersion()
        {
            Version? version = null;
            try
            {
                version = Assembly.GetExecutingAssembly()?.GetName()?.Version;
            }
            catch (Exception e)
            {
                //Unable to retrieve version number
                Console.WriteLine("", e);
                return "";
            }
            return version.Major + "." + version.Minor + "." + version.Build;
        }

        public static double Stdev(List<double> theNumbers, bool unbiased)
        {

            double vari = Variance(theNumbers, unbiased);
            double stdev = Math.Sqrt(vari);
            return (stdev);
        }

        public static double Variance(List<double> theNumbers, bool unbiased)
        {
            double variance = 0;

            double avg = Average(theNumbers);

            //sum(xBar - x)^2;
            double numerator = theNumbers.Sum(a => Math.Pow(a - avg, 2));

            if (unbiased)
            {
                variance = theNumbers.Count - 1 > 0 ? numerator / ((double)theNumbers.Count - 1) : numerator / ((double)theNumbers.Count);
            }
            else
            {
                variance = numerator / ((double)theNumbers.Count);
            }

            return (variance);
        }

        public static double Average(List<double> theNumbers)
        {
            theNumbers.RemoveAll(a => a == double.NegativeInfinity || a == double.PositiveInfinity);
            if (theNumbers.Count > 0)
            {
                return theNumbers.Average();
            }
            else
            {
                return 0;
            }
        }

        public static int MedianIndex(List<double> data)
        {
            if (data == null || data.Count == 0) return 0;
            else data.Sort();

            int n = data.Count;
            if (n % 2 == 0)
                return n / 2 - 1;
            else
                return n / 2;
        }
        public static double Median(List<double> data)
        {
            if (data == null || data.Count == 0) return 0;
            else data.Sort();

            int n = data.Count;
            if (n % 2 == 0)
            {
                // If the number of data points is even, average the middle two values
                int middleIndex1 = n / 2 - 1;
                int middleIndex2 = n / 2;
                return (data[middleIndex1] + data[middleIndex2]) / 2.0;
            }
            else
            {
                // If the number of data points is odd, return the middle value
                int middleIndex = n / 2;
                return data[middleIndex];
            }
        }

        public static (double lowerBound, double upperBound) OutlierBounds(double q1, double q3)
        {
            double iqr = q3 - q1;
            return ((q1 - 1.5 * iqr), (q3 + 1.5 * iqr));
        }

        public static double Quartile(List<double> data, double quartile)
        {
            if (data == null || data.Count == 0) return 0;
            else data.Sort();

            int n = data.Count;
            int k = (int)Math.Ceiling(quartile * (n + 1)) - 1;
            int f = (int)Math.Floor(quartile * (n + 1)) - 1;

            if (n < 0 || k < 0 || f < 0 ||
                k >= data.Count || f >= data.Count) return 0;

            // Interpolate if necessary
            if (k != f)
            {
                double d0 = data[f];
                double d1 = data[k];
                return d0 + (quartile * (n + 1) - Math.Floor(quartile * (n + 1))) * (d1 - d0);
            }
            else
            {
                return data[k];
            }
        }

        public static double[] ComputeRANSACLinearRegression(double[] x, double[] y)
        {
            if (x.Length == 0 || y.Length == 0) return new double[0];

            // Now, fit simple linear regression using RANSAC
            int maxTrials = 100;
            int minSamples = 20;
            double probability = 0.95;
            double errorThreshold = 1.0;

            // Create a RANSAC algorithm to fit a simple linear regression
            var ransac = new RANSAC<SimpleLinearRegression>(minSamples)
            {
                Probability = probability,
                Threshold = errorThreshold,
                MaxEvaluations = maxTrials,

                // Define a fitting function
                Fitting = delegate (int[] sample)
                {
                    // Retrieve the training data
                    double[] inputs = x.Submatrix(sample);
                    double[] outputs = y.Submatrix(sample);

                    // Build a Simple Linear Regression model
                    var r = new SimpleLinearRegression();
                    r.Regress(inputs, outputs);
                    return r;
                },

                // Define a check for degenerate samples
                Degenerate = delegate (int[] sample)
                {
                    // In this case, we will not be performing such checks.
                    return false;
                },

                // Define a inlier detector function
                Distances = delegate (SimpleLinearRegression r, double threshold)
                {
                    List<int> inliers = new List<int>();
                    for (int i = 0; i < x.Length; i++)
                    {
                        // Compute error for each point
                        double error = r.Compute(x[i]) - y[i];

                        // If the squared error is below the given threshold,
                        //  the point is considered to be an inlier.
                        if (error * error < threshold)
                            inliers.Add(i);
                    }

                    return inliers.ToArray();
                }
            };


            // Now that the RANSAC hyperparameters have been specified, we can 
            // compute another regression model using the RANSAC algorithm:

            int[] inlierIndices;
            SimpleLinearRegression robustRegression = ransac.Compute(x.Length, out inlierIndices);


            if (robustRegression == null)
            {
                Console.WriteLine(" ERROR: RANSAC failed. Please try again after adjusting its parameters.");
                return null; // the RANSAC algorithm did not find any inliers and no model was created
            }

            // Compute the output of the model fitted by RANSAC
            double[] ransacOutput = robustRegression.Compute(x);

            return ransacOutput;
        }

        public static DateTime ConvertStrToDate(string datestr)
        {
            try
            {
                return DateTime.ParseExact(datestr, "dd/MM/yyyy h:mm:ss", CultureInfo.InvariantCulture);
            }
            catch (Exception)
            {
                try
                {
                    return DateTime.ParseExact(datestr, "d/M/yyyy", CultureInfo.InvariantCulture);
                }
                catch (Exception)
                {
                    try
                    {
                        return DateTime.ParseExact(datestr, "M/yyyy", CultureInfo.InvariantCulture);
                    }
                    catch (Exception)
                    {
                        try
                        {
                            return DateTime.ParseExact(datestr, "yyyy", CultureInfo.InvariantCulture);
                        }
                        catch (Exception)
                        {
                            try
                            {
                                return DateTime.ParseExact(datestr, "d/M/yyyy HH:mm:ss", CultureInfo.InvariantCulture);
                            }
                            catch (Exception)
                            {
                                try
                                {
                                    return DateTime.ParseExact(datestr, "M/yyyy HH:mm:ss", CultureInfo.InvariantCulture);
                                }
                                catch (Exception)
                                {
                                    try
                                    {
                                        return DateTime.ParseExact(datestr, "yyyy HH:mm:ss", CultureInfo.InvariantCulture);
                                    }
                                    catch (Exception)
                                    {
                                        return DateTime.Now;
                                    }
                                }
                            }
                        }
                    }
                }

            }
        }

        public static UCWaitScreen CallWaitWindow(string primaryMsg, string secondaryMg)
        {
            UCWaitScreen waitScreen = new UCWaitScreen(primaryMsg, secondaryMg);
            Grid.SetRow(waitScreen, 0);
            Grid.SetRowSpan(waitScreen, 2);
            waitScreen.Margin = new Thickness(0, 0, 0, 0);
            return waitScreen;
        }

        public static Grid CloneGrid(Grid sourceGrid)
        {
            if (sourceGrid == null)
                return null;

            Grid newGrid = new Grid();

            // Copy properties of the source Grid
            newGrid.Name = sourceGrid.Name;
            newGrid.Width = sourceGrid.Width;
            newGrid.Height = sourceGrid.Height;
            // Copy any other properties you need...

            // Clone and add child elements (UI controls) to the new Grid
            foreach (UIElement child in sourceGrid.Children)
            {
                if (child is FrameworkElement frameworkElement)
                {
                    // Create a new instance of the same type
                    var newChild = (FrameworkElement)Activator.CreateInstance(child.GetType());

                    // Copy properties of the child element
                    newChild.Name = frameworkElement.Name;
                    newChild.Width = frameworkElement.Width;
                    newChild.Height = frameworkElement.Height;
                    // Copy any other properties you need...

                    // Add the cloned child to the new Grid
                    newGrid.Children.Add(newChild);
                }
            }

            return newGrid;
        }

        public static string GetSelectedValue(DataGrid grid, DependencyProperty TagProperty, int columnIndex = 0)
        {
            if (grid.SelectedCells.Count == 0) return string.Empty;

            DataGridCellInfo cellInfo = grid.SelectedCells[columnIndex];
            if (cellInfo.Column == null) return "0";

            DataGridBoundColumn column = cellInfo.Column as DataGridBoundColumn;
            if (column == null) return null;

            FrameworkElement element = new FrameworkElement() { DataContext = cellInfo.Item };
            BindingOperations.SetBinding(element, TagProperty, column.Binding);

            return element.Tag.ToString();
        }

        public static bool IsValidEmail(string email)
        {
            // Regular expression pattern for basic email validation
            string pattern = @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$";

            // Use Regex.IsMatch to check if the email matches the pattern
            return Regex.IsMatch(email, pattern);
        }

        public static string EncryptString(string plainText)
        {
            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = Encoding.UTF8.GetBytes(Key);
                aesAlg.GenerateIV();

                ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

                using (MemoryStream msEncrypt = new MemoryStream())
                {
                    using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                        {
                            swEncrypt.Write(plainText);
                        }
                    }
                    return Convert.ToBase64String(aesAlg.IV.Concat(msEncrypt.ToArray()).ToArray());
                }
            }
        }

        public static string DecryptString(string cipherText)
        {
            byte[] cipherBytes = Convert.FromBase64String(cipherText);
            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = Encoding.UTF8.GetBytes(Key);
                aesAlg.IV = cipherBytes.Take(16).ToArray(); // Extract IV from the cipher text

                ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

                using (MemoryStream msDecrypt = new MemoryStream(cipherBytes.Skip(16).ToArray()))
                {
                    using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                    {
                        using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                        {
                            return srDecrypt.ReadToEnd();
                        }
                    }
                }
            }
        }

        public static string ComputeMD5Hash(string input)
        {
            using (MD5 md5 = MD5.Create())
            {
                byte[] inputBytes = Encoding.UTF8.GetBytes(input);
                byte[] hashBytes = md5.ComputeHash(inputBytes);

                // Convert the byte array to a hexadecimal string
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++)
                {
                    sb.Append(hashBytes[i].ToString("x2"));
                }

                return sb.ToString();
            }
        }

        public static string GetLetter(int number)
        {
            string result = "";
            while (number >= 0)
            {
                int remainder = number % 26;
                char letter = (char)('A' + remainder);
                result = letter + result;
                number = (number - remainder) / 26 - 1;
            }
            return result;
        }

        public static double CalculateTotalMinutes(List<DateTime> dateTimeList)
        {
            List<DateTime> sorted_dateTimeList = new List<DateTime>(dateTimeList);
            sorted_dateTimeList.Sort();

            double totalMinutes = 0;

            for (int i = 1; i < dateTimeList.Count; i++)
            {
                TimeSpan timeDifference = dateTimeList[i - 1] - dateTimeList[i];
                totalMinutes += timeDifference.TotalMinutes;
            }

            return totalMinutes;
        }
        public enum KnownFolder
        {
            Contacts,
            Downloads,
            Favorites,
            Links,
            SavedGames,
            SavedSearches
        }

        public static class KnownFolders
        {
            private static readonly Dictionary<KnownFolder, Guid> _guids = new()
            {
                [KnownFolder.Contacts] = new("56784854-C6CB-462B-8169-88E350ACB882"),
                [KnownFolder.Downloads] = new("374DE290-123F-4565-9164-39C4925E467B"),
                [KnownFolder.Favorites] = new("1777F761-68AD-4D8A-87BD-30B759FA33DD"),
                [KnownFolder.Links] = new("BFB9D5E0-C6A9-404C-B2B2-AE6DB6AF4968"),
                [KnownFolder.SavedGames] = new("4C5C32FF-BB9D-43B0-B5B4-2D72E54EAAA4"),
                [KnownFolder.SavedSearches] = new("7D1D3A04-DEBB-4115-95CF-2F29DA2920DA")
            };

            public static string GetPath(KnownFolder knownFolder)
            {
                return SHGetKnownFolderPath(_guids[knownFolder], 0);
            }

            [DllImport("shell32",
                CharSet = CharSet.Unicode, ExactSpelling = true, PreserveSig = false)]
            private static extern string SHGetKnownFolderPath(
                [MarshalAs(UnmanagedType.LPStruct)] Guid rfid, uint dwFlags,
                nint hToken = 0);
        }
    }
}
