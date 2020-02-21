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
	public partial class EntitiesTests
	{
		[TestMethod]
		public void Collections()
		{
			// Delete all existing collections
			var deleteCollections = Delete.From<Collection>().Where(true);
			_db.Execute(deleteCollections);

			_db.Insert(new Collection() { Value = 1, Description = "One" });
			_db.Insert(new Collection() { Value = 2, Description = "Two" });
			_db.Insert(new Collection() { Value = 3, Description = "Three" });
			_db.Insert(new Collection() { Value = 4, Description = "Four" });
			_db.Insert(new Collection() { Value = 5, Description = "Five" });

			// Load all of the collections except for one
			var select = Select.From("Collection").Where("Description", SqlOperator.NotEquals, "Four").OrderBy("Description");
			var collection = _db.LoadCollection<Collection>(select);
			Assert.AreEqual(4, collection.Count);
			Assert.AreEqual("Two", collection[3].Description);

			// Load all of the collections except for one with an IN statement
			var select2 = Select.From("Collection").Where("Value", SqlOperator.IsIn, new int[] { 1, 2, 3, 5, 6}).OrderBy("Description");
			var collection2 = _db.LoadCollection<Collection>(select2);
			Assert.AreEqual(4, collection2.Count);
			Assert.AreEqual("Two", collection2[3].Description);
		}
	}
}
