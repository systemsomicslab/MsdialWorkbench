using CompMs.App.Msdial.Common;
using CompMs.Common.Enum;
using CompMs.Common.Extension;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialDimsCore.Parameter;
using CompMs.MsdialGcMsApi.Parameter;
using CompMs.MsdialImmsCore.Parameter;
using CompMs.MsdialLcImMsApi.Parameter;
using CompMs.MsdialLcmsApi.Parameter;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.App.Msdial.Utility
{
    class ParameterFactory
    {
        public static ParameterBase CreateParameter(Ionization ionization, SeparationType separationType) {
            if (ionization == Ionization.EI && separationType == SeparationType.Chromatography)
                return new MsdialGcmsParameter();
            if (ionization == Ionization.ESI && separationType == SeparationType.Chromatography)
                return new MsdialLcmsParameter();
            if (ionization == Ionization.ESI && separationType == (SeparationType.Chromatography | SeparationType.IonMobility))
                return new MsdialLcImMsParameter();
            if (ionization == Ionization.ESI && separationType == SeparationType.Infusion)
                return new MsdialDimsParameter();
            if (ionization == Ionization.ESI && separationType == (SeparationType.Infusion | SeparationType.IonMobility))
                return new MsdialImmsParameter();
            throw new Exception("Not supported separation type is selected.");
        }

        public static void SetParameterFromAnalysisFiles(ParameterBase parameter, IList<AnalysisFileBean> files) {
            parameter.FileID_AnalysisFileType = new Dictionary<int, AnalysisFileType>();
            parameter.FileID_ClassName = new Dictionary<int, string>();
            foreach (var analysisfile in files) {
                parameter.FileID_ClassName[analysisfile.AnalysisFileId] = analysisfile.AnalysisFileClass;
                parameter.FileID_AnalysisFileType[analysisfile.AnalysisFileId] = analysisfile.AnalysisFileType;
            }

            parameter.ClassnameToOrder = new Dictionary<string, int>();
            parameter.ClassnameToColorBytes = new Dictionary<string, List<byte>>();
            foreach (var (classId, idx) in files.Select(analysisfile => analysisfile.AnalysisFileClass).Distinct().WithIndex()) {
                parameter.ClassnameToOrder[classId] = idx;
                var brush = ChartBrushes.SolidColorBrushList[idx];
                parameter.ClassnameToColorBytes[classId] = new List<byte> { brush.Color.R, brush.Color.G, brush.Color.B, brush.Color.A };
            }

            parameter.IsBoxPlotForAlignmentResult = false;
            var classNumAve = files.GroupBy(analysisfile => analysisfile.AnalysisFileType)
                                   .Average(group => group.Count());
            if (classNumAve > 4)
                parameter.IsBoxPlotForAlignmentResult = true;
        }
    }
}
