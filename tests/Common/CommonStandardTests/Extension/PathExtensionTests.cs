using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;

namespace CompMs.Common.Extension.Tests
{
    [TestClass()]
    public class PathExtensionTests
    {
        [TestMethod()]
        public void GetRelativePathTest() {
            var basepath = @"C:\abc\def\";
            var targetpath = @"C:\abc\def\ghi\jkl.mn";
            var expected = @"ghi\jkl.mn";
            var actual = PathExtension.GetRelativePath(basepath, targetpath);
            Assert.AreEqual(expected, actual);
            Assert.AreEqual(targetpath, Path.GetFullPath(Path.Combine(basepath, actual)));
        }
    }
}