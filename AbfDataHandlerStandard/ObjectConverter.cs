using Reifycs.RDAM;
using Rfx.Riken.OsakaUniv;
using Riken.Metabolomics.RawData;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Riken.Metabolomics.AbfDataHandler
{
    public class ObjectConverter
    {
        public RAW_Measurement ReadAbf(string abfFilePath, int id)
        {
            //tentativelly
            var rawMes = new RAW_Measurement() {
                SourceFileInfo = new RAW_SourceFileInfo() {
                    Id = id.ToString(),
                    Name = System.IO.Path.GetFileNameWithoutExtension(abfFilePath),
                    Location = abfFilePath
                },
                Sample = new RAW_Sample() {
                    Id = id.ToString(),
                    Name = System.IO.Path.GetFileNameWithoutExtension(abfFilePath)
                }
            };

            using (var rdam = new RDAMfileDataAccess(abfFilePath)) {
                var mes = rdam.Measurements[id];
                var functionMs = (RDAM_FunctionElem)mes.Functions[0];
                var spectrumList = functionMs.RetrieveAllScans_Raw();
                var scanNum = 0;
                foreach (var spectrum in spectrumList) {

                    var rawSpec = new RAW_Spectrum() {
                        BasePeakMz = spectrum.BasePeakMz,
                        BasePeakIntensity = spectrum.BasePeakInt,
                        MinIntensity = spectrum.MinInt, 
                        MsLevel = (int)spectrum.MsLevel,
                        HighestObservedMz = spectrum.MzHigh,
                        LowestObservedMz = spectrum.MzLow,
                        ScanStartTime = spectrum.RTmin,
                        ScanStartTimeUnit = Units.Minute,
                        ScanPolarity = spectrum.Polarity == '+' ? ScanPolarity.Positive : ScanPolarity.Negative,
                        ScanNumber = scanNum,
                        TotalIonCurrent = spectrum.TIC
                    };
                    if (spectrum.PrecursorMz != null) {
                        rawSpec.Precursor = new RAW_PrecursorIon() { SelectedIonMz = (double)spectrum.PrecursorMz };
                    }

                    if (spectrum.Spect.Length > 0) {
                        rawSpec.Spectrum = new RAW_PeakElement[spectrum.Spect.Length];
                        for (int i = 0; i < spectrum.Spect.Length; i++) {
                            rawSpec.Spectrum[i].Mz = spectrum.Spect[i].Mz;
                            rawSpec.Spectrum[i].Intensity = spectrum.Spect[i].SigInt;
                        }
                    }
                    rawMes.SpectrumList.Add(rawSpec);
                    scanNum++;
                }
            }
            return rawMes;
        }

        private void progressReports(int current, int total, System.ComponentModel.BackgroundWorker bgWorker)
        {
            var progress = (double)current / (double)total * 100.0;
            bgWorker.ReportProgress((int)progress);
        }

    }
}
