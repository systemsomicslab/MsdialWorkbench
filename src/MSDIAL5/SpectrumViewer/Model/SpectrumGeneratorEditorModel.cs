using CompMs.Common.DataObj.Property;
using CompMs.Common.Enum;
using CompMs.Common.Interfaces;
using CompMs.Common.Lipidomics;
using CompMs.Common.Parser;
using CompMs.CommonMVVM;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace CompMs.App.SpectrumViewer.Model
{
    public class SpectrumGeneratorEditorModel : BindableBase
    {
        public SpectrumGeneratorEditorModel() {
            SpectrumModel = new SpectrumModel(string.Empty);

            LipidClasses = Enum.GetValues(typeof(LbmClass)).Cast<LbmClass>().ToList().AsReadOnly();
            LipidClass = LbmClass.PC;

            Adducts = new List<AdductIon>
            {
                AdductIon.GetAdductIon("[M+H]+"),
                AdductIon.GetAdductIon("[M+Na]+"),
            }.AsReadOnly();
            Adduct = Adducts[0];

            Rules = new ObservableCollection<SpectrumGenerationRuleModel>();
            PreviewAdduct = Adducts[0];

            Variables = new ObservableCollection<MzVariableModel>();

            Factory = new LipidSpectrumGeneratorFactory();
        }

        public SpectrumModel SpectrumModel { get; }

        public LipidSpectrumGeneratorFactory Factory { get; }

        public IMSScanProperty PreviewedSpectrum {
            get => previewedSpectrum;
            set => SetProperty(ref previewedSpectrum, value);
        }
        private IMSScanProperty previewedSpectrum;

        public ReadOnlyCollection<LbmClass> LipidClasses { get; }
        public LbmClass LipidClass {
            get => lipidClass;
            set => SetProperty(ref lipidClass, value);
        }
        private LbmClass lipidClass;

        public ReadOnlyCollection<AdductIon> Adducts { get; }
        public AdductIon Adduct {
            get => adduct;
            set => SetProperty(ref adduct, value);
        }
        private AdductIon adduct;

        public ObservableCollection<MzVariableModel> Variables { get; }

        public void AddVariable() {
            Variables.Add(new MzVariableModel(Variables));
        }

        public void RemoveVariable(MzVariableModel variable) {
            if (Variables.Contains(variable)) {
                Variables.Remove(variable);
            }
        }

        public ObservableCollection<SpectrumGenerationRuleModel> Rules { get; }

        public void AddRule() {
            Rules.Add(new SpectrumGenerationRuleModel(Variables));
        }

        public void RemoveRule(SpectrumGenerationRuleModel rule) {
            if (Rules.Contains(rule)) {
                Rules.Remove(rule);
            }
        }

        public LipidSelectionModel PreviewLipidModel { get; } = new LipidSelectionModel();

        public AdductIon PreviewAdduct {
            get => previewAdduct;
            set => SetProperty(ref previewAdduct, value);
        }
        private AdductIon previewAdduct;

        public void PreviewSpectrum() {
            if (PreviewedSpectrum != null) {
                SpectrumModel.RemoveScan(PreviewedSpectrum);
            }
            var generator = Factory.Create(lipidClass, adduct, Rules.Select(m => m.Create()).Where(rule => rule != null).ToArray());
            PreviewedSpectrum = PreviewLipidModel.Create().GenerateSpectrum(generator, PreviewAdduct);
            SpectrumModel.AddScan(PreviewedSpectrum);
        }
    }
}
