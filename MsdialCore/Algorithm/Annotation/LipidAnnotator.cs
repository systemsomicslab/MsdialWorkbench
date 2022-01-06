using CompMs.Common.Components;
using CompMs.Common.DataObj.Property;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.Common.Interfaces;
using CompMs.Common.Lipidomics;
using CompMs.Common.Parameter;
using CompMs.MsdialCore.DataObj;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.MsdialCore.Algorithm.Annotation
{
    public class EadLipidAnnotator : IAnnotator<ChromatogramPeakFeature, MoleculeMsReference, MsScanMatchResult>
    {
        public string Key { get; } = "EadLipid";

        public EadLipidAnnotator() {
            lipidGenerator = new LipidGenerator();
        }

        private readonly ILipidGenerator lipidGenerator;

        public MsScanMatchResult Annotate(ChromatogramPeakFeature query) {
            throw new NotImplementedException();
        }

        public MsScanMatchResult CalculateScore(ChromatogramPeakFeature query, MoleculeMsReference reference) {
            throw new NotImplementedException();
        }

        public List<MsScanMatchResult> FilterByThreshold(IEnumerable<MsScanMatchResult> results, MsRefSearchParameterBase parameter = null) {
            throw new NotImplementedException();
        }

        public List<MsScanMatchResult> FindCandidates(ChromatogramPeakFeature query) {
            var scans = SearchCore(query);
            throw new NotImplementedException();
        }

        public bool IsAnnotationSuggested(MsScanMatchResult result, MsRefSearchParameterBase parameter = null) {
            throw new NotImplementedException();
        }

        public bool IsReferenceMatched(MsScanMatchResult result, MsRefSearchParameterBase parameter = null) {
            throw new NotImplementedException();
        }

        public MoleculeMsReference Refer(MsScanMatchResult result) {
            throw new NotImplementedException();
        }

        public List<MoleculeMsReference> Search(ChromatogramPeakFeature query) {
            return SearchCore(query).Select(scan => ConvertToReference(scan, query)).ToList();
        }

        private IEnumerable<IMSScanProperty> SearchCore(ChromatogramPeakFeature query) {
            var lipid = ConvertToLipid(query);
            if (lipid is null) {
                return new List<MoleculeMsReference>(0);
            }

            var lipids = lipid.Generate(lipidGenerator);
            return lipids.Select(l => l.GenerateSpectrum(FacadeLipidSpectrumGenerator.Default, query.AdductType, query));
        }

        public List<MsScanMatchResult> SelectReferenceMatchResults(IEnumerable<MsScanMatchResult> results, MsRefSearchParameterBase parameter = null) {
            throw new NotImplementedException();
        }

        public MsScanMatchResult SelectTopHit(IEnumerable<MsScanMatchResult> results, MsRefSearchParameterBase parameter = null) {
            throw new NotImplementedException();
        }

        private ILipid ConvertToLipid(IMoleculeProperty molecule) {
            return FacadeLipidParser.Default.Parse(molecule.Name);
        }

        private MoleculeMsReference ConvertToReference(IMSScanProperty scan, ChromatogramPeakFeature molecule) {
            return scan as MoleculeMsReference ?? new MoleculeMsReference
            {
                ScanID = -1,
                PrecursorMz = molecule.PrecursorMz,
                IonMode = molecule.IonMode,
                Spectrum = scan.Spectrum,
                Name = molecule.Name,
                Formula = molecule.Formula,
                Ontology = molecule.Ontology,
                SMILES = molecule.SMILES,
                InChIKey = molecule.InChIKey,
                AdductType = molecule.AdductType,
                CompoundClass = molecule.Ontology,
                Charge = molecule.AdductType.ChargeNumber,
                MsLevel = 2,
            };
        }
    }
}
