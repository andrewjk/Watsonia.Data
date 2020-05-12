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

			customer.Name = "darryl";
			Assert.AreEqual("darryl", ((IDynamicProxy)customer).__GetValue("name"));

			((IDynamicProxy)customer).__SetValue("name", "darren");
			Assert.AreEqual("darren", customer.Name);
		}
	}
}
