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
		public void DefaultValues()
		{
			var def = DynamicProxyFactory.GetDynamicProxy<Defaults>(_db);

			Assert.AreEqual(true, def.Bool);
			Assert.AreEqual(10, def.Int);
			Assert.AreEqual(12, def.NullableInt);
			Assert.AreEqual(20, def.Long);
			Assert.AreEqual(30, def.Decimal);
			Assert.AreEqual("Hi", def.String);
			Assert.AreEqual("", def.EmptyString);
			Assert.AreEqual(new DateTime(1900, 1, 1), def.Date);
		}
	}
}
