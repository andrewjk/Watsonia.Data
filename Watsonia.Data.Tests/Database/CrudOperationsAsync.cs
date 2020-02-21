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
		public async Task CrudOperationsAsync()
		{
			// Delete all existing cruds
			var deleteCruds = Delete.From<Crud>().Where(true);
			await _db.ExecuteAsync(deleteCruds);

			// Check that the delete worked
			var countCruds = Select.From("Crud").Count("*");
			Assert.AreEqual(0, Convert.ToInt32(await _db.LoadValueAsync(countCruds)));

			// Insert a crud and check that the insert worked and the new ID is correctly set
			var newCrud = _db.Create<Crud>();
			newCrud.Name = "ABC";
			await _db.SaveAsync(newCrud);
			Assert.AreEqual(1, Convert.ToInt32(await _db.LoadValueAsync(countCruds)));
			Assert.IsTrue(newCrud.ID > 0);

			// Load the inserted crud
			var crud = await _db.LoadAsync<Crud>(newCrud.ID);
			Assert.AreEqual("ABC", crud.Name);

			// Update the crud
			crud.Name = "DEF";
			await _db.SaveAsync(crud);

			// Load the updated crud
			var updatedCrud = await _db.LoadAsync<Crud>(newCrud.ID);
			Assert.AreEqual("DEF", crud.Name);

			// And delete it
			await _db.DeleteAsync(updatedCrud);
			Assert.AreEqual(0, Convert.ToInt32(await _db.LoadValueAsync(countCruds)));
		}
	}
}
