/* Copyright (C) 2003-2007  Miguel Howard <miguel@jmol.org>
 *
 * Contact: cdk-devel@lists.sf.net
 *
 *  This library is free software; you can redistribute it and/or
 *  modify it under the terms of the GNU Lesser General Public
 *  License as published by the Free Software Foundation; either
 *  version 2.1 of the License, or (at your option) any later version.
 *
 *  This library is distributed in the hope that it will be useful,
 *  but WITHOUT Any WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 *  Lesser General Public License for more details.
 *
 *  You should have received a copy of the GNU Lesser General Public
 *  License along with this library; if not, write to the Free Software
 *  Foundation, 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 USA.
 */

using System;
using System.Collections;
using System.Collections.Generic;

namespace NCDK.Graphs.Rebond
{
    /// <summary>
    ///  BSP-Tree stands for Binary Space Partitioning Tree.
    ///  The tree partitions n-dimensional space (in our case 3) into little
    ///  boxes, facilitating searches for things which are *nearby*.
    ///  For some useful background info, search the web for "bsp tree faq".
    ///  Our application is somewhat simpler because we are storing points instead
    ///  of polygons.
    /// </summary>
    /// <example>
    /// We are working with three dimensions. For the purposes of the Bspt code
    ///  these dimensions are stored as 0, 1, or 2. Each node of the tree splits
    ///  along the next dimension, wrapping around to 0.
    /// <code>
    ///    mySplitDimension = (parentSplitDimension + 1) % 3;
    /// </code>
    ///  A split value is stored in the node. Values which are ÅÖ splitValue are
    ///  stored down the left branch. Values which are ÅÜ splitValue are stored
    ///  down the right branch. When this happens, the search must proceed down
    ///  both branches.
    ///  Planar and crystalline substructures can generate values which are == along
    ///  one dimension.
    /// </example>
    /// <remarks>
    /// <para>
    /// To get a good picture in your head, first think about it in one dimension,
    ///  points on a number line. The tree just partitions the points.
    ///  Now think about 2 dimensions. The first node of the tree splits the plane
    ///  into two rectangles along the x dimension. The second level of the tree
    ///  splits the subplanes (independently) along the y dimension into smaller
    ///  rectangles. The third level splits along the x dimension.
    ///  In three dimensions, we are doing the same thing, only working with
    ///  3-d boxes.
    ///  </para>
    /// Three enumerators are provided
    /// <list type="bullet">
    ///    <item>EnumNear(Bspt.Tuple center, double distance)<br/>
    ///      returns all the points contained in of all the boxes which are within
    ///      distance from the center.</item>
    ///    <item>EnumSphere(Bspt.Tuple center, double distance)<br/>
    ///      returns all the points which are contained within the sphere (inclusive)
    ///      defined by center + distance</item>
    ///    <item>EnumHemiSphere(Bspt.Tuple center, double distance)<br/>
    ///      same as sphere, but only the points which are greater along the
    ///      x dimension</item>
    /// </list>
    /// </remarks>
    // @author  Miguel Howard
    // @cdk.created 2003-05
    // @cdk.module  standard
    // @cdk.keyword rebonding
    // @cdk.keyword Binary Space Partitioning Tree
    // @cdk.keyword join-the-dots
    public sealed class Bspt<T> : IEnumerable<T> where T: ITuple
    {
        private const int LeafCount = 4;
        private const int StackDepth = 64;  // this corresponds to the max height of the tree
        int dimMax;
        IElement eleRoot;

        // static double Distance(int dim, Tuple t1, Tuple t2) { return
        // Math.Sqrt(Distance2(dim, t1, t2)); } static double Distance2(int dim,
        // Tuple t1, Tuple t2) { double distance2 = 0.0; while (--dim >= 0) { double
        // distT = t1.GetDimValue(dim) - t2.GetDimValue(dim); distance2 +=
        // distT*distT; } return distance2; }
        public Bspt(int dimMax)
        {
            this.dimMax = dimMax;
            this.eleRoot = new Leaf(this);
        }

        public void AddTuple(T tuple)
        {
            if (!eleRoot.AddTuple(tuple))
            {
                eleRoot = new Node(this, 0, dimMax, (Leaf)eleRoot);
                if (!eleRoot.AddTuple(tuple)) throw new Exception("Bspt.AddTuple() failed");
            }
        }

        public override string ToString()
        {
            return eleRoot.ToString();
        }

        internal void Dump()
        {
            eleRoot.Dump(0);
        }

