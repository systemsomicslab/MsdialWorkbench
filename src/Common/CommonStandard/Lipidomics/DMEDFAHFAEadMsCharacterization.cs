using CompMs.Common.Components;
using CompMs.Common.Extension;
using CompMs.Common.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.Common.Lipidomics
{
    internal class DMEDFAHFAEadMsCharacterization
    {
        public static (ILipid, double[]) Characterize(
            IMSScanProperty scan, ILipid molecule, MoleculeMsReference reference,
            float tolerance, float mzBegin, float mzEnd)
        {
            var snPositionMzValues = reference.Spectrum.Where(n => n.SpectrumComment == SpectrumComment.snposition).ToList();
            List<DiagnosticIon> dions4position = null;
            if (!snPositionMzValues.IsEmptyOrNull()) {
                dions4position = new List<DiagnosticIon>() {
                        new DiagnosticIon() { MzTolerance = 0.05, IonAbundanceCutOff = 10, Mz = snPositionMzValues[0].Mass }
                    };
            }
            else {
                Console.WriteLine();                    
            }

            var defaultResult = EieioMsCharacterizationUtility.GetDefaultScore(
                    scan, reference, tolerance, mzBegin, mzEnd, 1, 1, 1, 0.5, 
                    dIons4position: dions4position);
            return StandardMsCharacterizationUtility.GetDefaultCharacterizationResultForGlycerophospholipid(molecule, defaultResult);
        }
    }
}
