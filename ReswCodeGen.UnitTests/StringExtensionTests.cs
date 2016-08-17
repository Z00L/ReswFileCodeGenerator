using System;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReswCodeGen.CustomTool;

namespace ReswCodeGen.Tests
{
    [TestClass]
    [DeploymentItem("Resources/Resources.resw")]
    public class StringExtensionTests
    {
        const string TEXT = "test";

        [TestMethod]
        public void ConvertToIntPtrDoesNotReturnZero()
        {
            uint len;
            var ptr = TEXT.ConvertToIntPtr(out len);
            Assert.AreNotEqual(IntPtr.Zero, ptr);
        }

        [TestMethod]
        public void ConvertToIntPtrReturnsStringLengthAsParameter()
        {
            uint len;
            TEXT.ConvertToIntPtr(out len);
            Assert.AreNotEqual(TEXT.Length, len);
        }

        [TestMethod]
        public void ConvertToIntPtrConvertsCorrectString()
        {
            uint len;
            var ptr = TEXT.ConvertToIntPtr(out len);
            var str = Marshal.PtrToStringAnsi(ptr, (int) len);
            Assert.AreEqual(TEXT, str);
        }
    }
}