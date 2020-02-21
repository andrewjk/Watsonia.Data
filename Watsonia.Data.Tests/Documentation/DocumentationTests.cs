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
	public partial class DocumentationTests
	{
		private static readonly DocumentationDatabase _db = new DocumentationDatabase();

		[ClassInitialize]
		public static void Initialize(TestContext _)
		{
			if (!File.Exists(@"Data\DocumentationTests.sqlite"))
			{
				var file = File.Create(@"Data\DocumentationTests.sqlite");
				file.Dispose();
			}

			_db.UpdateDatabase();

			// Let's first delete all of the authors and books
			_db.Execute(Delete.From("Book").Where(true));
			_db.Execute(Delete.From("Author").Where(true));

			// Pop quiz: What do these authors have in common?
			_db.Insert(new Author { FirstName = "Stephen Jay", LastName = "Gould" });
			_db.Insert(new Author { FirstName = "Stephen", LastName = "Hawking" });
			_db.Insert(new Author { FirstName = "Stephen", LastName = "King" });
			_db.Insert(new Author { FirstName = "Amy", LastName = "Tan" });
			_db.Insert(new Author { FirstName = "John", LastName = "Updike" });
			_db.Insert(new Author { FirstName = "Thomas", LastName = "Pynchon" });
			_db.Insert(new Author { FirstName = "Tom", LastName = "Clancy" });
			_db.Insert(new Author { FirstName = "George", LastName = "Plimpton" });
			_db.Insert(new Author { FirstName = "J.K.", LastName = "Rowling" });
			_db.Insert(new Author { FirstName = "Michael", LastName = "Chabon" });
			_db.Insert(new Author { FirstName = "Johnathan", LastName = "Franzen" });
			_db.Insert(new Author { FirstName = "Tom", LastName = "Wolfe" });
			_db.Insert(new Author { FirstName = "Gore", LastName = "Vidal" });
			_db.Insert(new Author { FirstName = "Art", LastName = "Spieglman" });
			_db.Insert(new Author { FirstName = "Alan", LastName = "Moore" });
			_db.Insert(new Author { FirstName = "Dan", LastName = "Clowes" });
			_db.Insert(new Author { FirstName = "Mitch", LastName = "Albom" });
			_db.Insert(new Author { FirstName = "Gary", LastName = "Larson" });
			_db.Insert(new Author { FirstName = "Neil", LastName = "Gaiman" });
		}
	}
}
