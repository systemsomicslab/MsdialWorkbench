using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Rfx.Riken.OsakaUniv;
using Msdial.Lcms.Dataprocess.Algorithm;
using Msdial.Lcms.Dataprocess.Utility;
using CompMs.Common.DataObj;

namespace Msdial.Common.Export
{
    public static class ExportMassSpectrum
    {
        public static void WriteSpectrumFromAlignment(StreamWriter sw, ProjectPropertyBean projectProp, List<MspFormatCompoundInformationBean> mspDB, MS2DecResult ms2DecResult, AlignmentPropertyBean alignmentProperty, ObservableCollection<AnalysisFileBean> files, bool isMsp = false)
        {
            var name = alignmentProperty.MetaboliteName;
            if (name == string.Empty) name = "Unknown";
            else if (!isMsp && name.Contains("w/o")) name = "Unknown";
            var adduct = alignmentProperty.AdductIonName;
            if (string.IsNullOrEmpty(adduct)) {
                if (projectProp.IonMode == IonMode.Positive) adduct = "[M+H]+";
                else if (projectProp.IonMode == IonMode.Negative) adduct = "[M-H]-";
            }
            sw.WriteLine("NAME: " + name);
            sw.WriteLine("SCANNUMBER: " + alignmentProperty.AlignmentID);
            sw.WriteLine("RETENTIONTIME: " + ms2DecResult.PeakTopRetentionTime);
            sw.WriteLine("PRECURSORMZ: " + ms2DecResult.Ms1AccurateMass);
            sw.WriteLine("PRECURSORTYPE: " + adduct);
            sw.WriteLine("IONMODE: " + projectProp.IonMode);
            sw.WriteLine("INTENSITY: " + ms2DecResult.Ms1PeakHeight);
            sw.WriteLine("INCHIKEY: " + MspDataRetrieve.GetInChIKey(alignmentProperty.LibraryID, mspDB));
            sw.WriteLine("SMILES: " + MspDataRetrieve.GetSMILES(alignmentProperty.LibraryID, mspDB));
            sw.WriteLine("FORMULA: " + MspDataRetrieve.GetFormula(alignmentProperty.LibraryID, mspDB));
            WriteProjectProperty(sw, projectProp);

            var com = "";
            if ((alignmentProperty.Comment != null && alignmentProperty.Comment != string.Empty))
                com = alignmentProperty.Comment + "; ";
            else
                com = "";
            if (projectProp.Comment != null && projectProp.Comment != string.Empty)
                com = com + System.IO.Path.GetFileNameWithoutExtension(files[alignmentProperty.RepresentativeFileID].AnalysisFilePropertyBean.AnalysisFilePath) + "; " + projectProp.Comment;
            else
                com = com + System.IO.Path.GetFileNameWithoutExtension(files[alignmentProperty.RepresentativeFileID].AnalysisFilePropertyBean.AnalysisFilePath);

            if (!isMsp)
            {
                sw.WriteLine("COMMENT: " + com);
                sw.Write("MSTYPE: ");
                sw.WriteLine("MS1");

                sw.WriteLine("Num Peaks: 3");
                sw.WriteLine(Math.Round(ms2DecResult.Ms1AccurateMass, 5) + "\t" + ms2DecResult.Ms1PeakHeight);
                sw.WriteLine(Math.Round(ms2DecResult.Ms1AccurateMass + 1.00335, 5) + "\t" + ms2DecResult.Ms1IsotopicIonM1PeakHeight);
                sw.WriteLine(Math.Round(ms2DecResult.Ms1AccurateMass + 2.00671, 5) + "\t" + ms2DecResult.Ms1IsotopicIonM2PeakHeight);

                sw.Write("MSTYPE: ");
                sw.WriteLine("MS2");
            }
            sw.Write("Num Peaks: ");
            var peaks = ms2DecResult.MassSpectra.Where(x => x[1] >= 0.5).ToList();
            sw.WriteLine(peaks.Count);

            for (int i = 0; i < peaks.Count; i++)
                sw.WriteLine(Math.Round(peaks[i][0], 5) + "\t" + Math.Round(peaks[i][1], 0));
        }

