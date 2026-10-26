using System.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Watsonia.Data.TestPerformance;

namespace Watsonia.Data.Tests.Integration
{
	/// <summary>
	/// Runs the benchmark correctness cross-check as part of the test suite:
	/// every framework must return identical data.
	/// </summary>
	[TestClass]
	public class BenchmarkVerificationTests
	{
		[TestMethod]
		public void AllFrameworksReturnTheSameData()
		{
			Assert.IsTrue(Verification.Run());
		}
	}
}
