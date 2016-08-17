using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReswCodeGen.CustomTool;

namespace ReswCodeGen.Tests
{
	[TestClass]
	[DeploymentItem("Resources/Resources.resw")]
	public class CSharpCodeGeneratorTestsInternal
	{
		private const string FilePath = "Resources.resw";
		private ICodeGenerator _generator;
		private string _fileContent;
		private string _generatedCode;

		[TestInitialize]
		public void Initialize()
		{
			_fileContent = File.ReadAllText(FilePath);
			_generator = new CodeGeneratorFactory().Create(Path.GetFileNameWithoutExtension(FilePath), "TestApp", _fileContent, classAccessibility: TypeAttributes.NestedAssembly);
			_generatedCode = _generator.GenerateCode();
		}

		[TestMethod]
		public void GenerateCodeDoesNotReturnNull()
		{
			Assert.IsNotNull(_generatedCode);
		}

		[TestMethod]
		public void GeneratedCodeIsAnInternalClass()
		{
			Assert.IsTrue(_generatedCode.Contains("internal partial class"));
		}

		[TestMethod]
		public void GeneratedCodeContainsPropertiesDefinedInResources()
		{
			var resourceItems = _generator.ResourceParser.Parse();

			foreach (var i in resourceItems.Where(x => !x.Name.Contains(".")))
				Assert.IsTrue(_generatedCode.Contains("public static string " + i.Name));
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
			var className = Path.GetFileNameWithoutExtension(FilePath);
			Assert.IsTrue(_generatedCode.Contains("class " + className));
		}
	}
}