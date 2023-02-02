using System;
using System.Collections.Generic;
using System.Text;
using CompMs.Common.Components;
using CompMs.Common.Interfaces;

namespace CompMs.Common.Lipidomics {
    public sealed class EidDefaultCharacterization {
        private EidDefaultCharacterization() { }

        public static (ILipid, double[]) Characterize4AlkylAcylGlycerols(
            IMSScanProperty scan, ILipid molecule, MoleculeMsReference reference,
            float tolerance, float mzBegin, float mzEnd) {

            var defaultResult = EieioMsCharacterizationUtility.GetDefaultScore(
                    scan, reference, tolerance, mzBegin, mzEnd, 1, 0, 1, 0.5);
            return StandardMsCharacterizationUtility.GetDefaultCharacterizationResultForAlkylAcylGlycerols(molecule, defaultResult);
        }

        public static (ILipid, double[]) Characterize4Ceramides(
            IMSScanProperty scan, ILipid molecule, MoleculeMsReference reference,
            float tolerance, float mzBegin, float mzEnd) {

            var defaultResult = EieioMsCharacterizationUtility.GetDefaultScore(
                    scan, reference, tolerance, mzBegin, mzEnd, 1, 0, 0, 0.5);
            return StandardMsCharacterizationUtility.GetDefaultCharacterizationResultForCeramides(molecule, defaultResult);
        }

        public static (ILipid, double[]) Characterize4DiacylGlycerols(
            IMSScanProperty scan, ILipid molecule, MoleculeMsReference reference,
            float tolerance, float mzBegin, float mzEnd) {

            var defaultResult = EieioMsCharacterizationUtility.GetDefaultScore(
                    scan, reference, tolerance, mzBegin, mzEnd, 1, 0, 1, 0.5);
            return StandardMsCharacterizationUtility.GetDefaultCharacterizationResultForGlycerophospholipid(molecule, defaultResult);
        }

        public static (ILipid, double[]) Characterize4MonoacylGlycerols(
            IMSScanProperty scan, ILipid molecule, MoleculeMsReference reference,
            float tolerance, float mzBegin, float mzEnd) {

            var defaultResult = EieioMsCharacterizationUtility.GetDefaultScore(
                    scan, reference, tolerance, mzBegin, mzEnd, 1, 0, 0, 0.5);
            return StandardMsCharacterizationUtility.GetDefaultCharacterizationResultForSingleAcylChainLipid(molecule, defaultResult);
        }

        public static (ILipid, double[]) Characterize4SingleAcylChain(
            IMSScanProperty scan, ILipid molecule, MoleculeMsReference reference,
            float tolerance, float mzBegin, float mzEnd)
        {

            var defaultResult = EieioMsCharacterizationUtility.GetDefaultScore(
                    scan, reference, tolerance, mzBegin, mzEnd, 1, 0, 0, 0.5);
            return StandardMsCharacterizationUtility.GetDefaultCharacterizationResultForSingleAcylChainLipid(molecule, defaultResult);
        }


        public static (ILipid, double[]) Characterize4TriacylGlycerols(
            IMSScanProperty scan, ILipid molecule, MoleculeMsReference reference,
            float tolerance, float mzBegin, float mzEnd) {

            var defaultResult = EieioMsCharacterizationUtility.GetDefaultScore(
                    scan, reference, tolerance, mzBegin, mzEnd, 1, 0, 1, 0.5);
            return StandardMsCharacterizationUtility.GetDefaultCharacterizationResultForTriacylGlycerols(molecule, defaultResult);
        }
    }
}
