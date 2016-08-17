namespace ReswCodeGen.CustomTool
{
	public interface ICodeGenerator
	{
		IResourceParser ResourceParser { get; }
		string Namespace { get; }
		string GenerateCode();
	}
}