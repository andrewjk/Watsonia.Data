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
		public void ProxyProperties()
		{
			var customerProxy = (IDynamicProxy)DynamicProxyFactory.GetDynamicProxy<Customer>(_db);
			var orderProxy = (IDynamicProxy)DynamicProxyFactory.GetDynamicProxy<Order>(_db);

			// Test that an ID property is overridden in Customer and created in Order
			var customerIDProperty = customerProxy.GetType().GetProperty("ID", BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
			Assert.AreNotEqual(null, customerIDProperty);
			//customerIDProperty.SetValue(customerProxy, 1, null);

			var orderIDProperty = orderProxy.GetType().GetProperty("ID", BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
			Assert.AreNotEqual(null, orderIDProperty);
			orderIDProperty.SetValue(orderProxy, -1, null);
		}
	}
}
