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
		public void Equality()
		{
			var a = DynamicProxyFactory.GetDynamicProxy<Customer>(_db);
			var b = DynamicProxyFactory.GetDynamicProxy<Customer>(_db);

			((IDynamicProxy)a).__PrimaryKeyValue = 5;
			((IDynamicProxy)b).__PrimaryKeyValue = 5;
	
			Assert.AreEqual(true, a.Equals(b), "Using Equals failed");
			Assert.AreNotEqual("", a.GetHashCode(), "GetHashCode failed");

			// We can't add operator overloads at run-time but we can test whether adding them in a base class will work
			Assert.AreEqual(true, a == b, "Using == failed");
			Assert.AreEqual(false, a != b, "Using != failed");

			var c = DynamicProxyFactory.GetDynamicProxy<Order>(_db);
			((IDynamicProxy)c).__PrimaryKeyValue = 5;
			Assert.AreEqual(false, a.Equals(c), "Comparing different types failed");
		}
	}
}