        public static void WriteSpectrumFromPeakSpot(StreamWriter sw, ProjectPropertyBean projectProp, List<MspFormatCompoundInformationBean> mspDB, AnalysisParametersBean param,
            MS2DecResult ms2DecResult, PeakAreaBean pab, ObservableCollection<RawSpectrum> lcmsSpectra, AnalysisFileBean file, bool isMsp = false)
        {
            var name = pab.MetaboliteName;
            if (name == string.Empty) name = "Unknown";
            else if (!isMsp && name.Contains("w/o")) name = "Unknown";
            var adduct = pab.AdductIonName;
            if ((adduct == null || adduct == string.Empty) && projectProp.IonMode == IonMode.Positive) adduct = "[M+H]+";
            else if ((adduct == null || adduct == string.Empty) && projectProp.IonMode == IonMode.Negative) adduct = "[M-H]-";

            sw.WriteLine("NAME: " + name);
            sw.WriteLine("SCANNUMBER: " + pab.ScanNumberAtPeakTop);
            sw.WriteLine("PEAKID: " + pab.PeakID);
            sw.WriteLine("RETENTIONTIME: " + ms2DecResult.PeakTopRetentionTime);
            sw.WriteLine("PRECURSORMZ: " + ms2DecResult.Ms1AccurateMass);
            sw.WriteLine("PRECURSORTYPE: " + adduct);
            sw.WriteLine("IONMODE: " + projectProp.IonMode);
            sw.WriteLine("INTENSITY: " + ms2DecResult.Ms1PeakHeight);
            sw.WriteLine("INCHIKEY: " + MspDataRetrieve.GetInChIKey(pab.LibraryID, mspDB));
            sw.WriteLine("SMILES: " + MspDataRetrieve.GetSMILES(pab.LibraryID, mspDB));
            sw.WriteLine("FORMULA: " + MspDataRetrieve.GetFormula(pab.LibraryID, mspDB));

            WriteProjectProperty(sw, projectProp);

            var com = "";
            if ((pab.Comment != null && pab.Comment != string.Empty))
                com = pab.Comment + "; ";
            else
                com = "";
            if (projectProp.Comment != null && projectProp.Comment != string.Empty)
                com = com + System.IO.Path.GetFileNameWithoutExtension(file.AnalysisFilePropertyBean.AnalysisFilePath) + "; " + projectProp.Comment;
            else
                com = com + System.IO.Path.GetFileNameWithoutExtension(file.AnalysisFilePropertyBean.AnalysisFilePath);

            sw.WriteLine("COMMENT: " + com);

            if (!isMsp)
            {
                sw.Write("MSTYPE: ");
                sw.WriteLine("MS1");

                //var ms1spec = lcmsSpectra[pab.Ms1LevelDatapointNumber].Spectrum;
                //if (projectProp.DataType == DataType.Profile) ms1spec = SpectralCentroiding.Centroid(ms1spec);
                var ms1spec = DataAccessLcUtility.GetCentroidMassSpectra(lcmsSpectra, projectProp.DataType, pab.Ms1LevelDatapointNumber, param.CentroidMs2Tolerance, param.PeakDetectionBasedCentroid);
                sw.WriteLine("Num Peaks: " + ms1spec.Count());
                foreach(var spec in ms1spec) {
                    sw.WriteLine(spec[0] + "\t" + spec[1]);
                }

                sw.Write("MSTYPE: ");
                sw.WriteLine("MS2");
            }
            sw.Write("Num Peaks: ");
            var peaks = ms2DecResult.MassSpectra.Where(x => x[1] >= 0.5).ToList();
            sw.WriteLine(peaks.Count);

            for (int i = 0; i < peaks.Count; i++)
                sw.WriteLine(Math.Round(peaks[i][0], 5) + "\t" + Math.Round(peaks[i][1], 0));
        }


        private static void WriteProjectProperty(StreamWriter sw, ProjectPropertyBean projectProp)
        {
            sw.WriteLine("SPECTRUMTYPE: " + projectProp.DataTypeMS2);
            if (projectProp.CollisionEnergy != null && projectProp.CollisionEnergy != string.Empty)
            {
                sw.WriteLine("COLLISIONENERGY: " + projectProp.CollisionEnergy);
            }
            if (projectProp.InstrumentType != null && projectProp.InstrumentType != string.Empty)
            {
                sw.WriteLine("INSTRUMENTTYPE: " + projectProp.InstrumentType);
            }
            if (projectProp.Instrument != null && projectProp.Instrument != string.Empty)
            {
                sw.WriteLine("INSTRUMENT: " + projectProp.Instrument);
            }
            if (projectProp.FinalSavedDate != null)
            {
                sw.WriteLine("DATE: " + projectProp.FinalSavedDate.Date);
            }

            if (projectProp.Authors != null && projectProp.Authors != string.Empty)
            {
                sw.WriteLine("AUTHORS: " + projectProp.Authors);
            }

            if (projectProp.License != null && projectProp.License != string.Empty)
            {
                sw.WriteLine("LICENSE: " + projectProp.License);
            }
        }

    }
}
