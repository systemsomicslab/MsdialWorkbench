using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using CompMs.Common.Spectrum;
using CompMs.Common.Interfaces;
using CompMs.Common.Enum;

namespace CompMs.Common.Components.Tests
{
    public class ComponentsDemo
    {
        /*
         * How to use Components
         * 
         * 1. Times : Retentiont time, retention index, and drift time
         * 2. SpectrumPeak: m/z and intensity
         * 3. ChromatogramPeak: SpectrumPeak + time + id
         * 4. MoleculeProperty: property for molecules (e.g. text library, meta data of MSP record)
         * 5. MSScanProperty: property for MS scan (e.g. full MSP record, scan from raw data), MspRecord is an extension
         *
         * Example 1. Set demo data.
         * 
         * Comparing spectrum to get similarity values (example method: GetTotalPeaks)
         * 
         * Example 2. 
         * 
         * 
         * 
         */

        #region Demo data
        public List<MoleculeProperty> TextBasedLibraries { get; set; }
        public List<MspRecord> MspRecords { get; set; }
        public MSScanProperty DemoMsSpectrum { get; set; }
        public List<ChromatogramPeak> DemoChromatogram { get; set; }
        #endregion

        public ComponentsDemo() { }

        public void Demo()
        {
            SetDemoScan();
            SetDemoMspRecord();
            SetDemoChromatogram();
            DebugPrintDemoData();

            // calc property similarity (m/z and time)
            var score = -1;
            foreach (var msp in MspRecords)
            {
                var val = GetPropertySimilarity(DemoMsSpectrum, msp);
                if (score < val) score = val;
            }
            Debug.Print("Test property similarity score: " + score);

            // calc spectrum similarity 
            score = -1;
            foreach (var msp in MspRecords)
            {
                // number of total peaks is calculated instead of similarity score...
                var val = GetTotalPeaks(DemoMsSpectrum.Spectrum, msp.Spectrum);
                if (score < val) score = val;
            }
            Debug.Print("Test ms similarity score: " + score);
        }

        #region Set values
        public void SetDemoScan()
        {
            DemoMsSpectrum = new MSScanProperty(0, 100, new RetentionTime(10), IonMode.Positive);
            DemoMsSpectrum.PrecursorMz = 100;
            DemoMsSpectrum.Spectrum = new List<SpectrumPeak>();
            DemoMsSpectrum.AddPeak(100, 10);
            DemoMsSpectrum.AddPeak(130, 30);
            DemoMsSpectrum.AddPeak(150, 100);
        }

        public void SetDemoMspRecord()
        {
            MspRecords = new List<MspRecord>();
            for (var i = 1; i < 10; i++)
            {
                var mspRecord = new MspRecord();
                mspRecord.ScanID = i;
                mspRecord.PrecursorMz = 10 * i;
                // mspRecord.Times = new Times(new RetentionTime(10));
                mspRecord.ChromXs = new ChromXs() { RT = new RetentionTime(i * 0.1), RI = new RetentionIndex(i * 0.1 / 5), Drift = new DriftTime(3), MainType = ChromXType.RT };
                mspRecord.AddPeak(10 * i, 5);
                mspRecord.AddPeak(10 * i, 15);
                mspRecord.AddPeak(10 * i, 25);
                mspRecord.AddPeak(10 * i, 35);
                mspRecord.AddPeak(10 * i, 100);
                MspRecords.Add(mspRecord);
            }
        }

        public void SetDemoChromatogram()
        {
            DemoChromatogram = new List<ChromatogramPeak>();
            for (var i = 0; i < 1000; i++)
            {
                var chrom = new ChromatogramPeak(i, 200, i * 0.1, new RetentionTime(i * 0.05));
                DemoChromatogram.Add(chrom);
            }
        }

        #endregion

        #region Example methods
        // get dot product or something lie that.
        public int GetTotalPeaks(IReadOnlyList<ISpectrumPeak> spectrum1, IReadOnlyList<ISpectrumPeak> spectrum2)
        {
            int score = -1;
            score = spectrum1.Count + spectrum2.Count;
            return score;
        }

        // Comparing all time property and m/z value
        public int GetPropertySimilarity(IMSScanProperty prop1, IMSScanProperty prop2)
        {
            int score = -1;
            if (Math.Abs(prop1.PrecursorMz - prop2.PrecursorMz) < 1) score += 1;
            if (prop1.ChromXs.HasAbsolute() && prop2.ChromXs.HasAbsolute())
                if (Math.Abs(prop1.ChromXs.RT.Value - prop2.ChromXs.RT.Value) < 1)
                    score += 1;
            if (prop1.ChromXs.HasRelative() && prop2.ChromXs.HasRelative())
                if (Math.Abs(prop1.ChromXs.RI.Value - prop2.ChromXs.RI.Value) < 1)
                    score += 1;
            if (prop1.ChromXs.HasDrift() && prop2.ChromXs.HasDrift())
            {
                if (Math.Abs(prop1.ChromXs.Drift.Value - prop2.ChromXs.Drift.Value) < 1)
                {
                    score += 1;
                }
            }
            return score;
        }

        public void DebugPrintDemoData()
        {
            var counter = 0;
            // spectrum
            if (DemoMsSpectrum != null)
            {
                Debug.Print("Show MS spectrum");
                Debug.Print($"ID: {DemoMsSpectrum.ScanID}, mz: ${DemoMsSpectrum.PrecursorMz:F3}, {DemoMsSpectrum.ChromXs.GetRepresentativeXAxis().ToString()}");
                Debug.Print($"Num Peaks: {DemoMsSpectrum.Spectrum.Count}");
                foreach (var peak in DemoMsSpectrum.Spectrum)
                {
                    Debug.Print($"{peak.Mass:F2}\t{peak.Intensity:F1}");
                    if (counter++ > 10) return;
                }
            }
            Debug.Print("");
            // chromatogram
            if (DemoChromatogram != null)
            {
                Debug.Print("Show chromatogram");
                Debug.Print($"Num Peaks: {DemoChromatogram.Count}");
                foreach (var peak in DemoChromatogram)
                {
                    Debug.Print($"ID: {peak.ID}\tmz: {peak.Mass:F2}\tint: {peak.Intensity:F1}\t{peak.ChromXs.GetRepresentativeXAxis().ToString()}");
                    if (counter++ > 10) return;
                }
            }

        }
        #endregion
    }

    [TestClass()]
    public class ComponentsDemoTests
    {
        [TestMethod()]
        public void DemoTest()
        {
            var demoClass = new ComponentsDemo();
            demoClass.Demo();
        }
    }
}