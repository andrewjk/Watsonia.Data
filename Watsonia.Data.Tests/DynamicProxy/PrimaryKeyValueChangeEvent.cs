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
		public void PrimaryKeyValueChangeEvent()
		{
			var winston = _db.Create<Customer>();
			winston.ID = 500;
			winston.Name = "Winston";

			var jerry = _db.Create<Customer>();
			jerry.ID = -501;
			jerry.Name = "Jerry";

			var address = _db.Create<Address>();
			var property = address.GetType().GetProperty("CustomerID");

			address.Customer = winston;
			Assert.AreEqual(500, (long)property.GetValue(address));

			address.Customer = jerry;
			Assert.AreEqual(-501, (long)property.GetValue(address));

			// Updating Jerry's ID (like, for instance, when saving a new customer) should update the CustomerID too
			var eventIsFiringOk = false;
			((IDynamicProxy)jerry).__PrimaryKeyValueChanged += delegate
			{
				eventIsFiringOk = true;
			};
			jerry.ID = 1000;
			Assert.IsTrue(eventIsFiringOk);
			Assert.AreEqual(1000, (long)property.GetValue(address));

			// Updating Winston's ID should no longer update the CustomerID
			winston.ID = 2000;
			Assert.AreEqual(1000, (long)property.GetValue(address));
		}
	}
}
