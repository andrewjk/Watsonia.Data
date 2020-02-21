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
		public void DataErrorInfoMethods()
		{
			var invalid = _db.Create<Invalid>();
			var invalidProxy = (IDynamicProxy)invalid;

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
			var child1 = _db.Create<InvalidChild>();
			invalid.InvalidChildren.Add(child1);
			var child2 = _db.Create<InvalidChild>();
			invalid.InvalidChildren.Add(child2);

			child1.Name = "Good";
			child2.Name = "Baaaaaaaaaad";
			Assert.IsFalse(invalidProxy.StateTracker.IsValid);

			child1.Name = "Baaaaaaaaaad";
			child2.Name = "Good";
			Assert.IsFalse(invalidProxy.StateTracker.IsValid);

			child1.Name = "Good";
			child2.Name = "Yep";
			Assert.IsTrue(invalidProxy.StateTracker.IsValid);
		}
	}
}
