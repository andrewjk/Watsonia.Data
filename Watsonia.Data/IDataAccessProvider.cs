using System.Collections.Generic;
using System.Data.Common;
using Watsonia.Data.Mapping;
using Watsonia.QueryBuilder;

namespace Watsonia.Data
{
	/// <summary>
	/// Provides access to a database and builds commands to execute against that database for fluent SQL statements.
	/// </summary>
	public interface IDataAccessProvider
	{
		/// <summary>
		/// Gets the name of the provider, which the user can use to specify which provider a database should use.
		/// </summary>
		/// <value>
		/// The name of the provider.
		/// </value>
		string ProviderName
		{
			get;
		}

		/// <summary>
		/// Opens and returns a database connection.
		/// </summary>
		/// <param name="configuration">The configuration options used for mapping to and accessing the database.</param>
		/// <returns>
		/// An open database connection.
		/// </returns>
		DbConnection OpenConnection(DatabaseConfiguration configuration);

		/// <summary>
		/// Ensures that the database is deleted.
		/// </summary>
		/// <param name="configuration">The configuration options used for mapping to and accessing the database.</param>
		void EnsureDatabaseDeleted(DatabaseConfiguration configuration);

		/// <summary>
		/// Ensures that the database is created.
		/// </summary>
		/// <param name="configuration">The configuration options used for mapping to and accessing the database.</param>
		void EnsureDatabaseCreated(DatabaseConfiguration configuration);

		/// <summary>
		/// Updates the database with any changes that have been made to tables and columns.
		/// </summary>
		/// <param name="tables">The tables that should exist in the database.</param>
		/// <param name="views">The views that should exist in the database.</param>
		/// <param name="procedures">The stored procedures that should exist in the database.</param>
		/// <param name="functions">The user-defined functions that should exist in the database.</param>
		/// <param name="configuration">The configuration options used for mapping to and accessing the database.</param>
		void UpdateDatabase(IEnumerable<MappedTable> tables, IEnumerable<MappedView> views, IEnumerable<MappedProcedure> procedures, IEnumerable<MappedFunction> functions, DatabaseConfiguration configuration);

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
		string GetUpdateScript(IEnumerable<MappedTable> tables, IEnumerable<MappedView> views, IEnumerable<MappedProcedure> procedures, IEnumerable<MappedFunction> functions, DatabaseConfiguration configuration);

		/// <summary>
		/// Gets columns that exist in the database but are not mapped.
		/// </summary>
		/// <param name="tables">The tables that should exist in the database.</param>
		/// <param name="views">The views that should exist in the database.</param>
		/// <param name="procedures">The stored procedures that should exist in the database.</param>
		/// <param name="functions">The user-defined functions that should exist in the database.</param>
		/// <param name="configuration">The configuration options used for mapping to and accessing the database.</param>
		/// <returns>
		/// A string containing the unmapped columns.
		/// </returns>
		string GetUnmappedColumns(IEnumerable<MappedTable> tables, IEnumerable<MappedView> views, IEnumerable<MappedProcedure> procedures, IEnumerable<MappedFunction> functions, DatabaseConfiguration configuration);

		/// <summary>
		/// Builds a command to return the ID of the last inserted item.
		/// </summary>
		/// <param name="configuration">The configuration options used for mapping to and accessing the database.</param>
		/// <returns>
		/// A database command that will return the ID of the last inserted item when executed.
		/// </returns>
		DbCommand BuildInsertedIDCommand(DatabaseConfiguration configuration);

		/// <summary>
		/// Builds a command from a statement to execute against the database.
		/// </summary>
		/// <param name="statement">The statement.</param>
		/// <param name="configuration">The configuration options used for mapping to and accessing the database.</param>
		/// <returns>
		/// A database command that can be used to execute the provided statement.
		/// </returns>
		DbCommand BuildCommand(Statement statement, DatabaseConfiguration configuration);

		/// <summary>
		/// Builds a command from a string and parameters to execute against the database.
		/// </summary>
		/// <param name="statement">The statement.</param>
		/// <param name="configuration">The configuration.</param>
		/// <param name="parameters">The parameters.</param>
		/// <returns>
		/// A database command that can be used to execute the provided statement.
		/// </returns>
		DbCommand BuildCommand(string statement, DatabaseConfiguration configuration, params object[] parameters);

		/// <summary>
		/// Builds a command to execute a stored procedure against the database.
		/// </summary>
		/// <param name="procedureName">The name of the procedure.</param>
		/// <param name="parameters">Any parameters that need to be passed to the stored procedure.</param>
		/// <returns></returns>
		DbCommand BuildProcedureCommand(string procedureName, params Parameter[] parameters);
	}
}
