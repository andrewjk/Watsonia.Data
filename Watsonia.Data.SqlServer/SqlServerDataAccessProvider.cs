using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Data.Common;
using System.Data.SqlClient;
using System.Reflection;
using System.Text;

namespace Watsonia.Data.SqlServer
{
	/// <summary>
	/// Provides access to a Microsoft SQL Server database and builds commands to execute against that database for statements.
	/// </summary>
	[Export(typeof(IDataAccessProvider))]
	public sealed class SqlServerDataAccessProvider : IDataAccessProvider
	{
		/// <summary>
		/// Gets the name of the provider, which the user can use to specify which provider a database should use.
		/// </summary>
		/// <value>
		/// The name of the provider.
		/// </value>
		public string ProviderName
		{
			get
			{
				return "Watsonia.Data.SqlServer";
			}
		}

		/// <summary>
		/// Gets or sets the configuration options used for mapping to and accessing the database.
		/// </summary>
		/// <value>
		/// The configuration.
		/// </value>
		public DatabaseConfiguration Configuration
		{
			get;
			set;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="SqlServerDataAccessProvider" /> class.
		/// </summary>
		public SqlServerDataAccessProvider()
		{
			// This constructor is required for MEF
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="SqlServerDataAccessProvider" /> class.
		/// </summary>
		/// <param name="configuration">The configuration options used for mapping to and accessing the database.</param>
		public SqlServerDataAccessProvider(DatabaseConfiguration configuration)
		{
			this.Configuration = configuration;
		}

		/// <summary>
		/// Opens and returns a database connection.
		/// </summary>
		/// <returns>
		/// An open database connection.
		/// </returns>
		/// <exception cref="System.InvalidOperationException">Configuration not initialized</exception>
		public DbConnection OpenConnection()
		{
			if (this.Configuration == null)
			{
				throw new InvalidOperationException("Configuration not initialized");
			}

			if (string.IsNullOrEmpty(this.Configuration.ConnectionString))
			{
				throw new InvalidOperationException("Connection string not initialized");
			}

			var connection = new SqlConnection(this.Configuration.ConnectionString);
			connection.Open();
			return connection;
		}

		/// <summary>
		/// Updates the database with any changes that have been made to tables and columns.
		/// </summary>
		/// <param name="tables">The tables that should exist in the database.</param>
		public void UpdateDatabase(IEnumerable<MappedTable> tables)
		{
			var updater = new SqlServerDatabaseUpdater(this);
			updater.UpdateDatabase(tables);
		}

		/// <summary>
		/// Gets the update script for any changes that have been made to tables and columns.
		/// </summary>
		/// <param name="tables">The tables that should exist in the database.</param>
		/// <returns>
		/// A string containing the update script.
		/// </returns>
		public string GetUpdateScript(IEnumerable<MappedTable> tables)
		{
			var updater = new SqlServerDatabaseUpdater(this);
			return updater.GetUpdateScript(tables);
		}

		/// <summary>
		/// Builds a command to return the ID of the last inserted item.
		/// </summary>
		/// <returns>
		/// A database command that will return the ID of the last inserted item when executed.
		/// </returns>
		public DbCommand BuildInsertedIDCommand()
		{
			var command = new SqlCommand();
			command.CommandText = "SELECT @@IDENTITY";
			return command;
		}

		/// <summary>
		/// Builds a command from a statement to execute against the database.
		/// </summary>
		/// <param name="statement">The statement.</param>
		/// <returns>
		/// A database command that can be used to execute the provided statement.
		/// </returns>
		public DbCommand BuildCommand(Statement statement)
		{
			var builder = new SqlServerCommandBuilder();
			return builder.BuildCommand(statement);
		}

		/// <summary>
		/// Builds a command from a string and parameters to execute against the database.
		/// </summary>
		/// <param name="statement">The statement.</param>
		/// <param name="parameters">The parameters.</param>
		/// <returns>
		/// A database command that can be used to execute the provided statement.
		/// </returns>
		public DbCommand BuildCommand(string statement, params object[] parameters)
		{
			var builder = new SqlServerCommandBuilder();
			return builder.BuildCommand(statement, parameters);
		}
	}
}
