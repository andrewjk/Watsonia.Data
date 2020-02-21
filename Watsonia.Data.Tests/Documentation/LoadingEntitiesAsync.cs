using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Watsonia.Data.SqlServer;
using Watsonia.Data.Tests.Documentation.Entities;
using Watsonia.QueryBuilder;

namespace Watsonia.Data.Tests.Documentation
{
	/// <summary>
	/// This contains tests for all of the things we talk about in the documentation.
	/// </summary>
	public partial class DocumentationTestsAsync
	{
		[TestMethod]
		public async Task LoadingEntitiesAsync()
		{
			// It would be embarrassing if any of these didn't work!
			object existingAuthorID = null;

			// Test a LINQ query
			var query = from a in _db.Query<Author>()
						where a.LastName.StartsWith("P", StringComparison.InvariantCultureIgnoreCase)
						select a;
			foreach (var a in query)
			{
				if (existingAuthorID == null)
				{
					existingAuthorID = ((IDynamicProxy)a).__PrimaryKeyValue;
				}
			}
			Assert.AreEqual(2, query.ToList().Count);

			// Test a fluent SQL query
			var query2 = Select.From("Author").Where("LastName", SqlOperator.StartsWith, "P");
			Assert.AreEqual(2, (await _db.LoadCollectionAsync<Author>(query2)).Count);

			var query22 = Select.From<Author>().Where(a => a.LastName.StartsWith("P", StringComparison.InvariantCultureIgnoreCase));
			Assert.AreEqual(2, (await _db.LoadCollectionAsync(query22)).Count);

			// Test an SQL string
			var query3 = "SELECT * FROM Author WHERE LastName LIKE @0";
			Assert.AreEqual(2, (await _db.LoadCollectionAsync<Author>(query3, "P%")).Count);

			// Test loading a scalar value
			var query4 = Select.From("Author").Count("*").Where("LastName", SqlOperator.StartsWith, "P");
			var count = Convert.ToInt32(await _db.LoadValueAsync(query4));
			Assert.AreEqual(2, count);

			var query44 = Select.From<Author>().Count().Where(a => a.LastName.StartsWith("P", StringComparison.InvariantCultureIgnoreCase));
			var count44 = Convert.ToInt32(await _db.LoadValueAsync(query44));
			Assert.AreEqual(2, count44);

			// Test loading an item
			var author = await _db.LoadAsync<Author>(existingAuthorID);
			Assert.IsNotNull(author);
		}
	}
}
