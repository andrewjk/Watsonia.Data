using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Text;
using Microsoft.Data.Sqlite;

namespace Watsonia.Data.TestPerformance
{
	static class Config
	{
		private const string SqliteConnectionString = @"Data Source=Data\Performance.sqlite";
		private const string SqlServerConnectionString = @"Data Source=[Server];Initial Catalog=PerformanceTests;Integrated Security=SSPI;Persist Security Info=False;Packet Size=4096";

		public const int RunCount = 5;
		public const int MaxOperations = 50;

		public const int PostCount = 500;
		public const int SportCount = 5;
		public const int TeamsPerSportCount = 10;
		public const int PlayersPerTeamCount = 10;

		public static bool UseSqlServer { get; } = false;

		public static string ConnectionString
		{
			get
			{
				return UseSqlServer ? SqlServerConnectionString : SqliteConnectionString;
			}
		}

		public static DbConnection OpenConnection()
		{
			if (UseSqlServer)
			{
				return OpenSqlServerConnection();
			}
			else
			{
				return OpenSqliteConnection();
			}
		}

		public static DbCommand CreateCommand(string query, DbConnection conn)
		{
			if (UseSqlServer)
			{
				return CreateSqlServerCommand(query, (SqlConnection)conn);
			}
			else
			{
				return CreateSqliteCommand(query, (SqliteConnection)conn);
			}
		}

		public static DbParameter CreateParameter(string name, object value)
		{
			if (UseSqlServer)
			{
				return CreateSqlServerParameter(name, value);
			}
			else
			{
				return CreateSqliteParameter(name, value);
			}
		}

		private static SqlConnection OpenSqlServerConnection()
		{
			var conn = new SqlConnection(ConnectionString);
			conn.Open();
			return conn;
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Security", "CA2100:Review SQL queries for security vulnerabilities", Justification = "<Pending>")]
		private static SqlCommand CreateSqlServerCommand(string query, SqlConnection conn)
		{
			var command = new SqlCommand(query, conn);
			return command;
		}

		private static SqlParameter CreateSqlServerParameter(string name, object value)
		{
			var command = new SqlParameter(name, value);
			return command;
		}

		private static SqliteConnection OpenSqliteConnection()
		{
			var conn = new SqliteConnection(ConnectionString);
			conn.Open();
			return conn;
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Security", "CA2100:Review SQL queries for security vulnerabilities", Justification = "<Pending>")]
		private static SqliteCommand CreateSqliteCommand(string query, SqliteConnection conn)
		{
			var command = new SqliteCommand(query, conn);
			return command;
		}

		private static SqliteParameter CreateSqliteParameter(string name, object value)
		{
			var parameter = new SqliteParameter(name, value);
			return parameter;
		}
	}
}
