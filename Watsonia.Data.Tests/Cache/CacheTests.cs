﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Watsonia.Data.Tests.Cache
{
	[TestClass]
	public class CacheTests
	{
		private static CacheDatabase db = new CacheDatabase();

		[ClassInitialize]
		public static void Initialize(TestContext context)
		{
			var author = db.Create<Author>();
			var book = db.Create<Book>();
			var chapter = db.Create<Chapter>();
		}

		[TestMethod]
		public void TestValueBagClasses()
		{
			// Check that the author bag has all of its properties
			var assembly = AppDomain.CurrentDomain.GetAssemblies().First(a => a.FullName.StartsWith("Watsonia.Data.DynamicProxies,"));
			var authorBagType = assembly.GetType("Watsonia.Data.DynamicProxies.CacheDatabaseAuthorValueBag");
			var authorBag = (IValueBag)authorBagType.GetConstructor(Type.EmptyTypes).Invoke(Type.EmptyTypes);
			Assert.IsNotNull(authorBag);

			var authorProperties = authorBagType.GetProperties();
			Assert.AreEqual(7, authorProperties.Length);
			Assert.IsTrue(authorProperties.Any(p => p.Name == "ID"));
			Assert.IsTrue(authorProperties.Any(p => p.Name == "FirstName"));
			Assert.IsTrue(authorProperties.Any(p => p.Name == "LastName"));
			Assert.IsTrue(authorProperties.Any(p => p.Name == "Email"));
			Assert.IsTrue(authorProperties.Any(p => p.Name == "DateOfBirth"));
			Assert.IsTrue(authorProperties.Any(p => p.Name == "Age"));
			Assert.IsTrue(authorProperties.Any(p => p.Name == "Rating"));

			// Check that the book bag has all of its properties
			var bookBagType = assembly.GetType("Watsonia.Data.DynamicProxies.CacheDatabaseBookValueBag");
			var bookBag = bookBagType.GetConstructor(Type.EmptyTypes).Invoke(Type.EmptyTypes);
			Assert.IsNotNull(bookBag);

			var bookProperties = bookBagType.GetProperties();
			Assert.AreEqual(3, bookProperties.Length);
			Assert.IsTrue(bookProperties.Any(p => p.Name == "ID"));
			Assert.IsTrue(bookProperties.Any(p => p.Name == "Title"));
			Assert.IsTrue(bookProperties.Any(p => p.Name == "AuthorID"));

			// Check that the chapter bag has all of its properties
			var chapterBagType = assembly.GetType("Watsonia.Data.DynamicProxies.CacheDatabaseChapterValueBag");
			var chapterBag = chapterBagType.GetConstructor(Type.EmptyTypes).Invoke(Type.EmptyTypes);
			Assert.IsNotNull(chapterBag);

			var chapterProperties = chapterBagType.GetProperties();
			Assert.AreEqual(4, chapterProperties.Length);
			Assert.IsTrue(chapterProperties.Any(p => p.Name == "ID"));
			Assert.IsTrue(chapterProperties.Any(p => p.Name == "Title"));
			Assert.IsTrue(chapterProperties.Any(p => p.Name == "PageCount"));
			Assert.IsTrue(chapterProperties.Any(p => p.Name == "BookID"));

			// Check SetValuesFromBag
			var author = db.Create<Author>();
			authorProperties.First(p => p.Name == "ID").SetValue(authorBag, 25);
			authorProperties.First(p => p.Name == "FirstName").SetValue(authorBag, "Dan");
			authorProperties.First(p => p.Name == "LastName").SetValue(authorBag, "Brown");
			authorProperties.First(p => p.Name == "Email").SetValue(authorBag, "dan.brown@example.com");
			authorProperties.First(p => p.Name == "DateOfBirth").SetValue(authorBag, new DateTime(1960, 1, 1));
			authorProperties.First(p => p.Name == "Age").SetValue(authorBag, null);
			authorProperties.First(p => p.Name == "Rating").SetValue(authorBag, 10);
			var authorProxy = (IDynamicProxy)author;
			authorProxy.SetValuesFromBag(authorBag);
			Assert.AreEqual(25, (long)authorProxy.PrimaryKeyValue);
			Assert.AreEqual("Dan", author.FirstName);
			Assert.AreEqual("Brown", author.LastName);
			Assert.AreEqual("dan.brown@example.com", author.Email);
			Assert.AreEqual(new DateTime(1960, 1, 1), author.DateOfBirth);
			Assert.AreEqual(null, author.Age);
			Assert.AreEqual(10, author.Rating);

			// Check GetBagFromValues
			var authorBag2 = authorProxy.GetBagFromValues();
			Assert.AreEqual(25, (long)authorProperties.First(p => p.Name == "ID").GetValue(authorBag2));
			Assert.AreEqual("Dan", authorProperties.First(p => p.Name == "FirstName").GetValue(authorBag2));
			Assert.AreEqual("Brown", authorProperties.First(p => p.Name == "LastName").GetValue(authorBag2));
			Assert.AreEqual("dan.brown@example.com", authorProperties.First(p => p.Name == "Email").GetValue(authorBag2));
			Assert.AreEqual(new DateTime(1960, 1, 1), authorProperties.First(p => p.Name == "DateOfBirth").GetValue(authorBag2));
			Assert.AreEqual(null, authorProperties.First(p => p.Name == "Age").GetValue(authorBag2));
			Assert.AreEqual(10, authorProperties.First(p => p.Name == "Rating").GetValue(authorBag2));
		}

		[TestMethod]
		public void TestItemCache()
		{
			// Add test data
			var cache = new ItemCache(900000, 20);
			for (int i = 0; i < 20; i++)
			{
				cache.AddOrUpdate(
					i,
					new TestValueBag() { ID = i },
					(object o, IValueBag values) => values);
			}
			Assert.AreEqual(20, cache.ItemCount);

			// Test that the initial lists look good
			Assert.AreEqual(20, cache.ItemsByAccessedTime.Count);
			Assert.AreEqual(0, cache.ItemsByAccessedTime[0]);
			Assert.AreEqual(1, cache.ItemsByAccessedTime[1]);
			Assert.AreEqual(2, cache.ItemsByAccessedTime[2]);
			Assert.AreEqual(3, cache.ItemsByAccessedTime[3]);
			Assert.AreEqual(4, cache.ItemsByAccessedTime[4]);
			Assert.AreEqual(18, cache.ItemsByAccessedTime[18]);
			Assert.AreEqual(19, cache.ItemsByAccessedTime[19]);

			Assert.AreEqual(20, cache.ItemsByLoadedTime.Count);
			Assert.AreEqual(0, cache.ItemsByLoadedTime[0].Item1);
			Assert.AreEqual(1, cache.ItemsByLoadedTime[1].Item1);
			Assert.AreEqual(2, cache.ItemsByLoadedTime[2].Item1);
			Assert.AreEqual(3, cache.ItemsByLoadedTime[3].Item1);
			Assert.AreEqual(4, cache.ItemsByLoadedTime[4].Item1);
			Assert.AreEqual(18, cache.ItemsByLoadedTime[18].Item1);
			Assert.AreEqual(19, cache.ItemsByLoadedTime[19].Item1);

			// Test that getting something from the cache updates the associated lists correctly
			var value = cache.GetValues(4);
			Assert.AreEqual(20, cache.ItemsByAccessedTime.Count);
			Assert.AreEqual(0, cache.ItemsByAccessedTime[0]);
			Assert.AreEqual(1, cache.ItemsByAccessedTime[1]);
			Assert.AreEqual(2, cache.ItemsByAccessedTime[2]);
			Assert.AreEqual(3, cache.ItemsByAccessedTime[3]);
			Assert.AreEqual(5, cache.ItemsByAccessedTime[4]);
			Assert.AreEqual(19, cache.ItemsByAccessedTime[18]);
			Assert.AreEqual(4, cache.ItemsByAccessedTime[19]);

			Assert.AreEqual(20, cache.ItemsByLoadedTime.Count);
			Assert.AreEqual(0, cache.ItemsByLoadedTime[0].Item1);
			Assert.AreEqual(1, cache.ItemsByLoadedTime[1].Item1);
			Assert.AreEqual(2, cache.ItemsByLoadedTime[2].Item1);
			Assert.AreEqual(3, cache.ItemsByLoadedTime[3].Item1);
			Assert.AreEqual(4, cache.ItemsByLoadedTime[4].Item1);
			Assert.AreEqual(18, cache.ItemsByLoadedTime[18].Item1);
			Assert.AreEqual(19, cache.ItemsByLoadedTime[19].Item1);
		}

		[TestMethod]
		public void TestItemCacheMaxItems()
		{
			// Test that adding 25 items to a cache that has a limit of 20 has the right number of items
			var cache = new ItemCache(900000, 20);
			for (int i = 0; i < 25; i++)
			{
				cache.AddOrUpdate(
					i,
					new TestValueBag() { ID = i },
					(object o, IValueBag values) => values);
			}
			Assert.AreEqual(20, cache.ItemCount);

			// Test that the values look good
			Assert.AreEqual(20, cache.ItemsByAccessedTime.Count);
			Assert.AreEqual(5, cache.ItemsByAccessedTime[0]);
			Assert.AreEqual(6, cache.ItemsByAccessedTime[1]);
			Assert.AreEqual(7, cache.ItemsByAccessedTime[2]);
			Assert.AreEqual(8, cache.ItemsByAccessedTime[3]);
			Assert.AreEqual(9, cache.ItemsByAccessedTime[4]);
			Assert.AreEqual(23, cache.ItemsByAccessedTime[18]);
			Assert.AreEqual(24, cache.ItemsByAccessedTime[19]);

			Assert.AreEqual(20, cache.ItemsByLoadedTime.Count);
			Assert.AreEqual(5, cache.ItemsByLoadedTime[0].Item1);
			Assert.AreEqual(6, cache.ItemsByLoadedTime[1].Item1);
			Assert.AreEqual(7, cache.ItemsByLoadedTime[2].Item1);
			Assert.AreEqual(8, cache.ItemsByLoadedTime[3].Item1);
			Assert.AreEqual(9, cache.ItemsByLoadedTime[4].Item1);
			Assert.AreEqual(23, cache.ItemsByLoadedTime[18].Item1);
			Assert.AreEqual(24, cache.ItemsByLoadedTime[19].Item1);
		}

		[TestMethod]
		public void TestItemCacheExpiry()
		{
			// Add test data
			var cache = new ItemCache(900000, 20);
			for (int i = 0; i < 20; i++)
			{
				cache.AddOrUpdate(
					i,
					new TestValueBag() { ID = i },
					(object o, IValueBag values) => values);
			}
			Assert.AreEqual(20, cache.ItemCount);

			// Test that the initial lists look good
			Assert.AreEqual(20, cache.ItemsByAccessedTime.Count);
			Assert.AreEqual(0, cache.ItemsByAccessedTime[0]);
			Assert.AreEqual(1, cache.ItemsByAccessedTime[1]);
			Assert.AreEqual(2, cache.ItemsByAccessedTime[2]);
			Assert.AreEqual(3, cache.ItemsByAccessedTime[3]);
			Assert.AreEqual(4, cache.ItemsByAccessedTime[4]);
			Assert.AreEqual(18, cache.ItemsByAccessedTime[18]);
			Assert.AreEqual(19, cache.ItemsByAccessedTime[19]);

			Assert.AreEqual(20, cache.ItemsByLoadedTime.Count);
			Assert.AreEqual(0, cache.ItemsByLoadedTime[0].Item1);
			Assert.AreEqual(1, cache.ItemsByLoadedTime[1].Item1);
			Assert.AreEqual(2, cache.ItemsByLoadedTime[2].Item1);
			Assert.AreEqual(3, cache.ItemsByLoadedTime[3].Item1);
			Assert.AreEqual(4, cache.ItemsByLoadedTime[4].Item1);
			Assert.AreEqual(18, cache.ItemsByLoadedTime[18].Item1);
			Assert.AreEqual(19, cache.ItemsByLoadedTime[19].Item1);

			// Test that checking for an expired record returns false and updates the associated lists correctly
			// We have to fudge this because we can't update the date or wait for fifteen minutes
			cache.ItemsByLoadedTime[4] = new Tuple<object, DateTime>(
				cache.ItemsByLoadedTime[4].Item1,
				cache.ItemsByLoadedTime[4].Item2.AddMinutes(-16));
			Assert.AreEqual(false, cache.ContainsKey(4));

			Assert.AreEqual(19, cache.ItemsByLoadedTime.Count);
			Assert.AreEqual(0, cache.ItemsByAccessedTime[0]);
			Assert.AreEqual(1, cache.ItemsByAccessedTime[1]);
			Assert.AreEqual(2, cache.ItemsByAccessedTime[2]);
			Assert.AreEqual(3, cache.ItemsByAccessedTime[3]);
			Assert.AreEqual(5, cache.ItemsByAccessedTime[4]);
			Assert.AreEqual(18, cache.ItemsByAccessedTime[17]);
			Assert.AreEqual(19, cache.ItemsByAccessedTime[18]);

			Assert.AreEqual(19, cache.ItemsByLoadedTime.Count);
			Assert.AreEqual(0, cache.ItemsByLoadedTime[0].Item1);
			Assert.AreEqual(1, cache.ItemsByLoadedTime[1].Item1);
			Assert.AreEqual(2, cache.ItemsByLoadedTime[2].Item1);
			Assert.AreEqual(3, cache.ItemsByLoadedTime[3].Item1);
			Assert.AreEqual(5, cache.ItemsByLoadedTime[4].Item1);
			Assert.AreEqual(18, cache.ItemsByLoadedTime[17].Item1);
			Assert.AreEqual(19, cache.ItemsByLoadedTime[18].Item1);
		}

		private class TestValueBag : IValueBag
		{
			public int ID;
		}
	}
}
