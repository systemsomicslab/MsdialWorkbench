using CompMs.Common.Lipidomics;
using CompMs.CommonMVVM;
using System.Collections.ObjectModel;

namespace CompMs.App.SpectrumViewer.Model
{
    public class SpectrumGenerationRuleModel : BindableBase {

        public SpectrumGenerationRuleModel(ObservableCollection<MzVariableModel> variables) {
            Variables = variables;
        }

        public double Intensity {
            get => intensity;
            set => SetProperty(ref intensity, value);
        }
        private double intensity;

        public string Comment {
            get => comment;
            set => SetProperty(ref comment, value);
        }
        private string comment;

        public ObservableCollection<MzVariableModel> Variables { get; }

        public MzVariableModel Variable {
            get => variable;
            set => SetProperty(ref variable, value);
        }
        private MzVariableModel variable;
 
        public ISpectrumGenerationRule Create() {
            return new MzVariableRule(Variable.Prepare(), Intensity, Comment);
        }
    }
}
