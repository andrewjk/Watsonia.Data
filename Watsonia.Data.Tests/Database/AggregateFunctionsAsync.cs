using System.Data.SqlClient;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Data.Common;
using Watsonia.Data.SqlServer;
using System.IO;
using System;
using Watsonia.QueryBuilder;
using System.Threading.Tasks;
using Watsonia.Data.Tests.Database.Entities;

namespace Watsonia.Data.Tests.Database
{
	/// <summary>
	/// Contains general tests for the database.
	/// </summary>
	public partial class EntitiesTestsAsync
	{
		[TestMethod]
		public async Task AggregateFunctionsAsync()
		{
			// Delete all existing aggs
			var deleteAggregates = Delete.From<Aggregated>().Where(true);
			await _db.ExecuteAsync(deleteAggregates);

			// Add some test aggs
			await _db.InsertAsync(new Aggregated() { Value = 1 });
			await _db.InsertAsync(new Aggregated() { Value = 3 });
			await _db.InsertAsync(new Aggregated() { Value = 5 });
			await _db.InsertAsync(new Aggregated() { Value = 7 });
			await _db.InsertAsync(new Aggregated() { Value = 11 });

			// Test count
			var selectCount = Select.From("Aggregated").Count("*");
			Assert.AreEqual(5, Convert.ToInt32(await _db.LoadValueAsync(selectCount)));

			// Test sum
			var selectSum = Select.From("Aggregated").Sum("Value");
			Assert.AreEqual(27d, Convert.ToDouble(await _db.LoadValueAsync(selectSum)));

			// Test average
			var selectAverage = Select.From("Aggregated").Average("Value");
			Assert.AreEqual(5.4, Convert.ToDouble(await _db.LoadValueAsync(selectAverage)));

			// Test minimum
			var selectMin = Select.From("Aggregated").Min("Value");
			Assert.AreEqual(1d, Convert.ToDouble(await _db.LoadValueAsync(selectMin)));

			// Test maximum
			var selectMax = Select.From("Aggregated").Max("Value");
			Assert.AreEqual(11d, Convert.ToDouble(await _db.LoadValueAsync(selectMax)));
		}
	}
}
