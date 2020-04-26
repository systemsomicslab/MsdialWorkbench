namespace CompMs.Common.Mathematics.Matrix {
    public class LUmatrix
    {
        double[,] matrix;
        int[] indexVector;
        double reverse;

        public double[,] Matrix
        {
            get { return matrix; }
            set { matrix = value; }
        }

        public int[] IndexVector
        {
            get { return indexVector; }
            set { indexVector = value; }
        }

        public double Reverse
        {
            get { return reverse; }
            set { reverse = value; }
        }

        public LUmatrix(double[,] matrix, int[] indexVector, double reverse) { this.matrix = matrix; this.indexVector = indexVector; this.reverse = reverse; }
    }
}
