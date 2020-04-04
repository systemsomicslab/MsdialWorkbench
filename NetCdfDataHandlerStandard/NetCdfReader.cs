using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
//using Riken.Metabolomics.NetCDF4;
using Riken.Metabolomics.RawData;
using Riken.Metabolomics.NcData;

namespace Riken.Metabolomics.Netcdf
{

    public class NC_Measurement : RAW_Measurement
    {

    }
    public class NetCdfReader
    {
        RAW_Measurement measurement;
        //BackgroundWorker bgWorker;

        public RAW_Measurement ReadNetCdf(string filepath, int id)
        {
            var errorMessage = string.Empty;
            var ncObject = new NcObject(filepath, true);

            this.measurement = new RAW_Measurement() {
                SourceFileInfo = new RAW_SourceFileInfo() {
                    Id = id.ToString(),
                    Name = System.IO.Path.GetFileNameWithoutExtension(filepath),
                    Location = filepath
                },
                Sample = new RAW_Sample() {
                    Id = id.ToString(),
                    Name = System.IO.Path.GetFileNameWithoutExtension(filepath)
                }//,
                //SpectrumList = specList,

            };
            var ncMes = ncObject.Measurement;
            // mes
            var ncPol = ncMes.MeasurementPolarity;
            var pol = ncPol == Polarity.positive ? ScanPolarity.Positive
                             : (ncPol == Polarity.negative ? ScanPolarity.Negative : ScanPolarity.Undefined);
            var specrep = ncMes.ProcessMethod == ProcessMethod.centroid ? SpectrumRepresentation.Centroid
                : (ncMes.ProcessMethod == ProcessMethod.profile ? SpectrumRepresentation.Profile : SpectrumRepresentation.Undefined);
            var ncQAList = ncMes.QualAnalysisList;
            List<RAW_Spectrum> specList;
            foreach (var ncQA in ncQAList) {
                //Console.WriteLine("QAT:{0} #P:{1} #S:{2}", ncQA.Type, ncQA.NumPoint, ncQA.NumScan);
                var ncSpecList = ncQA.SpectrumList;

                specList = new List<RAW_Spectrum>(ncSpecList.Count);
                foreach (var ncSpec in ncSpecList) {
                    var ncPeakList = ncSpec.PeakList;
                    RAW_PeakElement[] peList = new RAW_PeakElement[ncPeakList.Count];
                    for (var i = 0; i < ncPeakList.Count; i++) {
                        var ncPeak = ncPeakList[i];
                        //Console.WriteLine("RT:{0} m/z:{1} int:{2}",ncSpec.RetentionTime,ncPeak.Mz,ncPeak.Intensity);
                        peList[i] = new RAW_PeakElement() {
                            Mz = ncPeak.Mz,
                            Intensity = ncPeak.Intensity
                        };
                    }
                    double int_max = 0;
                    int i_base = -1;

                    for (var i = 0; i < peList.Length; i++) {
                        if (peList[i].Intensity > int_max) {
                            int_max = peList[i].Intensity;
                            i_base = i;
                        }
                    }
                    double mz_base = i_base == -1 ? -1 : peList[i_base].Mz;
                    double int_base = i_base == -1 ? -1 : peList[i_base].Intensity;
                    /*
                    Console.WriteLine("NC SCAN Id:{0} SN:{1} Pol:{2} TIC:{3} #Peak:{4} m/z:{5}-{6}"
                        , ncSpec.Id
                        , ncSpec.ScanNumber
                        , ncMes.MeasurementPolarity
                        , ncSpec.TotalIntensity
                        , ncPeakList.Count
                        , ncSpec.LowestPeakMass
                        , ncSpec.HighestPeakMass
                       
                        );
                    */
                    RAW_Spectrum spec = new RAW_Spectrum() {
                        Id = ncSpec.Id.ToString()
                                                               ,
                        ScanNumber = ncSpec.ScanNumber
                                                               ,
                        ScanStartTime = ncSpec.RetentionTime
                                                               ,
                        ScanStartTimeUnit = Units.Minute
                                                               ,
                        ScanPolarity = pol
                                                               ,
                        TotalIonCurrent = ncSpec.TotalIntensity
                                                               ,
                        Spectrum = peList
                                                               ,
                        BasePeakMz = mz_base
                                                               ,
                        BasePeakIntensity = int_base
                                                               ,
                        LowestObservedMz = ncSpec.LowestPeakMass
                                                               ,
                        HighestObservedMz = ncSpec.HighestPeakMass
                                                               ,
                        DefaultArrayLength = ncSpec.PointCount
                                                               ,
                        MsLevel = 1
                                                               ,
                        SpectrumRepresentation = specrep
                    };
                    specList.Add(spec);

                }
                this.measurement.SpectrumList = specList;
            }
            return this.measurement;

        }
    }

}
