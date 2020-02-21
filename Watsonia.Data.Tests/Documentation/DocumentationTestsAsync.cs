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
	[TestClass]
	public partial class DocumentationTestsAsync
	{
		private static readonly DocumentationDatabase _db = new DocumentationDatabase();

		[ClassInitialize]
		public static async Task InitializeAsync(TestContext _)
		{
			if (!File.Exists(@"Data\DocumentationTestsAsync.sqlite"))
			{
				var file = File.Create(@"Data\DocumentationTestsAsync.sqlite");
				file.Dispose();
			}

			_db.UpdateDatabase();

			// Let's first delete all of the authors and books
			await _db.ExecuteAsync(Delete.From("Book").Where(true));
			await _db.ExecuteAsync(Delete.From("Author").Where(true));

			// Pop quiz: What do these authors have in common?
			await _db.InsertAsync(new Author { FirstName = "Stephen Jay", LastName = "Gould" });
			await _db.InsertAsync(new Author { FirstName = "Stephen", LastName = "Hawking" });
			await _db.InsertAsync(new Author { FirstName = "Stephen", LastName = "King" });
			await _db.InsertAsync(new Author { FirstName = "Amy", LastName = "Tan" });
			await _db.InsertAsync(new Author { FirstName = "John", LastName = "Updike" });
			await _db.InsertAsync(new Author { FirstName = "Thomas", LastName = "Pynchon" });
			await _db.InsertAsync(new Author { FirstName = "Tom", LastName = "Clancy" });
			await _db.InsertAsync(new Author { FirstName = "George", LastName = "Plimpton" });
			await _db.InsertAsync(new Author { FirstName = "J.K.", LastName = "Rowling" });
			await _db.InsertAsync(new Author { FirstName = "Michael", LastName = "Chabon" });
			await _db.InsertAsync(new Author { FirstName = "Johnathan", LastName = "Franzen" });
			await _db.InsertAsync(new Author { FirstName = "Tom", LastName = "Wolfe" });
			await _db.InsertAsync(new Author { FirstName = "Gore", LastName = "Vidal" });
			await _db.InsertAsync(new Author { FirstName = "Art", LastName = "Spieglman" });
			await _db.InsertAsync(new Author { FirstName = "Alan", LastName = "Moore" });
			await _db.InsertAsync(new Author { FirstName = "Dan", LastName = "Clowes" });
			await _db.InsertAsync(new Author { FirstName = "Mitch", LastName = "Albom" });
			await _db.InsertAsync(new Author { FirstName = "Gary", LastName = "Larson" });
			await _db.InsertAsync(new Author { FirstName = "Neil", LastName = "Gaiman" });
		}
	}
}
