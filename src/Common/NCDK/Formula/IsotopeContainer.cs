using NCDK.Tools.Manipulator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NCDK.Formula
{
    /// <summary>
    /// This class defines a isotope container. It contains in principle a
    /// <see cref="IMolecularFormula"/>, a mass and intensity/abundance value.
    /// </summary>
    // @author Miguel Rojas Cherto
    // @cdk.module  formula
    public class IsotopeContainer
    {
        private List<IMolecularFormula> forms = new List<IMolecularFormula>();
        private double mass;
        private double intensity;

        public IsotopeContainer()
        {
        }

        /// <summary>
        /// Constructor of the <see cref="IsotopeContainer"/> object setting a <see cref="IMolecularFormula"/> object and intensity value.
        /// </summary>
        /// <param name="formula">The formula of this container</param>
        /// <param name="intensity">The intensity of this container</param>
        public IsotopeContainer(IMolecularFormula formula, double intensity)
        {
            forms.Add(formula);
            if (formula != null)
                Mass = MolecularFormulaManipulator.GetTotalExactMass(formula);
            Intensity = intensity;
        }

        /// <summary>
        /// Constructor of the <see cref="IsotopeContainer"/> object setting a mass and intensity value.
        /// </summary>
        /// <param name="mass">The mass of this container</param>
        /// <param name="intensity">The intensity of this container</param>
        public IsotopeContainer(double mass, double intensity)
        {
            Mass = mass;
            Intensity = intensity;
        }

        public IsotopeContainer(IsotopeContainer container)
        {
            mass = container.mass;
            intensity = container.intensity;
            forms = new List<IMolecularFormula>(container.forms);
        }

        /// <summary>
        /// The <see cref="IMolecularFormula"/> object of this container.
        /// </summary>
        public IMolecularFormula Formula
        {
            get => !forms.Any() ? null : forms[0];
            set
            {
                forms.Clear();
                forms.Add(value);
            }
        }

        /// <summary>
        /// The formulas of this isotope container.
        /// </summary>
        public IReadOnlyList<IMolecularFormula> Formulas
        {
            get => forms;
        }

        /// <summary>
        /// Add a formula to this isotope container.
        /// </summary>
        /// <param name="formula">the new formula</param>
        public void AddFormula(IMolecularFormula formula)
        {
            this.forms.Add(formula);
        }

        /// <summary>
        /// the mass value of this container.
        /// </summary>
        public double Mass
        {
            get => mass;
            set => mass = value;
        }

        /// <summary>
        /// the intensity value of this container.
        /// </summary>
        public double Intensity
        {
            get => intensity;
            set => intensity = value;
        }

        /// <summary>
        /// Clones this <see cref="IsotopeContainer"/> object and its content.
        /// </summary>
        /// <returns>The cloned object</returns>
        public object Clone()
        {
            var isoClone = new IsotopeContainer();
            isoClone.forms.AddRange(forms);
            isoClone.intensity = intensity;
            isoClone.mass = mass;
            return isoClone;
        }

        /// <summary>
        /// Pretty-print the MFs of this isotope container. 
        /// </summary>
        /// <returns>the MFs</returns>
        string GetFormulasString()
        {
            var sb = new StringBuilder();
            foreach (var mf in Formulas)
            {
                if (sb.Length != 0)
                    sb.Append(", ");
                sb.Append(MolecularFormulaManipulator.GetString(mf, false, true));
            }
            return sb.ToString();
        }

        public override string ToString()
        {
            return "IsotopeContainer{" +
                   "mass=" + mass +
                   ", intensity=" + intensity +
                   ", MF=" + GetFormulasString() +
                   '}';
        }
    }
}
