using CompMs.Common.Components;
using CompMs.Common.DataObj;
using CompMs.Common.DataObj.Property;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.Common.Extension;
using CompMs.Common.Interfaces;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace CompMs.MsdialCore.Export
{
    public static class SpectraExport
    {
        public static void SaveSpectraTable(
            ExportSpectraFileFormat spectraFormat, 
            Stream exportStream,
            ChromatogramPeakFeature chromPeakFeature, 
            IMSScanProperty scan,
            IReadOnlyList<RawSpectrum> spectrumList,
            DataBaseMapper mapper,
            ParameterBase parameter) {
            switch (spectraFormat) {
                case ExportSpectraFileFormat.msp:
                    SaveSpectraTableAsNistFormat(exportStream, chromPeakFeature, scan.Spectrum, mapper, parameter);
                    break;
                case ExportSpectraFileFormat.mgf:
                    SaveSpectraTableAsMgfFormat(exportStream, chromPeakFeature, scan.Spectrum);
                    break;
                case ExportSpectraFileFormat.mat:
                    SaveSpectraTableAsMatFormat(exportStream, chromPeakFeature, scan.Spectrum, spectrumList, mapper, parameter);
                    break;
                case ExportSpectraFileFormat.ms:
                    SaveSpectraTableAsSiriusMsFormat(exportStream, chromPeakFeature, scan.Spectrum, spectrumList, mapper, parameter);
                    break;
                default:
                    SaveSpectraTableAsNistFormat(exportStream, chromPeakFeature, scan.Spectrum, mapper, parameter);
                    break;
            }
        }

        public static void SaveSpectraTable(
            ExportSpectraFileFormat spectraFormat, 
            Stream exportStream,
            AlignmentSpotProperty spotProperty, 
            IMSScanProperty scan,
            DataBaseMapper mapper,
            ParameterBase parameter,
            AlignmentSpotProperty isotopeTrackedLastSpot = null) {
            switch (spectraFormat) {
                case ExportSpectraFileFormat.msp:
                    SaveSpectraTableAsNistFormat(exportStream, spotProperty, scan.Spectrum, mapper, parameter);
                    break;
                case ExportSpectraFileFormat.mgf:
                    SaveSpectraTableAsMgfFormat(exportStream, spotProperty, scan.Spectrum);
                    break;
                case ExportSpectraFileFormat.mat:
                    SaveSpectraTableAsMatFormat(exportStream, spotProperty, scan.Spectrum, mapper, parameter, isotopeTrackedLastSpot);
                    break;
                case ExportSpectraFileFormat.ms:
                    SaveSpectraTableAsSiriusMsFormat(exportStream, spotProperty, scan.Spectrum, mapper, parameter);
                    break;
                default:
                    SaveSpectraTableAsNistFormat(exportStream, spotProperty, scan.Spectrum, mapper, parameter);
                    break;
            }
        }
        
        #region msp
        /// <summary>
        /// Saves spectral data along with chromatographic peak and molecular property information in NIST MSP format to a specified stream.
        /// </summary>
        /// <typeparam name="T">The type of the chromatographic peak feature which must implement IMoleculeProperty, IChromatogramPeak, IIonProperty, and IAnnotatedObject interfaces.</typeparam>
        /// <param name="stream">The stream to which the NIST format data will be written.</param>
        /// <param name="chromPeakFeature">The chromatographic peak feature containing molecular and ion property information.</param>
        /// <param name="massSpectra">A collection of mass spectrum peaks to be included in the output.</param>
        /// <param name="refer">A reference object that maps molecular mass spectrum references to their scan match results.</param>
        /// <param name="parameter">Parameters used for generating the spectral data output.</param>
        /// <remarks>
        /// This method writes the spectral data, including molecular information and chromatographic peak features, into the stream in the NIST MSP format.
        /// It is intended for serialization of complex spectral data for further analysis or sharing within the scientific community.
        /// </remarks>
        public static void SaveSpectraTableAsNistFormat<T>(
            Stream stream,
            T chromPeakFeature,
            IEnumerable<ISpectrumPeak> massSpectra,
            IMatchResultRefer<MoleculeMsReference, MsScanMatchResult> refer,
            ParameterBase parameter)
            where T: IMoleculeProperty, IChromatogramPeak, IIonProperty, IAnnotatedObject {
            var builder = new NistRecordBuilder();
            builder.SetNameProperty(chromPeakFeature.Name);
            builder.SetChromatogramPeakProperties(chromPeakFeature);
            switch (chromPeakFeature) {
                case ChromatogramPeakFeature peak:
                    builder.SetComment(peak);
                    break;
                case AlignmentSpotProperty spot:
                    builder.SetComment(spot);
                    break;
            }
            builder.SetMoleculeProperties(chromPeakFeature.Refer(refer));
            builder.SetIonPropertyProperties(chromPeakFeature);
            builder.SetProjectParameterProperties(parameter.ProjectParam);
            builder.SetScan(new MSScanProperty { Spectrum = massSpectra.Select(p => new SpectrumPeak { Mass = p.Mass, Intensity = p.Intensity }).ToList() });
            builder.Export(stream);
        }

        public static void SaveSpectraTableAsNistFormat(
            string exportFilePath,
            ChromatogramPeakFeature chromPeakFeature,
            IEnumerable<ISpectrumPeak> massSpectra,
            IMatchResultRefer<MoleculeMsReference, MsScanMatchResult> refer,
            ParameterBase parameter) {
            using (var file = File.Open(exportFilePath, FileMode.Create)) {
                SaveSpectraTableAsNistFormat(file, chromPeakFeature, massSpectra, refer, parameter);
            }
        }

        public static void SaveSpectraTableAsNistFormat(
            Stream stream,
            ChromatogramPeakFeature chromPeakFeature,
            IEnumerable<ISpectrumPeak> massSpectra,
            IMatchResultRefer<MoleculeMsReference?, MsScanMatchResult?> refer,
            ParameterBase parameter) {
            var builder = new NistRecordBuilder();
            builder.SetNameProperty(chromPeakFeature.Name);
            builder.SetChromatogramPeakFeatureProperties(chromPeakFeature, chromPeakFeature.MasterPeakID);
            builder.SetChromatogramPeakProperties(chromPeakFeature);
            builder.SetComment(chromPeakFeature);
            builder.SetMoleculeProperties(chromPeakFeature.Refer(refer));
            builder.SetIonPropertyProperties(chromPeakFeature);
            builder.SetProjectParameterProperties(parameter.ProjectParam);
            builder.SetScan(new MSScanProperty { Spectrum = massSpectra.Select(p => new SpectrumPeak { Mass = p.Mass, Intensity = p.Intensity }).ToList() });
            builder.Export(stream);
        }

        public static void SaveSpectraTableAsNistFormat(
            string exportFilePath,
            AlignmentSpotProperty spotProperty,
            IEnumerable<ISpectrumPeak> massSpectra,
            IMatchResultRefer<MoleculeMsReference, MsScanMatchResult> refer,
            ParameterBase parameter) {

            using (var file = File.Open(exportFilePath, FileMode.Create)) {
                SaveSpectraTableAsNistFormat(file, spotProperty, massSpectra, refer, parameter);
            }
        }

        public static void SaveSpectraTableAsNistFormat(
            Stream stream,
            AlignmentSpotProperty spotProperty,
            IEnumerable<ISpectrumPeak> massSpectra,
            IMatchResultRefer<MoleculeMsReference, MsScanMatchResult> mapper,
            ParameterBase parameter) {
            var builder = new NistRecordBuilder();
            builder.SetNameProperty(spotProperty.Name);
            builder.SetChromatogramPeakProperties(spotProperty);
            builder.SetComment(spotProperty);
            builder.SetMoleculeProperties(spotProperty.Refer(mapper));
            builder.SetIonPropertyProperties(spotProperty);
            builder.SetProjectParameterProperties(parameter.ProjectParam);
            builder.SetScan(new MSScanProperty { Spectrum = massSpectra.Select(p => new SpectrumPeak { Mass = p.Mass, Intensity = p.Intensity }).ToList() });
            builder.Export(stream);
        }

        private static void WriteChromPeakFeatureInfoAsMSP(
            StreamWriter sw,
            ChromatogramPeakFeature feature,
            IMatchResultRefer<MoleculeMsReference, MsScanMatchResult> refer) {
            sw.WriteLine("NAME: " + GetNameField(feature));
            sw.WriteLine("PRECURSORMZ: " + feature.PrecursorMz);
            sw.WriteLine("PRECURSORTYPE: " + feature.AdductType.AdductIonName);
            WriteChromXFieldAsMSP(sw, feature.PeakFeature.ChromXsTop, feature.CollisionCrossSection);
            sw.WriteLine("FORMULA: " + feature.GetFormula(refer));
            sw.WriteLine("ONTOLOGY: " + feature.GetOntology(refer));
            sw.WriteLine("INCHIKEY: " + feature.GetInChIKey(refer));
            sw.WriteLine("SMILES: " + feature.GetSMILES(refer));
            sw.WriteLine("COMMENT: " + GetCommentField(feature));
        }

        private static void WriteChromPeakFeatureInfoAsMSP(
            StreamWriter sw,
            AlignmentSpotProperty feature,
            IMatchResultRefer<MoleculeMsReference, MsScanMatchResult> refer) {
            sw.WriteLine("NAME: " + GetNameField(feature));
            sw.WriteLine("PRECURSORMZ: " + feature.MassCenter);
            sw.WriteLine("PRECURSORTYPE: " + feature.AdductType.AdductIonName);
            WriteChromXFieldAsMSP(sw, feature.TimesCenter, feature.CollisionCrossSection);
            sw.WriteLine("FORMULA: " + feature.GetFormula(refer));
            sw.WriteLine("ONTOLOGY: " + feature.GetOntology(refer));
            sw.WriteLine("INCHIKEY: " + feature.GetInChIKey(refer));
            sw.WriteLine("SMILES: " + feature.GetSMILES(refer));
            sw.WriteLine("COMMENT: " + GetCommentField(feature));
        }

        private static void WriteChromPeakFeatureInfoAsMSP<T>(
            StreamWriter sw,
            T feature,
            IMatchResultRefer<MoleculeMsReference, MsScanMatchResult> refer)
            where T: IMoleculeProperty, IChromatogramPeak, IIonProperty, IAnnotatedObject {
            sw.WriteLine("NAME: " + GetNameField(feature));
            sw.WriteLine("PRECURSORMZ: " + feature.Mass);
            sw.WriteLine("PRECURSORTYPE: " + feature.AdductType.AdductIonName);
            WriteChromXFieldAsMSP(sw, feature.ChromXs, feature.CollisionCrossSection);
            sw.WriteLine("FORMULA: " + feature.GetFormula(refer));
            sw.WriteLine("ONTOLOGY: " + feature.GetOntology(refer));
            sw.WriteLine("INCHIKEY: " + feature.GetInChIKey(refer));
            sw.WriteLine("SMILES: " + feature.GetSMILES(refer));
            sw.WriteLine("COMMENT: " + GetCommentField(feature));
        }

        private static void WriteChromXFieldAsMSP(
            StreamWriter sw,
            ChromXs chromXs,
            double ccs) {
            if (chromXs.RT.Value > 0)
                sw.WriteLine("RETENTIONTIME: " + chromXs.RT.Value);
            if (chromXs.RI.Value > 0)
                sw.WriteLine("RETENTIONINDEX: " + chromXs.RI.Value);
            if (chromXs.Drift.Value > 0) {
                sw.WriteLine("MOBILITY: " + chromXs.Drift.Value);
                sw.WriteLine("CCS: " + ccs);
            }
        }
        #endregion

        #region mgf
        public static void SaveSpectraTableAsMgfFormat(
            Stream stream,
            ChromatogramPeakFeature chromPeakFeature,
            IEnumerable<ISpectrumPeak> massSpectra) {
            using (StreamWriter sw = new StreamWriter(stream, Encoding.ASCII, 4096, true)) {
                sw.WriteLine("BEGIN IONS");
                WriteChromPeakFeatureInfoAsMgf(sw, chromPeakFeature);
                WriteSpectrumPeakInfo(sw, massSpectra);
                sw.WriteLine("END IONS");
                sw.WriteLine();
            }
        }

        public static void SaveSpectraTableAsMgfFormat(Stream stream, AlignmentSpotProperty spotProperty, IEnumerable<ISpectrumPeak> spectrum) {
            using (StreamWriter sw = new StreamWriter(stream, Encoding.ASCII, 4096, true)) {
                sw.WriteLine("BEGIN IONS");
                WriteChromPeakFeatureInfoAsMgf(sw, spotProperty);
                WriteSpectrumPeakInfo(sw, spectrum);
                sw.WriteLine("END IONS");
                sw.WriteLine();
            }
        }

        public static void WriteChromPeakFeatureInfoAsMgf(StreamWriter sw, ChromatogramPeakFeature feature) {
            var nameField = GetNameField(feature);
            var commentField = GetCommentField(feature);
            var chargeChar = feature.AdductType.IonMode == IonMode.Positive ? "+" : "-";
            var chargeString = feature.AdductType.ChargeNumber + chargeChar;

            sw.WriteLine("TITLE=" + nameField + "|" + commentField);
            sw.WriteLine("PEPMASS=" + feature.PrecursorMz);
            sw.WriteLine("ION=" + feature.AdductType.AdductIonName);
            sw.WriteLine("CHARGE=" + chargeString);
            WriteChromXFieldAsMGF(sw, feature.PeakFeature.ChromXsTop, feature.CollisionCrossSection);
        }

        public static void WriteChromPeakFeatureInfoAsMgf(
            StreamWriter sw,
            AlignmentSpotProperty feature) {
            var nameField = GetNameField(feature);
            var commentField = GetCommentField(feature);
            var chargeChar = feature.AdductType.IonMode == IonMode.Positive ? "+" : "-";
            var chargeString = feature.AdductType.ChargeNumber + chargeChar;

            sw.WriteLine("TITLE=" + nameField + "|" + commentField);
            sw.WriteLine("PEPMASS=" + feature.MassCenter);
            sw.WriteLine("ION=" + feature.AdductType.AdductIonName);
            sw.WriteLine("CHARGE=" + chargeString);
            WriteChromXFieldAsMGF(sw, feature.TimesCenter, feature.CollisionCrossSection);
        }

        private static void WriteChromXFieldAsMGF(
            StreamWriter sw,
            ChromXs chromXs,
            double ccs) {
            if (chromXs.RT.Value > 0)
                sw.WriteLine("RTINMINUTES=" + chromXs.RT.Value);
            if (chromXs.RI.Value > 0)
                sw.WriteLine("RETENTIONINDEX=" + chromXs.RI.Value);
            if (chromXs.Drift.Value > 0) {
                sw.WriteLine("MOBILITY=" + chromXs.Drift.Value);
                sw.WriteLine("CCS=" + ccs);
            }
        }
        #endregion

        #region mat
        private static void SaveSpectraTableAsMatFormat(
            Stream stream, 
            ChromatogramPeakFeature feature, 
            IEnumerable<SpectrumPeak> spectrum,
            IReadOnlyList<RawSpectrum> spectrumList,
            IMatchResultRefer<MoleculeMsReference, MsScanMatchResult> refer, 
            ParameterBase parameter) {
            using (StreamWriter sw = new StreamWriter(stream, Encoding.ASCII, 4096, true)) {
                WriteChromPeakFeatureInfoAsMSP(sw, feature, refer);
                sw.WriteLine("IONMODE: " + feature.IonMode);
                WriteParameterInfoAsNist(sw, parameter);
                var ms1Spectrum = spectrumList.FirstOrDefault(spec => spec.OriginalIndex == feature.MS1RawSpectrumIdTop);
                if (ms1Spectrum != null) {
                    var isotopes = DataAccess.GetIsotopicPeaks(ms1Spectrum.Spectrum, (float)feature.PrecursorMz, parameter.CentroidMs1Tolerance, parameter.PeakPickBaseParam.MaxIsotopesDetectedInMs1Spectrum);
                    if (!isotopes.IsEmptyOrNull()) {
                        sw.WriteLine("MSTYPE: MS1");
                        WriteSpectrumPeakInfo(sw, isotopes);
                    }
                }
                sw.WriteLine("MSTYPE: MS2");
                WriteSpectrumPeakInfo(sw, spectrum);
                sw.WriteLine();
            }
        }

        public static void SaveSpectraTableAsMatFormat(
            Stream stream,
            AlignmentSpotProperty feature,
            IEnumerable<SpectrumPeak> spectrum, 
            IMatchResultRefer<MoleculeMsReference, MsScanMatchResult> refer,
            ParameterBase parameter,
            AlignmentSpotProperty isotopeTrackedLastSpot) {
            using (StreamWriter sw = new StreamWriter(stream, Encoding.ASCII, 4096, true)) {
                WriteChromPeakFeatureInfoAsMSP(sw, feature, refer);
                sw.WriteLine("IONMODE: " + feature.IonMode);
                if (isotopeTrackedLastSpot != null) {
                    WriteIsotopeTrackingFeature(sw, feature, parameter, isotopeTrackedLastSpot);
                }
                WriteParameterInfoAsNist(sw, parameter);
                var isotopes = feature.IsotopicPeaks;
                if (!isotopes.IsEmptyOrNull()) {
                    sw.WriteLine("MSTYPE: MS1");
                    WriteSpectrumPeakInfo(sw, isotopes);
                }
                sw.WriteLine("MSTYPE: MS2");
                WriteSpectrumPeakInfo(sw, spectrum);
                sw.WriteLine();
            }
        }

        public static void SaveSpectraTableForGcmsAsMatFormat(Stream stream, IMSScanProperty scan, IMoleculeProperty molecule, IChromatogramPeakFeature peakFeature, ProjectBaseParameter projectParameter) {
            SaveSpectraTableForGcmsAsMatFormatCore(stream, scan, molecule, peakFeature.Mass, peakFeature.PeakHeightTop, projectParameter);
        }

        public static void SaveSpectraTableForGcmsAsMatFormat(Stream stream, IMSScanProperty scan, IMoleculeProperty molecule, IChromatogramPeak peak, ProjectBaseParameter projectParameter) {
            SaveSpectraTableForGcmsAsMatFormatCore(stream, scan, molecule, peak.Mass, peak.Intensity, projectParameter);
        }

        private static void SaveSpectraTableForGcmsAsMatFormatCore(Stream stream, IMSScanProperty scan, IMoleculeProperty molecule, double quantmass, double peakHeight, ProjectBaseParameter projectParameter) {
            using (StreamWriter sw = new StreamWriter(stream, Encoding.ASCII, bufferSize: 4096, leaveOpen: true))
            {
                sw.Write("NAME: ");
                sw.WriteLine(scan.ScanID + "-" + scan.ChromXs.RT.Value + "-" + scan.ChromXs.RI.Value + "-" + peakHeight);

                sw.WriteLine("RETENTIONTIME: " + scan.ChromXs.RT.Value);
                sw.WriteLine("RETENTIONINDEX: " + scan.ChromXs.RI.Value);
                sw.WriteLine("QUANTMASS: " + quantmass);

                var precursorMz = quantmass;
                if (scan.Spectrum.Count > 0) {
                    precursorMz = scan.Spectrum.Max(s => s.Mass);
                }

                sw.WriteLine("PRECURSORMZ: " + precursorMz);

                sw.WriteLine("PRECURSORTYPE: " + "[M-CH3]+.");

                sw.WriteLine("IONMODE: " + "Positive");
                sw.WriteLine("SPECTRUMTYPE: Centroid");
                sw.WriteLine("INTENSITY: " + peakHeight);

                sw.WriteLine("INCHIKEY: " + molecule.InChIKey);
                sw.WriteLine("SMILES: " + molecule.SMILES);
                sw.WriteLine("FORMULA: " + molecule.Formula);

                if (projectParameter.FinalSavedDate != default) {
                    sw.WriteLine("DATE: " + projectParameter.FinalSavedDate.Date);
                }

                if (!string.IsNullOrEmpty(projectParameter.Authors)) {
                    sw.WriteLine("AUTHORS: " + projectParameter.Authors);
                }

                if (!string.IsNullOrEmpty(projectParameter.License)) {
                    sw.WriteLine("LICENSE: " + projectParameter.License);
                }

                if (!string.IsNullOrEmpty(projectParameter.CollisionEnergy)) {
                    sw.WriteLine("COLLISIONENERGY: " + projectParameter.CollisionEnergy);
                }

                if (!string.IsNullOrEmpty(projectParameter.InstrumentType)) {
                    sw.WriteLine("INSTRUMENTTYPE: " + projectParameter.InstrumentType);
                }

                if (!string.IsNullOrEmpty(projectParameter.Instrument)) {
                    sw.WriteLine("INSTRUMENT: " + projectParameter.Instrument);
                }

                if (!string.IsNullOrEmpty(projectParameter.Comment)) {
                    sw.WriteLine("COMMENT: " + projectParameter.Comment);
                }

                sw.WriteLine("MSTYPE: MS1");
                sw.WriteLine("Num Peaks: " + scan.Spectrum.Count);

                for (int i = 0; i < scan.Spectrum.Count; i++)
                    sw.WriteLine(Math.Round(scan.Spectrum[i].Mass, 5) + "\t" + Math.Round(scan.Spectrum[i].Intensity, 0));

                sw.WriteLine("MSTYPE: MS2");
                sw.WriteLine("Num Peaks: " + scan.Spectrum.Count);

                for (int i = 0; i < scan.Spectrum.Count; i++)
                    sw.WriteLine(Math.Round(scan.Spectrum[i].Mass, 5) + "\t" + Math.Round(scan.Spectrum[i].Intensity, 0));
            }
        }
        private static void WriteIsotopeTrackingFeature(
            StreamWriter sw, 
            AlignmentSpotProperty feature, 
            ParameterBase parameter, 
            AlignmentSpotProperty lastFeature) {
            var isotopeLabel = parameter.IsotopeTrackingDictionary;
            var labelType = isotopeLabel.IsotopeElements[isotopeLabel.SelectedID].ElementName;
            var isotopeTrackNum = lastFeature.PeakCharacter.IsotopeWeightNumber;
            sw.WriteLine("#Specific field for labeled experiment");
            switch (labelType) {
                case "13C":
                    sw.WriteLine("CarbonCount: " + isotopeTrackNum);
                    break;
                case "15N":
                    sw.WriteLine("NitrogenCount: " + isotopeTrackNum);
                    break;
                case "34S":
                    sw.WriteLine("SulfurCount: " + isotopeTrackNum);
                    break;
                case "18O":
                    sw.WriteLine("OxygenCount: " + isotopeTrackNum);
                    break;
                case "13C+15N":
                    sw.WriteLine("CarbonNitrogenCount: " + isotopeTrackNum);
                    break;
                case "13C+34S":
                    sw.WriteLine("CarbonSulfurCount: " + isotopeTrackNum);
                    break;
                case "15N+34S":
                    sw.WriteLine("NitrogenSulfurCount: " + isotopeTrackNum);
                    break;
                case "13C+15N+34S":
                    sw.WriteLine("CarbonNitrogenSulfurCount: " + isotopeTrackNum);
                    break;
            }
        }

        #endregion

        #region sirius ms
        private static void SaveSpectraTableAsSiriusMsFormat(
            Stream stream, 
            ChromatogramPeakFeature feature,
            IEnumerable<SpectrumPeak> spectrum, 
            IReadOnlyList<RawSpectrum> spectrumList, 
            DataBaseMapper mapper, 
            ParameterBase parameter) {
            using (StreamWriter sw = new StreamWriter(stream, Encoding.ASCII, 4096, true)) {
                WriteChromPeakFeatureInfoAsSiriusMs(sw, feature, mapper);
                sw.WriteLine();

                var ms1Spectrum = spectrumList.FirstOrDefault(spec => spec.OriginalIndex == feature.MS1RawSpectrumIdTop);
                if (ms1Spectrum != null) {
                    var isotopes = DataAccess.GetIsotopicPeaks(ms1Spectrum.Spectrum, (float)feature.PrecursorMz, parameter.CentroidMs1Tolerance, parameter.PeakPickBaseParam.MaxIsotopesDetectedInMs1Spectrum);
                    if (!isotopes.IsEmptyOrNull()) {
                        sw.WriteLine(">ms1");
                        WriteSpectrumPeakInfo(sw, isotopes);
                    }
                }
                sw.WriteLine();
                sw.WriteLine(">ms2");
                WriteSpectrumPeakInfo(sw, spectrum);
                sw.WriteLine();
            }
        }

        private static void SaveSpectraTableAsSiriusMsFormat(
            Stream stream,
            AlignmentSpotProperty feature,
            IEnumerable<SpectrumPeak> spectrum,
            DataBaseMapper mapper,
            ParameterBase parameter) {
            using (StreamWriter sw = new StreamWriter(stream, Encoding.ASCII, 4096, true)) {
                WriteChromPeakFeatureInfoAsSiriusMs(sw, feature, mapper);
                sw.WriteLine();
                var isotopes = feature.IsotopicPeaks;
                if (!isotopes.IsEmptyOrNull()) {
                    sw.WriteLine(">ms1");
                    WriteSpectrumPeakInfo(sw, isotopes);
                }
                
                sw.WriteLine();
                sw.WriteLine(">ms2");
                WriteSpectrumPeakInfo(sw, spectrum);
                sw.WriteLine();
            }
        }

        private static void WriteChromPeakFeatureInfoAsSiriusMs(
            StreamWriter sw, 
            ChromatogramPeakFeature feature, 
            DataBaseMapper mapper) {
            sw.WriteLine(">compound " + GetNameField(feature));
            sw.WriteLine(">parentmass " + feature.PrecursorMz);
            sw.WriteLine(">ionization " + feature.AdductType.AdductIonName);
            sw.WriteLine(">formula " + feature.GetFormula(mapper));
        }

        private static void WriteChromPeakFeatureInfoAsSiriusMs(
            StreamWriter sw,
            AlignmentSpotProperty feature,
            DataBaseMapper mapper) {
            sw.WriteLine(">compound " + GetNameField(feature));
            sw.WriteLine(">parentmass " + feature.MassCenter);
            sw.WriteLine(">ionization " + feature.AdductType.AdductIonName);
            sw.WriteLine(">formula " + feature.GetFormula(mapper));
        }
        #endregion

        private static string GetCommentField(ChromatogramPeakFeature feature) {
            var comment = feature.Comment;
            var id = "|PEAKID=" + feature.MasterPeakID.ToString();
            var ms1 = "|MS1SCAN=" + feature.MS1RawSpectrumIdTop;
            var ms2 = "|MS2SCAN=" + feature.MS2RawSpectrumID;
            var height = "|PEAKHEIGHT=" + Math.Round(feature.PeakFeature.PeakHeightTop, 0).ToString();
            var area = "|PEAKAREA=" + Math.Round(feature.PeakFeature.PeakAreaAboveZero, 0).ToString();
            var isotope = "|ISOTOPE=" + "M+" + feature.PeakCharacter.IsotopeWeightNumber.ToString();
            return comment + id + ms1 + ms2 + height + area + isotope;
        }

        private static string GetCommentField(AlignmentSpotProperty feature) {
            var comment = feature.Comment;
            var id = "|PEAKID=" + feature.MasterAlignmentID.ToString();
            var isotope = "|ISOTOPE=" + "M+" + feature.PeakCharacter.IsotopeWeightNumber.ToString();
            return comment + id + isotope;
        }

        private static string GetCommentField(IChromatogramPeak feature) {
            if (feature is ChromatogramPeakFeature chromatogramPeakFeature) {
                return GetCommentField(chromatogramPeakFeature);
            }
            if (feature is AlignmentSpotProperty alignmentSpotProperty) {
                return GetCommentField(alignmentSpotProperty);
            }
            return $"PEAKID={feature.ID}|PEAKHEIGHT={Math.Round(feature.Intensity, 0)}";
        }

        private static string GetNameField(ChromatogramPeakFeature feature) {
            if (feature.Name.IsEmptyOrNull() || feature.Name.ToLower() == "unknown") {
                var id = "|ID=" + feature.MasterPeakID.ToString();
                var rt = feature.PeakFeature.ChromXsTop.RT.Value > 0 ? "|RT=" + Math.Round(feature.PeakFeature.ChromXsTop.RT.Value, 3) : string.Empty;
                var ri = feature.PeakFeature.ChromXsTop.RI.Value > 0 ? "|RI=" + Math.Round(feature.PeakFeature.ChromXsTop.RI.Value, 3) : string.Empty;
                var dt = feature.PeakFeature.ChromXsTop.Drift.Value > 0 ? "|DT=" + Math.Round(feature.PeakFeature.ChromXsTop.Drift.Value, 3) : string.Empty;
                var mz = "|MZ=" + Math.Round(feature.PrecursorMz, 4).ToString();
                return "Unknown" + id + mz + rt + ri + dt;
            }
            else {
                return feature.Name;
            }
        }

        private static string GetNameField(AlignmentSpotProperty feature) {
            if (feature.Name.IsEmptyOrNull() || feature.Name.ToLower() == "unknown") {
                var id = "|ID=" + feature.MasterAlignmentID;
                var rt = feature.TimesCenter.RT.Value > 0 ? "|RT=" + Math.Round(feature.TimesCenter.RT.Value, 3) : string.Empty;
                var ri = feature.TimesCenter.RI.Value > 0 ? "|RI=" + Math.Round(feature.TimesCenter.RI.Value, 3) : string.Empty;
                var dt = feature.TimesCenter.Drift.Value > 0 ? "|DT=" + Math.Round(feature.TimesCenter.Drift.Value, 3) : string.Empty;
                var mz = "|MZ=" + Math.Round(feature.MassCenter, 4).ToString();
                return "Unknown" + id + mz + rt + ri + dt;
            }
            else {
                return feature.Name;
            }
        }

        private static string GetNameField<T>(T feature) where T : IMoleculeProperty, IChromatogramPeak {
            if (feature.Name.IsEmptyOrNull() || feature.Name.ToLower() == "unknown") {
                var id = "|ID=" + feature.ID;
                var rt = feature.ChromXs.RT.Value > 0 ? "|RT=" + Math.Round(feature.ChromXs.RT.Value, 3) : string.Empty;
                var ri = feature.ChromXs.RI.Value > 0 ? "|RI=" + Math.Round(feature.ChromXs.RI.Value, 3) : string.Empty;
                var dt = feature.ChromXs.Drift.Value > 0 ? "|DT=" + Math.Round(feature.ChromXs.Drift.Value, 3) : string.Empty;
                var mz = Math.Round(feature.Mass, 4).ToString();
                return "Unknown" + id + mz + rt + ri + dt;
            }
            else {
                return feature.Name;
            }
        }

        private static void WriteParameterInfoAsNist(StreamWriter sw, ParameterBase parameter)
        {
            if (!string.IsNullOrEmpty(parameter.Authors)) {
                sw.WriteLine("AUTHORS: " + parameter.Authors);
            }

            if (!string.IsNullOrEmpty(parameter.License)) {
                sw.WriteLine("LICENSE: " + parameter.License);
            }

            if (!string.IsNullOrEmpty(parameter.CollisionEnergy)) {
                sw.WriteLine("COLLISIONENERGY: " + parameter.CollisionEnergy);
            }

            if (!string.IsNullOrEmpty(parameter.InstrumentType)) {
                sw.WriteLine("INSTRUMENTTYPE: " + parameter.InstrumentType);
            }

            if (!string.IsNullOrEmpty(parameter.Instrument)) {
                sw.WriteLine("INSTRUMENT: " + parameter.Instrument);
            }

            if (!string.IsNullOrEmpty(parameter.Comment)) {
                sw.WriteLine("PARAMETERCOMMENT: " + parameter.Comment);
            }
        }

        private static void WriteSpectrumPeakInfo(StreamWriter sw, IEnumerable<ISpectrumPeak> massSpectra)
        {
            if (massSpectra is null) {
                return;
            }
            var peaks = massSpectra.Where(spec => spec.Intensity > 0).ToList();
            sw.WriteLine("Num Peaks: " + peaks.Count);
            foreach (var peak in peaks)
            {
                sw.WriteLine(Math.Round(peak.Mass, 5) + "\t" + Math.Round(peak.Intensity, 0));
            }
        }

        private static void WriteSpectrumPeakInfo(StreamWriter sw, IEnumerable<IsotopicPeak> isotopes) {
            if (!isotopes.IsEmptyOrNull()) {
                var peaks = isotopes.Where(spec => spec.AbsoluteAbundance > 0).ToList();
                sw.WriteLine("Num Peaks: " + peaks.Count);
                foreach (var peak in peaks) {
                    sw.WriteLine(Math.Round(peak.Mass, 5) + "\t" + Math.Round(peak.AbsoluteAbundance, 0));
                }
            }
        }
    }
}
