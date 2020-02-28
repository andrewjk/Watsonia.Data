using System.Data.SqlClient;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Data.Common;
using Watsonia.Data.SqlServer;
using System.IO;
using System;
using Watsonia.QueryBuilder;
using System.Threading.Tasks;
using Watsonia.Data.Tests.Saving.Entities;

namespace Watsonia.Data.Tests.Saving
{
	/// <summary>
	/// Contains general tests for the database.
	/// </summary>
	public partial class SavingTests
	{
		[TestMethod]
		public async Task SavingAsync()
		{
			// Delete all existing items
			var deleteSubChilds = Delete.From<GrandChild>().Where(true);
			await _db.ExecuteAsync(deleteSubChilds);
			var deleteChilds = Delete.From<Child>().Where(true);
			await _db.ExecuteAsync(deleteChilds);
			var deleteParents = Delete.From<Parent>().Where(true);
			await _db.ExecuteAsync(deleteParents);

			// Create an item with all three levels
			var parent = _db.Create<Parent>();
			parent.Name = "Parent!";

			var oldestChild = _db.Create<Child>();
			oldestChild.Name = "Oldest!";
			oldestChild.Parent = parent;

			var middleChild = _db.Create<Child>();
			middleChild.Name = "Middle!";
			middleChild.Parent = parent;

			var youngestChild = _db.Create<Child>();
			youngestChild.Name = "Youngest!";
			youngestChild.Parent = parent;

			parent.Children.Add(oldestChild);
			parent.Children.Add(middleChild);
			parent.Children.Add(youngestChild);

			var oldestGrandChild = _db.Create<GrandChild>();
			oldestGrandChild.Name = "Oldest Grandchild!";
			oldestGrandChild.Parent = oldestChild;

			var youngestGrandChild = _db.Create<GrandChild>();
			youngestGrandChild.Name = "Youngest Grandchild!";
			youngestGrandChild.Parent = oldestChild;

			oldestChild.GrandChildren.Add(oldestGrandChild);
			oldestChild.GrandChildren.Add(youngestGrandChild);

			// Save the youngest child - everyone should end up in the database via
			// youngest child => parent => children => other grandchildren
			await _db.SaveAsync(youngestChild);

			var dbParent = _db.Query<Parent>().FirstOrDefault(p => p.Name == "Parent!");
			Assert.IsNotNull(dbParent);
			Assert.AreEqual(3, dbParent.Children.Count);
			Assert.AreEqual(2, dbParent.Children.First(c => c.Name == "Oldest!").GrandChildren.Count);

			// TODO: Update items at all levels and re-save
		}
	}
}
