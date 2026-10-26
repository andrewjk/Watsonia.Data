using System;
using System.Globalization;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Watsonia.QueryBuilder.Tests
{
	[TestClass]
	public static class TestSetup
	{
		[AssemblyInitialize]
		public static void AssemblyInitialize(TestContext context)
		{
			// Used by CI to run the suite under a non-English culture
			var culture = Environment.GetEnvironmentVariable("WATSONIA_TEST_CULTURE");
			if (!string.IsNullOrEmpty(culture))
			{
				var cultureInfo = new CultureInfo(culture);
				CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
				CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;
				CultureInfo.CurrentCulture = cultureInfo;
				CultureInfo.CurrentUICulture = cultureInfo;
			}
		}
	}
}
