using System.Globalization;

namespace NCDK.LibIO.CML
{
    public partial class CMLScalar
    {
        public CMLScalar(int value)
            : this()
        {
            SetValue(value);
        }

        public CMLScalar(double value)
            : this()
        {
            SetValue(value);
        }

        public CMLScalar(bool value)
            : this()
        {
            SetValue(value);
        }

        public void SetValue(bool scalar)
        {
            Value = scalar.ToString(NumberFormatInfo.InvariantInfo).ToLowerInvariant();
            DataType = "xsd:boolean";
        }

        public void SetValue(double scalar)
        {
            Value = scalar.ToString(NumberFormatInfo.InvariantInfo);
            DataType = "xsd:double";
        }

        /// <summary>
        /// sets value to int.. updates dataType.
        /// </summary>
        /// <param name="scalar"></param>
        public void SetValue(int scalar)
        {
            Add(scalar.ToString(NumberFormatInfo.InvariantInfo));
            DataType = "xsd:integer";
        }
    }
}
