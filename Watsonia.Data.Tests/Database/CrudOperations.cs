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
		public void CrudOperations()
		{
			// Delete all existing cruds
			var deleteCruds = Delete.From<Crud>().Where(true);
			_db.Execute(deleteCruds);

			// Check that the delete worked
			var countCruds = Select.From("Crud").Count("*");
			Assert.AreEqual(0, Convert.ToInt32(_db.LoadValue(countCruds)));

			// Insert a crud and check that the insert worked and the new ID is correctly set
			var newCrud = _db.Create<Crud>();
			newCrud.Name = "ABC";
			_db.Save(newCrud);
			Assert.AreEqual(1, Convert.ToInt32(_db.LoadValue(countCruds)));
			Assert.IsTrue(newCrud.ID > 0);

			// Load the inserted crud
			var crud = _db.Load<Crud>(newCrud.ID);
			Assert.AreEqual("ABC", crud.Name);

			// Update the crud
			crud.Name = "DEF";
			_db.Save(crud);

			// Load the updated crud
			var updatedCrud = _db.Load<Crud>(newCrud.ID);
			Assert.AreEqual("DEF", crud.Name);

			// And delete it
			_db.Delete(updatedCrud);
			Assert.AreEqual(0, Convert.ToInt32(_db.LoadValue(countCruds)));
		}
	}
}
