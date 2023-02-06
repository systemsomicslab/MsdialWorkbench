using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rfx.Riken.OsakaUniv
{
    public class DataGridWithCheckBox<MyClass>: ViewModelBase
    {
        private bool isChecked = false;
        private MyClass myClass;

        public bool IsChecked {
            get { return isChecked; }
            set { if (this.isChecked == value) return;
                this.isChecked = value;
                OnPropertyChanged("IsChecked");
            }
        }

        public MyClass Value {
            get { return this.myClass; }
            set {
                this.myClass = value;
                OnPropertyChanged("Value");
            }
        }

        public DataGridWithCheckBox(MyClass myClass, bool isChecked = false) {
            this.IsChecked = isChecked;
            this.Value = myClass;
        }
    }
}