        public IEnumerator<T> GetEnumerator()
        {
            Node[] stack = new Node[StackDepth];
            int sp = 0;
            IElement ele = eleRoot;
            while (ele is Node)
            {
                Node node = (Node)ele;
                if (sp == StackDepth) throw new ApplicationException("Bspt.EnumerateAll tree stack overflow");
                stack[sp++] = node;
                ele = node.eleLE;
            }
            Leaf leaf = (Leaf)ele;
            int i = 0;

            while ((i < leaf.count) || (sp > 0))
            {
                if (i == leaf.count)
                {
                    //        Debug.WriteLine($"-->{stack[sp-1].splitValue}");
                    ele = stack[--sp].eleGE;
                    while (ele is Node)
                    {
                        Node node = (Node)ele;
                        stack[sp++] = node;
                        ele = node.eleLE;
                    }
                    leaf = (Leaf)ele;
                    i = 0;
                }
                yield return leaf.tuples[i++];
            }
            yield break;
        }

        public IEnumerable<T> EnumerateNears(ITuple center, double distance)
        {
            Node[] stack = new Node[StackDepth];
            int sp = 0;
            IElement ele = eleRoot;
            while (ele is Node)
            {
                Node node = (Node)ele;
                if (center.GetDimValue(node.dim) - distance <= node.splitValue)
                {
                    if (sp == StackDepth) throw new ApplicationException("Bspt.EnumerateNear tree stack overflow");
                    stack[sp++] = node;
                    ele = node.eleLE;
                }
                else
                {
                    ele = node.eleGE;
                }
            }
            Leaf leaf = (Leaf)ele;
            int i = 0;

            while (true)
            {
                if (i < leaf.count)
                {
                    yield return leaf.tuples[i++];
                    continue;
                }
                if (sp == 0)
                    yield break;

                ele = stack[--sp];
                while (ele is Node)
                {
                    Node node = (Node)ele;
                    if (center.GetDimValue(node.dim) + distance < node.splitValue)
                    {
                        if (sp == 0) yield break;
                        ele = stack[--sp];
                    }
                    else
                    {
                        ele = node.eleGE;
                        while (ele is Node)
                        {
                            Node nodeLeft = (Node)ele;
                            stack[sp++] = nodeLeft;
                            ele = nodeLeft.eleLE;
                        }
                    }
                }
                leaf = (Leaf)ele;
                i = 0;
                yield return leaf.tuples[i++];
            }
        }

        public IEnumerable<T> EnumerateSphere(ITuple center, double distance)
        {
            return EnumerateSphere(center, distance, false);
        }

        public IEnumerable<T> EnumerateHemiSphere(ITuple center, double distance)
        {
            return EnumerateSphere(center, distance, true);
        }

