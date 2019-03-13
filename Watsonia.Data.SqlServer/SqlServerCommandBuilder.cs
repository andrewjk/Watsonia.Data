using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using Watsonia.QueryBuilder;

namespace Watsonia.Data.SqlServer
{
	/// <summary>
	/// Builds a command for a Microsoft SQL Server database from statements.
	/// </summary>
	internal class SqlServerCommandBuilder
	{
		public SqlServerCommandBuilder()
		{
		}

		public SqlCommand BuildCommand(Statement statement, DatabaseConfiguration configuration)
		{
			var builder = new Watsonia.QueryBuilder.SqlServerCommandBuilder();
			builder.VisitStatement(statement, new QueryMapper(configuration));

			var command = new SqlCommand();
			command.CommandText = builder.CommandText.ToString();
			AddParameters(builder, command);
			return command;
		}

		private void AddParameters(Watsonia.QueryBuilder.SqlCommandBuilder builder, SqlCommand command)
		{
			for (var i = 0; i < builder.ParameterValues.Count; i++)
			{
				var parameter = new SqlParameter();
				parameter.ParameterName = "@" + i;
				parameter.Value = builder.ParameterValues[i] ?? DBNull.Value;
				command.Parameters.Add(parameter);
			}
		}

		public SqlCommand BuildCommand(string statement, params object[] parameters)
		{
			var command = new SqlCommand();
			command.CommandText = statement;
			AddParameters(command, parameters);
			return command;
		}

		private void AddParameters(SqlCommand command, params object[] parameters)
		{
			for (var i = 0; i < parameters.Length; i++)
			{
				var parameter = new SqlParameter();
				parameter.ParameterName = "@" + i;
				parameter.Value = parameters[i] ?? DBNull.Value;
				command.Parameters.Add(parameter);
			}
		}

		public SqlCommand BuildProcedureCommand(string procedureName, params Parameter[] parameters)
		{
			var command = new SqlCommand();
			command.CommandType = System.Data.CommandType.StoredProcedure;
			command.CommandText = procedureName;
			AddProcedureParameters(command, parameters);
			return command;
		}

		private void AddProcedureParameters(SqlCommand command, params Parameter[] parameters)
		{
			for (var i = 0; i < parameters.Length; i++)
			{
				var parameter = new SqlParameter();
				parameter.ParameterName = parameters[i].Name;
				parameter.Value = parameters[i].Value ?? DBNull.Value;
				command.Parameters.Add(parameter);
			}
		}
	}
}
