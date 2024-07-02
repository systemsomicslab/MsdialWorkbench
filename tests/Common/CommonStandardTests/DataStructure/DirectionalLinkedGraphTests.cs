using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace CompMs.Common.DataStructure.Tests
{
    [TestClass()]
    public class DirectionalLinkedGraphTests
    {

        [TestMethod()]
        public void ContainsTest() {
            var tree = new DirectionalLinkedGraph<string>();
            tree.Add("child1", new[] { "grandchild1", "grandchild2", });
            tree.Add("child2", new[] { "grandchild3", "grandchild4", "grandchild5", });
            tree.Add("parent", new[] { "child1", "child2", });

            Assert.IsTrue(tree.Contains("parent"));
            Assert.IsTrue(tree.Contains("child1"));
            Assert.IsFalse(tree.Contains("child3"));
        }

        [TestMethod()]
        public void AddTest() {
            var tree = new DirectionalLinkedGraph<string>();
            tree.Add("child1", new[] { "grandchild1", "grandchild2", });
            tree.Add("child2", new[] { "grandchild3", "grandchild4", "grandchild5", });
            tree.Add("parent", new[] { "child1", "child2", });
        }

        [TestMethod()]
        public void GetChildrenTest() {
            var tree = new DirectionalLinkedGraph<string>();
            tree.Add("child1", new[] { "grandchild1", "grandchild2", });
            tree.Add("child2", new[] { "grandchild3", "grandchild4", "grandchild5", });
            tree.Add("child3", new[] { "grandchild6", });
            tree.Add("parent1", new[] { "child1", "child2", });
            tree.Add("parent2", new[] { "child2", "child3", });

            CollectionAssert.AreEqual(new[] { "child1", "child2", }, tree.GetChildren("parent1"));
            CollectionAssert.AreEqual(new[] { "child2", "child3", }, tree.GetChildren("parent2"));
            CollectionAssert.AreEqual(new[] { "grandchild1", "grandchild2", }, tree.GetChildren("child1"));
            CollectionAssert.AreEqual(new[] { "grandchild3", "grandchild4", "grandchild5", }, tree.GetChildren("child2"));
            CollectionAssert.AreEqual(new[] { "grandchild6", }, tree.GetChildren("child3"));
        }

        [TestMethod()]
        public void GetDescendantsTest() {
            var tree = new DirectionalLinkedGraph<string>();
            tree.Add("child1", new[] { "grandchild1", "grandchild2", });
            tree.Add("child2", new[] { "grandchild3", "grandchild4", "grandchild5", });
            tree.Add("child3", new[] { "grandchild6", });
            tree.Add("parent1", new[] { "child1", "child2", });
            tree.Add("parent2", new[] { "child2", "child3", });

            CollectionAssert.AreEqual(new[] { "child1", "grandchild1", "grandchild2", "child2", "grandchild3", "grandchild4", "grandchild5", }, tree.GetDescendants("parent1"));
            CollectionAssert.AreEqual(new[] { "child2", "grandchild3", "grandchild4", "grandchild5", "child3", "grandchild6", }, tree.GetDescendants("parent2"));
            CollectionAssert.AreEqual(new[] { "grandchild1", "grandchild2", }, tree.GetDescendants("child1"));
            CollectionAssert.AreEqual(new[] { "grandchild3", "grandchild4", "grandchild5", }, tree.GetDescendants("child2"));
            CollectionAssert.AreEqual(new[] { "grandchild6", }, tree.GetDescendants("child3"));
        }

        [TestMethod()]
        public void DirectionalLinkedTreeConstructorTest() {
            var tree = new DirectionalLinkedGraph<string>(new LengthComparer());
            tree.Add("bb", new[] { "ddd", "eee", });
            tree.Add("a", new[] { "cc", });

            CollectionAssert.AreEqual(new[] { "bb", }, tree.GetChildren("a"));
            CollectionAssert.AreEqual(new[] { "ddd", }, tree.GetChildren("cc"));
        }

        class LengthComparer : IEqualityComparer<string>
        {
#if NETSTANDARD || NETFRAMEWORK
            public bool Equals(string x, string y) {
#else
            public bool Equals([AllowNull] string x, [AllowNull] string y) {
#endif
                if (x is null && y is null) return true;
                if (x is null || y is null) return false;
                return x.Length == y.Length;
            }

#if NETSTANDARD || NETFRAMEWORK
            public int GetHashCode(string obj) {
#else
            public int GetHashCode([DisallowNull] string obj) {
#endif
                return obj.Length.GetHashCode();
            }
        }
    }
}