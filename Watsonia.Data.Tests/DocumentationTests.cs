using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Watsonia.Data.Tests.Models;

namespace Watsonia.Data.Tests
{
	[TestClass]
	public class DocumentationTests
	{
		public const string ConnectionString = @"Data Source=Data\DocumentationTests.sdf;Persist Security Info=False";

		private readonly static Database db = new Database(DatabaseTests.ConnectionString, "Watsonia.Data.Tests.Models");

		[ClassInitialize]
		public static void Initialize(TestContext context)
		{
			if (!File.Exists(@"Data\DocumentationTests.sdf"))
			{
				File.Create(@"Data\DocumentationTests.sdf");
			}

			db.Configuration.ProviderName = "Watsonia.Data.SqlServerCe";
			db.UpdateDatabase();

			// Let's first delete all of the authors
			var delete = Delete.From("Author").Where(true);
			db.Execute(delete);

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
				Console.WriteLine(a.FullName);
			}
			Assert.AreEqual(2, query.ToList().Count);

			// Test a fluent SQL query
			var query2 = Select.From("Author").Where("LastName", SqlOperator.StartsWith, "P");
			foreach (Author a in db.LoadCollection<Author>(query2))
			{
				Console.WriteLine(a.FullName);
			}
			Assert.AreEqual(2, db.LoadCollection<Author>(query2).Count);

			// Test an SQL string
			var query3 = "SELECT * FROM Author WHERE LastName LIKE {0} + '%'";
			foreach (Author a in db.LoadCollection<Author>(query3, "P"))
			{
				Console.WriteLine(a.FullName);
			}
			Assert.AreEqual(2, db.LoadCollection<Author>(query3, "P").Count);

			// Test loading a scalar value
			var query4 = Select.From("Author").Count("*").Where("LastName", SqlOperator.StartsWith, "P");
			int count = (int)db.LoadValue(query4);
			Assert.AreEqual(2, count);

			// Test loading an item
			var author = db.Load<Author>(existingAuthorID);
			Assert.IsNotNull(author);
		}

		[TestMethod]
		public void TestSavingEntities()
		{
			// Get a random author's ID:
			var selectAuthor = Select.From("Author").Limit(1).Where("LastName", SqlOperator.StartsWith, "P");
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
			var update2 = "UPDATE Author SET Rating = 95 WHERE LastName LIKE {0} + '%'";
			db.Execute(update2, "P");

			// Delete using fluent SQL
			var delete = Delete.From("Author").Where("Rating", SqlOperator.IsLessThan, 80);
			db.Execute(delete);

			// Delete using an SQL string
			var delete2 = "DELETE FROM Author WHERE Rating < {0}";
			db.Execute(delete2, 80);
		}

		[TestMethod]
		public void TestValidation()
		{
		}
	}
}
