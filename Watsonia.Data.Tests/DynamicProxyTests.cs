﻿using System;
using System.ComponentModel;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Watsonia.Data.Tests.Models;

namespace Watsonia.Data.Tests
{
	[TestClass]
	public class DynamicProxyTests
	{
		private static Database db = new Database("", "Watsonia.Data.Tests.Models");

		[ClassInitialize]
		public static void Initialize(TestContext context)
		{
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
		public void TestProxyCaching()
		{
			// TODO:
		}

		[TestMethod]
		public void TestEquality()
		{
			IDynamicProxy customerProxy = (IDynamicProxy)DynamicProxyFactory.GetDynamicProxy<Customer>(db);
			customerProxy.PrimaryKeyValue = 5;
			IDynamicProxy customerProxy2 = (IDynamicProxy)DynamicProxyFactory.GetDynamicProxy<Customer>(db);
			customerProxy2.PrimaryKeyValue = 5;
			Assert.AreEqual(true, customerProxy.Equals(customerProxy2));
			// NOTE: This doesn't work too well with an equals operator overload on the interface
			//Assert.AreEqual(true, customerProxy == customerProxy2);
			Assert.AreNotEqual("", customerProxy.GetHashCode());

			IDynamicProxy orderProxy = (IDynamicProxy)DynamicProxyFactory.GetDynamicProxy<Order>(db);
			orderProxy.PrimaryKeyValue = 5;
			Assert.AreEqual(false, customerProxy.Equals(orderProxy));
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
		public void TestPropertyChangeEvents()
		{
			IDynamicProxy customerProxy = (IDynamicProxy)DynamicProxyFactory.GetDynamicProxy<Customer>(db);

			// Test that PropertyChanging, PropertyChanged and OnNameChanged are called correctly
			bool propertyChanging = false;
			bool propertyChanged = false;
			bool nameChanged = false;
			customerProxy.PropertyChanging += delegate(object sender, PropertyChangingEventArgs e)
			{
				Assert.AreEqual("Name", e.PropertyName);
				propertyChanging = true;
			};
			customerProxy.PropertyChanged += delegate(object sender, PropertyChangedEventArgs e)
			{
				Assert.AreEqual("Name", e.PropertyName);
				propertyChanged = true;
			};
			((Customer)customerProxy).NameChanged += delegate(object sender, EventArgs e)
			{
				nameChanged = true;
			};

			((Customer)customerProxy).Name = "Barry";
			Assert.AreEqual(true, propertyChanging);
			Assert.AreEqual(true, propertyChanged);
			Assert.AreEqual(true, nameChanged);
		}

		[TestMethod]
		public void TestDefaultValues()
		{
			Def def = DynamicProxyFactory.GetDynamicProxy<Def>(db);

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
		public void TestDataErrorInfoMethods()
		{
			Bung bung = DynamicProxyFactory.GetDynamicProxy<Bung>(db);
			IDataErrorInfo bungErrorInfo = (IDataErrorInfo)bung;

			bung.RequiredString = "Aoeu";
			bung.RequiredString = "";
			Assert.AreEqual("The Required string field is required.", bungErrorInfo["RequiredString"]);
			bung.RequiredString = "A string";
			Assert.AreEqual("", bungErrorInfo["RequiredString"]);

			bung.RequiredNullable = null;
			Assert.AreEqual("The Required nullable field is required.", bungErrorInfo["RequiredNullable"]);
			bung.RequiredNullable = 5;
			Assert.AreEqual("", bungErrorInfo["RequiredNullable"]);

			bung.ShortString = "Far too long";
			Assert.AreEqual("The field Short string must be a string with a maximum length of 10.", bungErrorInfo["ShortString"]);
			bung.ShortString = "Better";
			Assert.AreEqual("", bungErrorInfo["ShortString"]);

			bung.InvalidPostCode = "30001";
			Assert.AreEqual(@"The field Invalid post code must match the regular expression '^\d{4}$'.", bungErrorInfo["InvalidPostCode"]);
			bung.InvalidPostCode = "3001";
			Assert.AreEqual("", bungErrorInfo["InvalidPostCode"]);
	
			bung.EmailAddress = "info@donotreply.com";
			Assert.AreEqual("The Email address field is no good.", bungErrorInfo["EmailAddress"]);
			
			bung.ConfirmEmailAddress = "support@donotreply.com";
			Assert.AreEqual("The Confirm email address field is no good.", bungErrorInfo["ConfirmEmailAddress"]);
		}
	}
}
