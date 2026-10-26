using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Watsonia.Data.Extensions;
using Watsonia.Data.SQLite;
using Watsonia.QueryBuilder;

namespace Watsonia.Data.Tests.Extensions
{
	[TestClass]
	public class ExtensionsIntegrationTests
	{
		private const string ConnectionString = @"Data Source=Data/EntitiesTests.sqlite";
		private const string EntityNamespace = "Watsonia.Data.Tests.Database.Entities";

		private static Watsonia.Data.Database CreateDatabase()
		{
			return new Watsonia.Data.Database(new DatabaseConfiguration(
				new SQLiteDataAccessProvider(),
				ConnectionString,
				EntityNamespace));
		}

		[TestMethod]
		public void AddWatsoniaDataRegistersDatabase()
		{
			var services = new ServiceCollection();
			services.AddMemoryCache();
			services.AddWatsoniaData(options =>
			{
				options.ConnectionString = ConnectionString;
				options.DataAccessProvider = new SQLiteDataAccessProvider();
				options.EntityNamespace = EntityNamespace;
				options.LazyLoadingEnabled = false;
			});

			using var provider = services.BuildServiceProvider();
			var database = provider.GetRequiredService<Watsonia.Data.Database>();

			Assert.IsNotNull(database);
			Assert.IsFalse(database.Configuration.LazyLoadingEnabled);
			Assert.IsInstanceOfType(database.Cache, typeof(MemoryCacheStore));
		}

		[TestMethod]
		public void CommandExecutionEmitsActivities()
		{
			var activities = new List<Activity>();
			using var listener = new ActivityListener
			{
				ShouldListenTo = source => source.Name == "Watsonia.Data",
				Sample = (ref ActivityCreationOptions<ActivityContext> options) => ActivitySamplingResult.AllDataAndRecorded,
				ActivityStopped = activity =>
				{
					lock (activities)
					{
						activities.Add(activity);
					}
				}
			};
			ActivitySource.AddActivityListener(listener);

			var database = CreateDatabase();
			using var instrumentation = DatabaseInstrumentation.Attach(database);

			database.LoadCollection<Database.Entities.LoadingParent>(Select.From("LoadingParent"));

			Assert.IsTrue(activities.Any(a => a.OperationName == "db.command"));
			Assert.IsTrue(activities.Any(a => a.Tags.Any(t => t.Key == "db.statement")));
		}

		[TestMethod]
		public void LoggingReceivesCommandText()
		{
			var logger = new CapturingLogger();
			var database = CreateDatabase();
			using var instrumentation = DatabaseInstrumentation.Attach(database, new CapturingLoggerFactory(logger));

			database.LoadCollection<Database.Entities.LoadingParent>(Select.From("LoadingParent"));

			Assert.IsTrue(logger.Messages.Any(m => m.Contains("LoadingParent")));
		}

		[TestMethod]
		public void MemoryCacheStoreStoresValues()
		{
			using var memoryCache = new MemoryCache(new MemoryCacheOptions());
			var store = new MemoryCacheStore(memoryCache);
			var itemCache = store.GetOrAdd("TestKey", 60000, 100);
			var bag = new FakeValueBag();

			Assert.IsFalse(itemCache.ContainsKey(1L));
			itemCache.AddOrUpdate(1L, bag, null);

			Assert.IsTrue(itemCache.ContainsKey(1L));
			Assert.AreSame(bag, itemCache.GetValues(1L));
			Assert.IsTrue(store.ContainsItemWithKey("TestKey", 1L));

			itemCache.Remove(1L);
			Assert.IsFalse(itemCache.ContainsKey(1L));
		}

		private sealed class FakeValueBag : IValueBag
		{
		}

		private sealed class CapturingLogger : ILogger
		{
			public List<string> Messages { get; } = new List<string>();

			public IDisposable BeginScope<TState>(TState state)
			{
				return null;
			}

			public bool IsEnabled(LogLevel logLevel)
			{
				return true;
			}

			public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
			{
				lock (this.Messages)
				{
					this.Messages.Add(formatter(state, exception));
				}
			}
		}

		private sealed class CapturingLoggerFactory : ILoggerFactory
		{
			private readonly ILogger _logger;

