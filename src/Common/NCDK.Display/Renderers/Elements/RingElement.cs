using System.Windows;
using System.Windows.Media;

namespace NCDK.Renderers.Elements
{
    /// <summary>
    /// A ring is just a circle - in other words, an oval whose width and height are the same.
    /// </summary>
    // @cdk.module renderbasic
    public class RingElement : OvalElement, IRenderingElement
    {
        /// <summary>
        /// Make a ring element centered on (x, y) with radius and color given.
        /// </summary>
        /// <param name="center">the coordinate of the ring center</param>
        /// <param name="radius">the radius of the circle</param>
        /// <param name="color">the color of the circle</param>
        public RingElement(Point center, double radius, Color color)
            : base(center, radius, false, color)
        { }

        public override void Accept(IRenderingVisitor v, Transform transform)
        {
            v.Visit(this, transform);
        }

        /// <inheritdoc/>
        public override void Accept(IRenderingVisitor v)
        {
            v.Visit(this);
        }
    }
}
