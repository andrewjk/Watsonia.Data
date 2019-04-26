using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;
using System.Linq;
using System.Text;
using Watsonia.QueryBuilder;

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
			var builder = new QueryBuilder.SQLiteCommandBuilder();
			builder.VisitStatement(statement, new QueryMapper(configuration));

			var command = new SqliteCommand();
			command.CommandText = builder.CommandText.ToString();
			AddParameters(builder, command);
			return command;
		}

		private void AddParameters(SqlCommandBuilder builder, SqliteCommand command)
		{
			for (var i = 0; i < builder.ParameterValues.Count; i++)
			{
				var parameter = BuildParameter("@" + i, builder.ParameterValues[i]);
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
				var parameter = BuildParameter("@" + i, parameters[i]);
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
				var parameter = BuildParameter(parameters[i].Name, parameters[i].Value);
				command.Parameters.Add(parameter);
			}
		}

		private SqliteParameter BuildParameter(string name, object value)
		{
			var parameter = new SqliteParameter();
			parameter.ParameterName = name;
			// NOTE: Can't check parameter.DbType because it throws exceptions if the type can't be mapped
			var parameterValue = value ?? DBNull.Value;
			if (parameterValue.GetType() == typeof(char) ||
				parameterValue.GetType() == typeof(char?))
			{
				// HACK: SQLite doesn't seem to handle chars correctly?
				parameterValue = parameterValue.ToString();
			}
			else if (parameterValue.GetType() == typeof(DateTime) ||
					 parameterValue.GetType() == typeof(DateTime?))
			{
				// HACK: Is there a better way to do this? SQLite doesn't seem to ignore times on dates...
				var dateValue = Convert.ToDateTime(parameterValue);
				if (dateValue.Hour > 0 || dateValue.Minute > 0 || dateValue.Second > 0)
				{
					parameterValue = dateValue.ToString("yyyy-MM-dd hh:mm:ss");
				}
				else
				{
					parameterValue = dateValue.ToString("yyyy-MM-dd");
				}
			}
			parameter.Value = parameterValue;
			return parameter;
		}
	}
}
