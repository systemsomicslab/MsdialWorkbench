using System;

namespace NCDK.QSAR
{
    [AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
    sealed class DescriptorResultAttribute : Attribute
    {
        public string Prefix { get; private set; }

        public int BaseIndex { get; private set; }

        public DescriptorResultAttribute(string prefix = null, int baseIndex = 0)
        {
            this.Prefix = null;
            this.BaseIndex = BaseIndex;
        }
    }
}
