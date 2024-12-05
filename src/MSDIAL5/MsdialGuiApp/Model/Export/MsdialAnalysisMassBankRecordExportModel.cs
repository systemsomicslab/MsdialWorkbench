using CompMs.App.Msdial.Model.DataObj;
using CompMs.Common.Extension;
using CompMs.Common.Parser;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using System;
using System.IO;
using System.Linq;

namespace CompMs.App.Msdial.Model.Export
{
    internal sealed class MsdialAnalysisMassBankRecordExportModel : BindableBase, IMsdialAnalysisExport
    {
        private readonly MassBankRecordHandler _handler;
        private readonly StudyContextModel _studyContextModel;

        public MsdialAnalysisMassBankRecordExportModel(ProjectBaseParameter parameter, StudyContextModel studyContextModel) {
            if (parameter is null) {
                throw new ArgumentNullException(nameof(parameter));
            }

            var splash = new NSSplash.Splash();
            _handler = new MassBankRecordHandler(parameter.IonMode, parameter.InstrumentType, scan => scan.Spectrum.IsEmptyOrNull() ? "NA" : splash.splashIt(new NSSplash.impl.MSSpectrum(string.Join(" ", scan.Spectrum.Select(p => $"{p.Mass}:{p.Intensity}")))));
            _studyContextModel = studyContextModel;
        }

        public string Label { get; } = "MassBank record";

        public string? ContributorID {
            get => _contributorID;
            set => SetProperty(ref _contributorID, value);
        }
        private string? _contributorID;

        public bool ShouldExport {
            get => _shouldExport;
            set => SetProperty(ref _shouldExport, value);
        }
        private bool _shouldExport = false;

        void IMsdialAnalysisExport.Export(string destinationFolder, AnalysisFileBeanModel fileBeanModel) {
            if (!ShouldExport) {
                return;
            }

            _handler.Authors = _studyContextModel.Authors;
            _handler.License = _studyContextModel.License;
            _handler.Instrument = _studyContextModel.Instrument;
            _handler.InstrumentType = _studyContextModel.InstrumentType;
            _handler.ContributorIdentifier = ContributorID ?? string.Empty;

            var features = ChromatogramPeakFeatureCollection.LoadAsync(fileBeanModel.PeakAreaBeanInformationFilePath).Result;
            features = features.Flatten();
            var loader = fileBeanModel.MSDecLoader;

            foreach (var feature in features.Items) {
                var msdec = loader.LoadMSDecResult(feature.GetMSDecResultID());
                var path = Path.Combine(destinationFolder, Path.GetFileNameWithoutExtension(fileBeanModel.AnalysisFileName), _handler.GetAccession(feature) + ".txt");
                if (!Directory.Exists(Path.GetDirectoryName(path))) {
                    Directory.CreateDirectory(Path.GetDirectoryName(path));
                }
                using (var stream = File.Open(path, FileMode.Create, FileAccess.Write)) {
                    _handler.WriteRecord(stream, feature, feature, msdec, feature);
                }
            }
        }
    }
}
