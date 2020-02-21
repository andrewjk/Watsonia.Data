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
		public void HasChanges()
		{
			// Create an author and some books
			var author = _db.Create<Author>();
			author.FirstName = "Ernest";
			author.LastName = "Hemingway";
			author.Rating = 95;

			var book1 = _db.Create<Book>();
			book1.Title = "The Sun Also Rises";
			author.Books.Add(book1);

			var book2 = _db.Create<Book>();
			book2.Title = "The Old Man And The Sea";
			author.Books.Add(book2);

			// Everything should be new
			Assert.IsTrue(((IDynamicProxy)author).StateTracker.IsNew, "Author should be new");
			Assert.IsTrue(((IDynamicProxy)book1).StateTracker.IsNew, "Book 1 should be new");
			Assert.IsTrue(((IDynamicProxy)book2).StateTracker.IsNew, "Book 2 should be new");

			// Save the author
			_db.Save(author);

			// Everything should be not new and have no changes
			Assert.IsFalse(((IDynamicProxy)author).StateTracker.IsNew, "Author shouldn't be new");
			Assert.IsFalse(((IDynamicProxy)book1).StateTracker.IsNew, "Book 1 shouldn't be new");
			Assert.IsFalse(((IDynamicProxy)book2).StateTracker.IsNew, "Book 2 shouldn't be new");

			Assert.IsFalse(((IDynamicProxy)author).StateTracker.HasChanges, "Author shouldn't have changes");
			Assert.IsFalse(((IDynamicProxy)book1).StateTracker.HasChanges, "Book 1 shouldn't have changes");
			Assert.IsFalse(((IDynamicProxy)book2).StateTracker.HasChanges, "Book 2 shouldn't have changes");

			// Oops, fix some mistakes
			book1.Title = "The Sun Also Rises";
			book2.Title = "The Old Man and the Sea";

			// Some things should have changes
			Assert.IsFalse(((IDynamicProxy)author).StateTracker.HasChanges, "Author shouldn't have changes");
			Assert.IsFalse(((IDynamicProxy)book1).StateTracker.HasChanges, "Book 1 shouldn't have changes");
			Assert.IsTrue(((IDynamicProxy)book2).StateTracker.HasChanges, "Book 2 should have changes");

			// Save the author
			_db.Save(author);

			// Nothing should have changes
			Assert.IsFalse(((IDynamicProxy)author).StateTracker.HasChanges, "Author shouldn't have changes");
			Assert.IsFalse(((IDynamicProxy)book1).StateTracker.HasChanges, "Book 1 shouldn't have changes");
			Assert.IsFalse(((IDynamicProxy)book2).StateTracker.HasChanges, "Book 2 shouldn't have changes");
		}
	}
}
