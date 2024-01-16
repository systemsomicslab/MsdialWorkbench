using CompMs.Common.Components;
using CompMs.Common.DataObj;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Export;
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialCore.Parameter;
using System.Collections.Generic;

namespace CompMs.MsdialGcMsApi.Export
{
    public sealed class GcmsAnalysisMetadataAccessor : BaseAnalysisMetadataAccessor
    {
        public GcmsAnalysisMetadataAccessor(IMatchResultRefer<MoleculeMsReference, MsScanMatchResult> refer, ParameterBase parameter, ExportspectraType type) : base(refer, parameter, type) {

        }

        protected override string[] GetHeadersCore() {
            return new string[] {
                "Name",
                "Scan",
                "RT (min)",
                "Retention index",
                "Model Masses",
                "Model ion mz",
                "Model ion height",
                "Model ion area",
                "Integrated height",
                "Integrated area",
                "SMILES",
                "InChIKey",
                "Simple dot product",
                "Weighted dot product",
                "Reverse dot product",
                "Fragment presence %",
                "Total score",
                "Spectrum"
            };
        }

        protected override Dictionary<string, string> GetContentCore(ChromatogramPeakFeature feature, MSDecResult msdec, MoleculeMsReference reference, MsScanMatchResult matchResult, IReadOnlyList<RawSpectrum> spectrumList, AnalysisFileBean analysisFile) {
            var content = base.GetContentCore(feature, msdec, reference, matchResult, spectrumList, analysisFile);
            content["Retention index"] = msdec.ChromXs.RI.Value != -1
                ?  string.Format("{0:F2}", msdec.ChromXs.RI.Value)
                : string.Empty;
            content["Model Masses"] = "[" + string.Join(",", msdec.ModelMasses) + "]";
            content["Model ion mz"] = feature.PeakFeature.Mass.ToString();
            content["Model ion height"] = msdec.ModelPeakHeight.ToString();
            content["Model ion area"] = msdec.ModelPeakArea.ToString();
            content["Integrated height"] = msdec.IntegratedHeight.ToString();
            content["Integrated area"] = msdec.IntegratedArea.ToString();
            content["Spectrum"] = content["MSMS spectrum"];
            return content;
        }
    }
}
