using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Rfx.Riken.OsakaUniv;

namespace Msdial.StatisticsTests
{
    [TestClass]
    public class ClusteringWard2DistanceTest
    {
        [TestMethod]
        public void ValidCaseTest1()
        {
            var datamatrix = new double[,]
            {
                {0.28930059, 0.38002444},
                {0.36180131, 0.19259621},
                {0.14719256, 0.15078414},
                {0.45727788, 0.30089984},
                {0.182489  , 0.87303251},
                {0.32148578, 0.88993592},
                {0.01473583, 0.62397616},
                {0.02729128, 0.38599099},
                {0.10430449, 0.67930607},
                {0.55204816, 0.12145724}
            };
            var tree = StatisticsMathematics.ClusteringWard2Distance(
                datamatrix,
                StatisticsMathematics.CalculateEuclideanDistance
                );
            var heights = new int[tree.Count];
            tree.PreOrder(e =>
                heights[e.To] = heights[e.From] + 1
            );
            Console.WriteLine(tree.Root);
            tree.PreOrder(e =>
                Console.WriteLine(
                    new string('\t', heights[e.To])
                    + e.To.ToString()
                    )
            );

            var clusters = new HashSet<int>[tree.Count];
            tree.PostOrder(e =>
            {
                if (tree.Leaves.Contains(e.To))
                {
                    clusters[e.To] = new HashSet<int> { e.To };
                }
                else
                {
                    clusters[e.To] = tree[e.To].Select(e_ =>
                            clusters[e_.To]
                        ).Aggregate<IEnumerable<int>>((acc, s) =>
                            acc.Union(s)
                        ).ToHashSet();
                }
            });
            clusters[tree.Root] = tree[tree.Root].Select(
                e_ => clusters[e_.To]
                ).Aggregate<IEnumerable<int>>(
                (acc, s) => acc.Union(s)
                ).ToHashSet();

            var expected = new HashSet<int>[]
            {
                new HashSet<int>{ 0 },
                new HashSet<int>{ 1 },
                new HashSet<int>{ 2 },
                new HashSet<int>{ 3 },
                new HashSet<int>{ 4 },
                new HashSet<int>{ 5 },
                new HashSet<int>{ 6 },
                new HashSet<int>{ 7 },
                new HashSet<int>{ 8 },
                new HashSet<int>{ 9 },
                new HashSet<int>{ 1, 3 },
                new HashSet<int>{ 0, 1, 3 },
                new HashSet<int>{ 0, 1, 3, 9 },
                new HashSet<int>{ 2, 7 },
                new HashSet<int>{ 0, 1, 2, 3, 7, 9 },
                new HashSet<int>{ 4, 5 },
                new HashSet<int>{ 6, 8 },
                new HashSet<int>{ 4, 5, 6, 8 },
                new HashSet<int>{ 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 },
            };

            Assert.IsTrue(
                clusters.All(cluster =>
                    expected.Any(e => e.SetEquals(cluster))
                    )
                );
            // Assert.Fail("This test is not implemented.");
        }

        [TestMethod]
        public void ValidCaseTest2()
        {
            var datamatrix = new double[,]
            {
                { 0.12188640, 0.4157666, 0.3245484, 0.8071808 },
                { 0.33874629, 0.5200476, 0.6615696, 0.4407271 },
                { 0.96822653, 0.3111284, 0.4520809, 0.6706221 },
                { 0.08066689, 0.9169908, 0.1892961, 0.8114488 },
                { 0.65530505, 0.9568018, 0.4718157, 0.9970233 },
                { 0.53586964, 0.1804955, 0.1772223, 0.1360114 },
                { 0.59519613, 0.8259075, 0.2502059, 0.5033505 },
                { 0.31393497, 0.1870488, 0.7576250, 0.1830884 },
                { 0.28299501, 0.1431359, 0.2397246, 0.7369075 },
                { 0.81593436, 0.6609351, 0.2512783, 0.9234077 }
            };
            var tree = StatisticsMathematics.ClusteringWard2Distance(
                datamatrix,
                StatisticsMathematics.CalculateEuclideanDistance
                );
            var heights = new int[tree.Count];
            tree.PreOrder(e =>
                heights[e.To] = heights[e.From] + 1
            );
            Console.WriteLine(tree.Root);
            tree.PreOrder(e =>
                Console.WriteLine(
                    new string('\t', heights[e.To])
                    + e.To.ToString()
                    )
            );

            var clusters = new HashSet<int>[tree.Count];
            tree.PostOrder(e =>
            {
                if (tree.Leaves.Contains(e.To))
                {
                    clusters[e.To] = new HashSet<int> { e.To };
                }
                else
                {
                    clusters[e.To] = tree[e.To].Select(e_ =>
                            clusters[e_.To]
                        ).Aggregate<IEnumerable<int>>((acc, s) =>
                            acc.Union(s)
                        ).ToHashSet();
                }
            });
            clusters[tree.Root] = tree[tree.Root].Select(
                e_ => clusters[e_.To]
                ).Aggregate<IEnumerable<int>>(
                (acc, s) => acc.Union(s)
                ).ToHashSet();

            var expected = new HashSet<int>[]
            {
                new HashSet<int>{ 0 },
                new HashSet<int>{ 1 },
                new HashSet<int>{ 2 },
                new HashSet<int>{ 3 },
                new HashSet<int>{ 4 },
                new HashSet<int>{ 5 },
                new HashSet<int>{ 6 },
                new HashSet<int>{ 7 },
                new HashSet<int>{ 8 },
                new HashSet<int>{ 9 },
                new HashSet<int>{ 0, 8 },
                new HashSet<int>{ 0, 3, 8 },
                new HashSet<int>{ 1, 7 },
                new HashSet<int>{ 1, 5, 7 },
                new HashSet<int>{ 0, 1, 3, 5, 7, 8 },
                new HashSet<int>{ 4, 9 },
                new HashSet<int>{ 4, 6, 9 },
                new HashSet<int>{ 2, 4, 6, 9 },
                new HashSet<int>{ 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 },
            };

            Assert.IsTrue(
                clusters.All(cluster =>
                    expected.Any(e => e.SetEquals(cluster))
                    )
                );
        }
    }
}
