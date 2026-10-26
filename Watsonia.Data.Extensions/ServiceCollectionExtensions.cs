using System;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Watsonia.Data.Extensions
{
	/// <summary>
	/// Registration extensions for Watsonia.Data.
	/// </summary>
	public static class ServiceCollectionExtensions
	{
		/// <summary>
		/// Registers a Watsonia.Data database with the service collection, wired up to logging,
		/// tracing and (if an IMemoryCache is registered) item caching.
		/// </summary>
		/// <param name="services">The service collection.</param>
		/// <param name="configure">Configures the database options.</param>
		/// <returns>The service collection.</returns>
		public static IServiceCollection AddWatsoniaData(this IServiceCollection services, Action<DatabaseOptions> configure = null)
		{
			if (services == null)
			{
				throw new ArgumentNullException(nameof(services));
			}

			var options = new DatabaseOptions();
			configure?.Invoke(options);
			if (options.DataAccessProvider == null)
			{
				throw new InvalidOperationException("The database options must specify a data access provider");
			}

			if (string.IsNullOrEmpty(options.EntityNamespace))
			{
				throw new InvalidOperationException("The database options must specify an entity namespace");
			}

			services.TryAddSingleton(options);
			services.TryAddSingleton(sp =>
			{
				var databaseOptions = sp.GetRequiredService<DatabaseOptions>();
				var configuration = new DatabaseConfiguration(
					databaseOptions.DataAccessProvider,
					databaseOptions.ConnectionString,
					databaseOptions.EntityNamespace)
				{
					LazyLoadingEnabled = databaseOptions.LazyLoadingEnabled
				};
				databaseOptions.Configure?.Invoke(configuration);

				var database = new Watsonia.Data.Database(configuration);

				if (databaseOptions.EnableCaching)
				{
					var memoryCache = sp.GetService<IMemoryCache>();
					if (memoryCache != null)
					{
						database.Cache = new MemoryCacheStore(memoryCache);
					}
				}

				DatabaseInstrumentation.Attach(database, sp.GetService<ILoggerFactory>());

				return database;
			});

			return services;
		}
	}
}
