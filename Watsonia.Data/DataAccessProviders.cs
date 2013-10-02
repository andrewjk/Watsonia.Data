using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Watsonia.Data
{
	/// <summary>
	/// Provides access to the data access providers for different database systems.
	/// </summary>
	public static class DataAccessProviders
	{
		private static string _providerPath;
		private static List<IDataAccessProvider> _providers = null;

		/// <summary>
		/// Gets or sets the path in which to look for provider assemblies.
		/// </summary>
		/// <value>
		/// The provider path.
		/// </value>
		public static string ProviderPath
		{
			get
			{
				return _providerPath;
			}
			set
			{
				_providerPath = value;
			}
		}

		/// <summary>
		/// Gets the data access providers that have been placed in the provider path.
		/// </summary>
		/// <value>
		/// The providers.
		/// </value>
		[ImportMany(typeof(IDataAccessProvider))]
		public static List<IDataAccessProvider> Providers
		{
			get
			{
				if (_providers == null)
				{
					LoadProviders();
				}
				return _providers;
			}
		}

		/// <summary>
		/// Initializes the <see cref="DataAccessProviders" /> class.
		/// </summary>
		static DataAccessProviders()
		{
			// Default to the directory containing the executing assembly
			_providerPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
		}

		private static void LoadProviders()
		{
			// Scan through the supplied directory and get the assemblies
			var catalog = new AggregateCatalog();
			catalog.Catalogs.Add(new DirectoryCatalog(_providerPath));
			CompositionContainer container = new CompositionContainer(catalog);
			_providers = container.GetExportedValues<IDataAccessProvider>().ToList();
		}
	}
}
