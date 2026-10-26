using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[assembly: DoNotParallelize]

namespace Watsonia.Data.Tests
{
	[TestClass]
	public static class TestSetup
	{
		[AssemblyInitialize]
		public static void AssemblyInitialize(TestContext context)
		{
			// Run from the test assembly's directory so that relative paths
			// to the Data folder and baseline files resolve correctly
			var assemblyDirectory = Path.GetDirectoryName(typeof(TestSetup).Assembly.Location);
			if (!string.IsNullOrEmpty(assemblyDirectory))
			{
				Directory.SetCurrentDirectory(assemblyDirectory);
			}
		}
	}
}
