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
		public async Task BulkInsertUpdateAndDeleteAsync()
		{
			// Update using fluent SQL
			var update = Update.Table("Author").Set("Rating", 95).Where("LastName", SqlOperator.StartsWith, "P");
			await _db.ExecuteAsync(update);

			// Update using an SQL string
			var update2 = "UPDATE Author SET Rating = 95 WHERE LastName LIKE @0";
			await _db.ExecuteAsync(update2, "P%");

			// Delete using fluent SQL
			var delete = Delete.From("Author").Where("Rating", SqlOperator.IsLessThan, 80);
			await _db.ExecuteAsync(delete);

			// Delete using an SQL string
			var delete2 = "DELETE FROM Author WHERE Rating < @0";
			await _db.ExecuteAsync(delete2, 80);
		}
	}
}
