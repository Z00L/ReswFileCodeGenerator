using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VsSDK.UnitTestLibrary;

namespace ReswCodeGen.Package.UnitTests
{
	[TestClass]
	public class PackageTest
	{
		[TestMethod]
		public void CreateInstance()
		{
			var package = new VisualStudio2012Package();
		}

		[TestMethod]
		public void IsIVsPackage()
		{
			var package = new VisualStudio2012Package();
			Assert.IsNotNull(package as IVsPackage, "The object does not implement IVsPackage");
		}

		[TestMethod]
		public void SetSite()
		{
			// Create the package
			var package = new VisualStudio2012Package() as IVsPackage;
			Assert.IsNotNull(package, "The object does not implement IVsPackage");

			// Create a basic service provider
			var serviceProvider = OleServiceProvider.CreateOleServiceProviderWithBasicServices();

			// Site the package
			Assert.AreEqual(0, package.SetSite(serviceProvider), "SetSite did not return S_OK");

			// Unsite the package
			Assert.AreEqual(0, package.SetSite(null), "SetSite(null) did not return S_OK");
		}
	}
}