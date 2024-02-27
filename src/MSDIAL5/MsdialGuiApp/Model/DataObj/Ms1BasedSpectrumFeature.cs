using CompMs.App.Msdial.Model.Service;
using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Utility;
using System;
using System.ComponentModel;

namespace CompMs.App.Msdial.Model.DataObj
{
    public sealed class Ms1BasedSpectrumFeature : BindableBase, IDisposable, IAnnotatedObject
    {
        private SpectrumFeature _spectrumFeature;
        private bool _disposedValue;

        public Ms1BasedSpectrumFeature(SpectrumFeature spectrumFeature) {
            _spectrumFeature = spectrumFeature;
            _comment = spectrumFeature.Comment;
            Molecule = new MoleculeModel(spectrumFeature.AnnotatedMSDecResult.Molecule);
            Scan = new ScanModel(spectrumFeature.AnnotatedMSDecResult.MSDecResult);
            MatchResults = new MsScanMatchResultContainerModel(spectrumFeature.AnnotatedMSDecResult.MatchResults);
            _quantifiedChromatogramPeak = spectrumFeature.QuantifiedChromatogramPeak;
        }

        public MoleculeModel Molecule { get; }
        public ScanModel Scan { get; }
        public MsScanMatchResultContainerModel MatchResults { get; }

        public QuantifiedChromatogramPeak QuantifiedChromatogramPeak {
            get => _quantifiedChromatogramPeak;
            set => SetProperty(ref _quantifiedChromatogramPeak, value);
        }
        private QuantifiedChromatogramPeak _quantifiedChromatogramPeak;

        public string Comment {
            get => _comment;
            set => SetProperty(ref _comment, value);
        }
        private string _comment;

        public bool Confirmed {
            get => _spectrumFeature.TagCollection.IsSelected(PeakSpotTag.CONFIRMED);
            set => SetPeakSpotTag(PeakSpotTag.CONFIRMED, value, nameof(Confirmed));
        }
        public bool LowQualitySpectrum {
            get => _spectrumFeature.TagCollection.IsSelected(PeakSpotTag.LOW_QUALITY_SPECTRUM);
            set => SetPeakSpotTag(PeakSpotTag.LOW_QUALITY_SPECTRUM, value, nameof(LowQualitySpectrum));
        }
        public bool Misannotation {
            get => _spectrumFeature.TagCollection.IsSelected(PeakSpotTag.MISANNOTATION);
            set => SetPeakSpotTag(PeakSpotTag.MISANNOTATION, value, nameof(Misannotation));
        }
        public bool Coelution {
            get => _spectrumFeature.TagCollection.IsSelected(PeakSpotTag.COELUTION);
            set => SetPeakSpotTag(PeakSpotTag.COELUTION, value, nameof(Coelution));
        }
        public bool Overannotation {
            get => _spectrumFeature.TagCollection.IsSelected(PeakSpotTag.OVERANNOTATION);
            set => SetPeakSpotTag(PeakSpotTag.OVERANNOTATION, value, nameof(Overannotation));
        }

        private bool SetPeakSpotTag(PeakSpotTag tag, bool value, string propertyname) {
            if (value == _spectrumFeature.TagCollection.IsSelected(tag)) {
                return false;
            }
            if (value) {
                _spectrumFeature.TagCollection.Select(tag);
            }
            else {
                _spectrumFeature.TagCollection.Deselect(tag);
            }
            OnPropertyChanged(propertyname);
            return true;
        }

        public void SwitchPeakSpotTag(PeakSpotTag tag) {
            if (tag == PeakSpotTag.CONFIRMED) {
                Confirmed = !Confirmed;
            }
            if (tag == PeakSpotTag.LOW_QUALITY_SPECTRUM) {
                LowQualitySpectrum = !LowQualitySpectrum;
            }
            if (tag == PeakSpotTag.MISANNOTATION) {
                Misannotation = !Misannotation;
            }
            if (tag == PeakSpotTag.COELUTION) {
                Coelution = !Coelution;
            }
            if (tag == PeakSpotTag.OVERANNOTATION) {
                Overannotation = !Overannotation;
            }
        }

        public PeakSpotTagCollection TagCollection => _spectrumFeature.TagCollection;

        public SpectrumFeature GetCurrentSpectrumFeature() => _spectrumFeature;

        protected override void OnPropertyChanged(PropertyChangedEventArgs args) {
            base.OnPropertyChanged(args);
            if (args.PropertyName == nameof(QuantifiedChromatogramPeak)) {
                _spectrumFeature = new SpectrumFeature(_spectrumFeature.AnnotatedMSDecResult, QuantifiedChromatogramPeak, _spectrumFeature.FeatureFilterStatus)
                {
                    Comment = Comment,
                };
            }
            if (args.PropertyName == nameof(Comment)) {
                _spectrumFeature.Comment = Comment;
            }
        }

        private void Dispose(bool disposing) {
            if (!_disposedValue) {
                if (disposing) {
                    MatchResults?.Dispose();
                }
                _disposedValue = true;
            }
        }

        public void Dispose() {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public void SetConfidence(MoleculeMsReference reference, MsScanMatchResult result) {
            DataAccess.SetMoleculeMsPropertyAsConfidence(Molecule, reference);
            MatchResults.RemoveManuallyResults();
            MatchResults.AddResult(result);
            OnPropertyChanged(string.Empty);
        }

        public void SetUnsettled(MoleculeMsReference reference, MsScanMatchResult result) {
            DataAccess.SetMoleculeMsPropertyAsUnsettled(Molecule, reference);
            MatchResults.RemoveManuallyResults();
            MatchResults.AddResult(result);
            OnPropertyChanged(string.Empty);
        }

        public void SetUnknown(UndoManager undoManager) {
            IDoCommand command = new SetUnknownDoCommand(Molecule, MatchResults);
            command.Do();
            undoManager.Add(command);
        }

        MsScanMatchResultContainer IAnnotatedObject.MatchResults => _spectrumFeature.AnnotatedMSDecResult.MatchResults;
    }
}
