using CompMs.Common.Lipidomics;
using CompMs.CommonMVVM;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace CompMs.App.SpectrumViewer.Model
{
    public class MzVariableModel : BindableBase
    {
        public MzVariableModel(ObservableCollection<MzVariableModel> mzs) {
            Mzs = new ReadOnlyObservableCollection<MzVariableModel>(mzs);
        }

        public static ReadOnlyCollection<string> VariableTypes { get; } = new List<string>
        {
            "Constant",
            "Placeholder",
            "Loss",
            "EAD chain desorption"
        }.AsReadOnly();

        public string VariableType {
            get => variableType;
            set => SetProperty(ref variableType, value);
        }
        private string variableType  = "Constant"; 

        public static ReadOnlyCollection<string> PlaceholderTypes { get; } = new List<string>
        {
            "Precursor",
            "All chains",
            "Specific chain",
        }.AsReadOnly();

        public string PlaceholderType {
            get => placeholderType;
            set => SetProperty(ref placeholderType, value);
        }
        private string placeholderType = "Precursor";

        public double ExactMass {
            get => exactMass;
            set => SetProperty(ref exactMass, value);
        }
        private double exactMass;

        public int ChainPosition {
            get => chainPosition;
            set => SetProperty(ref chainPosition, value);
        }
        private int chainPosition = 1;

        public ReadOnlyObservableCollection<MzVariableModel> Mzs { get; }

        public MzVariableModel Left {
            get => left;
            set => SetProperty(ref left, value);
        }

        public MzVariableModel Right {
            get => right;
            set => SetProperty(ref right, value);
        }
        private MzVariableModel left, right;

        private IMzVariable cache;
        protected override void OnPropertyChanged(PropertyChangedEventArgs args) {
            cache = null;
            base.OnPropertyChanged(args);
        }

        public IMzVariable Prepare() {
            if (cache != null) {
                return cache;
            }
            switch (VariableType) {
                case "Constant":
                    return cache = new ConstantMz(ExactMass);
                case "Placeholder":
                    switch (PlaceholderType) {
                        case "Precursor":
                            return cache = new PrecursorMz();
                        case "All chains":
                            return cache = new MolecularLevelChains();
                        case "Specific chain":
                            return cache = new PositionChainMz(ChainPosition);
                    }
                    break;
                case "Loss":
                    return cache = new LossMz(Left.Prepare(), Right.Prepare());
                case "EAD chain desorption":
                    return cache = new ChainDesorptionMz(ChainPosition);
            }
            return new EmptyMz();
        }

        public override string ToString() {
            switch (VariableType) {
                case "Constant":
                    return $"Constant: {ExactMass}";
                case "Placeholder":
                    switch (PlaceholderType) {
                        case "Precursor":
                            return "Precursor";
                        case "All chains":
                            return "All chains";
                        case "Specific chain":
                            return $"Chain {ChainPosition}";
                    }
                    break;
                case "Loss":
                    try {
                        return $"{Left} - {Right}";
                    }
                    catch (StackOverflowException) {
                        return "Loss";
                    }
                case "EAD chain desorption":
                    return $"EAD chain desorption: {ChainPosition}";
            }
            return string.Empty;
        }
    }
}
