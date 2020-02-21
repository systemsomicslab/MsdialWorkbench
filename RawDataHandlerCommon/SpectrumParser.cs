using Riken.Metabolomics.RawData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Riken.Metabolomics.RawDataHandlerCommon.Parser {
    public sealed class SpectrumParser
    {
        private SpectrumParser() { }

        public static void setSpectrumProperties(RAW_Spectrum spectrum, 
            Dictionary<int, double[]> accumulatedMassBin) { // key: m/z * 100000, value [0] base peak m/z [1] summed intensity [2] base peak intensity

            //spectrum.DefaultArrayLength = accumulatedMassIntensityArray.Count(n => n > 1);

            var basepeakIntensity = 0.0;
            var basepeakMz = 0.0;
            var totalIonCurrnt = 0.0;
            var lowestMz = double.MaxValue;
            var highestMz = double.MinValue;
            var minIntensity = double.MaxValue;

            var spectra = new List<RAW_PeakElement>();

            //spectrum.Spectrum = new RAW_PeakElement[spectrum.DefaultArrayLength];

            foreach (var pair in accumulatedMassBin) {
                var pMzKey = pair.Key * 0.001;
                var pBasepeakMz = pair.Value[0];
                var pSummedIntensity = pair.Value[1];
                var pBasepeakIntensity = pair.Value[2];

                totalIonCurrnt += pSummedIntensity;

                if (pSummedIntensity > basepeakIntensity) {
                    basepeakIntensity = pSummedIntensity;
                    basepeakMz = pBasepeakMz;
                }
                if (lowestMz > pBasepeakMz) lowestMz = pBasepeakMz;
                if (highestMz < pBasepeakMz) highestMz = pBasepeakMz;
                if (minIntensity > pSummedIntensity) minIntensity = pSummedIntensity;

                var spec = new RAW_PeakElement() {
                    Mz = Math.Round(pBasepeakMz, 5),
                    Intensity = Math.Round(pSummedIntensity, 0)
                };
                //Console.WriteLine("mz {0}, intensity {1}", spec.Mz, spec.Intensity);
                spectra.Add(spec);
            }
            spectra = spectra.OrderBy(n => n.Mz).ToList();

            spectrum.Spectrum = spectra.ToArray();
            spectrum.DefaultArrayLength = spectra.Count();
            spectrum.BasePeakIntensity = basepeakIntensity;
            spectrum.BasePeakMz = basepeakMz;
            spectrum.TotalIonCurrent = totalIonCurrnt;
            spectrum.LowestObservedMz = lowestMz;
            spectrum.HighestObservedMz = highestMz;
            spectrum.MinIntensity = minIntensity;
        }

        public static void setSpectrumProperties(RAW_Spectrum spectrum, double[] accumulatedMassIntensityArray) {

            //spectrum.DefaultArrayLength = accumulatedMassIntensityArray.Count(n => n > 1);

            var basepeakIntensity = 0.0;
            var basepeakMz = 0.0;
            var totalIonCurrnt = 0.0;
            var lowestMz = double.MaxValue;
            var highestMz = double.MinValue;
            var minIntensity = double.MaxValue;

            var spectra = new List<RAW_PeakElement>();

            //spectrum.Spectrum = new RAW_PeakElement[spectrum.DefaultArrayLength];
            for (int i = 0; i < accumulatedMassIntensityArray.Length; i++) {
                if (accumulatedMassIntensityArray[i] < 1) continue;
                var mass = (double)i * 0.00001;
                var intensity = accumulatedMassIntensityArray[i];

                totalIonCurrnt += intensity;

                if (intensity > basepeakIntensity) {
                    basepeakIntensity = intensity;
                    basepeakMz = mass;
                }
                if (lowestMz > mass) lowestMz = mass;
                if (highestMz < mass) highestMz = mass;
                if (minIntensity > intensity) minIntensity = intensity;

                var spec = new RAW_PeakElement() {
                    Mz = Math.Round(mass, 5),
                    Intensity = Math.Round(intensity, 0)
                };
                //Console.WriteLine("mz {0}, intensity {1}", spec.Mz, spec.Intensity);
                spectra.Add(spec);
            }

            spectrum.Spectrum = spectra.ToArray();
            spectrum.DefaultArrayLength = spectra.Count();
            spectrum.BasePeakIntensity = basepeakIntensity;
            spectrum.BasePeakMz = basepeakMz;
            spectrum.TotalIonCurrent = totalIonCurrnt;
            spectrum.LowestObservedMz = lowestMz;
            spectrum.HighestObservedMz = highestMz;
            spectrum.MinIntensity = minIntensity;
        }

        public static void setSpectrumProperties(RAW_Spectrum spectrum, double[] masses, double[] intensities, double peakCutOff, ref double[] accumulatedMassIntensityArray) {
            //spectrum.DefaultArrayLength = masses.Length;

            var basepeakIntensity = 0.0;
            var basepeakMz = 0.0;
            var totalIonCurrnt = 0.0;
            var lowestMz = double.MaxValue;
            var highestMz = double.MinValue;
            var minIntensity = double.MaxValue;

            //spectrum.Spectrum = new RAW_PeakElement[spectrum.DefaultArrayLength];
            var spectra = new List<RAW_PeakElement>();
            for (int i = 0; i < masses.Length; i++) {
                var mass = masses[i];
                var intensity = intensities[i];
                if (intensity < peakCutOff) continue;
                totalIonCurrnt += intensity;

                if (intensity > basepeakIntensity) {
                    basepeakIntensity = intensity;
                    basepeakMz = mass;
                }
                if (lowestMz > mass) lowestMz = mass;
                if (highestMz < mass) highestMz = mass;
                if (minIntensity > intensity) minIntensity = intensity;

                //spectrum.Spectrum[i].Mz = mass;
                //spectrum.Spectrum[i].Intensity = intensity;
                // accumulatedMassIntensityArray[(int)(mass * 1000)] += intensity;

                var spec = new RAW_PeakElement() {
                    Mz = Math.Round(mass, 5),
                    Intensity = Math.Round(intensity, 0)
                };
                //Console.WriteLine("mz {0}, intensity {1}", spec.Mz, spec.Intensity);
                spectra.Add(spec);

                if (spectrum.MsLevel == 1) {
                    accumulatedMassIntensityArray[(int)(mass * 100000)] += intensity;
                }
            }

            spectrum.Spectrum = spectra.OrderBy(n => n.Mz).ToArray();
            spectrum.DefaultArrayLength = spectra.Count;
            spectrum.BasePeakIntensity = basepeakIntensity;
            spectrum.BasePeakMz = basepeakMz;
            spectrum.TotalIonCurrent = totalIonCurrnt;
            spectrum.LowestObservedMz = lowestMz;
            spectrum.HighestObservedMz = highestMz;
            spectrum.MinIntensity = minIntensity;
        }

        public static void setSpectrumProperties(RAW_Spectrum spectrum, double[] masses, double[] intensities, double peakCutOff, 
            Dictionary<int, double[]> accumulatedMassBin) { // key: m/z * 100000, value [0] base peak m/z [1] summed intensity [2] base peak intensity
            //spectrum.DefaultArrayLength = masses.Length;

            var basepeakIntensity = 0.0;
            var basepeakMz = 0.0;
            var totalIonCurrnt = 0.0;
            var lowestMz = double.MaxValue;
            var highestMz = double.MinValue;
            var minIntensity = double.MaxValue;

            //spectrum.Spectrum = new RAW_PeakElement[spectrum.DefaultArrayLength];
            var spectra = new List<RAW_PeakElement>();
            for (int i = 0; i < masses.Length; i++) {
                var mass = masses[i];
                var intensity = intensities[i];
                if (intensity < peakCutOff) continue;
                totalIonCurrnt += intensity;

                if (intensity > basepeakIntensity) {
                    basepeakIntensity = intensity;
                    basepeakMz = mass;
                }
                if (lowestMz > mass) lowestMz = mass;
                if (highestMz < mass) highestMz = mass;
                if (minIntensity > intensity) minIntensity = intensity;

                //spectrum.Spectrum[i].Mz = mass;
                //spectrum.Spectrum[i].Intensity = intensity;
                // accumulatedMassIntensityArray[(int)(mass * 1000)] += intensity;

                var spec = new RAW_PeakElement() {
                    Mz = Math.Round(mass, 5),
                    Intensity = Math.Round(intensity, 0)
                };
                //Console.WriteLine("mz {0}, intensity {1}", spec.Mz, spec.Intensity);
                spectra.Add(spec);

                if (spectrum.MsLevel == 1) {
                    AddToMassBinDictionary(accumulatedMassBin, mass, intensity);
                }
            }

            spectrum.Spectrum = spectra.OrderBy(n => n.Mz).ToArray();
            spectrum.DefaultArrayLength = spectra.Count;
            spectrum.BasePeakIntensity = basepeakIntensity;
            spectrum.BasePeakMz = basepeakMz;
            spectrum.TotalIonCurrent = totalIonCurrnt;
            spectrum.LowestObservedMz = lowestMz;
            spectrum.HighestObservedMz = highestMz;
            spectrum.MinIntensity = minIntensity;
        }

        public static void AddToMassBinDictionary(Dictionary<int, double[]> accumulatedMassBin, double mass, double intensity) {
            var massBin = (int)(mass * 1000);
            if (!accumulatedMassBin.ContainsKey(massBin)) {
                accumulatedMassBin[massBin] = new double[3] { mass, intensity, intensity };
            }
            else {
                accumulatedMassBin[massBin][1] += intensity;
                if (accumulatedMassBin[massBin][2] < intensity) {
                    accumulatedMassBin[massBin][0] = mass;
                    accumulatedMassBin[massBin][2] = intensity;
                }
            }
        }

        public static void setSpectrumProperties(RAW_Spectrum spectrum, List<double> masses, List<double> intensities, double peakCutOff,
            ref double[] accumulatedMassIntensityArray) {
            //spectrum.DefaultArrayLength = masses.Count;

            var basepeakIntensity = 0.0;
            var basepeakMz = 0.0;
            var totalIonCurrnt = 0.0;
            var lowestMz = double.MaxValue;
            var highestMz = double.MinValue;
            var minIntensity = double.MaxValue;
            var spectra = new List<RAW_PeakElement>();
            //spectrum.Spectrum = new RAW_PeakElement[spectrum.DefaultArrayLength];
            for (int i = 0; i < masses.Count; i++) {
                var mass = masses[i];
                var intensity = intensities[i];
                if (intensity < peakCutOff) continue;
                totalIonCurrnt += intensity;

                if (intensity > basepeakIntensity) {
                    basepeakIntensity = intensity;
                    basepeakMz = mass;
                }
                if (lowestMz > mass) lowestMz = mass;
                if (highestMz < mass) highestMz = mass;
                if (minIntensity > intensity) minIntensity = intensity;

                //spectrum.Spectrum[i].Mz = mass;
                //spectrum.Spectrum[i].Intensity = intensity;
                // accumulatedMassIntensityArray[(int)(mass * 1000)] += intensity;
                var spec = new RAW_PeakElement() {
                    Mz = Math.Round(mass, 5),
                    Intensity = Math.Round(intensity, 0)
                };
                spectra.Add(spec);

                if (spectrum.MsLevel == 1) {
                    accumulatedMassIntensityArray[(int)(mass * 100000)] += intensity;
                }
            }

            spectrum.Spectrum = spectra.OrderBy(n => n.Mz).ToArray();
            spectrum.DefaultArrayLength = spectra.Count;
            spectrum.BasePeakIntensity = basepeakIntensity;
            spectrum.BasePeakMz = basepeakMz;
            spectrum.TotalIonCurrent = totalIonCurrnt;
            spectrum.LowestObservedMz = lowestMz;
            spectrum.HighestObservedMz = highestMz;
            spectrum.MinIntensity = minIntensity;
        }

        public static void setSpectrumProperties(RAW_Spectrum spectrum, List<double> masses, List<double> intensities, double peakCutOff,
          Dictionary<int, double[]> accumulatedMassBin) { // key: m/z * 100000, value [0] base peak m/z [1] summed intensity [2] base peak intensity
            //spectrum.DefaultArrayLength = masses.Count;

            var basepeakIntensity = 0.0;
            var basepeakMz = 0.0;
            var totalIonCurrnt = 0.0;
            var lowestMz = double.MaxValue;
            var highestMz = double.MinValue;
            var minIntensity = double.MaxValue;
            var spectra = new List<RAW_PeakElement>();
            //spectrum.Spectrum = new RAW_PeakElement[spectrum.DefaultArrayLength];
            for (int i = 0; i < masses.Count; i++) {
                var mass = masses[i];
                var intensity = intensities[i];
                if (intensity < peakCutOff) continue;
                totalIonCurrnt += intensity;

                if (intensity > basepeakIntensity) {
                    basepeakIntensity = intensity;
                    basepeakMz = mass;
                }
                if (lowestMz > mass) lowestMz = mass;
                if (highestMz < mass) highestMz = mass;
                if (minIntensity > intensity) minIntensity = intensity;

                //spectrum.Spectrum[i].Mz = mass;
                //spectrum.Spectrum[i].Intensity = intensity;
                // accumulatedMassIntensityArray[(int)(mass * 1000)] += intensity;
                var spec = new RAW_PeakElement() {
                    Mz = Math.Round(mass, 5),
                    Intensity = Math.Round(intensity, 0)
                };
                spectra.Add(spec);

                if (spectrum.MsLevel == 1) {
                    AddToMassBinDictionary(accumulatedMassBin, mass, intensity);
                }
            }

            spectrum.Spectrum = spectra.OrderBy(n => n.Mz).ToArray();
            spectrum.DefaultArrayLength = spectra.Count;
            spectrum.BasePeakIntensity = basepeakIntensity;
            spectrum.BasePeakMz = basepeakMz;
            spectrum.TotalIonCurrent = totalIonCurrnt;
            spectrum.LowestObservedMz = lowestMz;
            spectrum.HighestObservedMz = highestMz;
            spectrum.MinIntensity = minIntensity;
        }



        public static void setSpectrumProperties(RAW_Spectrum spectrum, float[] masses, float[] intensities, double peakCutOff,
            ref double[] accumulatedMassIntensityArray) {
            //spectrum.DefaultArrayLength = masses.Length;

            var basepeakIntensity = 0.0;
            var basepeakMz = 0.0;
            var totalIonCurrnt = 0.0;
            var lowestMz = double.MaxValue;
            var highestMz = double.MinValue;
            var minIntensity = double.MaxValue;

            //spectrum.Spectrum = new RAW_PeakElement[spectrum.DefaultArrayLength];
            var spectra = new List<RAW_PeakElement>();
            for (int i = 0; i < masses.Length; i++) {
                var mass = masses[i];
                var intensity = intensities[i];
                if (intensity < peakCutOff) continue;
                totalIonCurrnt += intensity;

                if (intensity > basepeakIntensity) {
                    basepeakIntensity = intensity;
                    basepeakMz = mass;
                }
                if (lowestMz > mass) lowestMz = mass;
                if (highestMz < mass) highestMz = mass;
                if (minIntensity > intensity) minIntensity = intensity;

                //spectrum.Spectrum[i].Mz = mass;
                //spectrum.Spectrum[i].Intensity = intensity;
                var spec = new RAW_PeakElement() {
                    Mz = Math.Round(mass, 5),
                    Intensity = Math.Round(intensity, 0)
                };
                spectra.Add(spec);

                if (spectrum.MsLevel == 1) {
                    accumulatedMassIntensityArray[(int)(mass * 100000)] += intensity;
                }
            }

            spectrum.Spectrum = spectra.OrderBy(n => n.Mz).ToArray();
            spectrum.DefaultArrayLength = spectra.Count;
            spectrum.BasePeakIntensity = basepeakIntensity;
            spectrum.BasePeakMz = basepeakMz;
            spectrum.TotalIonCurrent = totalIonCurrnt;
            spectrum.LowestObservedMz = lowestMz;
            spectrum.HighestObservedMz = highestMz;
            spectrum.MinIntensity = minIntensity;
        }

        public static void setSpectrumProperties(RAW_Spectrum spectrum, float[] masses, float[] intensities, double peakCutOff,
            Dictionary<int, double[]> accumulatedMassBin) { // key: m/z * 100000, value [0] base peak m/z [1] summed intensity [2] base peak intensity
            //spectrum.DefaultArrayLength = masses.Length;

            var basepeakIntensity = 0.0;
            var basepeakMz = 0.0;
            var totalIonCurrnt = 0.0;
            var lowestMz = double.MaxValue;
            var highestMz = double.MinValue;
            var minIntensity = double.MaxValue;

            //spectrum.Spectrum = new RAW_PeakElement[spectrum.DefaultArrayLength];
            var spectra = new List<RAW_PeakElement>();
            for (int i = 0; i < masses.Length; i++) {
                var mass = masses[i];
                var intensity = intensities[i];
                if (intensity < peakCutOff) continue;
                totalIonCurrnt += intensity;

                if (intensity > basepeakIntensity) {
                    basepeakIntensity = intensity;
                    basepeakMz = mass;
                }
                if (lowestMz > mass) lowestMz = mass;
                if (highestMz < mass) highestMz = mass;
                if (minIntensity > intensity) minIntensity = intensity;

                //spectrum.Spectrum[i].Mz = mass;
                //spectrum.Spectrum[i].Intensity = intensity;
                var spec = new RAW_PeakElement() {
                    Mz = Math.Round(mass, 5),
                    Intensity = Math.Round(intensity, 0)
                };
                spectra.Add(spec);

                if (spectrum.MsLevel == 1) {
                    AddToMassBinDictionary(accumulatedMassBin, mass, intensity);
                }
            }

            spectrum.Spectrum = spectra.OrderBy(n => n.Mz).ToArray();
            spectrum.DefaultArrayLength = spectra.Count;
            spectrum.BasePeakIntensity = basepeakIntensity;
            spectrum.BasePeakMz = basepeakMz;
            spectrum.TotalIonCurrent = totalIonCurrnt;
            spectrum.LowestObservedMz = lowestMz;
            spectrum.HighestObservedMz = highestMz;
            spectrum.MinIntensity = minIntensity;
        }

        public static void setSpectrumProperties(RAW_Spectrum spectrum, float[] masses, float[] intensities, double peakCutOff) {
           // spectrum.DefaultArrayLength = masses.Length;

            var basepeakIntensity = 0.0;
            var basepeakMz = 0.0;
            var totalIonCurrnt = 0.0;
            var lowestMz = double.MaxValue;
            var highestMz = double.MinValue;
            var minIntensity = double.MaxValue;
            var spectra = new List<RAW_PeakElement>();
            //spectrum.Spectrum = new RAW_PeakElement[spectrum.DefaultArrayLength];
            for (int i = 0; i < masses.Length; i++) {
                var mass = masses[i];
                var intensity = intensities[i];
                if (intensity < peakCutOff) continue;
                totalIonCurrnt += intensity;

                if (intensity > basepeakIntensity) {
                    basepeakIntensity = intensity;
                    basepeakMz = mass;
                }
                if (lowestMz > mass) lowestMz = mass;
                if (highestMz < mass) highestMz = mass;
                if (minIntensity > intensity) minIntensity = intensity;

                //spectrum.Spectrum[i].Mz = mass;
                //spectrum.Spectrum[i].Intensity = intensity;
                var spec = new RAW_PeakElement() {
                    Mz = Math.Round(mass, 5),
                    Intensity = Math.Round(intensity, 0)
                };
                spectra.Add(spec);
            }

            spectrum.Spectrum = spectra.OrderBy(n => n.Mz).ToArray();
            spectrum.DefaultArrayLength = spectra.Count;
            spectrum.BasePeakIntensity = basepeakIntensity;
            spectrum.BasePeakMz = basepeakMz;
            spectrum.TotalIonCurrent = totalIonCurrnt;
            spectrum.LowestObservedMz = lowestMz;
            spectrum.HighestObservedMz = highestMz;
            spectrum.MinIntensity = minIntensity;
        }

        public static void setSpectrumProperties(RAW_Spectrum spectrum, List<double> masses, List<double> intensities, double peakCutOff) {
            //spectrum.DefaultArrayLength = masses.Count;

            var basepeakIntensity = 0.0;
            var basepeakMz = 0.0;
            var totalIonCurrnt = 0.0;
            var lowestMz = double.MaxValue;
            var highestMz = double.MinValue;
            var minIntensity = double.MaxValue;
            var spectra = new List<RAW_PeakElement>();
            //spectrum.Spectrum = new RAW_PeakElement[spectrum.DefaultArrayLength];
            for (int i = 0; i < masses.Count; i++) {
                var mass = masses[i];
                var intensity = intensities[i];
                if (intensity < peakCutOff) continue;
                totalIonCurrnt += intensity;

                if (intensity > basepeakIntensity) {
                    basepeakIntensity = intensity;
                    basepeakMz = mass;
                }
                if (lowestMz > mass) lowestMz = mass;
                if (highestMz < mass) highestMz = mass;
                if (minIntensity > intensity) minIntensity = intensity;

                //spectrum.Spectrum[i].Mz = mass;
                //spectrum.Spectrum[i].Intensity = intensity;
                var spec = new RAW_PeakElement() {
                    Mz = Math.Round(mass, 5),
                    Intensity = Math.Round(intensity, 0)
                };
                spectra.Add(spec);
            }

            spectrum.Spectrum = spectra.OrderBy(n => n.Mz).ToArray();
            spectrum.DefaultArrayLength = spectra.Count;
            spectrum.BasePeakIntensity = basepeakIntensity;
            spectrum.BasePeakMz = basepeakMz;
            spectrum.TotalIonCurrent = totalIonCurrnt;
            spectrum.LowestObservedMz = lowestMz;
            spectrum.HighestObservedMz = highestMz;
            spectrum.MinIntensity = minIntensity;
        }
    }
}
