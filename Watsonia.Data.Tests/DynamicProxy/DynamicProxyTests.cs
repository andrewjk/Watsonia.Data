using System;
using System.ComponentModel;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Watsonia.Data.Tests.DynamicProxy
{
	[TestClass]
	public class DynamicProxyTests
	{
		private static Database db = new Database(null, "", "Watsonia.Data.Tests.DynamicProxy");

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
			IDataErrorInfo invalidErrorInfo = (IDataErrorInfo)invalid;
			IDynamicProxy invalidProxy = (IDynamicProxy)invalid;

			// Make sure that the RequiredString doesn't have an error until its property gets changed
			Assert.AreEqual("", invalidErrorInfo["RequiredString"]);

			invalid.RequiredString = "Aoeu";
			invalid.RequiredString = "";
			Assert.AreEqual("The Required string field is required.", invalidErrorInfo["RequiredString"]);
			invalid.RequiredString = "A string";
			Assert.AreEqual("", invalidErrorInfo["RequiredString"]);

			invalid.RequiredNullable = null;
			Assert.AreEqual("The Required nullable field is required.", invalidErrorInfo["RequiredNullable"]);
			invalid.RequiredNullable = 5;
			Assert.AreEqual("", invalidErrorInfo["RequiredNullable"]);

			invalid.ShortString = "Far too long";
			Assert.AreEqual("The field Short string must be a string with a maximum length of 10.", invalidErrorInfo["ShortString"]);
			invalid.ShortString = "Better";
			Assert.AreEqual("", invalidErrorInfo["ShortString"]);

			invalid.InvalidPostCode = "30001";
			Assert.AreEqual(@"The field Invalid post code must match the regular expression '^\d{4}$'.", invalidErrorInfo["InvalidPostCode"]);
			invalid.InvalidPostCode = "3001";
			Assert.AreEqual("", invalidErrorInfo["InvalidPostCode"]);
	
			invalid.EmailAddress = "info.donotreply.com";
			Assert.AreEqual("The Email address field is no good.", invalidErrorInfo["EmailAddress"]);
			invalid.EmailAddress = "info@donotreply.com";
			Assert.AreEqual("", invalidErrorInfo["EmailAddress"]);
			
			invalid.ConfirmEmailAddress = "support.donotreply.com";
			Assert.AreEqual("The Confirm email address field is no good.", invalidErrorInfo["ConfirmEmailAddress"]);
			invalid.ConfirmEmailAddress = "support@donotreply.com";
			Assert.AreEqual("", invalidErrorInfo["ConfirmEmailAddress"]);

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
