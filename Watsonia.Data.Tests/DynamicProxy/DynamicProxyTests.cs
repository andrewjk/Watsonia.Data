using System;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Watsonia.Data.Tests.DynamicProxy
{
	[TestClass]
	public partial class DynamicProxyTests
	{
		private static readonly DynamicProxyDatabase _db = new DynamicProxyDatabase();

		[ClassInitialize]
		public static void InitializeAsync(TestContext _)
		{
			if (!File.Exists(@"Data\DynamicProxyTests.sqlite"))
			{
				var file = File.Create(@"Data\DynamicProxyTests.sqlite");
				file.Dispose();
			}

			_db.UpdateDatabase();
		}
	}
}
