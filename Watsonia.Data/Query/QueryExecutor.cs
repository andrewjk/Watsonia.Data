using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Watsonia.Data.Query.Expressions;
using Watsonia.Data.Query.Translation;
using Watsonia.QueryBuilder;

namespace Watsonia.Data.Query
{
	public class QueryExecutor
	{
		private readonly Database _database;

		public QueryExecutor(Database database)
		{
			_database = database;
		}

		public IEnumerable<T> Execute<T>(QueryCommand command, Func<FieldReader, T> fnProjector, MappingEntity entity, object[] paramValues)
		{
			Expression expression = SubstituteNamedValues(command, paramValues);
			SelectStatement select = StatementCreator.Compile(expression);
			if (entity != null)
			{
				select.IncludePaths.AddRange(entity.IncludePaths);
			}

			// Entity queries materialize tracked proxy items through the database
			if (entity != null || fnProjector == null)
			{
				return _database.LoadCollection<T>(select);
			}

			// Other projections are materialized client-side by the projector
			var results = new List<T>();
			using (var connection = _database.OpenConnection())
			using (var dbCommand = _database.Configuration.DataAccessProvider.BuildCommand(select, _database.Configuration))
			{
				dbCommand.Connection = connection;
				using (var reader = dbCommand.ExecuteReader())
				{
					var fieldReader = new DbFieldReader(reader);
					while (reader.Read())
					{
						results.Add(fnProjector(fieldReader));
					}
				}
			}
			return results;
		}

		/// <summary>
		/// Replaces the named values in a query command with the values supplied at execution time.
		/// </summary>
		private static Expression SubstituteNamedValues(QueryCommand command, object[] paramValues)
		{
			if (paramValues == null || paramValues.Length == 0)
			{
				return command.Expression;
			}

			var namedValues = NamedValueGatherer.Gather(command.Expression);
			if (namedValues.Count == 0)
			{
				return command.Expression;
			}

			// The execution plan builds the parameter values and the named values from the
			// same expression, so they are in the same order.
			var replaceWith = new Expression[namedValues.Count];
			for (var i = 0; i < namedValues.Count; i++)
			{
				replaceWith[i] = MakeConstant(paramValues[i], namedValues[i].Type);
			}

			return DbExpressionReplacer.ReplaceAll(command.Expression, namedValues.Cast<Expression>().ToArray(), replaceWith);
		}

		private static Expression MakeConstant(object value, Type type)
		{
			if (value == null && type.IsValueType && Nullable.GetUnderlyingType(type) == null)
			{
				type = typeof(Nullable<>).MakeGenericType(type);
			}
			return Expression.Constant(value, type);
		}
	}
}
