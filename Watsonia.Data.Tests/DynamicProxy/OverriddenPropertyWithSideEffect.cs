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
			var friend = _db.Create<Friend>();
			friend.Name = "Harold";
			friend.DateOfBirth = DateTime.Today.AddYears(-21).AddDays(-7);
			Assert.AreEqual("It's not your birthday...", friend.BirthdayMessage);

			friend.DateOfBirth = new DateTime(1980, DateTime.Today.Month, DateTime.Today.Day);
			Assert.AreEqual("Happy birthday, Harold!", friend.BirthdayMessage);

			friend.DateOfBirth = DateTime.Today.AddYears(-21).AddMonths(-1);
			Assert.AreEqual("It's not your birthday...", friend.BirthdayMessage);
		}
	}
}