        private IEnumerable<T> EnumerateSphere(ITuple center, double distance, bool tHemisphere) 
        {
            Node[] stack;
            int sp;
            int i;
            Leaf leaf;

            double distance2 = distance * distance;
            double[] centerValues = new double[dimMax];
            for (int dim = dimMax; --dim >= 0;)
                centerValues[dim] = center.GetDimValue(dim);
            stack = new Node[StackDepth];
            sp = 0;
            IElement ele = eleRoot;
            while (ele is Node)
            {
                Node node = (Node)ele;
                if (center.GetDimValue(node.dim) - distance <= node.splitValue)
                {
                    if (sp == StackDepth) throw new ApplicationException("Bspt.EnumerateSphere tree stack overflow");
                    stack[sp++] = node;
                    ele = node.eleLE;
                }
                else
                {
                    ele = node.eleGE;
                }
            }
            leaf = (Leaf)ele;
            i = 0;

            while (true)
            {
                for (; i < leaf.count; ++i)
                {
                    var lt = leaf.tuples[i];
                    var distT = lt.GetDimValue(0) - centerValues[0];
                    if (tHemisphere && distT < 0)
                        goto Break_Widthin;
                    double dist2 = distT * distT;
                    if (dist2 > distance2)
                        goto Break_Widthin;
                    for (int dim = dimMax; --dim > 0;)
                    {
                        distT = lt.GetDimValue(dim) - centerValues[dim];
                        dist2 += distT * distT;
                        if (dist2 > distance2)
                            goto Break_Widthin;
                    }
                    var ret = leaf.tuples[i++];
                    ret.Distance2 = dist2;
                    yield return ret;
                    goto continue_while;
                Break_Widthin:
                    ;
                }
                if (sp == 0) yield break;
                ele = stack[--sp];
                while (ele is Node)
                {
                    Node node = (Node)ele;
                    if (center.GetDimValue(node.dim) + distance < node.splitValue)
                    {
                        if (sp == 0) yield break;
                        ele = stack[--sp];
                    }
                    else
                    {
                        ele = node.eleGE;
                        while (ele is Node)
                        {
                            Node nodeLeft = (Node)ele;
                            stack[sp++] = nodeLeft;
                            ele = nodeLeft.eleLE;
                        }
                    }
                }
                leaf = (Leaf)ele;
                i = 0;
            continue_while:
                ;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        interface IElement 
        {
            bool AddTuple(T tuple);

            void Dump(int level);

            bool IsLeafWithSpace();
        }

        class Node : IElement 
        {
            public IElement eleLE;
            public int dim;
            public int dimMax;
            public double splitValue;
            public IElement eleGE;

            private readonly Bspt<T> parent;

            public Node(Bspt<T> parent, int dim, int dimMax, Leaf leafLE)
            {
                this.parent = parent;

                this.eleLE = leafLE;
                this.dim = dim;
                this.dimMax = dimMax;
                this.splitValue = leafLE.GetSplitValue(dim);
                this.eleGE = new Leaf(parent, leafLE, dim, splitValue);
            }


            public bool AddTuple(T tuple)
            {
                if (tuple.GetDimValue(dim) < splitValue)
                {
                    if (eleLE.AddTuple(tuple)) return true;
                    eleLE = new Node(parent, (dim + 1) % dimMax, dimMax, (Leaf)eleLE);
                    return eleLE.AddTuple(tuple);
                }
                if (tuple.GetDimValue(dim) > splitValue)
                {
                    if (eleGE.AddTuple(tuple)) return true;
                    eleGE = new Node(parent, (dim + 1) % dimMax, dimMax, (Leaf)eleGE);
                    return eleGE.AddTuple(tuple);
                }
                if (eleLE.IsLeafWithSpace())
                    eleLE.AddTuple(tuple);
                else if (eleGE.IsLeafWithSpace())
                    eleGE.AddTuple(tuple);
                else if (eleLE is Node)
                    eleLE.AddTuple(tuple);
                else if (eleGE is Node)
                    eleGE.AddTuple(tuple);
                else
                {
                    eleLE = new Node(parent, (dim + 1) % dimMax, dimMax, (Leaf)eleLE);
                    return eleLE.AddTuple(tuple);
                }
                return true;
            }

            public override string ToString()
            {
                return eleLE.ToString() + dim + ":" + splitValue + "\n" + eleGE.ToString();
            }

            public void Dump(int level)
            {
                Console.Out.WriteLine("");
                eleLE.Dump(level + 1);
                for (int i = 0; i < level; ++i)
                    Console.Out.Write("-");
                Console.Out.WriteLine(">" + splitValue);
                eleGE.Dump(level + 1);
            }


            public bool IsLeafWithSpace()
            {
                return false;
            }
        }

        class Leaf : IElement
        {
            public int count;
            public T[] tuples;
            private Bspt<T> parent;

            public Leaf(Bspt<T> parent)
            {
                this.parent = parent;
                count = 0;
                tuples = new T[LeafCount];
            }

            public Leaf(Bspt<T> parent, Leaf leaf, int dim, double splitValue)
                  : this(parent)
            {
                for (int i = LeafCount; --i >= 0;)
                {
                    T tuple = leaf.tuples[i];
                    double value = tuple.GetDimValue(dim);
                    if (value > splitValue || (value == splitValue && ((i & 1) == 1)))
                    {
                        leaf.tuples[i] = default(T);
                        tuples[count++] = tuple;
                    }
                }
                int dest = 0;
                for (int src = 0; src < LeafCount; ++src)
                    if (leaf.tuples[src] != null) leaf.tuples[dest++] = leaf.tuples[src];
                leaf.count = dest;
                if (count == 0) tuples[LeafCount] = default(T); // explode
            }

            public double GetSplitValue(int dim)
            {
                if (count != LeafCount) tuples[LeafCount] = default(T);
                return (tuples[0].GetDimValue(dim) + tuples[LeafCount - 1].GetDimValue(dim)) / 2;
            }


            public override string ToString()
            {
                return "leaf:" + count + "\n";
            }


            public bool AddTuple(T tuple)
            {
                if (count == LeafCount) return false;
                tuples[count++] = tuple;
                return true;
            }


            public void Dump(int level)
            {
                for (int i = 0; i < count; ++i)
                {
                    T t = tuples[i];
                    for (int j = 0; j < level; ++j)
                        Console.Out.Write(".");
                    for (int dim = 0; dim < parent.dimMax - 1; ++dim)
                        Console.Out.Write("" + t.GetDimValue(dim) + ",");
                    Console.Out.WriteLine("" + t.GetDimValue(parent.dimMax - 1));
                }
            }

            public bool IsLeafWithSpace()
            {
                return count < LeafCount;
            }
        }
    }

    public interface ITuple
    {
        double GetDimValue(int dimension);
        double Distance2 { get; set; } // the dist squared of a found Element;
    }
}
