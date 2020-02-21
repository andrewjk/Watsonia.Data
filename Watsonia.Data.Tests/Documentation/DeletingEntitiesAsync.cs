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
		public async Task DeletingEntitiesAsync()
		{
			await _db.InsertAsync(new Author { FirstName = "James", LastName = "Frey" });
			await _db.InsertAsync(new Author { FirstName = "Helen", LastName = "Demidenko" });

			// Delete item
			var jf = _db.Query<Author>().FirstOrDefault(a => a.FirstName == "James" && a.LastName == "Frey");
			Assert.IsNotNull(jf);
			await _db.DeleteAsync(jf);
			var jfGone = _db.Query<Author>().FirstOrDefault(a => a.FirstName == "James" && a.LastName == "Frey");
			Assert.IsNull(jfGone);

			// Delete by id
			var hd = _db.Query<Author>().FirstOrDefault(a => a.FirstName == "Helen" && a.LastName == "Demidenko");
			Assert.IsNotNull(hd);
			await _db.DeleteAsync<Author>(hd.ID);
			var hdGone = _db.Query<Author>().FirstOrDefault(a => a.FirstName == "Helen" && a.LastName == "Demidenko");
			Assert.IsNull(hdGone);
		}
	}
}
