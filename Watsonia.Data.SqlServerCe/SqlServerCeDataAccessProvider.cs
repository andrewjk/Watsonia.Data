﻿using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Data.Common;
using System.Data.SqlServerCe;
using System.Reflection;
using System.Text;

namespace Watsonia.Data.SqlServerCe
{
	/// <summary>
	/// Provides access to a Microsoft SQL Server Compact Edition database and builds commands to execute against that database for statements.
	/// </summary>
	[Export(typeof(IDataAccessProvider))]
	public sealed class SqlServerCeDataAccessProvider : IDataAccessProvider
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
				return "Watsonia.Data.SqlServerCe";
			}
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="SqlServerCeDataAccessProvider" /> class.
		/// </summary>
		public SqlServerCeDataAccessProvider()
		{
		}

		/// <summary>
		/// Opens and returns a database connection.
		/// </summary>
		/// <param name="configuration">The configuration options used for mapping to and accessing the database.</param>
		/// <returns>
		/// An open database connection.
		/// </returns>
		public DbConnection OpenConnection(DatabaseConfiguration configuration)
		{
			var connection = new SqlCeConnection(configuration.ConnectionString);
			connection.Open();
			return connection;
		}

		/// <summary>
		/// Updates the database with any changes that have been made to tables and columns.
		/// </summary>
		/// <param name="tables">The tables that should exist in the database.</param>
		/// <param name="views">The views that should exist in the database.</param>
		/// <param name="configuration">The configuration options used for mapping to and accessing the database.</param>
		public void UpdateDatabase(IEnumerable<MappedTable> tables, IEnumerable<MappedView> views, DatabaseConfiguration configuration)
		{
			var updater = new SqlServerCeDatabaseUpdater(this, configuration);
			updater.UpdateDatabase(tables, views);
		}

		/// <summary>
		/// Gets the update script for any changes that have been made to tables and columns.
		/// </summary>
		/// <param name="tables">The tables that should exist in the database.</param>
		/// <param name="views">The views that should exist in the database.</param>
		/// <param name="configuration">The configuration options used for mapping to and accessing the database.</param>
		/// <returns>
		/// A string containing the update script.
		/// </returns>
		public string GetUpdateScript(IEnumerable<MappedTable> tables, IEnumerable<MappedView> views, DatabaseConfiguration configuration)
		{
			var updater = new SqlServerCeDatabaseUpdater(this, configuration);
			return updater.GetUpdateScript(tables, views);
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
			var command = new SqlCeCommand();
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
			var builder = new SqlServerCeCommandBuilder();
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
			var builder = new SqlServerCeCommandBuilder();
			return builder.BuildCommand(statement, parameters);
		}

		/// <summary>
		/// Gets columns that exist in the database but are not mapped.
		/// </summary>
		/// <param name="tables">The tables that should exist in the database.</param>
		/// <param name="views">The views that should exist in the database.</param>
		/// <param name="configuration">The configuration options used for mapping to and accessing the database.</param>
		/// <returns>
		/// A string containing the unmapped columns.
		/// </returns>
		/// <exception cref="System.NotImplementedException"></exception>
		public string GetUnmappedColumns(IEnumerable<MappedTable> tables, IEnumerable<MappedView> views, DatabaseConfiguration configuration)
		{
			var updater = new SqlServerCeDatabaseUpdater(this, configuration);
			return updater.GetUnmappedColumns(tables, views);
		}
	}
}
