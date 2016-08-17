namespace ReswCodeGen.CustomTool
{
	public abstract class CodeGenerator : ICodeGenerator
	{
		protected CodeGenerator(IResourceParser resourceParser, string defaultNamespace)
		{
			ResourceParser = resourceParser;
			Namespace = defaultNamespace;
		}

		public IResourceParser ResourceParser { get; }
		public string Namespace { get; }
		public abstract string GenerateCode();
	}
}