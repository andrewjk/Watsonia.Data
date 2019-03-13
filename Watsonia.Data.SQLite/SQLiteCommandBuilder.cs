using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;
using System.Linq;
using System.Text;
using Watsonia.Data.Sql;

namespace Watsonia.Data.SQLite
{
	/// <summary>
	/// Builds a command for an SQLite database from statements.
	/// </summary>
	internal class SQLiteCommandBuilder
	{
		public SQLiteCommandBuilder()
		{
		}

		public SqliteCommand BuildCommand(Statement statement, DatabaseConfiguration configuration)
		{
			var builder = new SqlCommandBuilder();
			builder.VisitStatement(statement, configuration);

			var command = new SqliteCommand();
			command.CommandText = builder.CommandText.ToString();
			AddParameters(builder, command);
			return command;
		}

		private void AddParameters(SqlCommandBuilder builder, SqliteCommand command)
		{
			for (var i = 0; i < builder.ParameterValues.Count; i++)
			{
				var parameter = new SqliteParameter();
				parameter.ParameterName = "@" + i;
				parameter.Value = builder.ParameterValues[i] ?? DBNull.Value;
				CheckParameterValue(parameter);
				command.Parameters.Add(parameter);
			}
		}

		public SqliteCommand BuildCommand(string statement, params object[] parameters)
		{
			var command = new SqliteCommand();
			command.CommandText = statement;
			AddParameters(command, parameters);
			return command;
		}

		private void AddParameters(SqliteCommand command, params object[] parameters)
		{
			for (var i = 0; i < parameters.Length; i++)
			{
				var parameter = new SqliteParameter();
				parameter.ParameterName = "@" + i;
				parameter.Value = parameters[i] ?? DBNull.Value;
				command.Parameters.Add(parameter);
			}
		}

		public SqliteCommand BuildProcedureCommand(string procedureName, params Parameter[] parameters)
		{
			var command = new SqliteCommand();
			command.CommandType = System.Data.CommandType.StoredProcedure;
			command.CommandText = procedureName;
			AddProcedureParameters(command, parameters);
			return command;
		}

		private void AddProcedureParameters(SqliteCommand command, params Parameter[] parameters)
		{
			for (var i = 0; i < parameters.Length; i++)
			{
				var parameter = new SqliteParameter();
				parameter.ParameterName = parameters[i].Name;
				parameter.Value = parameters[i].Value ?? DBNull.Value;
				CheckParameterValue(parameter);
				command.Parameters.Add(parameter);
			}
		}

		private void CheckParameterValue(SqliteParameter parameter)
		{
			if (parameter.Value.GetType() == typeof(char))
			{
				// HACK: SQLite doesn't seem to handle chars correctly?
				parameter.Value = parameter.Value.ToString();
			}
			else if (parameter.Value.GetType() == typeof(DateTime))
			{
				// HACK: Is there a better way to do this? SQLite doesn't seem to ignore times on dates...
				var value = Convert.ToDateTime(parameter.Value);
				if (value.Hour > 0 || value.Minute > 0 || value.Second > 0)
				{
					parameter.Value = value.ToString("yyyy-MM-dd hh:mm:ss");
				}
				else
				{
					parameter.Value = value.ToString("yyyy-MM-dd");
				}
			}
		}
	}
}
