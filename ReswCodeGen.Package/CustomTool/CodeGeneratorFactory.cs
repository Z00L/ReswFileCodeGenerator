using System.CodeDom.Compiler;
using System.Reflection;

namespace ReswCodeGen.CustomTool
{
	public class CodeGeneratorFactory
	{
		public ICodeGenerator Create(string className, string defaultNamespace, string inputFileContents, CodeDomProvider codeDomProvider = null, TypeAttributes? classAccessibility = null)
		{
			return new CodeDomCodeGenerator(new ResourceParser(inputFileContents), className, defaultNamespace, codeDomProvider, classAccessibility, VisualStudioHelper.GetVersion());
		}
	}
}