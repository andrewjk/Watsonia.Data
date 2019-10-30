using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SqlClient;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Watsonia.Data.Mapping;
using Watsonia.QueryBuilder;

namespace Watsonia.Data.SqlServer
{
	/// <summary>
	/// Provides access to a Microsoft SQL Server database and builds commands to execute against that database for statements.
	/// </summary>
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
		/// Initializes a new instance of the <see cref="SqlServerDataAccessProvider" /> class.
		/// </summary>
		public SqlServerDataAccessProvider()
		{
		}

		/// <summary>
		/// Opens and returns a database connection.
		/// </summary>
		/// <param name="configuration">The configuration options used for mapping to and accessing the database.</param>
		/// <returns>
		/// An open database connection.
		/// </returns>
		public async Task<DbConnection> OpenConnectionAsync(DatabaseConfiguration configuration)
		{
			var connection = new SqlConnection(configuration.ConnectionString);
			await connection.OpenAsync();
			return connection;
		}

		/// <summary>
		/// Ensures that the database is deleted.
		/// </summary>
		/// <param name="configuration">The configuration options used for mapping to and accessing the database.</param>
		public async Task EnsureDatabaseDeletedAsync(DatabaseConfiguration configuration)
		{
			var updater = new SqlServerDatabaseUpdater(this, configuration);
			await updater.EnsureDatabaseDeletedAsync();
		}

		/// <summary>
		/// Ensures that the database is created.
		/// </summary>
		/// <param name="configuration">The configuration options used for mapping to and accessing the database.</param>
		public async Task EnsureDatabaseCreatedAsync(DatabaseConfiguration configuration)
		{
			var updater = new SqlServerDatabaseUpdater(this, configuration);
			await updater.EnsureDatabaseCreatedAsync();
		}

		/// <summary>
		/// Updates the database with any changes that have been made to tables and columns.
		/// </summary>
		/// <param name="tables">The tables that should exist in the database.</param>
		/// <param name="views">The views that should exist in the database.</param>
		/// <param name="procedures">The stored procedures that should exist in the database.</param>
		/// <param name="functions">The user-defined functions that should exist in the database.</param>
		/// <param name="configuration">The configuration options used for mapping to and accessing the database.</param>
		public async Task UpdateDatabaseAsync(IEnumerable<MappedTable> tables, IEnumerable<MappedView> views, IEnumerable<MappedProcedure> procedures, IEnumerable<MappedFunction> functions, DatabaseConfiguration configuration)
		{
			var updater = new SqlServerDatabaseUpdater(this, configuration);
			await updater.UpdateDatabaseAsync(tables, views, procedures, functions);
		}

		/// <summary>
		/// Gets the update script for any changes that have been made to tables and columns.
		/// </summary>
		/// <param name="tables">The tables that should exist in the database.</param>
		/// <param name="views">The views that should exist in the database.</param>
		/// <param name="procedures">The stored procedures that should exist in the database.</param>
		/// <param name="functions">The user-defined functions that should exist in the database.</param>
		/// <param name="configuration">The configuration options used for mapping to and accessing the database.</param>
		/// <returns>
		/// A string containing the update script.
		/// </returns>
		public async Task<string> GetUpdateScriptAsync(IEnumerable<MappedTable> tables, IEnumerable<MappedView> views, IEnumerable<MappedProcedure> procedures, IEnumerable<MappedFunction> functions, DatabaseConfiguration configuration)
		{
			var updater = new SqlServerDatabaseUpdater(this, configuration);
			return await updater.GetUpdateScriptAsync(tables, views, procedures, functions);
		}

		/// <summary>
		/// Builds a command to return the ID of the last inserted item.
		/// </summary>
		/// <param name="configuration">The configuration options used for mapping to and accessing the database.</param>
		/// <returns>
		/// A database command that will return the ID of the last inserted item when executed.
		/// </returns>
		public DbCommand BuildInsertedIDCommand(DatabaseConfiguration configuration)
		{
			var command = new SqlCommand();
			command.CommandText = "SELECT @@IDENTITY";
			return command;
		}

		/// <summary>
		/// Builds a command from a statement to execute against the database.
		/// </summary>
		/// <param name="statement">The statement.</param>
		/// <param name="configuration">The configuration options used for mapping to and accessing the database.</param>
		/// <returns>
		/// A database command that can be used to execute the provided statement.
		/// </returns>
		public DbCommand BuildCommand(Statement statement, DatabaseConfiguration configuration)
		{
			var builder = new SqlServerCommandBuilder();
			return builder.BuildCommand(statement, configuration);
		}

		/// <summary>
		/// Builds a command from a string and parameters to execute against the database.
		/// </summary>
		/// <param name="statement">The statement.</param>
		/// <param name="configuration">The configuration options used for mapping to and accessing the database.</param>
		/// <param name="parameters">The parameters.</param>
		/// <returns>
		/// A database command that can be used to execute the provided statement.
		/// </returns>
		public DbCommand BuildCommand(string statement, DatabaseConfiguration configuration, params object[] parameters)
		{
			var builder = new SqlServerCommandBuilder();
			return builder.BuildCommand(statement, parameters);
		}

		/// <summary>
		/// Builds a command to execute a stored procedure against the database.
		/// </summary>
		/// <param name="procedureName">The name of the procedure.</param>
		/// <param name="parameters">Any parameters that need to be passed to the stored procedure.</param>
		/// <returns></returns>
		/// <exception cref="NotImplementedException"></exception>
		public DbCommand BuildProcedureCommand(string procedureName, params Parameter[] parameters)
		{
			var builder = new SqlServerCommandBuilder();
			return builder.BuildProcedureCommand(procedureName, parameters);
		}

		/// <summary>
		/// Gets columns that exist in the database but are not mapped.
		/// </summary>
		/// <param name="tables">The tables that should exist in the database.</param>
		/// <param name="configuration">The configuration options used for mapping to and accessing the database.</param>
		/// <returns>
		/// A string containing the unmapped columns.
		/// </returns>
		/// <exception cref="NotImplementedException"></exception>
		public async Task<string> GetUnmappedColumnsAsync(IEnumerable<MappedTable> tables, DatabaseConfiguration configuration)
		{
			var updater = new SqlServerDatabaseUpdater(this, configuration);
			return await updater.GetUnmappedColumnsAsync(tables);
		}
	}
}
