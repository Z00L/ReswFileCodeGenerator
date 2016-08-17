using System.Collections.Generic;

namespace ReswCodeGen.CustomTool
{
	public interface IResourceParser
	{
		string ReswFileContents { get; }
		IEnumerable<ResourceItem> Parse();
	}
}