using NCDK.FaulonSignatures;
using System.Collections.Generic;
using System.Collections;

namespace NCDK.FaulonSignatures.EdgeColored
{
    public class EdgeColoredGraphPermutor : Permutor, IEnumerable<EdgeColoredGraph>
    {
        private readonly EdgeColoredGraph graph;

        public EdgeColoredGraphPermutor(EdgeColoredGraph graph)
            : base(graph.GetVertexCount())
        {
            this.graph = graph;
        }

        public IEnumerator<EdgeColoredGraph> GetEnumerator()
        {
            while (base.HasNext())
            {
                int[] nextPermutation = base.GetNextPermutation();
                yield return new EdgeColoredGraph(graph, nextPermutation);
            }
            yield break;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
