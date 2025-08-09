using Accord.IO;
using Accord.Math.Integration;
using Microsoft.Win32;
using PdfSharp.Charting;
using PdfSharp.Drawing;
using PdfSharp.Drawing.Layout;
using PdfSharp.Pdf;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Security;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Media.Imaging;
using ThermoFisher.CommonCore.Data.FilterEnums;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;
using System.Windows.Forms;
using Q2C.Control.Statistics;
using System.Security.AccessControl;
using System.Windows.Input;
using Google.Apis.Logging;
using System.Windows.Media;
using System.Security.RightsManagement;
using System.Reflection.Metadata;
using System.Threading;
using System.Windows.Controls.DataVisualization.Charting;
using DataPoint = Q2C.Control.Statistics.DataPoint;
using System.Xml.Linq;
using static System.Windows.Forms.DataFormats;

namespace Q2C.Control
{
    public class PDFExporter
    {
        public static void OpenFile(string fileName)
        {
            try
            {
                var process = new Process();
                process.StartInfo.FileName = GetAdobeReaderPath();
                process.StartInfo.Arguments = fileName;
                process.StartInfo.WindowStyle = ProcessWindowStyle.Maximized;
                process.EnableRaisingEvents = true;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.Start();
            }
            catch (Exception)
            {
                try
                {
                    System.Diagnostics.Process.Start(fileName);
                }
                catch (Exception)
                {
                }
            }

        }
        private static string GetAdobeReaderPath()
        {
            string reader_path = "";
            // Registry path for Adobe Reader
            string adobeReaderRegistryPath = @"SOFTWARE\Adobe\Acrobat Reader";

            // Get the registry key for Adobe Reader
            RegistryKey adobeReaderKey = Registry.LocalMachine.OpenSubKey(adobeReaderRegistryPath);
            if (adobeReaderKey == null)
                adobeReaderRegistryPath = @"SOFTWARE\WOW6432Node\Adobe\Acrobat Reader";

            adobeReaderKey = Registry.LocalMachine.OpenSubKey(adobeReaderRegistryPath);
            if (adobeReaderKey != null)
            {

                // Get the subkeys (versions) under the Adobe Reader key
                string[] versionSubkeys = adobeReaderKey.GetSubKeyNames();
                bool isFound = false;

                foreach (string versionSubkey in versionSubkeys)
                {
                    // Open the subkey for the specific version
                    using (RegistryKey versionKey = adobeReaderKey.OpenSubKey(versionSubkey))
                    {
                        if (versionKey != null)
                        {
                            // Look for the "InstallPath" value
                            reader_path = versionKey.GetValue("InstallPath") as string;

                            if (!string.IsNullOrEmpty(reader_path))
                            {
                                string[] _valueNames = adobeReaderKey.GetValueNames();
                                string full_name = "";
                                foreach (var _valueName in _valueNames)
                                {
                                    full_name = adobeReaderKey.GetValue(_valueName) as string;
                                    if (!string.IsNullOrEmpty(full_name))
                                    {
                                        if (full_name.Contains(reader_path))
                                        {
                                            reader_path = full_name;
                                            isFound = true;
                                        }
                                        else
                                            reader_path = "";
                                        break;
                                    }
                                }
                            }
                            else
                            {
                                string[] versionKeySubkeys = versionKey.GetSubKeyNames();
                                foreach (string subKey in versionKeySubkeys)
                                {
                                    if (!subKey.Equals("InstallPath")) continue;
                                    using (RegistryKey versionSubKey = versionKey.OpenSubKey(subKey))
                                    {
                                        //Get (Default) value
                                        reader_path = versionSubKey.GetValue("") as string;

                                        if (!string.IsNullOrEmpty(reader_path))
                                        {
                                            string[] _valueNames = adobeReaderKey.GetValueNames();
                                            string full_name = "";
                                            foreach (var _valueName in _valueNames)
                                            {
                                                full_name = adobeReaderKey.GetValue(_valueName) as string;
                                                if (!string.IsNullOrEmpty(full_name))
                                                {
                                                    if (full_name.Contains(reader_path))
                                                    {
                                                        reader_path = full_name;
                                                        isFound = true;
                                                    }
                                                    else
                                                        reader_path = "";
                                                    break;
                                                }
                                            }
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                    }
                    if (isFound) break;
                }
            }

            return reader_path;
        }

        public static byte AddMachineTitle(string machine, string faims, XGraphics gfx)
        {
            XSize size = gfx.PageSize;
            size.Height -= 10;
            size.Width -= 40;
            XRect rect = new XRect(new XPoint(), size);
            rect.Inflate(-5, 85);

            double offsetY = 140;
            XStringFormat format = new XStringFormat();

            // Create a font
            XFont font = new XFont("Arial", 8, XFontStyle.Bold);

            format.Alignment = XStringAlignment.Near;//Left

            // Draw the text
            rect.Offset(25, offsetY);
            gfx.DrawString("Machine: " + machine + " :: " + faims, font, XBrushes.Black, rect, format);
            return 0;
        }

        public static byte AddOTITTitle(XGraphics gfx, bool hasOT = true, bool hasIT = true)
        {
            XSize size = gfx.PageSize;
            size.Height -= 10;
            size.Width -= 40;
            XRect rect = new XRect(new XPoint(), size);
            rect.Inflate(-5, 85);

            double offsetY = 140;
            XStringFormat format = new XStringFormat();

            // Create a font
            XFont font = new XFont("Arial", 8, XFontStyle.Bold);
            format.Alignment = XStringAlignment.Near;//Left

            // Draw the text
            rect.Offset(25, offsetY);

            if (hasOT)
            {
                if (hasIT)
                    gfx.DrawString("OT", font, XBrushes.Black, 160, 105);
                else
                    gfx.DrawString("OT", font, XBrushes.Black, 300, 105);
            }
            if (hasIT)
            {
                if (hasOT)
                    gfx.DrawString("IT", font, XBrushes.Black, 440, 105);
                else
                    gfx.DrawString("IT", font, XBrushes.Black, 300, 105);
            }

            return 0;
        }
        public static byte AddNewPage(PdfDocument document, out XGraphics gfx)
        {
            byte returnOK = 0;//0 -> ok, 1 -> failed, 2 -> cancel
            if (document == null)
                document = new PdfDocument();
            gfx = null;
            try
            {
                // Create an empty page
                PdfPage page = document.AddPage();
                page.Size = PdfSharp.PageSize.A4;

                // Get an XGraphics object for drawing
                gfx = XGraphics.FromPdfPage(page);

                XSize size = gfx.PageSize;
                size.Height -= 10;
                size.Width -= 40;
                XRect rect = new XRect(new XPoint(), size);
                rect.Inflate(-5, 85);

                double offsetY = 100;
                // Create a format
                XStringFormat format = new XStringFormat();

                // Create a font
                XFont font = new XFont("Arial", 8, XFontStyle.Bold);

                format.Alignment = XStringAlignment.Near;//Left

                #region header
                // Draw the text
                rect.Offset(25, offsetY);
                gfx.DrawString("Q2C :: Machine evaluations", font, XBrushes.Black, rect, format);

                // If you're going to read from the stream, you may need to reset the position to the start
                Bitmap _icon = Q2C.Properties.Resources.q2c_icon;
                MemoryStream msIcon = new MemoryStream();
                msIcon.Position = 0;
                _icon.Save(msIcon, System.Drawing.Imaging.ImageFormat.Png);
                XImage image = XImage.FromStream(msIcon);

                // Left position in point
                double center_x = (size.Width - 60) / 2 + 45;
                gfx.DrawImage(image, center_x, 10, 24, 18);

                string strDate = DateTime.Now.ToLongDateString() + " " + DateTime.Now.ToShortTimeString();
                // Draw the text
                format.Alignment = XStringAlignment.Far;//Right
                gfx.DrawString(strDate, font, XBrushes.Black, rect, format);

                XColor line_color = XColor.FromArgb(71, 130, 136);
                XPen line = new XPen(line_color, 1.2);
                gfx.DrawLine(line, 23, 35, 580, 35);

                #endregion
                #region footer 
                gfx.DrawLine(line, 23, 820, 580, 820);

                font = new XFont("Arial", 8, XFontStyle.Bold);
                format.Alignment = XStringAlignment.Far;
                format.LineAlignment = XLineAlignment.Far;
                rect.Offset(0, -180);
                gfx.DrawString("Page: " + document.PageCount.ToString(), font, XBrushes.Black, rect, format);
                #endregion
            }
            catch (Exception)
            {
                return 1;
            }
            return returnOK;
        }
        public static byte CreatePDF(out PdfDocument document, out XGraphics gfx)
        {
            byte returnOK = 0;//0 -> ok, 1 -> failed, 2 -> cancel
            document = new PdfDocument();
            gfx = null;
            try
            {
                // Create a new PDF document
                document.Info.Title = "Q2C :: Machine evaluations";
                document.Info.Author = "Q2C";

                returnOK = 0;

            }
            catch (Exception)
            {
                returnOK = 1;
            }
            return returnOK;

        }
        public static byte ClosePDF(string fileName, PdfDocument document)
        {
            //0 -> ok, 1 -> failed, 2 -> cancel

            if (document == null) return 1;
            document.Save(fileName);
            return 0;

        }
    }
}
