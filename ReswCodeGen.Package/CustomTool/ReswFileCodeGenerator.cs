using System;
using System.CodeDom.Compiler;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.VisualStudio.Shell.Interop;

namespace ReswCodeGen.CustomTool
{
	[ComVisible(true)]
	public abstract class ReswFileCodeGenerator : IVsSingleFileGenerator
	{
		private readonly CodeDomProvider codeDomProvider;
		private readonly TypeAttributes? classAccessibility;

		protected ReswFileCodeGenerator(CodeDomProvider codeDomProvider, TypeAttributes? classAccessibility = null)
		{
			this.codeDomProvider = codeDomProvider;
			this.classAccessibility = classAccessibility;
		}

		public abstract int DefaultExtension(out string defaultExtension);

		public virtual int Generate(string wszInputFilePath,
									string bstrInputFileContents,
									string wszDefaultNamespace,
									IntPtr[] rgbOutputFileContents,
									out uint pcbOutput,
									IVsGeneratorProgress pGenerateProgress)
		{
			try
			{
				var className = ClassNameExtractor.GetClassName(wszInputFilePath);
				var factory = new CodeGeneratorFactory();
				var codeGenerator = factory.Create(className, wszDefaultNamespace, bstrInputFileContents, codeDomProvider, classAccessibility);
				var code = codeGenerator.GenerateCode();

				rgbOutputFileContents[0] = code.ConvertToIntPtr(out pcbOutput);
			}
			catch (Exception e)
			{
				MessageBox.Show(e.Message, "Unable to generate code");
				throw;
			}

			return 0;
		}
	}
}