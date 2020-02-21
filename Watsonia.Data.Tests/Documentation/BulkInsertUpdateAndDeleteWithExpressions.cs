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
	public partial class DocumentationTests
	{
		[TestMethod]
		public void BulkInsertUpdateAndDeleteWithExpressions()
		{
			// Not sure how useful these would be:
			//var insert = Insert.Into<Author>().Values(new Author() { FirstName = "Enter", LastName = "Name" }, 20);
			//db.Execute(insert);

			//var insert = Insert.Into<Author>().Select(Select.From<Author>().Where(a => a.LastName.StartsWith("P"));
			//db.Execute(insert);

			var update = Update.Table<Author>().Set(a => a.Rating, 95).Where(a => a.LastName.StartsWith("P"));
			_db.Execute(update);

			var delete = Delete.From<Author>().Where(a => a.Rating < 80);
			_db.Execute(delete);
		}
	}
}
