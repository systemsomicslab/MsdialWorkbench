using Microsoft.VisualStudio.TestTools.UnitTesting;
using CompMs.CommonMVVM.Common;
using System;
using System.Linq;
using System.Collections.ObjectModel;

namespace CompMs.CommonMVVM.Common.Tests
{
    [TestClass()]
    public class MappedObservableCollectionTests
    {
        [TestMethod()]
        public void MappedReadOnlyObservableCollectionTest() {
            var source = new ObservableCollection<int> { 1, 2, 3 };
            var dest = source.ToMappedObservableCollection(x => (double)x, y => (int)y);

            Console.WriteLine("source: " + string.Join(",", source));
            Console.WriteLine("dest: " + string.Join(",", dest));
            CollectionAssert.AreEqual(source.Select(item => (double)item).ToList(), dest);
        }

        [TestMethod()]
        public void AddItemsTest() {
            var source = new ObservableCollection<int> { 1, 2, 3 };
            var dest = source.ToMappedObservableCollection(x => (double)x, y => (int)y);

            source.Add(4);
            source.Add(5);
            Console.WriteLine("source: " + string.Join(",", source));
            Console.WriteLine("dest: " + string.Join(",", dest));
            CollectionAssert.AreEqual(source.Select(item => (double)item).ToList(), dest);

            dest.Add(6.0);
            dest.Add(7.0);
            Console.WriteLine("source: " + string.Join(",", source));
            Console.WriteLine("dest: " + string.Join(",", dest));
            CollectionAssert.AreEqual(source.Select(item => (double)item).ToList(), dest);
        }

        [TestMethod()]
        public void InsertItemsTest() {
            var source = new ObservableCollection<int> { 1, 2, 3, 4, 5 };
            var dest = source.ToMappedObservableCollection(x => (double)x, y => (int)y);

            source.Insert(4, 6);
            source.Insert(2, 7);
            Console.WriteLine("source: " + string.Join(",", source));
            Console.WriteLine("dest: " + string.Join(",", dest));
            CollectionAssert.AreEqual(source.Select(item => (double)item).ToList(), dest);

            dest.Insert(3, 8d);
            dest.Insert(0, 9d);
            Console.WriteLine("source: " + string.Join(",", source));
            Console.WriteLine("dest: " + string.Join(",", dest));
            CollectionAssert.AreEqual(source.Select(item => (double)item).ToList(), dest);
        }

        [TestMethod()]
        public void RemoveItemsTest() {
            var source = new ObservableCollection<int> { 1, 2, 3, 4, 5 };
            var dest = source.ToMappedObservableCollection(x => (double)x, y => (int)y);

            source.RemoveAt(4);
            source.Remove(2);
            Console.WriteLine("source: " + string.Join(",", source));
            Console.WriteLine("dest: " + string.Join(",", dest));
            CollectionAssert.AreEqual(source.Select(item => (double)item).ToList(), dest);

            dest.RemoveAt(0);
            dest.Remove(3d);
            Console.WriteLine("source: " + string.Join(",", source));
            Console.WriteLine("dest: " + string.Join(",", dest));
            CollectionAssert.AreEqual(source.Select(item => (double)item).ToList(), dest);
        }

        [TestMethod()]
        public void ReplaceItemsTest() {
            var source = new ObservableCollection<int> { 1, 2, 3, 4, 5 };
            var dest = source.ToMappedObservableCollection(x => (double)x, y => (int)y);

            source[2] = 6;
            source[0] = 7;
            Console.WriteLine("source: " + string.Join(",", source));
            Console.WriteLine("dest: " + string.Join(",", dest));
            CollectionAssert.AreEqual(source.Select(item => (double)item).ToList(), dest);

            dest[3] = 8;
            dest[4] = 9;
            Console.WriteLine("source: " + string.Join(",", source));
            Console.WriteLine("dest: " + string.Join(",", dest));
            CollectionAssert.AreEqual(source.Select(item => (double)item).ToList(), dest);
        }

        [TestMethod()]
        public void ClearItemsTest() {
            var source = new ObservableCollection<int> { 1, 2, 3, 4, 5 };
            var dest = source.ToMappedObservableCollection(x => (double)x, y => (int)y);

            source.Clear();
            Console.WriteLine("source: " + string.Join(",", source));
            Console.WriteLine("dest: " + string.Join(",", dest));
            CollectionAssert.AreEqual(source.Select(item => (double)item).ToList(), dest);

            source = new ObservableCollection<int> { 1, 2, 3, 4, 5 };
            dest = source.ToMappedObservableCollection(x => (double)x, y => (int)y);

            dest.Clear();
            Console.WriteLine("source: " + string.Join(",", source));
            Console.WriteLine("dest: " + string.Join(",", dest));
            CollectionAssert.AreEqual(source.Select(item => (double)item).ToList(), dest);
        }

        [TestMethod()]
        public void MoveItemsTest() {
            var source = new ObservableCollection<int> { 1, 2, 3, 4, 5 };
            var dest = source.ToMappedObservableCollection(x => (double)x, y => (int)y);

            source.Move(3, 1);
            source.Move(0, 4);
            Console.WriteLine("source: " + string.Join(",", source));
            Console.WriteLine("dest: " + string.Join(",", dest));
            CollectionAssert.AreEqual(source.Select(item => (double)item).ToList(), dest);

            dest.Move(2, 4);
            dest.Move(3, 1);
            Console.WriteLine("source: " + string.Join(",", source));
            Console.WriteLine("dest: " + string.Join(",", dest));
            CollectionAssert.AreEqual(source.Select(item => (double)item).ToList(), dest);
        }
    }
}