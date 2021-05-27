using Microsoft.VisualStudio.TestTools.UnitTesting;
using CompMs.CommonMVVM.Common;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompMs.CommonMVVM.Common.Tests
{
    [TestClass()]
    public class MappingReadOnlyObservableCollectionTests
    {
        [TestMethod()]
        public void MappedReadOnlyObservableCollectionTest() {
            var source = new ObservableCollection<int> { 1, 2, 3 };
            var mapCollection = source.ToMappedReadOnlyObservableCollection(x => x.ToString());

            CollectionAssert.AreEqual(source.Select(item => item.ToString()).ToList(), mapCollection);
        }

        [TestMethod()]
        public void AddItemsTest() {
            var source = new ObservableCollection<int> { 1, 2, 3 };
            var mapCollection = new MappedReadOnlyObservableCollection<int, string>(source, x => x.ToString());

            source.Add(4);
            source.Add(5);
            CollectionAssert.AreEqual(source.Select(item => item.ToString()).ToList(), mapCollection);
        }

        [TestMethod()]
        public void InsertItemsTest() {
            var source = new ObservableCollection<int> { 1, 2, 3, 4, 5 };
            var mapCollection = new MappedReadOnlyObservableCollection<int, string>(source, x => x.ToString());

            source.Insert(4, 6);
            source.Insert(2, 7);
            CollectionAssert.AreEqual(source.Select(item => item.ToString()).ToList(), mapCollection);
        }

        [TestMethod()]
        public void RemoveItemsTest() {
            var source = new ObservableCollection<int> { 1, 2, 3, 4, 5 };
            var mapCollection = new MappedReadOnlyObservableCollection<int, string>(source, x => x.ToString());

            source.Remove(4);
            source.Remove(2);
            CollectionAssert.AreEqual(source.Select(item => item.ToString()).ToList(), mapCollection);
        }

        [TestMethod()]
        public void ReplaceItemsTest() {
            var source = new ObservableCollection<int> { 1, 2, 3, 4, 5 };
            var mapCollection = new MappedReadOnlyObservableCollection<int, string>(source, x => x.ToString());

            source[2] = 6;
            source[0] = 7;
            CollectionAssert.AreEqual(source.Select(item => item.ToString()).ToList(), mapCollection);
        }

        [TestMethod()]
        public void ClearItemsTest() {
            var source = new ObservableCollection<int> { 1, 2, 3, 4, 5 };
            var mapCollection = new MappedReadOnlyObservableCollection<int, string>(source, x => x.ToString());

            source.Clear();
            CollectionAssert.AreEqual(source.Select(item => item.ToString()).ToList(), mapCollection);
        }

        [TestMethod()]
        public void MoveItemsTest() {
            var source = new ObservableCollection<int> { 1, 2, 3, 4, 5 };
            var mapCollection = new MappedReadOnlyObservableCollection<int, string>(source, x => x.ToString());

            source.Move(3, 1);
            source.Move(0, 3);
            CollectionAssert.AreEqual(source.Select(item => item.ToString()).ToList(), mapCollection);
        }
    }
}