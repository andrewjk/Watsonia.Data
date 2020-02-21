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
		public async Task SavingEntitiesAsync()
		{
			// Get a random author's ID:
			var selectAuthor = Select.From("Author").Take(1).Where("LastName", SqlOperator.StartsWith, "P");
			var existingAuthors = await _db.LoadCollectionAsync<Author>(selectAuthor);
			var existingAuthor = existingAuthors[0];
			var existingAuthorID = ((IDynamicProxy)existingAuthor).__PrimaryKeyValue;

			// Update an existing author
			var author = await _db.LoadAsync<Author>(existingAuthorID);
			author.Rating = 85;
			await _db.SaveAsync(author);

			// Create an author
			var newAuthor = _db.Create<Author>();
			newAuthor.FirstName = "Eric";
			newAuthor.LastName = "Blair";
			await _db.SaveAsync(newAuthor);

			// Create an author more tersely
			var newAuthor2 = await _db.InsertAsync(new Author() { FirstName = "Eric", LastName = "Blair" });
			Assert.IsNotNull(newAuthor2);
		}
	}
}
