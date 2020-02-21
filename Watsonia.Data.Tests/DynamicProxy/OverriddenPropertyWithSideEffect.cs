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
		public void OverriddenPropertyWithSideEffect()
		{
			var customer = _db.Create<Customer>();
			customer.Name = "Harold";
			customer.DateOfBirth = DateTime.Today.AddYears(-21).AddDays(-7);
			Assert.AreEqual("It's not your birthday...", customer.BirthdayMessage);

			customer.DateOfBirth = new DateTime(1980, DateTime.Today.Month, DateTime.Today.Day);
			Assert.AreEqual("Happy birthday, Harold!", customer.BirthdayMessage);

			customer.DateOfBirth = DateTime.Today.AddYears(-21).AddMonths(-1);
			Assert.AreEqual("It's not your birthday...", customer.BirthdayMessage);
		}
	}
}
