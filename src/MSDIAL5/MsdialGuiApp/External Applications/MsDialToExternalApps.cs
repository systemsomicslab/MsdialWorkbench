using CompMs.App.Msdial.Model.DataObj;
using CompMs.Common.DataObj;
using CompMs.Common.Enum;
using CompMs.Common.Interfaces;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Export;
using CompMs.MsdialCore.Parameter;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows;

namespace CompMs.App.Msdial.ExternalApp
{
    internal static class MsDialToExternalApps
    {
        public static void SendToMsFinderProgram(
            IFileBean file,
            ChromatogramPeakFeature feature,
            IMSScanProperty scan,
            IReadOnlyList<RawSpectrum> spectrumList,
            DataBaseMapper mapper,
            ParameterBase parameter)
        {
            var msdialIni = MsDialIniParcer.Read();
            if (!CheckFilePath(msdialIni)) return;

            var msdialTempDir = Path.Combine(Path.GetDirectoryName(msdialIni.MsfinderFilePath), "MSDIAL_TEMP");
            FolderCheckInMsFinder(msdialTempDir);

            var dt = DateTime.Now;
            var timeString = dt.Year.ToString() + "_" + dt.Month.ToString() + "_" + dt.Day.ToString() + "_" + dt.Hour.ToString() + dt.Minute.ToString();
            var id = feature.MasterPeakID;
            var fileString = file.FileName;
            var filePath = Path.Combine(msdialTempDir, timeString + "_" + fileString + "_" + id + "." + ExportSpectraFileFormat.mat);

            using (var fileStream = File.Open(filePath, FileMode.Create)) {
                SpectraExport.SaveSpectraTable(
                    ExportSpectraFileFormat.mat,
                    fileStream,
                    feature,
                    scan,
                    spectrumList,
                    mapper,
                    parameter);
            }

            var process = new Process();
            process.StartInfo.FileName = msdialIni.MsfinderFilePath;
            process.StartInfo.Arguments = msdialTempDir;
            process.Start();
        }

        public static void SendToMsFinderProgram(
           AlignmentFileBeanModel file,
           AlignmentSpotProperty feature,
           IMSScanProperty scan,
           DataBaseMapper mapper,
           ParameterBase parameter) {
            var msdialIni = MsDialIniParcer.Read();
            if (!CheckFilePath(msdialIni)) return;

            var msdialTempDir = Path.Combine(Path.GetDirectoryName(msdialIni.MsfinderFilePath), "MSDIAL_TEMP");
            FolderCheckInMsFinder(msdialTempDir);

            var dt = DateTime.Now;
            var timeString = dt.Year.ToString() + "_" + dt.Month.ToString() + "_" + dt.Day.ToString() + "_" + dt.Hour.ToString() + dt.Minute.ToString();
            var id = feature;
            var fileString = file.FileName;
            var filePath = Path.Combine(msdialTempDir, timeString + "_" + fileString + "_" + id + "." + ExportSpectraFileFormat.mat);

            using (var fileStream = File.Open(filePath, FileMode.Create)) {
                SpectraExport.SaveSpectraTable(
                    ExportSpectraFileFormat.mat,
                    fileStream,
                    feature,
                    scan,
                    mapper,
                    parameter);
            }

            var process = new Process();
            process.StartInfo.FileName = msdialIni.MsfinderFilePath;
            process.StartInfo.Arguments = msdialTempDir;
            process.Start();
        }

        public static void SendToMsFinderProgramForGcms(IFileBean file, SpectrumFeature spectrumFeature, ProjectBaseParameter parameter)
        {
            var msdialIni = MsDialIniParcer.Read();
            if (!CheckFilePath(msdialIni)) return;

            var msdialTempDir = Path.Combine(Path.GetDirectoryName(msdialIni.MsfinderFilePath), "MSDIAL_TEMP");
            FolderCheckInMsFinder(msdialTempDir);

            var dt = DateTime.Now;
            var timeString = dt.Year.ToString() + "_" + dt.Month.ToString() + "_" + dt.Day.ToString() + "_" + dt.Hour.ToString() + dt.Minute.ToString();
            var id = spectrumFeature.AnnotatedMSDecResult.MSDecResult.ScanID;
            var fileString = file.FileName;
            var filePath = Path.Combine(msdialTempDir, timeString + "_" + fileString + "_" + id + "." + ExportSpectraFileFormat.mat);

            using (var fileStream = File.Open(filePath, FileMode.Create)) {
                SpectraExport.SaveSpectraTableForGcmsAsMatFormat(fileStream, spectrumFeature.AnnotatedMSDecResult.MSDecResult, spectrumFeature.AnnotatedMSDecResult.Molecule, spectrumFeature.QuantifiedChromatogramPeak.PeakFeature, parameter);
            }

            var process = new Process();
            process.StartInfo.FileName = msdialIni.MsfinderFilePath;
            process.StartInfo.Arguments = msdialTempDir;
            process.Start();
        }

        public static void SendToMsFinderProgramForGcms(AlignmentFileBeanModel file, AlignmentSpotProperty feature, IMSScanProperty scan, ProjectBaseParameter parameter) {
            var msdialIni = MsDialIniParcer.Read();
            if (!CheckFilePath(msdialIni)) return;

            var msdialTempDir = Path.Combine(Path.GetDirectoryName(msdialIni.MsfinderFilePath), "MSDIAL_TEMP");
            FolderCheckInMsFinder(msdialTempDir);

            var dt = DateTime.Now;
            var timeString = dt.Year.ToString() + "_" + dt.Month.ToString() + "_" + dt.Day.ToString() + "_" + dt.Hour.ToString() + dt.Minute.ToString();
            var id = feature;
            var fileString = file.FileName;
            var filePath = Path.Combine(msdialTempDir, timeString + "_" + fileString + "_" + id + "." + ExportSpectraFileFormat.mat);

            using (var fileStream = File.Open(filePath, FileMode.Create)) {
                SpectraExport.SaveSpectraTableForGcmsAsMatFormat(fileStream, scan, feature, feature, parameter);
            }

            var process = new Process();
            process.StartInfo.FileName = msdialIni.MsfinderFilePath;
            process.StartInfo.Arguments = msdialTempDir;
            process.Start();
        }

        private static void FolderCheckInMsFinder(string msdialTempDir)
        {
            if (Directory.Exists(msdialTempDir)) return;
            else
            {
                _ = Directory.CreateDirectory(msdialTempDir);
            }
        }

        private static bool CheckFilePath(MsDialIniField iniField)
        {
            var path = iniField.MsfinderFilePath;
            while (!File.Exists(path))
            {
                if (MessageBox.Show("The file path of MS-FINDER program is not correct. Please select the correct path.", "Error", MessageBoxButton.OKCancel, MessageBoxImage.Error) == MessageBoxResult.Cancel)
                    return false;

                var ofd = new OpenFileDialog
                {
                    Filter = "MS-FINDER Exe (*.exe)|*.exe",
                    Title = "Select MS-FINDER Exe File",
                    RestoreDirectory = true
                };

                if (ofd.ShowDialog() == true) {
                    path = ofd.FileName;
                }
                else {
                    return false;
                }
            }
            
            iniField.MsfinderFilePath = path;
            MsDialIniParcer.Write(iniField);

            return true;
        }

    }
}
