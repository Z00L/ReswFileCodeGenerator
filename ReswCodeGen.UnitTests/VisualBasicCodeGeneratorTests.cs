using System.IO;
using System.Linq;
using Microsoft.VisualBasic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReswCodeGen.CustomTool;

namespace ReswCodeGen.Tests
{
	[TestClass]
	[DeploymentItem("Resources/Resources.resw")]
	public class VisualBasicCodeGeneratorTests
	{
		private const string FilePath = "Resources.resw";
		private ICodeGenerator _generator;
		private string _fileContent;
		private string _generatedCode;

		[TestInitialize]
		public void Initialize()
		{
			_fileContent = File.ReadAllText(FilePath);
			_generator = new CodeGeneratorFactory().Create(Path.GetFileNameWithoutExtension(FilePath), "TestApp", _fileContent, new VBCodeProvider());
			_generatedCode = _generator.GenerateCode();
		}

		[TestMethod]
		public void GenerateCodeDoesNotReturnNull()
		{
			Assert.IsNotNull(_generatedCode);
		}

		[TestMethod]
		public void GeneratedCodeContainsPropertiesDefinedInResources()
		{
			var resourceItems = _generator.ResourceParser.Parse();

			foreach (var i in resourceItems.Where(x => !x.Name.Contains(".")))
				Assert.IsTrue(_generatedCode.Contains("Public Shared ReadOnly Property " + i.Name + "() As String"));
		}

		[TestMethod]
		public void GeneratedCodePropertiesContainsCommentsSimilarToValuesDefinedInResources()
		{
			var resourceItems = _generator.ResourceParser.Parse().ToList();
			foreach (var i in resourceItems.Where(x => !string.IsNullOrWhiteSpace(x.Value)))
				Assert.IsTrue(_generatedCode.Contains(i.Value));
			foreach (var i in resourceItems.Where(x => !string.IsNullOrWhiteSpace(x.Comment)))
				Assert.IsTrue(_generatedCode.Contains(i.Comment));
		}

		[TestMethod]
		public void ClassNameEqualsFileNameWithoutExtension()
		{
			Assert.IsTrue(_generatedCode.Contains("Class Resources"));
		}
	}
}