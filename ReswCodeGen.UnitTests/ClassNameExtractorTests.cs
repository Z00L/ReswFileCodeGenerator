using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReswCodeGen.CustomTool;

namespace ReswCodeGen.Tests
{
    [TestClass]
    [DeploymentItem("Resources/Resources.resw")]
    public class ClassNameExtractorTests
    {
        private const string FileName = "Resources.resw";

        [TestMethod]
        public void DoesNotReturnNull()
        {
            var actual = ClassNameExtractor.GetClassName(FileName);
            Assert.IsNotNull(actual);
        }

        [TestMethod]
        public void ReturnsFileNameWithoutExtension()
        {
			var actual = ClassNameExtractor.GetClassName(FileName);
            Assert.AreEqual("Resources", actual);
        }

        [TestMethod]
        [ExpectedException(typeof(FileNotFoundException))]
        public void ThrowsFileNotFoundException()
        {
            ClassNameExtractor.GetClassName("C:\\Test\\Resources\\Strings.resw");
        }
    }
}