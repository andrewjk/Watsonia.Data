using System.Collections.Generic;
using System.Data.Common;

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
		/// Gets the database configuration, which tells us how to access the database and how to map entities to database objects.
		/// </summary>
		DatabaseConfiguration Configuration
		{
			get;
			set;
		}

		/// <summary>
		/// Opens and returns a database connection.
		/// </summary>
		/// <returns>An open database connection.</returns>
		DbConnection OpenConnection();

		/// <summary>
		/// Updates the database with any changes that have been made to tables and columns.
		/// </summary>
		/// <param name="tables">The tables that should exist in the database.</param>
		void UpdateDatabase(IEnumerable<MappedTable> tables);

		/// <summary>
		/// Gets the update script for any changes that have been made to tables and columns.
		/// </summary>
		/// <param name="tables">The tables that should exist in the database.</param>
		/// <returns>A string containing the update script.</returns>
		string GetUpdateScript(IEnumerable<MappedTable> tables);

		/// <summary>
		/// Builds a command to return the ID of the last inserted item.
		/// </summary>
		/// <returns>A database command that will return the ID of the last inserted item when executed.</returns>
		DbCommand BuildInsertedIDCommand();

		/// <summary>
		/// Builds a command from a statement to execute against the database.
		/// </summary>
		/// <param name="statement">The statement.</param>
		/// <returns>A database command that can be used to execute the provided statement.</returns>
		DbCommand BuildCommand(Statement statement);

		/// <summary>
		/// Builds a command from a string and parameters to execute against the database.
		/// </summary>
		/// <param name="statement">The statement.</param>
		/// <param name="parameters">The parameters.</param>
		/// <returns>A database command that can be used to execute the provided statement.</returns>
		DbCommand BuildCommand(string statement, params object[] parameters);
	}
}