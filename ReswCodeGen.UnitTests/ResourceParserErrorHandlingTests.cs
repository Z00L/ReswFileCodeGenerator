using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReswCodeGen.CustomTool;

namespace ReswCodeGen.Tests
{
	[TestClass]
	[DeploymentItem("Resources/ResourcesWithoutValues.resw")]
	[DeploymentItem("Resources/ResourcesWithoutComments.resw")]
	public class ResourceParserErrorHandlingTests
	{
		[TestMethod]
		public void ParseHandlesMissingResourceItemValues()
		{
			var reswFileContents = File.ReadAllText("ResourcesWithoutValues.resw");
			var target = new ResourceParser(reswFileContents);
			var actual = target.Parse().ToList();

			Assert.IsNotNull(actual);
			CollectionAssert.AllItemsAreNotNull(actual);
		}

		[TestMethod]
		public void ParseWithMissingResourceItemValuesSetsValueNull()
		{
			var reswFileContents = File.ReadAllText("ResourcesWithoutValues.resw");
			var target = new ResourceParser(reswFileContents);
			var actual = target.Parse();

			foreach (var i in actual.Where(x => x != null))
				Assert.IsTrue(string.IsNullOrEmpty(i.Value));
		}

		[TestMethod]
		public void ParseHandlesMissingResourceItemComments()
		{
			var reswFileContents = File.ReadAllText("ResourcesWithoutComments.resw");
			var target = new ResourceParser(reswFileContents);
			var actual = target.Parse().ToList();

			Assert.IsNotNull(actual);
			CollectionAssert.AllItemsAreNotNull(actual);
		}

		[TestMethod]
		public void ParseWithMissingResourceItemCommentsSetsCommentsNull()
		{
			var reswFileContents = File.ReadAllText("ResourcesWithoutComments.resw");
			var target = new ResourceParser(reswFileContents);
			var actual = target.Parse();
			
			foreach (var item in actual.Where(x => x != null))
				Assert.IsTrue(string.IsNullOrEmpty(item.Comment));
		}
	}
}