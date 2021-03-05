using CompMs.CommonMVVM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompMs.Graphics.UI.ProgressBar
{
    public class ProgressBarVM : ViewModelBase
    {
        public string Label {
            get => label;
            set => SetProperty(ref label, value);
        }
        public int CurrentValue {
            get => currentValue;
            set => SetProperty(ref currentValue, value);
        }

        public bool IsIndeterminate {
            get => isIndeterminate;
            set => SetProperty(ref isIndeterminate, value);
        }

        private string label = string.Empty;
        private int currentValue = 0;
        private bool isIndeterminate = false;
    }
}
