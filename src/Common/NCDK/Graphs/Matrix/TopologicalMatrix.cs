namespace NCDK.Graphs.Matrix
{
    /// <summary>
    /// Calculator for a topological matrix representation of this <see cref="IAtomContainer"/>. An
    /// topological matrix is a matrix of quare NxN matrix, where N is the number of
    /// atoms in the <see cref="IAtomContainer"/>. The element i,j of the matrix is the distance between
    /// two atoms in a molecule.
    /// </summary>
    // @author federico
    // @cdk.module  qsarmolecular
    public class TopologicalMatrix : IGraphMatrix
    {
        /// <summary>
        /// Returns the topological matrix for the given AtomContainer.
        /// </summary>
        /// <param name="container">The AtomContainer for which the matrix is calculated</param>
        /// <returns>A topological matrix representating this AtomContainer</returns>
        public static int[][] GetMatrix(IAtomContainer container)
        {
            int[][] conMat = AdjacencyMatrix.GetMatrix(container);
            int[][] topolDistance = PathTools.ComputeFloydAPSP(conMat);

            return topolDistance;
        }
    }
}
