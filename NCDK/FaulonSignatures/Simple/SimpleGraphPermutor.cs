using System.Collections;
using System.Collections.Generic;

namespace NCDK.FaulonSignatures.Simple
{
    public class SimpleGraphPermutor : Permutor, IEnumerable<SimpleGraph>
    {
        private SimpleGraph graph;

        public SimpleGraphPermutor(SimpleGraph graph)
                : base(graph.GetVertexCount())
        {
            this.graph = graph;
        }

        public IEnumerator<SimpleGraph> GetEnumerator()
        {
            while (base.HasNext())
            {
                int[] nextPermutation = base.GetNextPermutation();
                yield return new SimpleGraph(graph, nextPermutation);
            }
            yield break;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
