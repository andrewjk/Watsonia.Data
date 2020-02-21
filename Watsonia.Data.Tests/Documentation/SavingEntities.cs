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
		public void SavingEntities()
		{
			// Get a random author's ID:
			var selectAuthor = Select.From("Author").Take(1).Where("LastName", SqlOperator.StartsWith, "P");
			var existingAuthors = _db.LoadCollection<Author>(selectAuthor);
			var existingAuthor = existingAuthors[0];
			var existingAuthorID = ((IDynamicProxy)existingAuthor).__PrimaryKeyValue;

			// Update an existing author
			var author = _db.Load<Author>(existingAuthorID);
			author.Rating = 85;
			_db.Save(author);

			// Create an author
			var newAuthor = _db.Create<Author>();
			newAuthor.FirstName = "Eric";
			newAuthor.LastName = "Blair";
			_db.Save(newAuthor);

			// Create an author more tersely
			var newAuthor2 = _db.Insert(new Author() { FirstName = "Eric", LastName = "Blair" });
			Assert.IsNotNull(newAuthor2);
		}
	}
}
