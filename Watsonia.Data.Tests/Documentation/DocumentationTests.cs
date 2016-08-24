using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Watsonia.Data.Tests.Documentation
{
	/// <summary>
	/// This contains tests for all of the things we talk about in the documentation.
	/// </summary>
	[TestClass]
	public class DocumentationTests
	{
		public const string ConnectionString = @"Data Source=Data\DocumentationTests.sdf;Persist Security Info=False";

		private readonly static Database db = new Database(DocumentationTests.ConnectionString, "Watsonia.Data.Tests.Documentation");

		[ClassInitialize]
		public static void Initialize(TestContext context)
		{
			if (!File.Exists(@"Data\DocumentationTests.sdf"))
			{
				File.Create(@"Data\DocumentationTests.sdf");
			}

			db.Configuration.ProviderName = "Watsonia.Data.SqlServerCe";
			db.UpdateDatabase();

			// Let's first delete all of the authors and books
			db.Execute(Delete.From("Book").Where(true));
			db.Execute(Delete.From("Author").Where(true));

			// Pop quiz: What do these authors have in common?
			db.Insert(new Author { FirstName = "Stephen Jay", LastName = "Gould" });
			db.Insert(new Author { FirstName = "Stephen", LastName = "Hawking" });
			db.Insert(new Author { FirstName = "Stephen", LastName = "King" });
			db.Insert(new Author { FirstName = "Amy", LastName = "Tan" });
			db.Insert(new Author { FirstName = "John", LastName = "Updike" });
			db.Insert(new Author { FirstName = "Thomas", LastName = "Pynchon" });
			db.Insert(new Author { FirstName = "Tom", LastName = "Clancy" });
			db.Insert(new Author { FirstName = "George", LastName = "Plimpton" });
			db.Insert(new Author { FirstName = "J.K.", LastName = "Rowling" });
			db.Insert(new Author { FirstName = "Michael", LastName = "Chabon" });
			db.Insert(new Author { FirstName = "Johnathan", LastName = "Franzen" });
			db.Insert(new Author { FirstName = "Tom", LastName = "Wolfe" });
			db.Insert(new Author { FirstName = "Gore", LastName = "Vidal" });
			db.Insert(new Author { FirstName = "Art", LastName = "Spieglman" });
			db.Insert(new Author { FirstName = "Alan", LastName = "Moore" });
			db.Insert(new Author { FirstName = "Dan", LastName = "Clowes" });
			db.Insert(new Author { FirstName = "Mitch", LastName = "Albom" });
			db.Insert(new Author { FirstName = "Gary", LastName = "Larson" });
			db.Insert(new Author { FirstName = "Neil", LastName = "Gaiman" });
		}

		[TestMethod]
		public void TestLoadingEntities()
		{
			// It would be embarrassing if any of these didn't work!
			object existingAuthorID = null;

			// Test a LINQ query
			var query = from a in db.Query<Author>()
						where a.LastName.StartsWith("P", StringComparison.InvariantCultureIgnoreCase)
						select a;
			foreach (Author a in query)
			{
				if (existingAuthorID == null)
				{
					existingAuthorID = ((IDynamicProxy)a).PrimaryKeyValue;
				}
			}
			Assert.AreEqual(2, query.ToList().Count);

			// Test a fluent SQL query
			var query2 = Select.From("Author").Where("LastName", SqlOperator.StartsWith, "P");
			Assert.AreEqual(2, db.LoadCollection<Author>(query2).Count);

			var query22 = Select.From<Author>().Where(a => a.LastName.StartsWith("P", StringComparison.InvariantCultureIgnoreCase));
			Assert.AreEqual(2, db.LoadCollection<Author>(query22).Count);

			// Test an SQL string
			var query3 = "SELECT * FROM Author WHERE LastName LIKE @0";
			Assert.AreEqual(2, db.LoadCollection<Author>(query3, "P%").Count);

			// Test loading a scalar value
			var query4 = Select.From("Author").Count("*").Where("LastName", SqlOperator.StartsWith, "P");
			int count = (int)db.LoadValue(query4);
			Assert.AreEqual(2, count);

			var query44 = Select.From<Author>().Count().Where(a => a.LastName.StartsWith("P", StringComparison.InvariantCultureIgnoreCase));
			int count44 = (int)db.LoadValue(query44);
			Assert.AreEqual(2, count44);

			// Test loading an item
			var author = db.Load<Author>(existingAuthorID);
			Assert.IsNotNull(author);
		}

		[TestMethod]
		public void TestSavingEntities()
		{
			// Get a random author's ID:
			var selectAuthor = Select.From("Author").Take(1).Where("LastName", SqlOperator.StartsWith, "P");
			var existingAuthors = db.LoadCollection<Author>(selectAuthor);
			var existingAuthor = existingAuthors[0];
			var existingAuthorID = ((IDynamicProxy)existingAuthor).PrimaryKeyValue;

			// Update an existing author
			var author = db.Load<Author>(existingAuthorID);
			author.Rating = 85;
			db.Save(author);

			// Create an author
			var newAuthor = db.Create<Author>();
			newAuthor.FirstName = "Eric";
			newAuthor.LastName = "Blair";
			db.Save(newAuthor);

			// Create an author more tersely
			var newAuthor2 = db.Insert(new Author() { FirstName = "Eric", LastName = "Blair" });
		}

		[TestMethod]
		public void TestBulkInsertUpdateAndDelete()
		{
			// Update using fluent SQL
			var update = Update.Table("Author").Set("Rating", 95).Where("LastName", SqlOperator.StartsWith, "P");
			db.Execute(update);

			// Update using an SQL string
			var update2 = "UPDATE Author SET Rating = 95 WHERE LastName LIKE @0";
			db.Execute(update2, "P%");

			// Delete using fluent SQL
			var delete = Delete.From("Author").Where("Rating", SqlOperator.IsLessThan, 80);
			db.Execute(delete);

			// Delete using an SQL string
			var delete2 = "DELETE FROM Author WHERE Rating < @0";
			db.Execute(delete2, 80);
		}

		[TestMethod]
		public void TestBulkInsertUpdateAndDeleteWithExpressions()
		{
			// Not sure how useful these would be:
			//var insert = Insert.Into<Author>().Values(new Author() { FirstName = "Enter", LastName = "Name" }, 20);
			//db.Execute(insert);

			//var insert = Insert.Into<Author>().Select(Select.From<Author>().Where(a => a.LastName.StartsWith("P"));
			//db.Execute(insert);

			var update = Update.Table<Author>().Set(a => a.Rating, 95).Where(a => a.LastName.StartsWith("P"));
			db.Execute(update);

			var delete = Delete.From<Author>().Where(a => a.Rating < 80);
			db.Execute(delete);
		}

		[TestMethod]
		public void TestValidation()
		{
			Author author = db.Create<Author>();

			// There should be an error for the first and last name being required
			Assert.IsFalse(author.IsValid, "Author should be invalid");
			Assert.AreEqual(2, author.ValidationErrors.Count, "Author validation error count should be 2");

			// Fix those errors
			author.FirstName = "Eric";
			author.LastName = "Blair";

			// There shouldn't be any errors now
			Assert.IsTrue(author.IsValid, "Author should be valid");
			Assert.AreEqual(0, author.ValidationErrors.Count, "Author validation error count should be 0");

			// Add a book without a title
			Book book = db.Create<Book>();
			author.Books.Add(book);

			// There should be an error for the book's title being required
			Assert.IsFalse(author.IsValid, "Author should be invalid because of a book");
			Assert.AreEqual(1, author.ValidationErrors.Count, "Author validation error count should be 1");

			// Fix up the book
			book.Title = "1984";

			// Add another dodgy book and make sure that saving it to the database fails
			Book book2 = db.Create<Book>();
			author.Books.Add(book2);
			try
			{
				db.Save(author);
				Assert.Fail("Book with no title shouldn't save");
			}
			catch (ValidationException)
			{
			}
			Assert.AreEqual(1, author.ValidationErrors.Count, "Book validation error count should be 1");
			Assert.AreEqual("Title", author.ValidationErrors[0].PropertyName, "Book validation error should be title");

			// Fix up that book too
			book2.Title = "Animal Farm";

			// Add yet another dodgy book and make sure that saving it to the database fails
			Book book3 = db.Create<Book>();
			book3.Title = "Bad Book";
			author.Books.Add(book3);
			try
			{
				db.Save(author);
				Assert.Fail("Book with bad title shouldn't save");
			}
			catch (ValidationException)
			{
			}
			Assert.AreEqual(1, author.ValidationErrors.Count, "Bad book validation error count should be 1");
			Assert.AreEqual("Nope", author.ValidationErrors[0].ErrorMessage, "Book validation error should be nope");
		}

		[TestMethod]
		public void TestHasChanges()
		{
			// Create an author and some books
			Author author = db.Create<Author>();
			author.FirstName = "Ernest";
			author.LastName = "Hemingway";
			author.Rating = 95;

			Book book1 = db.Create<Book>();
			book1.Title = "The Sun Also Rises";
			author.Books.Add(book1);

			Book book2 = db.Create<Book>();
			book2.Title = "The Old Man And The Sea";
			author.Books.Add(book2);

			// Everything should be new
			Assert.IsTrue(author.IsNew, "Author should be new");
			Assert.IsTrue(book1.IsNew, "Book 1 should be new");
			Assert.IsTrue(book2.IsNew, "Book 2 should be new");

			// Save the author
			db.Save(author);

			// Everything should be not new and have no changes
			Assert.IsFalse(author.IsNew, "Author shouldn't be new");
			Assert.IsFalse(book1.IsNew, "Book 1 shouldn't be new");
			Assert.IsFalse(book2.IsNew, "Book 2 shouldn't be new");

			Assert.IsFalse(author.HasChanges, "Author shouldn't have changes");
			Assert.IsFalse(book1.HasChanges, "Book 1 shouldn't have changes");
			Assert.IsFalse(book2.HasChanges, "Book 2 shouldn't have changes");

			// Oops, fix some mistakes
			book1.Title = "The Sun Also Rises";
			book2.Title = "The Old Man and the Sea";

			// Some things should have changes
			Assert.IsFalse(author.HasChanges, "Author shouldn't have changes");
			Assert.IsFalse(book1.HasChanges, "Book 1 shouldn't have changes");
			Assert.IsTrue(book2.HasChanges, "Book 2 should have changes");

			// Save the author
			db.Save(author);

			// Nothing should have changes
			Assert.IsFalse(author.HasChanges, "Author shouldn't have changes");
			Assert.IsFalse(book1.HasChanges, "Book 1 shouldn't have changes");
			Assert.IsFalse(book2.HasChanges, "Book 2 shouldn't have changes");
		}
	}
}