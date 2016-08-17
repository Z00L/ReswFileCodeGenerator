using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReswCodeGen.CustomTool;

namespace ReswCodeGen.Tests
{
    [TestClass]
    [DeploymentItem("Resources/Valid/Resources.resw")]
    public class CodeGeneratorFactoryTests
    {
		private const string ClassName = "C:\\Test\\Resources\\Strings.resw";
		private string _fileContent;
        
        [TestInitialize]
        public void Initialize()
        {
            _fileContent = File.ReadAllText("Resources.resw");
        }

        [TestMethod]
        public void CodeGeneratorFactoryReturnsValidInstance()
        {
            var target = new CodeGeneratorFactory();
            var actual = target.Create(ClassName, "TestApp", _fileContent);
            Assert.IsNotNull(actual);
        }
    }
}