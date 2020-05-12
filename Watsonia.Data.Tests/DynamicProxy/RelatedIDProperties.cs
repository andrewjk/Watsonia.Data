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
		public async Task RelatedIDProperties()
		{
			// Create and save a state
			var vic = _db.Create<State>();
			vic.Name = "VIC";
			await _db.SaveAsync(vic);

			// Create and save a suburb
			var southMelb = _db.Create<Suburb>();
			southMelb.Name = "South Melbourne";
			await _db.SaveAsync(southMelb);

			// Set the State property and make sure the StateID is correct
			southMelb.State = vic;
			Assert.AreEqual(southMelb.StateID, vic.ID);
		}
	}
}
