using System;

namespace Watsonia.Data.Extensions
{
	/// <summary>
	/// Options for registering Watsonia.Data with a dependency injection container.
	/// </summary>
	public class DatabaseOptions
	{
		/// <summary>
		/// Gets or sets the connection string used to access the database.
		/// </summary>
		public string ConnectionString { get; set; }

		/// <summary>
		/// Gets or sets the data access provider, e.g. SQLiteDataAccessProvider or SqlServerDataAccessProvider.
		/// </summary>
		public IDataAccessProvider DataAccessProvider { get; set; }

		/// <summary>
		/// Gets or sets the namespace in which the entity classes are located.
		/// </summary>
		public string EntityNamespace { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether lazy loading is enabled. Defaults to true.
		/// </summary>
		public bool LazyLoadingEnabled { get; set; } = true;

		/// <summary>
		/// Gets or sets a value indicating whether items are cached in the registered IMemoryCache. Defaults to true.
		/// </summary>
		public bool EnableCaching { get; set; } = true;

		/// <summary>
		/// Gets or sets an action that can customise the database configuration before the database is created.
		/// </summary>
		public Action<DatabaseConfiguration> Configure { get; set; }
	}
}
