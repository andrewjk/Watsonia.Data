using System;
using System.Collections.Generic;
using System.Data;
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

		public SqlCeCommand BuildCommand(Statement statement, DatabaseConfiguration configuration)
		{
			TSqlCommandBuilder builder = new TSqlCommandBuilder();
			builder.VisitStatement(statement, configuration);

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
				if (parameter.Value is char)
				{
					parameter.Size = 1;
				}
				// HACK: Have to explicitly set the DbType for SQL Server CE for some reason
				if (parameter.Value != null)
				{
					parameter.SqlDbType = DatabaseTypeFromFramework(parameter.Value.GetType());
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
				// HACK: Have to explicitly set the DbType for SQL Server CE for some reason
				if (parameter.Value != null)
				{
					parameter.SqlDbType = DatabaseTypeFromFramework(parameter.Value.GetType());
				}
				command.Parameters.Add(parameter);
			}
		}

		private SqlDbType DatabaseTypeFromFramework(Type type)
		{
			if (type == typeof(bool) || type == typeof(bool?))
			{
				return SqlDbType.Bit;
			}
			else if (type == typeof(DateTime) || type == typeof(DateTime?))
			{
				return SqlDbType.DateTime;
			}
			else if (type == typeof(decimal) || type == typeof(decimal?))
			{
				return SqlDbType.Decimal;
			}
			else if (type == typeof(double) || type == typeof(double?))
			{
				return SqlDbType.Float;
			}
			else if (type == typeof(int) || type == typeof(int?))
			{
				return SqlDbType.Int;
			}
			else if (type == typeof(long) || type == typeof(long?))
			{
				return SqlDbType.BigInt;
			}
			else if (type == typeof(byte) || type == typeof(byte?))
			{
				return SqlDbType.TinyInt;
			}
			else if (type == typeof(char))
			{
				return SqlDbType.NChar;
			}
			else if (type == typeof(string))
			{
				return SqlDbType.NVarChar;
			}
			else if (type == typeof(byte[]))
			{
				return SqlDbType.VarBinary;
			}
			else if (type == typeof(Guid) || type == typeof(Guid?))
			{
				return SqlDbType.UniqueIdentifier;
			}
			else
			{
				throw new InvalidOperationException("Invalid column type: " + type);
			}
		}
	}
}