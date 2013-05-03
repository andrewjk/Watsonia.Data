using System;
using System.Collections.Generic;
using System.Data.SqlServerCe;
using System.Linq;
using System.Text;
using Watsonia.Data.Sql;
using Watsonia.Data.SqlServer;

namespace Watsonia.Data.SqlServerCe
{
	/// <summary>
	/// Builds a command for a Microsoft SQL Server Compact Edition database from statements.
	/// </summary>
	internal sealed class SqlServerCeCommandBuilder
	{
		public SqlServerCeCommandBuilder()
		{
		}

		public SqlCeCommand BuildCommand(Statement statement)
		{
			TSqlCommandBuilder builder = new TSqlCommandBuilder();
			builder.VisitStatement(statement);

			SqlCeCommand command = new SqlCeCommand();
			command.CommandText = builder.CommandText.ToString();
			AddParameters(builder, command);
			return command;
		}

		private void AddParameters(TSqlCommandBuilder builder, SqlCeCommand command)
		{
			for (int i = 0; i < builder.ParameterValues.Count; i++)
			{
				SqlCeParameter parameter = new SqlCeParameter();
				parameter.ParameterName = "@" + i;
				parameter.Value = builder.ParameterValues[i] ?? DBNull.Value;
				// HACK: Ugh, something's not quite working with SQL Server CE
				if (command.CommandText.Contains("LIKE '%' + " + parameter.ParameterName + " + '%'"))
				{
					command.CommandText = command.CommandText.Replace(
						"LIKE '%' + " + parameter.ParameterName + " + '%'",
						"LIKE " + parameter.ParameterName);
					parameter.Value = "%" + parameter.Value.ToString() + "%";
				}
				else if (command.CommandText.Contains("LIKE " + parameter.ParameterName + " + '%'"))
				{
					command.CommandText = command.CommandText.Replace(
						"LIKE " + parameter.ParameterName + " + '%'",
						"LIKE " + parameter.ParameterName);
					parameter.Value = parameter.Value.ToString() + "%";
				}
				else if (command.CommandText.Contains("LIKE '%' + " + parameter.ParameterName))
				{
					command.CommandText = command.CommandText.Replace(
						"LIKE '%' + " + parameter.ParameterName,
						"LIKE " + parameter.ParameterName);
					parameter.Value = "%" + parameter.Value.ToString();
				}
				command.Parameters.Add(parameter);
			}
		}

		public SqlCeCommand BuildCommand(string statement, params object[] parameters)
		{
			SqlCeCommand command = new SqlCeCommand();
			command.CommandText = statement;
			AddParameters(command, parameters);
			return command;
		}

		private void AddParameters(SqlCeCommand command, params object[] parameters)
		{
			for (int i = 0; i < parameters.Length; i++)
			{
				SqlCeParameter parameter = new SqlCeParameter();
				parameter.ParameterName = "@" + i;
				parameter.Value = parameters[i] ?? DBNull.Value;
				command.Parameters.Add(parameter);
			}
		}
	}
}