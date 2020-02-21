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
		public void GetAndSetValue()
		{
			var customer = _db.Create<Customer>();

			customer.Age = 55;
			Assert.AreEqual(55, ((IDynamicProxy)customer).__GetValue("age"));

			((IDynamicProxy)customer).__SetValue("age", 66);
			Assert.AreEqual(66, customer.Age);
		}
	}
}
