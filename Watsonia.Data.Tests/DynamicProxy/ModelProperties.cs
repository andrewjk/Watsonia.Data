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
		public void ModelProperties()
		{
			var orderProxy = (IDynamicProxy)DynamicProxyFactory.GetDynamicProxy<Order>(_db);

			((Order)orderProxy).NumberOfItems = 5;
			Assert.AreEqual(5, ((Order)orderProxy).NumberOfItems);
			((Order)orderProxy).DeliveryNotes = "OK to leave at the door";
			Assert.AreEqual("OK to leave at the door", ((Order)orderProxy).DeliveryNotes);
			((Order)orderProxy).DateRequired = DateTime.Today.AddDays(7);
			Assert.AreEqual(DateTime.Today.AddDays(7), ((Order)orderProxy).DateRequired);
			((Order)orderProxy).NumberOfItemsToOrder = 2;
			Assert.AreEqual(2, ((Order)orderProxy).NumberOfItemsToOrder);

			//IDynamicProxy customerProxy = (IDynamicProxy)DynamicProxyFactory.GetDynamicProxy<Customer>(_database);
			//Assert.AreEqual(0, ((Customer)customerProxy).Orders.Count);
		}
	}
}
