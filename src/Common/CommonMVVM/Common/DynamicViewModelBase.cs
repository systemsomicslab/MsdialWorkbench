using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompMs.CommonMVVM.Common
{
    // reference(https://blog.okazuki.jp/entry/20100702/1278056325)
    public class DynamicViewModelBase<T> : DynamicObject, INotifyPropertyChanged where T : class
    {
        private T innerModel;

        protected T InnerModel => innerModel;

        public DynamicViewModelBase(T innerModel) {
            this.innerModel = innerModel;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyname) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyname));

        public override bool TryGetMember(GetMemberBinder binder, out object result) {
            var propertyname = binder.Name;
            var property = innerModel.GetType().GetProperty(propertyname);
            if (property == null || !property.CanRead) {
                result = null;
                return false;
            }

            result = property.GetValue(innerModel, null);
            return true;
        }

        public override bool TrySetMember(SetMemberBinder binder, object value) {
            var propertyname = binder.Name;
            var property = innerModel.GetType().GetProperty(propertyname);
            if (property == null || !property.CanWrite) {
                return false;
            }

            var propType = property.PropertyType;
            //Console.WriteLine(propType);
            if (propType == typeof(Single)) {
                property.SetValue(innerModel, float.Parse(value.ToString()), null);
            }
            else if (propType == typeof(Double)) {
                property.SetValue(innerModel, float.Parse(value.ToString()), null);
            }
            else if (propType == typeof(Int32)) {
                property.SetValue(innerModel, float.Parse(value.ToString()), null);
            }
            else if (propType == typeof(Int64)) {
                property.SetValue(innerModel, float.Parse(value.ToString()), null);
            }
            else {
                property.SetValue(innerModel, value, null);
            }
            //property.SetValue(innerModel, value, null);

            OnPropertyChanged(propertyname);
            return true;
        }
    }
}
