using CompMs.App.Msdial.Model.Chart;
using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Setting;
using CompMs.Common.DataObj;
using CompMs.Common.DataObj.Property;
using CompMs.Common.Enum;
using CompMs.Common.FormulaGenerator.Parser;
using CompMs.Common.Parameter;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.DataObj;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CompMs.App.Msdial.Model.Search {
    internal class InternalMsFinderMetaboliteList : BindableBase {
        private readonly AlignmentSpotPropertyModelCollection _spots;
        private readonly InternalMsfinderSettingModel _settingModel;

        public InternalMsFinderMetaboliteList(List<MsfinderQueryFile>msfinderQueryFiles, AlignmentSpotPropertyModelCollection spots, InternalMsfinderSettingModel settingModel, AnalysisParamOfMsfinder parameter) {
            _spots = spots;
            _settingModel = settingModel;
            var metabolites = LoadMetabolites(msfinderQueryFiles, parameter);
            _observedMetabolites = new ObservableCollection<MsfinderObservedMetabolite>(metabolites);
            ObservedMetabolites = new ReadOnlyObservableCollection<MsfinderObservedMetabolite>(_observedMetabolites);
            _selectedObservedMetabolite = ObservedMetabolites.FirstOrDefault();
        }

        private List<MsfinderObservedMetabolite> LoadMetabolites(List<MsfinderQueryFile>msfinderQueryFiles, AnalysisParamOfMsfinder parameter) {
            var metaboliteList = new List<MsfinderObservedMetabolite>();
            foreach (var queryFile in msfinderQueryFiles) {
                var metabolite = new MsfinderObservedMetabolite(queryFile, parameter);
                metaboliteList.Add(metabolite);
            }
            return metaboliteList;
        }

        public ReadOnlyObservableCollection<MsfinderObservedMetabolite> ObservedMetabolites { get; }
        private ObservableCollection<MsfinderObservedMetabolite> _observedMetabolites;

        public MsfinderObservedMetabolite? SelectedObservedMetabolite {
            get => _selectedObservedMetabolite; 
            set => SetProperty(ref _selectedObservedMetabolite, value);
        }
        private MsfinderObservedMetabolite? _selectedObservedMetabolite;
    }

    internal sealed class MsfinderObservedMetabolite : BindableBase {
        private readonly MsfinderQueryFile _queryFile;
        private readonly AnalysisParamOfMsfinder _parameter;
        private RawData? _spotData;

        public string metaboliteName {
            get => _spotData.Name;
            set
            {
                if (_spotData.Name != value)
                {
                    _spotData.Name = value;
                    OnPropertyChanged(nameof(metaboliteName));
                }
            }
        }
        public int alignmentID { 
            get => _spotData.ScanNumber;
            set
            {
                if (_spotData?.ScanNumber != value)
                {
                    _spotData.ScanNumber = value;
                    OnPropertyChanged(nameof(alignmentID));
                }
            }
        }
        public double retentionTime {
            get => _spotData.RetentionTime;
            set
            {
                if (_spotData.RetentionTime != value)
                {
                    _spotData.RetentionTime = value;
                    OnPropertyChanged(nameof(retentionTime));
                }
            }
        }
        public double centralCcs {  
            get => _spotData.Ccs;
            set 
            {
                if (_spotData.Ccs != value)
                {
                    _spotData.Ccs = value;
                    OnPropertyChanged(nameof(centralCcs));
                }
            } 
        }
        public double mass {  
            get => _spotData.PrecursorMz;
            set 
            {
                if (_spotData.PrecursorMz != value)
                {
                    _spotData.PrecursorMz = value;
                    OnPropertyChanged(nameof(mass));
                }
            }
        }
        public string adduct { 
            get => _spotData.PrecursorType;
            set 
            {
                if (_spotData.PrecursorType != value)
                {
                    _spotData.PrecursorType = value;
                    OnPropertyChanged(nameof(adduct));
                }
            } 
        }
        public string formula {  
            get => _spotData.Formula;
            set 
            {
                if (_spotData?.Formula != value)
                {
                    _spotData.Formula = value;
                    OnPropertyChanged(nameof(formula));
                }
            }
        }
        public string ontology {  
            get => _spotData.Ontology;
            set 
            {
                if (_spotData.Ontology != value)
                {
                    _spotData.Ontology = value;
                    OnPropertyChanged(nameof(ontology));
                }
            } 
        }
        public string smiles {  
            get => _spotData.Smiles;
            set 
            {
                if (_spotData.Smiles != value)
                {
                    _spotData.Smiles = value;
                    OnPropertyChanged(nameof(smiles));
                }
            } 
        } 
        public string inchikey { 
            get => _spotData.InchiKey;
            set 
            {
                if (_spotData.InchiKey != value)
                {
                    _spotData.InchiKey = value;
                    OnPropertyChanged(nameof(inchikey));
                }
            } 
        }
        public string comment { 
            get => _spotData.Comment;
            set 
            {
                if (_spotData.Comment != value)
                {
                    _spotData.Comment = value;
                    OnPropertyChanged(nameof(comment));
                }
            } 
        }
        public IonMode ionMode { 
            get => _spotData.IonMode;
            set 
            {
                if (_spotData.IonMode != value)
                {
                    _spotData.IonMode = value;
                    OnPropertyChanged(nameof(ionMode));
                    OnPropertyChanged(nameof(adductIons));
                }
            }
        }
        public List<AdductIon> adductIons
        {
            get 
            {
                if (ionMode == IonMode.Positive)
                {
                    return _parameter.MS1PositiveAdductIonList;
                }
                if (ionMode == IonMode.Negative)
                {
                    return _parameter.MS1NegativeAdductIonList;
                }
                else 
                {
                    return _parameter.MS1PositiveAdductIonList.Concat(_parameter.MS1NegativeAdductIonList).ToList();
                }
            }
        }
        public MSDataType spectrumType
        {
            get => _spotData.SpectrumType;
            set
            {
                if (_spotData.SpectrumType != value)
                {
                    _spotData.SpectrumType = value;
                    OnPropertyChanged(nameof(spectrumType));
                }
            }
        }
        public int ms1Num {
            get => _spotData.Ms1PeakNumber;
        }
        public int ms2Num {
            get => _spotData.Ms2PeakNumber;
        }
        public MsSpectrum ms1Spectrum { get; private set; }
        public MsSpectrum ms2Spectrum { get; private set; }

        public MsfinderObservedMetabolite(MsfinderQueryFile queryFile, AnalysisParamOfMsfinder parameter) {
            _queryFile = queryFile;
            _parameter = parameter;
            Load();
        }

        public AlignmentSpotPropertyModel Spot { get; private set; }
        public InternalMsfinderSettingModel SettingModel { get; }

        private void Load() {
            _spotData = RawDataParcer.RawDataFileReader(_queryFile.RawDataFilePath, _parameter);
            ms1Spectrum = new MsSpectrum(_spotData.Ms1Spectrum);
            ms2Spectrum = new MsSpectrum(_spotData.Ms2Spectrum);
        }

        public Task ClearAsync(CancellationToken token = default) {
            throw new NotImplementedException();
            // formula = null;
        }

        public Task ReflectToMsdialAsync(CancellationToken token = default) {
            throw new NotImplementedException();
            //Spot.Formula = formula;
        }
    }
}
