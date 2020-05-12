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
		public void ProxyIDProperties()
		{
			var suburbProxy = (IDynamicProxy)DynamicProxyFactory.GetDynamicProxy<Suburb>(_db);
			var stateProxy = (IDynamicProxy)DynamicProxyFactory.GetDynamicProxy<State>(_db);

			var flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly;

			// Test that an ID property is overridden in Suburb and created in State
			var customerIDProperty = suburbProxy.GetType().GetProperty("ID", flags);
			Assert.AreNotEqual(null, customerIDProperty);
			customerIDProperty.SetValue(suburbProxy, 1, null);

			var orderIDProperty = stateProxy.GetType().GetProperty("ID", flags);
			Assert.AreNotEqual(null, orderIDProperty);
			orderIDProperty.SetValue(stateProxy, -1, null);
		}
	}
}
