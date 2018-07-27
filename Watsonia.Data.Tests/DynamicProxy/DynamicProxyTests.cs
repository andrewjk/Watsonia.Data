﻿using System;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Watsonia.Data.Tests.DynamicProxy
{
	[TestClass]
	public class DynamicProxyTests
	{
		private static DynamicProxyDatabase db = new DynamicProxyDatabase();

		[ClassInitialize]
		public static void Initialize(TestContext context)
		{
			if (!File.Exists(@"Data\DynamicProxyTests.sqlite"))
			{
				File.Create(@"Data\DynamicProxyTests.sqlite");
			}

			db.UpdateDatabase();
		}

		[TestMethod]
		public void TestProxyProperties()
		{
			IDynamicProxy customerProxy = (IDynamicProxy)DynamicProxyFactory.GetDynamicProxy<Customer>(db);
			IDynamicProxy orderProxy = (IDynamicProxy)DynamicProxyFactory.GetDynamicProxy<Order>(db);

			// Test that an ID property is overridden in Customer and created in Order
			PropertyInfo customerIDProperty = customerProxy.GetType().GetProperty("ID", BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
			Assert.AreNotEqual(null, customerIDProperty);
			//customerIDProperty.SetValue(customerProxy, 1, null);

			PropertyInfo orderIDProperty = orderProxy.GetType().GetProperty("ID", BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
			Assert.AreNotEqual(null, orderIDProperty);
			orderIDProperty.SetValue(orderProxy, -1, null);

			// Test that the IsNew property is created
			PropertyInfo isNewProperty = orderProxy.GetType().GetProperty("IsNew", BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
			Assert.AreNotEqual(null, isNewProperty);
		}

		[TestMethod]
		public void TestEquality()
		{
			Customer a = DynamicProxyFactory.GetDynamicProxy<Customer>(db);
			Customer b = DynamicProxyFactory.GetDynamicProxy<Customer>(db);

			((IDynamicProxy)a).PrimaryKeyValue = 5;
			((IDynamicProxy)b).PrimaryKeyValue = 5;
	
			Assert.AreEqual(true, a.Equals(b), "Using Equals failed");
			Assert.AreNotEqual("", a.GetHashCode(), "GetHashCode failed");

			// We can't add operator overloads at run-time but we can test whether adding them in a base class will work
			Assert.AreEqual(true, a == b, "Using == failed");
			Assert.AreEqual(false, a != b, "Using != failed");

			Order c = DynamicProxyFactory.GetDynamicProxy<Order>(db);
			((IDynamicProxy)c).PrimaryKeyValue = 5;
			Assert.AreEqual(false, a.Equals(c), "Comparing different types failed");
		}

		[TestMethod]
		public void TestModelProperties()
		{
			IDynamicProxy orderProxy = (IDynamicProxy)DynamicProxyFactory.GetDynamicProxy<Order>(db);

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

		[TestMethod]
		public void TestOverriddenPropertyWithSideEffect()
		{
			var customer = db.Create<Customer>();
			customer.Name = "Harold";
			customer.DateOfBirth = DateTime.Today.AddYears(-21).AddDays(-7);
			Assert.AreEqual("It's not your birthday...", customer.BirthdayMessage);

			customer.DateOfBirth = new DateTime(1980, DateTime.Today.Month, DateTime.Today.Day);
			Assert.AreEqual("Happy birthday, Harold!", customer.BirthdayMessage);

			customer.DateOfBirth = DateTime.Today.AddYears(-21).AddMonths(-1);
			Assert.AreEqual("It's not your birthday...", customer.BirthdayMessage);
		}

		[TestMethod]
		public void TestPrimaryKeyValueChangeEvent()
		{
			var winston = db.Create<Customer>();
			winston.ID = 500;
			winston.Name = "Winston";

			var jerry = db.Create<Customer>();
			jerry.ID = -501;
			jerry.Name = "Jerry";

			var address = db.Create<Address>();
			var property = address.GetType().GetProperty("CustomerID");

			address.Customer = winston;
			Assert.AreEqual(500, (long)property.GetValue(address));

			address.Customer = jerry;
			Assert.AreEqual(-501, (long)property.GetValue(address));

			// Updating Jerry's ID (like, for instance, when saving a new customer) should update the CustomerID too
			bool eventIsFiringOk = false;
			((IDynamicProxy)jerry).PrimaryKeyValueChanged += delegate
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

		[TestMethod]
		public void TestDefaultValues()
		{
			Defaults def = DynamicProxyFactory.GetDynamicProxy<Defaults>(db);

			Assert.AreEqual(true, def.Bool);
			Assert.AreEqual(10, def.Int);
			Assert.AreEqual(12, def.NullableInt);
			Assert.AreEqual(20, def.Long);
			Assert.AreEqual(30, def.Decimal);
			Assert.AreEqual("Hi", def.String);
			Assert.AreEqual("", def.EmptyString);
			Assert.AreEqual(new DateTime(1900, 1, 1), def.Date);
		}

		[TestMethod]
		public void TestConstructorInitialization()
		{
			ConstructorInitializer initializer = DynamicProxyFactory.GetDynamicProxy<ConstructorInitializer>(db);

			Assert.AreEqual("Hello", initializer.Name);
			Assert.AreEqual("Hey", initializer.Description);
		}

		[TestMethod]
		public void TestDataErrorInfoMethods()
		{
			Invalid invalid = db.Create<Invalid>();
			IDynamicProxy invalidProxy = (IDynamicProxy)invalid;

			Assert.AreEqual("The Required string field is required.", invalidProxy.StateTracker.GetErrorText("RequiredString"));

			invalid.RequiredString = "Aoeu";
			Assert.AreEqual("", invalidProxy.StateTracker.GetErrorText("RequiredString"));

			invalid.RequiredString = "";
			Assert.AreEqual("The Required string field is required.", invalidProxy.StateTracker.GetErrorText("RequiredString"));
			invalid.RequiredString = "A string";
			Assert.AreEqual("", invalidProxy.StateTracker.GetErrorText("RequiredString"));

			invalid.RequiredNullable = null;
			Assert.AreEqual("The Required nullable field is required.", invalidProxy.StateTracker.GetErrorText("RequiredNullable"));
			invalid.RequiredNullable = 5;
			Assert.AreEqual("", invalidProxy.StateTracker.GetErrorText("RequiredNullable"));

			invalid.ShortString = "Far too long";
			Assert.AreEqual("The field Short string must be a string with a maximum length of 10.", invalidProxy.StateTracker.GetErrorText("ShortString"));
			invalid.ShortString = "Better";
			Assert.AreEqual("", invalidProxy.StateTracker.GetErrorText("ShortString"));

			invalid.InvalidPostCode = "30001";
			Assert.AreEqual(@"The field Invalid post code must match the regular expression '^\d{4}$'.", invalidProxy.StateTracker.GetErrorText("InvalidPostCode"));
			invalid.InvalidPostCode = "3001";
			Assert.AreEqual("", invalidProxy.StateTracker.GetErrorText("InvalidPostCode"));
	
			invalid.EmailAddress = "info.donotreply.com";
			Assert.AreEqual("The Email address field is no good.", invalidProxy.StateTracker.GetErrorText("EmailAddress"));
			invalid.EmailAddress = "info@donotreply.com";
			Assert.AreEqual("", invalidProxy.StateTracker.GetErrorText("EmailAddress"));
			
			invalid.ConfirmEmailAddress = "support.donotreply.com";
			Assert.AreEqual("The Confirm email address field is no good.", invalidProxy.StateTracker.GetErrorText("ConfirmEmailAddress"));
			invalid.ConfirmEmailAddress = "support@donotreply.com";
			Assert.AreEqual("", invalidProxy.StateTracker.GetErrorText("ConfirmEmailAddress"));

			// Validate the whole thing
			InvalidChild child1 = db.Create<InvalidChild>();
			invalid.InvalidChildren.Add(child1);
			InvalidChild child2 = db.Create<InvalidChild>();
			invalid.InvalidChildren.Add(child2);

			child1.Name = "Good";
			child2.Name = "Baaaaaaaaaad";
			Assert.IsFalse(invalidProxy.IsValid);

			child1.Name = "Baaaaaaaaaad";
			child2.Name = "Good";
			Assert.IsFalse(invalidProxy.IsValid);

			child1.Name = "Good";
			child2.Name = "Yep";
			Assert.IsTrue(invalidProxy.IsValid);
		}
	}
}
