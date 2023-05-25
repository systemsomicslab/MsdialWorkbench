using CompMs.Common.DataStructure;

namespace CompMs.Common.Lipidomics
{
    internal class ChainShorthandNotation : IVisitor<AcylChain, AcylChain>, IVisitor<AlkylChain, AlkylChain>, IVisitor<SphingoChain, SphingoChain>
    {
        private readonly ChainVisitor _chainVisitor;

        public static ChainShorthandNotation Default { get; } = new ChainShorthandNotation();

        private ChainShorthandNotation() {
            var builder = new ChainVisitorBuilder();
            var director = new ShorthandNotationDirector(builder);
            director.Construct();
            _chainVisitor = builder.Create();
        }

        public AcylChain Visit(AcylChain item) {
            return ((IVisitor<AcylChain, AcylChain>)_chainVisitor).Visit(item);
        }

        public AlkylChain Visit(AlkylChain item) {
            return ((IVisitor<AlkylChain, AlkylChain>)_chainVisitor).Visit(item);
        }

        public SphingoChain Visit(SphingoChain item) {
            return ((IVisitor<SphingoChain, SphingoChain>)_chainVisitor).Visit(item);
        }
    }
}
