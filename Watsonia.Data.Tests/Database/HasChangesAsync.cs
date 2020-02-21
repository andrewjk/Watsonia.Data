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
		public async Task HasChangesAsync()
		{
			// Delete all existing has changes items
			var deleteHasChanges = Delete.From<HasChanges>().Where(true);
			await _db.ExecuteAsync(deleteHasChanges);
			var deleteHasChangesRelated = Delete.From<HasChangesRelated>().Where(true);
			await _db.ExecuteAsync(deleteHasChangesRelated);

			// Check that the delete worked
			var countHasChanges = Select.From("HasChanges").Count("*");
			Assert.AreEqual(0, Convert.ToInt32(await _db.LoadValueAsync(countHasChanges)));
			var countHasChangesRelated = Select.From("HasChangesRelated").Count("*");
			Assert.AreEqual(0, Convert.ToInt32(await _db.LoadValueAsync(countHasChangesRelated)));

			// Create a HasChanges and check that it has no changes
			var newHasChanges = _db.Create<HasChanges>();
			Assert.IsTrue(((IDynamicProxy)newHasChanges).StateTracker.IsNew);
			Assert.IsFalse(((IDynamicProxy)newHasChanges).StateTracker.HasChanges);

			// Create two related items and check that they have no changes
			var hasChangesRelated1 = _db.Create<HasChangesRelated>();
			Assert.IsTrue(((IDynamicProxy)hasChangesRelated1).StateTracker.IsNew);
			Assert.IsFalse(((IDynamicProxy)hasChangesRelated1).StateTracker.HasChanges);
			await _db.SaveAsync(hasChangesRelated1);

			var hasChangesRelated2 = _db.Create<HasChangesRelated>();
			Assert.IsTrue(((IDynamicProxy)hasChangesRelated2).StateTracker.IsNew);
			Assert.IsFalse(((IDynamicProxy)hasChangesRelated2).StateTracker.HasChanges);
			await _db.SaveAsync(hasChangesRelated2);

			// Even if we set things to their default values
			newHasChanges.Bool = false;
			newHasChanges.Int = 0;
			newHasChanges.DateTimeNullable = null;
			newHasChanges.Direction = HasChangesDirection.Unknown;
			newHasChanges.DecimalWithDefault = 5.5m;
			Assert.IsFalse(((IDynamicProxy)newHasChanges).StateTracker.HasChanges);
			Assert.AreEqual(0, ((IDynamicProxy)newHasChanges).StateTracker.ChangedFields.Count);

			// Insert it and check that the insert worked
			newHasChanges.String = "ABC";
			await _db.SaveAsync(newHasChanges);
			Assert.AreEqual(1, Convert.ToInt32(await _db.LoadValueAsync(countHasChanges)));
			Assert.IsFalse(((IDynamicProxy)newHasChanges).StateTracker.IsNew);
			Assert.IsFalse(((IDynamicProxy)newHasChanges).StateTracker.HasChanges);
			Assert.AreEqual(0, ((IDynamicProxy)newHasChanges).StateTracker.ChangedFields.Count);

			// Load the inserted HasChanges
			var hasChanges = await _db.LoadAsync<HasChanges>(((IDynamicProxy)newHasChanges).__PrimaryKeyValue);
			Assert.AreEqual("ABC", hasChanges.String);
			Assert.IsFalse(((IDynamicProxy)hasChanges).StateTracker.IsNew);
			Assert.IsFalse(((IDynamicProxy)hasChanges).StateTracker.HasChanges);
			Assert.AreEqual(0, ((IDynamicProxy)hasChanges).StateTracker.ChangedFields.Count);

			// Make sure that setting things to their existing values doesn't change HasChanges
			hasChanges.String = "ABC";
			hasChanges.DecimalWithDefault = 5.5m;
			hasChanges.Int = 0;
			Assert.IsFalse(((IDynamicProxy)hasChanges).StateTracker.HasChanges);
			Assert.AreEqual(0, ((IDynamicProxy)hasChanges).StateTracker.ChangedFields.Count);

			// Start setting things and checking that it's all good
			hasChanges.Bool = true;
			Assert.IsTrue(((IDynamicProxy)hasChanges).StateTracker.HasChanges);
			Assert.AreEqual(1, ((IDynamicProxy)hasChanges).StateTracker.ChangedFields.Count);
			Assert.AreEqual("Bool", ((IDynamicProxy)hasChanges).StateTracker.ChangedFields[0]);
			hasChanges.Bool = false;
			Assert.IsFalse(((IDynamicProxy)hasChanges).StateTracker.HasChanges);
			Assert.AreEqual(0, ((IDynamicProxy)hasChanges).StateTracker.ChangedFields.Count);

			hasChanges.BoolNullable = true;
			Assert.IsTrue(((IDynamicProxy)hasChanges).StateTracker.HasChanges);
			Assert.AreEqual(1, ((IDynamicProxy)hasChanges).StateTracker.ChangedFields.Count);
			Assert.AreEqual("BoolNullable", ((IDynamicProxy)hasChanges).StateTracker.ChangedFields[0]);
			hasChanges.BoolNullable = false;
			Assert.IsTrue(((IDynamicProxy)hasChanges).StateTracker.HasChanges);
			Assert.AreEqual(1, ((IDynamicProxy)hasChanges).StateTracker.ChangedFields.Count);
			Assert.AreEqual("BoolNullable", ((IDynamicProxy)hasChanges).StateTracker.ChangedFields[0]);
			hasChanges.BoolNullable = null;
			Assert.IsFalse(((IDynamicProxy)hasChanges).StateTracker.HasChanges);
			Assert.AreEqual(0, ((IDynamicProxy)hasChanges).StateTracker.ChangedFields.Count);

			hasChanges.Int = 10;
			Assert.IsTrue(((IDynamicProxy)hasChanges).StateTracker.HasChanges);
			Assert.AreEqual(1, ((IDynamicProxy)hasChanges).StateTracker.ChangedFields.Count);
			Assert.AreEqual("Int", ((IDynamicProxy)hasChanges).StateTracker.ChangedFields[0]);
			hasChanges.Int = 0;
			Assert.IsFalse(((IDynamicProxy)hasChanges).StateTracker.HasChanges);
			Assert.AreEqual(0, ((IDynamicProxy)hasChanges).StateTracker.ChangedFields.Count);

			hasChanges.IntNullable = 11;
			Assert.IsTrue(((IDynamicProxy)hasChanges).StateTracker.HasChanges);
			Assert.AreEqual(1, ((IDynamicProxy)hasChanges).StateTracker.ChangedFields.Count);
			Assert.AreEqual("IntNullable", ((IDynamicProxy)hasChanges).StateTracker.ChangedFields[0]);
			hasChanges.IntNullable = 0;
			Assert.IsTrue(((IDynamicProxy)hasChanges).StateTracker.HasChanges);
			Assert.AreEqual(1, ((IDynamicProxy)hasChanges).StateTracker.ChangedFields.Count);
			Assert.AreEqual("IntNullable", ((IDynamicProxy)hasChanges).StateTracker.ChangedFields[0]);
			hasChanges.IntNullable = null;
			Assert.IsFalse(((IDynamicProxy)hasChanges).StateTracker.HasChanges);
			Assert.AreEqual(0, ((IDynamicProxy)hasChanges).StateTracker.ChangedFields.Count);

			hasChanges.Decimal = 12.2m;
			Assert.IsTrue(((IDynamicProxy)hasChanges).StateTracker.HasChanges);
			Assert.AreEqual(1, ((IDynamicProxy)hasChanges).StateTracker.ChangedFields.Count);
			Assert.AreEqual("Decimal", ((IDynamicProxy)hasChanges).StateTracker.ChangedFields[0]);
			hasChanges.Decimal = 0;
			Assert.IsFalse(((IDynamicProxy)hasChanges).StateTracker.HasChanges);
			Assert.AreEqual(0, ((IDynamicProxy)hasChanges).StateTracker.ChangedFields.Count);

			hasChanges.DecimalNullable = 13.3m;
			Assert.IsTrue(((IDynamicProxy)hasChanges).StateTracker.HasChanges);
			Assert.AreEqual(1, ((IDynamicProxy)hasChanges).StateTracker.ChangedFields.Count);
			Assert.AreEqual("DecimalNullable", ((IDynamicProxy)hasChanges).StateTracker.ChangedFields[0]);
			hasChanges.DecimalNullable = 0;
			Assert.IsTrue(((IDynamicProxy)hasChanges).StateTracker.HasChanges);
			Assert.AreEqual(1, ((IDynamicProxy)hasChanges).StateTracker.ChangedFields.Count);
			Assert.AreEqual("DecimalNullable", ((IDynamicProxy)hasChanges).StateTracker.ChangedFields[0]);
			hasChanges.DecimalNullable = null;
			Assert.IsFalse(((IDynamicProxy)hasChanges).StateTracker.HasChanges);
			Assert.AreEqual(0, ((IDynamicProxy)hasChanges).StateTracker.ChangedFields.Count);

			hasChanges.DateTime = DateTime.Now;
			Assert.IsTrue(((IDynamicProxy)hasChanges).StateTracker.HasChanges);
			Assert.AreEqual(1, ((IDynamicProxy)hasChanges).StateTracker.ChangedFields.Count);
			Assert.AreEqual("DateTime", ((IDynamicProxy)hasChanges).StateTracker.ChangedFields[0]);
			hasChanges.DateTime = new DateTime(1900, 1, 1);
			Assert.IsFalse(((IDynamicProxy)hasChanges).StateTracker.HasChanges);
			Assert.AreEqual(0, ((IDynamicProxy)hasChanges).StateTracker.ChangedFields.Count);

			hasChanges.DateTimeNullable = DateTime.Now.AddDays(1);
			Assert.IsTrue(((IDynamicProxy)hasChanges).StateTracker.HasChanges);
			Assert.AreEqual(1, ((IDynamicProxy)hasChanges).StateTracker.ChangedFields.Count);
			Assert.AreEqual("DateTimeNullable", ((IDynamicProxy)hasChanges).StateTracker.ChangedFields[0]);
			hasChanges.DateTimeNullable = DateTime.MinValue;
			Assert.IsTrue(((IDynamicProxy)hasChanges).StateTracker.HasChanges);
			Assert.AreEqual(1, ((IDynamicProxy)hasChanges).StateTracker.ChangedFields.Count);
			Assert.AreEqual("DateTimeNullable", ((IDynamicProxy)hasChanges).StateTracker.ChangedFields[0]);
			hasChanges.DateTimeNullable = null;
			Assert.IsFalse(((IDynamicProxy)hasChanges).StateTracker.HasChanges);
			Assert.AreEqual(0, ((IDynamicProxy)hasChanges).StateTracker.ChangedFields.Count);

			hasChanges.Direction = HasChangesDirection.East;
			Assert.IsTrue(((IDynamicProxy)hasChanges).StateTracker.HasChanges);
			Assert.AreEqual(1, ((IDynamicProxy)hasChanges).StateTracker.ChangedFields.Count);
			Assert.AreEqual("Direction", ((IDynamicProxy)hasChanges).StateTracker.ChangedFields[0]);
			hasChanges.Direction = HasChangesDirection.Unknown;
			Assert.IsFalse(((IDynamicProxy)hasChanges).StateTracker.HasChanges);
			Assert.AreEqual(0, ((IDynamicProxy)hasChanges).StateTracker.ChangedFields.Count);

			hasChanges.String = "DEFGH";
			Assert.IsTrue(((IDynamicProxy)hasChanges).StateTracker.HasChanges);
			Assert.AreEqual(1, ((IDynamicProxy)hasChanges).StateTracker.ChangedFields.Count);
			Assert.AreEqual("String", ((IDynamicProxy)hasChanges).StateTracker.ChangedFields[0]);
			hasChanges.String = "ABC";
			Assert.IsFalse(((IDynamicProxy)hasChanges).StateTracker.HasChanges);
			Assert.AreEqual(0, ((IDynamicProxy)hasChanges).StateTracker.ChangedFields.Count);

			// Test setting a related item
			hasChanges.Related = hasChangesRelated1;
			Assert.IsTrue(((IDynamicProxy)hasChanges).StateTracker.HasChanges);
			Assert.AreEqual(1, ((IDynamicProxy)hasChanges).StateTracker.ChangedFields.Count);
			Assert.AreEqual("RelatedID", ((IDynamicProxy)hasChanges).StateTracker.ChangedFields[0]);
			hasChanges.Related = null;
			Assert.IsFalse(((IDynamicProxy)hasChanges).StateTracker.HasChanges);
			Assert.AreEqual(0, ((IDynamicProxy)hasChanges).StateTracker.ChangedFields.Count);

			hasChanges.Related = hasChangesRelated2;
			Assert.IsTrue(((IDynamicProxy)hasChanges).StateTracker.HasChanges);
			Assert.AreEqual(1, ((IDynamicProxy)hasChanges).StateTracker.ChangedFields.Count);
			Assert.AreEqual("RelatedID", ((IDynamicProxy)hasChanges).StateTracker.ChangedFields[0]);
			await _db.SaveAsync(hasChanges);
			Assert.IsFalse(((IDynamicProxy)hasChanges).StateTracker.HasChanges);
			Assert.AreEqual(0, ((IDynamicProxy)hasChanges).StateTracker.ChangedFields.Count);

			hasChanges.Related = hasChangesRelated1;
			Assert.IsTrue(((IDynamicProxy)hasChanges).StateTracker.HasChanges);
			Assert.AreEqual(1, ((IDynamicProxy)hasChanges).StateTracker.ChangedFields.Count);
			Assert.AreEqual("RelatedID", ((IDynamicProxy)hasChanges).StateTracker.ChangedFields[0]);

			hasChanges.Related = null;
			Assert.IsTrue(((IDynamicProxy)hasChanges).StateTracker.HasChanges);
			Assert.AreEqual(1, ((IDynamicProxy)hasChanges).StateTracker.ChangedFields.Count);
			Assert.AreEqual("RelatedID", ((IDynamicProxy)hasChanges).StateTracker.ChangedFields[0]);

			hasChanges.Related = hasChangesRelated2;
			Assert.IsFalse(((IDynamicProxy)hasChanges).StateTracker.HasChanges);
			Assert.AreEqual(0, ((IDynamicProxy)hasChanges).StateTracker.ChangedFields.Count);

			// Test setting multiple things
			hasChanges.IntNullable = 400;
			hasChanges.Int = 500;
			hasChanges.Bool = true;
			hasChanges.String = "GHI";
			Assert.IsTrue(((IDynamicProxy)hasChanges).StateTracker.HasChanges);
			Assert.AreEqual(4, ((IDynamicProxy)hasChanges).StateTracker.ChangedFields.Count);
			Assert.AreEqual("IntNullable", ((IDynamicProxy)hasChanges).StateTracker.ChangedFields[0]);
			Assert.AreEqual("Int", ((IDynamicProxy)hasChanges).StateTracker.ChangedFields[1]);
			Assert.AreEqual("Bool", ((IDynamicProxy)hasChanges).StateTracker.ChangedFields[2]);
			Assert.AreEqual("String", ((IDynamicProxy)hasChanges).StateTracker.ChangedFields[3]);

			// Test saving and resetting original fields
			hasChanges.Bool = true;
			hasChanges.BoolNullable = false;
			hasChanges.Int = 6000;
			hasChanges.IntNullable = 7000;
			hasChanges.Decimal = 100.1m;
			hasChanges.DecimalNullable = 0;
			hasChanges.DateTime = DateTime.Today;
			hasChanges.DateTimeNullable = DateTime.Today.AddDays(-1);
			hasChanges.Direction = HasChangesDirection.North;
			hasChanges.String = "JKL";
			hasChanges.DecimalWithDefault = 200.2m;
			hasChanges.Related = hasChangesRelated1;
			await _db.SaveAsync(hasChanges);
			Assert.IsFalse(((IDynamicProxy)hasChanges).StateTracker.HasChanges);
			Assert.AreEqual(0, ((IDynamicProxy)hasChanges).StateTracker.ChangedFields.Count);

			hasChanges.Bool = false;
			hasChanges.BoolNullable = true;
			hasChanges.Int = 8000;
			hasChanges.IntNullable = 9000;
			hasChanges.Decimal = 300.3m;
			hasChanges.DecimalNullable = 400.4m;
			hasChanges.DateTime = DateTime.Today.AddMonths(1);
			hasChanges.DateTimeNullable = DateTime.Today.AddDays(-1).AddMonths(1);
			hasChanges.Direction = HasChangesDirection.South;
			hasChanges.String = "MNO";
			hasChanges.DecimalWithDefault = 500.5m;
			hasChanges.Related = hasChangesRelated2;
			Assert.IsTrue(((IDynamicProxy)hasChanges).StateTracker.HasChanges);
			Assert.AreEqual(12, ((IDynamicProxy)hasChanges).StateTracker.ChangedFields.Count);
			Assert.AreEqual("Bool", ((IDynamicProxy)hasChanges).StateTracker.ChangedFields[0]);
			Assert.AreEqual("BoolNullable", ((IDynamicProxy)hasChanges).StateTracker.ChangedFields[1]);
			Assert.AreEqual("Int", ((IDynamicProxy)hasChanges).StateTracker.ChangedFields[2]);
			Assert.AreEqual("IntNullable", ((IDynamicProxy)hasChanges).StateTracker.ChangedFields[3]);
			Assert.AreEqual("Decimal", ((IDynamicProxy)hasChanges).StateTracker.ChangedFields[4]);
			Assert.AreEqual("DecimalNullable", ((IDynamicProxy)hasChanges).StateTracker.ChangedFields[5]);
			Assert.AreEqual("DateTime", ((IDynamicProxy)hasChanges).StateTracker.ChangedFields[6]);
			Assert.AreEqual("DateTimeNullable", ((IDynamicProxy)hasChanges).StateTracker.ChangedFields[7]);
			Assert.AreEqual("Direction", ((IDynamicProxy)hasChanges).StateTracker.ChangedFields[8]);
			Assert.AreEqual("String", ((IDynamicProxy)hasChanges).StateTracker.ChangedFields[9]);
			Assert.AreEqual("DecimalWithDefault", ((IDynamicProxy)hasChanges).StateTracker.ChangedFields[10]);
			Assert.AreEqual("RelatedID", ((IDynamicProxy)hasChanges).StateTracker.ChangedFields[11]);

			hasChanges.Bool = true;
			hasChanges.BoolNullable = false;
			hasChanges.Int = 6000;
			hasChanges.IntNullable = 7000;
			hasChanges.Decimal = 100.1m;
			hasChanges.DecimalNullable = 0;
			hasChanges.DateTime = DateTime.Today;
			hasChanges.DateTimeNullable = DateTime.Today.AddDays(-1);
			hasChanges.Direction = HasChangesDirection.North;
			hasChanges.String = "JKL";
			hasChanges.DecimalWithDefault = 200.2m;
			hasChanges.Related = hasChangesRelated1;
			Assert.IsFalse(((IDynamicProxy)hasChanges).StateTracker.HasChanges);
			Assert.AreEqual(0, ((IDynamicProxy)hasChanges).StateTracker.ChangedFields.Count);
		}
	}
}
