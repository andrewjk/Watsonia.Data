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
		public async Task ValidationAsync()
		{
			var author = _db.Create<Author>();

			// There should be an error for the first and last name being required
			Assert.IsFalse(((IDynamicProxy)author).StateTracker.IsValid, "Author should be invalid");
			Assert.AreEqual(2, ((IDynamicProxy)author).StateTracker.ValidationErrors.Count, "Author validation error count should be 2");

			// Fix those errors
			author.FirstName = "Eric";
			author.LastName = "Blair";

			// There shouldn't be any errors now
			Assert.IsTrue(((IDynamicProxy)author).StateTracker.IsValid, "Author should be valid");
			Assert.AreEqual(0, ((IDynamicProxy)author).StateTracker.ValidationErrors.Count, "Author validation error count should be 0");

			// Add a book without a title
			var book = _db.Create<Book>();
			author.Books.Add(book);

			// There should be an error for the book's title being required
			Assert.IsFalse(((IDynamicProxy)author).StateTracker.IsValid, "Author should be invalid because of a book");
			Assert.AreEqual(1, ((IDynamicProxy)author).StateTracker.ValidationErrors.Count, "Author validation error count should be 1");

			// Fix up the book
			book.Title = "1984";

			// Add another dodgy book and make sure that saving it to the database fails
			var book2 = _db.Create<Book>();
			author.Books.Add(book2);
			try
			{
				await _db.SaveAsync(author);
				Assert.Fail("Book with no title shouldn't save");
			}
			catch (ValidationException)
			{
			}
			Assert.AreEqual(1, ((IDynamicProxy)author).StateTracker.ValidationErrors.Count, "Book validation error count should be 1");
			Assert.AreEqual("Title", ((IDynamicProxy)author).StateTracker.ValidationErrors[0].PropertyName, "Book validation error should be title");

			// Fix up that book too
			book2.Title = "Animal Farm";

			// Add yet another dodgy book and make sure that saving it to the database fails
			var book3 = _db.Create<Book>();
			book3.Title = "Bad Book";
			author.Books.Add(book3);
			try
			{
				await _db.SaveAsync(author);
				Assert.Fail("Book with bad title shouldn't save");
			}
			catch (ValidationException)
			{
			}
			Assert.AreEqual(1, ((IDynamicProxy)author).StateTracker.ValidationErrors.Count, "Bad book validation error count should be 1");
			Assert.AreEqual("Nope", ((IDynamicProxy)author).StateTracker.ValidationErrors[0].ErrorMessage, "Book validation error should be nope");
		}
	}
}
