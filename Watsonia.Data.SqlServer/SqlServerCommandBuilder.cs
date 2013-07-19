using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using Watsonia.Data.Sql;

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
			TSqlCommandBuilder builder = new TSqlCommandBuilder();
			builder.VisitStatement(statement, configuration);

			SqlCommand command = new SqlCommand();
			command.CommandText = builder.CommandText.ToString();
			AddParameters(builder, command);
			return command;
		}

		private void AddParameters(TSqlCommandBuilder builder, SqlCommand command)
		{
			for (int i = 0; i < builder.ParameterValues.Count; i++)
			{
				SqlParameter parameter = new SqlParameter();
				parameter.ParameterName = "@" + i;
				parameter.Value = builder.ParameterValues[i] ?? DBNull.Value;
				command.Parameters.Add(parameter);
			}
		}

		public SqlCommand BuildCommand(string statement, params object[] parameters)
		{
			SqlCommand command = new SqlCommand();
			command.CommandText = statement;
			AddParameters(command, parameters);
			return command;
		}

		private void AddParameters(SqlCommand command, params object[] parameters)
		{
			for (int i = 0; i < parameters.Length; i++)
			{
				SqlParameter parameter = new SqlParameter();
				parameter.ParameterName = "@" + i;
				parameter.Value = parameters[i] ?? DBNull.Value;
				command.Parameters.Add(parameter);
			}
		}
	}
}