﻿using System;
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
			var builder = new QueryBuilder.SqlServer.SqlServerCommandBuilder();
			builder.VisitStatement(statement, new QueryMapper(configuration));

			var command = new SqlCommand();
			command.CommandText = builder.CommandText.ToString();
			AddParameters(builder, command);
			return command;
		}

		private void AddParameters(QueryBuilder.SqlCommandBuilder builder, SqlCommand command)
		{
			for (var i = 0; i < builder.ParameterValues.Count; i++)
			{
				var parameter = BuildParameter("@" + i, builder.ParameterValues[i]);
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
				var parameter = BuildParameter("@" + i, parameters[i]);
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
				var parameter = BuildParameter(parameters[i].Name, parameters[i].Value);
				command.Parameters.Add(parameter);
			}
		}

		private SqlParameter BuildParameter(string name, object value)
		{
			var parameter = new SqlParameter();
			parameter.ParameterName = name;
			var parameterValue = value ?? DBNull.Value;
			parameter.Value = parameterValue;
			// NOTE: Can't check parameter.DbType because it throws exceptions if the type can't be mapped
			if (parameterValue.GetType() == typeof(DateTime) ||
				parameterValue.GetType() == typeof(DateTime?))
			{
				parameter.DbType = DbType.DateTime2;
			}
			return parameter;
		}
	}
}
