using System;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Watsonia.Data.Tests.DynamicProxy.Entities;

namespace Watsonia.Data.Tests.DynamicProxy
{
	public partial class DynamicProxyTests
	{
		[TestMethod]
		public void ConstructorInitialization()
		{
			var initializer = DynamicProxyFactory.GetDynamicProxy<ConstructorInitializer>(_db);

			Assert.AreEqual("Hello", initializer.Name);
			Assert.AreEqual("Hey", initializer.Description);
		}
	}
}