			public CapturingLoggerFactory(ILogger logger)
			{
				_logger = logger;
			}

			public void AddProvider(ILoggerProvider provider)
			{
			}

			public ILogger CreateLogger(string categoryName)
			{
				return _logger;
			}

			public void Dispose()
			{
			}
		}

		[TestMethod]
		public void AddWatsoniaDataValidatesOptions()
		{
			var services = new ServiceCollection();
			Assert.ThrowsExactly<InvalidOperationException>(
				() => services.AddWatsoniaData(options => options.ConnectionString = ConnectionString));

			var services2 = new ServiceCollection();
			Assert.ThrowsExactly<InvalidOperationException>(
				() => services2.AddWatsoniaData(options =>
				{
					options.ConnectionString = ConnectionString;
					options.DataAccessProvider = new SQLiteDataAccessProvider();
				}));
		}

		[TestMethod]
		public void AddWatsoniaDataTwiceRegistersOnce()
		{
			var services = new ServiceCollection();
			services.AddMemoryCache();
			services.AddWatsoniaData(options =>
			{
				options.ConnectionString = ConnectionString;
				options.DataAccessProvider = new SQLiteDataAccessProvider();
				options.EntityNamespace = EntityNamespace;
			});
			services.AddWatsoniaData(options =>
			{
				options.ConnectionString = ConnectionString;
				options.DataAccessProvider = new SQLiteDataAccessProvider();
				options.EntityNamespace = EntityNamespace;
				options.LazyLoadingEnabled = false;
			});

			using var provider = services.BuildServiceProvider();
			var first = provider.GetRequiredService<Watsonia.Data.Database>();
			var second = provider.GetRequiredService<Watsonia.Data.Database>();

			Assert.AreSame(first, second);
			Assert.IsTrue(first.Configuration.LazyLoadingEnabled); // the first registration wins
		}

		[TestMethod]
		public void CachingDisabledKeepsTheDefaultStore()
		{
			var services = new ServiceCollection();
			services.AddMemoryCache();
			services.AddWatsoniaData(options =>
			{
				options.ConnectionString = ConnectionString;
				options.DataAccessProvider = new SQLiteDataAccessProvider();
				options.EntityNamespace = EntityNamespace;
				options.EnableCaching = false;
			});

			using var provider = services.BuildServiceProvider();
			var database = provider.GetRequiredService<Watsonia.Data.Database>();
			Assert.IsInstanceOfType(database.Cache, typeof(DatabaseItemCache));
		}

		[TestMethod]
		public void MissingMemoryCacheKeepsTheDefaultStore()
		{
			var services = new ServiceCollection();
			services.AddWatsoniaData(options =>
			{
				options.ConnectionString = ConnectionString;
				options.DataAccessProvider = new SQLiteDataAccessProvider();
				options.EntityNamespace = EntityNamespace;
			});

			using var provider = services.BuildServiceProvider();
			var database = provider.GetRequiredService<Watsonia.Data.Database>();
			Assert.IsInstanceOfType(database.Cache, typeof(DatabaseItemCache));
		}

		[TestMethod]
		public void DisposingInstrumentationStopsTracing()
		{
			var activities = new List<Activity>();
			using var listener = new ActivityListener
			{
				ShouldListenTo = source => source.Name == "Watsonia.Data",
				Sample = (ref ActivityCreationOptions<ActivityContext> options) => ActivitySamplingResult.AllDataAndRecorded,
				ActivityStopped = activity =>
				{
					lock (activities)
					{
						activities.Add(activity);
					}
				}
			};
			ActivitySource.AddActivityListener(listener);

			var database = CreateDatabase();
			var instrumentation = DatabaseInstrumentation.Attach(database);
			database.LoadCollection<Database.Entities.LoadingParent>(Select.From("LoadingParent"));
			var countWhileAttached = activities.Count;
			Assert.IsTrue(countWhileAttached > 0);

			instrumentation.Dispose();
			database.LoadCollection<Database.Entities.LoadingParent>(Select.From("LoadingParent"));

			Assert.AreEqual(countWhileAttached, activities.Count);
		}
	}
}
