using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReswCodeGen.CustomTool;

namespace ReswCodeGen.Package.IntegrationTests
{
	[TestClass]
	public class VisualStudioHelperTests
	{
		[TestMethod]
		public void GetVersionTests()
		{
			var actual = VisualStudioHelper.GetVersion();
			Assert.AreNotEqual(VisualStudioVersion.Unknown, actual);
		}
	}
}