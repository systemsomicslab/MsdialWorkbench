using System;
using System.Linq;

namespace NCDK.LibIO.CML
{
    public partial class CMLCrystal
    {
        /// <summary>
        /// dictRef ids for 6 scalar children of crystal.
        /// </summary>
        static readonly string[] CRYSTAL_DICT_REFS =
            {
                "cml:a",
                "cml:b",
                "cml:c",
                "cml:alpha",
                "cml:beta",
                "cml:gamma",
            };

        /// <summary>
        ///  unit refs for 6 scalar children of crystal.
        /// </summary>
        static readonly string[] CRYSTAL_DICT_UNITS =
            {
                "units:ang",
                "units:ang",
                "units:ang",
                "units:degree",
                "units:degree",
                "units:degree",
            };

        public static CMLScalar CreateScalar(string dictRef, double param, string unitRef, double error)
        {
            CMLScalar scalar = new CMLScalar(param)
            {
                DictRef = dictRef
            };
            if (unitRef != null)
            {
                scalar.Units = unitRef;
            }
            if (!double.IsNaN(error))
            {
                scalar.ErrorValue = error;
            }
            return scalar;
        }

        public void SetCellParameters(double[] parameters)
        {
            SetCellParameters(parameters, null);
        }

        public void SetCellParameters(double[] parameters, double[] error)
        {
            if (parameters == null || parameters.Length != 6)
            {
                throw new ApplicationException("Must have 6 cell parameters");
            }
            var cellParamVector = this.Elements(XName_CML_scalar);
            var count = cellParamVector.Count();
            if (count == 0)
            {
                for (int i = 0; i < 6; i++)
                {
                    CMLScalar cellParam = CMLCrystal.CreateScalar(
                            CRYSTAL_DICT_REFS[i], parameters[i], CRYSTAL_DICT_UNITS[i],
                            double.NaN);
                    this.Add(cellParam);
                }
            }
            else if (count == 6)
            {
                int i = 0;
                foreach (var e in cellParamVector)
                {
                    CMLScalar cellParam = (CMLScalar)e;
                    cellParam.SetValue(parameters[i]);
                    if (error != null)
                        cellParam.ErrorValue = parameters[i];
                    cellParam.DictRef = CRYSTAL_DICT_REFS[i];
                    i++;
                }
            }
            else
            {
                throw new ApplicationException(
                            "Corrupted cell parameters: must be exactly 6 (found: "
                                    + cellParamVector.Count());
            }
        }
    }
}
