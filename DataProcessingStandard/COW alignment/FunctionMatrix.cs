using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rfx.Riken.OsakaUniv
{
    /// <summary>
    /// This class is used in dynamic programming algorithm of CowAlignment.cs
    /// </summary>
    internal class FunctionMatrix
    {
        private FunctionElement[,] functionElementBeanMatrix;

        public FunctionMatrix(int rowSize, int columnSize)
        {
            this.functionElementBeanMatrix = new FunctionElement[rowSize, columnSize];
        }

        public FunctionElement this[int rowPosition, int columnPosition]
        {
            get { return functionElementBeanMatrix[rowPosition, columnPosition]; }
            set { functionElementBeanMatrix[rowPosition, columnPosition] = value; }
        }
    }
}
